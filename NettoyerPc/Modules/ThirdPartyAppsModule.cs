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
                new() { Name = "Battle.net (cache)", Category = "thirdparty" },
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
                else if (step.Name.StartsWith("Battle"))  CleanBattleNet(step);
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
    }
}
