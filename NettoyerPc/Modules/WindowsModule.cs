using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using NettoyerPc.Core;

namespace NettoyerPc.Modules
{
    public class WindowsModule : ICleaningModule
    {
        public string Name => "Windows";

        public List<CleaningStep> GetSteps(CleaningMode mode)
        {
            var steps = new List<CleaningStep>
            {
                new() { Name = "Corbeilles (tous disques)",     Category = "windows" },
                new() { Name = "Journaux Windows",              Category = "windows" },
                new() { Name = "Nettoyage disque (cleanmgr)",   Category = "windows" },
                new() { Name = "Cache Microsoft Store",          Category = "windows" },
                new() { Name = "Delivery Optimization",         Category = "windows" },
                new() { Name = "Logs CBS & installation Windows", Category = "windows" },
            };

            if (mode == CleaningMode.DeepClean || mode == CleaningMode.Advanced)
            {
                steps.Add(new() { Name = "Optimisations registre",                    Category = "windows" });
                steps.Add(new() { Name = "Vérification disque C:",                    Category = "windows" });
                steps.Add(new() { Name = "Défragmentation (tous disques)",            Category = "windows" });
                steps.Add(new() { Name = "Windows Update cache",                      Category = "windows" });
                steps.Add(new() { Name = "DISM cleanup",                              Category = "windows" });
                steps.Add(new() { Name = "Lancer la mise à jour Windows",             Category = "windows" });
                steps.Add(new() { Name = "Windows.old (restes mise à niveau)",        Category = "windows" });
                steps.Add(new() { Name = "Réinitialiser l'index de recherche",        Category = "windows" });
            }

            return steps;
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (step.Name.Contains("Corbeilles"))
                {
                    EmptyRecycleBins(step);
                }
                else if (step.Name.Contains("Journaux"))
                {
                    ClearEventLogs(step);
                }
                else if (step.Name.Contains("cleanmgr"))
                {
                    RunDiskCleanup(step);
                }
                else if (step.Name.Contains("registre"))
                {
                    OptimizeRegistry(step);
                }
                else if (step.Name.Contains("Vérification disque"))
                {
                    // chkntfs /c planifie chkdsk au prochain démarrage (plus fiable que /scan)
                    RunCommand("chkntfs", "/c C:", step);
                }
                else if (step.Name.Contains("Défragmentation"))
                {
                    DefragmentDrives(step);
                }
                else if (step.Name.Contains("Windows Update"))
                {
                    CleanWindowsUpdate(step);
                }
                else if (step.Name.Contains("DISM"))
                {
                    // Exécution dans une fenêtre console visible pour que DISM
                    // s'applique réellement (ne fonctionne pas en mode CreateNoWindow).
                    RunPrivilegedCommand("Dism.exe", "/Online /Cleanup-Image /StartComponentCleanup", step, 600);
                }
                else if (step.Name.Contains("Microsoft Store"))
                    CleanMicrosoftStore(step);
                else if (step.Name.Contains("Delivery Optimization"))
                    CleanDeliveryOptimization(step);
                else if (step.Name.Contains("CBS"))
                    CleanCBSLogs(step);
                else if (step.Name.Contains("mise à jour Windows"))
                    LaunchWindowsUpdate(step);
                else if (step.Name.Contains("Windows.old"))
                    CleanWindowsOld(step);
                else if (step.Name.Contains("index de recherche"))
                    ResetSearchIndex(step);
            }, cancellationToken);
        }

        private void EmptyRecycleBins(CleaningStep step)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    var recycleBin = Path.Combine(drive.RootDirectory.FullName, "$Recycle.Bin");
                    if (Directory.Exists(recycleBin))
                    {
                        try
                        {
                            var dirInfo = new DirectoryInfo(recycleBin);
                            long sz = 0;
                            int fc = 0;
                            foreach (var f in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                                try { sz += f.Length; fc++; } catch { }
                            Directory.Delete(recycleBin, true);
                            step.SpaceFreed += sz;
                            step.FilesDeleted += Math.Max(1, fc);
                            step.AddLog($"Corbeille {drive.Name} vidée : {fc} fichier(s)");
                        }
                        catch { }
                    }
                }
            }
        }

        private void ClearEventLogs(CleaningStep step)
        {
            try
            {
                step.AddLog("Récupération de la liste des journaux d'événements...");
                var psi = new ProcessStartInfo
                {
                    FileName = "wevtutil.exe",
                    Arguments = "el",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    int cleared = 0;
                    while (!process.StandardOutput.EndOfStream)
                    {
                        var logName = process.StandardOutput.ReadLine();
                        if (!string.IsNullOrEmpty(logName))
                        {
                            try
                            {
                                var clearPsi = new ProcessStartInfo
                                {
                                    FileName = "wevtutil.exe",
                                    Arguments = $"cl \"{logName}\"",
                                    RedirectStandardOutput = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };
                                using var clearProcess = Process.Start(clearPsi);
                                clearProcess?.WaitForExit(5000);
                                step.FilesDeleted++;
                                cleared++;
                            }
                            catch { }
                        }
                    }
                    process.WaitForExit();
                    step.AddLog($"{cleared} journaux d'événements effacés");
                }
            }
            catch { }
        }

        private void RunDiskCleanup(CleaningStep step)
        {
            try
            {
                // Configuration des flags de nettoyage
                string[] cleanupKeys = 
                {
                    "Active Setup Temp Folders", "BranchCache", "Content Indexer Cleaner",
                    "D3D Shader Cache", "Delivery Optimization Files", "Device Driver Packages",
                    "Downloaded Program Files", "Internet Cache Files", "Memory Dump Files",
                    "Recycle Bin", "Temporary Files", "Temporary Setup Files", 
                    "Thumbnail Cache", "Update Cleanup", "Windows Defender",
                    "Windows Error Reporting Files", "Windows ESD installation files",
                    "Windows Upgrade Log Files"
                };

                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches", true);
                
                if (key != null)
                {
                    foreach (var subKeyName in cleanupKeys)
                    {
                        try
                        {
                            using var subKey = key.OpenSubKey(subKeyName, true);
                            subKey?.SetValue("StateFlags0001", 2, RegistryValueKind.DWord);
                        }
                        catch { }
                    }
                }

                // Lancement du nettoyage — UseShellExecute=true : évite le gel
                // de l'UI causé par la boîte de dialogue cachée de cleanmgr.
                // Timeout 5 min : cleanmgr très lent sur disques mécaniques.
                var psi = new ProcessStartInfo
                {
                    FileName = "cleanmgr.exe",
                    Arguments = "/sagerun:1",
                    UseShellExecute = true,
                };

                step.AddLog("Lancement cleanmgr /sagerun:1 (timeout 5 min)...");
                using var process = Process.Start(psi);
                bool done = (process?.WaitForExit(300_000)) ?? true;
                step.AddLog(done ? "cleanmgr terminé" : "cleanmgr timeout 5min — passage à la suite");
                step.FilesDeleted++;
            }
            catch { }
        }

        private void OptimizeRegistry(CleaningStep step)
        {
            try
            {
                // ── Performances visuelles ────────────────────────────────────────
                using var key1 = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\WindowMetrics", true);
                key1?.SetValue("MinAnimate", "0", RegistryValueKind.String);

                // Délai de menu réduit
                using var key2 = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
                key2?.SetValue("MenuShowDelay", "0", RegistryValueKind.String);
                // Désactiver les effets de glisser-déposer lents
                key2?.SetValue("DragFullWindows", "0", RegistryValueKind.String);

                // Activer le thème de performance (réduire les animations)
                using var keyPerf = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true);
                keyPerf?.SetValue("VisualFXSetting", 2, RegistryValueKind.DWord); // 2 = Adjust for best performance

                // ── Démarrage & services ──────────────────────────────────────────
                // Réduire le délai de démarrage des services
                using var keyBoot = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control", true);
                keyBoot?.SetValue("WaitToKillServiceTimeout", "2000", RegistryValueKind.String);

                // ── Réseau & TCP ──────────────────────────────────────────────────
                using var keyTcp = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true);
                // Désactiver la résolution NetBIOS broadcasts (économie réseau)
                keyTcp?.SetValue("EnableLmhosts", 0U, RegistryValueKind.DWord);

                // Activer les congestion algorithms ECN
                using var keyEcn = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true);
                keyEcn?.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);

                // ── Explorateur Windows ───────────────────────────────────────────
                using var keyEx = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true);
                // Afficher les extensions de fichiers
                keyEx?.SetValue("HideFileExt", 0, RegistryValueKind.DWord);
                // Désactiver les vignettes d'aperçu dans la barre des tâches
                keyEx?.SetValue("ExtendedUIHoverTime", 3000, RegistryValueKind.DWord);

                // ── Télémétrie & vie privée ───────────────────────────────────────
                var services = new[] { "DiagTrack", "dmwappushservice" };
                foreach (var serviceName in services)
                {
                    try
                    {
                        using var service = new System.ServiceProcess.ServiceController(serviceName);
                        if (service.Status != System.ServiceProcess.ServiceControllerStatus.Stopped)
                            service.Stop();
                        RunCommand("sc", $"config {serviceName} start= disabled", step);
                    }
                    catch { }
                }

                // Désactiver la collecte de data publicitaire (advertising ID)
                using var keyAdv = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", true);
                keyAdv?.SetValue("Enabled", 0, RegistryValueKind.DWord);

                // ── Mise en veille et énergie ─────────────────────────────────────
                // Désactiver Fast Startup (peut causer des problèmes avec dual-boot / wake)
                // Note : commenté par défaut car peut impacter le temps de boot sur SSD
                // using var keyFS = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", true);
                // keyFS?.SetValue("HiberbootEnabled", 0, RegistryValueKind.DWord);

                step.FilesDeleted++;
            }
            catch { }
        }

        private void DefragmentDrives(CleaningStep step)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "defrag",
                            Arguments = $"{drive.Name} /O",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using var process = Process.Start(psi);
                        process?.WaitForExit(300000); // 5 minutes timeout par disque
                        step.FilesDeleted++;
                    }
                    catch { }
                }
            }
        }

        private void CleanWindowsUpdate(CleaningStep step)
        {
            try
            {
                // Arrêter les services
                StopService("wuauserv");
                StopService("bits");
                Thread.Sleep(2000);

                // Nettoyer le cache
                var downloadPath = @"C:\Windows\SoftwareDistribution\Download";
                if (Directory.Exists(downloadPath))
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(downloadPath);
                        step.SpaceFreed += GetDirectorySize(dirInfo);
                        
                        foreach (var file in Directory.GetFiles(downloadPath))
                        {
                            try { File.Delete(file); step.FilesDeleted++; }
                            catch { }
                        }
                        foreach (var dir in Directory.GetDirectories(downloadPath))
                        {
                            try { Directory.Delete(dir, true); step.FilesDeleted++; }
                            catch { }
                        }
                    }
                    catch { }
                }

                // Redémarrer les services
                StartService("wuauserv");
                StartService("bits");
            }
            catch { }
        }

        private void StopService(string serviceName)
        {
            try
            {
                using var service = new ServiceController(serviceName);
                if (service.Status != ServiceControllerStatus.Stopped)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
            }
            catch { }
        }

        private void StartService(string serviceName)
        {
            try
            {
                using var service = new ServiceController(serviceName);
                if (service.Status != ServiceControllerStatus.Running)
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }
            }
            catch { }
        }

        private void RunCommand(string command, string arguments, CleaningStep step)
        {
            step.AddLog($"> {command} {arguments}");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = command,
                    Arguments              = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                using var process = Process.Start(psi);
                process?.WaitForExit(60000);
            }
            catch (Exception ex) { step.AddLog($"Erreur: {ex.Message}"); }
        }

        /// <summary>Exécute une commande système lourde (DISM, SFC, chkdsk) dans
        /// une fenêtre CMD visible afin qu'elle s'applique réellement.</summary>
        private void RunPrivilegedCommand(string exe, string args, CleaningStep step, int timeoutSec = 600)
        {
            step.AddLog($"CMD (visible)> {exe} {args}");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "cmd.exe",
                    Arguments              = $"/c {exe} {args}",
                    UseShellExecute        = false,
                    CreateNoWindow         = false, // fenêtre CMD visible
                    RedirectStandardOutput = false,
                    RedirectStandardError  = false,
                };
                using var proc = Process.Start(psi);
                if (proc == null) { step.AddLog($"Impossible de démarrer : {exe}"); return; }
                bool finished = proc.WaitForExit(timeoutSec * 1000);
                step.AddLog(finished
                    ? $"{exe} terminé (code {proc.ExitCode})"
                    : $"{exe} timeout après {timeoutSec}s — continua en arrière-plan");
            }
            catch (Exception ex) { step.AddLog($"Erreur {exe}: {ex.Message}"); }
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

        private void CleanMicrosoftStore(CleaningStep step)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var storeCache = Path.Combine(local, "Packages",
                "Microsoft.WindowsStore_8wekyb3d8bbwe", "LocalCache");
            if (Directory.Exists(storeCache))
            {
                try
                {
                    var di = new DirectoryInfo(storeCache);
                    step.SpaceFreed += GetDirectorySize(di);
                    Directory.Delete(storeCache, true);
                    step.FilesDeleted++;
                }
                catch { }
            }
        }

        private void CleanDeliveryOptimization(CleaningStep step)
        {
            var doPath = @"C:\Windows\SoftwareDistribution\DeliveryOptimization";
            if (Directory.Exists(doPath))
            {
                try
                {
                    StopService("DoSvc");
                    var di = new DirectoryInfo(doPath);
                    step.SpaceFreed += GetDirectorySize(di);
                    Directory.Delete(doPath, true);
                    step.FilesDeleted++;
                    StartService("DoSvc");
                }
                catch { }
            }
            // Cache local DO
            var localDO = @"C:\Windows\ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization";
            if (Directory.Exists(localDO))
            {
                try
                {
                    var di = new DirectoryInfo(localDO);
                    step.SpaceFreed += GetDirectorySize(di);
                    Directory.Delete(localDO, true);
                    step.FilesDeleted++;
                }
                catch { }
            }
        }

        // ── Nouvelles méthodes ────────────────────────────────────────────────────

        /// <summary>Supprime les logs CBS, DISM et setup Windows — se recréent automatiquement.</summary>
        private void CleanCBSLogs(CleaningStep step)
        {
            var logDirs = new[]
            {
                @"C:\Windows\Logs\CBS",
                @"C:\Windows\Logs\DISM",
                @"C:\Windows\Logs\MoSetup",
                @"C:\Windows\Logs\WindowsUpdate",
            };
            foreach (var dir in logDirs)
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var f in Directory.GetFiles(dir))
                {
                    try
                    {
                        var fi = new FileInfo(f);
                        step.SpaceFreed += fi.Length;
                        File.Delete(f);
                        step.FilesDeleted++;
                    }
                    catch { }
                }
            }
            // Logs setup racine Windows
            foreach (var logFile in new[]
            {
                @"C:\Windows\setupact.log", @"C:\Windows\setuperr.log",
                @"C:\Windows\Panther\setupact.log", @"C:\Windows\Panther\setuperr.log",
            })
            {
                try
                {
                    if (!File.Exists(logFile)) continue;
                    step.SpaceFreed += new FileInfo(logFile).Length;
                    File.Delete(logFile);
                    step.FilesDeleted++;
                }
                catch { }
            }
        }

        /// <summary>Déclenche la recherche et l'installation des mises à jour Windows
        /// via UsoClient (silencieux) et ouvre la page Paramètres WU pour suivre la progression.</summary>
        private void LaunchWindowsUpdate(CleaningStep step)
        {
            try
            {
                // Déclenche le scan silencieux Windows Update
                var psi = new ProcessStartInfo
                {
                    FileName        = "UsoClient.exe",
                    Arguments       = "StartScan",
                    UseShellExecute = false,
                    CreateNoWindow  = true,
                };
                using var scan = Process.Start(psi);
                scan?.WaitForExit(15000);
                step.FilesDeleted++;
            }
            catch { }
            try
            {
                // Déclenche aussi le téléchargement + installation si disponible
                var psi2 = new ProcessStartInfo
                {
                    FileName        = "UsoClient.exe",
                    Arguments       = "StartDownload",
                    UseShellExecute = false,
                    CreateNoWindow  = true,
                };
                using var dl = Process.Start(psi2);
                dl?.WaitForExit(5000);
            }
            catch { }
            try
            {
                // Ouvre la page Windows Update dans les Paramètres pour le feedback visuel
                Process.Start(new ProcessStartInfo
                {
                    FileName        = "ms-settings:windowsupdate",
                    UseShellExecute = true,
                });
            }
            catch { }
        }

        /// <summary>Supprime C:\Windows.old si présent — peut libérer plusieurs Go
        /// après une mise à niveau Windows. Requiert les droits admin.</summary>
        private void CleanWindowsOld(CleaningStep step)
        {
            const string winOld = @"C:\Windows.old";
            if (!Directory.Exists(winOld)) return;
            try
            {
                var di = new DirectoryInfo(winOld);
                step.SpaceFreed += GetDirectorySize(di);
                // Prendre possession puis supprimer
                RunCommand("takeown", $"/f \"{winOld}\" /r /d y", step);
                RunCommand("icacls", $"\"{winOld}\" /grant administrators:F /t /q", step);
                Directory.Delete(winOld, true);
                step.FilesDeleted++;
            }
            catch { }
        }

        /// <summary>Arrête le service WSearch, supprime la base de l'index de recherche,
        /// puis redémarre le service — l'index est reconstruit automatiquement en arrière-plan.</summary>
        private void ResetSearchIndex(CleaningStep step)
        {
            try
            {
                StopService("WSearch");
                System.Threading.Thread.Sleep(2000);

                const string indexPath = @"C:\ProgramData\Microsoft\Search\Data\Applications\Windows";
                if (Directory.Exists(indexPath))
                {
                    foreach (var file in Directory.GetFiles(indexPath))
                    {
                        try
                        {
                            step.SpaceFreed += new FileInfo(file).Length;
                            File.Delete(file);
                            step.FilesDeleted++;
                        }
                        catch { }
                    }
                }
                StartService("WSearch");
            }
            catch { }
        }
    }
}
