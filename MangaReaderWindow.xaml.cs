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

        private bool isFitToWindow = true;
        private double actualImageWidth = 0;
        private double actualImageHeight = 0;

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
                    MessageBox.Show("Aucune image trouvée dans ce manga.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                // Initialiser l'affichage des pages (au lieu du slider)
                TotalPagesText.Text = imageFiles.Count.ToString();
                CurrentPageTextBox.Text = (currentPageIndex + 1).ToString();

                // Marquer comme initialisé
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

        private void UpdatePageDisplay()
        {
            if (!isInitialized || imageFiles == null || !imageFiles.Any())
                return;

            var readingMode = ReadingModeComboBox.SelectedIndex;

            // Mettre à jour le TextBox avec le numéro de page
            CurrentPageTextBox.Text = (currentPageIndex + 1).ToString();
            TotalPagesText.Text = imageFiles.Count.ToString();

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

        // Méthode pour gérer l'entrée directe du numéro de page
        private void CurrentPageTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Accepter uniquement les chiffres
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void CurrentPageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (int.TryParse(CurrentPageTextBox.Text, out int pageNumber))
                {
                    // Convertir en index (base 0)
                    int targetIndex = pageNumber - 1;

                    // Vérifier les limites
                    if (targetIndex >= 0 && targetIndex < imageFiles.Count)
                    {
                        currentPageIndex = targetIndex;
                        LoadCurrentPage();
                    }
                    else
                    {
                        // Restaurer la valeur correcte
                        CurrentPageTextBox.Text = (currentPageIndex + 1).ToString();
                        MessageBox.Show($"Entrez un numéro entre 1 et {imageFiles.Count}",
                                      "Page invalide", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    // Restaurer la valeur correcte
                    CurrentPageTextBox.Text = (currentPageIndex + 1).ToString();
                }

                // Retirer le focus du TextBox
                Keyboard.ClearFocus();
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
            if (!isInitialized)
                return;

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
                        UpdateFitToWindowButton(); // Nouvelle ligne
                        break;
                    case 1: // Double page
                        LoadDoublePage();
                        break;
                    case 2: // Défilement continu
                        LoadContinuousMode();
                        break;
                }

                CurrentPageTextBox.Text = (currentPageIndex + 1).ToString();
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

            // Sauvegarder les dimensions réelles de l'image
            actualImageWidth = bitmap.PixelWidth;
            actualImageHeight = bitmap.PixelHeight;

            // Appliquer le mode de redimensionnement actuel
            ApplyImageStretch();
        }

        private void FitToWindowButton_Click(object sender, RoutedEventArgs e)
        {
            isFitToWindow = !isFitToWindow;
            ApplyImageStretch();
            UpdateFitToWindowButton();
        }

        private void ApplyImageStretch()
        {
            if (CurrentPageImage == null) return;

            if (isFitToWindow)
            {
                // Ajuster complètement à la fenêtre SANS scrollbars
                CurrentPageImage.Stretch = System.Windows.Media.Stretch.Uniform;
                CurrentPageImage.Width = double.NaN;  // Auto
                CurrentPageImage.Height = double.NaN; // Auto
                CurrentPageImage.MaxWidth = SinglePageScrollViewer.ViewportWidth;
                CurrentPageImage.MaxHeight = SinglePageScrollViewer.ViewportHeight;
                CurrentPageImage.HorizontalAlignment = HorizontalAlignment.Center;
                CurrentPageImage.VerticalAlignment = VerticalAlignment.Center;

                // Désactiver les scrollbars quand ajusté à la fenêtre
                SinglePageScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                SinglePageScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

                // Réinitialiser la position
                SinglePageScrollViewer.ScrollToVerticalOffset(0);
                SinglePageScrollViewer.ScrollToHorizontalOffset(0);
            }
            else
            {
                // Taille réelle avec scrollbars si nécessaire
                CurrentPageImage.Stretch = System.Windows.Media.Stretch.None;
                CurrentPageImage.Width = actualImageWidth;
                CurrentPageImage.Height = actualImageHeight;
                CurrentPageImage.MaxWidth = double.PositiveInfinity;
                CurrentPageImage.MaxHeight = double.PositiveInfinity;
                CurrentPageImage.HorizontalAlignment = HorizontalAlignment.Center;
                CurrentPageImage.VerticalAlignment = VerticalAlignment.Center;

                // Réactiver les scrollbars pour la taille réelle
                SinglePageScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                SinglePageScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (isFitToWindow && ReadingModeComboBox?.SelectedIndex == 0)
            {
                // Réappliquer le redimensionnement quand la fenêtre change de taille
                ApplyImageStretch();
            }
        }

        private void UpdateFitToWindowButton()
        {
            if (FitToWindowButton == null) return;

            // Mettre à jour l'apparence du bouton selon l'état
            if (isFitToWindow)
            {
                FitToWindowButton.ToolTip = "Afficher en taille réelle";
                var textBlock = FitToWindowButton.Content as TextBlock;
                if (textBlock != null)
                {
                    textBlock.Text = "⇔"; // Icône d'expansion
                }
                FitToWindowButton.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0, 122, 204)); // Bleu quand actif
            }
            else
            {
                FitToWindowButton.ToolTip = "Ajuster à la fenêtre";
                var textBlock = FitToWindowButton.Content as TextBlock;
                if (textBlock != null)
                {
                    textBlock.Text = "↔"; // Icône de contraction
                }
                FitToWindowButton.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(45, 45, 48)); // Gris quand inactif
            }

            // Masquer le bouton si on n'est pas en mode page simple
            FitToWindowButton.Visibility = ReadingModeComboBox.SelectedIndex == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
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
                UpdatePageDisplay();
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
                UpdatePageDisplay();
            }
        }

        private void FirstPageButton_Click(object sender, RoutedEventArgs e)
        {
            currentPageIndex = 0;
            LoadCurrentPage();
            UpdatePageDisplay();
        }

        private void LastPageButton_Click(object sender, RoutedEventArgs e)
        {
            currentPageIndex = imageFiles.Count - 1;
            LoadCurrentPage();
            UpdatePageDisplay();

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

        private void LeftZone_Click(object sender, MouseButtonEventArgs e)
        {
            // Clic à gauche = page précédente
            PreviousPageButton_Click(sender, null);
            e.Handled = true;
        }

        private void RightZone_Click(object sender, MouseButtonEventArgs e)
        {
            // Clic à droite = page suivante
            NextPageButton_Click(sender, null);
            e.Handled = true;
        }

        // Optionnel : Ajouter une indication visuelle au survol
        private void Zone_MouseEnter(object sender, MouseEventArgs e)
        {
            var border = sender as Border;
            if (border != null)
            {
                // Créer un effet de survol subtil
                border.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(20, 255, 255, 255));
            }
        }

        private void Zone_MouseLeave(object sender, MouseEventArgs e)
        {
            var border = sender as Border;
            if (border != null)
            {
                border.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        #endregion

        #region Control Events

        private void ReadingModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized)
                return;

            LoadCurrentPage();

            // Masquer/afficher le bouton de redimensionnement selon le mode
            UpdateFitToWindowButton();
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
                case Key.F: // F pour Fit
                    if (ReadingModeComboBox.SelectedIndex == 0) // Si en mode page simple
                    {
                        FitToWindowButton_Click(sender, null);
                    }
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