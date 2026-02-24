using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NettoyerPc.Core;

namespace NettoyerPc.Modules
{
    public class BrowserModule : ICleaningModule
    {
        public string Name => "Navigateurs";

        // Processus a fermer avant le nettoyage
        private static readonly string[] BrowserProcesses =
        {
            "firefox", "chrome", "msedge", "brave", "opera", "opera_gx",
            "vivaldi", "browser" , "iexplore", "waterfox", "librewolf"
        };

        public List<CleaningStep> GetSteps(CleaningMode mode)
        {
            return new List<CleaningStep>
            {
                new() { Name = "Fermeture navigateurs ouverts",   Category = "browser" },
                new() { Name = "Nettoyage Firefox",               Category = "browser" },
                new() { Name = "Nettoyage Chrome",                Category = "browser" },
                new() { Name = "Nettoyage Edge",                  Category = "browser" },
                new() { Name = "Nettoyage Brave",                 Category = "browser" },
                new() { Name = "Nettoyage Opera / Opera GX",      Category = "browser" },
                new() { Name = "Nettoyage Vivaldi",               Category = "browser" },
            };
        }

        public async Task ExecuteStepAsync(CleaningStep step, CancellationToken ct)
        {
            await Task.Run(() =>
            {
                if      (step.Name.Contains("Fermeture")) KillBrowsers(step);
                else if (step.Name.Contains("Firefox"))   CleanFirefox(step);
                else if (step.Name.Contains("Chrome"))    CleanChromium(step, "Google",   "Chrome");
                else if (step.Name.Contains("Edge"))      CleanEdge(step);
                else if (step.Name.Contains("Brave"))     CleanChromium(step, "BraveSoftware", "Brave-Browser");
                else if (step.Name.Contains("Opera"))     CleanOpera(step);
                else if (step.Name.Contains("Vivaldi"))   CleanChromium(step, "Vivaldi",  "Application");
            }, ct);
        }

        // ─── Fermeture forcee ────────────────────────────────────────────────

        private static void KillBrowsers(CleaningStep step)
        {
            int killed = 0;
            foreach (var name in BrowserProcesses)
            {
                try
                {
                    var procs = Process.GetProcessesByName(name);
                    foreach (var p in procs)
                    {
                        try
                        {
                            p.Kill(entireProcessTree: true);
                            p.WaitForExit(3000);
                            step.AddLog($"Fermé : {p.ProcessName}.exe");
                            killed++;
                        }
                        catch { }
                    }
                }
                catch { }
            }
            if (killed > 0)
            {
                step.FilesDeleted += killed;
                step.AddLog($"{killed} processus navigateur(s) fermé(s)");
                Thread.Sleep(800);
            }
            else
            {
                step.AddLog("Aucun navigateur ouvert détecté");
            }
        }

        // ─── Firefox (tous les profils + cache local) ────────────────────────

        private void CleanFirefox(CleaningStep step)
        {
            var appData      = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Profils Roaming
            CleanAllFirefoxProfiles(Path.Combine(appData, "Mozilla", "Firefox", "Profiles"), step);
            // Cache local (double profil Firefox)
            CleanAllFirefoxProfiles(Path.Combine(localAppData, "Mozilla", "Firefox", "Profiles"), step);
            // Librewolf
            CleanAllFirefoxProfiles(Path.Combine(appData, "librewolf", "Profiles"), step);
            // Waterfox
            CleanAllFirefoxProfiles(Path.Combine(appData, "Waterfox", "Profiles"), step);
        }

        private void CleanAllFirefoxProfiles(string profilesRoot, CleaningStep step)
        {
            if (!Directory.Exists(profilesRoot)) return;
            step.AddLog($"Nettoyage profils Firefox : {profilesRoot}");
            foreach (var profile in Directory.GetDirectories(profilesRoot))
            {
                var cacheDirs = new[] { "cache2", "startupCache", "thumbnails", "crashes",
                    "minidumps", "jumpListCache", "safebrowsing", "storage/permanent/chrome",
                    "storage/default", "shader-cache", "SessionCheckpoints" };

                foreach (var d in cacheDirs)
                    DeleteDir(Path.Combine(profile, d), step);

                DeleteFilePattern(profile, "*.sqlite-wal", step);
                DeleteFilePattern(profile, "*.sqlite-shm", step);
                DeleteFilePattern(profile, "*.log",        step);
            }
        }

        // ─── Navigateurs Chromium generiques ────────────────────────────────

        private void CleanChromium(CleaningStep step, string vendor, string appName)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var root  = Path.Combine(local, vendor, appName, "User Data");
            if (!Directory.Exists(root)) return;

            // Default + tous les profils (Profile 1, Profile 2…)
            var profiles = new List<string> { "Default" };
            profiles.AddRange(Directory.GetDirectories(root, "Profile *").Select(Path.GetFileName)!);

            foreach (var profile in profiles)
            {
                var pPath = Path.Combine(root, profile);
                if (!Directory.Exists(pPath)) continue;

                var cacheDirs = new[]
                {
                    "Cache", "Cache2", "Code Cache", "GPUCache",
                    "Service Worker/CacheStorage", "Service Worker/ScriptCache",
                    "DawnGraphite", "DawnWebGPU",
                    "Network/Cookies-journal",
                    "IndexedDB", "databases", "blob_storage",
                    "BudgetDatabase", "GCM Store",
                    "crash_reports", "logs"
                };
                foreach (var d in cacheDirs)
                    DeleteDir(Path.Combine(pPath, d), step);

                DeleteFilePattern(pPath, "*.log", step);
                DeleteFilePattern(pPath, "*.dmp", step);
            }

            // Cache GPU partagé au niveau User Data
            DeleteDir(Path.Combine(root, "GrShaderCache"), step);
            DeleteDir(Path.Combine(root, "ShaderCache"),   step);
            DeleteDir(Path.Combine(root, "Crashpad"),      step);
        }

        // ─── Edge ────────────────────────────────────────────────────────────

        private void CleanEdge(CleaningStep step)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var root  = Path.Combine(local, "Microsoft", "Edge", "User Data");
            if (!Directory.Exists(root)) return;

            var profiles = new List<string> { "Default" };
            profiles.AddRange(Directory.GetDirectories(root, "Profile *").Select(Path.GetFileName)!);

            foreach (var profile in profiles)
            {
                var pPath = Path.Combine(root, profile);
                if (!Directory.Exists(pPath)) continue;
                var cacheDirs = new[]
                {
                    "Cache", "Cache2", "Code Cache", "GPUCache",
                    "Service Worker/CacheStorage", "Service Worker/ScriptCache",
                    "crash_reports", "logs", "IndexedDB"
                };
                foreach (var d in cacheDirs)
                    DeleteDir(Path.Combine(pPath, d), step);
                DeleteFilePattern(pPath, "*.log", step);
            }

            DeleteDir(Path.Combine(root, "ShaderCache"),  step);
            DeleteDir(Path.Combine(root, "Crashpad"),     step);
        }

        // ─── Opera / Opera GX ────────────────────────────────────────────────

        private void CleanOpera(CleaningStep step)
        {
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Opera Stable
            var opRoaming = Path.Combine(roaming, "Opera Software", "Opera Stable");
            var opLocal   = Path.Combine(local,   "Opera Software", "Opera Stable");
            CleanOperaProfile(opRoaming, step);
            CleanOperaProfile(opLocal,   step);

            // Opera GX
            var gxRoaming = Path.Combine(roaming, "Opera Software", "Opera GX Stable");
            var gxLocal   = Path.Combine(local,   "Opera Software", "Opera GX Stable");
            CleanOperaProfile(gxRoaming, step);
            CleanOperaProfile(gxLocal,   step);
        }

        private void CleanOperaProfile(string path, CleaningStep step)
        {
            if (!Directory.Exists(path)) return;
            var cacheDirs = new[]
            {
                "Cache", "Cache2", "Code Cache", "GPUCache",
                "Service Worker/CacheStorage", "IndexedDB", "logs"
            };
            foreach (var d in cacheDirs)
                DeleteDir(Path.Combine(path, d), step);
            DeleteFilePattern(path, "*.log", step);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private void DeleteDir(string path, CleaningStep step)
        {
            if (!Directory.Exists(path)) return;
            try
            {
                var di = new DirectoryInfo(path);
                step.SpaceFreed += GetDirSize(di);
                int n = 0;
                foreach (var f in di.GetFiles("*", SearchOption.AllDirectories))
                {
                    try { File.SetAttributes(f.FullName, FileAttributes.Normal); File.Delete(f.FullName); n++; }
                    catch { }
                }
                try { Directory.Delete(path, true); } catch { }
                step.FilesDeleted += n;
                if (n > 0) step.AddLog($"Supprimé : {path} ({n} fichier(s))");
            }
            catch { }
        }

        private void DeleteFilePattern(string dir, string pattern, CleaningStep step)
        {
            if (!Directory.Exists(dir)) return;
            foreach (var f in Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly))
            {
                try
                {
                    step.SpaceFreed += new FileInfo(f).Length;
                    File.Delete(f);
                    step.FilesDeleted++;
                }
                catch { }
            }
        }

        private long GetDirSize(DirectoryInfo di)
        {
            long sz = 0;
            try { foreach (var f in di.GetFiles("*", SearchOption.AllDirectories)) { try { sz += f.Length; } catch { } } }
            catch { }
            return sz;
        }
    }
}
