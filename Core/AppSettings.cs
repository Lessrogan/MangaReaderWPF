using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MangaReader.Core
{
    // Classe DTO pour la sérialisation/désérialisation
    internal class AppSettingsDto
    {
        public string MangaFolderPath { get; set; }
        public string Theme { get; set; }
        public bool SaveReadingPosition { get; set; }
        public int ThumbnailCacheSize { get; set; }
        public bool EnableLazyLoading { get; set; }
        public double DefaultZoomLevel { get; set; }
        public string LastExportPath { get; set; }
        public string LastImportPath { get; set; }
        public bool SupportArchiveFiles { get; set; }
        public int ImageCacheSize { get; set; }
        public bool PreloadNextPages { get; set; }
        public int PreloadPageCount { get; set; }
        public int ItemsPerPage { get; set; }
        public string DefaultSortBy { get; set; }
        public bool SortAscending { get; set; }
        public ThemeColors CustomTheme { get; set; }
    }

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

        // Paramètres de pagination
        public int ItemsPerPage { get; set; } = 20;
        public string DefaultSortBy { get; set; } = "Name"; // Name, DateAdded, LastRead, Author
        public bool SortAscending { get; set; } = true;

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
            Theme = "Dark";
            ItemsPerPage = 20;
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
                    // Utiliser la classe DTO au lieu de AppSettings directement
                    var loaded = JsonSerializer.Deserialize<AppSettingsDto>(json);

                    if (loaded != null)
                    {
                        CopyPropertiesFromDto(loaded);
                        System.Diagnostics.Debug.WriteLine($"Settings chargés: Theme={Theme}, Path={MangaFolderPath}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Fichier settings.json non trouvé, utilisation des valeurs par défaut");
                    // Sauvegarder les valeurs par défaut
                    SaveAsync().Wait();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement settings: {ex.Message}");
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

                // Créer un objet DTO avec toutes les propriétés à sauvegarder
                var settingsToSave = new AppSettingsDto
                {
                    MangaFolderPath = this.MangaFolderPath,
                    Theme = this.Theme,
                    SaveReadingPosition = this.SaveReadingPosition,
                    ThumbnailCacheSize = this.ThumbnailCacheSize,
                    EnableLazyLoading = this.EnableLazyLoading,
                    DefaultZoomLevel = this.DefaultZoomLevel,
                    LastExportPath = this.LastExportPath,
                    LastImportPath = this.LastImportPath,
                    SupportArchiveFiles = this.SupportArchiveFiles,
                    ImageCacheSize = this.ImageCacheSize,
                    PreloadNextPages = this.PreloadNextPages,
                    PreloadPageCount = this.PreloadPageCount,
                    ItemsPerPage = this.ItemsPerPage,
                    DefaultSortBy = this.DefaultSortBy,
                    SortAscending = this.SortAscending,
                    CustomTheme = this.CustomTheme
                };

                var json = JsonSerializer.Serialize(settingsToSave, options);
                await File.WriteAllTextAsync(_settingsPath, json);

                System.Diagnostics.Debug.WriteLine($"Settings sauvegardés dans {_settingsPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur sauvegarde settings: {ex.Message}");
            }
        }

        private void CopyPropertiesFromDto(AppSettingsDto source)
        {
            if (source == null) return;

            // Copier chaque propriété explicitement
            if (!string.IsNullOrEmpty(source.MangaFolderPath))
                MangaFolderPath = source.MangaFolderPath;
            if (!string.IsNullOrEmpty(source.Theme))
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
            ItemsPerPage = source.ItemsPerPage;
            DefaultSortBy = source.DefaultSortBy;
            SortAscending = source.SortAscending;

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