using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace NettoyerPc.Core
{
    public class CleaningReport
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration => EndTime - StartTime;
        public int TotalFilesDeleted { get; set; }
        public long TotalSpaceFreed { get; set; }
        public int ThreatsFound { get; set; }
        public List<CleaningStep> Steps { get; set; } = new();
        public string MachineName { get; set; } = Environment.MachineName;
        public string UserName { get; set; } = Environment.UserName;
        public bool RebootRequired { get; set; }
        public string OSVersion { get; set; } = GetOSVersion();
        public int SuccessfulSteps => Steps.Count(s => s.IsCompleted && !s.HasError);
        public int FailedSteps => Steps.Count(s => s.HasError);
        public List<string> DeletedFilePaths { get; set; } = new();
        public List<string> SkippedSteps { get; set; } = new();

        public string GenerateReport()
        {
            var L = Localizer.T;
            var sb = new StringBuilder();
            sb.AppendLine("╔═══════════════════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║   {(AppConstants.AppName + " v" + AppConstants.AppVersion).PadRight(79)}║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();

            sb.AppendLine($"【 {L("report.txt.sysinfo")} 】");
            sb.AppendLine($"  {L("report.txt.date")}    : {StartTime:dd/MM/yyyy}");
            sb.AppendLine($"  {L("report.txt.timestart")}: {StartTime:HH:mm:ss}");
            sb.AppendLine($"  {L("report.txt.timeend")}  : {EndTime:HH:mm:ss}");
            sb.AppendLine($"  {L("report.txt.duration")} : {TotalDuration.Hours}h {TotalDuration.Minutes}m {TotalDuration.Seconds}s");
            sb.AppendLine($"  {L("report.txt.user")}     : {UserName}");
            sb.AppendLine($"  {L("report.txt.machine")}  : {MachineName}");
            sb.AppendLine($"  {L("report.txt.os")}       : {OSVersion}");
            sb.AppendLine();

            sb.AppendLine($"【 {L("report.txt.summary")} 】");
            sb.AppendLine($"  ✓  {L("report.txt.files")}      : {TotalFilesDeleted}");
            sb.AppendLine($"  💾 {L("report.txt.space")}      : {FormatBytes(TotalSpaceFreed)}");
            sb.AppendLine($"  ⚠️  {L("report.txt.threats")}     : {ThreatsFound}");
            sb.AppendLine($"  ✅ {L("report.txt.steps.ok")}    : {SuccessfulSteps}/{Steps.Count}");
            sb.AppendLine($"  ❌ {L("report.txt.steps.failed")}: {FailedSteps}/{Steps.Count}");
            sb.AppendLine($"  ⏭️  {L("report.txt.skipped.label")}: {SkippedSteps.Count}");
            sb.AppendLine($"  🔄 {L("report.txt.reboot.label")}: {(RebootRequired ? L("report.txt.reboot.yes.short") : L("report.txt.reboot.no.short"))}");
            sb.AppendLine();

            if (SkippedSteps.Count > 0)
            {
                sb.AppendLine($"【 {L("report.txt.skipped")} 】");
                foreach (var s in SkippedSteps)
                    sb.AppendLine($"  ⏭️  {s}");
                sb.AppendLine();
            }

            sb.AppendLine($"【 {L("report.txt.stepdetails")} 】");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            
            foreach (var step in Steps)
            {
                var statusSymbol = step.HasError ? "✗" : "✓";
                sb.AppendLine();
                sb.AppendLine($"  {statusSymbol} [{step.Category.ToUpper()}] {step.Name}");
                sb.AppendLine($"      {L("report.txt.status")}     : {step.Status}");
                sb.AppendLine($"      {L("report.txt.files")}      : {step.FilesDeleted}");
                sb.AppendLine($"      {L("report.txt.space")}      : {FormatBytes(step.SpaceFreed)}");
                sb.AppendLine($"      {L("report.txt.duration2")}  : {step.Duration.TotalSeconds:0.00}s");
                if (step.HasError)
                    sb.AppendLine($"      ⚠️  {L("report.txt.error")} : {step.ErrorMessage}");
            }
            
            sb.AppendLine();
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine();
            
            if (DeletedFilePaths.Count > 0)
            {
                sb.AppendLine($"【 {L("report.txt.paths")} 】");
                foreach (var path in DeletedFilePaths.Take(500))
                    sb.AppendLine($"    • {path}");
                if (DeletedFilePaths.Count > 500)
                    sb.AppendLine($"    ... {L("report.txt.andmore").Replace("{0}", (DeletedFilePaths.Count - 500).ToString())}");
                sb.AppendLine();
            }
            
            sb.AppendLine($"【 {L("report.txt.reco")} 】");
            if (RebootRequired)
                sb.AppendLine($"  ⚠️  {L("report.txt.reco.reboot")}");
            else
                sb.AppendLine($"  ✓  {L("report.txt.reco.noreboot")}");
            if (FailedSteps > 0)
                sb.AppendLine($"  ⚠️  {L("report.txt.reco.errors").Replace("{0}", FailedSteps.ToString())}");
            else
                sb.AppendLine($"  ✓  {L("report.txt.reco.ok")}");

            sb.AppendLine();
            sb.AppendLine("╔═══════════════════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║   {L("report.txt.footer").PadRight(79)}║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════════════════════════╝");
            
            return sb.ToString();
        }

        public void SaveReport(string directory)
        {
            var fileName = $"CleanerReport_{StartTime:yyyy-MM-dd_HH-mm-ss}.txt";
            File.WriteAllText(Path.Combine(directory, fileName), GenerateReport());
        }

        public void SaveReportJson(string directory)
        {
            // Grouper les étapes par catégorie pour le rapport
            var byCategory = Steps
                .GroupBy(s => s.Category)
                .Select(g => new
                {
                    category            = g.Key,
                    stepsCount          = g.Count(),
                    successCount        = g.Count(s => !s.HasError),
                    totalFilesDeleted   = g.Sum(s => s.FilesDeleted),
                    totalSpaceFreedBytes = g.Sum(s => s.SpaceFreed),
                    totalSpaceFormatted = FormatBytes(g.Sum(s => s.SpaceFreed)),
                    durationSeconds     = g.Sum(s => s.Duration.TotalSeconds)
                }).ToList();

            var dto = new
            {
                reportVersion    = "3.0",
                metadata = new
                {
                    generatedAt     = DateTime.Now,
                    appName         = AppConstants.AppName,
                    appVersion      = AppConstants.AppVersion,
                    osVersion       = OSVersion,
                    machineName     = MachineName,
                    userName        = UserName
                },
                execution = new
                {
                    startTime           = StartTime,
                    endTime             = EndTime,
                    durationSeconds     = TotalDuration.TotalSeconds,
                    durationFormatted   = $"{TotalDuration.Hours}h {TotalDuration.Minutes}m {TotalDuration.Seconds}s"
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
                    skippedSteps             = SkippedSteps.Count,
                    rebootRequired           = RebootRequired
                },
                byCategory = byCategory,
                steps = Steps.Select(s => new
                {
                    name                = s.Name,
                    category            = s.Category,
                    status              = s.Status,
                    durationSeconds     = Math.Round(s.Duration.TotalSeconds, 2),
                    filesDeleted        = s.FilesDeleted,
                    spaceFreedBytes     = s.SpaceFreed,
                    spaceFreedFormatted = FormatBytes(s.SpaceFreed),
                    hasError            = s.HasError,
                    errorMessage        = s.ErrorMessage ?? ""
                }).ToList(),
                skippedSteps = SkippedSteps,
                deletedPaths = DeletedFilePaths.Take(500).ToList()
            };

            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            var fileName = $"CleanerReport_{StartTime:yyyy-MM-dd_HH-mm-ss}.json";
            File.WriteAllText(Path.Combine(directory, fileName), json);
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
