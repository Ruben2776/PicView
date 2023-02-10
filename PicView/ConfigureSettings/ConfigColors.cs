using PicView.Animations;
using PicView.Properties;
using PicView.Themes.Resources;
using PicView.UILogic;
using System;
using System.Windows;
using System.Windows.Media;

namespace PicView.ConfigureSettings
{
    /// <summary>
    /// Used for color and theming related coding
    /// </summary>
    internal static class ConfigColors
    {
        #region Update and set colors

        /// <summary>
        /// Helper for user color settings
        /// </summary>
        internal static Color BackgroundBorderColor { get; set; }

        /// <summary>
        /// Helper for user color settings
        /// </summary>
        internal static Color MainColor { get; set; }

        /// <summary>
        /// Update color values for brushes and window border
        /// </summary>
        /// <param name="remove">Remove border?</param>
        internal static void UpdateColor()
        {
            var getColor = AnimationHelper.GetPrefferedColor();
            var getColorBrush = new SolidColorBrush(getColor);

            Application.Current.Resources["ChosenColor"] = getColor;
            Application.Current.Resources["ChosenColorBrush"] = getColorBrush;

            Settings.Default.Save();
        }

        internal static void SetColors()
        {
            MainColor = (Color)Application.Current.Resources["IconColor"];
            BackgroundBorderColor = (Color)Application.Current.Resources["BackgroundColorAlt"];

            if (ConfigureWindows.GetMainWindow.MainImageBorder == null)
            {
                return;
            }

            ConfigureWindows.GetMainWindow.MainImageBorder.Background = BackgroundColorBrush;
        }

        #endregion Update and set colors

        #region Window LostFocus style change

        internal static void MainWindowUnfocus()
        {
            var w = ConfigureWindows.GetMainWindow;
            var fadeColor1 = (SolidColorBrush)Application.Current.Resources["IconColorBrush"];
            var fadeColor2 = (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushAlt"];

            w.TitleText.InnerTextBox.Foreground = fadeColor1;
            w.TitleText.Background = fadeColor2;
            w.LowerBar.Background = fadeColor2;
        }

        internal static void MainWindowFocus()
        {
            var w = ConfigureWindows.GetMainWindow;
            var main1 = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
            var main2 = (SolidColorBrush)Application.Current.Resources["BorderBrushAlt"];

            w.TitleText.InnerTextBox.Foreground = main1;
            w.TitleText.Background = main2;
            w.LowerBar.Background = main2;
        }

        #endregion

        #region Change background

        internal static void ChangeBackground()
        {
            if (ConfigureWindows.GetMainWindow.MainImageBorder == null)
            {
                return;
            }

            Settings.Default.BgColorChoice++;

            if (Settings.Default.BgColorChoice > 3)
            {
                Settings.Default.BgColorChoice = 0;
            }

            ConfigureWindows.GetMainWindow.MainImageBorder.Background = BackgroundColorBrush;

            Settings.Default.Save();
        }

        internal static Brush BackgroundColorBrush => Settings.Default.BgColorChoice switch
        {
            0 => Brushes.Transparent,
            1 => Settings.Default.DarkTheme ? Brushes.White : new SolidColorBrush(Color.FromRgb(25, 25, 25)),
            2 => DrawingBrushes.CheckerboardDrawingBrush(Colors.White),
            3 => DrawingBrushes.CheckerboardDrawingBrush(Color.FromRgb(76, 76, 76), Color.FromRgb(32, 32, 32), 56),
            _ => Brushes.Transparent,
        };

        #endregion Change background

        #region Change Theme

        internal static void ChangeToLightTheme()
        {
            Application.Current.Resources.MergedDictionaries[1] = new ResourceDictionary
            {
                Source = new Uri(@"/PicView;component/Themes/Styles/ColorThemes/Light.xaml", UriKind.Relative)
            };

            Settings.Default.DarkTheme = false;
        }

        internal static void ChangeToDarkTheme()
        {
            Application.Current.Resources.MergedDictionaries[1] = new ResourceDictionary
            {
                Source = new Uri(@"/PicView;component/Themes/Styles/ColorThemes/Dark.xaml", UriKind.Relative)
            };

            Settings.Default.DarkTheme = true;
        }

        #endregion Change Theme

        #region Set ColorTheme

        internal static void Blue(object sender, RoutedEventArgs e)
        {
            Settings.Default.ColorTheme = 1;
            UpdateColor();
        }

        internal static void Pink(object sender, RoutedEventArgs e)
        {
            Settings.Default.ColorTheme = 2;
            UpdateColor();
        }

        internal static void Orange(object sender, RoutedEventArgs e)
        {
            Settings.Default.ColorTheme = 3;
            UpdateColor();
        }

        internal static void Green(object sender, RoutedEventArgs e)
        {
            Settings.Default.ColorTheme = 4;
            UpdateColor();
        }

        internal static void Red(object sender, RoutedEventArgs e)
        {
            Settings.Default.ColorTheme = 5;
            UpdateColor();
        }

        internal static void Teal(object sender, RoutedEventArgs e)
        {
            Settings.Default.ColorTheme = 6;
            UpdateColor();
        }

        internal static void Aqua(object sender, RoutedEventArgs e)
        {
            Settings.Default.ColorTheme = 7;
            UpdateColor();
        }

        internal static void Golden(object sender, RoutedEventArgs e)
        {
            Settings.Default.ColorTheme = 8;
            UpdateColor();
        }

        internal static void Purple(object sender, RoutedEventArgs e)
        {
            Settings.Default.ColorTheme = 9;
            UpdateColor();
        }

        internal static void Cyan(object sender, RoutedEventArgs e)
        {
            Settings.Default.ColorTheme = 10;
            UpdateColor();
        }

        internal static void Magenta(object sender, RoutedEventArgs e)
        {
            Settings.Default.ColorTheme = 11;
            UpdateColor();
        }

        internal static void Lime(object sender, RoutedEventArgs e)
        {
            Settings.Default.ColorTheme = 12;
            UpdateColor();
        }

        #endregion Set ColorTheme
    }
}