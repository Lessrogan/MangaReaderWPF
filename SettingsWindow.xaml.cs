using MangaReader.Core;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MessageBox = System.Windows.MessageBox;

namespace MangaReader
{
    public partial class SettingsWindow : Window
    {
        private AppSettings settings;
        private ImageCacheManager cacheManager;
        private TagsManager tagsManager;

        public SettingsWindow()
        {
            InitializeComponent();
            settings = AppSettings.Instance;
            cacheManager = ImageCacheManager.Instance;
            tagsManager = TagsManager.Instance;
            LoadSettings();
            LoadTagsList();
            ShowPanel("General");
            UpdateCacheStats();
        }

        private void LoadTagsList()
        {
            TagsListBox.Items.Clear();
            foreach (var tag in tagsManager.AvailableTags)
            {
                TagsListBox.Items.Add(tag);
            }
        }

        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            NewTagTextBox.Text = "";
            NewTagTextBox.Focus();
        }

        private void EditTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (TagsListBox.SelectedItem != null)
            {
                NewTagTextBox.Text = TagsListBox.SelectedItem.ToString();
                NewTagTextBox.Focus();
            }
        }

        private async void DeleteTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (TagsListBox.SelectedItem != null)
            {
                var tag = TagsListBox.SelectedItem.ToString();
                var result = MessageBox.Show($"Supprimer le tag '{tag}' ?", "Confirmation",
                                            MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    tagsManager.RemoveTag(tag);
                    await tagsManager.SaveTagsAsync();
                    LoadTagsList();
                }
            }
        }

        private async void SaveTagButton_Click(object sender, RoutedEventArgs e)
        {
            var newTag = NewTagTextBox.Text.Trim();

            if (!string.IsNullOrEmpty(newTag))
            {
                if (TagsListBox.SelectedItem != null)
                {
                    // Modification
                    tagsManager.UpdateTag(TagsListBox.SelectedItem.ToString(), newTag);
                }
                else
                {
                    // Ajout
                    tagsManager.AddTag(newTag);
                }

                await tagsManager.SaveTagsAsync();
                LoadTagsList();
                NewTagTextBox.Text = "";
            }
        }

        private void LoadSettings()
        {
            // Général
            MangaFolderTextBox.Text = settings.MangaFolderPath;
            SupportArchivesCheckBox.IsChecked = settings.SupportArchiveFiles;
            SaveReadingPositionCheckBox.IsChecked = settings.SaveReadingPosition;

            // Apparence
            switch (settings.Theme)
            {
                case "Dark":
                    ThemeComboBox.SelectedIndex = 0;
                    break;
                case "Light":
                    ThemeComboBox.SelectedIndex = 1;
                    break;
                case "BlueNight":
                    ThemeComboBox.SelectedIndex = 2;
                    break;
                case "Custom":
                    ThemeComboBox.SelectedIndex = 3;
                    break;
            }

            DefaultZoomSlider.Value = settings.DefaultZoomLevel;
            ZoomValueText.Text = $"{(int)(settings.DefaultZoomLevel * 100)}%";

            if (settings.CustomTheme != null)
            {
                BackgroundColorTextBox.Text = settings.CustomTheme.Background;
                SecondaryBackgroundTextBox.Text = settings.CustomTheme.SecondaryBackground;
                AccentColorTextBox.Text = settings.CustomTheme.AccentColor;
                TextColorTextBox.Text = settings.CustomTheme.TextPrimary;
            }

            // Performance
            ImageCacheSlider.Value = settings.ImageCacheSize;
            ImageCacheText.Text = $"{settings.ImageCacheSize} MB";

            ThumbnailCacheSlider.Value = settings.ThumbnailCacheSize;
            ThumbnailCacheText.Text = settings.ThumbnailCacheSize.ToString();

            LazyLoadingCheckBox.IsChecked = settings.EnableLazyLoading;
            PreloadNextPagesCheckBox.IsChecked = settings.PreloadNextPages;
            PreloadCountSlider.Value = settings.PreloadPageCount;
            PreloadCountText.Text = settings.PreloadPageCount.ToString();

            ItemsPerPageSlider.Value = settings.ItemsPerPage;
        }

        public void ShowPanel(string panelName)
        {
            // Masquer tous les panneaux
            GeneralPanel.Visibility = Visibility.Collapsed;
            AppearancePanel.Visibility = Visibility.Collapsed;
            PerformancePanel.Visibility = Visibility.Collapsed;
            ImportExportPanel.Visibility = Visibility.Collapsed;
            TagsPanel.Visibility = Visibility.Collapsed;

            // Afficher le panneau sélectionné
            switch (panelName)
            {
                case "General":
                    GeneralPanel.Visibility = Visibility.Visible;
                    break;
                case "Appearance":
                    AppearancePanel.Visibility = Visibility.Visible;
                    break;
                case "Performance":
                    PerformancePanel.Visibility = Visibility.Visible;
                    break;
                case "ImportExport":
                    ImportExportPanel.Visibility = Visibility.Visible;
                    break;
                case "Tags":
                    TagsPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.Tag != null)
            {
                ShowPanel(button.Tag.ToString());
            }
        }

        private void BrowseFolderButton_Click_WPF(object sender, RoutedEventArgs e)
        {
            // Cette méthode utilise OpenFileDialog avec un hack pour sélectionner un dossier
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Sélectionnez un fichier dans le dossier de mangas",
                Filter = "Dossiers|*.folder",
                FileName = "Sélection du dossier",
                CheckFileExists = false,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                // Extraire le dossier du chemin complet
                var folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    MangaFolderTextBox.Text = folderPath;
                }
            }
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedIndex == 3) // Custom
            {
                CustomColorsGroup.Visibility = Visibility.Visible;
            }
            else
            {
                CustomColorsGroup.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyCustomColors_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.CustomTheme.Background = BackgroundColorTextBox.Text;
                settings.CustomTheme.SecondaryBackground = SecondaryBackgroundTextBox.Text;
                settings.CustomTheme.AccentColor = AccentColorTextBox.Text;
                settings.CustomTheme.TextPrimary = TextColorTextBox.Text;

                MessageBox.Show("Les couleurs personnalisées seront appliquées après le redémarrage de l'application.",
                               "Couleurs personnalisées", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'application des couleurs : {ex.Message}",
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DefaultZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ZoomValueText != null)
            {
                ZoomValueText.Text = $"{(int)(e.NewValue * 100)}%";
            }
        }

        private void ImageCacheSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ImageCacheText != null)
            {
                ImageCacheText.Text = $"{(int)e.NewValue} MB";
            }
        }

        private void ThumbnailCacheSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ThumbnailCacheText != null)
            {
                ThumbnailCacheText.Text = ((int)e.NewValue).ToString();
            }
        }

        private void PreloadCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PreloadCountText != null)
            {
                PreloadCountText.Text = ((int)e.NewValue).ToString();
            }
        }

        private void UpdateCacheStats()
        {
            var stats = cacheManager.GetCacheStats();
            var usedMB = stats.usedBytes / (1024.0 * 1024.0);

            CacheStatsText.Text = $"Utilisation actuelle : {usedMB:F1} MB\n" +
                                 $"Images en cache : {stats.imageCount}\n" +
                                 $"Miniatures en cache : {stats.thumbnailCount}";
        }

        private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Voulez-vous vraiment vider tous les caches ?",
                                        "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                cacheManager.ClearAllCaches();
                UpdateCacheStats();
                MessageBox.Show("Les caches ont été vidés.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Voulez-vous vraiment réinitialiser tous les paramètres ?",
                                        "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Créer de nouveaux paramètres par défaut
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                settings.MangaFolderPath = Path.Combine(documentsPath, "Mangas");
                settings.Theme = "Dark";
                settings.SaveReadingPosition = true;
                settings.ThumbnailCacheSize = 100;
                settings.EnableLazyLoading = true;
                settings.DefaultZoomLevel = 1.0;
                settings.SupportArchiveFiles = true;
                settings.ImageCacheSize = 50;
                settings.PreloadNextPages = true;
                settings.PreloadPageCount = 3;

                LoadSettings();
                MessageBox.Show("Les paramètres ont été réinitialisés.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ItemsPerPageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ItemsPerPageText != null)
            {
                ItemsPerPageText.Text = ((int)e.NewValue).ToString();
            }
        }

        private async void ExportJsonButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Fichiers JSON (*.json)|*.json",
                DefaultExt = ".json",
                FileName = $"manga_metadata_{DateTime.Now:yyyyMMdd}.json"
            };

            if (!string.IsNullOrEmpty(settings.LastExportPath))
            {
                dialog.InitialDirectory = settings.LastExportPath;
            }

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var database = new MangaDatabase();
                    await database.InitializeAsync();
                    var allMangas = await database.GetAllMangasAsync();

                    var success = await MetadataExporter.ExportToJsonAsync(dialog.FileName, allMangas);

                    if (success)
                    {
                        MessageBox.Show($"Les métadonnées ont été exportées avec succès.\n{allMangas.Count} mangas exportés.",
                                      "Export réussi", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Une erreur est survenue lors de l'export.",
                                      "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'export : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExportXmlButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Fichiers XML (*.xml)|*.xml",
                DefaultExt = ".xml",
                FileName = $"manga_metadata_{DateTime.Now:yyyyMMdd}.xml"
            };

            if (!string.IsNullOrEmpty(settings.LastExportPath))
            {
                dialog.InitialDirectory = settings.LastExportPath;
            }

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var database = new MangaDatabase();
                    await database.InitializeAsync();
                    var allMangas = await database.GetAllMangasAsync();

                    var success = await MetadataExporter.ExportToXmlAsync(dialog.FileName, allMangas);

                    if (success)
                    {
                        MessageBox.Show($"Les métadonnées ont été exportées avec succès.\n{allMangas.Count} mangas exportés.",
                                      "Export réussi", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Une erreur est survenue lors de l'export.",
                                      "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'export : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Fichiers de métadonnées (*.json;*.xml)|*.json;*.xml|Fichiers JSON (*.json)|*.json|Fichiers XML (*.xml)|*.xml"
            };

            if (!string.IsNullOrEmpty(settings.LastImportPath))
            {
                dialog.InitialDirectory = settings.LastImportPath;
            }

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var extension = Path.GetExtension(dialog.FileName).ToLower();
                    var importedData = extension == ".json"
                        ? await MetadataExporter.ImportFromJsonAsync(dialog.FileName)
                        : await MetadataExporter.ImportFromXmlAsync(dialog.FileName);

                    if (importedData.Any())
                    {
                        var database = new MangaDatabase();
                        await database.InitializeAsync();
                        var success = await MetadataExporter.ApplyImportedMetadata(importedData, database);

                        if (success)
                        {
                            ImportResultText.Text = $"✓ Import réussi : {importedData.Count} mangas importés";
                            ImportResultText.Foreground = System.Windows.Media.Brushes.LightGreen;
                            ImportResultText.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            ImportResultText.Text = "✗ Erreur lors de l'application des métadonnées";
                            ImportResultText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                            ImportResultText.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Aucune métadonnée trouvée dans le fichier.",
                                      "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'import : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Général
                settings.MangaFolderPath = MangaFolderTextBox.Text;
                settings.SupportArchiveFiles = SupportArchivesCheckBox.IsChecked ?? true;
                settings.SaveReadingPosition = SaveReadingPositionCheckBox.IsChecked ?? true;

                // Apparence
                switch (ThemeComboBox.SelectedIndex)
                {
                    case 0:
                        settings.Theme = "Dark";
                        break;
                    case 1:
                        settings.Theme = "Light";
                        break;
                    case 2:
                        settings.Theme = "BlueNight";
                        break;
                    case 3:
                        settings.Theme = "Custom";
                        break;
                }

                settings.DefaultZoomLevel = DefaultZoomSlider.Value;

                // Performance
                settings.ImageCacheSize = (int)ImageCacheSlider.Value;
                settings.ThumbnailCacheSize = (int)ThumbnailCacheSlider.Value;
                settings.EnableLazyLoading = LazyLoadingCheckBox.IsChecked ?? true;
                settings.PreloadNextPages = PreloadNextPagesCheckBox.IsChecked ?? true;
                settings.PreloadPageCount = (int)PreloadCountSlider.Value;

                // Sauvegarder
                await settings.SaveAsync();

                // Mettre à jour le cache manager
                cacheManager.UpdateCacheSettings();

                settings.ItemsPerPage = (int)ItemsPerPageSlider.Value;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}",
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}