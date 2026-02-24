using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NettoyerPc.Core;

namespace NettoyerPc.Modules
{
    public class SecurityModule : ICleaningModule
    {
        public string Name => "Sécurité";

        public List<CleaningStep> GetSteps(CleaningMode mode)
        {
            var steps = new List<CleaningStep>
            {
                new() { Name = "Mise à jour Windows Defender", Category = "security" },
                new() { Name = "Scan antivirus rapide",        Category = "security" }
            };

            if (mode == CleaningMode.DeepClean || mode == CleaningMode.Advanced)
            {
                steps.Add(new() { Name = "Scan antivirus complet", Category = "security" });
            }

            return steps;
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (step.Name.Contains("Mise à jour"))
                {
                    UpdateDefender(step);
                }
                else if (step.Name.Contains("rapide"))
                {
                    ScanDefender(step, quick: true);
                }
                else if (step.Name.Contains("complet"))
                {
                    ScanDefender(step, quick: false);
                }
            }, cancellationToken);
        }

        private void UpdateDefender(CleaningStep step)
        {
            step.AddLog("Mise à jour des signatures Windows Defender...");
            try
            {
                var defenderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Windows Defender",
                    "MpCmdRun.exe");

                if (!File.Exists(defenderPath))
                {
                    step.AddLog("MpCmdRun.exe introuvable — Defender non installé ou chemin modifié");
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName               = defenderPath,
                    Arguments              = "-SignatureUpdate",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(120000);
                    if (!string.IsNullOrWhiteSpace(output))
                        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                            if (line.Trim().Length > 2) step.AddLog(line.TrimEnd());
                    step.AddLog(process.ExitCode == 0
                        ? "✔ Signatures mises à jour avec succès"
                        : $"⚠ Mise à jour terminée (code {process.ExitCode})");
                    step.Status = process.ExitCode == 0 ? "Signatures à jour" : "Mise à jour échouée";
                    // Cette étape ne supprime PAS de fichiers — on ne touche pas à FilesDeleted
                }
            }
            catch (Exception ex) { step.AddLog($"Erreur : {ex.Message}"); }
        }

        private void ScanDefender(CleaningStep step, bool quick)
        {
            var scanLabel = quick ? "rapide" : "complet";
            step.AddLog($"Lancement du scan antivirus {scanLabel}...");
            try
            {
                var defenderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Windows Defender",
                    "MpCmdRun.exe");

                if (!File.Exists(defenderPath))
                {
                    step.AddLog("MpCmdRun.exe introuvable");
                    return;
                }

                var scanType = quick ? "1" : "2";
                var psi = new ProcessStartInfo
                {
                    FileName               = defenderPath,
                    Arguments              = $"-Scan -ScanType {scanType}",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    var timeout = quick ? 600000 : 3600000;
                    var output  = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(timeout);
                    if (!string.IsNullOrWhiteSpace(output))
                        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                            if (line.Trim().Length > 2) step.AddLog(line.TrimEnd());

                    if (process.ExitCode == 0)
                    {
                        step.AddLog("✔ Aucune menace détectée");
                        step.Status = "Scan terminé — système propre";
                    }
                    else if (process.ExitCode == 2)
                    {
                        step.AddLog("⚠ Menace(s) détectée(s) ! Vérifiez le Centre de sécurité.");
                        step.Status = "Menace(s) détectée(s) — action requise";
                    }
                    else
                    {
                        step.AddLog($"Scan terminé (code {process.ExitCode})");
                    }
                }
            }
            catch (Exception ex) { step.AddLog($"Erreur : {ex.Message}"); }
        }
    }
}
