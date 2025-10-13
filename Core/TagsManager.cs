using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MangaReader
{
    public class TagsManager
    {
        private static TagsManager _instance;
        private static readonly object _lock = new object();
        private string _tagsFilePath;
        private List<string> _availableTags;

        private TagsManager()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var mangaReaderPath = Path.Combine(appDataPath, "MangaReader");

            if (!Directory.Exists(mangaReaderPath))
            {
                Directory.CreateDirectory(mangaReaderPath);
            }

            _tagsFilePath = Path.Combine(mangaReaderPath, "tags.json");
            _availableTags = new List<string>();
            LoadTags();
        }

        public static TagsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new TagsManager();
                        }
                    }
                }
                return _instance;
            }
        }

        public List<string> AvailableTags => new List<string>(_availableTags);

        private void LoadTags()
        {
            try
            {
                if (File.Exists(_tagsFilePath))
                {
                    var json = File.ReadAllText(_tagsFilePath);
                    _availableTags = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
                else
                {
                    // Tags par défaut
                    _availableTags = new List<string>
                    {
                        "Shonen", "Seinen", "Shojo", "Josei",
                        "Action", "Aventure", "Comédie", "Drame", "Fantasy", "Horreur",
                        "Mystère", "Romance", "Sci-Fi", "Slice of Life", "Sport",
                        "Historique", "Mecha", "Psychologique", "Thriller",
                        "École", "Surnaturel", "Martial Arts", "Super Pouvoir"
                    };
                    SaveTags();
                }
            }
            catch
            {
                _availableTags = new List<string>();
            }
        }

        public async Task SaveTagsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_availableTags, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_tagsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur sauvegarde tags: {ex.Message}");
            }
        }

        private void SaveTags()
        {
            SaveTagsAsync().Wait();
        }

        public void AddTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag) && !_availableTags.Contains(tag))
            {
                _availableTags.Add(tag);
                _availableTags.Sort();
            }
        }

        public void RemoveTag(string tag)
        {
            _availableTags.Remove(tag);
        }

        public void UpdateTag(string oldTag, string newTag)
        {
            var index = _availableTags.IndexOf(oldTag);
            if (index >= 0 && !string.IsNullOrWhiteSpace(newTag))
            {
                _availableTags[index] = newTag;
                _availableTags.Sort();
            }
        }
    }
}