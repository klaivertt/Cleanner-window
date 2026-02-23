using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows;

namespace NettoyerPc
{
    public partial class MainForm : Window
    {
        public MainForm()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadLastReportStats();
        }

        private void LoadLastReportStats()
        {
            try
            {
                var reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                var latest = Directory.Exists(reportDir)
                    ? Directory.GetFiles(reportDir, "*.json").OrderByDescending(f => f).FirstOrDefault()
                    : null;

                if (latest != null)
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(latest));
                    var root = doc.RootElement;
                    var date  = root.GetProperty("startTime").GetDateTime();
                    var space = root.GetProperty("totalSpaceFreed").GetInt64();
                    var files = root.GetProperty("totalFilesDeleted").GetInt32();
                    TxtLastCleanup.Text = $"Dernier nettoyage : {date:dd/MM/yyyy à HH:mm}  |  {Core.CleaningReport.FormatBytes(space)} liberes  |  {files:N0} fichiers";

                    var count = Directory.GetFiles(reportDir, "*.json").Length;
                    BtnReports.Content = $"Rapports ({count})";
                }
            }
            catch { /* ignore — pas de rapport */ }
        }

        private void BtnComplete_Click(object sender, RoutedEventArgs e)
        {
            OfferForceClose(new[] { "firefox", "chrome", "msedge", "brave", "opera", "vivaldi" });
            LaunchCleaning(Core.CleaningMode.Complete);
        }

        private void BtnDeepClean_Click(object sender, RoutedEventArgs e)
        {
            if (!Confirm(
                "NETTOYAGE DE PRINTEMPS\n\n" +
                "Ce nettoyage inclut :\n" +
                "  - Tout le nettoyage rapide\n" +
                "  - Caches developpeur (npm, pip, gradle, NuGet)\n" +
                "  - Cache Windows Update (anciens fichiers)\n" +
                "  - Defragmentation et DISM\n" +
                "  - Navigateurs (Chrome, Firefox, Edge, Brave, Vivaldi, Opera GX)\n\n" +
                "Les caches dev (npm, pip...) se re-telechargeront au prochain build.\n" +
                "Un point de restauration sera cree automatiquement.\n\n" +
                "Duree : 60-120 minutes. Continuer ?"))
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
                // Caches gaming (100% sÃ»r - se recrÃ©e)
                "Steam (shader cache, logs, dumps)",
                "Epic Games (logs)",
                "Battle.net (cache)",
                "Steam cache (tous disques)",
                "DirectX Shader Cache",
                "Epic Games / Battle.net",
                // Caches apps (pas l'app elle-mÃªme)
                "Discord (cache, code cache, GPU cache)",
                "Spotify (storage cache)",
                "OBS Studio (logs)",
                // Optimisation disque
                "Optimisation/TRIM tous les disques",
                "VÃ©rification erreurs disques (chkdsk)",
                "DÃ©fragmentation intelligente (disques HDD)",
                "TRIM disques SSD",
                // Nettoyage de base sÃ»r
                "Suppression TEMP utilisateur",
                "Suppression Windows Temp",
                "Suppression Thumbnails",
                "Corbeilles (tous disques)",
            });
        }

        private void BtnAdvanced_Click(object sender, RoutedEventArgs e)
        {
            if (!Confirm(
                "MODE AVANCE - NETTOYAGE PROFOND\n\n" +
                "Ce mode inclut absolument tout :\n" +
                "  - Nettoyage complet + gaming + apps tierces\n" +
                "  - Optimisation systeme (SFC, DISM, reseau)\n" +
                "  - Suppression bloatwares Windows\n" +
                "  - Creation point de restauration automatique\n\n" +
                "Duree estimee : 90-180 minutes\n\n" +
                "Continuer ?"))
                return;

            OfferForceClose();
            LaunchCleaning(Core.CleaningMode.Advanced);
        }

        private void BtnSysOpt_Click(object sender, RoutedEventArgs e)
        {
            if (Confirm(
                "âš™ RÃ‰PARATION ET OPTIMISATION SYSTÃˆME\n\n" +
                "7 Ã©tapes :\n" +
                "  â€¢ SFC /scannow â€” vÃ©rifie l'intÃ©gritÃ© des fichiers Windows\n" +
                "  â€¢ DISM /RestoreHealth â€” rÃ©pare l'image Windows\n" +
                "  â€¢ DISM StartComponentCleanup â€” libÃ¨re espace WinSxS\n" +
                "  â€¢ Reset rÃ©seau complet + DNS Cloudflare 1.1.1.1\n" +
                "  â€¢ Rebuild cache polices\n" +
                "  â€¢ TRIM / DÃ©fragmentation tous les disques\n\n" +
                "â„¹ SFC et DISM peuvent durer 30 Ã  90 minutes.\n" +
                "Votre PC reste utilisable pendant l'opÃ©ration.\n\n" +
                "Continuer ?"))
                LaunchCleaning(Core.CleaningMode.SystemOptimization);
        }

        private void BtnBloatware_Click(object sender, RoutedEventArgs e)
        {
            if (Confirm(
                "âš  SUPPRESSION BLOATWARES\n\n" +
                "Les applications suivantes seront supprimÃ©es DÃ‰FINITIVEMENT :\n" +
                "  â€¢ Candy Crush et jeux King\n" +
                "  â€¢ Facebook, Instagram, TikTok\n" +
                "  â€¢ Xbox GameBar (raccourci Win+G ne fonctionnera plus)\n" +
                "  â€¢ Cortana, Bing Search\n" +
                "  â€¢ Netflix, Microsoft Solitaire, Clipchamp\n" +
                "  â€¢ Mixed Reality Portal\n" +
                "  + DÃ©sactivation de la tÃ©lÃ©mÃ©trie Microsoft\n\n" +
                "âŒ Vos jeux et applications tierces (Steam, Epic, etc.) ne sont PAS affectÃ©s.\n\n" +
                "Continuer ?"))
                LaunchCleaningCustomModules(new HashSet<string>
                {
                    "Analyse bloatwares installÃ©s",
                    "Suppression Candy Crush / Jeux King",
                    "Suppression Apps sociales (Facebook, Instagram, TikTok)",
                    "Suppression Xbox GameBar / Mixed Reality",
                    "Suppression Cortana / Bing Search",
                    "Suppression Netflix / Microsoft Solitaire",
                    "DÃ©sactivation tÃ©lÃ©mÃ©trie Windows",
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

        // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void LaunchCleaning(Core.CleaningMode mode)
        {
            var form = new CleaningForm(mode);
            Hide();
            form.ShowDialog();
            Show();
        }

        private void LaunchCleaningCustomModules(HashSet<string> steps)
        {
            var form = new CleaningForm(Core.CleaningMode.Custom, steps);
            Hide();
            form.ShowDialog();
            Show();
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
            var msg  = $"Les applications suivantes sont ouvertes :\n\n{list}\n\n" +
                       "Les fermer maintenant pour un nettoyage plus complet ?\n" +
                       "(Vous pourrez les rouvrir apres le nettoyage)";

            var result = MessageBox.Show(msg, "Applications ouvertes",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Core.ProcessHelper.KillApps(running);
                System.Threading.Thread.Sleep(600);
            }
        }
    }
}
