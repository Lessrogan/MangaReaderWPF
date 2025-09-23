using System;
using System.Windows.Media.Imaging;

namespace MangaReader
{
    /// <summary>
    /// Classe de base pour les informations d'un manga
    /// </summary>
    public class MangaInfo
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string FolderPath { get; set; }
        public BitmapImage CoverImage { get; set; }
        public DateTime? LastRead { get; set; }
        public bool IsFavorite { get; set; }
        public string Tags { get; set; }
        public string Characters { get; set; }
        public string Description { get; set; }
        public int PageCount { get; set; }
        public int Rating { get; set; }

        public MangaInfo()
        {
            Title = "";
            Author = "Auteur inconnu";
            FolderPath = "";
            Tags = "";
            Characters = "";
            Description = "";
            PageCount = 0;
            Rating = 0;
            IsFavorite = false;
        }

        /// <summary>
        /// Formatage de la dernière lecture pour l'affichage
        /// </summary>
        public string LastReadFormatted
        {
            get
            {
                if (LastRead.HasValue)
                {
                    var days = (DateTime.Now - LastRead.Value).Days;
                    if (days == 0) return "Aujourd'hui";
                    if (days == 1) return "Hier";
                    if (days < 7) return $"Il y a {days} jours";
                    if (days < 30) return $"Il y a {days / 7} semaines";
                    if (days < 365) return $"Il y a {days / 30} mois";
                    return $"Il y a {days / 365} ans";
                }
                return "Jamais lu";
            }
        }

        /// <summary>
        /// Copie les propriétés d'un autre MangaInfo
        /// </summary>
        public void CopyFrom(MangaInfo other)
        {
            if (other == null) return;

            Title = other.Title;
            Author = other.Author;
            FolderPath = other.FolderPath;
            CoverImage = other.CoverImage;
            LastRead = other.LastRead;
            IsFavorite = other.IsFavorite;
            Tags = other.Tags;
            Characters = other.Characters;
            Description = other.Description;
            PageCount = other.PageCount;
            Rating = other.Rating;
        }
    }

    /// <summary>
    /// Classe pour les miniatures de pages
    /// </summary>
    public class PageThumbnail
    {
        public string PageNumber { get; set; }
        public int PageIndex { get; set; }
        public BitmapImage Image { get; set; }
    }

    /// <summary>
    /// Classe pour les miniatures avec taille dynamique (vue toutes pages)
    /// </summary>
    public class PageThumbnailFull : PageThumbnail
    {
        public double ThumbnailSize { get; set; } = 150;
        public double ThumbnailHeight { get; set; } = 210;
    }
}