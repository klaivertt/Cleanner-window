using System;
using System.Windows;
using System.Windows.Threading;
using NettoyerPc.Core;

namespace NettoyerPc
{
    public partial class UpdateCheckForm : Window
    {
        private UpdateManager.UpdateInfo? _updateInfo;
        private bool _installing = false;

        public UpdateCheckForm()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            Loaded += (s, e) => { ApplyLanguage(); CheckForUpdatesAsync(); };
        }

        private void ApplyLanguage()
        {
            var L = Core.Localizer.T;
            Title                = L("update.window.title");
            TxtUpdateHeader.Text = L("update.header");
            TxtVerifying.Text    = L("update.checking");
            TxtUpToDateTitle.Text= L("update.uptodate.title");
            TxtUpToDateBody.Text = L("update.uptodate.body");
            TxtAvailableTitle.Text  = L("update.available.title");
            TxtLabelCurrent.Text    = L("update.label.current");
            TxtLabelNew.Text        = L("update.label.new");
            TxtChangelogLabel.Text  = L("update.changelog.title");
            TxtChangelog.Text       = L("update.changelog.loading");
            TxtErrorTitle.Text      = L("update.error.title");
            BtnInstall.Content      = L("update.btn.install");
            BtnClose.Content        = L("btn.close");
            ChkPreRelease.Content   = L("update.prerelease.checkbox");
            TxtPreReleaseBadge.Text = L("update.prerelease.badge");
        }

        private async void CheckForUpdatesAsync()
        {
            // Réinitialiser l'UI
            VerifyingPanel.Visibility       = Visibility.Visible;
            UpToDatePanel.Visibility        = Visibility.Collapsed;
            UpdateAvailablePanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility           = Visibility.Collapsed;
            BtnInstall.Visibility           = Visibility.Collapsed;

            bool includePreRelease = ChkPreRelease.IsChecked == true;

            try
            {
                TxtCurrentVer.Text = UpdateManager.CurrentVersion.ToString();

                _updateInfo = await UpdateManager.CheckForUpdatesAsync(null, includePreRelease);

                Dispatcher.Invoke(() =>
                {
                    if (_updateInfo == null) ShowUpToDate();
                    else                     ShowUpdateAvailable();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError(ex.Message));
            }
        }

        /// <summary>Relance la vérification quand l'option pre-release change.</summary>
        private void ChkPreRelease_Changed(object sender, System.Windows.RoutedEventArgs e)
            => CheckForUpdatesAsync();

        private void ShowUpToDate()
        {
            VerifyingPanel.Visibility = Visibility.Collapsed;
            UpToDatePanel.Visibility  = Visibility.Visible;
            UpdateAvailablePanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            BtnInstall.Visibility = Visibility.Collapsed;
        }

        private void ShowUpdateAvailable()
        {
            VerifyingPanel.Visibility = Visibility.Collapsed;
            UpToDatePanel.Visibility  = Visibility.Collapsed;
            UpdateAvailablePanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            BtnInstall.Visibility = Visibility.Visible;

            if (_updateInfo != null)
            {
                TxtNewVer.Text = _updateInfo.Version.ToString();
                TxtChangelog.Text = _updateInfo.ChangeLog.Length > 0
                    ? _updateInfo.ChangeLog
                    : Core.Localizer.T("update.changelog.none");

                // Afficher le badge pre-release si applicable
                PreReleaseBadge.Visibility = _updateInfo.IsPreRelease
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // Date de publication
                string date = _updateInfo.PublishedAt.ToString("dd/MM/yyyy");
                TxtAvailableTitle.Text = _updateInfo.IsPreRelease
                    ? Core.Localizer.T("update.available.prerelease").Replace("{0}", date)
                    : Core.Localizer.T("update.available.withdate").Replace("{0}", date);
            }
        }

        private void ShowError(string message)
        {
            VerifyingPanel.Visibility = Visibility.Collapsed;
            UpToDatePanel.Visibility  = Visibility.Collapsed;
            UpdateAvailablePanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            BtnInstall.Visibility = Visibility.Collapsed;

            TxtError.Text = message;
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (_installing || _updateInfo == null) return;

            _installing = true;
            BtnInstall.IsEnabled = false;
            DownloadProgress.Visibility = Visibility.Visible;

            var progressUpdate = new Action<int>(pct =>
            {
                Dispatcher.Invoke(() =>
                {
                    DownloadProgress.Value = pct;
                });
            });

            var success = await UpdateManager.DownloadAndInstallAsync(_updateInfo, progressUpdate);

            if (success)
            {
                var L = Core.Localizer.T;
                MessageBox.Show(
                    L("update.install.ok.body"),
                    L("update.install.ok.title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
            else
            {
                var L = Core.Localizer.T;
                MessageBox.Show(
                    L("update.install.err.body"),
                    L("update.install.err.title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                _installing = false;
                BtnInstall.IsEnabled = true;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        // ── Chrome borderless ─────────────────────────────────
        private void CloseWin_Click(object s, RoutedEventArgs e) => Close();
    }
}
