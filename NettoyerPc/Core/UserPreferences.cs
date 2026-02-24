using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NettoyerPc.Core
{
    /// <summary>
    /// Préférences utilisateur persistées en JSON dans %AppData%\NettoyerPc\prefs.json.
    /// Remplace l'ancien language.cfg et centralise toutes les préférences.
    /// </summary>
    public class UserPreferences
    {
        // ── Options JSON ────────────────────────────────────────────────────────
        private static readonly JsonSerializerOptions _opts =
            new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

        private static readonly string _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NettoyerPc", "prefs.json");

        // ── Propriétés sauvegardées ─────────────────────────────────────────────
        /// <summary>Tag BCP-47 de la langue active (ex: "fr-FR", "en-US")</summary>
        public string Language        { get; set; } = "fr-FR";

        /// <summary>Position et taille de la fenêtre principale</summary>
        public double WindowLeft      { get; set; } = double.NaN;
        public double WindowTop       { get; set; } = double.NaN;
        public double WindowWidth     { get; set; } = 960;
        public double WindowHeight    { get; set; } = 820;
        public bool   WindowMaximized { get; set; } = false;

        /// <summary>Dernière sélection dans SelectionForm (nom d'étape → coché)</summary>
        public Dictionary<string, bool> SelectionState { get; set; } = new();

        /// <summary>Applications activées pour le nettoyage (clé → true = l'utilisateur utilise cette app)</summary>
        public Dictionary<string, bool> EnabledApps { get; set; } = DefaultEnabledApps();

        /// <summary>Crée automatiquement un point de restauration avant les nettoyages profonds</summary>
        public bool AutoRestorePoint { get; set; } = true;

        /// <summary>Ouvre le rapport automatiquement après le nettoyage</summary>
        public bool AutoOpenReport { get; set; } = true;

        /// <summary>Ignore les étapes des apps désactivées (gain de temps + rapport précis)</summary>
        public bool SkipDisabledApps { get; set; } = true;

        /// <summary>Mode verbeux : affiche tous les détails dans les rapports</summary>
        public bool VerboseMode { get; set; } = false;

        /// <summary>Affiche un résumé avant de démarrer le nettoyage</summary>
        public bool ShowPreCleanSummary { get; set; } = true;

        /// <summary>Joue un son à la fin du nettoyage</summary>
        public bool PlaySoundOnComplete { get; set; } = true;

        private static Dictionary<string, bool> DefaultEnabledApps() => new()
        {
            // Navigateurs
            ["firefox"]   = true,  ["chrome"]    = true,  ["edge"]      = true,
            ["brave"]     = false, ["opera"]     = false, ["vivaldi"]   = false,
            // Gaming
            ["steam"]     = true,  ["epicgames"] = false, ["battlenet"] = false,
            ["eaapp"]     = false, ["ubisoft"]   = false, ["gog"]       = false,
            ["riot"]      = false, ["minecraft"] = false,
            // Communication & Productivité
            ["discord"]   = true,  ["teams"]     = false, ["slack"]     = false,
            ["zoom"]      = false, ["whatsapp"]  = false, ["telegram"]  = false,
            // Médias & Création
            ["spotify"]   = false, ["obs"]       = false, ["twitch"]    = false,
            ["vlc"]       = false, ["adobe"]     = false, ["figma"]     = false,
            ["notion"]    = false,
        };

        /// <summary>Retourne true si l'app est activée (fallback à true si clé absente = toujours exécuté)</summary>
        public bool IsAppEnabled(string key) =>
            !EnabledApps.TryGetValue(key, out var enabled) || enabled;

        // ── Singleton ──────────────────────────────────────────────────────────
        private static UserPreferences? _instance;
        public static UserPreferences Current => _instance ??= Load();

        // ── Chargement ─────────────────────────────────────────────────────────
        public static UserPreferences Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var json   = File.ReadAllText(_path, System.Text.Encoding.UTF8);
                    var loaded = JsonSerializer.Deserialize<UserPreferences>(json, _opts);
                    if (loaded != null)
                    {
                        _instance = loaded;
                        return loaded;
                    }
                }
            }
            catch { /* fichier corrompu → recréer */ }

            var fresh = new UserPreferences();
            MigrateLegacy(fresh);
            _instance = fresh;
            return fresh;
        }

        /// <summary>Migre depuis l'ancien language.cfg si prefs.json absent.</summary>
        private static void MigrateLegacy(UserPreferences prefs)
        {
            try
            {
                // Emplacement utilisé par l'ancienne version
                var legacy = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "NettoyerPc", "NettoyerPc", "language.cfg");
                if (File.Exists(legacy))
                {
                    var tag = File.ReadAllText(legacy).Trim();
                    if (!string.IsNullOrEmpty(tag)) prefs.Language = tag;
                }
            }
            catch { }
        }

        // ── Sauvegarde ─────────────────────────────────────────────────────────
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllText(_path, JsonSerializer.Serialize(this, _opts),
                                  System.Text.Encoding.UTF8);
            }
            catch { /* silently ignore write errors */ }
        }

        /// <summary>Réinitialise le singleton (utile pour les tests).</summary>
        public static void Reset() => _instance = null;
    }
}
