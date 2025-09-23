using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MangaReader.Core
{
    public class ImageCacheManager
    {
        private static ImageCacheManager _instance;
        private static readonly object _lock = new object();

        private readonly ConcurrentDictionary<string, BitmapImage> _imageCache;
        private readonly ConcurrentDictionary<string, BitmapImage> _thumbnailCache;
        private readonly Queue<string> _cacheOrder;
        private readonly Queue<string> _thumbnailCacheOrder;

        private int _maxCacheSize;
        private int _maxThumbnailCacheSize;
        private long _currentCacheSizeBytes;
        private readonly object _cacheSizeLock = new object();

        private ImageCacheManager()
        {
            _imageCache = new ConcurrentDictionary<string, BitmapImage>();
            _thumbnailCache = new ConcurrentDictionary<string, BitmapImage>();
            _cacheOrder = new Queue<string>();
            _thumbnailCacheOrder = new Queue<string>();

            var settings = AppSettings.Instance;
            _maxCacheSize = settings.ImageCacheSize * 1024 * 1024; // Convert MB to bytes
            _maxThumbnailCacheSize = settings.ThumbnailCacheSize;
        }

        public static ImageCacheManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ImageCacheManager();
                        }
                    }
                }
                return _instance;
            }
        }

        public async Task<BitmapImage> GetImageAsync(string imagePath, bool isThumbnail = false, int? decodeWidth = null)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return null;

            var cacheKey = isThumbnail ? $"thumb_{imagePath}_{decodeWidth}" : imagePath;
            var cache = isThumbnail ? _thumbnailCache : _imageCache;

            // Vérifier le cache
            if (cache.TryGetValue(cacheKey, out var cachedImage))
            {
                return cachedImage;
            }

            // Charger l'image de manière asynchrone
            return await Task.Run(() =>
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;

                    if (decodeWidth.HasValue)
                    {
                        bitmap.DecodePixelWidth = decodeWidth.Value;
                    }

                    bitmap.EndInit();
                    bitmap.Freeze(); // Important pour le multi-threading

                    // Ajouter au cache approprié
                    if (isThumbnail)
                    {
                        AddToThumbnailCache(cacheKey, bitmap);
                    }
                    else
                    {
                        AddToImageCache(cacheKey, bitmap);
                    }

                    return bitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur chargement image {imagePath}: {ex.Message}");
                    return null;
                }
            });
        }

        private void AddToImageCache(string key, BitmapImage image)
        {
            lock (_cacheSizeLock)
            {
                // Estimer la taille de l'image en mémoire
                long imageSize = EstimateImageSize(image);

                // Nettoyer le cache si nécessaire
                while (_currentCacheSizeBytes + imageSize > _maxCacheSize && _cacheOrder.Count > 0)
                {
                    var oldestKey = _cacheOrder.Dequeue();
                    if (_imageCache.TryRemove(oldestKey, out var removedImage))
                    {
                        _currentCacheSizeBytes -= EstimateImageSize(removedImage);
                    }
                }

                // Ajouter la nouvelle image
                if (_imageCache.TryAdd(key, image))
                {
                    _cacheOrder.Enqueue(key);
                    _currentCacheSizeBytes += imageSize;
                }
            }
        }

        private void AddToThumbnailCache(string key, BitmapImage image)
        {
            // Nettoyer le cache si nécessaire
            while (_thumbnailCache.Count >= _maxThumbnailCacheSize && _thumbnailCacheOrder.Count > 0)
            {
                var oldestKey = _thumbnailCacheOrder.Dequeue();
                _thumbnailCache.TryRemove(oldestKey, out _);
            }

            // Ajouter la nouvelle miniature
            if (_thumbnailCache.TryAdd(key, image))
            {
                _thumbnailCacheOrder.Enqueue(key);
            }
        }

        private long EstimateImageSize(BitmapImage image)
        {
            if (image == null) return 0;

            // Estimation approximative : largeur * hauteur * 4 bytes (RGBA)
            return image.PixelWidth * image.PixelHeight * 4;
        }

        public async Task PreloadImagesAsync(List<string> imagePaths, int startIndex, int count)
        {
            var settings = AppSettings.Instance;
            if (!settings.PreloadNextPages) return;

            var tasks = new List<Task>();

            for (int i = startIndex; i < Math.Min(startIndex + count, imagePaths.Count); i++)
            {
                if (i >= 0 && i < imagePaths.Count)
                {
                    tasks.Add(GetImageAsync(imagePaths[i]));
                }
            }

            await Task.WhenAll(tasks);
        }

        public void ClearCache()
        {
            lock (_cacheSizeLock)
            {
                _imageCache.Clear();
                _cacheOrder.Clear();
                _currentCacheSizeBytes = 0;
            }
        }

        public void ClearThumbnailCache()
        {
            _thumbnailCache.Clear();
            _thumbnailCacheOrder.Clear();
        }

        public void ClearAllCaches()
        {
            ClearCache();
            ClearThumbnailCache();
        }

        public void UpdateCacheSettings()
        {
            var settings = AppSettings.Instance;
            _maxCacheSize = settings.ImageCacheSize * 1024 * 1024;
            _maxThumbnailCacheSize = settings.ThumbnailCacheSize;
        }

        public (long usedBytes, int imageCount, int thumbnailCount) GetCacheStats()
        {
            return (_currentCacheSizeBytes, _imageCache.Count, _thumbnailCache.Count);
        }
    }
}