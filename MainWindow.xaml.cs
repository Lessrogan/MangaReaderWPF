using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MangaReader
{
    public partial class MainWindow : Window
    {
        private MangaDatabase database;
        private ObservableCollection<MangaInfo> recentMangas;
        private ObservableCollection<MangaInfo> allMangas;
        private string mangaFolderPath;

        public MainWindow()
        {
            InitializeComponent();
            InitializeApplication();

            // Rafraîchir les récents quand la fenêtre redevient active
            this.Activated += async (s, e) => await RefreshRecentMangas();
        }

        private async void InitializeApplication()
        {
            try
            {
                // Initialiser la base de données
                database = new MangaDatabase();
                await database.InitializeAsync();

                // Initialiser les collections
                recentMangas = new ObservableCollection<MangaInfo>();
                allMangas = new ObservableCollection<MangaInfo>();

                RecentMangasList.ItemsSource = recentMangas;
                AllMangasList.ItemsSource = allMangas;

                // Configurer les événements de clic sur les cartes
                RecentMangasList.MouseLeftButtonUp += MangaCard_Click;
                AllMangasList.MouseLeftButtonUp += MangaCard_Click;

                // Définir le dossier par défaut (à modifier selon vos besoins)
                mangaFolderPath = @"E:\Utils\Mangas"; // Changez ce chemin

                // Charger les mangas
                await LoadMangasAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'initialisation : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadMangasAsync()
        {
            try
            {
                if (!Directory.Exists(mangaFolderPath))
                {
                    NoMangasText.Visibility = Visibility.Visible;
                    NoRecentMangasText.Visibility = Visibility.Visible;
                    MessageBox.Show($"Le dossier de mangas n'existe pas : {mangaFolderPath}\n\nVeuillez vérifier le chemin dans le code (ligne 42).",
                                  "Dossier introuvable", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Vider les collections
                allMangas.Clear();
                recentMangas.Clear();

                // Parcourir les dossiers de mangas
                var mangaDirectories = Directory.GetDirectories(mangaFolderPath);

                if (mangaDirectories == null || mangaDirectories.Length == 0)
                {
                    NoMangasText.Visibility = Visibility.Visible;
                    NoRecentMangasText.Visibility = Visibility.Visible;
                    MessageBox.Show($"Aucun dossier trouvé dans : {mangaFolderPath}\n\nAjoutez des dossiers de mangas dans ce répertoire.",
                                  "Aucun manga", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateUI();
                    return;
                }

                foreach (var mangaDir in mangaDirectories)
                {
                    try
                    {
                        var mangaInfo = await CreateMangaInfoFromDirectory(mangaDir);
                        if (mangaInfo != null)
                        {
                            allMangas.Add(mangaInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur lors du traitement du dossier {mangaDir}: {ex.Message}");
                        // Continue avec les autres dossiers
                    }
                }

                // Charger les mangas récents depuis la base de données
                await RefreshRecentMangas();

                // Mettre à jour l'interface
                UpdateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des mangas : {ex.Message}\n\nVérifiez :\n- Le chemin du dossier de mangas\n- Les permissions d'accès\n- La structure des dossiers",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Erreur LoadMangasAsync: {ex}");
            }
        }

        private async System.Threading.Tasks.Task<MangaInfo> CreateMangaInfoFromDirectory(string mangaDir)
        {
            try
            {
                if (!Directory.Exists(mangaDir))
                    return null;

                var mangaName = Path.GetFileName(mangaDir);
                if (string.IsNullOrEmpty(mangaName))
                    return null;

                var coverImagePath = FindCoverImage(mangaDir);

                // Vérifier si le manga existe déjà dans la base de données
                MangaInfo existingManga = null;
                try
                {
                    existingManga = await database.GetMangaByPathAsync(mangaDir);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur base de données pour {mangaDir}: {ex.Message}");
                    // Continue sans les données de la DB
                }

                var mangaInfo = new MangaInfo
                {
                    Title = existingManga?.Title ?? mangaName,
                    Author = existingManga?.Author ?? "Auteur inconnu",
                    FolderPath = mangaDir,
                    CoverImage = LoadImageFromPath(coverImagePath),
                    LastRead = existingManga?.LastRead,
                    IsFavorite = existingManga?.IsFavorite ?? false,
                    Tags = existingManga?.Tags ?? "",
                    Characters = existingManga?.Characters ?? "",
                    Description = existingManga?.Description ?? ""
                };

                // Ajouter à la base de données si nouveau
                if (existingManga == null)
                {
                    try
                    {
                        await database.AddMangaAsync(mangaInfo);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur ajout DB pour {mangaDir}: {ex.Message}");
                        // Continue sans sauvegarder en DB
                    }
                }

                return mangaInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur CreateMangaInfoFromDirectory pour {mangaDir}: {ex.Message}");
                return null;
            }
        }

        private string FindCoverImage(string mangaDir)
        {
            try
            {
                if (!Directory.Exists(mangaDir))
                    return null;

                var imageExtensions = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp" };

                foreach (var extension in imageExtensions)
                {
                    var files = Directory.GetFiles(mangaDir, extension, SearchOption.AllDirectories);
                    if (files != null && files.Length > 0)
                    {
                        return files.OrderBy(f => f).First(); // Prendre la première image par ordre alphabétique
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur FindCoverImage pour {mangaDir}: {ex.Message}");
            }

            return null;
        }

        private BitmapImage LoadImageFromPath(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return null;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.DecodePixelWidth = 200; // Optimisation mémoire
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private async System.Threading.Tasks.Task RefreshRecentMangas()
        {
            try
            {
                // Vider la liste des récents
                recentMangas.Clear();

                // Recharger les mangas récents depuis la base de données
                var recentMangaInfos = await database.GetRecentMangasAsync(6);
                if (recentMangaInfos != null)
                {
                    foreach (var recentInfo in recentMangaInfos)
                    {
                        // Chercher le manga correspondant dans la liste complète
                        var mangaInfo = allMangas.FirstOrDefault(m => m.FolderPath == recentInfo.FolderPath);
                        if (mangaInfo != null)
                        {
                            // Mettre à jour la date de lecture
                            mangaInfo.LastRead = recentInfo.LastRead;

                            // Ajouter à la liste des récents (éviter les doublons)
                            if (!recentMangas.Any(rm => rm.FolderPath == mangaInfo.FolderPath))
                            {
                                recentMangas.Add(mangaInfo);
                            }
                        }
                    }
                }

                // Mettre à jour l'interface
                UpdateUI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur RefreshRecentMangas: {ex.Message}");
            }
        }

        private void UpdateUI()
        {
            try
            {
                // Mettre à jour les compteurs avec vérifications
                var totalCount = allMangas?.Count ?? 0;
                var favoritesCount = allMangas?.Count(m => m.IsFavorite) ?? 0;
                var recentCount = recentMangas?.Count ?? 0;

                TotalMangasText.Text = $"Total: {totalCount} mangas";
                FavoritesCountText.Text = $"Favoris: {favoritesCount}";
                RecentCountText.Text = $"Lus récemment: {recentCount}";
                MangaCountText.Text = $"({totalCount} mangas)";

                // Afficher/masquer les messages
                NoMangasText.Visibility = totalCount == 0 ? Visibility.Visible : Visibility.Collapsed;
                NoRecentMangasText.Visibility = recentCount == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur UpdateUI: {ex.Message}");
                // Valeurs par défaut en cas d'erreur
                TotalMangasText.Text = "Total: 0 mangas";
                FavoritesCountText.Text = "Favoris: 0";
                RecentCountText.Text = "Lus récemment: 0";
                MangaCountText.Text = "(0 mangas)";
            }
        }

        private async void MangaCard_Click(object sender, MouseButtonEventArgs e)
        {
            var frameworkElement = e.OriginalSource as FrameworkElement;
            var mangaInfo = frameworkElement?.DataContext as MangaInfo;

            if (mangaInfo != null)
            {
                await OpenManga(mangaInfo);
            }
        }

        private async System.Threading.Tasks.Task OpenManga(MangaInfo mangaInfo)
        {
            try
            {
                // Ouvrir la fenêtre de détails au lieu du lecteur directement
                var detailsWindow = new MangaDetailsWindow(mangaInfo);

                // Écouter la fermeture pour rafraîchir
                detailsWindow.Closed += async (s, e) => await RefreshRecentMangas();

                detailsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du manga : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Événements des boutons

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            // Filtrer par favoris
            var favorites = allMangas.Where(m => m.IsFavorite).ToList();

            if (favorites.Any())
            {
                MessageBox.Show($"Vous avez {favorites.Count} manga(s) en favoris", "Favoris", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Aucun manga en favoris", "Favoris", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void RandomMangaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!allMangas.Any())
            {
                MessageBox.Show("Aucun manga disponible", "Manga aléatoire", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var random = new Random();
            var randomManga = allMangas[random.Next(allMangas.Count)];

            var result = MessageBox.Show($"Manga sélectionné : {randomManga.Title}\n\nVoulez-vous l'ouvrir ?",
                                       "Manga aléatoire", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await OpenManga(randomManga);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadMangasAsync();
            MessageBox.Show("Liste des mangas actualisée", "Actualisation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Fenêtre des paramètres à implémenter", "Paramètres", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }

    // Classe pour les informations des mangas
    public class MangaInfo
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string FolderPath { get; set; }
        public BitmapImage CoverImage { get; set; }
        public DateTime? LastRead { get; set; }
        public bool IsFavorite { get; set; }
        public string Tags { get; set; }
        public string Characters { get; set; }
        public string Description { get; set; }

        public string LastReadFormatted
        {
            get
            {
                if (LastRead.HasValue)
                {
                    var days = (DateTime.Now - LastRead.Value).Days;
                    if (days == 0) return "Aujourd'hui";
                    if (days == 1) return "Hier";
                    if (days < 7) return $"Il y a {days} jours";
                    if (days < 30) return $"Il y a {days / 7} semaines";
                    return $"Il y a {days / 30} mois";
                }
                return "Jamais lu";
            }
        }
    }
}