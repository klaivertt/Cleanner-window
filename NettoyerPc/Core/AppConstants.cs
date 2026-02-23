using System;

namespace NettoyerPc.Core
{
    /// <summary>Constantes globales de l'application.</summary>
    public static class AppConstants
    {
        /// <summary>Nom complet de l'application.</summary>
        public const string AppName = "PC Clean";

        /// <summary>Version de l'application (synchronized avec .csproj).</summary>
        public const string AppVersion = "1.0.0";

        /// <summary>Numéro de version complet (pour compatibilité).</summary>
        public static readonly Version VersionNumber = new(1, 0, 0, 0);

        /// <summary>Répertoire des rapports (relatif à l'exécutable).</summary>
        public const string ReportsDirectory = "Reports";

        /// <summary>GitHub Owner pour les mises à jour.</summary>
        public const string GitHubOwner = "klaivertt";

        /// <summary>GitHub Repository pour les mises à jour.</summary>
        public const string GitHubRepo = "Cleanner-window";

        /// <summary>URL GitHub API pour les releases.</summary>
        public static string GitHubApiUrl => $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
    }
}
