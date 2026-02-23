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
        private readonly CleaningMode _mode;
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _stopwatch;
        private bool _isCompleted = false;
        private readonly Dictionary<CleaningStep, Border> _stepUI = new();

        public CleaningForm(CleaningMode mode, HashSet<string>? customSteps = null)
        {
            InitializeComponent();

            _mode = mode;
            _engine = new CleaningEngine();
            _stopwatch = new Stopwatch();

            // Appliquer les étapes personnalisées si fourni
            if (customSteps != null)
                foreach (var s in customSteps)
                    _engine.SelectedStepNames.Add(s);

            // Timer pour mettre à jour le temps écoulé
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            // Abonnement aux événements
            _engine.StepStarted += Engine_StepStarted;
            _engine.StepCompleted += Engine_StepCompleted;
            _engine.LogMessage += Engine_LogMessage;
            _engine.ProgressChanged += Engine_ProgressChanged;

            // Configuration de l'interface selon le mode
            TitleText.Text = mode switch
            {
                CleaningMode.Complete          => "NETTOYAGE COMPLET",
                CleaningMode.DeepClean         => "NETTOYAGE DE PRINTEMPS",
                CleaningMode.Custom            => "NETTOYAGE PERSONNALISÉ",
                CleaningMode.SystemOptimization=> "OPTIMISATION SYSTÈME",
                CleaningMode.Advanced          => "MODE AVANCÉ – NETTOYAGE PROFOND",
                _ => "NETTOYAGE EN COURS"
            };
            SubtitleText.Text = mode switch
            {
                CleaningMode.Complete          => "Durée estimée : 20-40 minutes",
                CleaningMode.DeepClean         => "Durée estimée : 60-120 minutes",
                CleaningMode.Custom            => $"{_engine.SelectedStepNames.Count} opération(s) sélectionnée(s)",
                CleaningMode.SystemOptimization=> "7 étapes • Durée estimée : 30-90 minutes",
                CleaningMode.Advanced          => "Mode complet + bloatwares + sysopt • 90-180 min",
                _ => "Veuillez patienter..."
            };

            // Démarrer le nettoyage
            Loaded += async (s, e) => await StartCleaningAsync();
        }

        private async Task StartCleaningAsync()
        {
            _stopwatch.Start();
            _timer.Start();

            AddLog("=== DÉBUT DU NETTOYAGE ===");
            AddLog($"Mode: {(_mode == CleaningMode.Complete ? "Complet" : "De printemps")}");
            AddLog($"Date: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            AddLog("");

            try
            {
                var progress = new Progress<int>(percent =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        GlobalProgress.Value = percent;
                        ProgressPercent.Text = $"{percent}%";
                    });
                });

                var report = await _engine.RunCleaningAsync(_mode, progress);

                _stopwatch.Stop();
                _timer.Stop();
                _isCompleted = true;

                // Afficher le résumé
                ShowSummary(report);
            }
            catch (Exception ex)
            {
                AddLog($"ERREUR CRITIQUE: {ex.Message}");
                MessageBox.Show(
                    $"Une erreur s'est produite:\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
            }
        }

        private void Engine_StepStarted(CleaningStep step)
        {
            Dispatcher.Invoke(() =>
            {
                // Créer l'UI pour l'étape
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(248, 248, 248)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(15)
                };

                var stackPanel = new StackPanel();

                var titleBlock = new TextBlock
                {
                    Text = $"▶ {step.Name}",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 215))
                };

                var statusBlock = new TextBlock
                {
                    Text = step.Status,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    Margin = new Thickness(0, 5, 0, 0)
                };

                stackPanel.Children.Add(titleBlock);
                stackPanel.Children.Add(statusBlock);
                border.Child = stackPanel;

                StepsPanel.Children.Add(border);
                _stepUI[step] = border;

                ProgressText.Text = $"En cours : {step.Name}";
            });
        }

        private void Engine_StepCompleted(CleaningStep step)
        {
            Dispatcher.Invoke(() =>
            {
                if (_stepUI.TryGetValue(step, out var border))
                {
                    var stackPanel = (StackPanel)border.Child;
                    var titleBlock = (TextBlock)stackPanel.Children[0];
                    var statusBlock = (TextBlock)stackPanel.Children[1];

                    if (step.HasError)
                    {
                        titleBlock.Text = $"❌ {step.Name}";
                        titleBlock.Foreground = new SolidColorBrush(Color.FromRgb(209, 52, 56));
                        statusBlock.Text = $"Erreur: {step.ErrorMessage}";
                        border.Background = new SolidColorBrush(Color.FromRgb(255, 240, 240));
                    }
                    else
                    {
                        titleBlock.Text = $"✅ {step.Name}";
                        titleBlock.Foreground = new SolidColorBrush(Color.FromRgb(16, 124, 16));
                        statusBlock.Text = $"Terminé - {step.FilesDeleted} fichiers - {FormatBytes(step.SpaceFreed)} - {step.Duration.TotalSeconds:F1}s";
                        border.Background = new SolidColorBrush(Color.FromRgb(240, 255, 240));
                    }
                }

                // Mettre à jour les totaux
                UpdateTotals();
            });
        }

        private void Engine_LogMessage(string message)
        {
            AddLog(message);
        }

        private void Engine_ProgressChanged(int percent)
        {
            Dispatcher.Invoke(() =>
            {
                GlobalProgress.Value = percent;
                ProgressPercent.Text = $"{percent}%";
            });
        }

        private void UpdateTotals()
        {
            var totalFiles = _engine.Steps.Sum(s => s.FilesDeleted);
            var totalSpace = _engine.Steps.Sum(s => s.SpaceFreed);

            FilesDeletedText.Text = totalFiles.ToString("N0");
            SpaceFreedText.Text = FormatBytes(totalSpace);
        }

        private void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogText.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
                LogScrollViewer.ScrollToEnd();
            });
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var elapsed = _stopwatch.Elapsed;
            ElapsedTimeText.Text = $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds}s";
        }

        private void ShowSummary(CleaningReport report)
        {
            Dispatcher.Invoke(() =>
            {
                TitleText.Text = "✅ NETTOYAGE TERMINÉ";
                SubtitleText.Text = "Opération réussie !";
                ProgressText.Text = "Terminé";
                GlobalProgress.Value = 100;
                ProgressPercent.Text = "100%";

                AddLog("");
                AddLog("=== RÉSUMÉ FINAL ===");
                AddLog($"Fichiers supprimés : {report.TotalFilesDeleted}");
                AddLog($"Espace libéré : {FormatBytes(report.TotalSpaceFreed)}");
                AddLog($"Menaces détectées : {report.ThreatsFound}");
                AddLog($"Durée totale : {report.TotalDuration.Hours}h {report.TotalDuration.Minutes}m {report.TotalDuration.Seconds}s");
                AddLog("");

                var reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                AddLog($"Rapport sauvegardé dans : {reportDir}");

                MessageBox.Show(
                    $"Nettoyage terminé avec succès !\n\n" +
                    $"📁 Fichiers supprimés : {report.TotalFilesDeleted}\n" +
                    $"💾 Espace libéré : {FormatBytes(report.TotalSpaceFreed)}\n" +
                    $"⏱️ Durée : {report.TotalDuration.Hours}h {report.TotalDuration.Minutes}m {report.TotalDuration.Seconds}s\n\n" +
                    $"Un rapport détaillé a été généré dans le dossier Reports.",
                    "Nettoyage terminé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
        }

        private string FormatBytes(long bytes)
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

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isCompleted)
            {
                var result = MessageBox.Show(
                    "Le nettoyage est en cours.\nÊtes-vous sûr de vouloir quitter ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
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
}
