using MangaReader.Core;
using System.Configuration;
using System.Data;
using System.Windows;

namespace MangaReader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Charger les paramètres au démarrage
            AppSettings.Instance.Load();

            // Appliquer le thème si nécessaire
            ApplyTheme();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Nettoyer le cache temporaire des archives
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MangaReader");
            if (System.IO.Directory.Exists(tempPath))
            {
                try
                {
                    System.IO.Directory.Delete(tempPath, true);
                }
                catch { }
            }

            base.OnExit(e);
        }

        private void ApplyTheme()
        {
            var settings = AppSettings.Instance;

            // Appliquer le thème en fonction des paramètres
            var dict = new ResourceDictionary();

            switch (settings.Theme)
            {
                case "Light":
                    // Thème clair (à implémenter)
                    break;
                case "BlueNight":
                    // Thème bleu nuit (à implémenter)
                    break;
                case "Custom":
                    // Appliquer les couleurs personnalisées
                    if (settings.CustomTheme != null)
                    {
                        // Créer les styles avec les couleurs personnalisées
                        // (implémentation à compléter selon les besoins)
                    }
                    break;
                default:
                    // Thème sombre par défaut
                    break;
            }

            if (dict.Count > 0)
            {
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }
        }
    }
}