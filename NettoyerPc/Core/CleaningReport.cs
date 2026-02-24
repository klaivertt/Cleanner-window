using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32;

namespace NettoyerPc.Core
{
    public class CleaningReport
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime   { get; set; }
        public TimeSpan TotalDuration => EndTime - StartTime;

        public int  TotalFilesDeleted { get; set; }
        public long TotalSpaceFreed   { get; set; }
        public int  ThreatsFound      { get; set; }

        public List<CleaningStep> Steps     { get; set; } = new();
        public string MachineName           { get; set; } = Environment.MachineName;
        public string UserName              { get; set; } = Environment.UserName;
        public bool   RebootRequired        { get; set; }
        public string OSVersion             { get; set; } = GetOSVersion();

        public int SuccessfulSteps => Steps.Count(s => s.IsCompleted && !s.HasError);
        public int FailedSteps     => Steps.Count(s => s.HasError);

        public List<string> DeletedFilePaths { get; set; } = new();
        public List<string> SkippedSteps     { get; set; } = new();

        // ── Nouvelles propriétés ─────────────────────────────────────────────────
        /// <summary>Benchmark disque mesuré AVANT le nettoyage.</summary>
        public DiskBenchmark?    BenchmarkBefore  { get; set; }
        /// <summary>Benchmark disque mesuré APRÈS le nettoyage.</summary>
        public DiskBenchmark?    BenchmarkAfter   { get; set; }
        /// <summary>Score de performance calculé à la fin du nettoyage.</summary>
        public PerformanceScore? Score            { get; set; }
        /// <summary>Mode de nettoyage exécuté.</summary>
        public string CleaningMode               { get; set; } = "";

        /// <summary>
        /// Génère et sauvegarde le rapport complet au format JSON.
        /// Le fichier est nommé CleanerReport_yyyy-MM-dd_HH-mm-ss.json.
        /// </summary>
        public string SaveReportJson(string directory)
        {
            Directory.CreateDirectory(directory);
            var fileName = $"CleanerReport_{StartTime:yyyy-MM-dd_HH-mm-ss}.json";
            var fullPath = Path.Combine(directory, fileName);
            File.WriteAllText(fullPath, ToJsonString());
            return fullPath;
        }

        /// <summary>Sérialise le rapport complet en JSON indenté.</summary>
        public string ToJsonString()
        {
            // ── Infos sysème ─────────────────────────────────────────────────────
            var sysInfo = GetSystemInfo();

            // ── Benchmark ────────────────────────────────────────────────────────
            object? benchBefore = BenchmarkBefore == null ? null : new
            {
                success      = BenchmarkBefore.Success,
                readMBs      = BenchmarkBefore.ReadSpeedMBs,
                writeMBs     = BenchmarkBefore.WriteSpeedMBs,
                measuredAt   = BenchmarkBefore.MeasuredAt,
                error        = BenchmarkBefore.ErrorMessage
            };
            object? benchAfter = BenchmarkAfter == null ? null : new
            {
                success      = BenchmarkAfter.Success,
                readMBs      = BenchmarkAfter.ReadSpeedMBs,
                writeMBs     = BenchmarkAfter.WriteSpeedMBs,
                measuredAt   = BenchmarkAfter.MeasuredAt,
                error        = BenchmarkAfter.ErrorMessage
            };
            object? benchDelta = (BenchmarkBefore?.Success == true && BenchmarkAfter?.Success == true) ? new
            {
                readDeltaMBs   = Math.Round(BenchmarkAfter!.ReadSpeedMBs  - BenchmarkBefore!.ReadSpeedMBs,  1),
                writeDeltaMBs  = Math.Round(BenchmarkAfter!.WriteSpeedMBs - BenchmarkBefore!.WriteSpeedMBs, 1),
                readDeltaPct   = Math.Round((BenchmarkAfter!.ReadSpeedMBs  - BenchmarkBefore!.ReadSpeedMBs)  / Math.Max(BenchmarkBefore!.ReadSpeedMBs,  1) * 100, 1),
                writeDeltaPct  = Math.Round((BenchmarkAfter!.WriteSpeedMBs - BenchmarkBefore!.WriteSpeedMBs) / Math.Max(BenchmarkBefore!.WriteSpeedMBs, 1) * 100, 1),
                summary        = Score?.BenchmarkDelta ?? "Non mesuré"
            } : null;

            // ── Score ─────────────────────────────────────────────────────────────
            object? scoreObj = Score == null ? null : new
            {
                value          = Score.Score,
                grade          = Score.Grade,
                message        = Score.Message,
                benchmarkDelta = Score.BenchmarkDelta,
                breakdown = new
                {
                    stepSuccess = Score.PtsStepSuccess,
                    spaceFreed  = Score.PtsSpaceFreed,
                    benchmark   = Score.PtsBenchmark,
                    files       = Score.PtsFiles
                }
            };

            // ── Résumé par catégorie ──────────────────────────────────────────────
            var byCategory = Steps
                .GroupBy(s => s.Category ?? "general")
                .OrderByDescending(g => g.Sum(s => s.SpaceFreed))
                .Select(g => new
                {
                    category              = g.Key,
                    stepsTotal            = g.Count(),
                    stepsSuccess          = g.Count(s => !s.HasError),
                    stepsFailed           = g.Count(s => s.HasError),
                    totalFilesDeleted     = g.Sum(s => s.FilesDeleted),
                    totalSpaceFreedBytes  = g.Sum(s => s.SpaceFreed),
                    totalSpaceFormatted   = FormatBytes(g.Sum(s => s.SpaceFreed)),
                    totalDurationSeconds  = Math.Round(g.Sum(s => s.Duration.TotalSeconds), 2)
                }).ToList();

            // ── Étapes détaillées ─────────────────────────────────────────────────
            var steps = Steps.Select(s =>
            {
                List<string> logs;
                lock (s.Logs) logs = s.Logs.ToList();
                return new
                {
                    name                = s.Name,
                    category            = s.Category ?? "general",
                    status              = s.Status,
                    durationSeconds     = Math.Round(s.Duration.TotalSeconds, 2),
                    filesDeleted        = s.FilesDeleted,
                    spaceFreedBytes     = s.SpaceFreed,
                    spaceFreedFormatted = FormatBytes(s.SpaceFreed),
                    hasError            = s.HasError,
                    errorMessage        = s.ErrorMessage ?? "",
                    logs                = logs
                };
            }).ToList();

            var dto = new
            {
                reportVersion = "4.0",

                metadata = new
                {
                    generatedAt   = DateTime.Now,
                    appName       = AppConstants.AppName,
                    appVersion    = AppConstants.AppVersion,
                    cleaningMode  = CleaningMode
                },

                system = sysInfo,

                execution = new
                {
                    startTime          = StartTime,
                    endTime            = EndTime,
                    durationSeconds    = Math.Round(TotalDuration.TotalSeconds, 1),
                    durationFormatted  = $"{(int)TotalDuration.TotalHours}h {TotalDuration.Minutes:00}m {TotalDuration.Seconds:00}s"
                },

                summary = new
                {
                    totalFilesDeleted        = TotalFilesDeleted,
                    totalSpaceFreedBytes     = TotalSpaceFreed,
                    totalSpaceFreedFormatted = FormatBytes(TotalSpaceFreed),
                    threatsFound             = ThreatsFound,
                    executedSteps            = Steps.Count,
                    successfulSteps          = SuccessfulSteps,
                    failedSteps              = FailedSteps,
                    skippedStepsCount        = SkippedSteps.Count,
                    rebootRequired           = RebootRequired
                },

                performanceScore = scoreObj,

                diskBenchmark = new
                {
                    before = benchBefore,
                    after  = benchAfter,
                    delta  = benchDelta
                },

                byCategory   = byCategory,
                steps        = steps,
                skippedSteps = SkippedSteps,
                deletedPaths = DeletedFilePaths.Take(1000).ToList()
            };

            return JsonSerializer.Serialize(dto, new JsonSerializerOptions
            {
                WriteIndented          = true,
                PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }

        // ─────────────────────────────────────────────────────────────────────────

        private static object GetSystemInfo()
        {
            // Espace disque C:
            long diskFree = 0, diskTotal = 0;
            try { var di = new DriveInfo("C"); diskFree = di.AvailableFreeSpace; diskTotal = di.TotalSize; }
            catch { }

            return new
            {
                machineName    = Environment.MachineName,
                userName       = Environment.UserName,
                osVersion      = GetOSVersion(),
                processorCount = Environment.ProcessorCount,
                is64Bit        = Environment.Is64BitOperatingSystem,
                diskC = new
                {
                    totalBytes     = diskTotal,
                    totalFormatted = FormatBytes(diskTotal),
                    freeBytes      = diskFree,
                    freeFormatted  = FormatBytes(diskFree),
                    usedPct        = diskTotal > 0 ? Math.Round((double)(diskTotal - diskFree) / diskTotal * 100, 1) : 0.0
                }
            };
        }

        private static string GetOSVersion()
        {
            try
            {
                var reg = Microsoft.Win32.Registry.LocalMachine
                    .OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                var productName = reg?.GetValue("ProductName") ?? "Windows";
                var currentVersion = reg?.GetValue("CurrentVersion") ?? "";
                var build = reg?.GetValue("CurrentBuildNumber") ?? "";
                return $"{productName} (build {build})";
            }
            catch
            {
                return Environment.OSVersion.VersionString;
            }
        }

        public static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
            return $"{len:0.##} {sizes[order]}";
        }

    }
}
