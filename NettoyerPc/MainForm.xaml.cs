using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace NettoyerPc
{
    public partial class MainForm : Window
    {
        private Core.UpdateManager.UpdateInfo? _pendingUpdate;

        public MainForm()
        {
            InitializeComponent();
            // Restore window geometry
            var prefs = Core.UserPreferences.Current;
            if (!double.IsNaN(prefs.WindowLeft))
            {
                Left   = prefs.WindowLeft;
                Top    = prefs.WindowTop;
                Width  = prefs.WindowWidth;
                Height = prefs.WindowHeight;
            }
            if (prefs.WindowMaximized)
                WindowState = WindowState.Maximized;

            Loaded += (s, e) =>
            {
                Core.Localizer.SetLanguage(prefs.Language);
                ApplyLanguage();
                LoadLastReportStats();
                _ = CheckUpdateSilentAsync();
            };
        }

        /// <summary>Vérifie les mises à jour en arrière-plan et affiche le bandeau si disponible.</summary>
        private async System.Threading.Tasks.Task CheckUpdateSilentAsync()
        {
            try
            {
                _pendingUpdate = await Core.UpdateManager.CheckForUpdatesAsync();
                if (_pendingUpdate == null) return;

                Dispatcher.Invoke(() =>
                {
                    var L = Core.Localizer.T;
                    var isPreRel = _pendingUpdate.IsPreRelease;
                    TxtBannerTitle.Text = isPreRel
                        ? L("update.banner.title.prerelease").Replace("{0}", _pendingUpdate.Version.ToString())
                        : L("update.banner.title.stable").Replace("{0}", _pendingUpdate.Version.ToString());
                    TxtBannerBody.Text = L("update.banner.published").Replace("{0}", _pendingUpdate.PublishedAt.ToString("dd/MM/yyyy")) +
                        (isPreRel ? L("update.banner.prerelease.warn") : "");
                    UpdateBanner.Visibility = Visibility.Visible;
                });
            }
            catch { /* silencieux */ }
        }

        private async void BtnBannerInstall_Click(object sender, RoutedEventArgs e)
        {
            if (_pendingUpdate == null) return;
            BtnBannerInstall.IsEnabled = false;
            TxtBannerBody.Text = Core.Localizer.T("update.banner.downloading");

            var ok = await Core.UpdateManager.DownloadAndInstallAsync(_pendingUpdate,
                pct => Dispatcher.Invoke(() => TxtBannerBody.Text = Core.Localizer.T("update.banner.progress").Replace("{0}", pct.ToString())));

            if (ok)
            {
                MessageBox.Show(Core.Localizer.T("update.install.ok.body"),
                    Core.Localizer.T("update.install.ok.title"), MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
            else
            {
                TxtBannerBody.Text = Core.Localizer.T("update.banner.error");
                BtnBannerInstall.IsEnabled = true;
            }
        }

        private void BtnBannerDismiss_Click(object sender, RoutedEventArgs e)
            => UpdateBanner.Visibility = Visibility.Collapsed;

        // ── Fenêtre borderless : chrome + état maximum ─────────────────────────────────
        private void Minimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object s, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void CloseWin_Click(object s, RoutedEventArgs e) => Close();

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            BtnMaximize.Content = WindowState == WindowState.Maximized ? "❐" : "⬜";
            // Fix : empêche le débordement sous la barre des tâches en mode maximisé
            RootGrid.Margin = WindowState == WindowState.Maximized ? new Thickness(6) : new Thickness(0);
        }

        // ── Préférences : sauvegarde position / taille ─────────────────────────────────
        private void MainForm_Closing(object sender, CancelEventArgs e)
        {
            var prefs = Core.UserPreferences.Current;
            prefs.WindowMaximized = WindowState == WindowState.Maximized;
            if (WindowState == WindowState.Normal)
            {
                prefs.WindowLeft   = Left;
                prefs.WindowTop    = Top;
                prefs.WindowWidth  = Width;
                prefs.WindowHeight = Height;
            }
            prefs.Save();
        }

        // ── Mise à jour des pilotes ────────────────────────────────────────────────────
        private void BtnDriverUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:windowsupdate-optionalupdates")
                    { UseShellExecute = true });
            }
            catch
            {
                Process.Start(new ProcessStartInfo("ms-settings:windowsupdate")
                    { UseShellExecute = true });
            }
        }

        private void BtnDriverScan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "pnputil.exe",
                    Arguments              = "/enum-devices /problem",
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                };
                var proc   = Process.Start(psi);
                var output = proc?.StandardOutput.ReadToEnd() ?? "";
                proc?.WaitForExit(8000);

                if (string.IsNullOrWhiteSpace(output) || output.Trim().Length < 30)
                    MessageBox.Show("Aucun pilote problématique détecté.\nTous vos composants fonctionnent correctement.",
                        "Diagnostic Pilotes ✓", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show(output.Length > 2500 ? output[..2500] + "\n..." : output,
                        "Diagnostic Pilotes — Composants avec problèmes", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'exécuter le diagnostic : {ex.Message}\n\nOuvrez 'Gestionnaire de périphériques' manuellement.",
                    "Diagnostic", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ── Traduction de cette fenêtre ────────────────────────────────────────
        public void ApplyLanguage()
        {
            var L = Core.Localizer.T;

            TxtSubtitle.Text       = L("app.subtitle");
            BtnSettings.Content    = L("btn.settings");
            BtnCheckUpdate.Content = L("btn.updates");

            // Custom card
            TxtCustomTitle.Text = L("card.custom.title");
            TxtCustomBadge.Text = L("card.custom.badge");
            TxtCustomBody.Text  = L("card.custom.body");
            BtnCustom.Content   = L("btn.custom");

            // Quick clean
            TxtQuickTitle.Text = L("card.quick.title");
            TxtQuickBadge.Text = L("card.quick.badge.safe");
            TxtQuickTime.Text  = L("card.quick.time");
            SetInlines(TxtQuickBody,  RichList(L("card.quick.s1.label"),    "#6BBBFF", L("card.quick.s1.items")));
            SetInlines(TxtQuickNever, RichLine(L("card.quick.never.label"), "#4EC98C", L("card.quick.never.items")));
            BtnComplete.Content = L("btn.quick");

            // Gaming
            TxtGamingTitle.Text = L("card.gaming.title");
            TxtGamingBadge.Text = L("card.gaming.badge");
            TxtGamingTime.Text  = L("card.gaming.time");
            SetInlines(TxtGamingBody1, RichList(L("card.gaming.s1.label"),    "#6BBBFF", L("card.gaming.s1.items")));
            SetInlines(TxtGamingBody2, RichList(L("card.gaming.s2.label"),    "#FFB830", L("card.gaming.s2.items")));
            SetInlines(TxtGamingNever, RichLine(L("card.gaming.never.label"), "#4EC98C", L("card.gaming.never.items")));
            BtnGamingDisk.Content = L("btn.gaming");

            // Deep clean
            TxtDeepTitle.Text = L("card.deep.title");
            TxtDeepBadge.Text = L("card.deep.badge");
            TxtDeepTime.Text  = L("card.deep.time");
            SetInlines(TxtDeepBody, RichList(L("card.deep.s1.label"),   "#6BBBFF", L("card.deep.s1.items")));
            SetInlines(TxtDeepInfo, RichLine(L("card.deep.info.label"), "#FFB830", L("card.deep.info.text")));
            BtnDeepClean.Content = L("btn.deep");

            // System optimisation
            TxtSysTitle.Text = L("card.sysopt.title");
            TxtSysBadge.Text = L("card.sysopt.badge");
            SetInlines(TxtSysBody, RichList(L("card.sysopt.s1.label"),  "#6BBBFF", L("card.sysopt.s1.items")));
            SetInlines(TxtSysDur,  RichLine(L("card.sysopt.dur.label"), "#4AACFF", L("card.sysopt.dur.text")));
            BtnSysOpt.Content = L("btn.sysopt");

            // Driver update card
            TxtDriverTitle.Text = L("card.driver.title");
            TxtDriverBadge.Text = L("card.driver.badge");
            SetInlines(TxtDriverBody, RichList(L("card.driver.s1.label"), "#6BBBFF", L("card.driver.s1.items")));
            BtnDriverUpdate.Content = L("btn.driver.update");
            BtnDriverScan.Content   = L("btn.driver.scan");

            // Third-party caches
            TxtThirdTitle.Text = L("card.thirdparty.title");
            TxtThirdBadge.Text = L("card.thirdparty.badge");
            SetInlines(TxtThirdBody, RichList(L("card.thirdparty.intro"), "#AAAACC", L("card.thirdparty.items")));
            BtnThirdParty.Content = L("btn.thirdparty");

            // Bloatware
            TxtBloatTitle.Text = L("card.bloat.title");
            TxtBloatBadge.Text = L("card.bloat.badge");
            SetInlines(TxtBloatBody, new Inline[]
            {
                new Run(L("card.bloat.body")),
                new LineBreak(),
                new Bold(new Run(L("card.bloat.warning"))) { Foreground = Clr("#FF9933") }
            });
            BtnBloatware.Content = L("btn.bloat");

            // Update banner
            BtnBannerInstall.Content = L("update.banner.install");
        }

        // ── Helpers Inline ─────────────────────────────────────────────────────
        private static SolidColorBrush Clr(string hex) =>
            (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;

        private static Bold Bld(string text, string hex) =>
            new Bold(new Run(text)) { Foreground = Clr(hex) };

        private static void SetInlines(TextBlock tb, IEnumerable<Inline> inlines)
        {
            tb.Inlines.Clear();
            foreach (var i in inlines) tb.Inlines.Add(i);
        }

        private static IEnumerable<Inline> RichList(string label, string color, string items)
        {
            var result = new List<Inline> { Bld(label, color) };
            foreach (var line in items.Split('\n'))
            {
                result.Add(new LineBreak());
                result.Add(new Run("  " + line));
            }
            return result;
        }

        private static IEnumerable<Inline> RichLine(string label, string color, string text) =>
            new Inline[] { Bld(label, color), new Run(" " + text) };

        private void LoadLastReportStats()
        {
            try
            {
                var reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                if (!Directory.Exists(reportDir)) return;

                var allFiles = Directory.GetFiles(reportDir, "*.json");
                if (allFiles.Length == 0) return;

                long totalFiles = 0, totalSpace = 0;
                DateTime lastDate = DateTime.MinValue;
                long lastSpace = 0;

                foreach (var f in allFiles)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(File.ReadAllText(f));
                        var root = doc.RootElement;
                        if (root.TryGetProperty("totalFilesDeleted", out var fi)) totalFiles += fi.GetInt64();
                        if (root.TryGetProperty("totalSpaceFreed",   out var sp)) totalSpace += sp.GetInt64();
                        if (root.TryGetProperty("startTime", out var dt))
                        {
                            var d = dt.GetDateTime();
                            if (d > lastDate)
                            {
                                lastDate = d;
                                lastSpace = root.TryGetProperty("totalSpaceFreed", out var ls) ? ls.GetInt64() : 0;
                            }
                        }
                    }
                    catch { }
                }

                BtnReports.Content  = $"📊 {Core.Localizer.T("btn.reports")} ({allFiles.Length})";
                var L = Core.Localizer.T;
                TxtLastCleanup.Text = lastDate > DateTime.MinValue
                    ? $"{L("main.last.prefix")}{lastDate:dd/MM/yyyy}  \u00b7  {Core.CleaningReport.FormatBytes(lastSpace)}{L("main.last.freed")}  \u00b7  {L("main.last.cumul")}{totalFiles:N0}{L("main.last.files")}  \u00b7  {Core.CleaningReport.FormatBytes(totalSpace)}"
                    : "";
            }
            catch { /* ignore */ }
        }

        private void BtnComplete_Click(object sender, RoutedEventArgs e)
        {
            OfferForceClose(new[] { "firefox", "chrome", "msedge", "brave", "opera", "vivaldi" });
            LaunchCleaning(Core.CleaningMode.Complete);
        }

        private void BtnDeepClean_Click(object sender, RoutedEventArgs e)
        {
            if (!Confirm(Core.Localizer.T("main.deepclean.confirm")))
                return;

            OfferForceClose();
            LaunchCleaning(Core.CleaningMode.DeepClean);
        }

        private void BtnGamingDisk_Click(object sender, RoutedEventArgs e)
        {
            OfferForceClose(new[] { "steam", "steamwebhelper", "EpicGamesLauncher",
                "discord", "spotify", "obs64", "chrome", "firefox", "msedge", "brave" });

            LaunchCleaningCustomModules(new HashSet<string>
            {
                // Caches gaming (100% sûr - se recrée)
                "Steam (shader cache, logs, dumps)",
                "Epic Games (logs)",
                "Battle.net (cache)",
                "Steam cache (tous disques)",
                "DirectX Shader Cache",
                "Epic Games / Battle.net",
                // Caches apps (pas l'app elle-même)
                "Discord (cache, code cache, GPU cache)",
                "Spotify (storage cache)",
                "OBS Studio (logs)",
                // Optimisation disque
                "Optimisation/TRIM tous les disques",
                "Vérification erreurs disques (chkdsk)",
                "Défragmentation intelligente (disques HDD)",
                "TRIM disques SSD",
                // Nettoyage de base sûr
                "Suppression TEMP utilisateur",
                "Suppression Windows Temp",
                "Suppression Thumbnails",
                "Corbeilles (tous disques)",
            });
        }

        private void BtnAdvanced_Click(object sender, RoutedEventArgs e)
        {
            if (!Confirm(Core.Localizer.T("main.advanced.confirm")))
                return;

            OfferForceClose();
            LaunchCleaning(Core.CleaningMode.Advanced);
        }

        private void BtnSysOpt_Click(object sender, RoutedEventArgs e)
        {
            if (Confirm(Core.Localizer.T("main.sysopt.confirm")))
                LaunchCleaning(Core.CleaningMode.SystemOptimization);
        }

        private void BtnBloatware_Click(object sender, RoutedEventArgs e)
        {
            if (Confirm(Core.Localizer.T("main.bloat.confirm")))
                LaunchCleaningCustomModules(new HashSet<string>
                {
                    "Analyse bloatwares installes",
                    "Suppression Candy Crush / Jeux King",
                    "Suppression Apps sociales (Facebook, Instagram, TikTok)",
                    "Suppression Xbox GameBar / Mixed Reality",
                    "Suppression Cortana / Bing Search",
                    "Suppression Netflix / Microsoft Solitaire",
                    "Desactivation telemetrie Windows",
                });
        }

        private void BtnThirdParty_Click(object sender, RoutedEventArgs e)
        {
            LaunchCleaningCustomModules(new HashSet<string>
            {
                "Discord (cache, code cache, GPU cache)",
                "Spotify (storage cache)",
                "Teams (cache, GPU cache)",
                "Slack (cache, code cache)",
                "OBS Studio (logs)",
                "Steam (shader cache, logs, dumps)",
                "Epic Games (logs)",
                "Battle.net (cache)",
            });
        }

        private void BtnCustom_Click(object sender, RoutedEventArgs e)
        {
            var selectionForm = new SelectionForm();
            if (selectionForm.ShowDialog() == true && selectionForm.Confirmed)
                LaunchCleaningCustomModules(selectionForm.SelectedSteps);
        }

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            var viewer = new ReportViewerForm();
            viewer.Owner = this;
            viewer.ShowDialog();
        }

        private void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            var form = new UpdateCheckForm();
            form.ShowDialog();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var form = new SettingsForm { Owner = this };
            form.ShowDialog();
            ApplyLanguage();        // rafraîchit si langue changée
            LoadLastReportStats();  // rafraîchit si rapports supprimés
        }

        // Helpers

        private void LaunchCleaning(Core.CleaningMode mode)
        {
            var form = new CleaningForm(mode);
            Hide();
            form.ShowDialog();
            Show();
            LoadLastReportStats();
        }

        private void LaunchCleaningCustomModules(HashSet<string> steps)
        {
            var form = new CleaningForm(Core.CleaningMode.Custom, steps);
            Hide();
            form.ShowDialog();
            Show();
            LoadLastReportStats();
        }

        private bool Confirm(string message)
        {
            return MessageBox.Show(message, "Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Propose de fermer les applications qui pourraient verrouiller des fichiers.
        /// Si <paramref name="processNames"/> est null, utilise la liste complete.
        /// </summary>
        private static void OfferForceClose(IEnumerable<string>? processNames = null)
        {
            var running = processNames == null
                ? Core.ProcessHelper.GetRunningApps()
                : processNames
                    .Where(n => System.Diagnostics.Process.GetProcessesByName(n).Any())
                    .ToList();

            if (running.Count == 0) return;

            var list = string.Join("\n", running.Take(10).Select(n => $"  - {n}"));
            var msg  = string.Format(Core.Localizer.T("main.forceclose.body"), list);

            var result = MessageBox.Show(msg, Core.Localizer.T("main.forceclose.title"),
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Core.ProcessHelper.KillApps(running);
                System.Threading.Thread.Sleep(600);
            }
        }
    }
}
