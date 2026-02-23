using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NettoyerPc.Core;
using Microsoft.Win32;

namespace NettoyerPc.Modules
{
    /// <summary>
    /// Désinstallation des bloatwares Windows courants via PowerShell/winget
    /// </summary>
    public class BloatwareModule : ICleaningModule
    {
        public string Name => "Suppression Bloatwares";

        // Appx packages à supprimer (AppxPackageName)
        private static readonly (string Display, string Package)[] AppxBloatware =
        {
            ("Candy Crush Solitaire",        "king.com.CandyCrushSolitaire"),
            ("Candy Crush Friends",           "king.com.CandyCrushFriends"),
            ("Farm Heroes Saga",             "king.com.FarmHeroesSaga"),
            ("Bubble Witch 3",               "king.com.BubbleWitch3Saga"),
            ("TikTok",                       "BytedancePteLtd.TikTok"),
            ("Facebook",                     "Facebook.Facebook"),
            ("Instagram",                    "Facebook.Instagram"),
            ("Netflix",                      "4DF9E0F3.Netflix"),
            ("Xbox GameBar",                 "Microsoft.XboxGamingOverlay"),
            ("Xbox Identity Provider",       "Microsoft.XboxIdentityProvider"),
            ("Get Started",                  "Microsoft.Getstarted"),
            ("Clipchamp",                    "Clipchamp.Clipchamp"),
            ("Microsoft Solitaire",          "Microsoft.MicrosoftSolitaireCollection"),
            ("Mixed Reality Portal",         "Microsoft.MixedReality.Portal"),
            ("Cortana",                      "Microsoft.549981C3F5F10"),
            ("Bing Search",                  "Microsoft.BingSearch"),
            ("Microsoft Tips",               "Microsoft.Getstarted"),
            ("ToDo",                         "Microsoft.Todos"),
        };

        public List<CleaningStep> GetSteps(CleaningMode mode)
        {
            var steps = new List<CleaningStep>
            {
                new() { Name = "Analyse bloatwares installés", Category = "bloatware" },
                new() { Name = "Suppression Candy Crush / Jeux King", Category = "bloatware" },
                new() { Name = "Suppression Apps sociales (Facebook, Instagram, TikTok)", Category = "bloatware" },
                new() { Name = "Suppression Xbox GameBar / Mixed Reality", Category = "bloatware" },
                new() { Name = "Suppression Cortana / Bing Search", Category = "bloatware" },
                new() { Name = "Suppression Netflix / Microsoft Solitaire", Category = "bloatware" },
                new() { Name = "Désactivation télémétrie Windows", Category = "bloatware" },
            };
            return steps;
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (step.Name.Contains("Analyse"))
                    AnalyseBloatware(step);
                else if (step.Name.Contains("Candy") || step.Name.Contains("King"))
                    RemoveAppxGroup(step, new[] { "king.com" });
                else if (step.Name.Contains("sociales"))
                    RemoveAppxGroup(step, new[] { "Facebook", "BytedancePteLtd", "Instagram" });
                else if (step.Name.Contains("Xbox") || step.Name.Contains("Mixed"))
                    RemoveAppxGroup(step, new[] { "Microsoft.XboxGamingOverlay", "Microsoft.XboxIdentityProvider", "Microsoft.MixedReality.Portal" });
                else if (step.Name.Contains("Cortana") || step.Name.Contains("Bing"))
                    RemoveAppxGroup(step, new[] { "Microsoft.549981C3F5F10", "Microsoft.BingSearch" });
                else if (step.Name.Contains("Netflix") || step.Name.Contains("Solitaire"))
                    RemoveAppxGroup(step, new[] { "4DF9E0F3.Netflix", "Microsoft.MicrosoftSolitaireCollection", "Clipchamp.Clipchamp" });
                else if (step.Name.Contains("télémétrie"))
                    DisableTelemetry(step);
            }, cancellationToken);
        }

        private void AnalyseBloatware(CleaningStep step)
        {
            try
            {
                var ps = RunPS("Get-AppxPackage | Select-Object -ExpandProperty Name");
                if (!string.IsNullOrEmpty(ps))
                {
                    var installed = ps.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    int found = 0;
                    foreach (var (_, pkg) in AppxBloatware)
                    {
                        if (installed.Any(i => i.Trim().Contains(pkg, StringComparison.OrdinalIgnoreCase)))
                            found++;
                    }
                    step.Status = $"{found} bloatwares détectés";
                    step.FilesDeleted = found;
                }
            }
            catch { }
        }

        private void RemoveAppxGroup(CleaningStep step, string[] keywords)
        {
            foreach (var kw in keywords)
            {
                try
                {
                    RunPS($"Get-AppxPackage *{kw}* | Remove-AppxPackage -ErrorAction SilentlyContinue");
                    RunPS($"Get-AppxProvisionedPackage -Online | Where-Object {{$_.DisplayName -like \"*{kw}*\"}} | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue");
                    step.FilesDeleted++;
                }
                catch { }
            }
        }

        private void DisableTelemetry(CleaningStep step)
        {
            try
            {
                // Désactiver DiagTrack
                RunCommand("sc", "config DiagTrack start= disabled");
                RunCommand("sc", "stop DiagTrack");
                // Désactiver dmwappushsvc
                RunCommand("sc", "config dmwappushsvc start= disabled");
                RunCommand("sc", "stop dmwappushsvc");
                // Hosts: bloquer les serveurs de télémétrie MS
                var hostsPath = @"C:\Windows\System32\drivers\etc\hosts";
                var entries = new[]
                {
                    "0.0.0.0 telemetry.microsoft.com",
                    "0.0.0.0 vortex.data.microsoft.com",
                    "0.0.0.0 settings-win.data.microsoft.com",
                };
                var currentHosts = System.IO.File.ReadAllText(hostsPath);
                foreach (var entry in entries)
                {
                    if (!currentHosts.Contains(entry))
                        System.IO.File.AppendAllText(hostsPath, "\n" + entry);
                }
                step.FilesDeleted++;
            }
            catch { }
        }

        private string RunPS(string script)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NonInteractive -NoProfile -Command \"{script}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                var output = proc?.StandardOutput.ReadToEnd() ?? "";
                proc?.WaitForExit(60000);
                return output;
            }
            catch { return ""; }
        }

        private void RunCommand(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(30000);
            }
            catch { }
        }
    }
}
