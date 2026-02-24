using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NettoyerPc.Core;

namespace NettoyerPc.Modules
{
    public class NetworkModule : ICleaningModule
    {
        public string Name => "Réseau";

        public List<CleaningStep> GetSteps(CleaningMode mode)
        {
            var steps = new List<CleaningStep>
            {
                new() { Name = "Flush DNS", Category = "network" }
            };

            if (mode == CleaningMode.DeepClean || mode == CleaningMode.Advanced)
            {
                steps.Add(new() { Name = "Configuration DNS Cloudflare", Category = "network" });
                steps.Add(new() { Name = "Reset IP",                     Category = "network" });
                steps.Add(new() { Name = "Reset Winsock",                Category = "network" });
                steps.Add(new() { Name = "Vidage cache ARP",             Category = "network" });
            }

            return steps;
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (step.Name.Contains("Flush DNS"))
                {
                    step.AddLog("Vidage du cache DNS (ipconfig /flushdns)...");
                    RunCommand("ipconfig", "/flushdns", step);
                }
                else if (step.Name.Contains("Cloudflare"))
                {
                    ConfigureCloudflare(step);
                }
                else if (step.Name.Contains("Reset IP"))
                {
                    step.AddLog("Réinitialisation de la pile TCP/IP v4...");
                    RunCommand("netsh", "int ip reset", step);
                    step.AddLog("⚠ Un redémarrage est requis pour appliquer la réinitialisation IP");
                }
                else if (step.Name.Contains("Winsock"))
                {
                    step.AddLog("Réinitialisation Winsock (catalogue de sockets)...");
                    RunCommand("netsh", "winsock reset", step);
                    step.AddLog("⚠ Un redémarrage est requis pour appliquer Winsock");
                }
                else if (step.Name.Contains("ARP"))
                {
                    step.AddLog("Suppression du cache ARP...");
                    RunCommand("netsh", "interface ip delete arpcache", step);
                }
            }, cancellationToken);
        }

        private void ConfigureCloudflare(CleaningStep step)
        {
            try
            {
                step.AddLog("Configuration DNS Cloudflare sur Ethernet (1.1.1.1 / 1.0.0.1)...");
                // Ethernet
                RunCommand("netsh", @"interface ip set dns ""Ethernet"" static 1.1.1.1 primary", step);
                RunCommand("netsh", @"interface ip add dns ""Ethernet"" 1.0.0.1 index=2", step);

                step.AddLog("Configuration DNS Cloudflare sur Wi-Fi (1.1.1.1 / 1.0.0.1)...");
                // Wi-Fi
                RunCommand("netsh", @"interface ip set dns ""Wi-Fi"" static 1.1.1.1 primary", step);
                RunCommand("netsh", @"interface ip add dns ""Wi-Fi"" 1.0.0.1 index=2", step);
                step.AddLog("✔ DNS Cloudflare configuré (1.1.1.1 / 1.0.0.1)");
            }
            catch (Exception ex) { step.AddLog($"Erreur DNS Cloudflare : {ex.Message}"); }
        }

        /// <summary>
        /// Exécute une commande réseau via cmd.exe visible (droits élevés),
        /// capture la sortie et l'enregistre dans les logs de l'étape.
        /// Les opérations réseau ne suppriment pas de fichiers : pas de FilesDeleted++.
        /// </summary>
        private void RunCommand(string command, string arguments, CleaningStep step)
        {
            step.AddLog($"> {command} {arguments}");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "cmd.exe",
                    Arguments              = $"/c {command} {arguments}",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = false,
                    WindowStyle            = System.Diagnostics.ProcessWindowStyle.Normal
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    string? line;
                    while ((line = process.StandardOutput.ReadLine()) != null)
                        if (!string.IsNullOrWhiteSpace(line)) step.AddLog(line.Trim());
                    process.WaitForExit(30000);
                    step.AddLog($"Code retour : {process.ExitCode}");
                }
            }
            catch (Exception ex) { step.AddLog($"Erreur : {ex.Message}"); }
        }
    }
}
