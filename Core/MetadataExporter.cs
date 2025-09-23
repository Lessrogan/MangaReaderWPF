using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MangaReader.Core
{
    public class MetadataExporter
    {
        public class MangaMetadata
        {
            public string Title { get; set; }
            public string Author { get; set; }
            public string FolderPath { get; set; }
            public string RelativePath { get; set; }
            public DateTime? LastRead { get; set; }
            public bool IsFavorite { get; set; }
            public string Tags { get; set; }
            public string Characters { get; set; }
            public string Description { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime UpdatedDate { get; set; }
            public int? LastPageRead { get; set; }
            public double? LastZoomLevel { get; set; }
        }

        public class ExportData
        {
            public string ExportVersion { get; set; } = "1.0";
            public DateTime ExportDate { get; set; }
            public string ExportedFrom { get; set; }
            public List<MangaMetadata> Mangas { get; set; }
        }

        public static async Task<bool> ExportToJsonAsync(string filePath, List<MangaInfo> mangas)
        {
            try
            {
                var exportData = new ExportData
                {
                    ExportDate = DateTime.Now,
                    ExportedFrom = Environment.MachineName,
                    Mangas = new List<MangaMetadata>()
                };

                foreach (var manga in mangas)
                {
                    var metadata = new MangaMetadata
                    {
                        Title = manga.Title,
                        Author = manga.Author,
                        FolderPath = manga.FolderPath,
                        RelativePath = GetRelativePath(manga.FolderPath),
                        LastRead = manga.LastRead,
                        IsFavorite = manga.IsFavorite,
                        Tags = manga.Tags,
                        Characters = manga.Characters,
                        Description = manga.Description,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };

                    // Récupérer la position de lecture si disponible
                    var readingPosition = await GetReadingPositionAsync(manga.FolderPath);
                    if (readingPosition != null)
                    {
                        metadata.LastPageRead = readingPosition.Item1;
                        metadata.LastZoomLevel = readingPosition.Item2;
                    }

                    exportData.Mangas.Add(metadata);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(exportData, options);
                await File.WriteAllTextAsync(filePath, json);

                // Sauvegarder le dernier chemin d'export
                AppSettings.Instance.LastExportPath = Path.GetDirectoryName(filePath);
                await AppSettings.Instance.SaveAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'export : {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> ExportToXmlAsync(string filePath, List<MangaInfo> mangas)
        {
            try
            {
                var exportData = new ExportData
                {
                    ExportDate = DateTime.Now,
                    ExportedFrom = Environment.MachineName,
                    Mangas = new List<MangaMetadata>()
                };

                foreach (var manga in mangas)
                {
                    var metadata = new MangaMetadata
                    {
                        Title = manga.Title,
                        Author = manga.Author,
                        FolderPath = manga.FolderPath,
                        RelativePath = GetRelativePath(manga.FolderPath),
                        LastRead = manga.LastRead,
                        IsFavorite = manga.IsFavorite,
                        Tags = manga.Tags,
                        Characters = manga.Characters,
                        Description = manga.Description,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };

                    var readingPosition = await GetReadingPositionAsync(manga.FolderPath);
                    if (readingPosition != null)
                    {
                        metadata.LastPageRead = readingPosition.Item1;
                        metadata.LastZoomLevel = readingPosition.Item2;
                    }

                    exportData.Mangas.Add(metadata);
                }

                var serializer = new XmlSerializer(typeof(ExportData));
                using (var stream = File.Create(filePath))
                {
                    serializer.Serialize(stream, exportData);
                }

                AppSettings.Instance.LastExportPath = Path.GetDirectoryName(filePath);
                await AppSettings.Instance.SaveAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'export XML : {ex.Message}");
                return false;
            }
        }

        public static async Task<List<MangaMetadata>> ImportFromJsonAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var exportData = JsonSerializer.Deserialize<ExportData>(json);

                if (exportData?.Mangas != null)
                {
                    AppSettings.Instance.LastImportPath = Path.GetDirectoryName(filePath);
                    await AppSettings.Instance.SaveAsync();
                    return exportData.Mangas;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'import JSON : {ex.Message}");
            }

            return new List<MangaMetadata>();
        }

        public static async Task<List<MangaMetadata>> ImportFromXmlAsync(string filePath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(ExportData));
                using (var stream = File.OpenRead(filePath))
                {
                    var exportData = serializer.Deserialize(stream) as ExportData;
                    if (exportData?.Mangas != null)
                    {
                        AppSettings.Instance.LastImportPath = Path.GetDirectoryName(filePath);
                        await AppSettings.Instance.SaveAsync();
                        return exportData.Mangas;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'import XML : {ex.Message}");
            }

            return new List<MangaMetadata>();
        }

        public static async Task<bool> ApplyImportedMetadata(List<MangaMetadata> importedData, MangaDatabase database)
        {
            try
            {
                var basePath = AppSettings.Instance.MangaFolderPath;
                int successCount = 0;

                foreach (var metadata in importedData)
                {
                    try
                    {
                        // Essayer de trouver le manga par son chemin relatif
                        var fullPath = Path.Combine(basePath, metadata.RelativePath ?? "");

                        if (!Directory.Exists(fullPath))
                        {
                            // Essayer de trouver par le nom du dossier
                            var mangaName = Path.GetFileName(metadata.RelativePath ?? metadata.Title);
                            fullPath = Path.Combine(basePath, mangaName);
                        }

                        if (Directory.Exists(fullPath))
                        {
                            var mangaInfo = new MangaInfo
                            {
                                Title = metadata.Title,
                                Author = metadata.Author,
                                FolderPath = fullPath,
                                LastRead = metadata.LastRead,
                                IsFavorite = metadata.IsFavorite,
                                Tags = metadata.Tags,
                                Characters = metadata.Characters,
                                Description = metadata.Description
                            };

                            await database.AddMangaAsync(mangaInfo);

                            // Restaurer la position de lecture si disponible
                            if (metadata.LastPageRead.HasValue)
                            {
                                await SaveReadingPositionAsync(fullPath, metadata.LastPageRead.Value, metadata.LastZoomLevel ?? 1.0);
                            }

                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur lors de l'application des métadonnées pour {metadata.Title}: {ex.Message}");
                    }
                }

                return successCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'application des métadonnées : {ex.Message}");
                return false;
            }
        }

        private static string GetRelativePath(string fullPath)
        {
            var basePath = AppSettings.Instance.MangaFolderPath;
            if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            return Path.GetFileName(fullPath);
        }

        private static async Task<Tuple<int, double>> GetReadingPositionAsync(string mangaPath)
        {
            try
            {
                var database = new MangaDatabase();
                await database.InitializeAsync();
                return await database.GetReadingPositionAsync(mangaPath);
            }
            catch
            {
                return null;
            }
        }

        private static async Task SaveReadingPositionAsync(string mangaPath, int pageIndex, double zoomLevel)
        {
            try
            {
                var database = new MangaDatabase();
                await database.InitializeAsync();
                await database.SaveReadingPositionAsync(mangaPath, pageIndex, zoomLevel);
            }
            catch
            {
                // Ignorer les erreurs
            }
        }
    }
}