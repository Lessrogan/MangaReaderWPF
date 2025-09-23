using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MangaReader.Core
{
    public class AppSettings
    {
        private static AppSettings _instance;
        private static readonly object _lock = new object();
        private string _settingsPath;

        // Paramètres de l'application
        public string MangaFolderPath { get; set; }
        public string Theme { get; set; } = "Dark";
        public bool SaveReadingPosition { get; set; } = true;
        public int ThumbnailCacheSize { get; set; } = 100;
        public bool EnableLazyLoading { get; set; } = true;
        public double DefaultZoomLevel { get; set; } = 1.0;
        public string LastExportPath { get; set; }
        public string LastImportPath { get; set; }
        public bool SupportArchiveFiles { get; set; } = true;
        public int ImageCacheSize { get; set; } = 50; // MB
        public bool PreloadNextPages { get; set; } = true;
        public int PreloadPageCount { get; set; } = 3;

        // Thème personnalisé
        public ThemeColors CustomTheme { get; set; } = new ThemeColors();

        private AppSettings()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var mangaReaderPath = Path.Combine(appDataPath, "MangaReader");

            if (!Directory.Exists(mangaReaderPath))
            {
                Directory.CreateDirectory(mangaReaderPath);
            }

            _settingsPath = Path.Combine(mangaReaderPath, "settings.json");

            // Définir le chemin par défaut des mangas
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            MangaFolderPath = Path.Combine(documentsPath, "Mangas");
        }

        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new AppSettings();
                            _instance.Load();
                        }
                    }
                }
                return _instance;
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null)
                    {
                        CopyProperties(loaded);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des paramètres : {ex.Message}");
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(this, options);
                await File.WriteAllTextAsync(_settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la sauvegarde des paramètres : {ex.Message}");
            }
        }

        private void CopyProperties(AppSettings source)
        {
            MangaFolderPath = source.MangaFolderPath;
            Theme = source.Theme;
            SaveReadingPosition = source.SaveReadingPosition;
            ThumbnailCacheSize = source.ThumbnailCacheSize;
            EnableLazyLoading = source.EnableLazyLoading;
            DefaultZoomLevel = source.DefaultZoomLevel;
            LastExportPath = source.LastExportPath;
            LastImportPath = source.LastImportPath;
            SupportArchiveFiles = source.SupportArchiveFiles;
            ImageCacheSize = source.ImageCacheSize;
            PreloadNextPages = source.PreloadNextPages;
            PreloadPageCount = source.PreloadPageCount;

            if (source.CustomTheme != null)
                CustomTheme = source.CustomTheme;
        }
    }

    public class ThemeColors
    {
        public string Background { get; set; } = "#1a1a1a";
        public string SecondaryBackground { get; set; } = "#252526";
        public string TertiaryBackground { get; set; } = "#2d2d30";
        public string BorderColor { get; set; } = "#404040";
        public string TextPrimary { get; set; } = "#FFFFFF";
        public string TextSecondary { get; set; } = "#CCCCCC";
        public string AccentColor { get; set; } = "#007ACC";
        public string HoverColor { get; set; } = "#3d3d40";
        public string SuccessColor { get; set; } = "#4CAF50";
        public string ErrorColor { get; set; } = "#F44336";
    }
}