using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NettoyerPc.Core;

namespace NettoyerPc.Modules
{
    public class ThirdPartyAppsModule : ICleaningModule
    {
        public string Name => "Applications tierces";

        public List<CleaningStep> GetSteps(CleaningMode mode)
        {
            return new List<CleaningStep>
            {
                // ── Communication ──────────────────────────────────────────────────────
                new() { Name = "Discord (cache, code cache, GPU cache)", Category = "thirdparty" },
                new() { Name = "Teams (cache, GPU cache)",               Category = "thirdparty" },
                new() { Name = "Slack (cache, code cache)",              Category = "thirdparty" },
                new() { Name = "Zoom (cache, logs)",                     Category = "thirdparty" },
                new() { Name = "WhatsApp Desktop (cache)",               Category = "thirdparty" },
                new() { Name = "Telegram Desktop (cache)",               Category = "thirdparty" },

                // ── Gaming ─────────────────────────────────────────────────────────────
                new() { Name = "Steam (shader cache, logs, dumps)",      Category = "thirdparty" },
                new() { Name = "Epic Games (logs)",                      Category = "thirdparty" },
                new() { Name = "Battle.net (cache)",                     Category = "thirdparty" },

                // ── Médias / Streaming ─────────────────────────────────────────────────
                new() { Name = "Spotify (storage cache)",                Category = "thirdparty" },
                new() { Name = "Apple Music (cache, logs)",              Category = "thirdparty" },
                new() { Name = "Cider (cache)",                          Category = "thirdparty" },
                new() { Name = "OBS Studio (logs)",                      Category = "thirdparty" },
                new() { Name = "Streamlabs (cache, logs)",               Category = "thirdparty" },
                new() { Name = "Twitch (cache)",                         Category = "thirdparty" },
                new() { Name = "VLC (cache, logs)",                      Category = "thirdparty" },

                // ── Productivité ───────────────────────────────────────────────────────
                new() { Name = "Adobe Creative Cloud (cache)",           Category = "thirdparty" },
                new() { Name = "Microsoft Office (cache, temp, MRU)",     Category = "thirdparty" },
                new() { Name = "LibreOffice / OpenOffice (cache, temp)",  Category = "thirdparty" },
                new() { Name = "WPS Office (cache)",                      Category = "thirdparty" },
                new() { Name = "Figma (cache)",                           Category = "thirdparty" },
                new() { Name = "Notion (cache)",                          Category = "thirdparty" },

                // ── Création 3D / Jeu ─────────────────────────────────────────────────
                new() { Name = "Unity (cache, logs)",                    Category = "thirdparty" },
                new() { Name = "Unreal Engine (logs, shader cache DDC)", Category = "thirdparty" },
                new() { Name = "Godot (cache)",                          Category = "thirdparty" },
                new() { Name = "Blender (cache)",                        Category = "thirdparty" },
                new() { Name = "DaVinci Resolve (cache, logs)",          Category = "thirdparty" },

                // ── Suite Autodesk ────────────────────────────────────────────────────
                new() { Name = "Autodesk (logs, cache globaux)",         Category = "thirdparty" },
                new() { Name = "AutoCAD (cache, logs)",                  Category = "thirdparty" },
                new() { Name = "Maya (cache, logs)",                     Category = "thirdparty" },
                new() { Name = "3ds Max (cache, logs)",                  Category = "thirdparty" },
                new() { Name = "Revit (cache, journals)",                Category = "thirdparty" },
                new() { Name = "Fusion 360 (cache, logs)",               Category = "thirdparty" },
                new() { Name = "Inventor (cache, logs)",                 Category = "thirdparty" },
                // ── Logiciels créatifs 2D ─────────────────────────────────────────
                new() { Name = "Krita (cache)",                          Category = "thirdparty" },
                new() { Name = "GIMP (cache, historique)",               Category = "thirdparty" },
                new() { Name = "Affinity suite (cache — Designer, Photo, Publisher)", Category = "thirdparty" },

                // ── Logiciels créatifs 3D / VFX ──────────────────────────────────
                new() { Name = "Cinema 4D / Maxon (cache, logs)",        Category = "thirdparty" },
                new() { Name = "Houdini (cache, logs, temp)",            Category = "thirdparty" },
                new() { Name = "ZBrush (cache, temp)",                   Category = "thirdparty" },
                new() { Name = "Substance 3D (cache, logs)",             Category = "thirdparty" },

                // ── Adobe — caches disque profonds ────────────────────────────────
                // ⚠ Ne supprime QUE les caches de rendu/preview, jamais les projets.
                new() { Name = "Adobe Premiere Pro — cache média disque", Category = "thirdparty" },
                new() { Name = "Adobe After Effects — cache disque",     Category = "thirdparty" },
                new() { Name = "Adobe Lightroom — cache d'aperçus",      Category = "thirdparty" },

                // ── Musique / Audio ───────────────────────────────────────────────
                new() { Name = "FL Studio (cache, logs, crash reports)", Category = "thirdparty" },
                new() { Name = "Ableton Live (logs, crash reports)",     Category = "thirdparty" },
                new() { Name = "Audacity (logs, autosaves temporaires)", Category = "thirdparty" },
                new() { Name = "Vegas Pro (cache, logs)",                Category = "thirdparty" },

                // ── Adobe — apps individuelles (Cache Raw, miniatures, rendu — jamais les projets) ──
                new() { Name = "Adobe Photoshop (Camera Raw cache, logs)",      Category = "thirdparty" },
                new() { Name = "Adobe Illustrator (cache, logs)",               Category = "thirdparty" },
                new() { Name = "Adobe InDesign (cache, logs)",                  Category = "thirdparty" },
                new() { Name = "Adobe Acrobat / Reader (cache, logs)",          Category = "thirdparty" },
                new() { Name = "Adobe Bridge (cache miniatures, logs)",          Category = "thirdparty" },
                new() { Name = "Adobe XD (cache, logs)",                        Category = "thirdparty" },
                new() { Name = "Adobe Audition (cache média, logs)",            Category = "thirdparty" },
                new() { Name = "Adobe Media Encoder (cache, logs)",             Category = "thirdparty" },

                // ── Création jeu — Deep clean optionnel ──────────────────────────
                // ⚠ DDC Unreal : cache de compilation (peut dépasser 50 GB).
                //   Se régénère à la réouverture du projet. PAS de données perdues.
                new() { Name = "Unreal Engine — DDC global (cache compilation, peut être volumineux)", Category = "thirdparty" },            };
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if      (step.Name.StartsWith("Discord"))        CleanDiscord(step);
                else if (step.Name.StartsWith("Spotify"))        CleanSpotify(step);
                else if (step.Name.StartsWith("Apple Music"))    CleanAppleMusic(step);
                else if (step.Name.StartsWith("Cider"))          CleanCider(step);
                else if (step.Name.StartsWith("Teams"))          CleanTeams(step);
                else if (step.Name.StartsWith("Slack"))          CleanSlack(step);
                else if (step.Name.StartsWith("OBS"))            CleanOBS(step);
                else if (step.Name.StartsWith("Streamlabs"))     CleanStreamlabs(step);
                else if (step.Name.StartsWith("Steam"))          CleanSteam(step);
                else if (step.Name.StartsWith("Epic"))           CleanEpic(step);
                else if (step.Name.StartsWith("Battle"))         CleanBattleNet(step);
                else if (step.Name.StartsWith("Zoom"))           CleanZoom(step);
                else if (step.Name.StartsWith("WhatsApp"))       CleanWhatsApp(step);
                else if (step.Name.StartsWith("Telegram"))       CleanTelegram(step);
                // ── Adobe ─────────────────────────────────────────────────────────
                // ⚠ ORDRE IMPORTANT : checks spécifiques AVANT le catch-all "Adobe"
                else if (step.Name.StartsWith("Adobe Photoshop"))      CleanAdobePhotoshop(step);
                else if (step.Name.StartsWith("Adobe Illustrator"))    CleanAdobeIllustrator(step);
                else if (step.Name.StartsWith("Adobe InDesign"))       CleanAdobeInDesign(step);
                else if (step.Name.StartsWith("Adobe Acrobat"))        CleanAdobeAcrobat(step);
                else if (step.Name.StartsWith("Adobe Bridge"))         CleanAdobeBridge(step);
                else if (step.Name.StartsWith("Adobe XD"))             CleanAdobeXD(step);
                else if (step.Name.StartsWith("Adobe Audition"))       CleanAdobeAudition(step);
                else if (step.Name.StartsWith("Adobe Media Encoder"))  CleanAdobeMediaEncoder(step);
                else if (step.Name.StartsWith("Adobe Premiere"))       CleanPremierePro(step);
                else if (step.Name.StartsWith("Adobe After"))          CleanAfterEffects(step);
                else if (step.Name.StartsWith("Adobe Lightroom"))      CleanLightroom(step);
                else if (step.Name.StartsWith("Adobe"))                CleanAdobe(step);  // Adobe Creative Cloud (cache) catch-all
                // ── Suites bureautiques ──────────────────────────────────────────
                else if (step.Name.StartsWith("Microsoft Office"))     CleanMicrosoftOffice(step);
                else if (step.Name.StartsWith("LibreOffice"))          CleanLibreOffice(step);
                else if (step.Name.StartsWith("WPS Office"))           CleanWPSOffice(step);
                else if (step.Name.StartsWith("Figma"))          CleanFigma(step);
                else if (step.Name.StartsWith("Notion"))         CleanNotion(step);
                else if (step.Name.StartsWith("Twitch"))         CleanTwitch(step);
                else if (step.Name.StartsWith("VLC"))            CleanVLC(step);
                else if (step.Name.StartsWith("Unity"))          CleanUnity(step);
                else if (step.Name.StartsWith("Unreal"))         CleanUnrealEngine(step);
                else if (step.Name.StartsWith("Godot"))          CleanGodot(step);
                else if (step.Name.StartsWith("Blender"))        CleanBlender(step);
                else if (step.Name.StartsWith("DaVinci"))        CleanDaVinciResolve(step);
                else if (step.Name.StartsWith("AutoCAD"))        CleanAutoCAD(step);
                else if (step.Name.StartsWith("Maya"))           CleanMaya(step);
                else if (step.Name.StartsWith("3ds Max"))        CleanMax(step);
                else if (step.Name.StartsWith("Revit"))          CleanRevit(step);
                else if (step.Name.StartsWith("Fusion 360"))     CleanFusion360(step);
                else if (step.Name.StartsWith("Inventor"))       CleanInventor(step);
                else if (step.Name.StartsWith("Autodesk"))       CleanAutodeskGlobal(step);
                // ── 2D créatif ────────────────────────────────────────────────────
                else if (step.Name.StartsWith("Krita"))          CleanKrita(step);
                else if (step.Name.StartsWith("GIMP"))           CleanGimp(step);
                else if (step.Name.StartsWith("Affinity"))       CleanAffinity(step);
                // ── 3D / VFX ─────────────────────────────────────────────────────
                else if (step.Name.StartsWith("Cinema 4D"))      CleanCinema4D(step);
                else if (step.Name.StartsWith("Houdini"))        CleanHoudini(step);
                else if (step.Name.StartsWith("ZBrush"))         CleanZBrush(step);
                else if (step.Name.StartsWith("Substance"))      CleanSubstance3D(step);
                // ── Audio ─────────────────────────────────────────────────────────
                else if (step.Name.StartsWith("FL Studio"))      CleanFlStudio(step);
                else if (step.Name.StartsWith("Ableton"))        CleanAbleton(step);
                else if (step.Name.StartsWith("Audacity"))       CleanAudacity(step);
                else if (step.Name.StartsWith("Vegas Pro"))      CleanVegasPro(step);
                // ── Deep clean Unreal DDC ─────────────────────────────────────────
                else if (step.Name.StartsWith("Unreal Engine — DDC")) CleanUnrealDDC(step);
            }, cancellationToken);
        }

        private void CleanDiscord(CleaningStep step)
        {
            // Fermer Discord proprement avant de toucher aux caches
            KillProcess(new[] { "Discord", "DiscordPTB", "DiscordCanary" }, step);

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var roaming = Path.Combine(appData, "discord");
            ClearDirContents(Path.Combine(roaming, "Cache"), step);
            ClearDirContents(Path.Combine(roaming, "Code Cache"), step);
            ClearDirContents(Path.Combine(roaming, "GPUCache"), step);
        }

        /// <summary>Tue les processus dont le nom figure dans <paramref name="names"/> avant nettoyage.
        /// Attend 600 ms pour que les fichiers soient libérés.</summary>
        private static void KillProcess(string[] names, CleaningStep step)
        {
            bool killed = false;
            foreach (var name in names)
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try
                    {
                        p.Kill(entireProcessTree: true);
                        p.WaitForExit(3000);
                        step.AddLog($"Fermé : {p.ProcessName}.exe");
                        killed = true;
                    }
                    catch { }
                }
            }
            if (killed) System.Threading.Thread.Sleep(600);
        }

        /// <summary>Supprime tous les FICHIERS d'un dossier (et ses sous-dossiers)
        /// sans supprimer le dossier lui-même — évite les crashs d'applis Electron.</summary>
        private void ClearDirContents(string path, CleaningStep step)
        {
            if (!Directory.Exists(path)) return;
            try
            {
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        step.SpaceFreed += fi.Length;
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                        step.FilesDeleted++;
                    }
                    catch { }
                }
                foreach (var dir in Directory.GetDirectories(path))
                    try { Directory.Delete(dir, true); } catch { }
                step.AddLog($"Cache vidé : {path}");
            }
            catch { }
        }

        private void CleanSpotify(CleaningStep step)
        {
            KillProcess(new[] { "Spotify" }, step);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            DeleteDir(Path.Combine(appData, "Spotify", "Storage"), step);
            DeleteDir(Path.Combine(local,   "Spotify", "Storage"), step);
        }

        private void CleanTeams(CleaningStep step)
        {
            KillProcess(new[] { "Teams", "ms-teams" }, step);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDir(Path.Combine(appData, "Microsoft", "Teams", "Cache"), step);
            DeleteDir(Path.Combine(appData, "Microsoft", "Teams", "GPUCache"), step);
            DeleteDir(Path.Combine(appData, "Microsoft", "Teams", "Service Worker", "CacheStorage"), step);
            DeleteDir(Path.Combine(local, "Packages", "MSTeams_8wekyb3d8bbwe", "LocalCache"), step);
        }

        private void CleanSlack(CleaningStep step)
        {
            KillProcess(new[] { "slack" }, step);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            DeleteDir(Path.Combine(appData, "Slack", "Cache"), step);
            DeleteDir(Path.Combine(appData, "Slack", "Code Cache"), step);
        }

        private void CleanOBS(CleaningStep step)
        {
            KillProcess(new[] { "obs64", "obs32", "obs" }, step);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var obsLogs = Path.Combine(appData, "obs-studio", "logs");
            if (Directory.Exists(obsLogs))
            {
                foreach (var file in Directory.GetFiles(obsLogs, "*.txt"))
                {
                    try { var fi = new FileInfo(file); step.SpaceFreed += fi.Length; File.Delete(file); step.FilesDeleted++; step.AddLog($"OBS log supprimé : {Path.GetFileName(file)}"); }
                    catch { }
                }
            }
            DeleteDir(Path.Combine(appData, "obs-studio", "crashes"), step);
        }

        private void CleanSteam(CleaningStep step)
        {
            KillProcess(new[] { "steam", "steamservice" }, step);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var steamPath = Path.Combine(programFilesX86, "Steam");
            DeleteDir(Path.Combine(steamPath, "logs"), step);
            DeleteDir(Path.Combine(steamPath, "dumps"), step);
            DeleteDir(Path.Combine(steamPath, "appcache", "httpcache"), step);
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Fixed) continue;
                var lib = Path.Combine(drive.RootDirectory.FullName, "SteamLibrary", "steamapps", "shadercache");
                DeleteDir(lib, step);
                var alt = Path.Combine(drive.RootDirectory.FullName, "Steam", "steamapps", "shadercache");
                DeleteDir(alt, step);
            }
        }

        private void CleanEpic(CleaningStep step)
        {
            KillProcess(new[] { "EpicGamesLauncher", "EpicWebHelper" }, step);
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDir(Path.Combine(local, "EpicGamesLauncher", "Saved", "Logs"), step);
            DeleteDir(Path.Combine(local, "EpicGamesLauncher", "Saved", "webcache"), step);
        }

        private void CleanBattleNet(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            DeleteDir(Path.Combine(appData, "Battle.net", "Cache"), step);
        }

        private void DeleteDir(string path, CleaningStep step)
        {
            if (!Directory.Exists(path)) return;
            try
            {
                var info = new DirectoryInfo(path);
                int fileCount = 0;
                try
                {
                    foreach (var f in info.GetFiles("*", SearchOption.AllDirectories))
                    {
                        step.SpaceFreed += f.Length;
                        fileCount++;
                    }
                }
                catch { step.SpaceFreed += GetDirSize(info); }
                Directory.Delete(path, true);
                step.FilesDeleted += Math.Max(1, fileCount);
                step.AddLog($"Supprimé : {path} ({fileCount} fichier(s))");
            }
            catch { }
        }

        private long GetDirSize(DirectoryInfo dir)
        {
            long size = 0;
            try { foreach (var f in dir.GetFiles("*", SearchOption.AllDirectories)) try { size += f.Length; } catch { } }
            catch { }
            return size;
        }

        private void CleanZoom(CleaningStep step)
        {
            KillProcess(new[] { "Zoom", "ZoomIt" }, step);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDir(Path.Combine(appData, "Zoom", "logs"), step);
            DeleteDir(Path.Combine(local,   "Zoom", "logs"), step);
            var zoomData = Path.Combine(appData, "Zoom", "data");
            if (Directory.Exists(zoomData))
                foreach (var dir in Directory.GetDirectories(zoomData))
                    if (Path.GetFileName(dir).EndsWith("cache", StringComparison.OrdinalIgnoreCase))
                        DeleteDir(dir, step);
        }

        private void CleanWhatsApp(CleaningStep step)
        {
            KillProcess(new[] { "WhatsApp" }, step);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDir(Path.Combine(appData, "WhatsApp", "Cache"),       step);
            DeleteDir(Path.Combine(appData, "WhatsApp", "Code Cache"),  step);
            DeleteDir(Path.Combine(appData, "WhatsApp", "GPUCache"),    step);
            DeleteDir(Path.Combine(local, "Packages", "5319275A.WhatsAppDesktop_cv1g1gvanyjgm", "LocalCache"), step);
        }

        private void CleanTelegram(CleaningStep step)
        {
            KillProcess(new[] { "Telegram", "Updater" }, step);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var tdata = Path.Combine(appData, "Telegram Desktop", "tdata");
            DeleteDir(Path.Combine(tdata, "emoji"),                step);
            DeleteDir(Path.Combine(tdata, "user_data", "cache"),   step);
            DeleteDir(Path.Combine(tdata, "user_data", "media_cache"), step);
        }

        private void CleanAdobe(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDir(Path.Combine(appData, "Adobe", "Common", "Media Cache Files"), step);
            DeleteDir(Path.Combine(appData, "Adobe", "Common", "Media Cache"),       step);
            DeleteDir(Path.Combine(appData, "Adobe", "CEP", "cache"),                step);
            DeleteDir(Path.Combine(appData, "Adobe", "CrashReports"),                step);
            DeleteDir(Path.Combine(local,   "Adobe", "Fonts"),                       step);
            // Per-product logs
            if (Directory.Exists(Path.Combine(appData, "Adobe")))
                foreach (var dir in Directory.GetDirectories(Path.Combine(appData, "Adobe")))
                    DeleteDir(Path.Combine(dir, "Logs"), step);
        }

        private void CleanFigma(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDir(Path.Combine(appData, "Figma", "Cache"),      step);
            DeleteDir(Path.Combine(appData, "Figma", "Code Cache"), step);
            DeleteDir(Path.Combine(appData, "Figma", "GPUCache"),   step);
            DeleteDir(Path.Combine(local,   "Figma", "Cache"),      step);
        }

        private void CleanNotion(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            DeleteDir(Path.Combine(appData, "Notion", "Cache"),      step);
            DeleteDir(Path.Combine(appData, "Notion", "Code Cache"), step);
            DeleteDir(Path.Combine(appData, "Notion", "GPUCache"),   step);
        }

        private void CleanTwitch(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDir(Path.Combine(appData, "Twitch", "Browser", "Cache"), step);
            DeleteDir(Path.Combine(appData, "Twitch", "Cache"),             step);
            DeleteDir(Path.Combine(local,   "Twitch", "Cache"),             step);
        }

        private void CleanVLC(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var vlcDir  = Path.Combine(appData, "vlc");
            if (!Directory.Exists(vlcDir)) return;
            DeleteDir(Path.Combine(vlcDir, "art", "artistalbum"), step);
            DeleteDir(Path.Combine(vlcDir, "art", "cover"),       step);
            foreach (var dir in Directory.GetDirectories(vlcDir))
                if (Path.GetFileName(dir).IndexOf("cache", StringComparison.OrdinalIgnoreCase) >= 0)
                    DeleteDir(dir, step);
        }

        // ── Musique ───────────────────────────────────────────────────────────────

        private void CleanAppleMusic(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // iTunes / Apple Music Desktop
            DeleteDir(Path.Combine(appData, "Apple Computer", "iTunes", "Album Artwork", "Cache"), step);
            DeleteDir(Path.Combine(local,   "Apple", "Apple Music", "Cache"),    step);
            DeleteDir(Path.Combine(local,   "Apple", "MobileSync", "Backup"),    step);
            // Logs
            foreach (var dir in SafeGetDirs(Path.Combine(appData, "Apple Computer")))
                DeleteDir(Path.Combine(dir, "Logs"), step);
        }

        private void CleanCider(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Cider v1 & v2 (Electron)
            DeleteDir(Path.Combine(appData, "Cider", "Cache"),      step);
            DeleteDir(Path.Combine(appData, "Cider", "Code Cache"), step);
            DeleteDir(Path.Combine(appData, "Cider", "GPUCache"),   step);
            DeleteDir(Path.Combine(local,   "Cider", "Cache"),      step);
            // Cider 2
            DeleteDir(Path.Combine(appData, "cider-client", "Cache"),      step);
            DeleteDir(Path.Combine(appData, "cider-client", "Code Cache"), step);
        }

        // ── Streaming ─────────────────────────────────────────────────────────────

        private void CleanStreamlabs(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Streamlabs OBS / Streamlabs Desktop
            DeleteDir(Path.Combine(appData, "slobs-client", "Cache"),      step);
            DeleteDir(Path.Combine(appData, "slobs-client", "Code Cache"), step);
            DeleteDir(Path.Combine(appData, "slobs-client", "GPUCache"),   step);
            var slobsLogs = Path.Combine(appData, "slobs-client", "node-obs", "logs");
            DeleteFilesInDir(slobsLogs, "*.log", step);
            DeleteDir(Path.Combine(appData, "streamlabs-obs", "Cache"),    step);
            DeleteDir(Path.Combine(local,   "Streamlabs", "Cache"),        step);
        }

        // ── Création 3D / Moteurs de jeu ─────────────────────────────────────────

        private void CleanUnity(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Unity Hub
            DeleteDir(Path.Combine(appData, "UnityHub", "Cache"),      step);
            DeleteDir(Path.Combine(appData, "UnityHub", "Code Cache"), step);
            DeleteDir(Path.Combine(local,   "Unity", "cache"),         step);
            // Éditeur Unity — cache global dans AppData
            DeleteDir(Path.Combine(appData, "Unity", "Asset Store-5.x"), step);
            var unityLocal = Path.Combine(local, "Unity");
            if (Directory.Exists(unityLocal))
                foreach (var dir in Directory.GetDirectories(unityLocal))
                    DeleteDir(Path.Combine(dir, "logs"), step);
        }

        private void CleanUnrealEngine(CleaningStep step)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Logs globaux
            DeleteFilesInDir(Path.Combine(local, "UnrealEngine", "Saved", "Logs"), "*.log", step);
            // Shader DDC global (peut être volumineux)
            DeleteDir(Path.Combine(local, "UnrealEngine", "DerivedDataCache"), step);
            // EpicGames Launcher overlay cache
            DeleteDir(Path.Combine(local, "EpicGamesLauncher", "Saved", "webcache"), step);
        }

        private void CleanGodot(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDir(Path.Combine(appData, "Godot", "cache"),        step);
            DeleteDir(Path.Combine(appData, "Godot", "shader_cache"), step);
            DeleteDir(Path.Combine(local,   "Godot", "cache"),        step);
        }

        private void CleanBlender(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var bfDir   = Path.Combine(appData, "Blender Foundation", "Blender");
            if (!Directory.Exists(bfDir)) return;
            foreach (var vDir in Directory.GetDirectories(bfDir))
            {
                // ex: 3.6, 4.0, 4.1 …
                DeleteDir(Path.Combine(vDir, "cache"),         step);
                DeleteDir(Path.Combine(vDir, "scripts", "addons", "__pycache__"), step);
            }
        }

        private void CleanDaVinciResolve(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var bmdBase = Path.Combine(appData, "Blackmagic Design", "DaVinci Resolve");
            DeleteDir(Path.Combine(bmdBase, "Support", "CrashReports"), step);
            DeleteFilesInDir(Path.Combine(bmdBase, "Support", "Logs"), "*.log", step);
            DeleteDir(Path.Combine(local, "DaVinciResolve", "DiskCache"), step);
            // Optimised media cache sous %AppData%
            DeleteDir(Path.Combine(bmdBase, "Cache"), step);
        }

        // ── Suite Autodesk ────────────────────────────────────────────────────────

        private void CleanAutodeskGlobal(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Logs & cache communs à tous les produits Autodesk
            var adAppData = Path.Combine(appData, "Autodesk");
            if (Directory.Exists(adAppData))
                foreach (var dir in Directory.GetDirectories(adAppData))
                {
                    DeleteDir(Path.Combine(dir, "Logs"),        step);
                    DeleteDir(Path.Combine(dir, "Cache"),       step);
                    DeleteDir(Path.Combine(dir, "CrashReports"),step);
                }
            DeleteDir(Path.Combine(local, "Autodesk", "Cache"),  step);
            DeleteDir(Path.Combine(local, "Autodesk", "Logs"),   step);
        }

        private void CleanAutoCAD(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // AutoCAD stocke les recents & temp ici (toutes versions)
            foreach (var dir in SafeGetDirs(Path.Combine(appData, "Autodesk", "AutoCAD")))
            {
                DeleteDir(Path.Combine(dir, "BackupFiles"), step);
                DeleteFilesInDir(Path.Combine(dir, "Logs"), "*.log", step);
            }
            DeleteDir(Path.Combine(local, "Autodesk", "AutoCAD", "Cache"), step);
        }

        private void CleanMaya(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // Maya — logs, temp script editor
            foreach (var dir in SafeGetDirs(Path.Combine(appData, "Autodesk", "maya")))
            {
                DeleteFilesInDir(Path.Combine(dir, "logs"), "*.log", step);
                DeleteDir(Path.Combine(dir, "cache", "particles"), step);
                DeleteDir(Path.Combine(dir, "cache", "nCache"),    step);
            }
        }

        private void CleanMax(CleaningStep step)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            foreach (var dir in SafeGetDirs(Path.Combine(local, "Autodesk", "3dsMax")))
                DeleteDir(Path.Combine(dir, "ENU", "temp"), step);
        }

        private void CleanRevit(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // Journals = fichiers log de débogage
            foreach (var dir in SafeGetDirs(Path.Combine(appData, "Autodesk", "Revit")))
                DeleteFilesInDir(Path.Combine(dir, "Journals"), "*.txt", step);
            DeleteDir(Path.Combine(appData, "Autodesk", "Revit", "Autodesk Revit", "CollaborationCache"), step);
        }

        private void CleanFusion360(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDir(Path.Combine(appData, "Autodesk", "Neutron Platform", "Options"), step);
            DeleteDir(Path.Combine(local,   "Autodesk", "webdeploy", "production"), step);
            DeleteDir(Path.Combine(local,   "Fusion360"), step);
        }

        private void CleanInventor(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            foreach (var dir in SafeGetDirs(Path.Combine(appData, "Autodesk", "Inventor")))
            {
                DeleteDir(Path.Combine(dir, "Backup"), step);
                DeleteFilesInDir(dir, "*.log", step);
            }
        }

        // ── Logiciels créatifs 2D ─────────────────────────────────────────────

        private void CleanKrita(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // Cache thumbnails et rendu preview — jamais les brushes/presets/projects
            DeleteDir(Path.Combine(appData, "krita", "cache"),     step);
            DeleteFilesInDir(Path.Combine(appData, "krita"), "*.log", step);
            // Krita 5+ stocke le cache ici
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDir(Path.Combine(local, "krita", "cache"), step);
        }

        private void CleanGimp(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // GIMP 2.x — cache thumbnail et fichiers temporaires
            // ⚠ Ne pas toucher : brushes, patterns, scripts, sessions
            foreach (var ver in new[] { "2.10", "2.99", "3.0" })
            {
                var gimpDir = Path.Combine(appData, "GIMP", ver);
                DeleteDir(Path.Combine(gimpDir, "cache"),    step);
                DeleteDir(Path.Combine(gimpDir, "tmp"),      step);
                DeleteFilesInDir(gimpDir, "*.log",           step);
                // Fichiers xsm (sessions autosave temporaires — UNIQUEMENT ceux anciens de + 7j)
                // Pour ne pas supprimer une session en cours, on ne touche pas aux .xsm
            }
        }

        private void CleanAffinity(CleaningStep step)
        {
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // Affinity v1 & v2 — uniquement les caches, jamais les documents
            foreach (var product in new[] { "Designer", "Photo", "Publisher", "Designer 2", "Photo 2", "Publisher 2" })
            {
                DeleteDir(Path.Combine(local,   "Affinity", product, "1.0", "cache"), step);
                DeleteDir(Path.Combine(appData, "Affinity", product, "cache"),        step);
            }
        }

        // ── Logiciels créatifs 3D / VFX ──────────────────────────────────────────

        private void CleanCinema4D(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Maxon / Cinema 4D — cache tex, logs
            // ⚠ Ne pas toucher : scènes, matériaux, presets utilisateur
            foreach (var dir in SafeGetDirs(Path.Combine(appData, "Maxon")))
            {
                DeleteDir(Path.Combine(dir, "cache"),     step);
                DeleteDir(Path.Combine(dir, "logs"),      step);
                DeleteDir(Path.Combine(dir, "crashlogs"), step);
                DeleteFilesInDir(dir, "*.log",            step);
            }
            DeleteDir(Path.Combine(local, "Maxon", "cache"), step);
        }

        private void CleanHoudini(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // SideFX Houdini — cache et fichiers temporaires
            // ⚠ Ne pas toucher : hip files, assets, presets
            foreach (var dir in SafeGetDirs(Path.Combine(appData, "Side Effects Software")))
            {
                DeleteDir(Path.Combine(dir, "cache"),   step);
                DeleteDir(Path.Combine(dir, "temp"),    step);
                DeleteFilesInDir(dir, "*.log",          step);
            }
            // Houdini temp dans %TEMP%
            var tmp = Path.GetTempPath();
            DeleteFilesInDir(tmp, "houdini*.tmp", step);
        }

        private void CleanZBrush(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // ZBrush / Pixologic — fichiers temporaires de session et cache
            // ⚠ Ne pas toucher : ZPR/ZTL (projets), ZBP (brushes), ZPL (palettes) — tout en Documents/ZBrush
            foreach (var dir in SafeGetDirs(Path.Combine(appData, "Pixologic")))
            {
                DeleteDir(Path.Combine(dir, "cache"),  step);
                DeleteDir(Path.Combine(dir, "temp"),   step);
                DeleteFilesInDir(dir, "*.log",         step);
            }
            foreach (var dir in SafeGetDirs(Path.Combine(appData, "Maxon", "ZBrush")))
            {
                DeleteDir(Path.Combine(dir, "cache"),  step);
                DeleteFilesInDir(dir, "*.log",         step);
            }
        }

        private void CleanSubstance3D(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Adobe Substance 3D Painter / Designer / Sampler
            // ⚠ Ne pas toucher : projects, presets, exports, shelf assets
            foreach (var product in new[] { "Substance 3D Painter", "Substance 3D Designer", "Substance 3D Sampler" })
            {
                var adobeDir = Path.Combine(appData, "Adobe", product);
                DeleteDir(Path.Combine(adobeDir, "cache"),    step);
                DeleteDir(Path.Combine(adobeDir, "logs"),     step);
                // Streaming cache texture — régénéré à la réouverture du projet
                DeleteDir(Path.Combine(adobeDir, "shelf", "cache"), step);
            }
            // Substance Painter standalone (pré-Adobe)
            var oldPainter = Path.Combine(appData, "Allegorithmic");
            if (Directory.Exists(oldPainter))
                foreach (var dir in SafeGetDirs(oldPainter))
                    DeleteDir(Path.Combine(dir, "cache"), step);
        }

        // ── Adobe — caches disque profonds ────────────────────────────────────────
        // RÈGLE STRICTE : on ne touche JAMAIS aux projets, séquences vidéo,
        // exports, presets, ni autosaves. Uniquement le cache réencodé/preview.

        private void CleanPremierePro(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Cache média connu — chemin par défaut (l'utilisateur peut l'avoir changé dans Premiere)
            // Ce cache est régénéré à l'ouverture du projet.
            foreach (var ver in SafeGetDirs(Path.Combine(appData, "Adobe", "Premiere Pro")))
            {
                DeleteDir(Path.Combine(ver, "Media Cache"),        step);
                DeleteDir(Path.Combine(ver, "Media Cache Files"),  step);
                DeleteFilesInDir(ver, "*.log",                     step);
                DeleteDir(Path.Combine(ver, "Crash Reports"),      step);
            }
            // Cache sur le disque local (dossier "Common\Media Cache")
            DeleteDir(Path.Combine(appData, "Adobe", "Common", "Media Cache"),       step);
            DeleteDir(Path.Combine(appData, "Adobe", "Common", "Media Cache Files"), step);
        }

        private void CleanAfterEffects(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // Disk Cache After Effects — peut faire plusieurs Go — UNIQUEMENT le cache de rendu preview
            // ⚠ Le cache disque est configurable. On cible le chemin par défaut.
            // Les projets AEP et les renders finaux ne sont JAMAIS touchés.
            foreach (var ver in SafeGetDirs(Path.Combine(appData, "Adobe", "After Effects")))
            {
                // On supprime uniquement les sous-dossiers clairement identifiés comme cache
                DeleteDir(Path.Combine(ver, "Cache-1"),            step);  // format <= AE 2020
                DeleteDir(Path.Combine(ver, "Preview Files"),      step);
                DeleteDir(Path.Combine(ver, "Disk Cache"),         step);
                DeleteFilesInDir(ver, "*.log",                     step);
                DeleteDir(Path.Combine(ver, "Crash Reports"),      step);
            }
        }

        private void CleanLightroom(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Lightroom Classic — previews (Smart Previews conservés — contiennent des données utilisateur)
            // ⚠ On supprime uniquement les previews standard (régénérés à l'ouverture)
            //   Les catalogues (.lrcat) et Smart Previews ne sont PAS touchés.
            foreach (var dir in SafeGetDirs(Path.Combine(appData, "Adobe", "Lightroom")))
            {
                DeleteFilesInDir(dir, "*.log",             step);
                DeleteDir(Path.Combine(dir, "cache"),      step);
            }
            // Cache Lightroom Classic dans %LocalAppData%
            DeleteDir(Path.Combine(local, "Adobe", "Lightroom", "cache"), step);
        }

        // ── Audio ─────────────────────────────────────────────────────────────────

        private void CleanFlStudio(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var flDir   = Path.Combine(appData, "Image-Line", "FL Studio");
            if (!Directory.Exists(flDir)) return;
            // Logs et crash reports FL Studio — jamais les projets .flp ni les samples
            foreach (var subDir in new[] { "Logs", "Crash reports", "temp" })
                DeleteDir(Path.Combine(flDir, subDir), step);
            DeleteFilesInDir(flDir, "*.log", step);
        }

        private void CleanAbleton(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // Ableton — uniquement logs et crash reports
            // ⚠ On ne touche JAMAIS à la Library (samples, instruments, presets) :
            //   son contenu représente le travail/achat de l'utilisateur.
            foreach (var dir in SafeGetDirs(Path.Combine(appData, "Ableton")))
            {
                DeleteFilesInDir(dir, "*.log",              step);
                DeleteDir(Path.Combine(dir, "Crash Reports"), step);
                DeleteDir(Path.Combine(dir, "Cache"),         step);
            }
        }

        private void CleanAudacity(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Audacity — fichiers temporaires de session (pas les projets .aup3)
            // ⚠ Les .aup3 sont les projets — on ne supprime JAMAIS les données de session en cours
            DeleteDir(Path.Combine(appData, "audacity", "SessionData"), step);
            DeleteFilesInDir(Path.Combine(appData, "audacity"), "*.log", step);
            // Audacity crée des fichiers au (e)temp dans %TEMP%
            var tmp = Path.GetTempPath();
            foreach (var d in Directory.Exists(tmp)
                ? Directory.GetDirectories(tmp, "audacity_temp*")
                : Array.Empty<string>())
                DeleteDir(d, step);
        }

        private void CleanVegasPro(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // MAGIX Vegas Pro — cache preview et logs
            // ⚠ Ne pas toucher aux projets .veg/.vf2 ni aux renders
            foreach (var dir in SafeGetDirs(Path.Combine(appData, "MAGIX", "Vegas Pro")))
            {
                DeleteDir(Path.Combine(dir, "Cache"),  step);
                DeleteDir(Path.Combine(dir, "Logs"),   step);
                DeleteFilesInDir(dir, "*.log",         step);
            }
            foreach (var dir in SafeGetDirs(Path.Combine(appData, "Sony", "Vegas Pro")))
            {
                DeleteDir(Path.Combine(dir, "Cache"),  step);
                DeleteFilesInDir(dir, "*.log",         step);
            }
        }

        // ── Deep clean Unreal DDC ─────────────────────────────────────────────────

        private void CleanUnrealDDC(CleaningStep step)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Derived Data Cache global d'Unreal Engine (partagé entre projets)
            // ⚠ Ce cache peut dépasser 50 GB. Il est ENTIÈREMENT régénéré à la réouverture du projet.
            //   Aucune donnée de projet, de blueprint, de mesh ou de texture source n'est touchée.
            //   Seul le cache précompilé est supprimé.
            DeleteDir(Path.Combine(local, "UnrealEngine", "DerivedDataCache"), step);
            // DDC par version: UnrealEngine\Common\DerivedDataCache
            DeleteDir(Path.Combine(local, "UnrealEngine", "Common", "DerivedDataCache"), step);

            step.Status = step.SpaceFreed > 0
                ? $"DDC Unreal supprimé ({FormatBytes(step.SpaceFreed)}) — sera régénéré à la prochaine ouverture de projet."
                : "Aucun DDC Unreal trouvé";
        }

        // ── Suites bureautiques ───────────────────────────────────────────────────

        /// <summary>Supprime les fichiers temporaires, cache de correction automatique, MRU récents
        /// et thumbnails Word/Excel/PowerPoint/Outlook/OneNote.
        /// Ne touche JAMAIS les documents .docx/.xlsx/.pptx, les templates personnalisés,
        /// les macros, le carnet d'adresses ni les données Outlook (.ost/.pst).</summary>
        private void CleanMicrosoftOffice(CleaningStep step)
        {
            var appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var temp     = Path.GetTempPath();

            // Fichiers temporaires Office dans %TEMP% (~ Word {GUID}.tmp, AutoRecovery retraités)
            if (Directory.Exists(temp))
                foreach (var f in SafeGetFiles(temp, "*.tmp", SearchOption.TopDirectoryOnly))
                {
                    var fn = Path.GetFileName(f);
                    if (fn.StartsWith("~WRL") || fn.StartsWith("~WRD") || fn.StartsWith("ppt") || fn.StartsWith("~Excel"))
                        try { step.SpaceFreed += new FileInfo(f).Length; File.Delete(f); step.FilesDeleted++; } catch { }
                }

            // Cache miniatures Backstage (nouveau Documents récents)
            var msOfficeRoot = Path.Combine(appData, "Microsoft", "Office");
            DeleteDir(Path.Combine(msOfficeRoot, "Recent"), step);

            // Cache de mise en page Word
            foreach (var dir in SafeGetDirs(Path.Combine(localApp, "Microsoft", "Office", "16.0")))
                DeleteDir(Path.Combine(dir, "WordContent"), step);

            // Dossier de travail temporaire Outlook (AttachTemp / Calendrier)
            DeleteDir(Path.Combine(localApp, "Microsoft", "Windows", "Temporary Internet Files", "Content.Outlook"), step);

            // Cache OneNote
            var oneNoteCache = Path.Combine(localApp, "Microsoft", "OneNote");
            foreach (var ver in SafeGetDirs(oneNoteCache))
                DeleteDir(Path.Combine(ver, "cache"),  step);

            // Logs ClickToRun
            DeleteDir(Path.Combine(localApp, "Microsoft", "ClickToRun", "Logs"), step);
        }

        /// <summary>Cache et verrous temporaires LibreOffice / OpenOffice.
        /// Ne touche jamais les documents, macros Basic, templates (.ott/.otg) ni les extensions.</summary>
        private void CleanLibreOffice(CleaningStep step)
        {
            var appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // LibreOffice config + cache (verrous et fichiers temporaires uniquement)
            foreach (var root in new[] { appData, localApp })
            {
                var loPath = Path.Combine(root, "LibreOffice");
                foreach (var ver in SafeGetDirs(loPath))
                {
                    DeleteDir(Path.Combine(ver, "cache"),  step);
                    // Verrous seulement dans user/registrymodifications.xcu (ne pas supprimer le fichier)
                    foreach (var lockFile in SafeGetFiles(ver, ".~lock.*", SearchOption.AllDirectories))
                        try { step.SpaceFreed += new FileInfo(lockFile).Length; File.Delete(lockFile); step.FilesDeleted++; } catch { }
                }

                // OpenOffice
                var ooPath = Path.Combine(root, "OpenOffice");
                foreach (var ver in SafeGetDirs(ooPath))
                {
                    DeleteDir(Path.Combine(ver, "cache"), step);
                    foreach (var lockFile in SafeGetFiles(ver, ".~lock.*", SearchOption.AllDirectories))
                        try { step.SpaceFreed += new FileInfo(lockFile).Length; File.Delete(lockFile); step.FilesDeleted++; } catch { }
                }
            }

            // Cache de configuration partagé
            DeleteDir(Path.Combine(localApp, "LibreOffice", "cache"), step);
        }

        /// <summary>Cache WPS Office (Kingsoft).
        /// Ne touche jamais les documents, templates personnalisés ni l'historique cloud.</summary>
        private void CleanWPSOffice(CleaningStep step)
        {
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            DeleteDir(Path.Combine(localApp, "Kingsoft", "WPS Office", "cache"),  step);
            DeleteDir(Path.Combine(appData,  "Kingsoft", "WPS Office", "cache"),  step);
            DeleteDir(Path.Combine(localApp, "Kingsoft", "WPS Office", "Logs"),   step);

            // Fichiers temporaires WPS
            foreach (var root in new[] { localApp, appData })
            {
                var wps = Path.Combine(root, "Kingsoft", "WPS Office");
                if (!Directory.Exists(wps)) continue;
                foreach (var f in SafeGetFiles(wps, "*.tmp", SearchOption.AllDirectories))
                    try { step.SpaceFreed += new FileInfo(f).Length; File.Delete(f); step.FilesDeleted++; } catch { }
            }
        }

        private static IEnumerable<string> SafeGetFiles(string path, string pattern, SearchOption opt)
        {
            try { return Directory.Exists(path) ? Directory.GetFiles(path, pattern, opt) : Array.Empty<string>(); }
            catch { return Array.Empty<string>(); }
        }

        // ── Adobe — apps individuelles ────────────────────────────────────────────

        /// <summary>Camera Raw cache + logs Photoshop — jamais les projets .psd/.psb, brushes, actions ou presets</summary>
        private void CleanAdobePhotoshop(CleaningStep step)
        {
            var appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Camera Raw cache — 100% sûr, se régénère à l'ouverture d'une RAW
            DeleteDir(Path.Combine(appData,  "Adobe", "CameraRaw", "Cache"), step);
            DeleteDir(Path.Combine(localApp, "Adobe", "CameraRaw"),          step);

            // Logs & caches internes de chaque version de Photoshop
            var adobeDir = Path.Combine(appData, "Adobe");
            if (!Directory.Exists(adobeDir)) return;
            foreach (var dir in Directory.GetDirectories(adobeDir))
            {
                var n = Path.GetFileName(dir);
                if (!n.StartsWith("Adobe Photoshop")) continue;
                DeleteFilesInDir(Path.Combine(dir, "Logs"),   "*.log",  step);
                DeleteFilesInDir(Path.Combine(dir, "Logs"),   "*.txt",  step);
                DeleteDir(Path.Combine(dir, "Caches"), step);
            }
        }

        /// <summary>Cache polices & logs Illustrator — jamais les fichiers .ai ni les bibliothèques</summary>
        private void CleanAdobeIllustrator(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var adobeDir = Path.Combine(appData, "Adobe");
            if (!Directory.Exists(adobeDir)) return;
            foreach (var dir in Directory.GetDirectories(adobeDir))
            {
                var n = Path.GetFileName(dir);
                if (!n.StartsWith("Adobe Illustrator")) continue;
                DeleteDir(Path.Combine(dir, "CachedFonts"), step);
                DeleteFilesInDir(Path.Combine(dir, "Logs"), "*.log",  step);
                DeleteFilesInDir(Path.Combine(dir, "Logs"), "*.txt",  step);
            }
        }

        /// <summary>Cache de récupération & logs InDesign — jamais les projets .indd ni les liens</summary>
        private void CleanAdobeInDesign(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var adobeDir = Path.Combine(appData, "Adobe");
            if (!Directory.Exists(adobeDir)) return;
            foreach (var dir in Directory.GetDirectories(adobeDir))
            {
                var n = Path.GetFileName(dir);
                if (!n.StartsWith("InDesign")) continue;
                // CachedMediaData : données temporaires régénérables
                foreach (var localeDir in SafeGetDirs(dir))
                    DeleteDir(Path.Combine(localeDir, "CachedMediaData"), step);
                DeleteFilesInDir(Path.Combine(dir, "Logs"), "*.log", step);
            }
        }

        /// <summary>Cache & logs Acrobat / Reader — jamais les PDFs ni les favoris</summary>
        private void CleanAdobeAcrobat(CleaningStep step)
        {
            var appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            foreach (var root in new[] { appData, localApp })
            {
                var adobeDir = Path.Combine(root, "Adobe");
                if (!Directory.Exists(adobeDir)) continue;
                foreach (var dir in Directory.GetDirectories(adobeDir))
                {
                    var n = Path.GetFileName(dir);
                    if (!n.StartsWith("Acrobat") && !n.StartsWith("Reader")) continue;
                    DeleteDir(Path.Combine(dir, "Cache"),    step);
                    DeleteFilesInDir(Path.Combine(dir, "LogFiles"), "*.log", step);
                }
            }
        }

        /// <summary>Cache de miniatures & logs Bridge — jamais les espaces de travail ni les collections</summary>
        private void CleanAdobeBridge(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var adobeDir = Path.Combine(appData, "Adobe");
            if (!Directory.Exists(adobeDir)) return;
            foreach (var dir in Directory.GetDirectories(adobeDir))
            {
                var n = Path.GetFileName(dir);
                if (!n.StartsWith("Bridge")) continue;
                DeleteDir(Path.Combine(dir, "Cache"),      step);
                DeleteDir(Path.Combine(dir, "Thumbnails"), step);
                DeleteFilesInDir(Path.Combine(dir, "Logs"), "*.log", step);
            }
        }

        /// <summary>Cache disque XD — jamais les prototypes ni les assets cloud</summary>
        private void CleanAdobeXD(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var adobeDir = Path.Combine(appData, "Adobe");
            if (!Directory.Exists(adobeDir)) return;
            foreach (var dir in Directory.GetDirectories(adobeDir))
            {
                var n = Path.GetFileName(dir);
                if (!n.StartsWith("Adobe XD")) continue;
                DeleteDir(Path.Combine(dir, "Cache"), step);
                DeleteFilesInDir(Path.Combine(dir, "logs"), "*.log", step);
            }
        }

        /// <summary>Cache média & logs Audition — jamais les projets .sesx ni les presets</summary>
        private void CleanAdobeAudition(CleaningStep step)
        {
            var appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            foreach (var root in new[] { appData, localApp })
            {
                var adobeDir = Path.Combine(root, "Adobe");
                if (!Directory.Exists(adobeDir)) continue;
                foreach (var dir in Directory.GetDirectories(adobeDir))
                {
                    var n = Path.GetFileName(dir);
                    if (!n.StartsWith("Audition")) continue;
                    DeleteDir(Path.Combine(dir, "Media Cache Files"), step);
                    DeleteDir(Path.Combine(dir, "Media Cache"),       step);
                    DeleteFilesInDir(Path.Combine(dir, "logs"),  "*.log", step);
                    DeleteFilesInDir(Path.Combine(dir, "Logs"),  "*.log", step);
                }
            }
        }

        /// <summary>Cache de rendu Media Encoder — jamais les préréglages d'export ni les queue sauvegardées</summary>
        private void CleanAdobeMediaEncoder(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var adobeDir = Path.Combine(appData, "Adobe");
            if (!Directory.Exists(adobeDir)) return;
            foreach (var dir in Directory.GetDirectories(adobeDir))
            {
                var n = Path.GetFileName(dir);
                if (!n.StartsWith("Adobe Media Encoder") && !n.StartsWith("Media Encoder")) continue;
                DeleteDir(Path.Combine(dir, "Cache"),  step);
                DeleteFilesInDir(Path.Combine(dir, "Logs"), "*.log", step);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string[] SafeGetDirs(string path)
        {
            try { return Directory.Exists(path) ? Directory.GetDirectories(path) : Array.Empty<string>(); }
            catch { return Array.Empty<string>(); }
        }

        private void DeleteFilesInDir(string path, string pattern, CleaningStep step)
        {
            if (!Directory.Exists(path)) return;
            try
            {
                foreach (var file in Directory.GetFiles(path, pattern))
                    try
                    {
                        var fi = new FileInfo(file);
                        step.SpaceFreed += fi.Length;
                        File.Delete(file);
                        step.FilesDeleted++;
                    }
                    catch { }
            }
            catch { }
        }

        private static string FormatBytes(long bytes)
        {
            string[] u = { "B", "KB", "MB", "GB" }; double v = bytes; int i = 0;
            while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
            return $"{v:0.##} {u[i]}";
        }
    }
}
