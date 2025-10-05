using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MangaReader
{
    public partial class MangaDetailsWindow : Window
    {
        private MangaInfo mangaInfo;
        private List<string> imageFiles;
        private List<PageThumbnail> pageThumbnails;
        private MangaDatabase database;

        public MangaDetailsWindow(MangaInfo manga)
        {
            InitializeComponent();
            mangaInfo = manga;
            DataContext = manga;

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                database = new MangaDatabase();
                await database.InitializeAsync();

                UpdateFavoriteButton();
                await LoadMangaInfoAsync();
                await LoadPageThumbnailsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'initialisation : {ex.Message}\n\nDétails: {ex.StackTrace}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Erreur InitializeAsync: {ex}");
            }
        }

        private async Task LoadMangaInfoAsync()
        {
            try
            {
                // Vérifier si c'est une archive ou un dossier
                bool isArchive = File.Exists(mangaInfo.FolderPath) &&
                                new[] { ".cbz", ".cbr", ".zip", ".rar" }.Contains(
                                    Path.GetExtension(mangaInfo.FolderPath).ToLower());

                if (!isArchive && !Directory.Exists(mangaInfo.FolderPath))
                {
                    MessageBox.Show($"Le dossier du manga n'existe pas :\n{mangaInfo.FolderPath}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Charger la liste des images
                if (isArchive)
                {
                    // Pour une archive, extraire temporairement
                    imageFiles = await ExtractArchiveImages(mangaInfo.FolderPath);
                }
                else
                {
                    // Pour un dossier normal
                    var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                    var allFiles = Directory.GetFiles(mangaInfo.FolderPath, "*.*", SearchOption.AllDirectories);

                    imageFiles = allFiles
                        .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()))
                        .OrderBy(f => f, new NaturalStringComparer())
                        .ToList();
                }

                // Mettre à jour le nombre de pages
                PageCountText.Text = $"{imageFiles.Count} pages";

               // Calculer la taille du dossier en arrière-plan
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var directoryInfo = new DirectoryInfo(mangaInfo.FolderPath);
                        var files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
                        var totalSize = files.Sum(file => file.Length);
                        var sizeInMB = totalSize / (1024.0 * 1024.0);

                        await Dispatcher.InvokeAsync(() =>
                        {
                            try
                            {
                                FolderSizeText.Text = sizeInMB > 1024
                                    ? $"{sizeInMB / 1024:F1} GB"
                                    : $"{sizeInMB:F1} MB";
                            }
                            catch
                            {
                                FolderSizeText.Text = "Inconnue";
                            }
                        });
                    }
                    catch
                    {
                        await Dispatcher.InvokeAsync(() => FolderSizeText.Text = "Inconnue");
                    }
                });

                // Date d'ajout (création du dossier)
                try
                {
                    var creationTime = Directory.GetCreationTime(mangaInfo.FolderPath);
                    AddedDateText.Text = creationTime.ToString("dd/MM/yyyy");
                }
                catch
                {
                    AddedDateText.Text = "Inconnue";
                }

                // Charger les métadonnées depuis la base de données
                try
                {
                    var dbManga = await database.GetMangaByPathAsync(mangaInfo.FolderPath);
                    if (dbManga != null)
                    {
                        AuthorTextBox.Text = dbManga.Author ?? "Auteur inconnu";
                        TagsTextBox.Text = dbManga.Tags ?? "";
                        CharactersTextBox.Text = dbManga.Characters ?? "";
                        DescriptionTextBox.Text = dbManga.Description ?? "";
                    }
                    else
                    {
                        AuthorTextBox.Text = mangaInfo.Author ?? "Auteur inconnu";
                        TagsTextBox.Text = mangaInfo.Tags ?? "";
                        CharactersTextBox.Text = mangaInfo.Characters ?? "";
                        DescriptionTextBox.Text = mangaInfo.Description ?? "";
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur chargement métadonnées DB: {ex.Message}");
                    // Utiliser les données par défaut
                    AuthorTextBox.Text = mangaInfo.Author ?? "Auteur inconnu";
                    TagsTextBox.Text = mangaInfo.Tags ?? "";
                    CharactersTextBox.Text = mangaInfo.Characters ?? "";
                    DescriptionTextBox.Text = mangaInfo.Description ?? "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des informations : {ex.Message}\n\nStackTrace: {ex.StackTrace}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Erreur LoadMangaInfoAsync: {ex}");
            }
        }

        private async Task<List<string>> ExtractArchiveImages(string archivePath)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "MangaReader",
                                           Path.GetFileNameWithoutExtension(archivePath));

                // Si déjà extrait, utiliser le cache
                if (Directory.Exists(tempPath))
                {
                    var cachedFiles = Directory.GetFiles(tempPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }.Contains(
                            Path.GetExtension(f).ToLower()))
                        .OrderBy(f => f, new NaturalStringComparer())
                        .ToList();

                    if (cachedFiles.Count > 0)
                        return cachedFiles;
                }

                // Créer le dossier temporaire
                Directory.CreateDirectory(tempPath);

                // Extraire selon le type
                var extension = Path.GetExtension(archivePath).ToLower();

                if (extension == ".cbz" || extension == ".zip")
                {
                    using (var archive = System.IO.Compression.ZipFile.OpenRead(archivePath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (IsImageFile(entry.Name))
                            {
                                var destinationPath = Path.Combine(tempPath, entry.Name);
                                var dir = Path.GetDirectoryName(destinationPath);
                                if (!Directory.Exists(dir))
                                    Directory.CreateDirectory(dir);

                                entry.ExtractToFile(destinationPath, true);
                            }
                        }
                    }
                }
                // Pour CBR/RAR, il faudrait SharpCompress

                // Retourner les fichiers extraits
                return Directory.GetFiles(tempPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => IsImageFile(f))
                    .OrderBy(f => f, new NaturalStringComparer())
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur extraction archive : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<string>();
            }
        }

        private bool IsImageFile(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" }.Contains(ext);
        }

        private async Task LoadPageThumbnailsAsync()
        {
            try
            {
                LoadingText.Visibility = Visibility.Visible;

                pageThumbnails = new List<PageThumbnail>();

                // Vérifier qu'on a des images à charger
                if (imageFiles == null || imageFiles.Count == 0)
                {
                    LoadingText.Text = "Aucune image trouvée dans ce manga";
                    PageThumbnailsControl.ItemsSource = pageThumbnails;
                    return;
                }

                // Charger les 20 premières pages comme aperçu
                var previewCount = Math.Min(20, imageFiles.Count);
                LoadingText.Text = $"Chargement de {previewCount} miniatures...";

                await Task.Run(() =>
                {
                    for (int i = 0; i < previewCount; i++)
                    {
                        try
                        {
                            // Vérification de sécurité pour l'index
                            if (i >= imageFiles.Count)
                                break;

                            var imagePath = imageFiles[i];

                            // Vérifier que le fichier existe encore
                            if (!File.Exists(imagePath))
                            {
                                System.Diagnostics.Debug.WriteLine($"Fichier introuvable: {imagePath}");
                                continue;
                            }

                            Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    var thumbnail = new PageThumbnail
                                    {
                                        PageNumber = $"Page {i + 1}",
                                        PageIndex = i,
                                        Image = LoadThumbnailImage(imagePath)
                                    };

                                    pageThumbnails.Add(thumbnail);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Erreur création thumbnail {i}: {ex.Message}");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Erreur traitement image {i}: {ex.Message}");
                            // Continue avec la suivante
                        }
                    }
                });

                PageThumbnailsControl.ItemsSource = pageThumbnails;
                LoadingText.Visibility = Visibility.Collapsed;

                if (pageThumbnails.Count == 0)
                {
                    LoadingText.Text = "Aucune miniature n'a pu être chargée";
                    LoadingText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LoadingText.Text = $"Erreur lors du chargement des miniatures : {ex.Message}";
                LoadingText.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"Erreur LoadPageThumbnailsAsync: {ex}");

                // S'assurer qu'on a au moins une collection vide
                pageThumbnails = new List<PageThumbnail>();
                PageThumbnailsControl.ItemsSource = pageThumbnails;
            }
        }

        private BitmapImage LoadThumbnailImage(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Image introuvable: {imagePath}");
                    return null;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.DecodePixelWidth = 120; // Petite taille pour les miniatures
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Important pour le threading
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement image {imagePath}: {ex.Message}");
                return null;
            }
        }

        private void UpdateFavoriteButton()
        {
            FavoriteButtonText.Text = mangaInfo.IsFavorite ? "★" : "☆";
        }

        #region Event Handlers

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await database.ToggleFavoriteAsync(mangaInfo.FolderPath);
                mangaInfo.IsFavorite = !mangaInfo.IsFavorite;
                UpdateFavoriteButton();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la mise à jour des favoris : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Marquer comme lu récemment
                mangaInfo.LastRead = DateTime.Now;
                await database.UpdateLastReadAsync(mangaInfo.FolderPath, mangaInfo.LastRead.Value);

                // Ouvrir le lecteur
                var readerWindow = new MangaReaderWindow(mangaInfo);

                // Écouter la fermeture du lecteur pour fermer cette fenêtre
                readerWindow.Closed += (s, e) =>
                {
                    // Fermer cette fenêtre pour retourner à l'accueil
                    Close();
                };

                readerWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du lecteur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Mettre à jour les métadonnées dans la base de données
                mangaInfo.Author = AuthorTextBox.Text.Trim();
                mangaInfo.Tags = TagsTextBox.Text.Trim();
                mangaInfo.Characters = CharactersTextBox.Text.Trim();
                mangaInfo.Description = DescriptionTextBox.Text.Trim();

                await database.UpdateMangaMetadataAsync(mangaInfo);

                MessageBox.Show("Métadonnées sauvegardées avec succès !", "Sauvegarde", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowAllPagesButton_Click(object sender, RoutedEventArgs e)
        {
            // Ouvrir une fenêtre avec toutes les miniatures
            var allPagesWindow = new AllPagesPreviewWindow(mangaInfo, imageFiles);
            allPagesWindow.ShowDialog();
        }

        private async void PageThumbnail_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var thumbnail = border?.Tag as PageThumbnail;

            if (thumbnail != null)
            {
                try
                {
                    // Marquer comme lu récemment
                    mangaInfo.LastRead = DateTime.Now;
                    await database.UpdateLastReadAsync(mangaInfo.FolderPath, mangaInfo.LastRead.Value);

                    // Ouvrir le lecteur à la page sélectionnée
                    var readerWindow = new MangaReaderWindow(mangaInfo, thumbnail.PageIndex);

                    // Écouter la fermeture du lecteur pour fermer cette fenêtre
                    readerWindow.Closed += (s, e) =>
                    {
                        // Fermer cette fenêtre pour retourner à l'accueil
                        Close();
                    };

                    readerWindow.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}