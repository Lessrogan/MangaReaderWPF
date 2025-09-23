using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using MangaReader.Core;

namespace MangaReader
{
    public class MangaDatabase
    {
        private string connectionString;
        private string dbPath;

        public MangaDatabase()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var mangaReaderPath = Path.Combine(appDataPath, "MangaReader");

            if (!Directory.Exists(mangaReaderPath))
            {
                Directory.CreateDirectory(mangaReaderPath);
            }

            dbPath = Path.Combine(mangaReaderPath, "mangas.db");
            connectionString = $"Data Source={dbPath};Version=3;";
        }

        public async Task InitializeAsync()
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            // Table principale des mangas
            var createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Mangas (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Author TEXT,
                    FolderPath TEXT UNIQUE NOT NULL,
                    LastRead DATETIME,
                    IsFavorite BOOLEAN DEFAULT 0,
                    Tags TEXT,
                    Characters TEXT,
                    Description TEXT,
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    IsArchive BOOLEAN DEFAULT 0,
                    ArchivePath TEXT,
                    PageCount INTEGER DEFAULT 0,
                    Rating INTEGER DEFAULT 0
                )";

            using (var command = new SQLiteCommand(createTableQuery, connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            // Table pour les positions de lecture
            var createReadingPositionTable = @"
                CREATE TABLE IF NOT EXISTS ReadingPositions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MangaPath TEXT UNIQUE NOT NULL,
                    PageIndex INTEGER NOT NULL,
                    ZoomLevel REAL DEFAULT 1.0,
                    ScrollPosition REAL DEFAULT 0,
                    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            using (var command = new SQLiteCommand(createReadingPositionTable, connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            // Table pour l'historique de lecture
            var createHistoryTable = @"
                CREATE TABLE IF NOT EXISTS ReadingHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MangaPath TEXT NOT NULL,
                    PageIndex INTEGER NOT NULL,
                    ReadDate DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            using (var command = new SQLiteCommand(createHistoryTable, connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            // Index pour améliorer les performances
            var createIndexes = @"
                CREATE INDEX IF NOT EXISTS idx_mangas_lastread ON Mangas(LastRead);
                CREATE INDEX IF NOT EXISTS idx_mangas_favorite ON Mangas(IsFavorite);
                CREATE INDEX IF NOT EXISTS idx_history_date ON ReadingHistory(ReadDate);
                CREATE INDEX IF NOT EXISTS idx_history_manga ON ReadingHistory(MangaPath);";

            using (var command = new SQLiteCommand(createIndexes, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task AddMangaAsync(MangaInfo manga)
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT OR REPLACE INTO Mangas 
                (Title, Author, FolderPath, LastRead, IsFavorite, Tags, Characters, Description, IsArchive, ArchivePath, PageCount, Rating)
                VALUES (@title, @author, @folderPath, @lastRead, @isFavorite, @tags, @characters, @description, @isArchive, @archivePath, @pageCount, @rating)";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@title", manga.Title);
            command.Parameters.AddWithValue("@author", manga.Author);
            command.Parameters.AddWithValue("@folderPath", manga.FolderPath);
            command.Parameters.AddWithValue("@lastRead", (object)manga.LastRead ?? DBNull.Value);
            command.Parameters.AddWithValue("@isFavorite", manga.IsFavorite);
            command.Parameters.AddWithValue("@tags", manga.Tags ?? "");
            command.Parameters.AddWithValue("@characters", manga.Characters ?? "");
            command.Parameters.AddWithValue("@description", manga.Description ?? "");
            command.Parameters.AddWithValue("@isArchive", ArchiveHandler.IsArchiveFile(manga.FolderPath));
            command.Parameters.AddWithValue("@archivePath", ArchiveHandler.IsArchiveFile(manga.FolderPath) ? manga.FolderPath : null);
            command.Parameters.AddWithValue("@pageCount", manga.PageCount);
            command.Parameters.AddWithValue("@rating", manga.Rating);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<MangaInfo> GetMangaByPathAsync(string folderPath)
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM Mangas WHERE FolderPath = @folderPath";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@folderPath", folderPath);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new MangaInfo
                {
                    Title = reader["Title"].ToString(),
                    Author = reader["Author"] == DBNull.Value ? "Auteur inconnu" : reader["Author"].ToString(),
                    FolderPath = reader["FolderPath"].ToString(),
                    LastRead = reader["LastRead"] == DBNull.Value ? null : Convert.ToDateTime(reader["LastRead"]),
                    IsFavorite = Convert.ToBoolean(reader["IsFavorite"]),
                    Tags = reader["Tags"] == DBNull.Value ? "" : reader["Tags"].ToString(),
                    Characters = reader["Characters"] == DBNull.Value ? "" : reader["Characters"].ToString(),
                    Description = reader["Description"] == DBNull.Value ? "" : reader["Description"].ToString(),
                    PageCount = reader["PageCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PageCount"]),
                    Rating = reader["Rating"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Rating"])
                };
            }

            return null;
        }

        public async Task<List<MangaInfo>> GetAllMangasAsync()
        {
            var mangas = new List<MangaInfo>();

            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM Mangas ORDER BY Title";
            using var command = new SQLiteCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                mangas.Add(new MangaInfo
                {
                    Title = reader["Title"].ToString(),
                    Author = reader["Author"] == DBNull.Value ? "Auteur inconnu" : reader["Author"].ToString(),
                    FolderPath = reader["FolderPath"].ToString(),
                    LastRead = reader["LastRead"] == DBNull.Value ? null : Convert.ToDateTime(reader["LastRead"]),
                    IsFavorite = Convert.ToBoolean(reader["IsFavorite"]),
                    Tags = reader["Tags"] == DBNull.Value ? "" : reader["Tags"].ToString(),
                    Characters = reader["Characters"] == DBNull.Value ? "" : reader["Characters"].ToString(),
                    Description = reader["Description"] == DBNull.Value ? "" : reader["Description"].ToString(),
                    PageCount = reader["PageCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PageCount"]),
                    Rating = reader["Rating"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Rating"])
                });
            }

            return mangas;
        }

        public async Task<List<MangaInfo>> GetRecentMangasAsync(int count = 10)
        {
            var mangas = new List<MangaInfo>();

            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT * FROM Mangas 
                WHERE LastRead IS NOT NULL 
                ORDER BY LastRead DESC 
                LIMIT @count";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@count", count);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                mangas.Add(new MangaInfo
                {
                    Title = reader["Title"].ToString(),
                    Author = reader["Author"] == DBNull.Value ? "Auteur inconnu" : reader["Author"].ToString(),
                    FolderPath = reader["FolderPath"].ToString(),
                    LastRead = Convert.ToDateTime(reader["LastRead"]),
                    IsFavorite = Convert.ToBoolean(reader["IsFavorite"]),
                    Tags = reader["Tags"] == DBNull.Value ? "" : reader["Tags"].ToString(),
                    Characters = reader["Characters"] == DBNull.Value ? "" : reader["Characters"].ToString(),
                    Description = reader["Description"] == DBNull.Value ? "" : reader["Description"].ToString(),
                    PageCount = reader["PageCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PageCount"]),
                    Rating = reader["Rating"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Rating"])
                });
            }

            return mangas;
        }

        public async Task<List<MangaInfo>> SearchMangasAsync(string searchTerm, string tagFilter = null, bool favoritesOnly = false)
        {
            var mangas = new List<MangaInfo>();

            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = @"SELECT * FROM Mangas WHERE 1=1";
            var parameters = new List<SQLiteParameter>();

            // Recherche par terme
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query += " AND (Title LIKE @searchTerm OR Author LIKE @searchTerm OR Characters LIKE @searchTerm OR Description LIKE @searchTerm)";
                parameters.Add(new SQLiteParameter("@searchTerm", $"%{searchTerm}%"));
            }

            // Filtre par tag
            if (!string.IsNullOrWhiteSpace(tagFilter))
            {
                query += " AND Tags LIKE @tagFilter";
                parameters.Add(new SQLiteParameter("@tagFilter", $"%{tagFilter}%"));
            }

            // Filtre favoris
            if (favoritesOnly)
            {
                query += " AND IsFavorite = 1";
            }

            query += " ORDER BY Title";

            using var command = new SQLiteCommand(query, connection);
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                mangas.Add(new MangaInfo
                {
                    Title = reader["Title"].ToString(),
                    Author = reader["Author"] == DBNull.Value ? "Auteur inconnu" : reader["Author"].ToString(),
                    FolderPath = reader["FolderPath"].ToString(),
                    LastRead = reader["LastRead"] == DBNull.Value ? null : Convert.ToDateTime(reader["LastRead"]),
                    IsFavorite = Convert.ToBoolean(reader["IsFavorite"]),
                    Tags = reader["Tags"] == DBNull.Value ? "" : reader["Tags"].ToString(),
                    Characters = reader["Characters"] == DBNull.Value ? "" : reader["Characters"].ToString(),
                    Description = reader["Description"] == DBNull.Value ? "" : reader["Description"].ToString(),
                    PageCount = reader["PageCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PageCount"]),
                    Rating = reader["Rating"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Rating"])
                });
            }

            return mangas;
        }

        public async Task UpdateLastReadAsync(string folderPath, DateTime lastRead)
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE Mangas 
                SET LastRead = @lastRead, UpdatedDate = CURRENT_TIMESTAMP
                WHERE FolderPath = @folderPath";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@lastRead", lastRead);
            command.Parameters.AddWithValue("@folderPath", folderPath);

            await command.ExecuteNonQueryAsync();

            // Ajouter à l'historique
            await AddToHistoryAsync(folderPath, 0);
        }

        public async Task UpdateMangaMetadataAsync(MangaInfo manga)
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE Mangas 
                SET Title = @title, Author = @author, Tags = @tags, Characters = @characters, 
                    Description = @description, Rating = @rating, UpdatedDate = CURRENT_TIMESTAMP
                WHERE FolderPath = @folderPath";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@title", manga.Title);
            command.Parameters.AddWithValue("@author", manga.Author ?? "");
            command.Parameters.AddWithValue("@tags", manga.Tags ?? "");
            command.Parameters.AddWithValue("@characters", manga.Characters ?? "");
            command.Parameters.AddWithValue("@description", manga.Description ?? "");
            command.Parameters.AddWithValue("@rating", manga.Rating);
            command.Parameters.AddWithValue("@folderPath", manga.FolderPath);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ToggleFavoriteAsync(string folderPath)
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE Mangas 
                SET IsFavorite = NOT IsFavorite, UpdatedDate = CURRENT_TIMESTAMP
                WHERE FolderPath = @folderPath";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@folderPath", folderPath);

            await command.ExecuteNonQueryAsync();
        }

        // Gestion des positions de lecture
        public async Task SaveReadingPositionAsync(string mangaPath, int pageIndex, double zoomLevel = 1.0, double scrollPosition = 0)
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT OR REPLACE INTO ReadingPositions (MangaPath, PageIndex, ZoomLevel, ScrollPosition, LastUpdated)
                VALUES (@mangaPath, @pageIndex, @zoomLevel, @scrollPosition, CURRENT_TIMESTAMP)";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@mangaPath", mangaPath);
            command.Parameters.AddWithValue("@pageIndex", pageIndex);
            command.Parameters.AddWithValue("@zoomLevel", zoomLevel);
            command.Parameters.AddWithValue("@scrollPosition", scrollPosition);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<Tuple<int, double>> GetReadingPositionAsync(string mangaPath)
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = "SELECT PageIndex, ZoomLevel FROM ReadingPositions WHERE MangaPath = @mangaPath";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@mangaPath", mangaPath);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var pageIndex = Convert.ToInt32(reader["PageIndex"]);
                var zoomLevel = Convert.ToDouble(reader["ZoomLevel"]);
                return new Tuple<int, double>(pageIndex, zoomLevel);
            }

            return null;
        }

        // Historique de lecture
        private async Task AddToHistoryAsync(string mangaPath, int pageIndex)
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT INTO ReadingHistory (MangaPath, PageIndex)
                VALUES (@mangaPath, @pageIndex)";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@mangaPath", mangaPath);
            command.Parameters.AddWithValue("@pageIndex", pageIndex);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<string>> GetAllTagsAsync()
        {
            var allTags = new HashSet<string>();

            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = "SELECT Tags FROM Mangas WHERE Tags IS NOT NULL AND Tags != ''";
            using var command = new SQLiteCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var tags = reader["Tags"].ToString();
                if (!string.IsNullOrWhiteSpace(tags))
                {
                    var tagList = tags.Split(',').Select(t => t.Trim());
                    foreach (var tag in tagList)
                    {
                        allTags.Add(tag);
                    }
                }
            }

            return allTags.OrderBy(t => t).ToList();
        }

        public async Task UpdateRatingAsync(string folderPath, int rating)
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE Mangas 
                SET Rating = @rating, UpdatedDate = CURRENT_TIMESTAMP
                WHERE FolderPath = @folderPath";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@rating", rating);
            command.Parameters.AddWithValue("@folderPath", folderPath);

            await command.ExecuteNonQueryAsync();
        }
    }
}