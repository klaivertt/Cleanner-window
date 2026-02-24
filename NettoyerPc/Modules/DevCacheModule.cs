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
                new() { Name = "Scan .svn (tous disques)",             Category = "dev" },
                new() { Name = "Nettoyage logs Git (tous disques)",    Category = "dev" },
                new() { Name = "Nettoyage Visual Studio (tous disques)",Category = "dev" },
                new() { Name = "Suppression node_modules (tous disques)",Category = "dev" },
                new() { Name = "Caches NuGet/Gradle/Maven/npm/pip",    Category = "dev" },
                new() { Name = "VS Code cache (tous disques)",         Category = "dev" },
                new() { Name = "JetBrains IDEs (caches, logs)",        Category = "dev" },
                new() { Name = "Eclipse / NetBeans (cache)",           Category = "dev" },
                new() { Name = "Android Studio (caches)",              Category = "dev" },
                new() { Name = "Cursor IDE (cache)",                   Category = "dev" },
            };

            if (mode == CleaningMode.DeepClean || mode == CleaningMode.Advanced)
            {
                steps.Add(new() { Name = "Docker system prune", Category = "dev" });
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
                else if (step.Name.Contains("JetBrains"))
                {
                    CleanJetBrains(step);
                }
                else if (step.Name.Contains("Eclipse"))
                {
                    CleanEclipseNetBeans(step);
                }
                else if (step.Name.Contains("Android Studio"))
                {
                    CleanAndroidStudio(step);
                }
                else if (step.Name.Contains("Cursor"))
                {
                    CleanCursorIDE(step);
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
            if (depth > 10) return;
            if (IsProtectedPath(path)) return;

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
                            int fc = 0;
                            foreach (var f in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                                try { step.SpaceFreed += f.Length; fc++; } catch { }
                            Directory.Delete(dir, true);
                            step.FilesDeleted += Math.Max(1, fc);
                            step.AddLog($"Supprimé {dir} ({fc} fichier(s))");
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
            step.AddLog("Scan de tous les dépôts Git sur les disques fixes...");
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                    FindAndCleanGitFolders(drive.RootDirectory.FullName, step);
            }
        }

        private void FindAndCleanGitFolders(string path, CleaningStep step, int depth = 0)
        {
            if (depth > 10) return;
            if (IsProtectedPath(path)) return;

            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    if (IsProtectedPath(dir)) continue;

                    if (Path.GetFileName(dir) == ".git")
                    {
                        var repoRoot = Directory.GetParent(dir)?.FullName ?? path;

                        // 1. Supprimer les logs de reflog
                        var logsPath = Path.Combine(dir, "logs");
                        if (Directory.Exists(logsPath))
                        {
                            try
                            {
                                foreach (var f in Directory.GetFiles(logsPath, "*", SearchOption.AllDirectories))
                                {
                                    try { step.SpaceFreed += new FileInfo(f).Length; File.Delete(f); step.FilesDeleted++; }
                                    catch { }
                                }
                                step.AddLog($"Logs Git effacés : {repoRoot}");
                            }
                            catch { }
                        }

                        // 2. Supprimer les fichiers de merge/cherry-pick temporaires
                        var tempRefs = new[] { "ORIG_HEAD", "MERGE_HEAD", "CHERRY_PICK_HEAD", "REVERT_HEAD" };
                        foreach (var refFile in tempRefs)
                        {
                            var refPath = Path.Combine(dir, refFile);
                            if (File.Exists(refPath))
                                try { File.Delete(refPath); step.FilesDeleted++; step.AddLog($"Supprimé {refFile} dans {repoRoot}"); } catch { }
                        }

                        // 3. git gc --auto : compresse les objets, supprime les loose objects
                        RunGitCommand(repoRoot, "gc --auto --quiet", step);

                        // 4. git remote prune origin : nettoie les références distantes obsolètes
                        RunGitCommand(repoRoot, "remote prune origin", step);
                    }
                    else if (Path.GetFileName(dir) != ".git")
                    {
                        FindAndCleanGitFolders(dir, step, depth + 1);
                    }
                }
            }
            catch { }
        }

        /// <summary>Exécute une commande git dans un dépôt donné, loggue la sortie.</summary>
        private void RunGitCommand(string repoRoot, string gitArgs, CleaningStep step)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "git",
                    Arguments              = gitArgs,
                    WorkingDirectory       = repoRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                };
                using var proc = Process.Start(psi);
                if (proc == null) return;
                var output = proc.StandardOutput.ReadToEnd();
                var error  = proc.StandardError.ReadToEnd();
                proc.WaitForExit(30_000);
                if (!string.IsNullOrWhiteSpace(output))
                    step.AddLog($"git {gitArgs}: {output.Trim()}");
                if (!string.IsNullOrWhiteSpace(error) && proc.ExitCode != 0)
                    step.AddLog($"git {gitArgs} stderr: {error.Trim()}");
            }
            catch { /* git non installé ou dépôt invalide — ignoré */ }
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

        // ── IDEs ─────────────────────────────────────────────────────────────────

        /// <summary>Cache disque + logs de toutes les IDEs JetBrains (IntelliJ, PyCharm, WebStorm,
        /// Rider, CLion, DataGrip, GoLand, PhpStorm, AppCode, Toolbox).
        /// Ne touche jamais les paramètres, plugins, workspaces ni les projets.</summary>
        private void CleanJetBrains(CleaningStep step)
        {
            var appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // JetBrains Toolbox path + toutes IDEs installées via Toolbox ou standalone
            var jbRoots = new[]
            {
                Path.Combine(appData,  "JetBrains"),
                Path.Combine(localApp, "JetBrains"),
                Path.Combine(userHome, ".cache", "JetBrains"),
            };

            // Noms de sous-dossiers sûrs à supprimer dans chaque versioned product dir
            var safeFolders = new[] { "caches", "log", "logs", "tmp", "index", "compile-server" };

            foreach (var root in jbRoots)
            {
                if (!Directory.Exists(root)) continue;
                try
                {
                    foreach (var productDir in Directory.GetDirectories(root))
                    {
                        foreach (var sub in Directory.GetDirectories(productDir))
                        {
                            var name = Path.GetFileName(sub).ToLowerInvariant();
                            if (Array.Exists(safeFolders, s => name == s || name.StartsWith(s)))
                                DeleteDirectoryIfExists(sub, step);
                        }
                        // Logs directs dans le product dir
                        foreach (var logFile in Directory.GetFiles(productDir, "*.log", SearchOption.TopDirectoryOnly))
                        {
                            try { step.SpaceFreed += new FileInfo(logFile).Length; File.Delete(logFile); step.FilesDeleted++; } catch { }
                        }
                    }
                }
                catch { }
            }

            // Toolbox itself
            var toolboxCache = Path.Combine(appData, "JetBrains", "Toolbox", "cache");
            DeleteDirectoryIfExists(toolboxCache, step);
            DeleteDirectoryIfExists(Path.Combine(appData, "JetBrains", "Toolbox", "logs"), step);
        }

        /// <summary>Cache Eclipse (.metadata temps/index) et NetBeans (cache, lock files).
        /// Ne touche jamais les workspaces, projets ni les plugins installés.</summary>
        private void CleanEclipseNetBeans(CleaningStep step)
        {
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Eclipse .metadata temp dans les workspaces (recherche limitée)
            var eclipsePaths = new[]
            {
                Path.Combine(userHome, ".eclipse"),
                Path.Combine(localApp, "eclipse"),
                Path.Combine(userHome, "eclipse"),
            };
            foreach (var ep in eclipsePaths)
            {
                if (!Directory.Exists(ep)) continue;
                // Ne supprimer QUE les dossiers .tmp et logs
                foreach (var dir in SafeDirectories(ep))
                {
                    try
                    {
                        var n = Path.GetFileName(dir).ToLowerInvariant();
                        if (n == ".tmp" || n == "tmp" || n == "log" || n == "logs")
                            DeleteDirectoryIfExists(dir, step);
                    }
                    catch { }
                }
            }

            // NetBeans
            var nbPaths = new[]
            {
                Path.Combine(appData, "NetBeans"),
                Path.Combine(localApp, "NetBeans"),
            };
            foreach (var np in nbPaths)
            {
                if (!Directory.Exists(np)) continue;
                foreach (var ver in SafeDirectories(np))
                {
                    DeleteDirectoryIfExists(Path.Combine(ver, "cache"), step);
                    DeleteDirectoryIfExists(Path.Combine(ver, "var",  "cache"), step);
                    DeleteDirectoryIfExists(Path.Combine(ver, "tmp"),   step);
                }
            }
        }

        /// <summary>Caches Android Studio (build cache, AVD snapshot cache, Gradle daemon logs).
        /// Ne touche jamais les AVD (.avd folders), les keystores, ni les fichiers source.</summary>
        private void CleanAndroidStudio(CleaningStep step)
        {
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // Caches Android Studio IDE (même archi JetBrains)
            foreach (var root in new[] { appData, localApp })
            {
                var jbRoot = Path.Combine(root, "Google", "AndroidStudio");
                if (!Directory.Exists(jbRoot)) continue;
                foreach (var ver in SafeDirectories(jbRoot))
                    foreach (var sub in new[] { "caches", "log", "logs", "tmp" })
                        DeleteDirectoryIfExists(Path.Combine(ver, sub), step);
            }

            // Gradle daemon logs (pas le cache de build — serait trop long à régénérer)
            var gradleDaemon = Path.Combine(userHome, ".gradle", "daemon");
            if (Directory.Exists(gradleDaemon))
                foreach (var ver in SafeDirectories(gradleDaemon))
                    foreach (var logFile in SafeFiles(ver, "*.log"))
                    {
                        try { step.SpaceFreed += new FileInfo(logFile).Length; File.Delete(logFile); step.FilesDeleted++; } catch { }
                    }

            // Caches AVD snapshot (regenerables) — PAS les .avd eux-mêmes
            var avdPath = Path.Combine(userHome, ".android", "avd");
            if (Directory.Exists(avdPath))
                foreach (var avdDir in SafeDirectories(avdPath))
                    DeleteDirectoryIfExists(Path.Combine(avdDir, "snapshots"), step);
        }

        /// <summary>Cache Cursor IDE (fork VS Code) — même structure que VS Code.
        /// Ne touche jamais les paramètres, extensions ni keybindings.</summary>
        private void CleanCursorIDE(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var cursorPath = Path.Combine(appData, "Cursor");
            if (!Directory.Exists(cursorPath)) return;
            foreach (var sub in new[] { "Cache", "CachedData", "logs", "GPUCache", "Code Cache", "CachedExtensions" })
                DeleteDirectoryIfExists(Path.Combine(cursorPath, sub), step);
        }

        private static string[] SafeDirectories(string path)
        {
            try { return Directory.Exists(path) ? Directory.GetDirectories(path) : Array.Empty<string>(); }
            catch { return Array.Empty<string>(); }
        }

        private static string[] SafeFiles(string path, string pattern)
        {
            try { return Directory.Exists(path) ? Directory.GetFiles(path, pattern) : Array.Empty<string>(); }
            catch { return Array.Empty<string>(); }
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
