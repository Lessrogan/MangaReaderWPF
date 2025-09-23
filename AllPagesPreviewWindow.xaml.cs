using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MangaReader
{
    public partial class AllPagesPreviewWindow : Window
    {
        private MangaInfo mangaInfo;
        private List<string> imageFiles;
        private List<PageThumbnailFull> pageThumbnails;
        private double currentThumbnailSize = 150;

        public AllPagesPreviewWindow(MangaInfo manga, List<string> images)
        {
            InitializeComponent();
            mangaInfo = manga;
            imageFiles = images;

            MangaTitleText.Text = manga.Title;
            PageCountText.Text = $"{images.Count} pages";

            LoadAllPagesAsync();
        }

        private async void LoadAllPagesAsync()
        {
            try
            {
                LoadingProgressBar.Maximum = imageFiles.Count;
                LoadingProgressBar.Value = 0;
                StatusText.Text = "Chargement des miniatures...";

                pageThumbnails = new List<PageThumbnailFull>();

                await Task.Run(async () =>
                {
                    for (int i = 0; i < imageFiles.Count; i++)
                    {
                        try
                        {
                            var imagePath = imageFiles[i];

                            await Dispatcher.InvokeAsync(() =>
                            {
                                var thumbnail = new PageThumbnailFull
                                {
                                    PageNumber = $"Page {i + 1}",
                                    PageIndex = i,
                                    Image = LoadThumbnailImage(imagePath),
                                    ThumbnailSize = currentThumbnailSize,
                                    ThumbnailHeight = currentThumbnailSize * 1.4 // Ratio manga typique
                                };

                                pageThumbnails.Add(thumbnail);

                                // Mettre à jour la progression
                                LoadingProgressBar.Value = i + 1;
                                StatusText.Text = $"Chargé {i + 1}/{imageFiles.Count} pages";
                            });

                            // Petite pause pour ne pas surcharger l'UI
                            if (i % 10 == 0)
                            {
                                await Task.Delay(10);
                            }
                        }
                        catch
                        {
                            // Ignorer les images corrompues
                            await Dispatcher.InvokeAsync(() =>
                            {
                                LoadingProgressBar.Value = i + 1;
                            });
                        }
                    }
                });

                AllPagesControl.ItemsSource = pageThumbnails;
                LoadingProgressBar.Visibility = Visibility.Collapsed;
                StatusText.Text = $"{pageThumbnails.Count} pages chargées";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Erreur lors du chargement : {ex.Message}";
                LoadingProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private BitmapImage LoadThumbnailImage(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.DecodePixelWidth = (int)currentThumbnailSize;
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

        private void ThumbnailSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentThumbnailSize = e.NewValue;

            if (pageThumbnails != null)
            {
                foreach (var thumbnail in pageThumbnails)
                {
                    thumbnail.ThumbnailSize = currentThumbnailSize;
                    thumbnail.ThumbnailHeight = currentThumbnailSize * 1.4;
                }

                // Forcer la mise à jour de l'affichage
                AllPagesControl.ItemsSource = null;
                AllPagesControl.ItemsSource = pageThumbnails;
            }
        }

        private async void PageThumbnail_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var thumbnail = border?.Tag as PageThumbnailFull;

            if (thumbnail != null)
            {
                try
                {
                    // Ouvrir le lecteur à la page sélectionnée
                    var readerWindow = new MangaReaderWindow(mangaInfo, thumbnail.PageIndex);
                    readerWindow.Show();

                    // Fermer cette fenêtre
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture du lecteur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void StartReadingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Commencer la lecture depuis la première page
                var readerWindow = new MangaReaderWindow(mangaInfo, 0);
                readerWindow.Show();

                // Fermer cette fenêtre
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du lecteur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}