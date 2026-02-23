using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace NettoyerPc
{
    public partial class MainForm : Window
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void BtnComplete_Click(object sender, RoutedEventArgs e)
        {
            var cleaningForm = new CleaningForm(Core.CleaningMode.Complete);
            Hide();
            cleaningForm.ShowDialog();
            Show();
        }

        private void BtnDeepClean_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Le nettoyage de printemps peut prendre 60-120 minutes.\n" +
                "Un point de restauration sera créé avant de commencer.\n\n" +
                "Voulez-vous continuer ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var cleaningForm = new CleaningForm(Core.CleaningMode.DeepClean);
                Hide();
                cleaningForm.ShowDialog();
                Show();
            }
        }

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            var reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            
            if (Directory.Exists(reportDir))
            {
                try
                {
                    Process.Start("explorer.exe", reportDir);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Impossible d'ouvrir le dossier des rapports:\n{ex.Message}",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show(
                    "Aucun rapport disponible.\nLancez un nettoyage pour générer un rapport.",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}
