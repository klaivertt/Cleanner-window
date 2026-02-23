using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NettoyerPc.Core
{
    public class UpdateManager
    {
        // À adapter : GitHub repo owner/name
        private const string GitHubOwner = "Scryl";
        private const string GitHubRepo  = "Cleanner-window";
        private const string ApiUrl      = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";

        // Version actuelle (à synchroniser avec le .csproj)
        public static readonly Version CurrentVersion = new(2, 0, 0, 0);

        /// <summary>Infos sur une version disponible depuis GitHub.</summary>
        public record UpdateInfo(Version Version, string DownloadUrl, string ChangeLog, DateTime PublishedAt);

        /// <summary>Récupère les infos de mise à jour depuis GitHub.</summary>
        public static async Task<UpdateInfo?> CheckForUpdatesAsync(Action<string>? progress = null)
        {
            try
            {
                progress?.Invoke("Connexion à GitHub...");

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "NettoyeurPC2000");

                var response = await client.GetAsync(ApiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    progress?.Invoke("Erreur : impossible de contacter GitHub");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Récupérer le tag version
                var tagName = root.GetProperty("tag_name").GetString() ?? "v0.0.0";
                var tag = ParseVersionTag(tagName);

                if (tag == null || tag <= CurrentVersion)
                {
                    progress?.Invoke("Vous avez la dernière version");
                    return null;
                }

                // Récupérer l'URL de téléchargement (chercher l'asset .exe)
                var assetsEl = root.GetProperty("assets");
                string? exeUrl = null;
                foreach (var assetEl in assetsEl.EnumerateArray())
                {
                    var name = assetEl.GetProperty("name").GetString() ?? "";
                    if (name.EndsWith(".exe"))
                    {
                        exeUrl = assetEl.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }

                if (exeUrl == null)
                {
                    progress?.Invoke("Aucun exe trouvé dans la release");
                    return null;
                }

                var changelog = root.GetProperty("body").GetString() ?? "";
                var publishedAt = root.GetProperty("published_at").GetDateTime();

                progress?.Invoke($"Nouvelle version disponible : {tag}");
                return new UpdateInfo(tag, exeUrl, changelog, publishedAt);
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Erreur : {ex.Message}");
                return null;
            }
        }

        /// <summary>Télécharge la nouvelle version et l'installe.</summary>
        public static async Task<bool> DownloadAndInstallAsync(UpdateInfo info, Action<int>? progressPercent = null)
        {
            try
            {
                using var client = new HttpClient();
                using var response = await client.GetAsync(info.DownloadUrl);
                response.EnsureSuccessStatusCode();

                var exeName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule?.FileName) + ".exe";
                var tempExe = Path.Combine(Path.GetTempPath(), exeName);

                progressPercent?.Invoke(50);

                var content = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(tempExe, content);

                progressPercent?.Invoke(100);

                // Créer un script batch pour remplacer l'exe au prochain démarrage
                var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
                if (currentExe == null) return false;

                var batScript = Path.Combine(Path.GetTempPath(), "update_install.bat");
                var batContent = $@"
@echo off
timeout /t 2 /nobreak
taskkill /IM {Path.GetFileName(currentExe)} /F 2>nul
timeout /t 1 /nobreak
copy ""{tempExe}"" ""{currentExe}"" /y
start """" ""{currentExe}""
del ""{batScript}""
exit /b 0
";
                await File.WriteAllTextAsync(batScript, batContent);

                // Lancer le script et fermer l'app
                Process.Start(new ProcessStartInfo
                {
                    FileName = batScript,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update erreur: {ex.Message}");
                return false;
            }
        }

        /// <summary>Parse un tag version comme "v2.0.1" ou "2.0.1".</summary>
        private static Version? ParseVersionTag(string tag)
        {
            try
            {
                var cleanTag = tag.TrimStart('v', 'V');
                return Version.Parse(cleanTag);
            }
            catch
            {
                return null;
            }
        }
    }
}
