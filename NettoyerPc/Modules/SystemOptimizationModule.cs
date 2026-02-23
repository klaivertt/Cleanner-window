using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
