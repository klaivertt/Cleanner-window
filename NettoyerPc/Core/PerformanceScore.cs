using System;

namespace NettoyerPc.Core
{
    /// <summary>
    /// Calcule un score de performance global (0-100) avec grade et message,
    /// basé sur : taux de succès des étapes, espace libéré, fichiers supprimés,
    /// et l'amélioration mesurée par le benchmark disque avant/après.
    /// </summary>
    public class PerformanceScore
    {
        // ── Résultat ─────────────────────────────────────────────────────────────
        public int    Score          { get; }
        public string Grade          { get; }
        public string Message        { get; }
        public string BenchmarkDelta { get; }

        // ── Détail des composantes ────────────────────────────────────────────────
        public double PtsStepSuccess  { get; }   // 40 pts max
        public double PtsSpaceFreed   { get; }   // 30 pts max
        public double PtsBenchmark    { get; }   // 20 pts max
        public double PtsFiles        { get; }   // 10 pts max

        public PerformanceScore(
            CleaningReport report,
            DiskBenchmark?  before = null,
            DiskBenchmark?  after  = null)
        {
            // ── 1. Taux de succès des étapes (40 pts) ────────────────────────────
            double successRate = report.Steps.Count > 0
                ? (double)report.SuccessfulSteps / report.Steps.Count
                : 1.0;
            PtsStepSuccess = Math.Round(successRate * 40, 1);

            // ── 2. Espace libéré (30 pts) ────────────────────────────────────────
            // Barème : 0 B = 0 pt, 1 GB = 15 pts, 2 GB+ = 30 pts
            double gbFreed = report.TotalSpaceFreed / (1024.0 * 1024 * 1024);
            PtsSpaceFreed  = Math.Round(Math.Min(30, gbFreed * 15.0), 1);

            // ── 3. Amélioration benchmark (20 pts) ───────────────────────────────
            if (before != null && after != null && before.Success && after.Success)
            {
                double readDelta  = (after.ReadSpeedMBs  - before.ReadSpeedMBs)
                                     / Math.Max(before.ReadSpeedMBs,  1);
                double writeDelta = (after.WriteSpeedMBs - before.WriteSpeedMBs)
                                     / Math.Max(before.WriteSpeedMBs, 1);
                double avg = (readDelta + writeDelta) / 2;

                // +40 % amélioration → 20 pts max ; régression → 0 pt
                PtsBenchmark = Math.Round(Math.Clamp(avg * 50, 0, 20), 1);

                BenchmarkDelta = avg >= 0.01
                    ? $"+{avg * 100:0.#}% vitesse disque"
                    : avg <= -0.01
                        ? $"{avg * 100:0.#}% (légère variance normale)"
                        : "Stable";
            }
            else
            {
                PtsBenchmark   = 10; // pas de bench = score neutre
                BenchmarkDelta = "Non mesuré";
            }

            // ── 4. Fichiers supprimés (10 pts) ───────────────────────────────────
            // Barème : 0 = 0 pt, 1000+ = 5 pts, 5000+ = 10 pts
            PtsFiles = Math.Round(Math.Min(10, report.TotalFilesDeleted / 500.0), 1);

            // ── Total ─────────────────────────────────────────────────────────────
            double total = PtsStepSuccess + PtsSpaceFreed + PtsBenchmark + PtsFiles;
            Score = (int)Math.Clamp(Math.Round(total), 0, 100);

            Grade = Score switch
            {
                >= 90 => "A+",
                >= 80 => "A",
                >= 70 => "B",
                >= 60 => "C",
                >= 50 => "D",
                _     => "F"
            };

            Message = Score switch
            {
                >= 90 => "PC en excellente forme — performances optimales 🚀",
                >= 80 => "Très bon nettoyage — améliorations significatives ✅",
                >= 70 => "Bon nettoyage — résultats visibles",
                >= 60 => "Nettoyage correct — approfondissez avec DeepClean",
                >= 50 => "Nettoyage partiel — vérifiez les étapes en erreur",
                _     => "Résultats faibles — relancez en tant qu'administrateur"
            };
        }
    }
}
