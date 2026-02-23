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
        public int SuccessfulSteps => Steps.Count(s => s.Status == "Réussi");
        public int FailedSteps => Steps.Count(s => s.HasError);
        public List<string> DeletedFilePaths { get; set; } = new();

        public string GenerateReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("╔═══════════════════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║                                                                                   ║");
            sb.AppendLine("║                       🧹 RAPPORT DÉTAILLÉ DE NETTOYAGE 🧹                        ║");
            sb.AppendLine($"║                          {AppConstants.AppName} - v{AppConstants.AppVersion}                              ║");
            sb.AppendLine("║                                                                                   ║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            
            sb.AppendLine("【 INFORMATIONS SYSTÈME 】");
            sb.AppendLine($"  📅 Date nettoyage   : {StartTime:dd/MM/yyyy}");
            sb.AppendLine($"  🕐 Heure début      : {StartTime:HH:mm:ss}");
            sb.AppendLine($"  🕑 Heure fin        : {EndTime:HH:mm:ss}");
            sb.AppendLine($"  ⏱️  Durée totale      : {TotalDuration.Hours}h {TotalDuration.Minutes}m {TotalDuration.Seconds}s");
            sb.AppendLine($"  👤 Utilisateur      : {UserName}");
            sb.AppendLine($"  💻 Ordinateur       : {MachineName}");
            sb.AppendLine($"  🖥️  Système          : {OSVersion}");
            sb.AppendLine();
            
            sb.AppendLine("【 RÉSUMÉ DES RÉSULTATS 】");
            sb.AppendLine($"  ✓ Fichiers supprimés  : {TotalFilesDeleted} fichiers");
            sb.AppendLine($"  💾 Espace libéré      : {FormatBytes(TotalSpaceFreed)}");
            sb.AppendLine($"  ⚠️  Menaces détectées : {ThreatsFound}");
            sb.AppendLine($"  ✅ Étapes réussies   : {SuccessfulSteps}/{Steps.Count}");
            sb.AppendLine($"  ❌ Étapes échouées    : {FailedSteps}/{Steps.Count}");
            sb.AppendLine($"  🔄 Redémarrage       : {(RebootRequired ? "REQUIS ⚠️" : "Non requis")}");
            sb.AppendLine();
            
            sb.AppendLine("【 DÉTAILS PAR ÉTAPE 】");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            
            foreach (var step in Steps)
            {
                var statusSymbol = step.Status == "Réussi" ? "✓" : step.HasError ? "✗" : "⊘";
                sb.AppendLine();
                sb.AppendLine($"  {statusSymbol} [{step.Category.ToUpper()}] {step.Name}");
                sb.AppendLine($"      Statut          : {step.Status}");
                sb.AppendLine($"      Fichiers        : {step.FilesDeleted}");
                sb.AppendLine($"      Espace libéré   : {FormatBytes(step.SpaceFreed)}");
                sb.AppendLine($"      Durée           : {step.Duration.TotalSeconds:0.00}s");
                if (step.HasError)
                {
                    sb.AppendLine($"      ⚠️  Erreur         : {step.ErrorMessage}");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine();
            
            if (DeletedFilePaths.Count > 0 && DeletedFilePaths.Count <= 1000)
            {
                sb.AppendLine("【 FICHIERS/DOSSIERS SUPPRIMÉS 】");
                sb.AppendLine($"  Total : {DeletedFilePaths.Count} éléments");
                sb.AppendLine();
                foreach (var path in DeletedFilePaths.Take(500))
                {
                    sb.AppendLine($"    • {path}");
                }
                if (DeletedFilePaths.Count > 500)
                {
                    sb.AppendLine($"    ... et {DeletedFilePaths.Count - 500} autres fichiers");
                }
                sb.AppendLine();
            }
            
            sb.AppendLine("【 RECOMMANDATIONS 】");
            if (RebootRequired)
            {
                sb.AppendLine("  ⚠️  Un redémarrage est recommandé pour appliquer tous les changements.");
            }
            else
            {
                sb.AppendLine("  ✓ Aucun redémarrage requis. Les changements sont appliqués immédiatement.");
            }
            
            if (FailedSteps > 0)
            {
                sb.AppendLine($"  ⚠️  {FailedSteps} étape(s) ont échoué. Consulter les détails ci-dessus.");
            }
            else
            {
                sb.AppendLine("  ✓ Toutes les étapes se sont déroulées correctement.");
            }
            
            sb.AppendLine();
            sb.AppendLine("╔═══════════════════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║                        FIN DU RAPPORT DE NETTOYAGE                               ║");
            sb.AppendLine("║                         Merci d'avoir utilisé PC Clean                           ║");
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
            var dto = new
            {
                version          = "2.1",
                metadata = new
                {
                    timestamp       = DateTime.Now,
                    appName         = AppConstants.AppName,
                    appVersion      = AppConstants.AppVersion,
                    osVersion       = OSVersion,
                    machineName     = MachineName,
                    userName        = UserName
                },
                execution = new
                {
                    startTime       = StartTime,
                    endTime         = EndTime,
                    durationSeconds = TotalDuration.TotalSeconds,
                    durationFormatted = $"{TotalDuration.Hours}h {TotalDuration.Minutes}m {TotalDuration.Seconds}s"
                },
                results = new
                {
                    totalFilesDeleted   = TotalFilesDeleted,
                    totalSpaceFreedBytes = TotalSpaceFreed,
                    totalSpaceFreedFormatted = FormatBytes(TotalSpaceFreed),
                    threatsFound        = ThreatsFound,
                    successfulSteps     = SuccessfulSteps,
                    failedSteps         = FailedSteps,
                    totalSteps          = Steps.Count,
                    rebootRequired      = RebootRequired
                },
                steps = Steps.Select(s => new
                {
                    name            = s.Name,
                    category        = s.Category,
                    status          = s.Status,
                    durationSeconds = s.Duration.TotalSeconds,
                    filesDeleted    = s.FilesDeleted,
                    spaceFreedBytes = s.SpaceFreed,
                    spaceFreedFormatted = FormatBytes(s.SpaceFreed),
                    hasError        = s.HasError,
                    errorMessage    = s.ErrorMessage ?? ""
                }).ToList(),
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
