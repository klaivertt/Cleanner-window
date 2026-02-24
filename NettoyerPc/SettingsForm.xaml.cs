using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NettoyerPc.Core;

namespace NettoyerPc
{
    public partial class SettingsForm : Window
    {
        // ── Chemins ────────────────────────────────────────────────────────────────────
        private const string ConfigFile       = "language.cfg";
        private const string AppDataFolder    = "NettoyerPc";
        private const string CacheFolder      = "Cache";
        private const string ReportsFolder    = "Reports";

        // Empêche le SelectionChanged de se déclencher à l'initialisation
        private bool _isLoading = true;

        // ── Constructeur ───────────────────────────────────────────────────────────────
        public SettingsForm()
        {
            InitializeComponent();
            Loaded += (_, _) => Initialize();
        }

        // ── Initialisation ─────────────────────────────────────────────────────────────
        private void Initialize()
        {
            VersionText.Text = $"Version {AppConstants.AppVersion}";
            LoadLanguageCombo();
            RefreshDataPage();
            LoadGeneralSettings();
            BuildAppsPage();
            ApplyLanguage();
            _isLoading = false;

            // Page par défaut
            ShowPage(PageGeneral, NavGeneral);
        }

        private void LoadLanguageCombo()
        {
            var saved = GetSavedLanguage();
            foreach (ComboBoxItem item in LanguageCombo.Items)
            {
                if (item.Tag?.ToString() == saved)
                {
                    LanguageCombo.SelectedItem = item;
                    return;
                }
            }
            LanguageCombo.SelectedIndex = 0;
        }

        private void RefreshDataPage()
        {
            UpdateCacheSize();
            LoadCumulativeStats();
        }

        // ── Navigation latérale ────────────────────────────────────────────────────────
        private void NavGeneral_Click(object sender, RoutedEventArgs e) => ShowPage(PageGeneral, NavGeneral);
        private void NavData_Click   (object sender, RoutedEventArgs e) { RefreshDataPage(); ShowPage(PageData, NavData); }
        private void NavAbout_Click  (object sender, RoutedEventArgs e) => ShowPage(PageAbout, NavAbout);
        private void NavApps_Click   (object sender, RoutedEventArgs e) => ShowPage(PageApps,  NavApps);
        private void NavHelp_Click   (object sender, RoutedEventArgs e) => ShowPage(PageHelp,  NavHelp);

        private void ShowPage(StackPanel page, Button navBtn)
        {
            PageGeneral.Visibility = PageData.Visibility = PageAbout.Visibility = PageApps.Visibility = PageHelp.Visibility = Visibility.Collapsed;
            page.Visibility = Visibility.Visible;

            var transparent = new SolidColorBrush(Colors.Transparent);
            var active      = new SolidColorBrush(Color.FromRgb(26, 26, 48));

            NavGeneral.Background = transparent;
            NavData.Background    = transparent;
            NavAbout.Background   = transparent;
            NavApps.Background    = transparent;
            NavHelp.Background    = transparent;
            navBtn.Background     = active;
        }

        // ── Changement de langue ───────────────────────────────────────────────────────
        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            var item = LanguageCombo.SelectedItem as ComboBoxItem;
            var tag  = item?.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            // Ne rien faire si c'est déjà la langue actuelle
            if (tag == GetSavedLanguage()) return;

            SaveLanguagePreference(tag);
            Localizer.SetLanguage(tag);

            // Appliquer sur cette fenêtre
            ApplyLanguage();
            // Appliquer sur la fenêtre principale
            (Owner as MainForm)?.ApplyLanguage();

            SetStatus("✓ " + Localizer.T("lang.hint"));
        }

        // ── Traduction de cette fenêtre ────────────────────────────────────────────────
        public void ApplyLanguage()
        {
            var L = Localizer.T;

            this.Title              = L("settings.caption");
            TxtWindowCaption.Text   = L("settings.caption");
            TxtSidebarTitle.Text    = L("btn.settings");

            NavGeneral.Content = L("nav.general");
            NavData.Content    = L("nav.data");
            NavAbout.Content   = L("nav.about");
            NavApps.Content    = L("nav.apps");
            NavHelp.Content    = L("nav.help");

            // Page Général
            TxtPageGeneral.Text         = L("page.general.title");
            TxtSettingsSectionTitle.Text = L("settings.section.title");
            TxtAutoRestoreLabel.Text    = L("settings.autorestore.label");
            TxtAutoRestoreDesc.Text     = L("settings.autorestore.desc");
            TxtAutoOpenLabel.Text       = L("settings.autoopen.label");
            TxtAutoOpenDesc.Text        = L("settings.autoopen.desc");
            TxtSkipDisabledLabel.Text   = L("settings.skipdisabled.label");
            TxtSkipDisabledDesc.Text    = L("settings.skipdisabled.desc");
            TxtLangSectionTitle.Text    = L("lang.section.title");
            TxtLangDesc.Text            = L("lang.description");
            TxtLangHint.Text            = "✓ " + L("lang.hint");

            // Options avancées
            TxtAdvancedSectionTitle.Text = L("settings.advanced.title");
            TxtVerboseLabel.Text         = L("settings.verbose.label");
            TxtVerboseDesc.Text          = L("settings.verbose.desc");
            TxtConfirmCleanLabel.Text    = L("settings.confirmclean.label");
            TxtConfirmCleanDesc.Text     = L("settings.confirmclean.desc");
            TxtSoundNotifsLabel.Text     = L("settings.soundnotifs.label");
            TxtSoundNotifsDesc.Text      = L("settings.soundnotifs.desc");

            // Page Help
            TxtPageHelp.Text = L("page.help.title");

            // Page Données
            TxtPageData.Text            = L("page.data.title");
            TxtStatsSectionTitle.Text   = L("stats.section.title");
            TxtReportsSectionTitle.Text = L("reports.section.title");
            TxtCacheSectionTitle.Text   = L("cache.section.title");
            BtnClearReports.Content     = L("btn.clear.reports");
            BtnClearCache.Content       = L("btn.clear.cache");
            RunReportsHint1.Text        = L("reports.hint");
            RunReportsHint2.Text        = L("reports.hint2");
            RunCacheHint.Text           = L("cache.hint");

            // Page Applications
            TxtPageApps.Text  = L("page.apps.title");
            TxtAppsIntro.Text = L("apps.intro");

            // Page À propos
            TxtPageAbout.Text           = L("page.about.title");
            TxtAboutDeveloperLabel.Text = L("about.developer");
            TxtAboutFrameworkLabel.Text = L("about.framework");
            TxtAboutFrameworkVal.Text   = L("about.framework.val");
            TxtAboutTargetLabel.Text    = L("about.target");
            TxtAboutTargetVal.Text      = L("about.target.val");
            TxtAboutRightsLabel.Text    = L("about.rights");
            TxtAboutRightsVal.Text      = L("about.rights.val");
            TxtAboutDescription.Text    = L("about.description");

            // Pied de page
            BtnClose.Content = L("btn.close");
        }

        // ── Statistiques cumulées ─────────────────────────────────────────────────────
        private void LoadCumulativeStats()
        {
            var reportsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ReportsFolder);
            var files = Directory.Exists(reportsPath)
                ? Directory.GetFiles(reportsPath, "*.json")
                : Array.Empty<string>();

            int   cleanCount   = files.Length;
            long  totalFiles   = 0;
            long  totalSpace   = 0;
            long  totalSeconds = 0;
            DateTime lastDate  = DateTime.MinValue;
            long  lastSpace    = 0;

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("totalFilesDeleted", out var f))  totalFiles   += f.GetInt64();
                    if (root.TryGetProperty("totalSpaceFreed",   out var s))  totalSpace   += s.GetInt64();
                    if (root.TryGetProperty("startTime",         out var dt))
                    {
                        var d = dt.GetDateTime();
                        if (d > lastDate)
                        {
                            lastDate = d;
                            lastSpace = root.TryGetProperty("totalSpaceFreed", out var ls)
                                ? ls.GetInt64() : 0;
                        }
                    }
                    if (root.TryGetProperty("startTime", out var t1) &&
                        root.TryGetProperty("endTime",   out var t2))
                    {
                        totalSeconds += (long)(t2.GetDateTime() - t1.GetDateTime()).TotalSeconds;
                    }
                }
                catch { /* rapport corrompu, on ignore */ }
            }

            // Formatage durée totale
            var dur = TimeSpan.FromSeconds(totalSeconds);
            var durStr = dur.TotalHours >= 1
                ? $"{(int)dur.TotalHours}h {dur.Minutes}m"
                : dur.TotalMinutes >= 1
                    ? $"{(int)dur.TotalMinutes}m {dur.Seconds}s"
                    : $"{totalSeconds}s";

            // Affichage
            StatCleanCount.Text    = cleanCount.ToString("N0");
            StatFilesTotal.Text    = totalFiles.ToString("N0");
            StatSpaceTotal.Text    = FormatBytes(totalSpace);
            StatDurationTotal.Text = durStr;

            if (lastDate > DateTime.MinValue)
            {
                StatLastClean.Text = $"{lastDate:dddd dd MMMM yyyy}  à  {lastDate:HH:mm}";
                StatLastSpace.Text = FormatBytes(lastSpace);
            }
            else
            {
                StatLastClean.Text = Localizer.T("stats.none");
                StatLastSpace.Text = "";
            }

            // Badge rapports
            ReportCountBadge.Text = cleanCount == 0
                ? Localizer.T("report.subtitle.none")
                : $"{cleanCount} {Localizer.T("report.subtitle.count").TrimStart()}";
        }

        // ── Cache ──────────────────────────────────────────────────────────────────────
        private void UpdateCacheSize()
        {
            var path = GetCachePath();
            if (Directory.Exists(path))
            {
                var size = CalculateDirectorySize(new DirectoryInfo(path));
                CacheSizeBadge.Text = size > 0 ? FormatBytes(size) : "Vide";
            }
            else
            {
                CacheSizeBadge.Text = "Vide";
            }
        }

        private void BtnClearCache_Click(object sender, RoutedEventArgs e)
        {
            var L = Localizer.T;
            if (MessageBox.Show(
                    L("confirm.clearcache.body"),
                    L("confirm.clearcache.title"),
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                var path = GetCachePath();
                if (Directory.Exists(path)) Directory.Delete(path, true);
                SetStatus(L("status.cache.cleared"));
            }
            catch (Exception ex)
            {
                SetStatus(L("status.cache.error") + ex.Message);
            }
            finally
            {
                UpdateCacheSize();
            }
        }

        // ── Rapports ───────────────────────────────────────────────────────────────────
        private void BtnClearReports_Click(object sender, RoutedEventArgs e)
        {
            var L = Localizer.T;
            var path  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ReportsFolder);
            var files = Directory.Exists(path) ? Directory.GetFiles(path, "*.json") : Array.Empty<string>();

            if (files.Length == 0)
            {
                SetStatus(L("status.reports.none"));
                return;
            }

            if (MessageBox.Show(
                    string.Format(L("confirm.clearreports.body"), files.Length),
                    L("confirm.clearreports.title"),
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            int deleted = 0;
            foreach (var f in files)
            {
                try { File.Delete(f); deleted++; }
                catch { /* fichier verrouillé, on continue */ }
            }

            LoadCumulativeStats();
            SetStatus(string.Format(L("status.reports.cleared"), deleted));
        }

        // ── Paramètres généraux (toggles) ─────────────────────────────────────────────
        private void LoadGeneralSettings()
        {
            var prefs = UserPreferences.Current;
            _isLoading = true;
            ChkAutoRestore.IsChecked   = prefs.AutoRestorePoint;
            ChkAutoOpen.IsChecked      = prefs.AutoOpenReport;
            ChkSkipDisabled.IsChecked  = prefs.SkipDisabledApps;
            ChkVerbose.IsChecked       = prefs.VerboseMode;
            ChkConfirmClean.IsChecked  = prefs.ShowPreCleanSummary;
            ChkSoundNotifs.IsChecked   = prefs.PlaySoundOnComplete;
            _isLoading = false;
        }

        private void ChkAutoRestore_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            UserPreferences.Current.AutoRestorePoint = ChkAutoRestore.IsChecked == true;
            UserPreferences.Current.Save();
        }

        private void ChkAutoOpen_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            UserPreferences.Current.AutoOpenReport = ChkAutoOpen.IsChecked == true;
            UserPreferences.Current.Save();
        }

        private void ChkSkipDisabled_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            UserPreferences.Current.SkipDisabledApps = ChkSkipDisabled.IsChecked == true;
            UserPreferences.Current.Save();
        }

        private void ChkVerbose_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            UserPreferences.Current.VerboseMode = ChkVerbose.IsChecked == true;
            UserPreferences.Current.Save();
        }

        private void ChkConfirmClean_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            UserPreferences.Current.ShowPreCleanSummary = ChkConfirmClean.IsChecked == true;
            UserPreferences.Current.Save();
        }

        private void ChkSoundNotifs_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            UserPreferences.Current.PlaySoundOnComplete = ChkSoundNotifs.IsChecked == true;
            UserPreferences.Current.Save();
        }

        // ── Page Applications ──────────────────────────────────────────────────────────
        // Groupes d'apps : label JSON → liste de (clé app, nom affiché)
        private static readonly (string GroupKey, (string Key, string DisplayName)[] Apps)[] AppGroups =
        {
            ("apps.group.browsers", new[]
            {
                ("firefox",  "Firefox"),
                ("chrome",   "Google Chrome"),
                ("edge",     "Microsoft Edge"),
                ("brave",    "Brave"),
                ("opera",    "Opera / Opera GX"),
                ("vivaldi",  "Vivaldi"),
            }),
            ("apps.group.gaming", new[]
            {
                ("steam",       "Steam"),
                ("epicgames",   "Epic Games"),
                ("battlenet",   "Battle.net"),
                ("eaapp",       "EA App"),
                ("ubisoft",     "Ubisoft Connect"),
                ("gog",         "GOG Galaxy"),
                ("riot",        "Riot Client"),
                ("minecraft",   "Minecraft"),
            }),
            ("apps.group.comms", new[]
            {
                ("discord",   "Discord"),
                ("teams",     "Microsoft Teams"),
                ("slack",     "Slack"),
                ("zoom",      "Zoom"),
                ("whatsapp",  "WhatsApp"),
                ("telegram",  "Telegram"),
            }),
            ("apps.group.media", new[]
            {
                ("spotify",      "Spotify"),
                ("applemusic",   "Apple Music"),
                ("cider",        "Cider"),
                ("obs",          "OBS Studio"),
                ("streamlabs",   "Streamlabs"),
                ("twitch",       "Twitch"),
                ("vlc",          "VLC"),
                ("adobe",        "Adobe Creative Cloud"),
                ("figma",        "Figma"),
                ("notion",       "Notion"),
            }),
            ("apps.group.creative", new[]
            {
                ("unity",         "Unity"),
                ("unreal",        "Unreal Engine"),
                ("unrealddc",     "Unreal DDC (deep clean)"),
                ("godot",         "Godot"),
                ("blender",       "Blender"),
                ("davinci",       "DaVinci Resolve"),
                ("cinema4d",      "Cinema 4D"),
                ("houdini",       "Houdini"),
                ("zbrush",        "ZBrush"),
                ("substance3d",   "Substance 3D"),
                ("krita",         "Krita"),
                ("gimp",          "GIMP"),
                ("affinity",      "Affinity suite"),
                ("premierepro",   "Adobe Premiere Pro"),
                ("aftereffects",  "Adobe After Effects"),
                ("lightroom",     "Adobe Lightroom"),
                ("photoshop",     "Adobe Photoshop"),
                ("illustrator",   "Adobe Illustrator"),
                ("indesign",      "Adobe InDesign"),
                ("acrobat",       "Adobe Acrobat / Reader"),
                ("bridge",        "Adobe Bridge"),
                ("adobexd",       "Adobe XD"),
                ("mediaencoder",  "Adobe Media Encoder"),
            }),
            ("apps.group.audio", new[]
            {
                ("flstudio",     "FL Studio"),
                ("ableton",      "Ableton Live"),
                ("audacity",     "Audacity"),
                ("audition",     "Adobe Audition"),
                ("vegaspro",     "Vegas Pro"),
            }),
            ("apps.group.autodesk", new[]
            {
                ("autodesk",      "Autodesk (global)"),
                ("autocad",       "AutoCAD"),
                ("maya",          "Maya"),
                ("max3ds",        "3ds Max"),
                ("revit",         "Revit"),
                ("fusion360",     "Fusion 360"),
                ("inventor",      "Inventor"),
            }),
            ("apps.group.office", new[]
            {
                ("msoffice",     "Microsoft Office"),
                ("libreoffice",  "LibreOffice / OpenOffice"),
                ("wpsoffice",    "WPS Office"),
            }),
            ("apps.group.ides", new[]
            {
                ("vscode",        "VS Code"),
                ("jetbrains",     "JetBrains IDEs"),
                ("eclipse",       "Eclipse / NetBeans"),
                ("androidstudio", "Android Studio"),
                ("cursor",        "Cursor IDE"),
                ("visualstudio",  "Visual Studio (cache build)"),
                ("gitlogs",       "Git (nettoyage logs)"),
                ("svn",           "SVN (.svn)"),
                ("nodemodules",   "node_modules"),
                ("devcaches",     "NuGet / Gradle / Maven / npm / pip"),
            }),
        };

        private void BuildAppsPage()
        {
            AppsContainer.Children.Clear();
            var prefs = UserPreferences.Current;

            foreach (var (groupKey, apps) in AppGroups)
            {
                // En-tête de groupe
                var header = new TextBlock
                {
                    Text       = Localizer.T(groupKey),
                    FontSize   = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0xAC, 0xFF)),
                    Margin     = new Thickness(0, 10, 0, 8),
                };
                AppsContainer.Children.Add(header);

                foreach (var (key, displayName) in apps)
                {
                    var cb = new CheckBox
                    {
                        Content   = displayName,
                        Tag       = key,
                        IsChecked = prefs.IsAppEnabled(key),
                        Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
                        Margin    = new Thickness(0, 0, 0, 6),
                    };
                    AppsContainer.Children.Add(cb);
                }
            }
        }

        private void BtnSaveApps_Click(object sender, RoutedEventArgs e)
        {
            var prefs = UserPreferences.Current;
            foreach (CheckBox cb in AppsContainer.Children.OfType<CheckBox>())
            {
                if (cb.Tag is string key)
                    prefs.EnabledApps[key] = cb.IsChecked == true;
            }
            prefs.Save();
            TxtAppsSavedStatus.Text = Localizer.T("apps.saved");
        }

        // ── Bouton Fermer ──────────────────────────────────────────────────────────────
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        // ── Chrome borderless ─────────────────────────────────────────────────────────────────────
        private void Minimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object s, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void CloseWin_Click(object s, RoutedEventArgs e) => Close();
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            BtnMaximize.Content = WindowState == WindowState.Maximized ? "❑" : "☐";
        }

        // ── Helpers privés ─────────────────────────────────────────────────────────────
        private void SetStatus(string message)
        {
            StatusBar.Text = message;
        }

        private string GetCachePath() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppDataFolder, CacheFolder);

        private string GetConfigPath() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppDataFolder, ConfigFile);

        private long CalculateDirectorySize(DirectoryInfo dir)
        {
            long size = 0;
            try
            {
                foreach (var f in dir.GetFiles())         size += f.Length;
                foreach (var d in dir.GetDirectories())   size += CalculateDirectorySize(d);
            }
            catch { }
            return size;
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double val = bytes;
            int i = 0;
            while (val >= 1024 && i < units.Length - 1) { val /= 1024; i++; }
            return $"{val:0.##} {units[i]}";
        }

        private void SaveLanguagePreference(string tag)
        {
            try
            {
                Core.UserPreferences.Current.Language = tag;
                Core.UserPreferences.Current.Save();
            }
            catch { }
        }

        private void RestartApplication()
        {
            var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exe != null)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true
                });
                Application.Current.Shutdown();
            }
        }

        // ── API publique (appelée par App.xaml.cs) ─────────────────────────────────────
        public static string GetSavedLanguage()
        {
            return Core.UserPreferences.Current.Language;
        }
    }
}
