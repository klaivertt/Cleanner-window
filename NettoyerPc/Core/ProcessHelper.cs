using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NettoyerPc.Core
{
    /// <summary>
    /// Utilitaire pour fermer les processus qui verrouillent des fichiers
    /// avant un nettoyage (navigateurs, apps tierces, outils dev…).
    /// </summary>
    public static class ProcessHelper
    {
        // Processus à fermer avant un nettoyage complet / de printemps
        private static readonly string[] AppProcesses =
        {
            // Navigateurs
            "firefox", "chrome", "msedge", "brave", "opera", "vivaldi",
            "iexplore", "waterfox", "librewolf",
            // Apps tierces
            "discord", "discordptb", "discordcanary",
            "spotify",
            "teams", "ms-teams",
            "slack",
            "obs64", "obs32",
            // Launchers gaming
            "steam", "steamwebhelper",
            "EpicGamesLauncher",
            "Agent",                // Battle.net
            "GogGalaxy",
            // Dev
            "node",
            "python", "python3",
            // Autres
            "OneDrive",
        };

        /// <summary>Détecte quels processus de la liste sont en cours d'exécution.</summary>
        public static List<string> GetRunningApps()
        {
            var running = new List<string>();
            foreach (var name in AppProcesses)
            {
                try
                {
                    if (Process.GetProcessesByName(name).Any())
                        running.Add(name);
                }
                catch { }
            }
            return running;
        }

        /// <summary>
        /// Ferme tous les processus de la liste <paramref name="names"/> (ou tous si null).
        /// Retourne le nombre de processus fermés.
        /// </summary>
        public static int KillApps(IEnumerable<string>? names = null)
        {
            var targets = (names ?? AppProcesses).ToArray();
            int killed  = 0;
            foreach (var name in targets)
            {
                try
                {
                    foreach (var p in Process.GetProcessesByName(name))
                    {
                        try
                        {
                            p.Kill(entireProcessTree: true);
                            killed++;
                        }
                        catch { }
                    }
                }
                catch { }
            }
            return killed;
        }
    }
}
