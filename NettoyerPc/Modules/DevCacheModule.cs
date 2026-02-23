using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NettoyerPc.Core;

namespace NettoyerPc.Modules
{
    public class DevCacheModule : ICleaningModule
    {
        public string Name => "Caches de développement";

        public List<CleaningStep> GetSteps(CleaningMode mode)
        {
            var steps = new List<CleaningStep>
            {
                new() { Name = "Scan .svn (tous disques)" },
                new() { Name = "Nettoyage logs Git (tous disques)" },
                new() { Name = "Nettoyage Visual Studio (tous disques)" },
                new() { Name = "Suppression node_modules (tous disques)" },
                new() { Name = "Caches NuGet/Gradle/Maven/npm/pip" },
                new() { Name = "VS Code cache (tous disques)" }
            };

            if (mode == CleaningMode.DeepClean)
            {
                steps.Add(new() { Name = "Docker system prune" });
            }

            return steps;
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (step.Name.Contains(".svn"))
                {
                    ScanAndDeleteFolders(".svn", step);
                }
                else if (step.Name.Contains("Git"))
                {
                    CleanGitLogs(step);
                }
                else if (step.Name.Contains("Visual Studio"))
                {
                    CleanVisualStudio(step);
                }
                else if (step.Name.Contains("node_modules"))
                {
                    ScanAndDeleteFolders("node_modules", step);
                }
                else if (step.Name.Contains("NuGet"))
                {
                    CleanDevCaches(step);
                }
                else if (step.Name.Contains("VS Code"))
                {
                    CleanVSCode(step);
                }
                else if (step.Name.Contains("Docker"))
                {
                    CleanDocker(step);
                }
            }, cancellationToken);
        }

        private void ScanAndDeleteFolders(string folderName, CleaningStep step)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    ScanDirectory(drive.RootDirectory.FullName, folderName, step);
                }
            }
        }

        // Chemins à ne JAMAIS toucher (applications, système)
        private static readonly string[] _protectedPaths =
        {
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles),
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86),
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows),
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.System),
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.SystemX86),
        };

        private bool IsProtectedPath(string path)
        {
            foreach (var p in _protectedPaths)
            {
                if (!string.IsNullOrEmpty(p) && path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            // Ne pas toucher les chemins node.js globaux
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (path.StartsWith(Path.Combine(appData, "npm"), StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private void ScanDirectory(string path, string targetFolder, CleaningStep step, int depth = 0)
        {
            if (depth > 10) return; // Limiter la profondeur
            if (IsProtectedPath(path)) return; // Protéger les chemins système

            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    if (IsProtectedPath(dir)) continue;

                    if (Path.GetFileName(dir) == targetFolder)
                    {
                        try
                        {
                            var dirInfo = new DirectoryInfo(dir);
                            step.SpaceFreed += GetDirectorySize(dirInfo);
                            Directory.Delete(dir, true);
                            step.FilesDeleted++;
                        }
                        catch { }
                    }
                    else
                    {
                        ScanDirectory(dir, targetFolder, step, depth + 1);
                    }
                }
            }
            catch { }
        }

        private void CleanGitLogs(CleaningStep step)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    FindAndCleanGitFolders(drive.RootDirectory.FullName, step);
                }
            }
        }

        private void FindAndCleanGitFolders(string path, CleaningStep step, int depth = 0)
        {
            if (depth > 10) return;

            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    if (Path.GetFileName(dir) == ".git")
                    {
                        var logsPath = Path.Combine(dir, "logs");
                        if (Directory.Exists(logsPath))
                        {
                            try
                            {
                                foreach (var file in Directory.GetFiles(logsPath, "*", SearchOption.AllDirectories))
                                {
                                    try
                                    {
                                        var fileInfo = new FileInfo(file);
                                        step.SpaceFreed += fileInfo.Length;
                                        File.Delete(file);
                                        step.FilesDeleted++;
                                    }
                                    catch { }
                                }
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        FindAndCleanGitFolders(dir, step, depth + 1);
                    }
                }
            }
            catch { }
        }

        private void CleanVisualStudio(CleaningStep step)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    CleanVSFolders(drive.RootDirectory.FullName, step);
                }
            }
        }

        private void CleanVSFolders(string path, CleaningStep step, int depth = 0)
        {
            if (depth > 10) return;

            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var dirName = Path.GetFileName(dir);
                    
                    if (dirName == ".vs")
                    {
                        try
                        {
                            var dirInfo = new DirectoryInfo(dir);
                            step.SpaceFreed += GetDirectorySize(dirInfo);
                            Directory.Delete(dir, true);
                            step.FilesDeleted++;
                        }
                        catch { }
                    }
                    else if (dirName == "Debug" || dirName == "Release" || dirName == "x64" || dirName == "ipch")
                    {
                        // Vérifier si c'est un dossier de build VS
                        var parentDir = Directory.GetParent(dir)?.FullName;
                        if (parentDir != null && 
                            (Directory.GetFiles(parentDir, "*.sln").Any() || 
                             Directory.GetFiles(parentDir, "*.vcxproj").Any()))
                        {
                            try
                            {
                                foreach (var file in Directory.GetFiles(dir, "*.*"))
                                {
                                    var ext = Path.GetExtension(file).ToLower();
                                    if (ext == ".pch" || ext == ".pdb" || ext == ".obj" || ext == ".idb")
                                    {
                                        try
                                        {
                                            var fileInfo = new FileInfo(file);
                                            step.SpaceFreed += fileInfo.Length;
                                            File.Delete(file);
                                            step.FilesDeleted++;
                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        CleanVSFolders(dir, step, depth + 1);
                    }
                }
            }
            catch { }
        }

        private void CleanDevCaches(CleaningStep step)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // NuGet
            DeleteDirectoryIfExists(Path.Combine(userProfile, ".nuget", "packages"), step);
            
            // Gradle
            DeleteDirectoryIfExists(Path.Combine(userProfile, ".gradle", "caches"), step);
            
            // Maven
            DeleteDirectoryIfExists(Path.Combine(userProfile, ".m2", "repository"), step);
            
            // npm
            DeleteDirectoryIfExists(Path.Combine(appData, "npm-cache"), step);
            
            // pip
            DeleteDirectoryIfExists(Path.Combine(localAppData, "pip", "Cache"), step);
            
            // Composer
            DeleteDirectoryIfExists(Path.Combine(appData, "Composer", "cache"), step);
            
            // Yarn
            DeleteDirectoryIfExists(Path.Combine(localAppData, "Yarn", "Cache"), step);
        }

        private void CleanVSCode(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var codePath = Path.Combine(appData, "Code");

            // Ne supprimer QUE les caches, pas les dossiers de configuration .vscode
            DeleteDirectoryIfExists(Path.Combine(codePath, "Cache"), step);
            DeleteDirectoryIfExists(Path.Combine(codePath, "CachedData"), step);
            DeleteDirectoryIfExists(Path.Combine(codePath, "logs"), step);
            DeleteDirectoryIfExists(Path.Combine(codePath, "GPUCache"), step);
            DeleteDirectoryIfExists(Path.Combine(codePath, "Code Cache"), step);
            // NE PAS supprimer les dossiers .vscode (contiennent settings/extensions workspace)
        }

        private void CleanDocker(CleaningStep step)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "system prune -af",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process?.WaitForExit(60000); // 1 minute timeout
            }
            catch { }
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
