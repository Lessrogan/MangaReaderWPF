using MangaReader.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MangaReader
{
    public partial class MainWindow : Window
    {
        private MangaDatabase database;
        private ImageCacheManager cacheManager;
        private AppSettings settings;

        private ObservableCollection<MangaInfoViewModel> recentMangas;
        private ObservableCollection<MangaInfoViewModel> allMangas;
        private ObservableCollection<MangaInfoViewModel> filteredMangas;

        private List<string> allTags;
        private string currentSearchTerm = "";
        private string currentTagFilter = null;
        private bool showFavoritesOnly = false;

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
                // Initialiser les composants
                settings = AppSettings.Instance;
                cacheManager = ImageCacheManager.Instance;
                database = new MangaDatabase();
                await database.InitializeAsync();

                // Initialiser les collections
                recentMangas = new ObservableCollection<MangaInfoViewModel>();
                allMangas = new ObservableCollection<MangaInfoViewModel>();
                filteredMangas = new ObservableCollection<MangaInfoViewModel>();

                RecentMangasList.ItemsSource = recentMangas;
                AllMangasList.ItemsSource = filteredMangas;

                // Configurer les événements de clic sur les cartes
                RecentMangasList.MouseLeftButtonUp += MangaCard_Click;
                AllMangasList.MouseLeftButtonUp += MangaCard_Click;

                // Initialiser la zone de recherche
                SearchTextBox.Text = SearchTextBox.Tag.ToString();
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;

                // Charger les tags
                await LoadTagsAsync();

                // Charger les mangas
                await LoadMangasAsync();

                // Mettre à jour les statistiques du cache
                UpdateCacheStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'initialisation : {ex.Message}", "Erreur",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadTagsAsync()
        {
            try
            {
                allTags = await database.GetAllTagsAsync();

                TagFilterComboBox.Items.Clear();
                TagFilterComboBox.Items.Add(new ComboBoxItem { Content = "Tous les tags", IsSelected = true });

                foreach (var tag in allTags)
                {
                    TagFilterComboBox.Items.Add(new ComboBoxItem { Content = tag });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement tags: {ex.Message}");
            }
        }

        private async Task LoadMangasAsync()
        {
            try
            {
                var mangaFolderPath = settings.MangaFolderPath;

                if (!Directory.Exists(mangaFolderPath))
                {
                    NoMangasText.Visibility = Visibility.Visible;
                    NoRecentMangasText.Visibility = Visibility.Visible;

                    var result = MessageBox.Show(
                        $"Le dossier de mangas n'existe pas : {mangaFolderPath}\n\n" +
                        "Voulez-vous configurer un nouveau dossier ?",
                        "Dossier introuvable", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        SettingsButton_Click(null, null);
                    }
                    return;
                }

                // Vider les collections
                allMangas.Clear();
                recentMangas.Clear();
                filteredMangas.Clear();

                // Parcourir les dossiers et fichiers de mangas
                var items = new List<string>();

                // Ajouter les dossiers
                items.AddRange(Directory.GetDirectories(mangaFolderPath));

                // Si support des archives, ajouter les fichiers CBZ/CBR
                if (settings.SupportArchiveFiles)
                {
                    var archiveExtensions = new[] { "*.cbz", "*.cbr", "*.zip", "*.rar" };
                    foreach (var ext in archiveExtensions)
                    {
                        items.AddRange(Directory.GetFiles(mangaFolderPath, ext));
                    }
                }

                if (items.Count == 0)
                {
                    NoMangasText.Visibility = Visibility.Visible;
                    NoRecentMangasText.Visibility = Visibility.Visible;
                    UpdateUI();
                    return;
                }

                // Charger les mangas avec lazy loading si activé
                var loadTasks = new List<Task<MangaInfoViewModel>>();

                foreach (var item in items)
                {
                    if (settings.EnableLazyLoading)
                    {
                        // Chargement différé - juste créer l'objet sans charger l'image
                        var mangaInfo = await CreateMangaInfoFromItemAsync(item, false);
                        if (mangaInfo != null)
                        {
                            allMangas.Add(mangaInfo);
                        }
                    }
                    else
                    {
                        // Chargement complet
                        loadTasks.Add(CreateMangaInfoFromItemAsync(item, true));
                    }
                }

                if (!settings.EnableLazyLoading)
                {
                    var results = await Task.WhenAll(loadTasks);
                    foreach (var mangaInfo in results.Where(m => m != null))
                    {
                        allMangas.Add(mangaInfo);
                    }
                }

                // Charger les mangas récents
                await RefreshRecentMangas();

                // Appliquer les filtres
                ApplyFilters();

                // Mettre à jour l'interface
                UpdateUI();

                // Si lazy loading, charger les images visibles
                if (settings.EnableLazyLoading)
                {
                    _ = LoadVisibleImagesAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des mangas : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<MangaInfoViewModel> CreateMangaInfoFromItemAsync(string itemPath, bool loadImage)
        {
            try
            {
                var mangaName = Path.GetFileName(itemPath);
                bool isArchive = File.Exists(itemPath) &&
                                new[] { ".cbz", ".cbr", ".zip", ".rar" }.Contains(Path.GetExtension(itemPath).ToLower());

                var mangaInfo = new MangaInfoViewModel
                {
                    Title = mangaName ?? "Sans titre",
                    Author = "Auteur inconnu",
                    FolderPath = itemPath,
                    LastRead = null,
                    IsFavorite = false,
                    Tags = "",
                    Characters = "",
                    Description = "",
                    IsArchive = isArchive,
                    Rating = 0,
                    PageCount = 0,
                    CoverImage = null
                };

                // Charger l'image si demandé
                if (loadImage)
                {
                    try
                    {
                        string coverPath = null;

                        if (!isArchive && Directory.Exists(itemPath))
                        {
                            // Chercher une image dans le dossier
                            var imageExtensions = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.webp" };
                            string firstImage = null;

                            foreach (var ext in imageExtensions)
                            {
                                var images = Directory.GetFiles(itemPath, ext, SearchOption.AllDirectories);
                                if (images.Length > 0)
                                {
                                    firstImage = images.OrderBy(f => f).First();
                                    break;
                                }
                            }

                            coverPath = firstImage;
                        }

                        if (!string.IsNullOrEmpty(coverPath) && File.Exists(coverPath))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(coverPath);
                            bitmap.DecodePixelWidth = 200;  // Taille miniature
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();

                            mangaInfo.CoverImage = bitmap;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur chargement image: {ex.Message}");
                        // Ignorer les erreurs de chargement d'image
                    }
                }

                // Ajouter à la base de données
                try
                {
                    await database.AddMangaAsync(mangaInfo);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur ajout DB: {ex.Message}");
                }

                return mangaInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur: {ex.Message}");
                return null;
            }
        }

        private async Task<BitmapImage> LoadCoverImageAsync(string itemPath, bool isArchive)
        {
            try
            {
                string coverImagePath = null;

                if (isArchive)
                {
                    // Extraire la première image de l'archive
                    var images = await ArchiveHandler.GetArchiveImagesAsync(itemPath);
                    if (images.Any())
                    {
                        coverImagePath = images.First();
                    }
                }
                else
                {
                    // Chercher une image dans le dossier
                    coverImagePath = FindCoverImage(itemPath);
                }

                if (!string.IsNullOrEmpty(coverImagePath))
                {
                    return await cacheManager.GetImageAsync(coverImagePath, true, 200);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement cover {itemPath}: {ex.Message}");
            }

            return null;
        }

        private string FindCoverImage(string mangaDir)
        {
            try
            {
                if (!Directory.Exists(mangaDir))
                    return null;

                var imageExtensions = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.webp" };

                foreach (var extension in imageExtensions)
                {
                    var files = Directory.GetFiles(mangaDir, extension, SearchOption.AllDirectories);
                    if (files != null && files.Length > 0)
                    {
                        return files.OrderBy(f => f).First();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur FindCoverImage {mangaDir}: {ex.Message}");
            }

            return null;
        }

        private async Task LoadVisibleImagesAsync()
        {
            // Charger les images des mangas visibles de manière asynchrone
            foreach (var manga in filteredMangas.Take(20))
            {
                if (manga.CoverImage == null)
                {
                    manga.CoverImage = await LoadCoverImageAsync(manga.FolderPath, manga.IsArchive);
                }
            }
        }

        private async Task RefreshRecentMangas()
        {
            try
            {
                recentMangas.Clear();

                var recentMangaInfos = await database.GetRecentMangasAsync(6);
                if (recentMangaInfos != null)
                {
                    foreach (var recentInfo in recentMangaInfos)
                    {
                        var mangaInfo = allMangas.FirstOrDefault(m => m.FolderPath == recentInfo.FolderPath);
                        if (mangaInfo != null)
                        {
                            mangaInfo.LastRead = recentInfo.LastRead;

                            if (!recentMangas.Any(rm => rm.FolderPath == mangaInfo.FolderPath))
                            {
                                recentMangas.Add(mangaInfo);
                            }
                        }
                    }
                }

                UpdateUI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur RefreshRecentMangas: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            filteredMangas.Clear();

            var query = allMangas.AsEnumerable();

            // Recherche textuelle
            if (!string.IsNullOrWhiteSpace(currentSearchTerm))
            {
                var searchLower = currentSearchTerm.ToLower();
                query = query.Where(m =>
                    m.Title.ToLower().Contains(searchLower) ||
                    m.Author.ToLower().Contains(searchLower) ||
                    m.Characters.ToLower().Contains(searchLower) ||
                    m.Description.ToLower().Contains(searchLower));
            }

            // Filtre par tag
            if (!string.IsNullOrWhiteSpace(currentTagFilter))
            {
                query = query.Where(m => m.Tags.Contains(currentTagFilter));
            }

            // Filtre favoris
            if (showFavoritesOnly)
            {
                query = query.Where(m => m.IsFavorite);
            }

            // Tri
            switch (SortComboBox?.SelectedIndex ?? 0)
            {
                case 0: // Nom
                    query = query.OrderBy(m => m.Title);
                    break;
                case 1: // Date de lecture
                    query = query.OrderByDescending(m => m.LastRead ?? DateTime.MinValue);
                    break;
                case 2: // Note
                    query = query.OrderByDescending(m => m.Rating);
                    break;
                case 3: // Auteur
                    query = query.OrderBy(m => m.Author);
                    break;
            }

            foreach (var manga in query)
            {
                filteredMangas.Add(manga);
            }

            UpdateSearchResultText();
        }

        private void UpdateSearchResultText()
        {
            if (!string.IsNullOrWhiteSpace(currentSearchTerm) ||
                !string.IsNullOrWhiteSpace(currentTagFilter) ||
                showFavoritesOnly)
            {
                var resultCount = filteredMangas.Count;
                var totalCount = allMangas.Count;

                SearchResultText.Text = $"Résultats : {resultCount} manga(s) sur {totalCount}";
                SearchResultText.Visibility = Visibility.Visible;

                if (!string.IsNullOrWhiteSpace(currentSearchTerm))
                {
                    AllMangasSectionTitle.Text = $"Résultats de recherche pour \"{currentSearchTerm}\"";
                }
                else if (showFavoritesOnly)
                {
                    AllMangasSectionTitle.Text = "Mangas favoris";
                }
                else if (!string.IsNullOrWhiteSpace(currentTagFilter))
                {
                    AllMangasSectionTitle.Text = $"Mangas avec le tag \"{currentTagFilter}\"";
                }
            }
            else
            {
                SearchResultText.Visibility = Visibility.Collapsed;
                AllMangasSectionTitle.Text = "Tous les mangas disponibles";
            }
        }

        private void UpdateUI()
        {
            try
            {
                var totalCount = allMangas?.Count ?? 0;
                var favoritesCount = allMangas?.Count(m => m.IsFavorite) ?? 0;
                var recentCount = recentMangas?.Count ?? 0;
                var archivesCount = allMangas?.Count(m => m.IsArchive) ?? 0;

                TotalMangasText.Text = $"Total: {totalCount} mangas";
                FavoritesCountText.Text = $"Favoris: {favoritesCount}";
                RecentCountText.Text = $"Lus récemment: {recentCount}";
                ArchivesCountText.Text = $"Archives: {archivesCount}";
                MangaCountText.Text = $"({filteredMangas?.Count ?? 0} affichés)";

                NoMangasText.Visibility = totalCount == 0 ? Visibility.Visible : Visibility.Collapsed;
                NoRecentMangasText.Visibility = recentCount == 0 ? Visibility.Visible : Visibility.Collapsed;

                // Masquer la section récents si on est en mode recherche/filtrage
                RecentSection.Visibility = (string.IsNullOrWhiteSpace(currentSearchTerm) &&
                                           string.IsNullOrWhiteSpace(currentTagFilter) &&
                                           !showFavoritesOnly)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur UpdateUI: {ex.Message}");
            }
        }

        private void UpdateCacheStats()
        {
            var stats = cacheManager.GetCacheStats();
            var usedMB = stats.usedBytes / (1024.0 * 1024.0);
            CacheInfoText.Text = $"Images: {usedMB:F1} MB";
        }

        #region Event Handlers

        private async void MangaCard_Click(object sender, MouseButtonEventArgs e)
        {
            var frameworkElement = e.OriginalSource as FrameworkElement;
            var mangaInfo = frameworkElement?.DataContext as MangaInfoViewModel;

            if (mangaInfo != null)
            {
                await OpenManga(mangaInfo);
            }
        }

        private async Task OpenManga(MangaInfo mangaInfo)
        {
            try
            {
                var detailsWindow = new MangaDetailsWindow(mangaInfo);
                detailsWindow.Closed += async (s, e) => await RefreshRecentMangas();
                detailsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du manga : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == SearchTextBox.Tag.ToString())
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = SearchTextBox.Tag.ToString();
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchTextBox.Text != SearchTextBox.Tag.ToString())
            {
                currentSearchTerm = SearchTextBox.Text;
                ClearSearchButton.Visibility = string.IsNullOrWhiteSpace(currentSearchTerm)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                ApplyFilters();
            }
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = SearchTextBox.Tag.ToString();
            SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            currentSearchTerm = "";
            ClearSearchButton.Visibility = Visibility.Collapsed;
            ApplyFilters();
        }

        private void TagFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Protection contre l'appel prématuré
            if (allMangas == null || filteredMangas == null)
                return;
            if (TagFilterComboBox.SelectedIndex == 0)
            {
                currentTagFilter = null;
            }
            else if (TagFilterComboBox.SelectedItem is ComboBoxItem item)
            {
                currentTagFilter = item.Content.ToString();
            }
            ApplyFilters();
        }

        private void FavoritesFilterCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            showFavoritesOnly = FavoritesFilterCheckBox.IsChecked ?? false;
            ApplyFilters();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (allMangas != null)
            {
                ApplyFilters();
            }
        }

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            FavoritesFilterCheckBox.IsChecked = !FavoritesFilterCheckBox.IsChecked;
        }

        private async void RandomMangaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!filteredMangas.Any())
            {
                MessageBox.Show("Aucun manga disponible", "Manga aléatoire",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var random = new Random();
            var randomManga = filteredMangas[random.Next(filteredMangas.Count)];

            var result = MessageBox.Show(
                $"Manga sélectionné : {randomManga.Title}\n\nVoulez-vous l'ouvrir ?",
                "Manga aléatoire", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await OpenManga(randomManga);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            cacheManager.ClearAllCaches();
            await LoadMangasAsync();
            await LoadTagsAsync();
            UpdateCacheStats();
            MessageBox.Show("Liste des mangas actualisée et cache vidé",
                          "Actualisation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ImportExportButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowPanel("ImportExport");
            settingsWindow.ShowDialog();
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            var result = settingsWindow.ShowDialog();

            if (result == true)
            {
                // Recharger si le dossier a changé
                if (settings.MangaFolderPath != AppSettings.Instance.MangaFolderPath)
                {
                    settings = AppSettings.Instance;
                    await LoadMangasAsync();
                }

                UpdateCacheStats();
            }
        }

        #endregion
    }

    // ViewModel pour MangaInfo avec INotifyPropertyChanged
    public class MangaInfoViewModel : MangaInfo, INotifyPropertyChanged
    {
        private BitmapImage _coverImage;

        public new BitmapImage CoverImage
        {
            get => _coverImage;
            set
            {
                _coverImage = value;
                OnPropertyChanged();
            }
        }

        public bool IsArchive { get; set; }

        public bool HasRating => Rating > 0;

        public string RatingStars
        {
            get
            {
                if (Rating <= 0) return "";
                return new string('★', Math.Min(Rating, 5)) + new string('☆', Math.Max(0, 5 - Rating));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}