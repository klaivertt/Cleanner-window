using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NettoyerPc.Core;

namespace NettoyerPc.Modules
{
    public class GamingModule : ICleaningModule
    {
        public string Name => "Gaming";

        public List<CleaningStep> GetSteps(CleaningMode mode)
        {
            var steps = new List<CleaningStep>
            {
                new() { Name = "Steam cache (tous disques)" }
            };

            if (mode == CleaningMode.DeepClean)
            {
                steps.Add(new() { Name = "DirectX Shader Cache" });
                steps.Add(new() { Name = "Epic Games / Battle.net" });
            }

            return steps;
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (step.Name.Contains("Steam"))
                {
                    CleanSteam(step);
                }
                else if (step.Name.Contains("DirectX"))
                {
                    CleanDirectXCache(step);
                }
                else if (step.Name.Contains("Epic"))
                {
                    CleanGamingPlatforms(step);
                }
            }, cancellationToken);
        }

        private void CleanSteam(CleaningStep step)
        {
            // Steam principal
            var steamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
            
            DeleteDirectoryIfExists(Path.Combine(steamPath, "logs"), step);
            DeleteDirectoryIfExists(Path.Combine(steamPath, "appcache"), step);
            DeleteDirectoryIfExists(Path.Combine(steamPath, "dumps"), step);
            DeleteDirectoryIfExists(Path.Combine(steamPath, "steamapps", "shadercache"), step);

            // SteamLibrary sur tous les disques
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    var steamLibPath = Path.Combine(drive.RootDirectory.FullName, "SteamLibrary");
                    DeleteDirectoryIfExists(Path.Combine(steamLibPath, "steamapps", "shadercache"), step);
                    
                    var steamAppsPath = Path.Combine(drive.RootDirectory.FullName, "steamapps");
                    DeleteDirectoryIfExists(Path.Combine(steamAppsPath, "shadercache"), step);
                }
            }
        }

        private void CleanDirectXCache(CleaningStep step)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            
            DeleteDirectoryIfExists(Path.Combine(localAppData, "D3DSCache"), step);
            DeleteDirectoryIfExists(Path.Combine(localAppData, "AMD", "DxCache"), step);
            DeleteDirectoryIfExists(Path.Combine(localAppData, "NVIDIA", "DXCache"), step);
        }

        private void CleanGamingPlatforms(CleaningStep step)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            
            // Epic Games
            DeleteDirectoryIfExists(Path.Combine(localAppData, "EpicGamesLauncher", "Saved", "webcache"), step);
            
            // Battle.net
            DeleteDirectoryIfExists(Path.Combine(appData, "Battle.net", "Cache"), step);
        }

        private void DeleteDirectoryIfExists(string path, CleaningStep step)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    var dirInfo = new DirectoryInfo(path);
                    step.SpaceFreed += GetDirectorySize(dirInfo);
                    Directory.Delete(path, true);
                    step.FilesDeleted++;
                }
                catch { }
            }
        }

        private long GetDirectorySize(DirectoryInfo dirInfo)
        {
            long size = 0;
            try
            {
                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    try { size += file.Length; }
                    catch { }
                }
            }
            catch { }
            return size;
        }
    }
}
