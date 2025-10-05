using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MangaReader
{
    public partial class MangaReaderWindow : Window
    {
        private MangaInfo mangaInfo;
        private List<string> imageFiles;
        private int currentPageIndex = 0;
        private bool isFullscreen = false;
        private WindowState previousWindowState;
        private WindowStyle previousWindowStyle;
        private bool isInitialized = false; // Flag pour éviter les appels prématurés

        public MangaReaderWindow(MangaInfo manga)
        {
            InitializeComponent();
            mangaInfo = manga;
            DataContext = manga;

            // Initialiser les listes avant tout
            imageFiles = new List<string>();

            LoadMangaPages();
            UpdateFavoriteButton();
        }

        public MangaReaderWindow(MangaInfo manga, int startPageIndex)
        {
            InitializeComponent();
            mangaInfo = manga;
            DataContext = manga;

            // Initialiser les listes avant tout
            imageFiles = new List<string>();
            currentPageIndex = startPageIndex;

            LoadMangaPages();
            UpdateFavoriteButton();
        }

        private void LoadMangaPages()
        {
            try
            {
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

                // Vérifier si c'est une archive
                bool isArchive = File.Exists(mangaInfo.FolderPath) &&
                                new[] { ".cbz", ".cbr", ".zip", ".rar" }.Contains(
                                    Path.GetExtension(mangaInfo.FolderPath).ToLower());

                if (isArchive)
                {
                    // Extraire l'archive de manière synchrone pour le moment
                    var tempPath = Path.Combine(Path.GetTempPath(), "MangaReader",
                                               Path.GetFileNameWithoutExtension(mangaInfo.FolderPath));

                    if (Directory.Exists(tempPath))
                    {
                        imageFiles = Directory.GetFiles(tempPath, "*.*", SearchOption.AllDirectories)
                            .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()))
                            .OrderBy(f => f, new NaturalStringComparer())
                            .ToList();
                    }
                    else
                    {
                        // Il faudrait extraire d'abord
                        ExtractArchiveSync(mangaInfo.FolderPath, tempPath);

                        imageFiles = Directory.GetFiles(tempPath, "*.*", SearchOption.AllDirectories)
                            .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()))
                            .OrderBy(f => f, new NaturalStringComparer())
                            .ToList();
                    }
                }
                else
                {
                    // Dossier normal
                    imageFiles = Directory.GetFiles(mangaInfo.FolderPath, "*.*", SearchOption.AllDirectories)
                        .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()))
                        .OrderBy(f => f, new NaturalStringComparer())
                        .ToList();
                }

                if (!imageFiles.Any())
                {
                    MessageBox.Show("Aucune image trouvée dans ce manga.",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                // Initialiser les contrôles seulement après avoir chargé les images
                PageSlider.Maximum = imageFiles.Count;
                PageSlider.Value = currentPageIndex + 1;

                // Marquer comme initialisé AVANT les appels qui déclenchent des événements
                isInitialized = true;

                UpdatePageDisplay();
                LoadCurrentPage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des pages : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void ExtractArchiveSync(string archivePath, string outputPath)
        {
            Directory.CreateDirectory(outputPath);

            var extension = Path.GetExtension(archivePath).ToLower();
            if (extension == ".cbz" || extension == ".zip")
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(archivePath, outputPath, true);
            }
        }

        private void LoadCurrentPage()
        {
            // Ne rien faire si pas encore initialisé
            if (!isInitialized)
                return;

            // Vérification de sécurité
            if (imageFiles == null || !imageFiles.Any())
            {
                MessageBox.Show("Aucune image chargée.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (currentPageIndex < 0 || currentPageIndex >= imageFiles.Count)
                return;

            try
            {
                var readingMode = ReadingModeComboBox.SelectedIndex;

                switch (readingMode)
                {
                    case 0: // Page simple
                        LoadSinglePage();
                        break;
                    case 1: // Double page
                        LoadDoublePage();
                        break;
                    case 2: // Défilement continu
                        LoadContinuousMode();
                        break;
                }

                PageSlider.Value = currentPageIndex + 1;
                UpdatePageDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de la page : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSinglePage()
        {
            SinglePageScrollViewer.Visibility = Visibility.Visible;
            DoublePageScrollViewer.Visibility = Visibility.Collapsed;
            ContinuousScrollViewer.Visibility = Visibility.Collapsed;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imageFiles[currentPageIndex]);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            CurrentPageImage.Source = bitmap;
        }

        private void LoadDoublePage()
        {
            SinglePageScrollViewer.Visibility = Visibility.Collapsed;
            DoublePageScrollViewer.Visibility = Visibility.Visible;
            ContinuousScrollViewer.Visibility = Visibility.Collapsed;

            // Page de droite (lecture japonaise)
            if (currentPageIndex < imageFiles.Count)
            {
                var rightBitmap = new BitmapImage();
                rightBitmap.BeginInit();
                rightBitmap.UriSource = new Uri(imageFiles[currentPageIndex]);
                rightBitmap.CacheOption = BitmapCacheOption.OnLoad;
                rightBitmap.EndInit();
                RightPageImage.Source = rightBitmap;
            }

            // Page de gauche
            if (currentPageIndex + 1 < imageFiles.Count)
            {
                var leftBitmap = new BitmapImage();
                leftBitmap.BeginInit();
                leftBitmap.UriSource = new Uri(imageFiles[currentPageIndex + 1]);
                leftBitmap.CacheOption = BitmapCacheOption.OnLoad;
                leftBitmap.EndInit();
                LeftPageImage.Source = leftBitmap;
            }
            else
            {
                LeftPageImage.Source = null;
            }
        }

        private void LoadContinuousMode()
        {
            SinglePageScrollViewer.Visibility = Visibility.Collapsed;
            DoublePageScrollViewer.Visibility = Visibility.Collapsed;
            ContinuousScrollViewer.Visibility = Visibility.Visible;

            var imageSources = new List<BitmapImage>();

            foreach (var imagePath in imageFiles)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.DecodePixelWidth = 800; // Optimisation
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    imageSources.Add(bitmap);
                }
                catch
                {
                    // Ignorer les images corrompues
                }
            }

            AllPagesItemsControl.ItemsSource = imageSources;
        }

        private void UpdatePageDisplay()
        {
            // Vérification de sécurité
            if (!isInitialized || imageFiles == null || !imageFiles.Any())
                return;

            var readingMode = ReadingModeComboBox.SelectedIndex;

            if (readingMode == 1) // Double page
            {
                PageInfoText.Text = $"Pages {currentPageIndex + 1}-{Math.Min(currentPageIndex + 2, imageFiles.Count)} / {imageFiles.Count}";
            }
            else if (readingMode == 2) // Continu
            {
                PageInfoText.Text = $"{imageFiles.Count} pages";
            }
            else
            {
                PageInfoText.Text = $"Page {currentPageIndex + 1} / {imageFiles.Count}";
            }
        }

        private void UpdateFavoriteButton()
        {
            FavoriteButtonText.Text = mangaInfo.IsFavorite ? "★" : "☆";
        }

        #region Navigation Events

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            var readingMode = ReadingModeComboBox.SelectedIndex;
            var step = readingMode == 1 ? 2 : 1; // Double page = 2, sinon 1

            if (currentPageIndex - step >= 0)
            {
                currentPageIndex -= step;
                LoadCurrentPage();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            var readingMode = ReadingModeComboBox.SelectedIndex;
            var step = readingMode == 1 ? 2 : 1;

            if (currentPageIndex + step < imageFiles.Count)
            {
                currentPageIndex += step;
                LoadCurrentPage();
            }
        }

        private void FirstPageButton_Click(object sender, RoutedEventArgs e)
        {
            currentPageIndex = 0;
            LoadCurrentPage();
        }

        private void LastPageButton_Click(object sender, RoutedEventArgs e)
        {
            currentPageIndex = imageFiles.Count - 1;
            LoadCurrentPage();
        }

        private void PageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Ne rien faire si pas encore initialisé
            if (!isInitialized)
                return;

            if (Math.Abs(e.NewValue - (currentPageIndex + 1)) > 0.5) // Éviter les boucles
            {
                currentPageIndex = (int)e.NewValue - 1;
                LoadCurrentPage();
            }
        }

        private void PageImage_Click(object sender, MouseButtonEventArgs e)
        {
            // Clic pour passer à la page suivante
            NextPageButton_Click(sender, null);
        }

        #endregion

        #region Control Events

        private void ReadingModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ne rien faire si pas encore initialisé
            if (!isInitialized)
                return;

            LoadCurrentPage();
        }

        private async void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var database = new MangaDatabase();
                await database.InitializeAsync();
                await database.ToggleFavoriteAsync(mangaInfo.FolderPath);
                mangaInfo.IsFavorite = !mangaInfo.IsFavorite;
                UpdateFavoriteButton();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la mise à jour des favoris : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isFullscreen)
            {
                previousWindowState = WindowState;
                previousWindowStyle = WindowStyle;

                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                isFullscreen = true;
            }
            else
            {
                WindowStyle = previousWindowStyle;
                WindowState = previousWindowState;
                isFullscreen = false;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.A:
                    PreviousPageButton_Click(sender, null);
                    break;
                case Key.Right:
                case Key.D:
                    NextPageButton_Click(sender, null);
                    break;
                case Key.Home:
                    FirstPageButton_Click(sender, null);
                    break;
                case Key.End:
                    LastPageButton_Click(sender, null);
                    break;
                case Key.F11:
                    FullscreenButton_Click(sender, null);
                    break;
                case Key.Escape:
                    if (isFullscreen)
                        FullscreenButton_Click(sender, null);
                    else
                        Close();
                    break;
            }
        }

        #endregion
    }

    // Comparateur pour tri naturel des fichiers (1, 2, 10 au lieu de 1, 10, 2)
    public class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var xParts = SplitIntoAlphaNumericParts(x);
            var yParts = SplitIntoAlphaNumericParts(y);

            for (int i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
            {
                var xPart = xParts[i];
                var yPart = yParts[i];

                if (int.TryParse(xPart, out int xNum) && int.TryParse(yPart, out int yNum))
                {
                    int result = xNum.CompareTo(yNum);
                    if (result != 0) return result;
                }
                else
                {
                    int result = string.Compare(xPart, yPart, StringComparison.OrdinalIgnoreCase);
                    if (result != 0) return result;
                }
            }

            return xParts.Length.CompareTo(yParts.Length);
        }

        private string[] SplitIntoAlphaNumericParts(string input)
        {
            var parts = new List<string>();
            var current = "";
            bool isDigit = false;

            foreach (char c in input)
            {
                bool charIsDigit = char.IsDigit(c);

                if (current.Length > 0 && charIsDigit != isDigit)
                {
                    parts.Add(current);
                    current = "";
                }

                current += c;
                isDigit = charIsDigit;
            }

            if (current.Length > 0)
                parts.Add(current);

            return parts.ToArray();
        }
    }
}