using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                new() { Name = "Steam cache (tous disques)",       Category = "gaming" },
                new() { Name = "EA App / Origin (cache, logs)",    Category = "gaming" },
                new() { Name = "Ubisoft Connect (cache)",          Category = "gaming" },
                new() { Name = "GOG Galaxy (cache)",               Category = "gaming" },
                new() { Name = "Riot Games / League (cache)",      Category = "gaming" },
                new() { Name = "Minecraft (logs, crash reports)",  Category = "gaming" }
            };

            if (mode == CleaningMode.DeepClean || mode == CleaningMode.Advanced)
            {
                steps.Add(new() { Name = "DirectX Shader Cache",  Category = "gaming" });
                steps.Add(new() { Name = "Epic Games / Battle.net",Category = "gaming" });
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
                else if (step.Name.Contains("EA App"))
                    CleanEAApp(step);
                else if (step.Name.Contains("Ubisoft"))
                    CleanUbisoft(step);
                else if (step.Name.Contains("GOG"))
                    CleanGOG(step);
                else if (step.Name.Contains("Riot"))
                    CleanRiot(step);
                else if (step.Name.Contains("Minecraft"))
                    CleanMinecraft(step);
            }, cancellationToken);
        }

        private void CleanSteam(CleaningStep step)
        {
            KillProcess(new[] { "steam", "steamservice", "steamwebhelper" }, step);
            var steamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
            DeleteDirectoryIfExists(Path.Combine(steamPath, "logs"), step);
            DeleteDirectoryIfExists(Path.Combine(steamPath, "appcache"), step);
            DeleteDirectoryIfExists(Path.Combine(steamPath, "dumps"), step);
            DeleteDirectoryIfExists(Path.Combine(steamPath, "steamapps", "shadercache"), step);
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
            KillProcess(new[] { "EpicGamesLauncher", "EpicWebHelper", "Battle.net", "Battle.net.exe" }, step);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData      = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            DeleteDirectoryIfExists(Path.Combine(localAppData, "EpicGamesLauncher", "Saved", "webcache"), step);
            DeleteDirectoryIfExists(Path.Combine(appData, "Battle.net", "Cache"), step);
        }

        private void DeleteDirectoryIfExists(string path, CleaningStep step)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    var dirInfo = new DirectoryInfo(path);
                    int fileCount = 0;
                    foreach (var f in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                    {
                        try { step.SpaceFreed += f.Length; fileCount++; } catch { }
                    }
                    Directory.Delete(path, true);
                    step.FilesDeleted += Math.Max(1, fileCount);
                    step.AddLog($"Supprimé : {path} ({fileCount} fichier(s))");
                }
                catch { }
            }
        }

        /// <summary>Tue les processus donnés avant le nettoyage pour libérer les fichiers.</summary>
        private static void KillProcess(string[] names, CleaningStep step)
        {
            bool killed = false;
            foreach (var name in names)
                foreach (var p in Process.GetProcessesByName(name))
                    try { p.Kill(entireProcessTree: true); p.WaitForExit(3000); step.AddLog($"Fermé : {p.ProcessName}.exe"); killed = true; } catch { }
            if (killed) Thread.Sleep(500);
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

        private void CleanEAApp(CleaningStep step)
        {
            KillProcess(new[] { "EADesktop", "EABackgroundService", "Origin", "OriginWebHelperService" }, step);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            DeleteDirectoryIfExists(Path.Combine(local, "Electronic Arts", "EA Desktop", "Cache"), step);
            DeleteDirectoryIfExists(Path.Combine(local, "Electronic Arts", "EA Desktop", "Logs"), step);
            DeleteDirectoryIfExists(Path.Combine(appData, "Origin", "webcache"), step);
            DeleteDirectoryIfExists(Path.Combine(local,   "Origin", "Webcache"), step);
        }

        private void CleanUbisoft(CleaningStep step)
        {
            KillProcess(new[] { "upc", "UbisoftGameLauncher", "UbisoftGameLauncher64" }, step);
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDirectoryIfExists(Path.Combine(local, "Ubisoft Game Launcher", "cache"),    step);
            DeleteDirectoryIfExists(Path.Combine(local, "Ubisoft Game Launcher", "webcache"), step);
            DeleteDirectoryIfExists(Path.Combine(local, "Ubisoft Game Launcher", "logs"),     step);
        }

        private void CleanGOG(CleaningStep step)
        {
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            DeleteDirectoryIfExists(Path.Combine(local,   "GOG.com", "Galaxy", "cache"),    step);
            DeleteDirectoryIfExists(Path.Combine(local,   "GOG.com", "Galaxy", "webcache"), step);
            DeleteDirectoryIfExists(Path.Combine(appData, "GOG.com", "Galaxy", "logs"),      step);
        }

        private void CleanRiot(CleaningStep step)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDirectoryIfExists(Path.Combine(local, "Riot Games", "Riot Client", "Data", "Temp"), step);
            DeleteDirectoryIfExists(Path.Combine(local, "Riot Games", "Riot Client", "Data", "Logs"), step);
            DeleteDirectoryIfExists(Path.Combine(local, "VALORANT", "Service", "Logs"), step);
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Fixed) continue;
                DeleteDirectoryIfExists(Path.Combine(drive.RootDirectory.FullName, "Riot Games", "League of Legends", "Logs"), step);
                DeleteDirectoryIfExists(Path.Combine(drive.RootDirectory.FullName, "Riot Games", "VALORANT", "ShooterGame", "Logs"), step);
            }
        }

        private void CleanMinecraft(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var mcDir   = Path.Combine(appData, ".minecraft");
            DeleteDirectoryIfExists(Path.Combine(mcDir, "logs"),          step);
            DeleteDirectoryIfExists(Path.Combine(mcDir, "crash-reports"), step);
            // Minecraft for Windows (Bedrock)
            DeleteDirectoryIfExists(Path.Combine(local, "Packages", "Microsoft.MinecraftUWP_8wekyb3d8bbwe", "LocalState", "logs"), step);
        }
    }
}
