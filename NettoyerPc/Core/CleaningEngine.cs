using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NettoyerPc.Core
{
    public enum CleaningMode
    {
        Complete,
        DeepClean,
        /// <summary>Mode personnalisé : exécute uniquement les étapes sélectionnées</summary>
        Custom,
        /// <summary>Optimisation système 7 étapes</summary>
        SystemOptimization,
        /// <summary>Mode avancé : tout inclus + bloatwares + sysopt + apps tierces</summary>
        Advanced
    }

    public class CleaningEngine
    {
        public CleaningReport Report { get; private set; } = new();
        public ObservableCollection<CleaningStep> Steps { get; } = new();
        /// <summary>Pour le mode Custom : noms des étapes à exécuter</summary>
        public HashSet<string> SelectedStepNames { get; } = new();
        
        public event Action<int>? ProgressChanged;
        public event Action<CleaningStep>? StepStarted;
        public event Action<CleaningStep>? StepCompleted;
        public event Action<string>? LogMessage;

        private readonly List<Modules.ICleaningModule> _modules = new();
        private CancellationTokenSource? _cancellationTokenSource;

        public CleaningEngine()
        {
            InitializeModules();
        }

        private void InitializeModules()
        {
            _modules.Add(new Modules.TempFilesModule());
            _modules.Add(new Modules.DevCacheModule());
            _modules.Add(new Modules.BrowserModule());
            _modules.Add(new Modules.GamingModule());
            _modules.Add(new Modules.NetworkModule());
            _modules.Add(new Modules.WindowsModule());
            _modules.Add(new Modules.SecurityModule());
            _modules.Add(new Modules.ThirdPartyAppsModule());
            _modules.Add(new Modules.DriverModule());
            _modules.Add(new Modules.SystemOptimizationModule());
            _modules.Add(new Modules.BloatwareModule());
            _modules.Add(new Modules.AdvancedCleaningModule());
        }

        /// <summary>Retourne toutes les étapes disponibles pour la sélection granulaire</summary>
        public List<(Modules.ICleaningModule Module, CleaningStep Step)> GetAllAvailableSteps()
        {
            var result = new List<(Modules.ICleaningModule, CleaningStep)>();
            foreach (var module in _modules)
                foreach (var step in module.GetSteps(CleaningMode.Advanced))
                    result.Add((module, step));
            return result;
        }

        /// <summary>Retourne le nombre total d'étapes qui seront exécutées pour le mode donné.
        /// Utilisé par le CleaningForm pour pré-initialiser le compteur du bas.</summary>
        public int GetExpectedStepCount(CleaningMode mode)
            => GetPairsForMode(mode).Count;

        public async Task<CleaningReport> RunCleaningAsync(CleaningMode mode, IProgress<int>? progress = null)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            Report = new CleaningReport
            {
                StartTime    = DateTime.Now,
                CleaningMode = mode.ToString()
            };
            Steps.Clear();

            try
            {
                // ── Benchmark AVANT le nettoyage ─────────────────────────────────
                LogMessage?.Invoke("═══ Benchmark disque (avant nettoyage) ═══");
                var bmBefore = new DiskBenchmark();
                await Task.Run(() => bmBefore.Run(msg => LogMessage?.Invoke(msg)), token);
                Report.BenchmarkBefore = bmBefore;
                LogMessage?.Invoke($"  Résultat : {bmBefore.Summary()}");
                LogMessage?.Invoke("");

                var pairs      = GetPairsForMode(mode);
                var totalSteps = pairs.Count;
                var currentIdx = 0;

                foreach (var (module, step) in pairs)
                {
                    if (token.IsCancellationRequested) break;

                    // Vérifier si l'étape doit être ignorée (app désactivée)
                    if (IsStepSkipped(step))
                    {
                        Report.SkippedSteps.Add(step.Name);
                        LogMessage?.Invoke($"Ignoré (app désactivée): {step.Name}");
                        currentIdx++;
                        var skipPct2 = (int)((double)currentIdx / totalSteps * 100);
                        progress?.Report(skipPct2);
                        ProgressChanged?.Invoke(skipPct2);
                        continue;
                    }

                    Steps.Add(step);
                    StepStarted?.Invoke(step);
                    LogMessage?.Invoke($"Démarrage: {step.Name}");

                    var startTime = DateTime.Now;
                    try
                    {
                        step.Status = "En cours...";
                        await module.ExecuteStepAsync(step, token);
                        step.IsCompleted = true;
                        step.Status      = "Terminé";
                        step.Progress    = 100;
                        Report.TotalFilesDeleted += step.FilesDeleted;
                        Report.TotalSpaceFreed   += step.SpaceFreed;
                    }
                    catch (Exception ex)
                    {
                        step.HasError     = true;
                        step.ErrorMessage = ex.Message;
                        step.Status       = "Erreur";
                        LogMessage?.Invoke($"Erreur: {step.Name} - {ex.Message}");
                    }
                    finally
                    {
                        step.Duration = DateTime.Now - startTime;
                        lock (step.Logs)
                            foreach (var logLine in step.Logs)
                                LogMessage?.Invoke($"    ▸ {logLine}");
                        StepCompleted?.Invoke(step);
                        Report.Steps.Add(step);
                    }

                    currentIdx++;
                    var pct = (int)((double)currentIdx / totalSteps * 100);
                    progress?.Report(pct);
                    ProgressChanged?.Invoke(pct);
                }

                Report.EndTime = DateTime.Now;

                // ── Benchmark APRÈS le nettoyage ─────────────────────────────────
                LogMessage?.Invoke("");
                LogMessage?.Invoke("═══ Benchmark disque (après nettoyage) ═══");
                var bmAfter = new DiskBenchmark();
                await Task.Run(() => bmAfter.Run(msg => LogMessage?.Invoke(msg)), CancellationToken.None);
                Report.BenchmarkAfter = bmAfter;
                LogMessage?.Invoke($"  Résultat : {bmAfter.Summary()}");
                LogMessage?.Invoke("");

                // ── Score de performance ──────────────────────────────────────────
                Report.Score = new PerformanceScore(Report, bmBefore, bmAfter);
                LogMessage?.Invoke($"═══ Score de performance : {Report.Score.Score}/100  (Grade {Report.Score.Grade}) ═══");
                LogMessage?.Invoke($"  {Report.Score.Message}");
                LogMessage?.Invoke($"  Benchmark delta : {Report.Score.BenchmarkDelta}");
                LogMessage?.Invoke("");

                // ── Sauvegarde rapport JSON ───────────────────────────────────────
                var reportDir  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                var reportPath = Report.SaveReportJson(reportDir);
                LogMessage?.Invoke($"Rapport JSON sauvegardé : {reportPath}");
                LogMessage?.Invoke($"Nettoyage terminé — {Report.TotalFilesDeleted} fichiers, {FormatBytes(Report.TotalSpaceFreed)} libérés");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Erreur critique: {ex.Message}");
                Report.EndTime = DateTime.Now;
            }

            return Report;
        }

        private List<(Modules.ICleaningModule Module, CleaningStep Step)> GetPairsForMode(CleaningMode mode)
        {
            var result = new List<(Modules.ICleaningModule Module, CleaningStep Step)>();

            if (mode == CleaningMode.Custom)
            {
                foreach (var module in _modules)
                    foreach (var step in module.GetSteps(CleaningMode.Advanced))
                        if (SelectedStepNames.Contains(step.Name))
                            result.Add((module, step));
                return result;
            }

            if (mode == CleaningMode.SystemOptimization)
            {
                var sysOpt = _modules.OfType<Modules.SystemOptimizationModule>().FirstOrDefault();
                if (sysOpt != null)
                    foreach (var step in sysOpt.GetSteps(mode))
                        result.Add((sysOpt, step));
                return result;
            }

            if (mode == CleaningMode.Advanced)
            {
                foreach (var module in _modules)
                    foreach (var step in module.GetSteps(CleaningMode.DeepClean))
                        result.Add((module, step));
                return result;
            }

            // Complete & DeepClean : modules de base uniquement
            var baseModuleTypes = new List<Type>
            {
                typeof(Modules.TempFilesModule), typeof(Modules.DevCacheModule),
                typeof(Modules.BrowserModule),   typeof(Modules.GamingModule),
                typeof(Modules.NetworkModule),   typeof(Modules.WindowsModule),
                typeof(Modules.SecurityModule)
            };
            foreach (var module in _modules.Where(m => baseModuleTypes.Contains(m.GetType())))
                foreach (var step in module.GetSteps(mode))
                    result.Add((module, step));

            return result;
        }

        private List<Modules.ICleaningModule> GetModulesForMode(CleaningMode mode) => _modules;

        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
            LogMessage?.Invoke("Annulation demandée...");
        }
        // ─── Mapping étape → clé d'app (null = toujours exécuté) ─────────────────
        // Lorsque SkipDisabledApps est vrai, les étapes dont l'app est désactivée
        // sont ignorées et ajoutées à Report.SkippedSteps.
        private static readonly Dictionary<string, string> StepAppMap =
            new(StringComparer.OrdinalIgnoreCase)
        {
            ["Nettoyage Firefox"]                         = "firefox",
            ["Nettoyage Chrome"]                          = "chrome",
            ["Nettoyage Edge"]                            = "edge",
            ["Nettoyage Brave"]                           = "brave",
            ["Nettoyage Opera / Opera GX"]                = "opera",
            ["Nettoyage Vivaldi"]                         = "vivaldi",
            ["Steam cache (tous disques)"]                = "steam",
            ["Steam (shader cache, logs, dumps)"]         = "steam",
            ["Epic Games / Battle.net"]                   = "epicgames",
            ["Epic Games (logs)"]                         = "epicgames",
            ["Battle.net (cache)"]                        = "battlenet",
            ["EA App / Origin (cache, logs)"]             = "eaapp",
            ["Ubisoft Connect (cache)"]                   = "ubisoft",
            ["GOG Galaxy (cache)"]                        = "gog",
            ["Riot Games / League (cache)"]               = "riot",
            ["Minecraft (logs, crash reports)"]           = "minecraft",
            ["Discord (cache, code cache, GPU cache)"]    = "discord",
            ["Spotify (storage cache)"]                   = "spotify",
            ["Teams (cache, GPU cache)"]                  = "teams",
            ["Slack (cache, code cache)"]                 = "slack",
            ["OBS Studio (logs)"]                         = "obs",
            ["Zoom (cache, logs)"]                        = "zoom",
            ["WhatsApp Desktop (cache)"]                  = "whatsapp",
            ["Telegram Desktop (cache)"]                  = "telegram",
            ["Adobe Creative Cloud (cache)"]              = "adobe",
            ["Adobe Photoshop (Camera Raw cache, logs)"]  = "photoshop",
            ["Adobe Illustrator (cache, logs)"]           = "illustrator",
            ["Adobe InDesign (cache, logs)"]              = "indesign",
            ["Adobe Acrobat / Reader (cache, logs)"]      = "acrobat",
            ["Adobe Bridge (cache miniatures, logs)"]     = "bridge",
            ["Adobe XD (cache, logs)"]                    = "adobexd",
            ["Adobe Audition (cache média, logs)"]        = "audition",
            ["Adobe Media Encoder (cache, logs)"]         = "mediaencoder",
            ["Adobe Premiere Pro — cache média disque"]   = "premierepro",
            ["Adobe After Effects — cache disque"]        = "aftereffects",
            ["Adobe Lightroom — cache d'aperçus"]         = "lightroom",            ["Microsoft Office (cache, temp, MRU)"]       = "msoffice",
            ["LibreOffice / OpenOffice (cache, temp)"]    = "libreoffice",
            ["WPS Office (cache)"]                        = "wpsoffice",
            ["Scan .svn (tous disques)"]                 = "svn",
            ["Nettoyage logs Git (tous disques)"]          = "gitlogs",
            ["Nettoyage Visual Studio (tous disques)"]     = "visualstudio",
            ["Suppression node_modules (tous disques)"]    = "nodemodules",
            ["Caches NuGet/Gradle/Maven/npm/pip"]          = "devcaches",
            ["VS Code cache (tous disques)"]               = "vscode",
            ["JetBrains IDEs (caches, logs)"]             = "jetbrains",
            ["Eclipse / NetBeans (cache)"]                = "eclipse",
            ["Android Studio (caches)"]                   = "androidstudio",
            ["Cursor IDE (cache)"]                        = "cursor",            ["Figma (cache)"]                             = "figma",
            ["Notion (cache)"]                            = "notion",
            ["Twitch (cache)"]                            = "twitch",
            ["VLC (cache, logs)"]                         = "vlc",
        };

        private static bool IsStepSkipped(CleaningStep step)
        {
            var prefs = UserPreferences.Current;
            if (!prefs.SkipDisabledApps) return false;
            if (!StepAppMap.TryGetValue(step.Name, out var appKey)) return false;
            return !prefs.IsAppEnabled(appKey);
        }
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
