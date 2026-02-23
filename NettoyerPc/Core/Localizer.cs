using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace NettoyerPc.Core
{
    /// <summary>
    /// Systeme de localisation extensible.
    /// Pour ajouter une langue : appelez RegisterLanguage avec un tag BCP-47
    /// et un dictionnaire de chaines avant l'initialisation de l'application.
    /// </summary>
    public static class Localizer
    {
        private static string _lang = "fr-FR";
        public static string CurrentLanguage => _lang;
        public static bool IsEnglish => _lang == "en-US";
        public static IReadOnlyList<string> AvailableLanguages =>
            _registry.Keys.OrderBy(k => k).ToList();

        private static readonly Dictionary<string, Dictionary<string, string>> _registry =
            new Dictionary<string, Dictionary<string, string>>();

        public static void RegisterLanguage(string tag, Dictionary<string, string> strings)
            => _registry[tag] = strings;

        public static void SetLanguage(string tag)
        {
            if (!_registry.ContainsKey(tag)) return;
            _lang = tag;
            var culture = CultureInfo.GetCultureInfo(tag);
            CultureInfo.CurrentCulture   = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public static string T(string key)
        {
            if (_registry.TryGetValue(_lang, out var cur) && cur.TryGetValue(key, out var v1))
                return v1;
            if (_lang != "fr-FR" && _registry.TryGetValue("fr-FR", out var fr) && fr.TryGetValue(key, out var v2))
                return v2;
            return key;
        }

        public static string Get(string key) => T(key);

        /// <summary>
        /// Scans the Languages/ folder next to the exe for *.json files.
        /// Each file can define a new language or override keys of an existing one.
        /// JSON format:  { "_meta": { "tag": "de-DE", "name": "Deutsch" }, "key": "value" }
        /// The file name is used as tag if _meta.tag is absent.
        /// </summary>
        public static void LoadExternalLanguages()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages");
            if (!Directory.Exists(dir)) return;

            // Load fr-FR.json first so other languages can use it as fallback base
            var files = Directory.GetFiles(dir, "*.json")
                .OrderBy(f => Path.GetFileNameWithoutExtension(f) == "fr-FR" ? 0 : 1)
                .ThenBy(f => f);

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file, Encoding.UTF8);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    // Determine language tag: prefer _meta.tag, fallback to filename (e.g. de-DE.json)
                    var tag = Path.GetFileNameWithoutExtension(file);
                    if (root.TryGetProperty("_meta", out var meta) &&
                        meta.TryGetProperty("tag", out var tagEl) &&
                        tagEl.ValueKind == JsonValueKind.String)
                        tag = tagEl.GetString()!;

                    if (!_registry.TryGetValue(tag, out var dict))
                    {
                        // New language: start from fr-FR base if loaded, so missing keys fall back gracefully
                        if (_registry.TryGetValue("fr-FR", out var frBase))
                            dict = new Dictionary<string, string>(frBase);
                        else
                            dict = new Dictionary<string, string>();
                        _registry[tag] = dict;
                    }

                    // Merge all string keys (overrides built-in when same tag)
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Name == "_meta") continue;
                        if (prop.Value.ValueKind == JsonValueKind.String)
                            dict[prop.Name] = prop.Value.GetString()!;
                    }
                }
                catch { /* ignore malformed files */ }
            }
        }
    }
}
