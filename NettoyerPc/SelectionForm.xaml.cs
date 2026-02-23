using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NettoyerPc.Core;

namespace NettoyerPc
{
    public partial class SelectionForm : Window
    {
        private readonly CleaningEngine _engine;
        private readonly List<CheckBox> _allCheckBoxes = new();

        public bool Confirmed { get; private set; } = false;

        // Catégories affichées avec leur libellé et couleur
        private static readonly Dictionary<string, (string Label, string Color)> CategoryInfo = new()
        {
            ["general"]   = ("🗑 Fichiers temporaires",              "#0078D7"),
            ["dev"]       = ("💻 Caches développement",              "#6B69D6"),
            ["browser"]   = ("🌐 Navigateurs",                        "#107C10"),
            ["gaming"]    = ("🎮 Gaming",                             "#D13438"),
            ["network"]   = ("🌐 Réseau / DNS",                       "#00B4D8"),
            ["windows"]   = ("🪟 Windows",                            "#0078D7"),
            ["security"]  = ("🛡 Sécurité",                           "#D83B01"),
            ["thirdparty"]= ("📦 Applications tierces",               "#5C2D91"),
            ["sysopt"]    = ("⚙ Optimisation Système (7 étapes)",     "#107C10"),
            ["bloatware"] = ("🧹 Suppression Bloatwares",              "#D13438"),
            ["advanced"]  = ("🔥 Nettoyage Avancé / Restauration",    "#D13438"),
        };

        public SelectionForm()
        {
            InitializeComponent();
            _engine = new CleaningEngine();
            Loaded += (s, e) => BuildUI();
        }

        private void BuildUI()
        {
            var allPairs = _engine.GetAllAvailableSteps();

            // Grouper par catégorie
            var grouped = allPairs.GroupBy(p => p.Step.Category);

            foreach (var group in grouped)
            {
                var cat = group.Key ?? "general";
                if (!CategoryInfo.TryGetValue(cat, out var info))
                    info = ("📋 " + cat, "#555555");

                // En-tête de groupe
                var header = new Border
                {
                    Background = (SolidColorBrush)new BrushConverter().ConvertFrom(info.Color)!,
                    CornerRadius = new CornerRadius(6, 6, 0, 0),
                    Padding = new Thickness(14, 10, 14, 10),
                    Margin = new Thickness(0, 12, 0, 0)
                };
                var headerStack = new StackPanel { Orientation = Orientation.Horizontal };
                var catCheckBox = new CheckBox
                {
                    IsChecked = true,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0),
                    Tag = cat
                };
                catCheckBox.Checked   += CatCheckBox_Changed;
                catCheckBox.Unchecked += CatCheckBox_Changed;

                headerStack.Children.Add(catCheckBox);
                headerStack.Children.Add(new TextBlock
                {
                    Text = info.Label,
                    FontSize = 15, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center
                });
                header.Child = headerStack;
                ContentPanel.Children.Add(header);

                // Conteneur des étapes
                var box = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(info.Color)!,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(0, 0, 6, 6),
                    Padding = new Thickness(14, 8, 14, 12),
                    Tag = cat
                };
                var stepPanel = new StackPanel { Tag = cat };
                foreach (var (_, step) in group)
                {
                    var cb = new CheckBox
                    {
                        Content = step.Name,
                        IsChecked = true,
                        Margin = new Thickness(0, 4, 0, 0),
                        FontSize = 13,
                        Tag = step.Name
                    };
                    cb.Checked   += StepCheckBox_Changed;
                    cb.Unchecked += StepCheckBox_Changed;
                    stepPanel.Children.Add(cb);
                    _allCheckBoxes.Add(cb);
                }
                box.Child = stepPanel;
                ContentPanel.Children.Add(box);
            }

            UpdateCount();
        }

        private void CatCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb) return;
            var cat = cb.Tag as string;
            var isChecked = cb.IsChecked == true;
            foreach (var c in _allCheckBoxes)
            {
                var stepName = c.Tag as string;
                // Find step category
                var pairs = _engine.GetAllAvailableSteps();
                var step = pairs.FirstOrDefault(p => p.Step.Name == stepName).Step;
                if (step != null && step.Category == cat)
                    c.IsChecked = isChecked;
            }
            UpdateCount();
        }

        private void StepCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateCount();

        private void UpdateCount()
        {
            var count = _allCheckBoxes.Count(c => c.IsChecked == true);
            SelectionCount.Text = $"{count} opération(s) sélectionnée(s)";
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in _allCheckBoxes) cb.IsChecked = true;
            UpdateCount();
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in _allCheckBoxes) cb.IsChecked = false;
            UpdateCount();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            var selected = _allCheckBoxes.Where(c => c.IsChecked == true)
                                         .Select(c => c.Tag as string ?? "")
                                         .Where(s => !string.IsNullOrEmpty(s))
                                         .ToHashSet();

            if (selected.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner au moins une opération.",
                    "Aucune sélection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Passer les sélections à CleaningForm via la propriété SelectedStepNames
            Confirmed = true;
            SelectedSteps = selected;
            DialogResult = true;
            Close();
        }

        public HashSet<string> SelectedSteps { get; private set; } = new();

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }
    }
}
