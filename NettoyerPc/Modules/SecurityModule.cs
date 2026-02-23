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
            try
            {
                var defenderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Windows Defender",
                    "MpCmdRun.exe");

                if (File.Exists(defenderPath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = defenderPath,
                        Arguments = "-SignatureUpdate",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process != null)
                    {
                        process.WaitForExit(120000); // 2 minutes timeout
                        step.FilesDeleted = process.ExitCode == 0 ? 1 : 0;
                    }
                }
            }
            catch { }
        }

        private void ScanDefender(CleaningStep step, bool quick)
        {
            try
            {
                var defenderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Windows Defender",
                    "MpCmdRun.exe");

                if (File.Exists(defenderPath))
                {
                    var scanType = quick ? "1" : "2"; // 1=quick, 2=full
                    var psi = new ProcessStartInfo
                    {
                        FileName = defenderPath,
                        Arguments = $"-Scan -ScanType {scanType}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process != null)
                    {
                        var timeout = quick ? 600000 : 3600000; // 10 min ou 1h
                        process.WaitForExit(timeout);
                        
                        if (process.ExitCode == 0)
                        {
                            step.FilesDeleted = 0; // Aucune menace
                        }
                        else if (process.ExitCode == 2)
                        {
                            step.FilesDeleted = 1; // Menaces détectées
                        }
                    }
                }
            }
            catch { }
        }
    }
}
