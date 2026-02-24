using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using NettoyerPc.Core;

namespace NettoyerPc
{
    public partial class ReportViewerForm : Window
    {
        private readonly string _reportDir;
        private ReportItem? _selectedItem;
        private List<ReportItem> _allReports = new();
        private SortMode _currentSortMode = SortMode.Date;
        private System.Windows.Threading.DispatcherTimer? _notificationTimer;

        private enum SortMode { Date, Space, Duration }

        private record ReportItem(string FileName, string DisplayLabel, string SubLabel, bool IsJson, 
            long SpaceFreed = 0, double DurationSeconds = 0, DateTime Date = default);

        public ReportViewerForm()
        {
            InitializeComponent();
            _reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            Loaded += (s, e) => { ApplyLanguage(); LoadReportList(); };
            
            // Raccourcis clavier
            KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                    Close();
                else if (e.Key == System.Windows.Input.Key.F && 
                        (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
                {
                    SearchBox.Focus();
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.E && 
                        (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
                {
                    if (BtnExportReport.IsEnabled)
                        BtnExportReport_Click(null, null!);
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.O && 
                        (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
                {
                    BtnOpenFolder_Click(null, null!);
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.F5)
                {
                    BtnRefresh_Click(null, null!);
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.Delete && _selectedItem != null)
                {
                    BtnDeleteReport_Click(null, null!);
                    e.Handled = true;
                }
            };
        }

        private void ApplyLanguage()
        {
            var L = Core.Localizer.T;
            Title                   = L("report.window.title");
            TxtRvHeader.Text        = L("report.header");
            BtnOpenFolder.Content   = L("report.btn.folder");
            BtnCloseRv.Content      = L("btn.close");
            TxtSidebarHistory.Text  = L("report.sidebar.history");
            BtnDeleteReport.Content = L("report.btn.delete");
            TxtPlaceholder1.Text    = L("report.placeholder.title");
            TxtPlaceholder2.Text    = L("report.placeholder.body");
            TxtStatSpaceLabel.Text  = L("report.stat.space");
            TxtStatSpaceSub.Text    = L("report.stat.space.sub");
            TxtStatFilesLabel.Text  = L("report.stat.files");
            TxtStatFilesSub.Text    = L("report.stat.files.sub");
            TxtStatDurLabel.Text    = L("report.stat.duration");
            TxtStatDurSub.Text      = L("report.stat.duration.sub");
            TxtStatStepsLabel.Text  = L("report.stat.steps");
            TxtDetailTitle.Text     = L("report.detail.title");
            TxtLegacyTitle.Text     = L("report.legacy.title");
        }

        private void LoadReportList()
        {
            ReportList.Items.Clear();
            _allReports.Clear();
            ShowPlaceholder();

            if (!Directory.Exists(_reportDir))
            {
                TxtSubtitle.Text = Core.Localizer.T("report.subtitle.none");
                UpdateGlobalStats();
                return;
            }

            var allFiles = Directory.GetFiles(_reportDir, "*.json")
                .Concat(Directory.GetFiles(_reportDir, "*.txt"))
                .ToList();

            if (allFiles.Count == 0)
            {
                TxtSubtitle.Text = Core.Localizer.T("report.subtitle.none");
                UpdateGlobalStats();
                return;
            }

            foreach (var file in allFiles)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var isJson = file.EndsWith(".json");
                var dateStr = "";
                long spaceFreed = 0;
                double duration = 0;
                DateTime date = File.GetCreationTime(file);
                
                try
                {
                    var parts = name.Split('_');
                    if (parts.Length >= 3)
                        dateStr = $"{parts[1]}  {parts[2].Replace('-', ':')}";
                    else
                        dateStr = name;
                    
                    // Extract metadata for sorting (JSON only)
                    if (isJson)
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(File.ReadAllText(file));
                            var root = doc.RootElement;
                            bool isV4 = root.TryGetProperty("reportVersion", out _);
                            
                            if (isV4)
                            {
                                spaceFreed = root.GetProperty("summary").GetProperty("totalSpaceFreedBytes").GetInt64();
                                duration = root.GetProperty("execution").GetProperty("durationSeconds").GetDouble();
                                date = root.GetProperty("execution").GetProperty("startTime").GetDateTime();
                            }
                            else
                            {
                                spaceFreed = root.GetProperty("totalSpaceFreed").GetInt64();
                                duration = root.GetProperty("durationSeconds").GetDouble();
                                date = root.GetProperty("startTime").GetDateTime();
                            }
                        }
                        catch { }
                    }
                }
                catch { dateStr = name; }

                var sub = isJson ? Core.Localizer.T("report.json.format") : Core.Localizer.T("report.txt.format");
                var item = new ReportItem(Path.GetFileName(file), dateStr, sub, isJson, spaceFreed, duration, date);
                _allReports.Add(item);
            }

            ApplySortAndFilter();
            UpdateFooterStats();
            UpdateGlobalStats();
        }

        private void ApplySortAndFilter()
        {
            var searchText = SearchBox?.Text?.ToLower() ?? "";
            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? _allReports
                : _allReports.Where(r => r.DisplayLabel.ToLower().Contains(searchText) || 
                                        r.FileName.ToLower().Contains(searchText)).ToList();

            var sorted = _currentSortMode switch
            {
                SortMode.Space => filtered.OrderByDescending(r => r.SpaceFreed).ToList(),
                SortMode.Duration => filtered.OrderByDescending(r => r.DurationSeconds).ToList(),
                _ => filtered.OrderByDescending(r => r.Date).ToList() // Date by default
            };

            ReportList.Items.Clear();
            foreach (var item in sorted)
                ReportList.Items.Add(item);

            TxtSubtitle.Text = $"{sorted.Count}{Core.Localizer.T("report.subtitle.count")}";
        }

        private void UpdateGlobalStats()
        {
            long totalSpace = 0;
            int totalReports = 0;

            if (!Directory.Exists(_reportDir))
            {
                TxtGlobalReports.Text = "0";
                TxtGlobalSpace.Text = "0 B";
                return;
            }

            var jsonFiles = Directory.GetFiles(_reportDir, "*.json");
            foreach (var file in jsonFiles)
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(file));
                    var root = doc.RootElement;
                    
                    bool isV4 = root.TryGetProperty("reportVersion", out _);
                    long space;
                    
                    if (isV4)
                        space = root.GetProperty("summary").GetProperty("totalSpaceFreedBytes").GetInt64();
                    else
                        space = root.GetProperty("totalSpaceFreed").GetInt64();
                    
                    totalSpace += space;
                    totalReports++;
                }
                catch { }
            }

            TxtGlobalReports.Text = totalReports.ToString();
            TxtGlobalSpace.Text = CleaningReport.FormatBytes(totalSpace);
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.ToLower();
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(searchText) ? Visibility.Visible : Visibility.Collapsed;
            ApplySortAndFilter();
        }

        private void BtnSortDate_Click(object sender, RoutedEventArgs e)
        {
            _currentSortMode = SortMode.Date;
            UpdateSortButtons();
            ApplySortAndFilter();
            ShowNotification("📅 Tri par date activé");
        }

        private void BtnSortSpace_Click(object sender, RoutedEventArgs e)
        {
            _currentSortMode = SortMode.Space;
            UpdateSortButtons();
            ApplySortAndFilter();
            ShowNotification("💾 Tri par espace libéré activé");
        }

        private void BtnSortDuration_Click(object sender, RoutedEventArgs e)
        {
            _currentSortMode = SortMode.Duration;
            UpdateSortButtons();
            ApplySortAndFilter();
            ShowNotification("⏱️ Tri par durée activé");
        }

        private void UpdateSortButtons()
        {
            // Reset all buttons
            BtnSortDate.Background = new SolidColorBrush(Color.FromRgb(26, 26, 46));
            BtnSortSpace.Background = new SolidColorBrush(Color.FromRgb(26, 26, 46));
            BtnSortDuration.Background = new SolidColorBrush(Color.FromRgb(26, 26, 46));
            
            BtnSortDate.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            BtnSortSpace.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            BtnSortDuration.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));

            // Highlight active button
            var activeBtn = _currentSortMode switch
            {
                SortMode.Space => BtnSortSpace,
                SortMode.Duration => BtnSortDuration,
                _ => BtnSortDate
            };

            activeBtn.Background = new SolidColorBrush(Color.FromRgb(42, 42, 62));
            activeBtn.Foreground = Brushes.White;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadReportList();
            ShowNotification("🔄 Liste des rapports actualisée");
        }

        private void ShowNotification(string message, bool isError = false)
        {
            NotificationText.Text = message;
            NotificationBar.Background = isError 
                ? new SolidColorBrush(Color.FromRgb(90, 26, 26))  // Red for errors
                : new SolidColorBrush(Color.FromRgb(42, 90, 42)); // Green for success
            NotificationBar.Visibility = Visibility.Visible;

            // Auto-hide after 3 seconds
            _notificationTimer?.Stop();
            _notificationTimer = new System.Windows.Threading.DispatcherTimer 
            { 
                Interval = TimeSpan.FromSeconds(3) 
            };
            _notificationTimer.Tick += (s, e) =>
            {
                NotificationBar.Visibility = Visibility.Collapsed;
                _notificationTimer.Stop();
            };
            _notificationTimer.Start();
        }

        private void CloseNotification_Click(object sender, RoutedEventArgs e)
        {
            NotificationBar.Visibility = Visibility.Collapsed;
            _notificationTimer?.Stop();
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
                
                // Support both v4.0 and legacy formats
                long space;
                int files;
                DateTime start;
                
                if (root.TryGetProperty("reportVersion", out _))
                {
                    // v4.0 format
                    space = root.GetProperty("summary").GetProperty("totalSpaceFreedBytes").GetInt64();
                    files = root.GetProperty("summary").GetProperty("totalFilesDeleted").GetInt32();
                    start = root.GetProperty("execution").GetProperty("startTime").GetDateTime();
                }
                else
                {
                    // Legacy format
                    space = root.GetProperty("totalSpaceFreed").GetInt64();
                    files = root.GetProperty("totalFilesDeleted").GetInt32();
                    start = root.GetProperty("startTime").GetDateTime();
                }
                
                var L2 = Core.Localizer.T;
                TxtFooterStats.Text = L2("report.footer.last") + start.ToString("dd/MM/yyyy") + "  |  " + CleaningReport.FormatBytes(space) + L2("report.footer.freed") + files + L2("report.footer.files");
            }
            catch { }
        }

        private void ReportList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReportList.SelectedItem is not ReportItem item) return;
            _selectedItem = item;
            BtnExportReport.IsEnabled = true; // Enable export button
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

            // Detect format version
            bool isV4 = root.TryGetProperty("reportVersion", out _);
            
            DateTime startTime;
            double durSec;
            long spaceFreed;
            int totalFiles;
            string machine;
            string user;
            JsonElement stepsEl;

            if (isV4)
            {
                // v4.0 format with nested structure
                startTime  = root.GetProperty("execution").GetProperty("startTime").GetDateTime();
                durSec     = root.GetProperty("execution").GetProperty("durationSeconds").GetDouble();
                spaceFreed = root.GetProperty("summary").GetProperty("totalSpaceFreedBytes").GetInt64();
                totalFiles = root.GetProperty("summary").GetProperty("totalFilesDeleted").GetInt32();
                machine    = root.GetProperty("system").GetProperty("machineName").GetString() ?? "";
                user       = root.GetProperty("system").GetProperty("userName").GetString() ?? "";
                stepsEl    = root.GetProperty("steps");
            }
            else
            {
                // Legacy format with flat structure
                startTime  = root.GetProperty("startTime").GetDateTime();
                durSec     = root.GetProperty("durationSeconds").GetDouble();
                spaceFreed = root.GetProperty("totalSpaceFreed").GetInt64();
                totalFiles = root.GetProperty("totalFilesDeleted").GetInt32();
                machine    = root.GetProperty("machineName").GetString() ?? "";
                user       = root.GetProperty("userName").GetString() ?? "";
                stepsEl    = root.GetProperty("steps");
            }

            var span    = TimeSpan.FromSeconds(durSec);
            var spanStr = span.TotalMinutes >= 1 ? $"{(int)span.TotalMinutes}m {span.Seconds:00}s" : $"{span.Seconds}s";

            var L = Core.Localizer.T;
            ReportTitle.Text = L("report.session") + startTime.ToString("dddd dd MMMM yyyy ") + "\u00e0 " + startTime.ToString("HH:mm");
            ReportMeta.Text  = L("report.user") + user + L("report.machine") + machine;

            StatSpace.Text    = CleaningReport.FormatBytes(spaceFreed);
            StatFiles.Text    = totalFiles.ToString("N0");
            StatDuration.Text = spanStr;

            var total = stepsEl.GetArrayLength();
            var ok    = stepsEl.EnumerateArray().Count(s => !s.GetProperty("hasError").GetBoolean());
            StatSteps.Text    = total.ToString();
            var Ls = Core.Localizer.T;
            StatStepsSub.Text = $"{ok}{Ls("report.steps.ok")}{total - ok}{Ls("report.steps.errors")}";

            // Display performance score and benchmark (v4.0 only)
            if (isV4 && root.TryGetProperty("performanceScore", out var scoreEl))
            {
                PerformanceScorePanel.Visibility = Visibility.Visible;
                var score = scoreEl.GetProperty("value").GetInt32();
                var grade = scoreEl.GetProperty("grade").GetString() ?? "—";
                
                TxtScoreValue.Text = score.ToString();
                TxtScoreGrade.Text = grade;
                TxtScoreMessage.Text = scoreEl.GetProperty("message").GetString() ?? "";
                
                // Dynamic colors based on grade
                var (bgColor, textColor) = grade switch
                {
                    "S" => ("#2E1A00", "#FFD700"), // Gold
                    "A" => ("#1E3E2E", "#5AE896"), // Green
                    "B" => ("#1E2E4E", "#6BBBFF"), // Blue
                    "C" => ("#3E3A1E", "#FFD060"), // Yellow
                    "D" => ("#3E2A1E", "#FF9D4A"), // Orange
                    "F" => ("#3E1E1E", "#FF6B6B"), // Red
                    _   => ("#1E3E2E", "#5AE896")  // Default green
                };
                
                ScoreBadge.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(bgColor)!;
                TxtScoreValue.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(textColor)!;
                TxtScoreGrade.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(textColor)!;
                
                if (scoreEl.TryGetProperty("benchmarkDelta", out var benchDelta))
                {
                    TxtBenchmarkDelta.Text = benchDelta.GetString() ?? "—";
                }
                else
                {
                    TxtBenchmarkDelta.Text = "—";
                }
            }
            else
            {
                PerformanceScorePanel.Visibility = Visibility.Collapsed;
            }

            // Display categories breakdown (v4.0 only)
            if (isV4 && root.TryGetProperty("byCategory", out var catEl) && catEl.ValueKind == JsonValueKind.Array)
            {
                CategoryPanel.Visibility = Visibility.Visible;
                CategoryList.Items.Clear();
                
                var categories = catEl.EnumerateArray()
                    .Select(c => new
                    {
                        Name = TranslateCategoryName(c.GetProperty("category").GetString() ?? ""),
                        Space = c.GetProperty("totalSpaceFreedBytes").GetInt64(),
                        ColorHex = GetCategoryColor(c.GetProperty("category").GetString() ?? "")
                    })
                    .OrderByDescending(c => c.Space)
                    .Take(6) // Top 6 categories
                    .ToList();
                
                long maxSpace = categories.FirstOrDefault()?.Space ?? 1;
                
                foreach (var cat in categories)
                {
                    if (cat.Space == 0) continue;
                    
                    var percentage = maxSpace > 0 ? (double)cat.Space / maxSpace * 100 : 0;
                    CategoryList.Items.Add(CreateCategoryBar(cat.Name, cat.Space, percentage, cat.ColorHex));
                }
            }
            else
            {
                CategoryPanel.Visibility = Visibility.Collapsed;
            }

            StepsList.Items.Clear();
            foreach (var step in stepsEl.EnumerateArray())
            {
                var sName  = step.GetProperty("name").GetString() ?? "";
                var sCat   = step.GetProperty("category").GetString() ?? "";
                var sStatus= step.GetProperty("status").GetString() ?? "";
                var sDur   = TimeSpan.FromSeconds(step.GetProperty("durationSeconds").GetDouble());
                
                // Support both formats for space/files
                int sFiles = step.GetProperty("filesDeleted").GetInt32();
                long sSpace;
                if (isV4)
                {
                    // v4.0 format uses spaceFreedBytes
                    sSpace = step.GetProperty("spaceFreedBytes").GetInt64();
                }
                else
                {
                    // Legacy format uses spaceFreed
                    sSpace = step.GetProperty("spaceFreed").GetInt64();
                }
                
                var sErr   = step.GetProperty("hasError").GetBoolean();
                var sErrMsg= step.TryGetProperty("errorMessage", out var em) ? em.GetString() : null;
                
                // Extract logs if available
                List<string> logs = new();
                if (step.TryGetProperty("logs", out var logsEl) && logsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var log in logsEl.EnumerateArray())
                    {
                        var logStr = log.GetString();
                        if (!string.IsNullOrWhiteSpace(logStr))
                            logs.Add(logStr);
                    }
                }
                
                var durStr = sDur.TotalMinutes >= 1 ? $"{(int)sDur.TotalMinutes}m {sDur.Seconds:00}s" : $"{sDur.Seconds}s";
                StepsList.Items.Add(BuildStepCard(sName, sCat, sStatus, durStr, sFiles, sSpace, sErr, sErrMsg, logs));
            }

            PlaceholderPanel.Visibility = Visibility.Collapsed;
            TxtPanel.Visibility         = Visibility.Collapsed;
            JsonPanel.Visibility        = Visibility.Visible;
            
            // Animate panel appearance
            AnimateFadeIn(JsonPanel);
        }

        private void AnimateFadeIn(UIElement element)
        {
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            element.BeginAnimation(OpacityProperty, animation);
        }

        private UIElement BuildStepCard(string name, string cat, string status, string duration,
            int files, long space, bool hasError, string? errorMsg, List<string> logs)
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
            
            // Add logs if available
            if (logs.Count > 0)
            {
                // Show first 3 logs
                var logsToShow = logs.Take(3).ToList();
                foreach (var log in logsToShow)
                {
                    leftSP.Children.Add(new TextBlock
                    {
                        Text = "  • " + log,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromArgb(160, 170, 170, 180)),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                }
                
                // If there are more logs, create expandable section
                if (logs.Count > 3)
                {
                    var hiddenLogsPanel = new StackPanel 
                    { 
                        Visibility = Visibility.Collapsed,
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    
                    // Add remaining logs to hidden panel
                    foreach (var log in logs.Skip(3))
                    {
                        hiddenLogsPanel.Children.Add(new TextBlock
                        {
                            Text = "  • " + log,
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.FromArgb(160, 170, 170, 180)),
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 2, 0, 0)
                        });
                    }
                    
                    // Create toggle button
                    var toggleButton = new TextBlock
                    {
                        Text = $"  ▸ Afficher {logs.Count - 3} autres entrées",
                        FontSize = 10,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(100, 150, 250)),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Margin = new Thickness(0, 4, 0, 0)
                    };
                    
                    // Toggle click handler
                    toggleButton.MouseLeftButtonDown += (s, e) =>
                    {
                        if (hiddenLogsPanel.Visibility == Visibility.Collapsed)
                        {
                            hiddenLogsPanel.Visibility = Visibility.Visible;
                            toggleButton.Text = "  ▾ Masquer les entrées supplémentaires";
                        }
                        else
                        {
                            hiddenLogsPanel.Visibility = Visibility.Collapsed;
                            toggleButton.Text = $"  ▸ Afficher {logs.Count - 3} autres entrées";
                        }
                    };
                    
                    leftSP.Children.Add(toggleButton);
                    leftSP.Children.Add(hiddenLogsPanel);
                }
            }
            
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

            var Lc = Core.Localizer.T;
            Chip(Lc("report.chip.status"),  hasError ? Lc("report.chip.error") : Lc("report.chip.ok"), hasError ? "#5A1010" : "#0D2A1A");
            Chip(Lc("report.chip.duration"), duration,                                                   "#1A1A3A");
            Chip(Lc("report.chip.files"),    files.ToString("N0"),                                       "#0D1F3C");
            Chip(Lc("report.chip.space"),    CleaningReport.FormatBytes(space),                         "#0D2A1A");

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
            try
            {
                Directory.CreateDirectory(_reportDir);
                Process.Start("explorer.exe", _reportDir);
                ShowNotification("📁 Dossier ouvert dans l'explorateur");
            }
            catch (Exception ex)
            {
                ShowNotification($"❌ Erreur : {ex.Message}", true);
            }
        }

        private void BtnExportReport_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            
            var path = Path.Combine(_reportDir, _selectedItem.FileName);
            if (!File.Exists(path)) return;

            try
            {
                // Copy file path to clipboard
                System.Windows.Clipboard.SetText(path);
                ShowNotification($"📋 Chemin copié : {_selectedItem.FileName}");
                
                var menu = new System.Windows.Controls.ContextMenu();
                
                var copyPathItem = new System.Windows.Controls.MenuItem { Header = "📋 Chemin copié dans le presse-papier" };
                copyPathItem.IsEnabled = false;
                menu.Items.Add(copyPathItem);
                
                var sep = new System.Windows.Controls.Separator();
                menu.Items.Add(sep);
                
                var openItem = new System.Windows.Controls.MenuItem { Header = "📂 Ouvrir l'emplacement" };
                openItem.Click += (s, ev) => 
                {
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                };
                menu.Items.Add(openItem);
                
                menu.PlacementTarget = BtnExportReport;
                menu.IsOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export : {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private UIElement CreateCategoryBar(string name, long bytes, double percentage, string colorHex)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameText = new TextBlock
            {
                Text = name,
                FontSize = 11,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameText, 0);

            var barBg = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 40)),
                Height = 18,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(8, 0, 8, 0)
            };

            var barFill = new Border
            {
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom(colorHex)!,
                Height = 18,
                CornerRadius = new CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = percentage * 2 // Max 200px for 100%
            };

            barBg.Child = barFill;
            Grid.SetColumn(barBg, 1);

            var sizeText = new TextBlock
            {
                Text = CleaningReport.FormatBytes(bytes),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(colorHex)!,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(sizeText, 2);

            grid.Children.Add(nameText);
            grid.Children.Add(barBg);
            grid.Children.Add(sizeText);

            return grid;
        }

        private string GetCategoryColor(string category)
        {
            return category switch
            {
                "general" => "#5AE896",
                "browser" => "#6BBBFF",
                "gaming" => "#CC99FF",
                "thirdparty" => "#4AACFF",
                "network" => "#5AE896",
                "windows" => "#6BBBFF",
                "dev" => "#FFD060",
                "sysopt" => "#6BBBFF",
                "security" => "#6BBBFF",
                "advanced" => "#FF9D4A",
                "bloatware" => "#FF6B6B",
                _ => "#888888"
            };
        }

        private string TranslateCategoryName(string category)
        {
            return category switch
            {
                "general" => "Général",
                "browser" => "Navigateurs",
                "gaming" => "Gaming",
                "thirdparty" => "Apps tierces",
                "network" => "Réseau",
                "windows" => "Windows",
                "dev" => "Développement",
                "sysopt" => "Optimisation",
                "security" => "Sécurité",
                "advanced" => "Avancé",
                "bloatware" => "Bloatware",
                "drivers" => "Pilotes",
                _ => category
            };
        }

        private void BtnDeleteReport_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            var Ld = Core.Localizer.T;
            if (MessageBox.Show(Ld("report.delete.confirm.pre") + _selectedItem.FileName + Ld("report.delete.confirm.suf"),
                    Ld("report.delete.title"),
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    var fileName = _selectedItem.FileName;
                    File.Delete(Path.Combine(_reportDir, fileName));
                    _selectedItem = null;
                    BtnExportReport.IsEnabled = false;
                    LoadReportList();
                    ShowNotification($"🗑️ Rapport supprimé : {fileName}");
                }
                catch (Exception ex)
                {
                    ShowNotification($"❌ Erreur : {ex.Message}", true);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        // ── Chrome borderless ──────────────────────────────────────────────────────
        private void Minimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object s, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void CloseWin_Click(object s, RoutedEventArgs e) => Close();
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            BtnMaximize.Content = WindowState == WindowState.Maximized ? "❐" : "⬜";
        }
    }
}
