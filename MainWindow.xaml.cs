// MainWindow.xaml.cs
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
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
                mangaFolderPath = @"E:\Utils\Mangas";

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
                    return;
                }

                // Vider les collections
                allMangas.Clear();
                recentMangas.Clear();

                // Parcourir les dossiers de mangas
                var mangaDirectories = Directory.GetDirectories(mangaFolderPath);

                foreach (var mangaDir in mangaDirectories)
                {
                    var mangaInfo = await CreateMangaInfoFromDirectory(mangaDir);
                    if (mangaInfo != null)
                    {
                        allMangas.Add(mangaInfo);
                    }
                }

                // Charger les mangas récents depuis la base de données
                var recentMangaInfos = await database.GetRecentMangasAsync(6);
                foreach (var recentInfo in recentMangaInfos)
                {
                    var mangaInfo = allMangas.FirstOrDefault(m => m.FolderPath == recentInfo.FolderPath);
                    if (mangaInfo != null)
                    {
                        mangaInfo.LastRead = recentInfo.LastRead;
                        recentMangas.Add(mangaInfo);
                    }
                }

                // Mettre à jour l'interface
                UpdateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des mangas : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task<MangaInfo> CreateMangaInfoFromDirectory(string mangaDir)
        {
            try
            {
                var mangaName = Path.GetFileName(mangaDir);
                var coverImagePath = FindCoverImage(mangaDir);

                // Vérifier si le manga existe déjà dans la base de données
                var existingManga = await database.GetMangaByPathAsync(mangaDir);

                var mangaInfo = new MangaInfo
                {
                    Title = existingManga?.Title ?? mangaName,
                    Author = existingManga?.Author ?? "Auteur inconnu",
                    FolderPath = mangaDir,
                    CoverImage = LoadImageFromPath(coverImagePath),
                    LastRead = existingManga?.LastRead,
                    IsFavorite = existingManga?.IsFavorite ?? false
                };

                // Ajouter à la base de données si nouveau
                if (existingManga == null)
                {
                    await database.AddMangaAsync(mangaInfo);
                }

                return mangaInfo;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string FindCoverImage(string mangaDir)
        {
            try
            {
                var imageExtensions = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp" };

                foreach (var extension in imageExtensions)
                {
                    var files = Directory.GetFiles(mangaDir, extension, SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        return files.OrderBy(f => f).First(); // Prendre la première image par ordre alphabétique
                    }
                }
            }
            catch { }

            return null;
        }

        private BitmapImage LoadImageFromPath(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                // Retourner une image par défaut ou null
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

        private void UpdateUI()
        {
            // Mettre à jour les compteurs
            var totalCount = allMangas.Count;
            var favoritesCount = allMangas.Count(m => m.IsFavorite);
            var recentCount = recentMangas.Count;

            TotalMangasText.Text = $"Total: {totalCount} mangas";
            FavoritesCountText.Text = $"Favoris: {favoritesCount}";
            RecentCountText.Text = $"Lus récemment: {recentCount}";
            MangaCountText.Text = $"({totalCount} mangas)";

            // Afficher/masquer les messages
            NoMangasText.Visibility = totalCount == 0 ? Visibility.Visible : Visibility.Collapsed;
            NoRecentMangasText.Visibility = recentCount == 0 ? Visibility.Visible : Visibility.Collapsed;
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
                // Marquer comme lu récemment
                mangaInfo.LastRead = DateTime.Now;
                await database.UpdateLastReadAsync(mangaInfo.FolderPath, mangaInfo.LastRead.Value);

                // Ouvrir la fenêtre de lecture
                var readerWindow = new MangaReaderWindow(mangaInfo);
                readerWindow.Show();

                // Actualiser l'affichage après fermeture
                readerWindow.Closed += async (s, e) => await LoadMangasAsync();
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
                // Créer une nouvelle fenêtre ou filtrer l'affichage actuel
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
            // Ouvrir la fenêtre des paramètres
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