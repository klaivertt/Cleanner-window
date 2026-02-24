using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using NettoyerPc.Core;

namespace NettoyerPc
{
    public partial class CleaningForm : Window
    {
        private readonly CleaningEngine _engine;
        private readonly CleaningMode   _mode;
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch       _stopwatch;
        private bool _isCompleted = false;
        private int  _stepsTotal  = 0;
        private int  _stepsOk     = 0;

        // step -> (border, titleBlock, statusBlock, subProgressBar, elapsedLabel, stepStopwatch)
        private readonly Dictionary<CleaningStep, (Border B, TextBlock T, TextBlock S, ProgressBar P, TextBlock E, Stopwatch SW)> _stepUI = new();

        // Couleurs de catégorie
        private static readonly Dictionary<string, string> CatBorderHex = new()
        {
            ["general"]    = "#1A6B3A", ["browser"]    = "#1A4B3A",
            ["gaming"]     = "#3B1E6E", ["thirdparty"] = "#1A3A5A",
            ["network"]    = "#1A4A2A", ["windows"]    = "#1A4B6B",
            ["dev"]        = "#4B4B1A", ["sysopt"]     = "#1A3A6B",
            ["security"]   = "#1A3A6B", ["advanced"]   = "#4A2A00",
            ["bloatware"]  = "#6B2A00", ["drivers"]    = "#1A3A5A",
        };

        public CleaningForm(CleaningMode mode, HashSet<string>? customSteps = null)
        {
            InitializeComponent();
            _mode      = mode;
            _engine    = new CleaningEngine();
            _stopwatch = new Stopwatch();

            if (customSteps != null)
                foreach (var s in customSteps)
                    _engine.SelectedStepNames.Add(s);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;

            _engine.StepStarted    += Engine_StepStarted;
            _engine.StepCompleted  += Engine_StepCompleted;
            _engine.LogMessage     += Engine_LogMessage;
            _engine.ProgressChanged+= Engine_ProgressChanged;

            TitleText.Text = mode switch
            {
                CleaningMode.Complete          => Localizer.T("clean.mode.complete"),
                CleaningMode.DeepClean         => Localizer.T("clean.mode.deepclean"),
                CleaningMode.Custom            => Localizer.T("clean.mode.custom"),
                CleaningMode.SystemOptimization=> Localizer.T("clean.mode.sysopt"),
                CleaningMode.Advanced          => Localizer.T("clean.mode.advanced"),
                _ => Localizer.T("clean.title.running")
            };
            SubtitleText.Text = mode switch
            {
                CleaningMode.Complete          => Localizer.T("clean.sub.complete"),
                CleaningMode.DeepClean         => Localizer.T("clean.sub.deepclean"),
                CleaningMode.Custom            => _engine.SelectedStepNames.Count + Localizer.T("sel.ops"),
                CleaningMode.SystemOptimization=> Localizer.T("clean.sub.sysopt"),
                CleaningMode.Advanced          => Localizer.T("clean.sub.advanced"),
                _ => Localizer.T("clean.sub.wait")
            };

            Loaded += (s, e) => { ApplyLanguage(); };
            Loaded += async (s, e) => await StartCleaningAsync();
        }

        private async Task StartCleaningAsync()
        {
            _stopwatch.Start();
            _timer.Start();
            AddLog(Localizer.T("clean.log.start"));
            AddLog($"Mode : {TitleText.Text}  |  {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            AddLog("");

            try
            {
// Pré-calcul du nombre total d'étapes pour que le compteur soit correct dès le début
            _stepsTotal = _engine.GetExpectedStepCount(_mode);
            StepsOkText.Text = $"0 / {_stepsTotal}";

            var progress = new Progress<int>(pct => Dispatcher.Invoke(() =>
                {
                    GlobalProgress.Value  = pct;
                    ProgressPercent.Text  = $"{pct}%";
                }));

                var report = await _engine.RunCleaningAsync(_mode, progress);

                _stopwatch.Stop();
                _timer.Stop();
                _isCompleted = true;
                ShowSummary(report);
            }
            catch (Exception ex)
            {
                AddLog($"ERREUR CRITIQUE : {ex.Message}");
                MessageBox.Show($"Une erreur s'est produite :\n{ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void Engine_StepStarted(CleaningStep step)
        {
            Dispatcher.Invoke(() =>
            {
                // _stepsTotal already initialized before cleaning — do not increment here
                StepsOkText.Text = $"{_stepsOk} / {_stepsTotal}";

                var catHex    = CatBorderHex.TryGetValue(step.Category ?? "general", out var h) ? h : "#2A2A3E";
                var catBrush  = (SolidColorBrush)new BrushConverter().ConvertFrom(catHex)!;

                // ── Ligne titre + chrono ──────────────────────────────────────────────
                var titleBlock = new TextBlock
                {
                    Text       = $"  {step.Name}",
                    FontSize   = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255)),
                };
                var elapsedTxt = new TextBlock
                {
                    Text              = "0s",
                    FontSize          = 10,
                    Foreground        = new SolidColorBrush(Color.FromRgb(100, 100, 140)),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin            = new Thickness(8, 0, 0, 0),
                };
                var headerRow = new Grid();
                headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Grid.SetColumn(titleBlock, 0);
                Grid.SetColumn(elapsedTxt, 1);
                headerRow.Children.Add(titleBlock);
                headerRow.Children.Add(elapsedTxt);

                // ── Barre de progression sous-tâche (indéterminée = pulsation) ───────
                var subBar = new ProgressBar
                {
                    Height          = 3,
                    IsIndeterminate = true,
                    Minimum         = 0,
                    Maximum         = 100,
                    Value           = 0,
                    Foreground      = new SolidColorBrush(Color.FromRgb(74, 172, 255)),
                    Background      = new SolidColorBrush(Color.FromRgb(30, 30, 50)),
                    BorderThickness = new Thickness(0),
                    Margin          = new Thickness(0, 5, 0, 5),
                };

                // ── Ligne status ──────────────────────────────────────────────────────
                var statusBlock = new TextBlock
                {
                    Text       = Localizer.T("clean.step.running"),
                    FontSize   = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 140))
                };

                // ── Indicateur de catégorie (barre latérale colorée) ──────────────────
                var indicator = new Border
                {
                    Width          = 4,
                    Background     = catBrush,
                    CornerRadius   = new CornerRadius(2, 0, 0, 2),
                    Margin         = new Thickness(0, 0, 12, 0)
                };

                var sp = new StackPanel();
                sp.Children.Add(headerRow);
                sp.Children.Add(subBar);
                sp.Children.Add(statusBlock);

                var rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetColumn(indicator, 0);
                Grid.SetColumn(sp, 1);
                rowGrid.Children.Add(indicator);
                rowGrid.Children.Add(sp);

                var border = new Border
                {
                    Background      = new SolidColorBrush(Color.FromRgb(26, 26, 45)),
                    BorderBrush     = catBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius    = new CornerRadius(6),
                    Margin          = new Thickness(0, 0, 0, 6),
                    Padding         = new Thickness(0, 10, 14, 10),
                    Child           = rowGrid
                };

                // ── Chrono individuel ────────────────────────────────────────────────
                var stepSw = new Stopwatch();
                stepSw.Start();

                StepsPanel.Children.Add(border);
                _stepUI[step] = (border, titleBlock, statusBlock, subBar, elapsedTxt, stepSw);
                ProgressText.Text = step.Name;

                // Auto-scroll
                StepsScrollViewer.ScrollToEnd();
            });
        }

        private void Engine_StepCompleted(CleaningStep step)
        {
            Dispatcher.Invoke(() =>
            {
                if (!_stepUI.TryGetValue(step, out var ui)) return;
                var (border, title, status, subBar, elapsedTxt, stepSw) = ui;

                stepSw.Stop();
                var finalElapsed = stepSw.Elapsed;
                var elStr = finalElapsed.TotalMinutes >= 1
                    ? $"{(int)finalElapsed.TotalMinutes}m {finalElapsed.Seconds:00}s"
                    : $"{finalElapsed.TotalSeconds:F1}s";

                // Finaliser la barre de progression
                subBar.IsIndeterminate = false;
                subBar.Value = 100;

                if (step.HasError)
                {
                    var errLabel = Localizer.T("clean.step.error");
                    subBar.Foreground  = new SolidColorBrush(Color.FromRgb(200, 60, 60));
                    title.Foreground   = new SolidColorBrush(Color.FromRgb(255, 80, 80));
                    status.Text        = $"{errLabel} : {step.ErrorMessage}";
                    status.Foreground  = new SolidColorBrush(Color.FromRgb(200, 80, 80));
                    elapsedTxt.Text    = elStr;
                    elapsedTxt.Foreground = new SolidColorBrush(Color.FromRgb(200, 80, 80));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(140, 30, 30));
                    border.Background  = new SolidColorBrush(Color.FromRgb(35, 18, 18));
                }
                else
                {
                    var okLabel = Localizer.T("clean.step.ok");
                    _stepsOk++;
                    subBar.Foreground  = new SolidColorBrush(Color.FromRgb(80, 200, 120));
                    title.Foreground   = new SolidColorBrush(Color.FromRgb(80, 200, 120));
                    elapsedTxt.Text    = elStr;
                    elapsedTxt.Foreground = new SolidColorBrush(Color.FromRgb(80, 160, 100));
                    status.Text = step.FilesDeleted > 0
                        ? $"{okLabel} — {step.FilesDeleted:N0} fichiers  |  {FormatBytes(step.SpaceFreed)}  |  {elStr}"
                        : $"{okLabel} — {elStr}";
                    status.Foreground = new SolidColorBrush(Color.FromRgb(70, 180, 100));
                    border.Background = new SolidColorBrush(Color.FromRgb(18, 30, 22));
                }

                StepsOkText.Text  = $"{_stepsOk} / {_stepsTotal}";
                ThreatsText.Text  = _engine.Report.ThreatsFound.ToString();
                UpdateTotals();
                StepsScrollViewer.ScrollToEnd();
            });
        }

        private void Engine_LogMessage(string message) => AddLog(message);

        private void Engine_ProgressChanged(int pct)
        {
            Dispatcher.Invoke(() =>
            {
                GlobalProgress.Value = pct;
                ProgressPercent.Text = $"{pct}%";
            });
        }

        private void UpdateTotals()
        {
            var files = _engine.Steps.Sum(s => s.FilesDeleted);
            var space = _engine.Steps.Sum(s => s.SpaceFreed);
            FilesDeletedText.Text = files.ToString("N0");
            SpaceFreedText.Text   = FormatBytes(space);
        }

        private void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogText.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                LogScrollViewer.ScrollToEnd();
            });
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // ── Chrono global ─────────────────────────────────────────────────────────
            var el = _stopwatch.Elapsed;
            ElapsedTimeText.Text = $"{(int)el.TotalMinutes}m {el.Seconds:00}s";

            // ── Chrono par étape (vivant uniquement si non terminé) ───────────────────
            foreach (var kvp in _stepUI)
            {
                var step = kvp.Key;
                if (step.IsCompleted || step.HasError) continue;

                var (_, _, _, _, elapsedTxt, stepSw) = kvp.Value;
                var stepEl = stepSw.Elapsed;
                var elStr = stepEl.TotalMinutes >= 1
                    ? $"{(int)stepEl.TotalMinutes}m {stepEl.Seconds:00}s"
                    : $"{(int)stepEl.TotalSeconds}s";

                // Orange après 2 minutes (tâches longues comme SFC/DISM)
                elapsedTxt.Text = elStr;
                elapsedTxt.Foreground = stepEl.TotalMinutes >= 2
                    ? new SolidColorBrush(Color.FromRgb(255, 184, 48))
                    : new SolidColorBrush(Color.FromRgb(100, 100, 140));

                // Hint sous la barre pour les très longues tâches
                if (stepEl.TotalMinutes >= 3)
                {
                    var (_, _, statusTxt, _, _, _) = kvp.Value;
                    if (!statusTxt.Text.Contains("SFC") && !statusTxt.Text.Contains("DISM")
                        && statusTxt.Text == Localizer.T("clean.step.running"))
                        statusTxt.Text = "⏳ " + Localizer.T("clean.frozen.hint");
                }
            }
        }

        private void ShowSummary(CleaningReport report)
        {
            Dispatcher.Invoke(() =>
            {
                var L   = Localizer.T;
                var score = report.Score;
                var bmB   = report.BenchmarkBefore;
                var bmA   = report.BenchmarkAfter;

                var dur = report.TotalDuration;
                var durStr = dur.TotalMinutes >= 60
                    ? $"{(int)dur.TotalHours}h {dur.Minutes}m"
                    : $"{(int)dur.TotalMinutes}m {dur.Seconds}s";

                TitleText.Text        = L("clean.title.done");
                SubtitleText.Text     = $"{report.TotalFilesDeleted:N0} fichiers  |  {FormatBytes(report.TotalSpaceFreed)} libérés  |  ⏱ {durStr}";
                ProgressText.Text     = L("clean.done.label");
                GlobalProgress.Value  = 100;
                ProgressPercent.Text  = "100%";

                UpdateTotals();


                // ── Log récapitulatif ──────────────────────────────────────────────
                AddLog("");
                AddLog("═══════════════════════════════════════");
                AddLog(L("clean.log.summary"));
                AddLog($"  Fichiers supprimés  : {report.TotalFilesDeleted:N0}");
                AddLog($"  Espace libéré       : {FormatBytes(report.TotalSpaceFreed)}");
                AddLog($"  Menaces             : {report.ThreatsFound}");
                AddLog($"  Étapes réussies     : {_stepsOk} / {_stepsTotal}");
                var errCount = report.Steps.Count(s => s.HasError);
                if (errCount > 0)
                    AddLog($"  ⚠ Étapes en erreur   : {errCount}");
                AddLog($"  Durée totale        : {durStr}");

                if (score != null)
                {
                    AddLog("");
                    AddLog($"  ★ Score performance : {score.Score}/100  (Grade {score.Grade})");
                    AddLog($"  {score.Message}");
                    if (score.BenchmarkDelta != "Non mesuré")
                        AddLog($"  Benchmark disque    : {score.BenchmarkDelta}");
                }

                // ── Détail des étapes significatives ───────────────────────────────────────
                var topSteps = report.Steps
                    .Where(s => s.SpaceFreed > 0 || s.FilesDeleted > 0)
                    .OrderByDescending(s => s.SpaceFreed)
                    .Take(10)
                    .ToList();
                if (topSteps.Count > 0)
                {
                    AddLog("");
                    AddLog("  ─ Top étapes par espace libéré ──────────────────");
                    foreach (var s in topSteps)
                    {
                        var icon = s.HasError ? "✗" : "✔";
                        var dur2 = s.Duration.TotalSeconds < 60
                            ? $"{s.Duration.TotalSeconds:0.#}s"
                            : $"{(int)s.Duration.TotalMinutes}m{s.Duration.Seconds:00}s";
                        AddLog($"  {icon} {s.Name}");
                        AddLog($"       {s.FilesDeleted:N0} fichiers  |  {FormatBytes(s.SpaceFreed)}  |  {dur2}");
                    }
                }

                // ── Étapes en erreur ──────────────────────────────────────────────
                var errorSteps = report.Steps.Where(s => s.HasError).ToList();
                if (errorSteps.Count > 0)
                {
                    AddLog("");
                    AddLog($"  ⚠ {errorSteps.Count} étape(s) en erreur ──────────────────────");
                    foreach (var s in errorSteps)
                        AddLog($"  ✗ {s.Name} : {s.ErrorMessage}");
                }
                if (bmB != null && bmA != null && bmB.Success && bmA.Success)
                {
                    AddLog("");
                    AddLog("  ─ Benchmark disque ─────────────────");
                    AddLog($"  Avant  — Lecture {bmB.ReadSpeedMBs} MB/s · Écriture {bmB.WriteSpeedMBs} MB/s");
                    AddLog($"  Après  — Lecture {bmA.ReadSpeedMBs} MB/s · Écriture {bmA.WriteSpeedMBs} MB/s");
                    var rdDelta = bmA.ReadSpeedMBs  - bmB.ReadSpeedMBs;
                    var wrDelta = bmA.WriteSpeedMBs - bmB.WriteSpeedMBs;
                    AddLog($"  Delta  — Lecture {(rdDelta >= 0 ? "+" : "")}{rdDelta:0.#} MB/s · Écriture {(wrDelta >= 0 ? "+" : "")}{wrDelta:0.#} MB/s");
                }

                AddLog("");
                AddLog("  Rapport JSON sauvegardé dans le dossier Reports/");
                AddLog("═══════════════════════════════════════");
                AddLog("");

                // Auto-ouvrir le rapport si activé
                if (Core.UserPreferences.Current.AutoOpenReport)
                {
                    try
                    {
                        var reportDir  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                        var latest = Directory.GetFiles(reportDir, "CleanerReport_*.json")
                            .OrderByDescending(f => f)
                            .FirstOrDefault();
                        if (latest != null)
                            Process.Start(new ProcessStartInfo(latest) { UseShellExecute = true });
                    }
                    catch { }
                }

                // ── MessageBox récapitulatif enrichi ──────────────────────────────
                var scoreLine = score != null
                    ? $"\n\n  ★ Score : {score.Score}/100  Grade {score.Grade}\n  {score.Message}"
                    : "";

                string benchLine = "";
                if (bmB?.Success == true && bmA?.Success == true)
                    benchLine = $"\n\n  Disque avant  : {bmB.ReadSpeedMBs} MB/s R · {bmB.WriteSpeedMBs} MB/s W" +
                                $"\n  Disque après  : {bmA.ReadSpeedMBs} MB/s R · {bmA.WriteSpeedMBs} MB/s W" +
                                $"\n  Delta lecture : {(bmA.ReadSpeedMBs - bmB.ReadSpeedMBs >= 0 ? "+" : "")}{bmA.ReadSpeedMBs - bmB.ReadSpeedMBs:0.#} MB/s";

                MessageBox.Show(
                    L("clean.summary.ok") + "\n\n" +
                    $"  {L("clean.summary.space")}  {FormatBytes(report.TotalSpaceFreed)}\n" +
                    $"  {L("clean.summary.files")}  {report.TotalFilesDeleted:N0}\n" +
                    $"  {L("clean.summary.steps")}  {_stepsOk} / {_stepsTotal}" +
                    (report.Steps.Any(s => s.HasError) ? $"  (⚠ {report.Steps.Count(s => s.HasError)} erreur(s))" : "") + "\n" +
                    $"  {L("clean.summary.dur")}  {durStr}" +
                    scoreLine + benchLine + "\n\n" +
                    L("clean.summary.report") +
                    (report.RebootRequired ? L("clean.summary.reboot") : ""),
                    L("clean.summary.title"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private static string FormatBytes(long bytes)
        {
            string[] sz = { "B", "KB", "MB", "GB", "TB" };
            double v = bytes; int o = 0;
            while (v >= 1024 && o < sz.Length - 1) { o++; v /= 1024; }
            return $"{v:0.##} {sz[o]}";
        }

        // ── Traduction ─────────────────────────────────────────────────────────────
        private void ApplyLanguage()
        {
            var L = Localizer.T;
            this.Title              = L("clean.title.running");
            ProgressText.Text       = L("clean.preparing");
            TxtStatSpaceLabel.Text  = L("clean.stat.space");
            TxtStatFilesLabel.Text  = L("clean.stat.files");
            TxtStatStepsLabel.Text  = L("clean.stat.steps");
            TxtStatThreatsLabel.Text= L("clean.stat.threats");
            TxtLogTitle.Text        = L("clean.log.title");
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isCompleted) return;
            if (MessageBox.Show(
                    Localizer.T("clean.closing.body"),
                    Localizer.T("clean.closing.title"),
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                _engine.Cancel();
                _timer.Stop();
                _stopwatch.Stop();
            }
        }

        // ── Chrome borderless ──────────────────────────────────────────────────────
        private void CloseWin_Click(object s, RoutedEventArgs e) => Close();
    }
}
