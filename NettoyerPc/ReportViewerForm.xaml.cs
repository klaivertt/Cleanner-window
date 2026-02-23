using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace NettoyerPc
{
    public partial class ReportViewerForm : Window
    {
        private readonly string _reportDir;

        public ReportViewerForm()
        {
            InitializeComponent();
            _reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            Loaded += (s, e) => LoadReportList();
        }

        private void LoadReportList()
        {
            ReportList.Items.Clear();
            ReportContent.Text = "";
            ReportTitle.Text = "Sélectionnez un rapport";

            if (!Directory.Exists(_reportDir))
            {
                ReportList.Items.Add("(Aucun rapport disponible)");
                return;
            }

            var files = Directory.GetFiles(_reportDir, "*.txt");
            Array.Sort(files, (a, b) => string.Compare(b, a, StringComparison.Ordinal)); // Plus récent en premier

            if (files.Length == 0)
            {
                ReportList.Items.Add("(Aucun rapport)");
                return;
            }

            foreach (var file in files)
                ReportList.Items.Add(Path.GetFileName(file));
        }

        private void ReportList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReportList.SelectedItem is string fileName && !fileName.StartsWith("("))
            {
                var path = Path.Combine(_reportDir, fileName);
                try
                {
                    ReportContent.Text = File.ReadAllText(path);
                    ReportTitle.Text = fileName;
                }
                catch (Exception ex)
                {
                    ReportContent.Text = $"Impossible de lire le rapport :\n{ex.Message}";
                }
            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(_reportDir))
                Process.Start("explorer.exe", _reportDir);
        }

        private void BtnDeleteReport_Click(object sender, RoutedEventArgs e)
        {
            if (ReportList.SelectedItem is string fileName && !fileName.StartsWith("("))
            {
                var result = MessageBox.Show(
                    $"Supprimer le rapport :\n{fileName} ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        File.Delete(Path.Combine(_reportDir, fileName));
                        LoadReportList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Impossible de supprimer :\n{ex.Message}");
                    }
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
