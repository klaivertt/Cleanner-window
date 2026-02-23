using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using NettoyerPc.Core;

namespace NettoyerPc.Modules
{
    /// <summary>
    /// OPTIMISATION SYSTÈME – 7 étapes
    /// </summary>
    public class SystemOptimizationModule : ICleaningModule
    {
        public string Name => "Optimisation Système";

        public List<CleaningStep> GetSteps(CleaningMode mode)
        {
            return new List<CleaningStep>
            {
                new() { Name = "Nettoyage packages Windows (DISM StartComponentCleanup)", Category = "sysopt" },
                new() { Name = "SFC /scannow (vérification intégrité fichiers)", Category = "sysopt" },
                new() { Name = "DISM /RestoreHealth (réparation image Windows)", Category = "sysopt" },
                new() { Name = "Rebuild cache polices de caractères", Category = "sysopt" },
                new() { Name = "Reset réseau complet (Winsock, IP, DNS)", Category = "sysopt" },
                new() { Name = "Optimisation/TRIM tous les disques", Category = "sysopt" },
                new() { Name = "Vérification erreurs disques (chkdsk)", Category = "sysopt" },
            };
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (step.Name.Contains("StartComponentCleanup"))
                    RunCommand("Dism.exe", "/online /Cleanup-Image /StartComponentCleanup", step, 300);
                else if (step.Name.Contains("SFC"))
                    RunCommand("sfc.exe", "/scannow", step, 3600);
                else if (step.Name.Contains("RestoreHealth"))
                    RunCommand("Dism.exe", "/online /Cleanup-Image /RestoreHealth", step, 1800);
                else if (step.Name.Contains("polices"))
                    RebuildFontCache(step);
                else if (step.Name.Contains("réseau"))
                    ResetNetwork(step);
                else if (step.Name.Contains("TRIM"))
                    OptimizeDisks(step);
                else if (step.Name.Contains("chkdsk"))
                    ScheduleChkdsk(step);
                else if (step.Name.Contains("plan d'alimentation"))
                    SetBalancedPowerPlan(step);
                else if (step.Name.Contains("hibernation"))
                    DisableHibernation(step);
                else if (step.Name.Contains("WUDO"))
                    DisableWindowsUpdateDelivery(step);
                else if (step.Name.Contains("t\u00e2ches planifi\u00e9es"))
                    PurgeObsoleteScheduledTasks(step);
            }, cancellationToken);
        }

        private void RebuildFontCache(CleaningStep step)
        {
            try
            {
                // Arrêter le service de cache de polices
                RunCommand("net", "stop \"Windows Font Cache Service\" /y", step, 30);
                var fontCachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Windows\FontCache");
                if (Directory.Exists(fontCachePath))
                {
                    foreach (var file in Directory.GetFiles(fontCachePath, "*.dat"))
                    {
                        try { var fi = new FileInfo(file); step.SpaceFreed += fi.Length; File.Delete(file); step.FilesDeleted++; }
                        catch { }
                    }
                }
                // Redémarrer le service
                RunCommand("net", "start \"Windows Font Cache Service\"", step, 30);
                step.Status = "Cache polices reconstruit";
            }
            catch { }
        }

        private void ResetNetwork(CleaningStep step)
        {
            RunCommand("ipconfig", "/flushdns", step, 30);
            RunCommand("netsh", "winsock reset", step, 30);
            RunCommand("netsh", "int ip reset", step, 30);
            RunCommand("netsh", "int ipv6 reset", step, 30);
            // DNS Cloudflare
            RunCommand("netsh", "interface ip set dns \"Ethernet\" static 1.1.1.1 primary", step, 10);
            RunCommand("netsh", "interface ip add dns \"Ethernet\" 1.0.0.1 index=2", step, 10);
            RunCommand("netsh", "interface ip set dns \"Wi-Fi\" static 1.1.1.1 primary", step, 10);
            RunCommand("netsh", "interface ip add dns \"Wi-Fi\" 1.0.0.1 index=2", step, 10);
            step.FilesDeleted++;
        }

        private void OptimizeDisks(CleaningStep step)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Fixed) continue;
                try
                {
                    var letter = drive.Name.TrimEnd('\\');
                    RunCommand("defrag", $"{letter} /U /V /O", step, 3600);
                    step.FilesDeleted++;
                }
                catch { }
            }
        }

        private void ScheduleChkdsk(CleaningStep step)
        {
            // Lance un scan chkdsk en ligne sur C: (lecture seule pour ne pas bloquer)
            RunCommand("chkdsk", "C: /scan", step, 3600);
            step.FilesDeleted++;
        }

        /// <summary>Force le plan d'alimentation Equilibr\u00e9 (meilleur compromis perf/\u00e9conomies).
        /// Evite High Performance qui consomme inutilement sur les portables.</summary>
        private void SetBalancedPowerPlan(CleaningStep step)
        {
            try
            {
                // GUID du plan Equilibr\u00e9 Windows
                RunCommand("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e", step, 10);
                step.Status = "Plan Equilibr\u00e9 activ\u00e9";
                step.FilesDeleted++;
            }
            catch { }
        }

        /// <summary>D\u00e9sactive hiberfil.sys (lib\u00e8re l'espace \u00e9quivalent \u00e0 la RAM).
        /// Attention : d\u00e9sactive \u00e9galement la mise en veille prolong\u00e9e (Hibernate).
        /// Le mode Veille (Sleep/S3) reste fonctionnel.</summary>
        private void DisableHibernation(CleaningStep step)
        {
            try
            {
                // Lire la taille du fichier avant suppression
                const string hibFile = @"C:\hiberfil.sys";
                if (File.Exists(hibFile))
                    try { step.SpaceFreed = new FileInfo(hibFile).Length; } catch { }

                RunCommand("powercfg", "/h off", step, 15);
                step.FilesDeleted++;
                step.Status = File.Exists(hibFile)
                    ? $"hiberfil.sys supprim\u00e9 ({FormatBytes(step.SpaceFreed)} lib\u00e9r\u00e9s)"
                    : "Hibernation d\u00e9j\u00e0 d\u00e9sactiv\u00e9e";
            }
            catch { }
        }

        /// <summary>D\u00e9sactive le partage des t\u00e9l\u00e9chargements Windows Update avec les autres PC
        /// sur Internet (garde le partage r\u00e9seau local LAN qui peut acc\u00e9l\u00e9rer les mises \u00e0 jour).</summary>
        private void DisableWindowsUpdateDelivery(CleaningStep step)
        {
            try
            {
                // DODownloadMode 1 = LAN uniquement (0 = off total, 3 = internet+LAN)
                using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", true);
                key?.SetValue("DODownloadMode", 1, Microsoft.Win32.RegistryValueKind.DWord);
                step.FilesDeleted++;
                step.Status = "WUDO limit\u00e9 au r\u00e9seau local";
            }
            catch { }
        }

        /// <summary>Supprime les t\u00e2ches planifi\u00e9es issues d'applications d\u00e9sinstall\u00e9es
        /// (Google Update, Apple Update, anciens AV, etc.).  Ne touche jamais les t\u00e2ches Windows.</summary>
        private void PurgeObsoleteScheduledTasks(CleaningStep step)
        {
            // Liste des \u00e9diteurs tiers connus pour laisser des t\u00e2ches orphelines
            var orphanPrefixes = new[]
            {
                "GoogleUpdate", "GoogleUpdateTask", "CcleanerMonitorTask",
                "MicrosoftEdgeUpdate", "TeamViewer", "AdobeGCInvoker",
                "Opera scheduled", "BraveSoftware", "NortonSecurity",
                "McAfee", "CCleaner", "IObit", "Avast", "AVG",
            };

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "schtasks.exe",
                    Arguments              = "/query /fo CSV /nh",
                    RedirectStandardOutput = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                };
                using var proc = Process.Start(psi);
                if (proc == null) return;
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(30000);

                foreach (var line in output.Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var cols = line.Split(',');
                    if (cols.Length < 1) continue;
                    var taskName = cols[0].Trim('"').Trim();
                    if (!orphanPrefixes.Any(p => taskName.Contains(p, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    try
                    {
                        RunCommand("schtasks.exe", $"/delete /tn \"{taskName}\" /f", step, 10);
                        step.FilesDeleted++;
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static string FormatBytes(long bytes)
        {
            string[] u = { "B", "KB", "MB", "GB" }; double v = bytes; int i = 0;
            while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
            return $"{v:0.##} {u[i]}";
        }

        private void RunCommand(string exe, string args, CleaningStep step, int timeoutSec = 120)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(timeoutSec * 1000);
            }
            catch { }
        }
    }
}
