using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NettoyerPc.Core;

namespace NettoyerPc
{
    public partial class ReportViewerForm : Window
    {
        private readonly string _reportDir;
        private ReportItem? _selectedItem;

        private record ReportItem(string FileName, string DisplayLabel, string SubLabel, bool IsJson);

        public ReportViewerForm()
        {
            InitializeComponent();
            _reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            Loaded += (s, e) => LoadReportList();
        }

        private void LoadReportList()
        {
            ReportList.Items.Clear();
            ShowPlaceholder();

            if (!Directory.Exists(_reportDir))
            {
                TxtSubtitle.Text = "Aucun rapport disponible";
                return;
            }

            var allFiles = Directory.GetFiles(_reportDir, "*.json")
                .Concat(Directory.GetFiles(_reportDir, "*.txt"))
                .OrderByDescending(f => f)
                .ToList();

            if (allFiles.Count == 0)
            {
                TxtSubtitle.Text = "Aucun rapport disponible";
                return;
            }

            foreach (var file in allFiles)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var isJson = file.EndsWith(".json");
                var dateStr = "";
                try
                {
                    var parts = name.Split('_');
                    if (parts.Length >= 3)
                        dateStr = $"{parts[1]}  {parts[2].Replace('-', ':')}";
                    else
                        dateStr = name;
                }
                catch { dateStr = name; }

                var sub = isJson ? "JSON — stats detaillees" : "TXT — ancien format";
                ReportList.Items.Add(new ReportItem(Path.GetFileName(file), dateStr, sub, isJson));
            }

            TxtSubtitle.Text = $"{allFiles.Count} rapport(s) enregistre(s)";
            UpdateFooterStats();
        }

        private void UpdateFooterStats()
        {
            if (!Directory.Exists(_reportDir)) return;
            var latest = Directory.GetFiles(_reportDir, "*.json").OrderByDescending(f => f).FirstOrDefault();
            if (latest == null) return;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(latest));
                var root = doc.RootElement;
                var space = root.GetProperty("totalSpaceFreed").GetInt64();
                var files = root.GetProperty("totalFilesDeleted").GetInt32();
                var start = root.GetProperty("startTime").GetDateTime();
                TxtFooterStats.Text = $"Derniere session : {start:dd/MM/yyyy}  |  {CleaningReport.FormatBytes(space)} liberes  |  {files} fichiers supprimes";
            }
            catch { }
        }

        private void ReportList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReportList.SelectedItem is not ReportItem item) return;
            _selectedItem = item;
            var path = Path.Combine(_reportDir, item.FileName);
            try
            {
                if (item.IsJson) ShowJsonReport(path);
                else             ShowTxtReport(path);
            }
            catch (Exception ex)
            {
                ShowTxtReport(null);
                ReportContent.Text = $"Impossible de lire le rapport :\n{ex.Message}";
            }
        }

        private void ShowJsonReport(string path)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;

            var startTime   = root.GetProperty("startTime").GetDateTime();
            var durSec      = root.GetProperty("durationSeconds").GetDouble();
            var spaceFreed  = root.GetProperty("totalSpaceFreed").GetInt64();
            var totalFiles  = root.GetProperty("totalFilesDeleted").GetInt32();
            var machine     = root.GetProperty("machineName").GetString() ?? "";
            var user        = root.GetProperty("userName").GetString() ?? "";
            var stepsEl     = root.GetProperty("steps");

            var span    = TimeSpan.FromSeconds(durSec);
            var spanStr = span.TotalMinutes >= 1 ? $"{(int)span.TotalMinutes}m {span.Seconds:00}s" : $"{span.Seconds}s";

            ReportTitle.Text = $"Session du {startTime:dddd dd MMMM yyyy à HH:mm}";
            ReportMeta.Text  = $"Utilisateur : {user}   •   Machine : {machine}";

            StatSpace.Text    = CleaningReport.FormatBytes(spaceFreed);
            StatFiles.Text    = totalFiles.ToString("N0");
            StatDuration.Text = spanStr;

            var total = stepsEl.GetArrayLength();
            var ok    = stepsEl.EnumerateArray().Count(s => !s.GetProperty("hasError").GetBoolean());
            StatSteps.Text    = total.ToString();
            StatStepsSub.Text = $"{ok} reussies  •  {total - ok} erreur(s)";

            StepsList.Items.Clear();
            foreach (var step in stepsEl.EnumerateArray())
            {
                var sName  = step.GetProperty("name").GetString() ?? "";
                var sCat   = step.GetProperty("category").GetString() ?? "";
                var sStatus= step.GetProperty("status").GetString() ?? "";
                var sDur   = TimeSpan.FromSeconds(step.GetProperty("durationSeconds").GetDouble());
                var sFiles = step.GetProperty("filesDeleted").GetInt32();
                var sSpace = step.GetProperty("spaceFreed").GetInt64();
                var sErr   = step.GetProperty("hasError").GetBoolean();
                var sErrMsg= step.TryGetProperty("errorMessage", out var em) ? em.GetString() : null;
                var durStr = sDur.TotalMinutes >= 1 ? $"{(int)sDur.TotalMinutes}m {sDur.Seconds:00}s" : $"{sDur.Seconds}s";
                StepsList.Items.Add(BuildStepCard(sName, sCat, sStatus, durStr, sFiles, sSpace, sErr, sErrMsg));
            }

            PlaceholderPanel.Visibility = Visibility.Collapsed;
            TxtPanel.Visibility         = Visibility.Collapsed;
            JsonPanel.Visibility        = Visibility.Visible;
        }

        private UIElement BuildStepCard(string name, string cat, string status, string duration,
            int files, long space, bool hasError, string? errorMsg)
        {
            var catColors = new Dictionary<string, string>
            {
                ["general"]    = "#1A6B3A", ["browser"] = "#1A4B3A", ["gaming"] = "#3B1E6E",
                ["thirdparty"] = "#1A3A5A", ["network"] = "#1A4A2A", ["windows"] = "#1A4B6B",
                ["dev"]        = "#4B4B1A", ["sysopt"]  = "#1A3A6B", ["security"]= "#1A3A6B",
                ["advanced"]   = "#4A2A00", ["bloatware"]= "#6B2A00",
            };
            var borderHex  = catColors.TryGetValue(cat, out var c) ? c : "#2A2A3E";
            var borderBrush= (SolidColorBrush)new BrushConverter().ConvertFrom(borderHex)!;

            var card = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(26, 26, 45)),
                BorderBrush     = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(8),
                Padding         = new Thickness(16, 12, 16, 12),
                Margin          = new Thickness(0, 0, 0, 6)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var leftSP = new StackPanel();
            leftSP.Children.Add(new TextBlock
            {
                Text = name, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White
            });
            if (hasError && !string.IsNullOrEmpty(errorMsg))
                leftSP.Children.Add(new TextBlock
                {
                    Text = errorMsg, FontSize = 11,
                    Foreground  = new SolidColorBrush(Color.FromRgb(255, 100, 100)),
                    TextWrapping= TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0)
                });
            Grid.SetColumn(leftSP, 0);

            var rightSP = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            void Chip(string lbl, string val, string bg)
            {
                var b = new Border
                {
                    Background   = (SolidColorBrush)new BrushConverter().ConvertFrom(bg)!,
                    CornerRadius = new CornerRadius(4),
                    Padding      = new Thickness(8, 3, 8, 3),
                    Margin       = new Thickness(6, 0, 0, 0)
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = lbl, FontSize = 9, Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)) });
                sp.Children.Add(new TextBlock { Text = val, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.White });
                b.Child = sp;
                rightSP.Children.Add(b);
            }

            Chip("statut",  hasError ? "ERREUR" : "OK",                hasError ? "#5A1010" : "#0D2A1A");
            Chip("duree",   duration,                                   "#1A1A3A");
            Chip("fichiers",files.ToString("N0"),                       "#0D1F3C");
            Chip("espace",  CleaningReport.FormatBytes(space),          "#0D2A1A");

            Grid.SetColumn(rightSP, 1);
            grid.Children.Add(leftSP);
            grid.Children.Add(rightSP);
            card.Child = grid;
            return card;
        }

        private void ShowTxtReport(string? path)
        {
            PlaceholderPanel.Visibility = Visibility.Collapsed;
            JsonPanel.Visibility        = Visibility.Collapsed;
            TxtPanel.Visibility         = Visibility.Visible;
            if (path != null) ReportContent.Text = File.ReadAllText(path);
        }

        private void ShowPlaceholder()
        {
            PlaceholderPanel.Visibility = Visibility.Visible;
            JsonPanel.Visibility        = Visibility.Collapsed;
            TxtPanel.Visibility         = Visibility.Collapsed;
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(_reportDir);
            Process.Start("explorer.exe", _reportDir);
        }

        private void BtnDeleteReport_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            if (MessageBox.Show($"Supprimer :\n{_selectedItem.FileName} ?", "Confirmation",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    File.Delete(Path.Combine(_reportDir, _selectedItem.FileName));
                    _selectedItem = null;
                    LoadReportList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Impossible de supprimer :\n{ex.Message}");
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
