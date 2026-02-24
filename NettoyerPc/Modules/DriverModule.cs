using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NettoyerPc.Core;

namespace NettoyerPc.Modules
{
    /// <summary>
    /// MODULE PILOTES — Nettoyage caches GPU (NVIDIA / AMD / Intel), logs pilotes,
    /// et suppression des packages obsolètes du Driver Store.
    ///
    /// RÈGLE DE SÉCURITÉ : uniquement les caches de shaders / logs / crash reports sont
    /// supprimés — jamais les drivers actifs, données utilisateur ou paramètres.
    /// Chaque cache se régénère automatiquement au prochain lancement du jeu.
    /// </summary>
    public class DriverModule : ICleaningModule
    {
        public string Name => "Pilotes (Drivers)";

        public List<CleaningStep> GetSteps(CleaningMode mode)
        {
            return new List<CleaningStep>
            {
                // ── Shader caches GPU ────────────────────────────────────────────────
                new() { Name = "Cache shaders DirectX partagé (D3DSCache)",      Category = "drivers" },
                new() { Name = "Cache shaders NVIDIA (DXCache, GLCache, Vulkan)", Category = "drivers" },
                new() { Name = "Cache shaders AMD (DxCache, GLCache, VkCache)",   Category = "drivers" },
                new() { Name = "Cache shaders Intel (ShaderCache)",               Category = "drivers" },
                // ── Logs & crash reports ─────────────────────────────────────────────
                new() { Name = "Logs NVIDIA — GeForce Experience & Container",    Category = "drivers" },
                new() { Name = "Logs AMD — Radeon Software & Adrenalin",          Category = "drivers" },
                new() { Name = "Logs d'installation de pilotes (SetupAPI)",       Category = "drivers" },
                // ── Driver Store ─────────────────────────────────────────────────────
                new() { Name = "Supprimer les packages pilotes obsolètes (Driver Store)", Category = "drivers" },
                // ── Rapport ──────────────────────────────────────────────────────────
                new() { Name = "Rapport matériel et pilotes installés",           Category = "drivers" },
            };
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if      (step.Name.Contains("D3DSCache"))          CleanD3DSCache(step);
                else if (step.Name.Contains("NVIDIA") && step.Name.Contains("shader")) CleanNvidiaShaderCache(step);
                else if (step.Name.Contains("AMD")   && step.Name.Contains("shader"))  CleanAmdShaderCache(step);
                else if (step.Name.Contains("Intel") && step.Name.Contains("shader"))  CleanIntelShaderCache(step);
                else if (step.Name.Contains("Logs NVIDIA"))        CleanNvidiaLogs(step);
                else if (step.Name.Contains("Logs AMD"))           CleanAmdLogs(step);
                else if (step.Name.Contains("SetupAPI"))           CleanSetupApiLogs(step);
                else if (step.Name.Contains("Driver Store"))       CleanDriverStore(step, cancellationToken);
                else if (step.Name.Contains("Rapport matériel"))   BuildHardwareReport(step);
            }, cancellationToken);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 1. Cache DirectX / Vulkan partagé — %LocalAppData%\D3DSCache
        // 100 % sûr. Regeneré automatiquement.
        // ═══════════════════════════════════════════════════════════════════════════════
        private void CleanD3DSCache(CleaningStep step)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDir(Path.Combine(local, "D3DSCache"),        step);
            DeleteDir(Path.Combine(local, "VkPipelineCache"),  step);
            step.Status = step.SpaceFreed > 0
                ? $"Cache DirectX/Vulkan partagé supprimé ({FormatBytes(step.SpaceFreed)})"
                : "Cache DirectX déjà vide";
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 2. Cache shaders NVIDIA — %LocalAppData%\NVIDIA\{DXCache,GLCache,VkCache,...}
        // Regeneré en quelques secondes au prochain lancement d'un jeu.
        // JAMAIS de touche aux drivers ou aux paramètres GeForce.
        // ═══════════════════════════════════════════════════════════════════════════════
        private void CleanNvidiaShaderCache(CleaningStep step)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            foreach (var sub in new[]
            {
                @"NVIDIA\DXCache",
                @"NVIDIA\GLCache",
                @"NVIDIA\VkCache",
                @"NVIDIA\OptixCache",
                @"NVIDIA Corporation\NV_Cache",
            })
                DeleteDir(Path.Combine(local, sub), step);

            step.Status = step.SpaceFreed > 0
                ? $"Cache shaders NVIDIA supprimé ({FormatBytes(step.SpaceFreed)})"
                : "Aucun cache shader NVIDIA (GPU absent ou déjà vide)";
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 3. Cache shaders AMD — %LocalAppData%\AMD\{DxCache,GLCache,VkCache,...}
        // ═══════════════════════════════════════════════════════════════════════════════
        private void CleanAmdShaderCache(CleaningStep step)
        {
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            foreach (var p in new[]
            {
                Path.Combine(local,   @"AMD\DxCache"),
                Path.Combine(local,   @"AMD\GLCache"),
                Path.Combine(local,   @"AMD\VkCache"),
                Path.Combine(local,   @"AMD\VulkanShaderCache"),
                Path.Combine(local,   @"AMD\ShaderCache"),
                Path.Combine(appData, @"AMD\CN\ShaderCache"),
            })
                DeleteDir(p, step);

            step.Status = step.SpaceFreed > 0
                ? $"Cache shaders AMD supprimé ({FormatBytes(step.SpaceFreed)})"
                : "Aucun cache shader AMD (GPU absent ou déjà vide)";
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 4. Cache shaders Intel Arc / UHD — %LocalAppData%\Intel\ShaderCache
        // ═══════════════════════════════════════════════════════════════════════════════
        private void CleanIntelShaderCache(CleaningStep step)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            foreach (var sub in new[] { @"Intel\ShaderCache", @"Intel\GFX-Cache", @"Intel\DXCache" })
                DeleteDir(Path.Combine(local, sub), step);

            step.Status = step.SpaceFreed > 0
                ? $"Cache shaders Intel supprimé ({FormatBytes(step.SpaceFreed)})"
                : "Aucun cache shader Intel (GPU absent ou déjà vide)";
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 5. Logs NVIDIA — GeForce Experience, NV Container
        // Aucun paramètre ou donnée de profil touché.
        // ═══════════════════════════════════════════════════════════════════════════════
        private void CleanNvidiaLogs(CleaningStep step)
        {
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var temp    = Path.GetTempPath();

            DeleteFilesInDir(Path.Combine(local, @"NVIDIA Corporation\NvBackend"),   "*.log", step);
            DeleteFilesInDir(Path.Combine(local, @"NVIDIA Corporation\NvContainer"), "*.log", step);
            DeleteDir       (Path.Combine(local, @"NVIDIA Corporation\NvContainer\CrashReports"), step);
            DeleteFilesInDir(Path.Combine(local, @"NVIDIA\NvBackend"), "*.log", step);
            DeleteFilesInDir(temp, "nvtelem*.log",          step);
            DeleteFilesInDir(temp, "NVIDIA_GeForce*.log",   step);

            step.Status = step.FilesDeleted > 0
                ? $"{step.FilesDeleted} log(s) NVIDIA supprimé(s)"
                : "Logs NVIDIA déjà propres";
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 6. Logs AMD — Radeon Software Adrenalin
        // ═══════════════════════════════════════════════════════════════════════════════
        private void CleanAmdLogs(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            DeleteFilesInDir(Path.Combine(appData, @"AMD\CN"),     "*.log", step);
            DeleteDir       (Path.Combine(appData, @"AMD\CN\CrashDumps"),   step);
            DeleteFilesInDir(Path.Combine(local,   @"AMD\CN"),     "*.log", step);
            DeleteFilesInDir(Path.Combine(appData, @"AMD\ReLive"), "*.log", step);
            DeleteFilesInDir(Path.Combine(appData, "AMD"),         "*.log", step);

            step.Status = step.FilesDeleted > 0
                ? $"{step.FilesDeleted} log(s) AMD supprimé(s)"
                : "Logs AMD déjà propres";
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 7. Logs SetupAPI — journaux d'installation des pilotes dans %windir%\inf
        // ═══════════════════════════════════════════════════════════════════════════════
        private void CleanSetupApiLogs(CleaningStep step)
        {
            var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var infDir = Path.Combine(winDir, "inf");

            foreach (var name in new[] { "setupapi.dev.log", "setupapi.app.log", "setupapi.setup.log" })
            {
                TruncateLogIfBig(Path.Combine(infDir, name), step);
                TruncateLogIfBig(Path.Combine(winDir, name), step);
            }

            if (Directory.Exists(infDir))
                foreach (var f in Directory.GetFiles(infDir, "oem*.log"))
                    SafeDeleteFile(f, step);

            step.Status = step.FilesDeleted > 0
                ? $"{step.FilesDeleted} fichier(s) log nettoyé(s) ({FormatBytes(step.SpaceFreed)})"
                : "Logs d'installation déjà propres";
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 8. Driver Store — supprimer uniquement les packages en double / obsolètes.
        // La version la plus récente de chaque pilote est TOUJOURS conservée.
        // ═══════════════════════════════════════════════════════════════════════════════
        private void CleanDriverStore(CleaningStep step, CancellationToken token)
        {
            try
            {
                var output = RunProcess("pnputil.exe", "/enum-drivers", 30);
                if (string.IsNullOrEmpty(output))
                {
                    step.Status = "pnputil : aucun résultat (droits admin requis)";
                    return;
                }

                var drivers = ParsePnputil(output);
                var groups  = new Dictionary<string, List<DriverEntry>>(StringComparer.OrdinalIgnoreCase);
                foreach (var d in drivers)
                {
                    var key = $"{d.ClassName}|{d.DriverDescription}";
                    if (!groups.TryGetValue(key, out var g)) groups[key] = g = new();
                    g.Add(d);
                }

                int removed = 0;
                foreach (var (_, list) in groups)
                {
                    if (list.Count <= 1) continue;
                    list.Sort((a, b) => string.Compare(b.DriverVersion, a.DriverVersion, StringComparison.Ordinal));
                    for (int i = 1; i < list.Count; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        // /force supprime le package du store sans désinstaller le driver actif
                        var r = RunProcess("pnputil.exe", $"/delete-driver {list[i].InfName} /force", 30);
                        if (r != null && r.Contains("successfully", StringComparison.OrdinalIgnoreCase))
                        {
                            removed++;
                            step.FilesDeleted++;
                            step.AddLog($"Supprimé : {list[i].InfName} — {list[i].DriverDescription} v{list[i].DriverVersion}");
                        }
                    }
                }

                step.Status = removed > 0
                    ? $"{removed} package(s) obsolète(s) supprimé(s) du Driver Store"
                    : "Aucun pilote en double — Driver Store propre";
            }
            catch (Exception ex) { step.Status = $"Erreur : {ex.Message}"; }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 9. Rapport matériel — GPU / CPU / RAM + liens de mise à jour
        // ═══════════════════════════════════════════════════════════════════════════════
        private void BuildHardwareReport(CleaningStep step)
        {
            try
            {
                var gpuRaw = RunProcess("wmic", "path win32_VideoController get Name,DriverVersion /value", 15) ?? "";
                var cpuRaw = RunProcess("wmic", "cpu get Name,MaxClockSpeed /value", 15) ?? "";
                var ramRaw = RunProcess("wmic", "memorychip get Capacity,Speed /value", 10) ?? "";

                var sb = new StringBuilder();
                sb.AppendLine("══════════════════ RAPPORT MATÉRIEL ══════════════════");

                sb.AppendLine(); sb.AppendLine("GPU(s) :");
                foreach (var blk in gpuRaw.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var name = ExtractWmicField(blk, "Name");
                    var dver = ExtractWmicField(blk, "DriverVersion");
                    if (!string.IsNullOrWhiteSpace(name))
                        sb.AppendLine($"  • {name}  (driver {dver})");
                }

                sb.AppendLine(); sb.AppendLine("CPU :");
                sb.AppendLine($"  • {ExtractWmicField(cpuRaw, "Name")}  ({ExtractWmicField(cpuRaw, "MaxClockSpeed")} MHz)");

                sb.AppendLine(); sb.AppendLine("RAM :");
                long totalRam = 0;
                foreach (var blk in ramRaw.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (long.TryParse(ExtractWmicField(blk, "Capacity"), out var cap))
                    {
                        totalRam += cap;
                        sb.AppendLine($"  • {cap / (1024L * 1024 * 1024)} GB  @ {ExtractWmicField(blk, "Speed")} MHz");
                    }
                }
                if (totalRam > 0) sb.AppendLine($"  Total : {totalRam / (1024L * 1024 * 1024)} GB");

                sb.AppendLine(); sb.AppendLine("══════════ LIENS MISE À JOUR DRIVERS ═════════════");
                sb.AppendLine("  NVIDIA   → GeForce Experience  /  nvidia.com/drivers");
                sb.AppendLine("  AMD      → AMD Software Adrenalin  /  amd.com/support");
                sb.AppendLine("  Intel    → Intel Driver & Support Assistant (iDSA)");
                sb.AppendLine("  Chipset  → Windows Update > Mises à jour facultatives");

                step.Status = sb.ToString().TrimEnd();
                // Emit rapport lines to activity log
                foreach (var rapportLine in sb.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    step.AddLog(rapportLine);
            }
            catch (Exception ex) { step.Status = $"Rapport partiel ({ex.Message})"; }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────────────

        private record DriverEntry(string InfName, string ClassName, string DriverDescription, string DriverVersion);

        private static List<DriverEntry> ParsePnputil(string output)
        {
            var result = new List<DriverEntry>();
            foreach (var block in output.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var inf = ExtractField(block, "Published Name");
                if (!string.IsNullOrEmpty(inf) && inf.StartsWith("oem", StringComparison.OrdinalIgnoreCase))
                    result.Add(new DriverEntry(inf,
                        ExtractField(block, "Class"),
                        ExtractField(block, "Driver Description"),
                        ExtractField(block, "Driver Version")));
            }
            return result;
        }

        private static string ExtractField(string block, string field)
        {
            var m = Regex.Match(block, $@"(?i){Regex.Escape(field)}\s*:\s*(.+)");
            return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
        }

        private static string ExtractWmicField(string block, string field)
        {
            var m = Regex.Match(block, $@"(?i){Regex.Escape(field)}=(.*)");
            return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
        }

        private void DeleteDir(string path, CleaningStep step)
        {
            if (!Directory.Exists(path)) return;
            try
            {
                var info = new DirectoryInfo(path);
                int fileCount = 0;
                foreach (var f in info.GetFiles("*", SearchOption.AllDirectories))
                {
                    try { step.SpaceFreed += f.Length; fileCount++; } catch { }
                }
                Directory.Delete(path, true);
                step.FilesDeleted += Math.Max(1, fileCount);
                step.AddLog($"Supprimé : {path} ({fileCount} fichier(s))");
            }
            catch { }
        }

        private void DeleteFilesInDir(string dir, string pattern, CleaningStep step)
        {
            if (!Directory.Exists(dir)) return;
            try { foreach (var f in Directory.GetFiles(dir, pattern)) SafeDeleteFile(f, step); }
            catch { }
        }

        private static void SafeDeleteFile(string path, CleaningStep step)
        {
            try
            {
                var fi = new FileInfo(path);
                step.SpaceFreed += fi.Length;
                File.Delete(path);
                step.FilesDeleted++;
                step.AddLog($"Supprimé : {Path.GetFileName(path)}");
            }
            catch { }
        }

        private static void TruncateLogIfBig(string path, CleaningStep step, long threshMiB = 5)
        {
            if (!File.Exists(path)) return;
            try
            {
                var fi = new FileInfo(path);
                if (fi.Length < threshMiB * 1024 * 1024) return;
                step.SpaceFreed += fi.Length;
                File.WriteAllText(path, $"[Tronqué par PC Clean — {DateTime.Now:yyyy-MM-dd HH:mm}]\n");
                step.FilesDeleted++;
            }
            catch { }
        }

        private static long GetDirSize(DirectoryInfo dir)
        {
            long size = 0;
            try { foreach (var f in dir.GetFiles("*", SearchOption.AllDirectories)) try { size += f.Length; } catch { } }
            catch { }
            return size;
        }

        private static string FormatBytes(long bytes)
        {
            string[] u = { "B", "KB", "MB", "GB" }; double v = bytes; int i = 0;
            while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
            return $"{v:0.##} {u[i]}";
        }

        private static string? RunProcess(string exe, string args, int timeoutSec = 60)
        {
            try
            {
                using var proc = new Process { StartInfo = new ProcessStartInfo
                {
                    FileName = exe, Arguments = args, UseShellExecute = false,
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    CreateNoWindow = true, StandardOutputEncoding = Encoding.UTF8,
                }};
                proc.Start();
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(timeoutSec * 1000);
                return output;
            }
            catch { return null; }
        }
    }
}

