using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NettoyerPc.Core;

namespace NettoyerPc.Modules
{
    public class BrowserModule : ICleaningModule
    {
        public string Name => "Navigateurs";

        public List<CleaningStep> GetSteps(CleaningMode mode)
        {
            return new List<CleaningStep>
            {
                new() { Name = "Nettoyage Firefox" },
                new() { Name = "Nettoyage Chrome" },
                new() { Name = "Nettoyage Edge" },
                new() { Name = "Nettoyage Brave/Opera" }
            };
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (step.Name.Contains("Firefox"))
                {
                    CleanFirefox(step);
                }
                else if (step.Name.Contains("Chrome"))
                {
                    CleanChrome(step);
                }
                else if (step.Name.Contains("Edge"))
                {
                    CleanEdge(step);
                }
                else if (step.Name.Contains("Brave"))
                {
                    CleanBraveOpera(step);
                }
            }, cancellationToken);
        }

        private void CleanFirefox(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var firefoxProfilesPath = Path.Combine(appData, "Mozilla", "Firefox", "Profiles");
            if (Directory.Exists(firefoxProfilesPath))
            {
                foreach (var profile in Directory.GetDirectories(firefoxProfilesPath))
                {
                    DeleteDirectoryIfExists(Path.Combine(profile, "cache2"), step);
                    DeleteDirectoryIfExists(Path.Combine(profile, "jumpListCache"), step);
                    DeleteDirectoryIfExists(Path.Combine(profile, "thumbnails"), step);
                    DeleteDirectoryIfExists(Path.Combine(profile, "crashes"), step);
                    
                    // SQLite temp files
                    try
                    {
                        foreach (var file in Directory.GetFiles(profile, "*.sqlite-wal"))
                        {
                            DeleteFile(file, step);
                        }
                        foreach (var file in Directory.GetFiles(profile, "*.sqlite-shm"))
                        {
                            DeleteFile(file, step);
                        }
                    }
                    catch { }
                }
            }

            var firefoxLocalPath = Path.Combine(localAppData, "Mozilla", "Firefox", "Profiles");
            if (Directory.Exists(firefoxLocalPath))
            {
                foreach (var profile in Directory.GetDirectories(firefoxLocalPath))
                {
                    DeleteDirectoryIfExists(Path.Combine(profile, "cache2"), step);
                }
            }
        }

        private void CleanChrome(CleaningStep step)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var chromePath = Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default");

            DeleteDirectoryIfExists(Path.Combine(chromePath, "Cache"), step);
            DeleteDirectoryIfExists(Path.Combine(chromePath, "Code Cache"), step);
            DeleteDirectoryIfExists(Path.Combine(chromePath, "GPUCache"), step);
            DeleteDirectoryIfExists(Path.Combine(chromePath, "Service Worker", "CacheStorage"), step);
        }

        private void CleanEdge(CleaningStep step)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var edgePath = Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default");

            DeleteDirectoryIfExists(Path.Combine(edgePath, "Cache"), step);
            DeleteDirectoryIfExists(Path.Combine(edgePath, "Code Cache"), step);
            DeleteDirectoryIfExists(Path.Combine(edgePath, "GPUCache"), step);
            DeleteDirectoryIfExists(Path.Combine(edgePath, "Service Worker", "CacheStorage"), step);
        }

        private void CleanBraveOpera(CleaningStep step)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            
            // Brave
            var bravePath = Path.Combine(localAppData, "BraveSoftware", "Brave-Browser", "User Data", "Default");
            DeleteDirectoryIfExists(Path.Combine(bravePath, "Cache"), step);
            DeleteDirectoryIfExists(Path.Combine(bravePath, "Code Cache"), step);
            
            // Opera
            var operaPath = Path.Combine(appData, "Opera Software", "Opera Stable");
            DeleteDirectoryIfExists(Path.Combine(operaPath, "Cache"), step);
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

        private void DeleteFile(string filePath, CleaningStep step)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                step.SpaceFreed += fileInfo.Length;
                File.Delete(filePath);
                step.FilesDeleted++;
            }
            catch { }
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
