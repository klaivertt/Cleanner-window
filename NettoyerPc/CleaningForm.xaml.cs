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

        // step -> (border, titleBlock, statusBlock)
        private readonly Dictionary<CleaningStep, (Border B, TextBlock T, TextBlock S)> _stepUI = new();

        // Couleurs de catégorie
        private static readonly Dictionary<string, string> CatBorderHex = new()
        {
            ["general"]    = "#1A6B3A", ["browser"]    = "#1A4B3A",
            ["gaming"]     = "#3B1E6E", ["thirdparty"] = "#1A3A5A",
            ["network"]    = "#1A4A2A", ["windows"]    = "#1A4B6B",
            ["dev"]        = "#4B4B1A", ["sysopt"]     = "#1A3A6B",
            ["security"]   = "#1A3A6B", ["advanced"]   = "#4A2A00",
            ["bloatware"]  = "#6B2A00",
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
                CleaningMode.Complete          => "NETTOYAGE RAPIDE",
                CleaningMode.DeepClean         => "NETTOYAGE DE PRINTEMPS",
                CleaningMode.Custom            => "NETTOYAGE PERSONNALISE",
                CleaningMode.SystemOptimization=> "OPTIMISATION SYSTEME",
                CleaningMode.Advanced          => "MODE AVANCE",
                _ => "NETTOYAGE EN COURS"
            };
            SubtitleText.Text = mode switch
            {
                CleaningMode.Complete          => "Duree estimee : 20-40 minutes",
                CleaningMode.DeepClean         => "Duree estimee : 60-120 minutes",
                CleaningMode.Custom            => $"{_engine.SelectedStepNames.Count} operation(s) selectionnee(s)",
                CleaningMode.SystemOptimization=> "7 etapes — SFC / DISM / reseau / disque",
                CleaningMode.Advanced          => "Mode complet + bloatwares + sysopt",
                _ => "Veuillez patienter..."
            };

            Loaded += async (s, e) => await StartCleaningAsync();
        }

        private async Task StartCleaningAsync()
        {
            _stopwatch.Start();
            _timer.Start();
            AddLog("=== DEBUT DU NETTOYAGE ===");
            AddLog($"Mode : {_mode}  |  {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            AddLog("");

            try
            {
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
                _stepsTotal++;
                StepsOkText.Text = $"{_stepsOk} / {_stepsTotal}";

                var catHex    = CatBorderHex.TryGetValue(step.Category ?? "general", out var h) ? h : "#2A2A3E";
                var catBrush  = (SolidColorBrush)new BrushConverter().ConvertFrom(catHex)!;

                var titleBlock = new TextBlock
                {
                    Text       = $"  {step.Name}",
                    FontSize   = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255)),
                    Margin     = new Thickness(0, 0, 0, 4)
                };
                var statusBlock = new TextBlock
                {
                    Text       = "En cours...",
                    FontSize   = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 140))
                };
                var indicator = new Border
                {
                    Width          = 4,
                    Background     = catBrush,
                    CornerRadius   = new CornerRadius(2, 0, 0, 2),
                    Margin         = new Thickness(0, 0, 12, 0)
                };

                var rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var sp = new StackPanel(); sp.Children.Add(titleBlock); sp.Children.Add(statusBlock);
                Grid.SetColumn(indicator, 0); Grid.SetColumn(sp, 1);
                rowGrid.Children.Add(indicator); rowGrid.Children.Add(sp);

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

                StepsPanel.Children.Add(border);
                _stepUI[step] = (border, titleBlock, statusBlock);
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
                var (border, title, status) = ui;

                if (step.HasError)
                {
                    title.Text       = $"  {step.Name}";
                    title.Foreground = new SolidColorBrush(Color.FromRgb(255, 80, 80));
                    status.Text      = $"ERREUR : {step.ErrorMessage}";
                    status.Foreground= new SolidColorBrush(Color.FromRgb(200, 80, 80));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(140, 30, 30));
                    border.Background  = new SolidColorBrush(Color.FromRgb(35, 18, 18));
                }
                else
                {
                    _stepsOk++;
                    title.Text       = $"  {step.Name}";
                    title.Foreground = new SolidColorBrush(Color.FromRgb(80, 200, 120));
                    var durStr = step.Duration.TotalMinutes >= 1
                        ? $"{(int)step.Duration.TotalMinutes}m {step.Duration.Seconds:00}s"
                        : $"{step.Duration.TotalSeconds:F1}s";
                    status.Text = step.FilesDeleted > 0
                        ? $"OK — {step.FilesDeleted:N0} fichiers  |  {FormatBytes(step.SpaceFreed)}  |  {durStr}"
                        : $"OK — {durStr}";
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
            var el = _stopwatch.Elapsed;
            ElapsedTimeText.Text = $"{(int)el.TotalMinutes}m {el.Seconds:00}s";
        }

        private void ShowSummary(CleaningReport report)
        {
            Dispatcher.Invoke(() =>
            {
                TitleText.Text        = "NETTOYAGE TERMINE";
                SubtitleText.Text     = $"{report.TotalFilesDeleted:N0} fichiers  |  {FormatBytes(report.TotalSpaceFreed)} liberes";
                ProgressText.Text     = "Termine";
                GlobalProgress.Value  = 100;
                ProgressPercent.Text  = "100%";

                UpdateTotals();

                var dur = report.TotalDuration;
                var durStr = dur.TotalMinutes >= 60
                    ? $"{(int)dur.TotalHours}h {dur.Minutes}m"
                    : $"{(int)dur.TotalMinutes}m {dur.Seconds}s";

                AddLog("");
                AddLog("=== RESUME FINAL ===");
                AddLog($"Fichiers supprimes  : {report.TotalFilesDeleted:N0}");
                AddLog($"Espace libere       : {FormatBytes(report.TotalSpaceFreed)}");
                AddLog($"Menaces detectees   : {report.ThreatsFound}");
                AddLog($"Etapes reussies     : {_stepsOk} / {_stepsTotal}");
                AddLog($"Duree totale        : {durStr}");
                AddLog($"Rapport JSON sauvegarde dans : Reports/");
                AddLog("");

                var ok = _stepsTotal - (_stepsTotal - _stepsOk);
                MessageBox.Show(
                    $"Nettoyage termine avec succes !\n\n" +
                    $"  Espace libere    :  {FormatBytes(report.TotalSpaceFreed)}\n" +
                    $"  Fichiers suppr.  :  {report.TotalFilesDeleted:N0}\n" +
                    $"  Etapes reussies  :  {_stepsOk} / {_stepsTotal}\n" +
                    $"  Duree totale     :  {durStr}\n\n" +
                    $"Un rapport JSON detaille a ete sauvegarde dans le dossier Reports.\n" +
                    (report.RebootRequired ? "\nUn redemarrage est recommande." : ""),
                    "Nettoyage termine",
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

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isCompleted) return;
            if (MessageBox.Show("Le nettoyage est en cours.\nQuitter quand meme ?",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
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
    }
}
