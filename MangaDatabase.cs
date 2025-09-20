using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

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
                    UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            using var command = new SQLiteCommand(createTableQuery, connection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task AddMangaAsync(MangaInfo manga)
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT OR REPLACE INTO Mangas (Title, Author, FolderPath, LastRead, IsFavorite, Tags, Characters, Description)
                VALUES (@title, @author, @folderPath, @lastRead, @isFavorite, @tags, @characters, @description)";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@title", manga.Title);
            command.Parameters.AddWithValue("@author", manga.Author);
            command.Parameters.AddWithValue("@folderPath", manga.FolderPath);
            command.Parameters.AddWithValue("@lastRead", (object)manga.LastRead ?? DBNull.Value);
            command.Parameters.AddWithValue("@isFavorite", manga.IsFavorite);
            command.Parameters.AddWithValue("@tags", manga.Tags ?? "");
            command.Parameters.AddWithValue("@characters", manga.Characters ?? "");
            command.Parameters.AddWithValue("@description", manga.Description ?? "");

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
                    Description = reader["Description"] == DBNull.Value ? "" : reader["Description"].ToString()
                };
            }

            return null;
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
                    Description = reader["Description"] == DBNull.Value ? "" : reader["Description"].ToString()
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
        }

        public async Task UpdateMangaMetadataAsync(MangaInfo manga)
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE Mangas 
                SET Title = @title, Author = @author, Tags = @tags, Characters = @characters, 
                    Description = @description, UpdatedDate = CURRENT_TIMESTAMP
                WHERE FolderPath = @folderPath";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@title", manga.Title);
            command.Parameters.AddWithValue("@author", manga.Author ?? "");
            command.Parameters.AddWithValue("@tags", manga.Tags ?? "");
            command.Parameters.AddWithValue("@characters", manga.Characters ?? "");
            command.Parameters.AddWithValue("@description", manga.Description ?? "");
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
    }
}