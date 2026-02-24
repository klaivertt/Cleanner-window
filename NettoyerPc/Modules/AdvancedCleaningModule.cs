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
    /// Nettoyage avancé : point de restauration, Windows Update, défrag intelligente
    /// </summary>
    public class AdvancedCleaningModule : ICleaningModule
    {
        public string Name => "Nettoyage Avancé";

        public List<CleaningStep> GetSteps(CleaningMode mode)
        {
            return new List<CleaningStep>
            {
                new() { Name = "Création point de restauration automatique", Category = "advanced" },
                new() { Name = "Nettoyage anciens points de restauration", Category = "advanced" },
                new() { Name = "Mise à jour Windows automatique", Category = "advanced" },
                new() { Name = "Vérification fichiers critiques système (SFC)", Category = "advanced" },
                new() { Name = "Défragmentation intelligente (disques HDD)", Category = "advanced" },
                new() { Name = "TRIM disques SSD", Category = "advanced" },
            };
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (step.Name.Contains("Création point"))
                    CreateRestorePoint(step);
                else if (step.Name.Contains("anciens points"))
                    CleanOldRestorePoints(step);
                else if (step.Name.Contains("Mise à jour Windows"))
                    TriggerWindowsUpdate(step);
                else if (step.Name.Contains("SFC"))
                    RunPrivilegedCommand("sfc.exe /scannow", step, 3600);
                else if (step.Name.Contains("Défragmentation"))
                    DefragHDD(step);
                else if (step.Name.Contains("TRIM"))
                    TrimSSD(step);
            }, cancellationToken);
        }

        private void CreateRestorePoint(CleaningStep step)
        {
            if (!Core.UserPreferences.Current.AutoRestorePoint)
            {
                step.AddLog("ℹ Création de point de restauration désactivée dans les paramètres.");
                step.Status = "Point de restauration désactivé (préférences)";
                return;
            }
            try
            {
                step.AddLog("Activation de la protection du système sur C: ...");
                step.AddLog("Création du point de restauration système...");
                var script = "$desc = 'NettoyeurPC2000 - ' + (Get-Date -Format 'yyyy-MM-dd HH:mm'); " +
                             "Enable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue; " +
                             "Checkpoint-Computer -Description $desc -RestorePointType 'APPLICATION_INSTALL' -ErrorAction SilentlyContinue; " +
                             "Write-Output ('Point cree : ' + $desc)";
                RunPS(script, step, 120);
                step.AddLog("✔ Point de restauration créé avec succès");
                step.Status = "Point de restauration créé";
            }
            catch (Exception ex)
            {
                step.AddLog($"✗ Échec création point de restauration : {ex.Message}");
                step.Status = "Point de restauration : échec";
            }
        }

        private void CleanOldRestorePoints(CleaningStep step)
        {
            try
            {
                step.AddLog("Inventaire des points de restauration système...");
                // Compter les points existants
                var countScript = "(Get-ComputerRestorePoint | Measure-Object).Count";
                var countStr = RunPSString(countScript).Trim();
                if (!int.TryParse(countStr, out int total))
                {
                    step.AddLog("⚠ Impossible de lire les points de restauration (droits insuffisants ?)");
                    step.Status = "Points de restauration inaccessibles";
                    return;
                }
                step.AddLog($"  {total} point(s) de restauration trouvé(s)");
                int toDelete = Math.Max(0, total - 3);
                if (toDelete == 0)
                {
                    step.AddLog("✅ 3 points ou moins — rien à supprimer");
                    step.Status = "Points de restauration à jour";
                    return;
                }
                step.AddLog($"  Suppression de {toDelete} ancien(s) point(s) (garde les 3 plus récents)...");
                // vssadmin delete shadows /for=C: /oldest /quiet supprime le plus ancien ou premier - on boucle
                for (int i = 0; i < toDelete; i++)
                {
                    RunCommand("vssadmin", "delete shadows /for=C: /oldest /quiet", step, 30);
                    step.AddLog($"  ✔ Ancien point supprimé ({i + 1}/{toDelete})");
                    step.FilesDeleted++;
                }
                step.AddLog($"✔ {toDelete} ancien(s) point(s) de restauration supprimé(s)");
                step.Status = $"{toDelete} ancien(s) point(s) supprimé(s)";
            }
            catch (Exception ex) { step.AddLog($"✗ Erreur points restauration : {ex.Message}"); }
        }

        private void TriggerWindowsUpdate(CleaningStep step)
        {
            try
            {
                step.AddLog("Vérification du module PSWindowsUpdate...");
                var checkScript = "Get-Module -ListAvailable -Name PSWindowsUpdate | Select-Object -ExpandProperty Name";
                var checkResult = RunPSString(checkScript);

                if (!string.IsNullOrEmpty(checkResult) && checkResult.Contains("PSWindowsUpdate"))
                {
                    step.AddLog("Module PSWindowsUpdate trouvé — lancement Install-WindowsUpdate");
                    RunPS("Import-Module PSWindowsUpdate; Install-WindowsUpdate -AcceptAll -IgnoreReboot -Silent", step, 3600);
                }
                else
                {
                    step.AddLog("Module PSWindowsUpdate absent — utilisation de usoclient");
                    RunCommand("usoclient.exe", "StartInteractiveScan", step, 120);
                }
                step.Status = "Mise à jour lancée";
            }
            catch { }
        }

        private void DefragHDD(CleaningStep step)
        {
            // Utilise /O (Optimize) qui choisit automatiquement le mode optimal :
            // défragmentation pour les HDD, TRIM/optimize pour les SSD.
            // Plus fiable que la détection manuelle du type de disque.
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Fixed) continue;
                try
                {
                    var letter = drive.Name.TrimEnd('\\');
                    var freeGb  = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
                    var totalGb = drive.TotalSize          / 1024.0 / 1024.0 / 1024.0;
                    step.AddLog($"Optimisation {letter}  {totalGb:0.#} GB  |  {freeGb:0.#} GB libre");
                    RunCommand("defrag", $"{letter} /O /U /V", step, 3600);
                    step.AddLog($"  ✔ {letter} optimisé");
                }
                catch { }
            }
            step.Status = "Disques optimisés";
        }

        private void TrimSSD(CleaningStep step)
        {
            int count = 0;
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Fixed) continue;
                try
                {
                    var letter = drive.Name.TrimEnd('\\');
                    step.AddLog($"TRIM / Optimisation SSD : {letter}");
                    RunCommand("defrag", $"{letter} /U /V /O", step, 600);
                    step.AddLog($"  ✔ {letter} TRIM effectué");
                    count++;
                }
                catch { }
            }
            step.AddLog($"✔ {count} disque(s) optimisé(s) (TRIM/Optimize)");
            step.Status = $"{count} disque(s) optimisé(s)";
        }

        private void RunPS(string script, CleaningStep step, int timeoutSec = 60)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NonInteractive -NoProfile -Command \"{script.Replace("\"", "\\\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string? line;
                    while ((line = proc.StandardOutput.ReadLine()) != null)
                        if (!string.IsNullOrWhiteSpace(line)) step.AddLog(line.Trim());
                    proc.WaitForExit(timeoutSec * 1000);
                }
            }
            catch { }
        }

        /// <summary>Exécute une commande nécessitant élévation dans un CMD visible (SFC, DISM…).</summary>
        private void RunPrivilegedCommand(string cmdLine, CleaningStep step, int timeoutSec = 120)
        {
            step.AddLog($"> {cmdLine}");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "cmd.exe",
                    Arguments              = $"/c {cmdLine}",
                    UseShellExecute        = false,
                    CreateNoWindow         = false,
                    WindowStyle            = ProcessWindowStyle.Normal
                };
                using var proc = Process.Start(psi);
                if (proc == null) { step.AddLog("Impossible de démarrer le processus."); return; }
                bool finished = proc.WaitForExit(timeoutSec * 1000);
                step.AddLog(finished ? $"Terminé (code {proc.ExitCode})" : "Délai dépassé — processus toujours actif");
            }
            catch (Exception ex) { step.AddLog($"Erreur : {ex.Message}"); }
        }

        private string RunPSString(string script)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NonInteractive -NoProfile -Command \"{script}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                var output = proc?.StandardOutput.ReadToEnd() ?? "";
                proc?.WaitForExit(30000);
                return output;
            }
            catch { return ""; }
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
                if (proc != null)
                {
                    string? line;
                    while ((line = proc.StandardOutput.ReadLine()) != null)
                        if (!string.IsNullOrWhiteSpace(line)) step.AddLog(line.Trim());
                    proc.WaitForExit(timeoutSec * 1000);
                }
            }
            catch (Exception ex) { step.AddLog($"Erreur : {ex.Message}"); }
        }
    }
}
