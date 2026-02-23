using System.Windows;

namespace NettoyerPc
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Charger la langue sauvegardée
            LoadApplicationLanguage();
            
            // Vérifier les droits administrateur
            if (!Core.AdminHelper.IsAdministrator())
            {
                MessageBox.Show(
                    Core.Localizer.T("app.admin.body"),
                    Core.Localizer.T("app.admin.title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void LoadApplicationLanguage()
        {
            try
            {
                // Load external JSON translations first (Languages/ folder)
                Core.Localizer.LoadExternalLanguages();
                // Use UserPreferences as single source of truth (migrates from language.cfg)
                var savedLanguage = Core.UserPreferences.Current.Language;
                Core.Localizer.SetLanguage(savedLanguage);
            }
            catch
            {
                Core.Localizer.SetLanguage("fr-FR");
            }
        }
    }
}
