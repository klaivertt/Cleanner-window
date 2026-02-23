using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NettoyerPc.Core;

namespace NettoyerPc.Modules
{
    public class TempFilesModule : ICleaningModule
    {
        public string Name => "Fichiers temporaires";

        public List<CleaningStep> GetSteps(CleaningMode mode)
        {
            return new List<CleaningStep>
            {
                new() { Name = "Suppression TEMP utilisateur",  Category = "general" },
                new() { Name = "Suppression Windows Temp",       Category = "general" },
                new() { Name = "Suppression Prefetch",           Category = "general" },
                new() { Name = "Suppression Thumbnails",         Category = "general" },
                new() { Name = "Rapports d'erreurs Windows (WER)",  Category = "general" },
                new() { Name = "Cache icones Windows",              Category = "general" },
                new() { Name = "Fichiers Crash Dumps",              Category = "general" }
            };
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (step.Name.Contains("TEMP utilisateur"))
                {
                    CleanDirectory(Environment.GetEnvironmentVariable("TEMP") ?? "", step);
                }
                else if (step.Name.Contains("Windows Temp"))
                {
                    CleanDirectory(@"C:\Windows\Temp", step);
                }
                else if (step.Name.Contains("Prefetch"))
                {
                    CleanDirectory(@"C:\Windows\Prefetch", step, false);
                }
                else if (step.Name.Contains("Thumbnails"))
                {
                    var explorerPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        @"Microsoft\Windows\Explorer");
                    
                    if (Directory.Exists(explorerPath))
                    {
                        foreach (var file in Directory.GetFiles(explorerPath, "thumbcache_*.db"))
                        {
                            try
                            {
                                var fileInfo = new FileInfo(file);
                                step.SpaceFreed += fileInfo.Length;
                                File.Delete(file);
                                step.FilesDeleted++;
                            }
                            catch { }
                        }
                    }
                }
                else if (step.Name.Contains("WER"))
                    CleanWER(step);
                else if (step.Name.Contains("icones"))
                    CleanIconCache(step);
                else if (step.Name.Contains("Crash Dumps"))
                    CleanCrashDumps(step);
            }, cancellationToken);
        }

        private void CleanDirectory(string path, CleaningStep step, bool recursive = true)
        {
            if (!Directory.Exists(path))
                return;

            try
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        step.SpaceFreed += fileInfo.Length;
                        File.Delete(file);
                        step.FilesDeleted++;
                    }
                    catch { }
                }

                if (recursive)
                {
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                            step.FilesDeleted++;
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void CleanWER(CleaningStep step)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            CleanDirectory(Path.Combine(local, @"Microsoft\Windows\WER\ReportQueue"), step);
            CleanDirectory(Path.Combine(local, @"Microsoft\Windows\WER\ReportArchive"), step);
            CleanDirectory(@"C:\ProgramData\Microsoft\Windows\WER\ReportQueue", step);
            CleanDirectory(@"C:\ProgramData\Microsoft\Windows\WER\ReportArchive", step);
        }

        private void CleanIconCache(CleaningStep step)
        {
            var explorerPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\Windows\Explorer");
            if (!Directory.Exists(explorerPath)) return;
            foreach (var file in Directory.GetFiles(explorerPath, "iconcache*.db"))
            {
                try
                {
                    var fi = new FileInfo(file);
                    step.SpaceFreed += fi.Length;
                    File.Delete(file);
                    step.FilesDeleted++;
                }
                catch { }
            }
        }

        private void CleanCrashDumps(CleaningStep step)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            CleanDirectory(Path.Combine(local, "CrashDumps"), step);
            CleanDirectory(@"C:\Windows\Minidump", step, false);
            CleanDirectory(Path.Combine(local, @"Microsoft\Windows\WER\ReportQueue"), step);
            CleanDirectory(Path.Combine(local, @"Microsoft\Windows\WER\ReportArchive"), step);
        }
    }
}
