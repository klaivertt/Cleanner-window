using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NettoyerPc.Core
{
    public class UpdateManager
    {
        // Version actuelle (synchronized avec AppConstants)
        public static readonly Version CurrentVersion = AppConstants.VersionNumber;

        /// <summary>Infos sur une version disponible depuis GitHub.</summary>
        public record UpdateInfo(Version Version, string DownloadUrl, string ChangeLog, DateTime PublishedAt, bool IsPreRelease = false);

        /// <summary>
        /// Récupère les infos de mise à jour depuis GitHub.
        /// <paramref name="includePreRelease"/> = true → cherche aussi les pre-releases.
        /// </summary>
        public static async Task<UpdateInfo?> CheckForUpdatesAsync(
            Action<string>? progress         = null,
            bool            includePreRelease = false)
        {
            try
            {
                progress?.Invoke("Connexion à GitHub...");

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", $"{AppConstants.AppName}/{AppConstants.AppVersion}");

                JsonElement root;

                if (includePreRelease)
                {
                    // Récupère toutes les releases (pre-releases incluses)
                    var resp = await client.GetAsync(AppConstants.GitHubApiAllReleasesUrl);
                    if (!resp.IsSuccessStatusCode) { progress?.Invoke("Erreur GitHub"); return null; }
                    var jsonArr = await resp.Content.ReadAsStringAsync();
                    using var docArr = JsonDocument.Parse(jsonArr);
                    var arr = docArr.RootElement;
                    if (arr.GetArrayLength() == 0) { progress?.Invoke("Aucune release trouvée"); return null; }
                    // Prendre la 1re release (la plus récente, triée par GitHub)
                    root = arr[0].Clone();
                }
                else
                {
                    var resp = await client.GetAsync(AppConstants.GitHubApiUrl);
                    if (!resp.IsSuccessStatusCode) { progress?.Invoke("Erreur : impossible de contacter GitHub"); return null; }
                    var json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    root = doc.RootElement.Clone();
                }

                var tagName    = root.GetProperty("tag_name").GetString() ?? "v0.0.0";
                var tag        = ParseVersionTag(tagName);
                var isPreRel   = root.TryGetProperty("prerelease", out var preProp) && preProp.GetBoolean();

                if (tag == null || CompareVersions(tag, CurrentVersion) <= 0)
                {
                    progress?.Invoke("Vous avez la dernière version");
                    return null;
                }

                // Chercher l'asset ZIP
                var assetsEl = root.GetProperty("assets");
                string? zipUrl = null;
                foreach (var assetEl in assetsEl.EnumerateArray())
                {
                    var name = assetEl.GetProperty("name").GetString() ?? "";
                    if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        zipUrl = assetEl.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }

                if (zipUrl == null) { progress?.Invoke("Aucun fichier ZIP trouvé dans la release"); return null; }

                var changelog   = root.GetProperty("body").GetString() ?? "";
                var publishedAt = root.GetProperty("published_at").GetDateTime();

                progress?.Invoke($"Nouvelle version disponible : {tag}{(isPreRel ? " (pre-release)" : "")}");
                return new UpdateInfo(tag, zipUrl, changelog, publishedAt, isPreRel);
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Erreur : {ex.Message}");
                return null;
            }
        }

        /// <summary>Télécharge le ZIP, l'extrait, et lance un script PowerShell d'auto-remplacement.</summary>
        public static async Task<bool> DownloadAndInstallAsync(UpdateInfo info, Action<int>? progressPercent = null)
        {
            var tempZip        = Path.Combine(Path.GetTempPath(), "PCClean_update.zip");
            var tempExtractDir = Path.Combine(Path.GetTempPath(), "PCClean_update_extract");

            try
            {
                using var client = new HttpClient();
                progressPercent?.Invoke(10);

                var response = await client.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                // ── Téléchargement avec progression ──────────────────────────────────
                var totalBytes = response.Content.Headers.ContentLength ?? 0L;
                await using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write))
                await using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var buffer     = new byte[81920];
                    long downloaded = 0;
                    int  read;
                    while ((read = await stream.ReadAsync(buffer)) > 0)
                    {
                        await fs.WriteAsync(buffer.AsMemory(0, read));
                        downloaded += read;
                        if (totalBytes > 0)
                            progressPercent?.Invoke(Math.Min(75, (int)(10 + downloaded * 65 / totalBytes)));
                    }
                }

                progressPercent?.Invoke(78);

                // ── Extraction ───────────────────────────────────────────────────────
                if (Directory.Exists(tempExtractDir))
                    Directory.Delete(tempExtractDir, recursive: true);
                ZipFile.ExtractToDirectory(tempZip, tempExtractDir);

                progressPercent?.Invoke(88);

                // Si le ZIP contient un unique sous-dossier, descendre dedans
                var sourceDir = tempExtractDir;
                var entries = Directory.GetFileSystemEntries(tempExtractDir);
                if (entries.Length == 1 && Directory.Exists(entries[0]))
                    sourceDir = entries[0];

                var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
                if (currentExe == null) return false;

                var appDir  = Path.GetDirectoryName(currentExe)!;
                var exeName = Path.GetFileName(currentExe);

                // ── Script PowerShell d'auto-remplacement ────────────────────────────
                var ps1Path = Path.Combine(Path.GetTempPath(), "PCClean_update.ps1");

                // Escape single-quotes for PS string literals
                var srcEsc  = sourceDir.Replace("'", "''");
                var dstEsc  = appDir.Replace("'", "''");
                var zipEsc  = tempZip.Replace("'", "''");
                var extEsc  = tempExtractDir.Replace("'", "''");
                var ps1Esc  = ps1Path.Replace("'", "''");
                var exeEsc  = exeName.Replace("'", "''");

                var ps1 = $@"
Start-Sleep -Seconds 2
$procName = [System.IO.Path]::GetFileNameWithoutExtension('{exeEsc}')
Get-Process -Name $procName -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

$src = '{srcEsc}'
$dst = '{dstEsc}'

Get-ChildItem -Path $src -Recurse | ForEach-Object {{
    $rel    = $_.FullName.Substring($src.Length).TrimStart([char]'\', [char]'/')
    $target = Join-Path $dst $rel
    if ($_.PSIsContainer) {{
        if (-not (Test-Path $target)) {{ New-Item -ItemType Directory -Path $target | Out-Null }}
    }} else {{
        $targetDir = Split-Path $target -Parent
        if (-not (Test-Path $targetDir)) {{ New-Item -ItemType Directory -Path $targetDir | Out-Null }}
        Copy-Item -Path $_.FullName -Destination $target -Force
    }}
}}

Start-Process -FilePath (Join-Path $dst '{exeEsc}')

Remove-Item -Path '{zipEsc}'  -Force              -ErrorAction SilentlyContinue
Remove-Item -Path '{extEsc}'  -Recurse -Force     -ErrorAction SilentlyContinue
Remove-Item -Path '{ps1Esc}'  -Force              -ErrorAction SilentlyContinue
";
                await File.WriteAllTextAsync(ps1Path, ps1.TrimStart());

                Process.Start(new ProcessStartInfo
                {
                    FileName  = "powershell.exe",
                    Arguments = $"-NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{ps1Path}\"",
                    UseShellExecute  = false,
                    CreateNoWindow   = true
                });

                progressPercent?.Invoke(100);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update erreur: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Parse "v0.2.0" ou "v0.2.0-beta" → Version(0,2,0).
        /// Les suffixes pre-release (-beta, -alpha, etc.) sont ignorés.
        /// </summary>
        private static Version? ParseVersionTag(string tag)
        {
            try
            {
                var clean = tag.TrimStart('v', 'V');
                var dashIdx = clean.IndexOf('-');
                if (dashIdx > 0) clean = clean[..dashIdx];
                return Version.Parse(clean);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Compare deux versions en normalisant Revision=-1 à 0,
        /// afin d'éviter que "0.2.0" soit jugé inférieur à "0.2.0.0".
        /// </summary>
        private static int CompareVersions(Version a, Version b)
        {
            var aNorm = new Version(a.Major, a.Minor, Math.Max(a.Build, 0), Math.Max(a.Revision, 0));
            var bNorm = new Version(b.Major, b.Minor, Math.Max(b.Build, 0), Math.Max(b.Revision, 0));
            return aNorm.CompareTo(bNorm);
        }
    }
}
