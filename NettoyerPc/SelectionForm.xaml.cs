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
        public HashSet<string> SelectedSteps { get; private set; } = new();

        // â”€â”€â”€ CatÃ©gories â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private static readonly Dictionary<string, (string Label, string ColorHex, bool DefaultOn)> CategoryInfo = new()
        {
            ["general"]    = ("Fichiers temporaires Windows",         "#1A6B3A", true),
            ["browser"]    = ("Cache Navigateurs",                     "#1A6B3A", true),
            ["gaming"]     = ("Gaming (shader cache, logs)",           "#1A6B3A", true),
            ["thirdparty"] = ("Applications tierces (caches/logs)",    "#1A6B3A", true),
            ["network"]    = ("Reseau / DNS",                          "#1A6B3A", true),
            ["windows"]    = ("Windows & Disque",                      "#1A4B6B", true),
            ["dev"]        = ("Caches developpement",                  "#4B4B1A", false),
            ["sysopt"]     = ("Optimisation Systeme (SFC/DISM)",       "#1A3A6B", false),
            ["security"]   = ("Securite (antivirus)",                  "#1A3A6B", true),
            ["advanced"]   = ("Nettoyage avance / Restauration",       "#4A2A00", false),
            ["bloatware"]  = ("Suppression Bloatwares (desinstalle!)", "#6B2A00", false),
        };

        // â”€â”€â”€ Descriptions par Ã©tape â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private static readonly Dictionary<string, (string Desc, string Risk, bool Safe)> StepInfo = new()
        {
            // Temporaires
            ["Suppression TEMP utilisateur"]           = ("Vide le dossier %TEMP% de votre session. Ces fichiers sont crees par Windows et les applications pour un usage temporaire. Se recree automatiquement.", "100% sur", true),
            ["Suppression Windows Temp"]               = ("Vide C:\\Windows\\Temp. Fichiers temporaires systeme. Aucun risque.", "100% sur", true),
            ["Suppression Prefetch"]                   = ("Supprime les fichiers prefetch Windows. Windows les recree apres le prochain demarrage. Peut ralentir le premier demarrage.", "Sur", true),
            ["Suppression Thumbnails"]                 = ("Supprime le cache de miniatures de l Explorateur. Se recree en quelques secondes en parcourant vos dossiers.", "100% sur", true),
            ["Rapports d'erreurs Windows (WER)"]       = ("Supprime les rapports d erreur Windows (WER) generes lors de plantages. Ces fichiers se recreent automatiquement.", "100% sur", true),
            ["Cache icones Windows"]                   = ("Supprime le cache d icones Windows (iconcache.db). Se recree automatiquement par l Explorateur.", "100% sur", true),
            ["Fichiers Crash Dumps"]                   = ("Supprime les crashdumps dans CrashDumps et Windows\\Minidump. Utile surtout apres des BSOD.", "Sur", true),

            // Navigateurs
            ["Fermeture navigateurs ouverts"]       = ("Ferme de force Chrome, Firefox, Edge, Brave, Vivaldi, Opera et Opera GX pour permettre la suppression de leurs fichiers verrous.", "100% sur", true),
            ["Nettoyage Firefox"]                      = ("Supprime le cache de Firefox (images, JS, CSS mis en cache). Vos favoris, mots de passe et historique sont conserves.", "100% sur", true),
            ["Nettoyage Chrome"]                       = ("Cache Chrome + tous les profils utilisateur. Vos favoris, extensions et mots de passe ne sont pas touches.", "100% sur", true),
            ["Nettoyage Edge"]                         = ("Cache Edge uniquement + tous les profils. Favoris et mots de passe conserves.", "100% sur", true),
            ["Nettoyage Brave"]                        = ("Cache Brave. Historique et mots de passe conserves.", "100% sur", true),
            ["Nettoyage Opera / Opera GX"]             = ("Cache Opera Stable et Opera GX. Les deux versions sont nettoyees.", "100% sur", true),
            ["Nettoyage Vivaldi"]                      = ("Cache Vivaldi. Donnees de navigation conservees.", "100% sur", true),

            // Gaming
            ["Steam cache (tous disques)"]             = ("Supprime le cache HTTP/appcache de Steam. Se recharge automatiquement. Vos jeux, saves et parametres ne sont pas touches.", "100% sur", true),
            ["DirectX Shader Cache"]                   = ("Supprime le cache de shaders DirectX/GPU (D3DSCache). Les shaders se recompilent au prochain lancement des jeux (quelques secondes). Aucun fichier de jeu touche.", "100% sur", true),
            ["Epic Games / Battle.net"]                = ("Cache et logs Epic Games Launcher et Battle.net. Les jeux eux-memes ne sont pas touches.", "100% sur", true),            ["EA App / Origin (cache, logs)"]           = ("Cache et logs EA App et Origin. Vos jeux et sauvegardes ne sont pas touches.", "100% sur", true),
            ["Ubisoft Connect (cache)"]                 = ("Cache et logs Ubisoft Connect. Vos jeux Ubisoft ne sont pas touches.", "100% sur", true),
            ["GOG Galaxy (cache)"]                     = ("Cache GOG Galaxy Launcher. Vos jeux GOG ne sont pas touches.", "100% sur", true),
            ["Riot Games / League (cache)"]             = ("Cache et logs Riot Client, League of Legends et Valorant. Vos comptes et skins ne sont pas touches.", "100% sur", true),
            ["Minecraft (logs, crash reports)"]         = ("Logs et crash reports Minecraft Java/Bedrock. Vos mondes et sauvegardes ne sont pas touches.", "100% sur", true),
            // Apps tierces
            ["Discord (cache, code cache, GPU cache)"] = ("Cache GPU et code cache Discord. Discord continue de fonctionner normalement.", "100% sur", true),
            ["Spotify (storage cache)"]                = ("Cache de stockage Spotify. La musique sera re-streamee (normal). Vos playlists et telechargements hors-ligne ne sont pas touches.", "100% sur", true),
            ["Teams (cache, GPU cache)"]               = ("Cache Teams. Teams continuera de fonctionner normalement.", "100% sur", true),
            ["Slack (cache, code cache)"]              = ("Cache Slack uniquement.", "100% sur", true),
            ["OBS Studio (logs)"]                      = ("Supprime uniquement les fichiers de log OBS. Vos scenes, configurations et enregistrements ne sont pas touches.", "100% sur", true),
            ["Steam (shader cache, logs, dumps)"]      = ("Shader cache, logs d erreur et fichiers crash dump Steam. Fichiers de jeux, saves et parametres conserves.", "100% sur", true),
            ["Epic Games (logs)"]                      = ("Fichiers de log Epic Games Launcher. Les jeux ne sont pas touches.", "100% sur", true),
            ["Battle.net (cache)"]                     = ("Cache Battle.net Launcher. Les jeux Blizzard ne sont pas touches.", "100% sur", true),            ["Zoom (cache, logs)"]                     = ("Cache et logs Zoom. Vos reunions recentes et parametres ne sont pas touches.", "100% sur", true),
            ["WhatsApp Desktop (cache)"]               = ("Cache WhatsApp Desktop. Vos messages et medias ne sont pas touches.", "100% sur", true),
            ["Telegram Desktop (cache)"]               = ("Cache media Telegram Desktop. Vos messages sont conserves dans le cloud.", "100% sur", true),
            ["Adobe Creative Cloud (cache)"]           = ("Cache Adobe (Media Cache, Logs, crash reports). Vos projets et fichiers sources ne sont pas touches.", "Sur", true),
            ["Figma (cache)"]                          = ("Cache Figma Desktop. Vos projets cloud ne sont pas affectes.", "100% sur", true),
            ["Notion (cache)"]                         = ("Cache local Notion. Vos donnees sont synchronisees dans le cloud.", "100% sur", true),
            ["Twitch (cache)"]                         = ("Cache Twitch Desktop. Vos chaines favorites et parametres ne sont pas touches.", "100% sur", true),
            ["VLC (cache, logs)"]                      = ("Cache de couverture d album et logs VLC. Vos medias ne sont pas touches.", "100% sur", true),
            // RÃ©seau
            ["Flush DNS"]                              = ("Vide le cache DNS local. Votre PC ira chercher les nouvelles adresses IP des sites. Aucun risque.", "100% sur", true),
            ["Configuration DNS Cloudflare"]           = ("Change votre DNS pour Cloudflare 1.1.1.1 (plus rapide et prive). Reversible a tout moment.", "Sur", true),
            ["Reset IP"]                               = ("Reinitialise la configuration IP locale. Utile si vous avez des problemes reseau.", "Modere", true),
            ["Reset Winsock"]                          = ("Reinitialise la pile reseau Windows. Utile en cas de problemes de connexion. Necessite un redemarrage.", "Modere", true),
            ["Vidage cache ARP"]                       = ("Vide le cache ARP (correspondances IP/MAC). Tres technique, sans risque.", "100% sur", true),

            // Windows
            ["Corbeilles (tous disques)"]              = ("Vide les corbeilles de tous les disques. Assurez-vous de ne rien vouloir recuperer avant.", "Sur", true),
            ["Journaux Windows"]                       = ("Supprime les journaux d evenements Windows (logs). N affecte pas le fonctionnement du systeme.", "100% sur", true),
            ["Nettoyage disque (cleanmgr)"]            = ("Lance le nettoyage disque Windows (cleanmgr). Supprime les fichiers systeme temporaires.", "100% sur", true),
            ["Optimisations registre"]                 = ("Nettoie les entrees orphelines du registre Windows. Un point de restauration est recommande avant.", "Modere", true),
            ["VÃ©rification disque C:"]                 = ("Lance chkdsk en lecture seule sur C:. Detecte les erreurs sans les corriger (rapide).", "100% sur", true),
            ["DÃ©fragmentation (tous disques)"]         = ("Defragmente les disques HDD et optimise les SSD (TRIM). Recommande sur les HDD pour de meilleures performances.", "Sur", true),
            ["Windows Update cache"]                   = ("Supprime les anciens fichiers de telechargement Windows Update. Liberation d espace sans risque.", "100% sur", true),
            ["DISM cleanup"]                           = ("Lance DISM /Cleanup-Image. Libere l espace occupe par les anciens composants Windows.", "Sur", true),
            ["Cache Microsoft Store"]                  = ("Reinitialise le cache du Microsoft Store. Resout les problemes de telechargement d applications.", "100% sur", true),
            ["Delivery Optimization"]                  = ("Supprime les fichiers Windows Update mis en cache pour la livraison optimisee. Peut liberer plusieurs Go.", "Sur", true),

            // Dev
            ["Scan .svn (tous disques)"]               = ("Supprime les dossiers .svn (anciens projets SVN). N affecte pas les projets actifs si vous n utilisez pas SVN.", "Modere", false),
            ["Nettoyage logs Git (tous disques)"]      = ("Supprime les fichiers de log dans les dossiers .git (pas les commits!). Vos repos Git ne sont pas modifies.", "Sur", false),
            ["Nettoyage Visual Studio (tous disques)"] = ("Supprime les dossiers .vs et fichiers de build temporaires (.obj, .pdb). Vos projets ne sont pas touches.", "Sur", false),
            ["Suppression node_modules (tous disques)"]= ("ATTENTION : supprime TOUS les dossiers node_modules trouves. Relancer 'npm install' sera necessaire dans chaque projet. Protege les chemins systeme.", "Attention", false),
            ["Caches NuGet/Gradle/Maven/npm/pip"]      = ("Supprime les caches de packages (NuGet, Gradle, Maven, npm, pip). Ils se re-telechargeront au prochain build. Peut prendre du temps.", "Modere", false),
            ["VS Code cache (tous disques)"]           = ("Supprime les caches de VS Code (cache GPU, CachedData, logs). VS Code continue de fonctionner normalement. Ne supprime PAS vos parametres ou extensions.", "100% sur", true),
            ["Docker system prune"]                    = ("Lance 'docker system prune' : supprime les images, conteneurs et volumes Docker inutilises. A utiliser avec prudence.", "Attention", false),

            // Sysopt
            ["Nettoyage packages Windows (DISM StartComponentCleanup)"] = ("Libere l espace occupe par les anciens composants Windows Update. Peut durer 15-30 min.", "Sur", false),
            ["SFC /scannow (vÃ©rification intÃ©gritÃ© fichiers)"]          = ("Verifie et repare les fichiers systeme Windows corrompu. Necessite les droits admin. Duree : 15-45 min.", "Sur", false),
            ["DISM /RestoreHealth (rÃ©paration image Windows)"]          = ("Repare l image Windows depuis Windows Update. Necessite une connexion internet. Duree : 20-60 min.", "Sur", false),
            ["Rebuild cache polices de caractÃ¨res"]                     = ("Reconstruit le cache des polices Windows. Resout les problemes d affichage des polices.", "100% sur", false),
            ["Reset rÃ©seau complet (Winsock, IP, DNS)"]                 = ("Reinitialise completement la pile reseau. Utile si vous avez des problemes internet persistants.", "Modere", false),
            ["Optimisation/TRIM tous les disques"]                      = ("Lance defrag /O sur tous les disques (TRIM pour SSD, defrag intelligente pour HDD).", "Sur", true),
            ["VÃ©rification erreurs disques (chkdsk)"]                   = ("Lance chkdsk /scan sur C: (ne bloque pas le PC, juste une analyse).", "100% sur", true),

            // Security
            ["Mise Ã  jour Windows Defender"]           = ("Met a jour les definitions antivirus de Windows Defender. Recommande.", "100% sur", true),
            ["Scan antivirus rapide"]                  = ("Lance un scan rapide avec Windows Defender.", "100% sur", true),
            ["Scan antivirus complet"]                 = ("Scan complet du systeme. Peut durer 1-4 heures.", "100% sur", false),

            // Advanced
            ["CrÃ©ation point de restauration automatique"] = ("Cree un point de restauration Windows avant le nettoyage. Permet de revenir en arriere si besoin.", "100% sur", true),
            ["Nettoyage anciens points de restauration"]   = ("Conserve seulement les 3 derniers points de restauration pour liberer de l espace.", "Sur", false),
            ["Mise Ã  jour Windows automatique"]            = ("Lance la recherche et l installation des mises a jour Windows.", "Sur", false),
            ["VÃ©rification fichiers critiques systÃ¨me (SFC)"] = ("Verification SFC via le module avance.", "Sur", false),
            ["DÃ©fragmentation intelligente (disques HDD)"] = ("Defragmentation des disques HDD detectes (evite les SSD).", "Sur", true),
            ["TRIM disques SSD"]                           = ("Envoie la commande TRIM aux SSD pour liberer les blocs inutilises.", "100% sur", true),

            // Bloatware
            ["Analyse bloatwares installÃ©s"]                              = ("Scanne les applications pre-installees par Microsoft.", "100% sur", false),
            ["Suppression Candy Crush / Jeux King"]                       = ("Desinstalle les jeux Candy Crush et autres jeux King pre-installes.", "Sur", false),
            ["Suppression Apps sociales (Facebook, Instagram, TikTok)"]   = ("Desinstalle les apps sociales pre-installees.", "Sur", false),
            ["Suppression Xbox GameBar / Mixed Reality"]                   = ("ATTENTION : supprime Xbox GameBar (Win+G) et Mixed Reality Portal. Les jeux ne sont pas affects, juste l overlay.", "Attention", false),
            ["Suppression Cortana / Bing Search"]                         = ("Desinstalle Cortana et Bing Search de la barre des taches.", "Sur", false),
            ["Suppression Netflix / Microsoft Solitaire"]                  = ("Desinstalle Netflix, Microsoft Solitaire, Clipchamp pre-installes.", "Sur", false),
            ["DÃ©sactivation tÃ©lÃ©mÃ©trie Windows"]                           = ("Desactive les services de telemetrie Microsoft (DiagTrack). Reduit les donnees envoyees a Microsoft.", "Sur", false),
        };

        public SelectionForm()
        {
            InitializeComponent();
            _engine = new CleaningEngine();
            Loaded += (s, e) => { ApplyLanguage(); BuildUI(); };
        }

        private void ApplyLanguage()
        {
            var L = Localizer.T;
            this.Title          = L("sel.window.title");
            TxtSelTitle.Text    = L("sel.title");
            TxtSelSubtitle.Text = L("sel.subtitle");
            TxtSelSafe.Text     = L("sel.subtitle.safe");
            BtnSelectAll.Content   = L("sel.select.all");
            BtnDeselectAll.Content = L("sel.deselect.all");
            BtnCancel.Content      = L("sel.cancel");
            BtnStart.Content       = L("sel.launch");
        }

        private void BuildUI()
        {
            var allPairs = _engine.GetAllAvailableSteps();
            var grouped  = allPairs.GroupBy(p => p.Step.Category ?? "general");

            foreach (var group in grouped)
            {
                var cat = group.Key;
                if (!CategoryInfo.TryGetValue(cat, out var catInfo))
                    catInfo = (cat, "#3A3A5A", true);

                var color  = (SolidColorBrush)new BrushConverter().ConvertFrom(catInfo.ColorHex)!;
                var dimClr = (SolidColorBrush)new BrushConverter().ConvertFrom(catInfo.ColorHex + "44")!;

                // â”€â”€ En-tÃªte de catÃ©gorie â”€â”€
                var header = new Border
                {
                    Background    = color,
                    CornerRadius  = new CornerRadius(8, 8, 0, 0),
                    Padding       = new Thickness(16, 10, 16, 10),
                    Margin        = new Thickness(0, 14, 0, 0)
                };
                var headerRow = new Grid();
                headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var catCB = new CheckBox
                {
                    IsChecked = catInfo.DefaultOn,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = cat
                };
                catCB.Checked   += CatCheckBox_Changed;
                catCB.Unchecked += CatCheckBox_Changed;

                var labelSP = new StackPanel { Orientation = Orientation.Horizontal };
                labelSP.Children.Add(catCB);
                labelSP.Children.Add(new TextBlock
                {
                    Text = catInfo.Label,
                    FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0)
                });
                Grid.SetColumn(labelSP, 0);

                var countTxt = new TextBlock
                {
                    Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                    FontSize = 11, VerticalAlignment = VerticalAlignment.Center
                };
                countTxt.Text = group.Count() + Core.Localizer.T("sel.cat.count");
                Grid.SetColumn(countTxt, 1);

                headerRow.Children.Add(labelSP);
                headerRow.Children.Add(countTxt);
                header.Child = headerRow;
                ContentPanel.Children.Add(header);

                // â”€â”€ Corps de catÃ©gorie â”€â”€
                var body = new Border
                {
                    Background    = new SolidColorBrush(Color.FromRgb(30, 30, 46)),
                    BorderBrush   = color,
                    BorderThickness = new Thickness(1),
                    CornerRadius  = new CornerRadius(0, 0, 8, 8),
                    Padding       = new Thickness(16, 8, 16, 12)
                };
                var stepPanel = new StackPanel { Tag = cat };

                foreach (var (_, step) in group)
                {
                    // RÃ©cupÃ©rer les infos
                    StepInfo.TryGetValue(step.Name, out var info);
                    var desc    = info.Desc ?? "Aucune description disponible.";
                    var risk    = info.Risk ?? "???";
                    var safeOn  = catInfo.DefaultOn && info.Safe;

                    // Badge couleur
                    var badgeColor = risk switch
                    {
                        "100% sur" => "#1A6B3A",
                        "Sur"      => "#1A5A4A",
                        "Modere"   => "#5A4A00",
                        "Attention"=> "#6B2A00",
                        _          => "#3A3A5A"
                    };
                    var badgeText = risk switch
                    {
                        "100% sur"  => Core.Localizer.T("sel.badge.safe100"),
                        "Sur"       => Core.Localizer.T("sel.badge.safe"),
                        "Modere"    => Core.Localizer.T("sel.badge.moderate"),
                        "Attention" => Core.Localizer.T("sel.badge.warning"),
                        _           => risk
                    };

                    // RangÃ©e d'Ã©tape
                    var row = new Border
                    {
                        Background    = new SolidColorBrush(Color.FromRgb(26, 26, 45)),
                        CornerRadius  = new CornerRadius(6),
                        Padding       = new Thickness(12, 10, 12, 10),
                        Margin        = new Thickness(0, 4, 0, 0)
                    };
                    var rowGrid = new Grid();
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    // CB
                    var cb = new CheckBox
                    {
                        IsChecked = safeOn,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 2, 12, 0),
                        Tag = step.Name
                    };
                    cb.Checked   += StepCheckBox_Changed;
                    cb.Unchecked += StepCheckBox_Changed;
                    _allCheckBoxes.Add(cb);
                    Grid.SetColumn(cb, 0);

                    // Texte
                    var txtSP = new StackPanel();
                    var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
                    titleRow.Children.Add(new TextBlock
                    {
                        Text = step.Name, FontSize = 13, FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center
                    });
                    var badge = new Border
                    {
                        Background    = (SolidColorBrush)new BrushConverter().ConvertFrom(badgeColor)!,
                        CornerRadius  = new CornerRadius(4),
                        Padding       = new Thickness(7, 1, 7, 1),
                        Margin        = new Thickness(10, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    badge.Child = new TextBlock
                    {
                        Text = badgeText, FontSize = 10, FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    };
                    titleRow.Children.Add(badge);

                    var descTxt = new TextBlock
                    {
                        Text = desc, FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 170)),
                        TextWrapping = TextWrapping.Wrap
                    };
                    txtSP.Children.Add(titleRow);
                    txtSP.Children.Add(descTxt);
                    Grid.SetColumn(txtSP, 1);

                    rowGrid.Children.Add(cb);
                    rowGrid.Children.Add(txtSP);
                    row.Child = rowGrid;
                    stepPanel.Children.Add(row);
                }

                body.Child = stepPanel;
                ContentPanel.Children.Add(body);
            }

            UpdateCount();
        }

        private void CatCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox catCb) return;
            var cat = catCb.Tag as string;
            var isOn = catCb.IsChecked == true;
            var pairs = _engine.GetAllAvailableSteps();
            foreach (var cb in _allCheckBoxes)
            {
                var name = cb.Tag as string ?? "";
                var step = pairs.FirstOrDefault(p => p.Step.Name == name).Step;
                if (step?.Category == cat)
                    cb.IsChecked = isOn;
            }
            UpdateCount();
        }

        private void StepCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateCount();

        private void UpdateCount()
        {
            var n = _allCheckBoxes.Count(c => c.IsChecked == true);
            SelectionCount.Text = n + Localizer.T("sel.ops");
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
            var selected = _allCheckBoxes
                .Where(c => c.IsChecked == true)
                .Select(c => c.Tag as string ?? "")
                .Where(s => s.Length > 0)
                .ToHashSet();

            if (selected.Count == 0)
            {
                MessageBox.Show(Core.Localizer.T("sel.nosel.body"),
                    Core.Localizer.T("sel.nosel.title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // VÃ©rifier si des opÃ©rations "Attention" sont cochÃ©es et prÃ©venir
            var attentionSteps = selected
                .Where(s => StepInfo.TryGetValue(s, out var i) && i.Risk == "Attention")
                .ToList();
            if (attentionSteps.Count > 0)
            {
                var msg = Core.Localizer.T("sel.warn.prefix") +
                          string.Join("\n", attentionSteps.Select(s => "  \u2022 " + s)) +
                          Core.Localizer.T("sel.warn.suffix");
                if (MessageBox.Show(msg, Core.Localizer.T("sel.warn.title"), MessageBoxButton.YesNo,
                        MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;
            }

            Confirmed = true;
            SelectedSteps = selected;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }

        // ── Chrome borderless ─────────────────────────────────
        private void Minimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object s, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void CloseWin_Click(object s, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            BtnMaximize.Content = WindowState == WindowState.Maximized ? "❑" : "☐";
        }
    }
}
