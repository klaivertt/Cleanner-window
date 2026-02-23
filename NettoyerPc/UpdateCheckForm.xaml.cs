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
            Loaded += (s, e) => CheckForUpdatesAsync();
        }

        private async void CheckForUpdatesAsync()
        {
            try
            {
                TxtCurrentVer.Text = UpdateManager.CurrentVersion.ToString();

                var progress = new Action<string>(msg =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        // Could add status message if needed
                    });
                });

                _updateInfo = await UpdateManager.CheckForUpdatesAsync(progress);

                Dispatcher.Invoke(() =>
                {
                    if (_updateInfo == null)
                    {
                        ShowUpToDate();
                    }
                    else
                    {
                        ShowUpdateAvailable();
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError(ex.Message));
            }
        }

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
                    : "Aucun changement note.";
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
                MessageBox.Show(
                    "Mise a jour telechargee avec succes !\n\n" +
                    "L'application se fermera pour installer la nouvelle version.",
                    "Mise a jour",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
            else
            {
                MessageBox.Show(
                    "Erreur lors du telechargement de la mise a jour.",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                _installing = false;
                BtnInstall.IsEnabled = true;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
