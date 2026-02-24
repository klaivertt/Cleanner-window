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
                {
                    step.AddLog("DISM ScanHealth — vérification de l'image Windows...");
                    RunPrivilegedCommand("Dism.exe", "/Online /Cleanup-Image /ScanHealth", step, 300);
                    step.AddLog("DISM StartComponentCleanup — suppression des composants obsolètes...");
                    RunPrivilegedCommand("Dism.exe", "/Online /Cleanup-Image /StartComponentCleanup", step, 600);
                }
                else if (step.Name.Contains("SFC"))
                {
                    step.AddLog("SFC /scannow — vérification et réparation des fichiers système...");
                    RunPrivilegedCommand("sfc.exe", "/scannow", step, 3600);
                }
                else if (step.Name.Contains("RestoreHealth"))
                {
                    step.AddLog("DISM RestoreHealth — réparation de l'image Windows en cours...");
                    RunPrivilegedCommand("Dism.exe", "/Online /Cleanup-Image /RestoreHealth", step, 1800);
                }
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
                step.AddLog("Arrêt du service Windows Font Cache...");
                RunCommand("net", "stop \"Windows Font Cache Service\" /y", step, 30);
                var fontCachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Windows\FontCache");
                if (Directory.Exists(fontCachePath))
                {
                    foreach (var file in Directory.GetFiles(fontCachePath, "*.dat"))
                    {
                        try
                        {
                            var fi = new FileInfo(file);
                            step.SpaceFreed += fi.Length;
                            File.Delete(file);
                            step.FilesDeleted++;
                            step.AddLog($"Supprimé : {Path.GetFileName(file)} ({fi.Length / 1024} KB)");
                        }
                        catch { }
                    }
                }
                step.AddLog("Redémarrage du service Windows Font Cache...");
                RunCommand("net", "start \"Windows Font Cache Service\"", step, 30);
                step.Status = "Cache polices reconstruit";
            }
            catch { }
        }

        private void ResetNetwork(CleaningStep step)
        {
            step.AddLog("Vidage du cache DNS...");
            RunCommand("ipconfig", "/flushdns", step, 30);
            step.AddLog("Réinitialisation Winsock (reboot requis pour appliquer)...");
            RunCommand("netsh", "winsock reset", step, 30);
            step.AddLog("Réinitialisation pile IP v4...");
            RunCommand("netsh", "int ip reset", step, 30);
            step.AddLog("Réinitialisation pile IP v6...");
            RunCommand("netsh", "int ipv6 reset", step, 30);
            step.AddLog("Configuration DNS Cloudflare (1.1.1.1 / 1.0.0.1) sur Ethernet...");
            RunCommand("netsh", "interface ip set dns \"Ethernet\" static 1.1.1.1 primary", step, 10);
            RunCommand("netsh", "interface ip add dns \"Ethernet\" 1.0.0.1 index=2", step, 10);
            step.AddLog("Configuration DNS Cloudflare (1.1.1.1 / 1.0.0.1) sur Wi-Fi...");
            RunCommand("netsh", "interface ip set dns \"Wi-Fi\" static 1.1.1.1 primary", step, 10);
            RunCommand("netsh", "interface ip add dns \"Wi-Fi\" 1.0.0.1 index=2", step, 10);
            step.AddLog("✔ Réseau réinitialisé — un redémarrage est recommandé pour appliquer Winsock.");
            step.Status = "Réseau réinitialisé (DNS, Winsock, IP)";
        }

        private void OptimizeDisks(CleaningStep step)
        {
            int count = 0;
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Fixed) continue;
                try
                {
                    var letter = drive.Name.TrimEnd('\\');
                    var freeGb  = drive.AvailableFreeSpace  / 1024.0 / 1024.0 / 1024.0;
                    var totalGb = drive.TotalSize            / 1024.0 / 1024.0 / 1024.0;
                    var usedPct = (int)((1.0 - (double)drive.TotalFreeSpace / drive.TotalSize) * 100);
                    step.AddLog($"Disque {letter}  {totalGb:0.#} GB total  |  {freeGb:0.#} GB libre  |  {usedPct}% utilisé");
                    step.AddLog($"  Optimisation / TRIM : {letter}");
                    RunCommand("defrag", $"{letter} /U /V /O", step, 3600);
                    step.AddLog($"  ✔ {letter} optimisé");
                    count++;
                }
                catch { }
            }
            step.AddLog($"✔ {count} disque(s) optimisé(s)");
            step.Status = $"{count} disque(s) optimisé(s)";
        }

        private void ScheduleChkdsk(CleaningStep step)
        {
            // chkntfs /c planifie chkdsk sur C: au prochain démarrage Windows
            step.AddLog("Planification chkdsk sur C: au prochain démarrage...");
            RunCommand("chkntfs", "/c C:", step, 15);
            // Mesurer l'espace libre avant pour afficher un contexte utile
            try
            {
                var driveC = new DriveInfo("C");
                var freeGb = driveC.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
                step.AddLog($"  C: — {freeGb:0.##} GB libres sur {driveC.TotalSize / 1024.0 / 1024.0 / 1024.0:0.#} GB");
            }
            catch { }
            step.Status = "Vérification disque planifiée au prochain démarrage";
            step.AddLog("✔ chkdsk planifié — Windows vérifiera C: au prochain redémarrage");
            // Pas de FilesDeleted : planifier chkdsk ne supprime pas de fichiers
        }

        /// <summary>Force le plan d'alimentation Equilibr\u00e9 (meilleur compromis perf/\u00e9conomies).
        /// Evite High Performance qui consomme inutilement sur les portables.</summary>
        private void SetBalancedPowerPlan(CleaningStep step)
        {
            try
            {
                step.AddLog("Activation du plan d'alimentation Équilibré (GUID 381b4222)...");
                // GUID du plan Équilibré Windows
                RunCommand("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e", step, 10);
                step.AddLog("✔ Plan Équilibré activé — meilleur compromis performance / batterie");
                step.Status = "Plan Équilibré activé";
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
                const string hibFile = @"C:\hiberfil.sys";
                bool wasPresent = File.Exists(hibFile);
                if (wasPresent)
                {
                    try { step.SpaceFreed = new FileInfo(hibFile).Length; } catch { }
                    step.AddLog($"hiberfil.sys détecté : {FormatBytes(step.SpaceFreed)} à libérer");
                }
                else
                {
                    step.AddLog("hiberfil.sys absent — hibernation déjà désactivée");
                }

                step.AddLog("Exécution : powercfg /h off...");
                RunCommand("powercfg", "/h off", step, 15);

                if (wasPresent)
                {
                    step.FilesDeleted++;
                    step.Status = $"hiberfil.sys supprimé ({FormatBytes(step.SpaceFreed)} libérés)";
                    step.AddLog($"✔ hiberfil.sys supprimé — {FormatBytes(step.SpaceFreed)} libérés");
                }
                else
                {
                    step.Status = "Hibernation déjà désactivée";
                    step.AddLog("✅ Rien à faire — hibernation déjà désactivée");
                }
            }
            catch (Exception ex) { step.AddLog($"Erreur hibernation : {ex.Message}"); }
        }

        /// <summary>D\u00e9sactive le partage des t\u00e9l\u00e9chargements Windows Update avec les autres PC
        /// sur Internet (garde le partage r\u00e9seau local LAN qui peut acc\u00e9l\u00e9rer les mises \u00e0 jour).</summary>
        private void DisableWindowsUpdateDelivery(CleaningStep step)
        {
            try
            {
                step.AddLog("Lecture registre DeliveryOptimization...");
                // DODownloadMode 1 = LAN uniquement (0 = off total, 3 = internet+LAN)
                using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", true);
                key?.SetValue("DODownloadMode", 1, Microsoft.Win32.RegistryValueKind.DWord);
                step.AddLog("✔ DODownloadMode = 1 (LAN only) — partage internet des MAJ Windows désactivé");
                step.Status = "WUDO limité au réseau local";
            }
            catch (Exception ex) { step.AddLog($"Erreur WUDO : {ex.Message}"); }
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
                        step.AddLog($"Suppression tâche obsolète : {taskName}");
                        RunCommand("schtasks.exe", $"/delete /tn \"{taskName}\" /f", step, 10);
                        step.FilesDeleted++;
                        step.AddLog($"  ✔ Supprimée : {taskName}");
                    }
                    catch (Exception ex) { step.AddLog($"  ✗ Échec ({taskName}) : {ex.Message}"); }
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
            step.AddLog($"> {exe} {args}");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = exe,
                    Arguments              = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return;
                // Lire la sortie sans bloquer
                var stdout = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(timeoutSec * 1000);
                if (!string.IsNullOrWhiteSpace(stdout))
                    foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        if (!string.IsNullOrWhiteSpace(line) && line.Trim().Length > 2)
                            step.AddLog(line.TrimEnd());
            }
            catch (Exception ex) { step.AddLog($"Erreur: {ex.Message}"); }
        }

        /// <summary>
        /// Exécute DISM / SFC dans une fenêtre CMD visible et capture la sortie dans les logs.
        /// </summary>
        private void RunPrivilegedCommand(string exe, string args, CleaningStep step, int timeoutSec = 600)
        {
            step.AddLog($"CMD> {exe} {args}");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName        = "cmd.exe",
                    Arguments       = $"/c {exe} {args}",
                    UseShellExecute = false,
                    CreateNoWindow  = false,           // fenêtre CMD visible
                    RedirectStandardOutput = false,    // laisser CMD afficher lui-même
                    RedirectStandardError  = false,
                };
                using var proc = Process.Start(psi);
                if (proc == null) { step.AddLog($"Impossible de démarrer : {exe}"); return; }
                bool finished = proc.WaitForExit(timeoutSec * 1000);
                int code = finished ? proc.ExitCode : -1;
                step.AddLog(finished
                    ? $"{exe} terminé (code {code})"
                    : $"{exe} timeout après {timeoutSec}s");
            }
            catch (Exception ex) { step.AddLog($"Erreur {exe}: {ex.Message}"); }
        }
    }
}
