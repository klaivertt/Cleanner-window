using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

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

        public string GenerateReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("==================================================================================");
            sb.AppendLine("                          RAPPORT DE NETTOYAGE WINDOWS");
            sb.AppendLine("==================================================================================");
            sb.AppendLine($"Date: {StartTime:dd/MM/yyyy}");
            sb.AppendLine($"Heure début: {StartTime:HH:mm:ss}");
            sb.AppendLine($"Heure fin: {EndTime:HH:mm:ss}");
            sb.AppendLine($"Utilisateur: {UserName}");
            sb.AppendLine($"Ordinateur: {MachineName}");
            sb.AppendLine("==================================================================================");
            sb.AppendLine();
            
            sb.AppendLine("RÉSUMÉ:");
            sb.AppendLine($"  • Fichiers supprimés: {TotalFilesDeleted}");
            sb.AppendLine($"  • Espace libéré: {FormatBytes(TotalSpaceFreed)}");
            sb.AppendLine($"  • Menaces détectées: {ThreatsFound}");
            sb.AppendLine($"  • Durée totale: {TotalDuration.Hours}h {TotalDuration.Minutes}m {TotalDuration.Seconds}s");
            sb.AppendLine($"  • Redémarrage requis: {(RebootRequired ? "OUI" : "NON")}");
            sb.AppendLine();
            
            sb.AppendLine("DÉTAILS DES ÉTAPES:");
            sb.AppendLine("==================================================================================");
            
            foreach (var step in Steps)
            {
                sb.AppendLine();
                sb.AppendLine($"• {step.Name}");
                sb.AppendLine($"  Statut: {step.Status}");
                sb.AppendLine($"  Durée: {step.Duration.Minutes}m {step.Duration.Seconds}s");
                sb.AppendLine($"  Fichiers: {step.FilesDeleted}");
                sb.AppendLine($"  Espace: {FormatBytes(step.SpaceFreed)}");
                if (step.HasError)
                {
                    sb.AppendLine($"  Erreur: {step.ErrorMessage}");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("==================================================================================");
            sb.AppendLine("                              FIN DU RAPPORT");
            sb.AppendLine("==================================================================================");
            
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
                version      = "2.0",
                startTime    = StartTime,
                endTime      = EndTime,
                durationSeconds = TotalDuration.TotalSeconds,
                machineName  = MachineName,
                userName     = UserName,
                totalFilesDeleted = TotalFilesDeleted,
                totalSpaceFreed   = TotalSpaceFreed,
                threatsFound = ThreatsFound,
                rebootRequired = RebootRequired,
                steps = Steps.Select(s => new
                {
                    name            = s.Name,
                    category        = s.Category,
                    status          = s.Status,
                    durationSeconds = s.Duration.TotalSeconds,
                    filesDeleted    = s.FilesDeleted,
                    spaceFreed      = s.SpaceFreed,
                    hasError        = s.HasError,
                    errorMessage    = s.ErrorMessage
                }).ToList()
            };
            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            var fileName = $"CleanerReport_{StartTime:yyyy-MM-dd_HH-mm-ss}.json";
            File.WriteAllText(Path.Combine(directory, fileName), json);
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
