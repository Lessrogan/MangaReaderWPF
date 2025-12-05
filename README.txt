Manga Reader WPF v2.0
Application moderne de lecture de mangas pour Windows avec interface dark theme, gestion de bibliothèque et support des archives CBZ/CBR.
📋 Prérequis

Windows 10/11
.NET 9.0 Runtime
Visual Studio 2022 (optionnel, pour le développement)

🚀 Installation
Option 1 : Depuis les sources
bashgit clone [votre-repo]
cd MangaReaderWPF
dotnet restore
dotnet build -c Release
dotnet run
Option 2 : Exécutable compilé

Téléchargez la dernière version depuis les Releases
Extrayez l'archive
Lancez MangaReader.exe

🎯 Fonctionnalités principales

📚 Gestion de bibliothèque : Organisation automatique de vos mangas
🔍 Recherche et filtres : Recherche par titre, auteur, tags
📖 Modes de lecture : Simple page, double page, défilement continu
📦 Support CBZ/CBR : Lecture directe des archives
⭐ Système de favoris : Marquez vos mangas préférés
🕒 Historique de lecture : Suivi automatique des derniers mangas lus
💾 Sauvegarde position : Reprise de lecture où vous vous êtes arrêté
🎨 Interface personnalisable : Thèmes et paramètres d'affichage
📥 Export/Import : Sauvegarde des métadonnées en JSON/XML

🏁 Premier démarrage

Configuration initiale

Au premier lancement, l'application créera un dossier Mangas dans vos Documents
Vous pouvez changer ce dossier via Paramètres → Général


Structure des dossiers

   Mangas/
   ├── One Piece/
   │   ├── Chapitre 1/
   │   │   ├── 001.jpg
   │   │   ├── 002.jpg
   │   │   └── ...
   │   └── Chapitre 2/
   ├── Naruto/
   ├── MonManga.cbz
   └── AutreManga.cbr

Formats supportés

Images : JPG, PNG, GIF, BMP, WebP
Archives : CBZ, CBR, ZIP, RAR



💻 Utilisation
Navigation principale

Recherche : Tapez dans la barre de recherche pour filtrer instantanément
Filtres : Utilisez les tags et le filtre favoris
Tri : Par nom, date de lecture, note ou auteur

Lecture d'un manga

Cliquez sur une couverture pour ouvrir les détails
Cliquez sur "Lire" pour commencer la lecture
Utilisez les flèches ou les boutons pour naviguer

Raccourcis clavier

← / → : Page précédente/suivante
Home / End : Première/dernière page
F11 : Plein écran
Escape : Quitter le mode plein écran
Ctrl + Molette : Zoom

⚙️ Configuration
Dossier des mangas

Allez dans Paramètres (bouton ⚙️)
Onglet Général
Cliquez sur "Parcourir" pour choisir votre dossier

Performance

Cache images : Ajustez la taille du cache (10-500 MB)
Lazy loading : Active le chargement différé
Préchargement : Configure le nombre de pages à précharger

Import/Export

Exportez vos métadonnées pour les sauvegarder
Importez-les sur un autre PC ou après réinstallation

🔧 Résolution de problèmes
Les mangas ne s'affichent pas

Vérifiez le chemin du dossier dans les paramètres
Assurez-vous que le dossier contient des sous-dossiers avec des images
Cliquez sur "Actualiser" pour recharger

Erreur de base de données

Fermez l'application
Supprimez %LOCALAPPDATA%\MangaReader\mangas.db
Relancez l'application

Performance lente

Réduisez la taille du cache si vous avez peu de RAM
Activez le lazy loading dans les paramètres
Désactivez le préchargement des pages

Antivirus détecte un faux positif

Compilez en Release : dotnet build -c Release
Ajoutez une exception pour le dossier du projet

📁 Structure du projet
MangaReaderWPF/
├── Core/
│   ├── MangaInfo.cs          # Classes de données
│   ├── AppSettings.cs        # Configuration
│   ├── ArchiveHandler.cs     # Support CBZ/CBR
│   ├── ImageCacheManager.cs  # Gestion du cache
│   └── MetadataExporter.cs   # Export/Import
├── MainWindow.xaml/cs         # Fenêtre principale
├── MangaDetailsWindow.*       # Détails du manga
├── MangaReaderWindow.*        # Lecteur
├── SettingsWindow.*           # Paramètres
└── MangaDatabase.cs          # Base de données SQLite
🛠️ Compilation depuis les sources
Prérequis développement

Visual Studio 2022 avec charge de travail .NET Desktop
.NET 9.0 SDK

Build
bash# Cloner le repo
git clone [votre-repo]
cd MangaReaderWPF

# Restaurer les packages
dotnet restore

# Compiler
dotnet build -c Release

# Publier
dotnet publish -c Release -r win-x64 --self-contained false
📝 Données et stockage

Base de données : %LOCALAPPDATA%\MangaReader\mangas.db
Configuration : %LOCALAPPDATA%\MangaReader\settings.json
Cache temporaire : %TEMP%\MangaReader\

🤝 Contribution
Les contributions sont bienvenues !

Fork le projet
Créez votre branche (git checkout -b feature/AmazingFeature)
Committez vos changements (git commit -m 'Add AmazingFeature')
Push vers la branche (git push origin feature/AmazingFeature)
Ouvrez une Pull Request

📄 Licence
Ce projet est sous licence MIT. Voir le fichier LICENSE pour plus de détails.
🙏 Remerciements

WPF Community
SQLite
SharpCompress pour le support des archives

📞 Support
Pour toute question ou problème :

Ouvrez une issue sur GitHub
Consultez la section Résolution de problèmes

//TODO

- Localization des trads
- Actualisation automatique lors d'un changement de répertoire // garder le bouton actualiser si on rajoute des fichiers dans le repertoire
- Paramètres 
	-> sauvegarder le thèmes choisit
	-> nombre de prévisualisation (doit afficher toutes les images) 
	-> TODO vérif import et export
- Check quoi faire avec le compteur quand defilement
- Todo check filtre ET tri
- Faire ne sorte de ne plus avoir besoin de tags.json (charger en base au 1er demarrage des tags (voir ne meme pas faire ça et se contenter destags que l'on peut mettre en paramétrage revert commit 9f6d5e0)) ->en a gardé, faut juste changer les tags de base
- Fair en sorte de ne plus avoir besoin de save les modifs d'un mangas
- trier par note ? aucun moyen de mettre de snotes, par auteur, on rentre ou le nom de l'auteur ? par date fonctionne pas
-Ajouter un systeme de lien entre mangas pour qui se suivent lors de la lecture? comment faire et où de manière UI/UX
Version : 2.0.0
Dernière mise à jour : Décembre 2024