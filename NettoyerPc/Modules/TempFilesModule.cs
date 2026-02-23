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
                new() { Name = "Suppression Thumbnails",         Category = "general" }
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
    }
}
