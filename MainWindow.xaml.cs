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

        // Variables de pagination
        private int currentPage = 1;
        private int totalPages = 1;
        private int itemsPerPage = 20;
        private string currentSortBy = "Name";
        private bool sortAscending = true;
        private bool isInitialized = false;
        private List<MangaInfoViewModel> sortedMangas = new List<MangaInfoViewModel>();


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

                // Charger les paramètres de pagination
                itemsPerPage = settings.ItemsPerPage;
                currentSortBy = settings.DefaultSortBy;
                sortAscending = settings.SortAscending;

                // Sélectionner la bonne option dans la ComboBox
                foreach (ComboBoxItem item in ItemsPerPageComboBox.Items)
                {
                    if (item.Content.ToString() == itemsPerPage.ToString())
                    {
                        item.IsSelected = true;
                        break;
                    }
                }

                // Si la valeur sauvegardée n'est pas dans la liste, utiliser 20 par défaut
                if (ItemsPerPageComboBox.SelectedItem == null)
                {
                    itemsPerPage = 20;
                    settings.ItemsPerPage = 20;
                    await settings.SaveAsync();

                    // Sélectionner 20 dans la ComboBox
                    foreach (ComboBoxItem item in ItemsPerPageComboBox.Items)
                    {
                        if (item.Content.ToString() == "20")
                        {
                            item.IsSelected = true;
                            break;
                        }
                    }
                }

                // Initialiser la ComboBox avec la bonne valeur
                InitializeItemsPerPageComboBox();

                // Initialiser les contrôles de tri
                InitializeSortControls();

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

        private void InitializeItemsPerPageComboBox()
        {
            // Sélectionner la bonne option selon la valeur sauvegardée
            foreach (ComboBoxItem item in ItemsPerPageComboBox.Items)
            {
                if (item.Content.ToString() == itemsPerPage.ToString())
                {
                    item.IsSelected = true;
                    break;
                }
            }
        }

        private void InitializeSortControls()
        {
            // Sélectionner l'option de tri par défaut
            switch (currentSortBy)
            {
                case "Name":
                    SortByComboBox.SelectedIndex = 0;
                    break;
                case "DateAdded":
                    SortByComboBox.SelectedIndex = 1;
                    break;
                case "LastRead":
                    SortByComboBox.SelectedIndex = 2;
                    break;
                case "Author":
                    SortByComboBox.SelectedIndex = 3;
                    break;
            }

            // Définir l'ordre de tri
            SortOrderButton.Content = sortAscending ? "↑" : "↓";
            SortOrderButton.ToolTip = sortAscending ? "Ordre croissant" : "Ordre décroissant";
        }

        private IEnumerable<MangaInfoViewModel> ApplySorting(IEnumerable<MangaInfoViewModel> mangas)
        {
            switch (currentSortBy)
            {
                case "Name":
                    return sortAscending
                        ? mangas.OrderBy(m => m.Title)
                        : mangas.OrderByDescending(m => m.Title);

                case "DateAdded":
                    // Vous pourriez ajouter une propriété DateAdded dans MangaInfo
                    return sortAscending
                        ? mangas.OrderBy(m => m.FolderPath) // Temporaire, utilisez DateAdded quand disponible
                        : mangas.OrderByDescending(m => m.FolderPath);

                case "LastRead":
                    return sortAscending
                        ? mangas.OrderBy(m => m.LastRead ?? DateTime.MinValue)
                        : mangas.OrderByDescending(m => m.LastRead ?? DateTime.MinValue);

                case "Author":
                    return sortAscending
                        ? mangas.OrderBy(m => m.Author)
                        : mangas.OrderByDescending(m => m.Author);

                default:
                    return mangas;
            }
        }

        private void CalculatePagination()
        {
            int totalItems = sortedMangas.Count;
            totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);

            // S'assurer que la page actuelle est valide
            if (currentPage > totalPages)
                currentPage = Math.Max(1, totalPages);

            UpdatePaginationControls();
        }

        private void DisplayCurrentPage()
        {
            filteredMangas.Clear();

            if (sortedMangas.Any())
            {
                int startIndex = (currentPage - 1) * itemsPerPage;
                var itemsToDisplay = sortedMangas.Skip(startIndex).Take(itemsPerPage).ToList();

                // Debug pour vérifier
                System.Diagnostics.Debug.WriteLine($"Page {currentPage}: Affichage de {itemsToDisplay.Count} items (début: {startIndex}, par page: {itemsPerPage})");

                foreach (var manga in itemsToDisplay)
                {
                    filteredMangas.Add(manga);
                }

                // Charger les images pour cette page seulement
                _ = LoadVisibleImagesAsync();
            }

            UpdatePaginationInfo();
            UpdatePaginationControls();
        }

        private void UpdatePaginationControls()
        {
            // Activer/désactiver les boutons selon la page actuelle
            FirstPageButton.IsEnabled = currentPage > 1;
            PreviousPageButton.IsEnabled = currentPage > 1;
            NextPageButton.IsEnabled = currentPage < totalPages;
            LastPageButton.IsEnabled = currentPage < totalPages;

            // Mettre à jour le texte de la page actuelle
            CurrentPageText.Text = totalPages > 0 ? $"Page {currentPage} / {totalPages}" : "Page 0 / 0";

            // Mettre à jour l'info d'affichage
            UpdatePaginationInfo();
        }

        private void UpdatePaginationInfo()
        {
            if (sortedMangas.Any())
            {
                int startIndex = (currentPage - 1) * itemsPerPage + 1;
                int endIndex = Math.Min(currentPage * itemsPerPage, sortedMangas.Count);

                // Afficher par exemple "Affichage 21-40 sur 100"
                var infoText = $"Affichage {startIndex}-{endIndex} sur {sortedMangas.Count}";

                // Si des filtres sont actifs, ajouter le total non filtré
                if (sortedMangas.Count != allMangas.Count)
                {
                    infoText += $" (Total: {allMangas.Count})";
                }

                PaginationInfoText.Text = infoText;
            }
            else
            {
                PaginationInfoText.Text = "Aucun manga à afficher";
                CurrentPageText.Text = "Page 0 / 0";
            }
        }

        // Event handlers pour la pagination
        private void FirstPage_Click(object sender, RoutedEventArgs e)
        {
            currentPage = 1;
            DisplayCurrentPage();
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                DisplayCurrentPage();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                DisplayCurrentPage();
            }
        }

        private void LastPage_Click(object sender, RoutedEventArgs e)
        {
            currentPage = totalPages;
            DisplayCurrentPage();
        }

        private void SortByComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized) return;

            switch (SortByComboBox.SelectedIndex)
            {
                case 0:
                    currentSortBy = "Name";
                    break;
                case 1:
                    currentSortBy = "DateAdded";
                    break;
                case 2:
                    currentSortBy = "LastRead";
                    break;
                case 3:
                    currentSortBy = "Author";
                    break;
            }

            currentPage = 1; // Retour à la première page
            ApplyFilters();
        }

        private void SortOrderButton_Click(object sender, RoutedEventArgs e)
        {
            sortAscending = !sortAscending;
            SortOrderButton.Content = sortAscending ? "↑" : "↓";
            SortOrderButton.ToolTip = sortAscending ? "Ordre croissant" : "Ordre décroissant";

            currentPage = 1; // Retour à la première page
            ApplyFilters();
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

                isInitialized = true;

                // Charger les mangas récents
                await RefreshRecentMangas();

                // Appliquer les filtres ET la pagination
                currentPage = 1; // Commencer à la page 1
                ApplyFilters();

                // Mettre à jour l'interface
                UpdateUI();
                UpdateCacheStats();

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
            // Ne charger que les images des mangas actuellement affichés
            var mangasToLoad = filteredMangas.Where(m => m.CoverImage == null).ToList();
            System.Diagnostics.Debug.WriteLine($"Chargement de {mangasToLoad.Count} images");

            foreach (var manga in mangasToLoad)
            {
                manga.CoverImage = await LoadCoverImageAsync(manga.FolderPath, manga.IsArchive);
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
            if (allMangas == null || filteredMangas == null)
                return;

            sortedMangas.Clear();

            var query = allMangas.AsEnumerable();

            // ÉTAPE 1 : APPLIQUER LES FILTRES

            // Filtre 1 : Recherche textuelle
            if (!string.IsNullOrWhiteSpace(currentSearchTerm))
            {
                var searchLower = currentSearchTerm.ToLower();
                query = query.Where(m =>
                    m.Title.ToLower().Contains(searchLower) ||
                    m.Author.ToLower().Contains(searchLower) ||
                    m.Characters.ToLower().Contains(searchLower) ||
                    m.Description.ToLower().Contains(searchLower));
            }

            // Filtre 2 : Par tag
            if (!string.IsNullOrWhiteSpace(currentTagFilter))
            {
                query = query.Where(m => !string.IsNullOrEmpty(m.Tags) && m.Tags.Contains(currentTagFilter));
            }

            // Filtre 3 : Favoris uniquement
            if (showFavoritesOnly)
            {
                query = query.Where(m => m.IsFavorite);
            }

            // ÉTAPE 2 : APPLIQUER LE TRI SUR LES RÉSULTATS FILTRÉS
            query = ApplySorting(query);

            // ÉTAPE 3 : STOCKER LES RÉSULTATS
            sortedMangas = query.ToList();

            // ÉTAPE 4 : CALCULER ET AFFICHER LA PAGINATION
            CalculatePagination();
            DisplayCurrentPage();

            // Mettre à jour les textes d'information
            UpdateInfoTexts();
        }

        private void UpdateInfoTexts()
        {
            int totalFiltered = sortedMangas.Count;
            int totalMangas = allMangas.Count;

            // Afficher le nombre de résultats
            if (totalFiltered == totalMangas)
            {
                // Aucun filtre actif
                PaginationInfoText.Text = $"{totalFiltered} manga{(totalFiltered > 1 ? "s" : "")} trouvé{(totalFiltered > 1 ? "s" : "")}";
            }
            else
            {
                // Des filtres sont actifs
                PaginationInfoText.Text = $"{totalFiltered} manga{(totalFiltered > 1 ? "s" : "")} trouvé{(totalFiltered > 1 ? "s" : "")} sur {totalMangas}";
            }

            // Construire le texte des filtres actifs
            var activeFilters = new List<string>();

            if (!string.IsNullOrWhiteSpace(currentSearchTerm))
                activeFilters.Add($"recherche: \"{currentSearchTerm}\"");

            if (!string.IsNullOrWhiteSpace(currentTagFilter))
                activeFilters.Add($"tag: {currentTagFilter}");

            if (showFavoritesOnly)
                activeFilters.Add("favoris");

            if (activeFilters.Any())
            {
                SearchResultText.Text = $"Filtres actifs : {string.Join(", ", activeFilters)}";
                SearchResultText.Visibility = Visibility.Visible;
            }
            else
            {
                SearchResultText.Visibility = Visibility.Collapsed;
            }
        }

        // Ajout de la gestion du changement de nombre d'items par page
        private async void ItemsPerPageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized) return;

            var selectedItem = (ComboBoxItem)ItemsPerPageComboBox.SelectedItem;
            if (selectedItem != null)
            {
                itemsPerPage = int.Parse(selectedItem.Content.ToString());
                currentPage = 1; // Retour à la première page

                // Sauvegarder la préférence
                settings.ItemsPerPage = itemsPerPage;
                await settings.SaveAsync();

                // Réappliquer l'affichage
                CalculatePagination();
                DisplayCurrentPage();
            }
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