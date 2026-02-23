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
                    RunCommand("ipconfig", "/flushdns", step);
                }
                else if (step.Name.Contains("Cloudflare"))
                {
                    ConfigureCloudllareDNS(step);
                }
                else if (step.Name.Contains("Reset IP"))
                {
                    RunCommand("netsh", "int ip reset", step);
                }
                else if (step.Name.Contains("Winsock"))
                {
                    RunCommand("netsh", "winsock reset", step);
                }
                else if (step.Name.Contains("ARP"))
                {
                    RunCommand("netsh", "interface ip delete arpcache", step);
                }
            }, cancellationToken);
        }

        private void ConfigureCloudllareDNS(CleaningStep step)
        {
            try
            {
                // Ethernet
                RunCommand("netsh", @"interface ip set dns ""Ethernet"" static 1.1.1.1 primary", step);
                RunCommand("netsh", @"interface ip add dns ""Ethernet"" 1.0.0.1 index=2", step);
                
                // Wi-Fi
                RunCommand("netsh", @"interface ip set dns ""Wi-Fi"" static 1.1.1.1 primary", step);
                RunCommand("netsh", @"interface ip add dns ""Wi-Fi"" 1.0.0.1 index=2", step);
            }
            catch { }
        }

        private void RunCommand(string command, string arguments, CleaningStep step)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit(30000);
                    step.FilesDeleted++;
                }
            }
            catch { }
        }
    }
}
