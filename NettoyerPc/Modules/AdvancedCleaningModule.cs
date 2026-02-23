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
                    RunCommand("sfc.exe", "/scannow", step, 3600);
                else if (step.Name.Contains("Défragmentation"))
                    DefragHDD(step);
                else if (step.Name.Contains("TRIM"))
                    TrimSSD(step);
            }, cancellationToken);
        }

        private void CreateRestorePoint(CleaningStep step)
        {
            try
            {
                var script = @"
                    $desc = 'NettoyeurPC2000 - ' + (Get-Date -Format 'yyyy-MM-dd HH:mm')
                    Enable-ComputerRestore -Drive 'C:\'
                    Checkpoint-Computer -Description $desc -RestorePointType 'APPLICATION_INSTALL'
                    Write-Output 'OK'
                ";
                RunPS(script, step, 120);
                step.FilesDeleted++;
                step.Status = "Point de restauration créé";
            }
            catch { }
        }

        private void CleanOldRestorePoints(CleaningStep step)
        {
            try
            {
                // Garder seulement les 3 derniers points de restauration
                var script = @"
                    $snapshots = Get-ComputerRestorePoint | Sort-Object CreationTime -Descending
                    $toDelete = $snapshots | Select-Object -Skip 3
                    foreach ($s in $toDelete) {
                        $id = $s.SequenceNumber
                        $cmd = ""vssadmin delete shadows /quiet /shadow=$id""
                        Invoke-Expression $cmd
                    }
                ";
                RunPS(script, step, 60);
                step.Status = "Anciens points nettoyés";
            }
            catch { }
        }

        private void TriggerWindowsUpdate(CleaningStep step)
        {
            try
            {
                // Lancer Windows Update via PowerShell (module PSWindowsUpdate si disponible, sinon usoclient)
                var checkScript = "Get-Module -ListAvailable -Name PSWindowsUpdate | Select-Object -ExpandProperty Name";
                var checkResult = RunPSString(checkScript);

                if (!string.IsNullOrEmpty(checkResult) && checkResult.Contains("PSWindowsUpdate"))
                {
                    RunPS("Import-Module PSWindowsUpdate; Install-WindowsUpdate -AcceptAll -IgnoreReboot -Silent", step, 3600);
                }
                else
                {
                    // Méthode alternative : usoclient
                    RunCommand("usoclient.exe", "StartInteractiveScan", step, 120);
                }
                step.FilesDeleted++;
                step.Status = "Mise à jour lancée";
            }
            catch { }
        }

        private void DefragHDD(CleaningStep step)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Fixed) continue;
                try
                {
                    // Vérifier si c'est un HDD (DriveType fixed, vérifier si pas SSD via PowerShell)
                    var letter = drive.Name.TrimEnd('\\');
                    var checkSSD = RunPSString($"(Get-PhysicalDisk | Where-Object {{$_.MediaType -eq 'SSD'}} | Select-Object -ExpandProperty DeviceId) -contains '{letter.Substring(0,1)}'");
                    if (checkSSD.Contains("True")) continue; // Skip SSDs

                    RunCommand("defrag", $"{letter} /U /V", step, 3600);
                    step.FilesDeleted++;
                }
                catch { }
            }
        }

        private void TrimSSD(CleaningStep step)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Fixed) continue;
                try
                {
                    var letter = drive.Name.TrimEnd('\\');
                    RunCommand("defrag", $"{letter} /U /V /O", step, 600);
                    step.FilesDeleted++;
                }
                catch { }
            }
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
                proc?.WaitForExit(timeoutSec * 1000);
            }
            catch { }
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
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    RedirectStandardOutput = true,
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
