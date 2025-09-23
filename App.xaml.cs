using MangaReader.Core;
using System;
using System.Windows;

namespace MangaReader
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Gestionnaire d'exceptions global
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            try
            {
                base.OnStartup(e);

                // Charger les paramètres au démarrage
                AppSettings.Instance.Load();

                // Appliquer le thème si nécessaire
                ApplyTheme();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur au démarrage:\n{ex.Message}\n\nInnerException:\n{ex.InnerException?.Message}",
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show($"Erreur non gérée:\n{ex?.Message}\n\nInnerException:\n{ex?.InnerException?.Message}\n\nStackTrace:\n{ex?.StackTrace}",
                           "Erreur Critique", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Erreur Dispatcher:\n{e.Exception.Message}\n\nInnerException:\n{e.Exception.InnerException?.Message}\n\nStackTrace:\n{e.Exception.StackTrace}",
                           "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void ApplyTheme()
        {
            try
            {
                var settings = AppSettings.Instance;
                // Code de thème simplifié pour éviter les erreurs
            }
            catch (Exception ex)
            {
                // Ignorer les erreurs de thème
                System.Diagnostics.Debug.WriteLine($"Erreur thème: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Nettoyer le cache temporaire des archives
            try
            {
                var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MangaReader");
                if (System.IO.Directory.Exists(tempPath))
                {
                    System.IO.Directory.Delete(tempPath, true);
                }
            }
            catch { }

            base.OnExit(e);
        }
    }
}