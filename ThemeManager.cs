using System;
using System.Windows;
using System.Windows.Media;

namespace MangaReader
{
    public static class ThemeManager
    {
        public static void ApplyTheme(string themeName)
        {
            var app = Application.Current;
            var dictionaries = app.Resources.MergedDictionaries;
            dictionaries.Clear();

            var themeDict = new ResourceDictionary();

            switch (themeName)
            {
                case "Light":
                    ApplyLightTheme(themeDict);
                    break;
                case "BlueNight":
                    ApplyBlueNightTheme(themeDict);
                    break;
                case "Dark":
                default:
                    ApplyDarkTheme(themeDict);
                    break;
            }

            dictionaries.Add(themeDict);
        }

        private static void ApplyDarkTheme(ResourceDictionary dict)
        {
            // Couleurs de base
            dict["BackgroundColor"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            dict["SecondaryBackgroundColor"] = new SolidColorBrush(Color.FromRgb(37, 37, 38));
            dict["TertiaryBackgroundColor"] = new SolidColorBrush(Color.FromRgb(45, 45, 48));
            dict["BorderColor"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));

            // Textes
            dict["TextPrimaryColor"] = new SolidColorBrush(Colors.White);
            dict["TextSecondaryColor"] = new SolidColorBrush(Color.FromRgb(204, 204, 204));
            dict["TextDisabledColor"] = new SolidColorBrush(Color.FromRgb(136, 136, 136));


            // Accent et interactions
            dict["AccentColor"] = new SolidColorBrush(Color.FromRgb(0, 122, 204));
            dict["HoverColor"] = new SolidColorBrush(Color.FromRgb(61, 61, 64));
            dict["SelectedColor"] = new SolidColorBrush(Color.FromRgb(0, 90, 158));

            // ComboBox spécifique
            dict["ComboBoxBackgroundColor"] = new SolidColorBrush(Color.FromRgb(45, 45, 48));
            dict["ComboBoxForegroundColor"] = new SolidColorBrush(Colors.Black);
            dict["ComboBoxDropDownColor"] = new SolidColorBrush(Colors.White);
        }

        private static void ApplyLightTheme(ResourceDictionary dict)
        {
            // Couleurs de base
            dict["BackgroundColor"] = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            dict["SecondaryBackgroundColor"] = new SolidColorBrush(Colors.White);
            dict["TertiaryBackgroundColor"] = new SolidColorBrush(Color.FromRgb(250, 250, 250));
            dict["BorderColor"] = new SolidColorBrush(Color.FromRgb(200, 200, 200));

            // Textes
            dict["TextPrimaryColor"] = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            dict["TextSecondaryColor"] = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            dict["TextDisabledColor"] = new SolidColorBrush(Color.FromRgb(160, 160, 160));

            // Accent et interactions
            dict["AccentColor"] = new SolidColorBrush(Color.FromRgb(0, 102, 204));
            dict["HoverColor"] = new SolidColorBrush(Color.FromRgb(230, 230, 230));
            dict["SelectedColor"] = new SolidColorBrush(Color.FromRgb(0, 122, 204));

            // ComboBox spécifique
            dict["ComboBoxBackgroundColor"] = new SolidColorBrush(Colors.White);
            dict["ComboBoxForegroundColor"] = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            dict["ComboBoxDropDownColor"] = new SolidColorBrush(Colors.White);
        }

        private static void ApplyBlueNightTheme(ResourceDictionary dict)
        {
            // Couleurs de base
            dict["BackgroundColor"] = new SolidColorBrush(Color.FromRgb(15, 25, 35));
            dict["SecondaryBackgroundColor"] = new SolidColorBrush(Color.FromRgb(20, 35, 50));
            dict["TertiaryBackgroundColor"] = new SolidColorBrush(Color.FromRgb(25, 45, 65));
            dict["BorderColor"] = new SolidColorBrush(Color.FromRgb(45, 65, 85));

            // Textes
            dict["TextPrimaryColor"] = new SolidColorBrush(Color.FromRgb(220, 230, 240));
            dict["TextSecondaryColor"] = new SolidColorBrush(Color.FromRgb(160, 180, 200));
            dict["TextDisabledColor"] = new SolidColorBrush(Color.FromRgb(100, 120, 140));

            // Accent et interactions
            dict["AccentColor"] = new SolidColorBrush(Color.FromRgb(64, 158, 255));
            dict["HoverColor"] = new SolidColorBrush(Color.FromRgb(35, 55, 75));
            dict["SelectedColor"] = new SolidColorBrush(Color.FromRgb(48, 120, 195));

            // ComboBox spécifique
            dict["ComboBoxBackgroundColor"] = new SolidColorBrush(Color.FromRgb(25, 45, 65));
            dict["ComboBoxForegroundColor"] = new SolidColorBrush(Color.FromRgb(220, 230, 240));
            dict["ComboBoxDropDownColor"] = new SolidColorBrush(Color.FromRgb(20, 35, 50));
        }
    }
}