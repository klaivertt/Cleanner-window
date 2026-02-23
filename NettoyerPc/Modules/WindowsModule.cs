using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using NettoyerPc.Core;

namespace NettoyerPc.Modules
{
    public class WindowsModule : ICleaningModule
    {
        public string Name => "Windows";

        public List<CleaningStep> GetSteps(CleaningMode mode)
        {
            var steps = new List<CleaningStep>
            {
                new() { Name = "Corbeilles (tous disques)",     Category = "windows" },
                new() { Name = "Journaux Windows",              Category = "windows" },
                new() { Name = "Nettoyage disque (cleanmgr)",   Category = "windows" }
            };

            if (mode == CleaningMode.DeepClean || mode == CleaningMode.Advanced)
            {
                steps.Add(new() { Name = "Optimisations registre",           Category = "windows" });
                steps.Add(new() { Name = "Vérification disque C:",           Category = "windows" });
                steps.Add(new() { Name = "Défragmentation (tous disques)",   Category = "windows" });
                steps.Add(new() { Name = "Windows Update cache",             Category = "windows" });
                steps.Add(new() { Name = "DISM cleanup",                     Category = "windows" });
            }

            return steps;
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (step.Name.Contains("Corbeilles"))
                {
                    EmptyRecycleBins(step);
                }
                else if (step.Name.Contains("Journaux"))
                {
                    ClearEventLogs(step);
                }
                else if (step.Name.Contains("cleanmgr"))
                {
                    RunDiskCleanup(step);
                }
                else if (step.Name.Contains("registre"))
                {
                    OptimizeRegistry(step);
                }
                else if (step.Name.Contains("Vérification disque"))
                {
                    RunCommand("chkdsk", "C: /scan", step);
                }
                else if (step.Name.Contains("Défragmentation"))
                {
                    DefragmentDrives(step);
                }
                else if (step.Name.Contains("Windows Update"))
                {
                    CleanWindowsUpdate(step);
                }
                else if (step.Name.Contains("DISM"))
                {
                    RunCommand("Dism.exe", "/online /Cleanup-Image /StartComponentCleanup", step);
                }
            }, cancellationToken);
        }

        private void EmptyRecycleBins(CleaningStep step)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    var recycleBin = Path.Combine(drive.RootDirectory.FullName, "$Recycle.Bin");
                    if (Directory.Exists(recycleBin))
                    {
                        try
                        {
                            var dirInfo = new DirectoryInfo(recycleBin);
                            step.SpaceFreed += GetDirectorySize(dirInfo);
                            Directory.Delete(recycleBin, true);
                            step.FilesDeleted++;
                        }
                        catch { }
                    }
                }
            }
        }

        private void ClearEventLogs(CleaningStep step)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "wevtutil.exe",
                    Arguments = "el",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        var logName = process.StandardOutput.ReadLine();
                        if (!string.IsNullOrEmpty(logName))
                        {
                            try
                            {
                                var clearPsi = new ProcessStartInfo
                                {
                                    FileName = "wevtutil.exe",
                                    Arguments = $"cl \"{logName}\"",
                                    RedirectStandardOutput = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };
                                using var clearProcess = Process.Start(clearPsi);
                                clearProcess?.WaitForExit(5000);
                                step.FilesDeleted++;
                            }
                            catch { }
                        }
                    }
                    process.WaitForExit();
                }
            }
            catch { }
        }

        private void RunDiskCleanup(CleaningStep step)
        {
            try
            {
                // Configuration des flags de nettoyage
                string[] cleanupKeys = 
                {
                    "Active Setup Temp Folders", "BranchCache", "Content Indexer Cleaner",
                    "D3D Shader Cache", "Delivery Optimization Files", "Device Driver Packages",
                    "Downloaded Program Files", "Internet Cache Files", "Memory Dump Files",
                    "Recycle Bin", "Temporary Files", "Temporary Setup Files", 
                    "Thumbnail Cache", "Update Cleanup", "Windows Defender",
                    "Windows Error Reporting Files", "Windows ESD installation files",
                    "Windows Upgrade Log Files"
                };

                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches", true);
                
                if (key != null)
                {
                    foreach (var subKeyName in cleanupKeys)
                    {
                        try
                        {
                            using var subKey = key.OpenSubKey(subKeyName, true);
                            subKey?.SetValue("StateFlags0001", 2, RegistryValueKind.DWord);
                        }
                        catch { }
                    }
                }

                // Lancement du nettoyage
                var psi = new ProcessStartInfo
                {
                    FileName = "cleanmgr.exe",
                    Arguments = "/sagerun:1",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process?.WaitForExit();
                step.FilesDeleted++;
            }
            catch { }
        }

        private void OptimizeRegistry(CleaningStep step)
        {
            try
            {
                // Désactiver animations
                using var key1 = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\WindowMetrics", true);
                key1?.SetValue("MinAnimate", "0", RegistryValueKind.String);

                // Menu rapide
                using var key2 = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
                key2?.SetValue("MenuShowDelay", "0", RegistryValueKind.String);

                // Désactiver télémétrie
                var services = new[] { "DiagTrack", "dmwappushservice" };
                foreach (var serviceName in services)
                {
                    try
                    {
                        using var service = new ServiceController(serviceName);
                        if (service.Status != ServiceControllerStatus.Stopped)
                        {
                            service.Stop();
                        }
                        
                        RunCommand("sc", $"config {serviceName} start= disabled", step);
                    }
                    catch { }
                }

                step.FilesDeleted++;
            }
            catch { }
        }

        private void DefragmentDrives(CleaningStep step)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "defrag",
                            Arguments = $"{drive.Name} /O",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using var process = Process.Start(psi);
                        process?.WaitForExit(300000); // 5 minutes timeout par disque
                        step.FilesDeleted++;
                    }
                    catch { }
                }
            }
        }

        private void CleanWindowsUpdate(CleaningStep step)
        {
            try
            {
                // Arrêter les services
                StopService("wuauserv");
                StopService("bits");
                Thread.Sleep(2000);

                // Nettoyer le cache
                var downloadPath = @"C:\Windows\SoftwareDistribution\Download";
                if (Directory.Exists(downloadPath))
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(downloadPath);
                        step.SpaceFreed += GetDirectorySize(dirInfo);
                        
                        foreach (var file in Directory.GetFiles(downloadPath))
                        {
                            try { File.Delete(file); step.FilesDeleted++; }
                            catch { }
                        }
                        foreach (var dir in Directory.GetDirectories(downloadPath))
                        {
                            try { Directory.Delete(dir, true); step.FilesDeleted++; }
                            catch { }
                        }
                    }
                    catch { }
                }

                // Redémarrer les services
                StartService("wuauserv");
                StartService("bits");
            }
            catch { }
        }

        private void StopService(string serviceName)
        {
            try
            {
                using var service = new ServiceController(serviceName);
                if (service.Status != ServiceControllerStatus.Stopped)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
            }
            catch { }
        }

        private void StartService(string serviceName)
        {
            try
            {
                using var service = new ServiceController(serviceName);
                if (service.Status != ServiceControllerStatus.Running)
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }
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
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process?.WaitForExit(60000);
            }
            catch { }
        }

        private long GetDirectorySize(DirectoryInfo dirInfo)
        {
            long size = 0;
            try
            {
                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    try { size += file.Length; }
                    catch { }
                }
            }
            catch { }
            return size;
        }
    }
}
