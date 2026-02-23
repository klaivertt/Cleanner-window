using System.Windows;

namespace NettoyerPc
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Vérifier les droits administrateur
            if (!Core.AdminHelper.IsAdministrator())
            {
                MessageBox.Show(
                    "Cette application nécessite des droits administrateur.\n" +
                    "Veuillez relancer l'application en tant qu'administrateur.",
                    "Droits insuffisants",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
