using System;
using System.Diagnostics;
using System.IO;

namespace NettoyerPc.Core
{
    /// <summary>
    /// Benchmark séquentiel lecture/écriture du disque système (C:\Temp).
    /// Utilise un fichier temporaire de 256 MB avec FileOptions.WriteThrough
    /// pour contourner le cache OS et mesurer la vitesse réelle du stockage.
    /// Exécuté avant et après le nettoyage pour mesurer l'amélioration.
    /// </summary>
    public class DiskBenchmark
    {
        // ── Résultats ────────────────────────────────────────────────────────────
        public double WriteSpeedMBs  { get; private set; }
        public double ReadSpeedMBs   { get; private set; }
        public bool   Success        { get; private set; }
        public string ErrorMessage   { get; private set; } = string.Empty;
        public DateTime MeasuredAt   { get; private set; }

        // ── Paramètres ───────────────────────────────────────────────────────────
        private const int FileSizeMB  = 256;
        private const int ChunkSizeKB = 64;

        /// <summary>
        /// Lance le benchmark (bloquant — appeler dans un Task.Run).
        /// <paramref name="log"/> reçoit les messages de progression.
        /// </summary>
        public void Run(Action<string>? log = null)
        {
            MeasuredAt = DateTime.Now;
            var tempFile = Path.Combine(Path.GetTempPath(), $"pcclean_bench_{Guid.NewGuid():N}.tmp");

            try
            {
                var chunk       = new byte[ChunkSizeKB * 1024];
                new Random(42).NextBytes(chunk);
                int totalChunks = (FileSizeMB * 1024) / ChunkSizeKB;

                // ── Écriture séquentielle ─────────────────────────────────────────
                log?.Invoke($"  ▶ Benchmark écriture ({FileSizeMB} MB)…");
                var sw = Stopwatch.StartNew();
                using (var fs = new FileStream(
                    tempFile, FileMode.Create, FileAccess.Write, FileShare.None,
                    bufferSize: chunk.Length, FileOptions.WriteThrough))
                {
                    for (int i = 0; i < totalChunks; i++)
                        fs.Write(chunk, 0, chunk.Length);
                }
                sw.Stop();
                WriteSpeedMBs = Math.Round(FileSizeMB / sw.Elapsed.TotalSeconds, 1);
                log?.Invoke($"  ✓ Écriture : {WriteSpeedMBs} MB/s  ({sw.ElapsedMilliseconds} ms)");

                // ── Lecture séquentielle ──────────────────────────────────────────
                log?.Invoke($"  ▶ Benchmark lecture ({FileSizeMB} MB)…");
                var buf = new byte[chunk.Length];
                sw.Restart();
                using (var fs = new FileStream(
                    tempFile, FileMode.Open, FileAccess.Read, FileShare.Read,
                    bufferSize: buf.Length, FileOptions.SequentialScan))
                {
                    while (fs.Read(buf, 0, buf.Length) > 0) { }
                }
                sw.Stop();
                ReadSpeedMBs = Math.Round(FileSizeMB / sw.Elapsed.TotalSeconds, 1);
                log?.Invoke($"  ✓ Lecture  : {ReadSpeedMBs} MB/s  ({sw.ElapsedMilliseconds} ms)");

                Success = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                log?.Invoke($"  ✗ Benchmark échoué : {ex.Message}");
            }
            finally
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }
        }

        /// <summary>Retourne un résumé one-liner pour les logs.</summary>
        public string Summary()
            => Success
                ? $"Lecture {ReadSpeedMBs} MB/s · Écriture {WriteSpeedMBs} MB/s"
                : $"Échec ({ErrorMessage})";
    }
}
