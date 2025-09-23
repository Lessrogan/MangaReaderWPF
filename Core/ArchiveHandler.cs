using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;

namespace MangaReader.Core
{
    public class ArchiveHandler
    {
        private static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        private static readonly string[] SupportedArchiveExtensions = { ".cbz", ".cbr", ".zip", ".rar" };

        public static bool IsArchiveFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return SupportedArchiveExtensions.Contains(extension);
        }

        public static async Task<List<string>> ExtractArchiveAsync(string archivePath, string outputDirectory)
        {
            var extractedFiles = new List<string>();

            try
            {
                // Créer le dossier de sortie
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var extension = Path.GetExtension(archivePath).ToLower();

                if (extension == ".cbz" || extension == ".zip")
                {
                    extractedFiles = await ExtractZipAsync(archivePath, outputDirectory);
                }
                else if (extension == ".cbr" || extension == ".rar")
                {
                    extractedFiles = await ExtractRarAsync(archivePath, outputDirectory);
                }

                // Trier les fichiers naturellement
                extractedFiles = extractedFiles
                    .OrderBy(f => f, new NaturalStringComparer())
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'extraction de l'archive : {ex.Message}");
                throw;
            }

            return extractedFiles;
        }

        private static async Task<List<string>> ExtractZipAsync(string zipPath, string outputDirectory)
        {
            var extractedFiles = new List<string>();

            await Task.Run(() =>
            {
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (IsImageFile(entry.Name))
                        {
                            var destinationPath = Path.Combine(outputDirectory, entry.Name);
                            var destinationDir = Path.GetDirectoryName(destinationPath);

                            if (!Directory.Exists(destinationDir))
                            {
                                Directory.CreateDirectory(destinationDir);
                            }

                            entry.ExtractToFile(destinationPath, true);
                            extractedFiles.Add(destinationPath);
                        }
                    }
                }
            });

            return extractedFiles;
        }

        private static async Task<List<string>> ExtractRarAsync(string rarPath, string outputDirectory)
        {
            var extractedFiles = new List<string>();

            await Task.Run(() =>
            {
                using (var archive = RarArchive.Open(rarPath))
                {
                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        if (IsImageFile(entry.Key))
                        {
                            var destinationPath = Path.Combine(outputDirectory, entry.Key);
                            var destinationDir = Path.GetDirectoryName(destinationPath);

                            if (!Directory.Exists(destinationDir))
                            {
                                Directory.CreateDirectory(destinationDir);
                            }

                            entry.WriteToFile(destinationPath, new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });

                            extractedFiles.Add(destinationPath);
                        }
                    }
                }
            });

            return extractedFiles;
        }

        private static bool IsImageFile(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return SupportedImageExtensions.Contains(extension);
        }

        public static string GetTempExtractionPath(string archivePath)
        {
            var tempPath = Path.GetTempPath();
            var mangaName = Path.GetFileNameWithoutExtension(archivePath);
            var hash = archivePath.GetHashCode().ToString("X");
            return Path.Combine(tempPath, "MangaReader", $"{mangaName}_{hash}");
        }

        public static void CleanupTempFiles(string tempPath)
        {
            try
            {
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du nettoyage des fichiers temporaires : {ex.Message}");
            }
        }

        public static async Task<List<string>> GetArchiveImagesAsync(string archivePath)
        {
            var tempPath = GetTempExtractionPath(archivePath);

            // Vérifier si déjà extrait
            if (Directory.Exists(tempPath))
            {
                var existingFiles = Directory.GetFiles(tempPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => IsImageFile(f))
                    .OrderBy(f => f, new NaturalStringComparer())
                    .ToList();

                if (existingFiles.Count > 0)
                {
                    return existingFiles;
                }
            }

            // Extraire l'archive
            return await ExtractArchiveAsync(archivePath, tempPath);
        }
    }
}