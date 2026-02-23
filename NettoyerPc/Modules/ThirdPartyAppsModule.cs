using System;
using System.Collections.Generic;
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
                new() { Name = "Discord (cache, code cache, GPU cache)", Category = "thirdparty" },
                new() { Name = "Spotify (storage cache)", Category = "thirdparty" },
                new() { Name = "Teams (cache, GPU cache)", Category = "thirdparty" },
                new() { Name = "Slack (cache, code cache)", Category = "thirdparty" },
                new() { Name = "OBS Studio (logs)", Category = "thirdparty" },
                new() { Name = "Steam (shader cache, logs, dumps)", Category = "thirdparty" },
                new() { Name = "Epic Games (logs)", Category = "thirdparty" },
                new() { Name = "Battle.net (cache)",              Category = "thirdparty" },
                new() { Name = "Zoom (cache, logs)",               Category = "thirdparty" },
                new() { Name = "WhatsApp Desktop (cache)",         Category = "thirdparty" },
                new() { Name = "Telegram Desktop (cache)",         Category = "thirdparty" },
                new() { Name = "Adobe Creative Cloud (cache)",     Category = "thirdparty" },
                new() { Name = "Figma (cache)",                    Category = "thirdparty" },
                new() { Name = "Notion (cache)",                   Category = "thirdparty" },
                new() { Name = "Twitch (cache)",                   Category = "thirdparty" },
                new() { Name = "VLC (cache, logs)",                Category = "thirdparty" },
            };
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                if (step.Name.StartsWith("Discord"))   CleanDiscord(step);
                else if (step.Name.StartsWith("Spotify")) CleanSpotify(step);
                else if (step.Name.StartsWith("Teams"))   CleanTeams(step);
                else if (step.Name.StartsWith("Slack"))   CleanSlack(step);
                else if (step.Name.StartsWith("OBS"))     CleanOBS(step);
                else if (step.Name.StartsWith("Steam"))   CleanSteam(step);
                else if (step.Name.StartsWith("Epic"))    CleanEpic(step);
                else if (step.Name.StartsWith("Battle"))   CleanBattleNet(step);
                else if (step.Name.StartsWith("Zoom"))      CleanZoom(step);
                else if (step.Name.StartsWith("WhatsApp"))  CleanWhatsApp(step);
                else if (step.Name.StartsWith("Telegram"))  CleanTelegram(step);
                else if (step.Name.StartsWith("Adobe"))     CleanAdobe(step);
                else if (step.Name.StartsWith("Figma"))     CleanFigma(step);
                else if (step.Name.StartsWith("Notion"))    CleanNotion(step);
                else if (step.Name.StartsWith("Twitch"))    CleanTwitch(step);
                else if (step.Name.StartsWith("VLC"))       CleanVLC(step);
            }, cancellationToken);
        }

        private void CleanDiscord(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var roaming = Path.Combine(appData, "discord");
            DeleteDir(Path.Combine(roaming, "Cache"), step);
            DeleteDir(Path.Combine(roaming, "Code Cache"), step);
            DeleteDir(Path.Combine(roaming, "GPUCache"), step);
        }

        private void CleanSpotify(CleaningStep step)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            DeleteDir(Path.Combine(appData, "Spotify", "Storage"), step);
            DeleteDir(Path.Combine(local, "Spotify", "Storage"), step);
        }

        private void CleanTeams(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDir(Path.Combine(appData, "Microsoft", "Teams", "Cache"), step);
            DeleteDir(Path.Combine(appData, "Microsoft", "Teams", "GPUCache"), step);
            DeleteDir(Path.Combine(appData, "Microsoft", "Teams", "Service Worker", "CacheStorage"), step);
            // New Teams
            DeleteDir(Path.Combine(local, "Packages", "MSTeams_8wekyb3d8bbwe", "LocalCache"), step);
        }

        private void CleanSlack(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            DeleteDir(Path.Combine(appData, "Slack", "Cache"), step);
            DeleteDir(Path.Combine(appData, "Slack", "Code Cache"), step);
        }

        private void CleanOBS(CleaningStep step)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var obsLogs = Path.Combine(appData, "obs-studio", "logs");
            if (Directory.Exists(obsLogs))
            {
                foreach (var file in Directory.GetFiles(obsLogs, "*.txt"))
                {
                    try { var fi = new FileInfo(file); step.SpaceFreed += fi.Length; File.Delete(file); step.FilesDeleted++; }
                    catch { }
                }
            }
            DeleteDir(Path.Combine(appData, "obs-studio", "crashes"), step);
        }

        private void CleanSteam(CleaningStep step)
        {
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var steamPath = Path.Combine(programFilesX86, "Steam");
            DeleteDir(Path.Combine(steamPath, "logs"), step);
            DeleteDir(Path.Combine(steamPath, "dumps"), step);
            DeleteDir(Path.Combine(steamPath, "appcache", "httpcache"), step);
            // Shader cache sur tous les disques
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
                step.SpaceFreed += GetDirSize(info);
                Directory.Delete(path, true);
                step.FilesDeleted++;
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
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DeleteDir(Path.Combine(appData, "WhatsApp", "Cache"),       step);
            DeleteDir(Path.Combine(appData, "WhatsApp", "Code Cache"),  step);
            DeleteDir(Path.Combine(appData, "WhatsApp", "GPUCache"),    step);
            // WhatsApp UWP
            DeleteDir(Path.Combine(local, "Packages", "5319275A.WhatsAppDesktop_cv1g1gvanyjgm", "LocalCache"), step);
        }

        private void CleanTelegram(CleaningStep step)
        {
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
    }
}
