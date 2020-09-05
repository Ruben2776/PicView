using PicView.Library.Resources;
using PicView.UILogic;
using PicView.UILogic.Animations;
using System;
using System.Diagnostics;
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
        internal static Color backgroundBorderColor;

        /// <summary>
        /// Helper for user color settings
        /// </summary>
        internal static Color mainColor;

        /// <summary>
        /// Update color values for brushes and window border
        /// </summary>
        /// <param name="remove">Remove border?</param>
        internal static void UpdateColor(bool remove = false)
        {
            if (remove)
            {
                Application.Current.Resources["WindowBorderColorBrush"] = new SolidColorBrush((Color)Application.Current.Resources["WindowBorderColor"]);
                return;
            }

            var getColor = AnimationHelper.GetPrefferedColorOver();
            var getColorBrush = new SolidColorBrush(getColor);

            Application.Current.Resources["ChosenColor"] = getColor;
            Application.Current.Resources["ChosenColorBrush"] = getColorBrush;

            if (Properties.Settings.Default.WindowBorderColorEnabled)
            {
                try
                {
                    Application.Current.Resources["WindowBorderColorBrush"] = getColorBrush;
                }
                catch (Exception e)
                {
#if DEBUG
                    Trace.WriteLine(nameof(UpdateColor) + " threw exception:  " + e.Message);
#endif
                }
            }
        }

        internal static void SetColors()
        {
            mainColor = (Color)Application.Current.Resources["IconColor"];
            backgroundBorderColor = (Color)Application.Current.Resources["BackgroundColorAlt"];

            if (ConfigureWindows.GetMainWindow.MainImageBorder == null)
            {
                return;
            }

            ConfigureWindows.GetMainWindow.MainImageBorder.Background = BackgroundColorBrush;
        }

        #endregion Update and set colors

        #region Change background

        internal static void ChangeBackground(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImageBorder == null)
            {
                return;
            }

            Properties.Settings.Default.BgColorChoice++;

            if (Properties.Settings.Default.BgColorChoice > 3)
            {
                Properties.Settings.Default.BgColorChoice = 0;
            }

            ConfigureWindows.GetMainWindow.MainImageBorder.Background = BackgroundColorBrush;
        }

        internal static Brush BackgroundColorBrush => Properties.Settings.Default.BgColorChoice switch
        {
            0 => Brushes.Transparent,
            1 => Properties.Settings.Default.DarkTheme ? Brushes.White : new SolidColorBrush(Color.FromRgb(25, 25, 25)),
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

            Properties.Settings.Default.DarkTheme = false;
        }

        internal static void ChangeToDarkTheme()
        {
            Application.Current.Resources.MergedDictionaries[1] = new ResourceDictionary
            {
                Source = new Uri(@"/PicView;component/Themes/Styles/ColorThemes/Dark.xaml", UriKind.Relative)
            };

            Properties.Settings.Default.DarkTheme = true;
        }

        #endregion Change Theme

        #region Set ColorTheme

        internal static void Blue(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 1;
            UpdateColor();
        }

        internal static void Pink(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 2;
            UpdateColor();
        }

        internal static void Orange(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 3;
            UpdateColor();
        }

        internal static void Green(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 4;
            UpdateColor();
        }

        internal static void Red(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 5;
            UpdateColor();
        }

        internal static void Teal(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 6;
            UpdateColor();
        }

        internal static void Aqua(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 7;
            UpdateColor();
        }

        internal static void Golden(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 8;
            UpdateColor();
        }

        internal static void Purple(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 9;
            UpdateColor();
        }

        internal static void Cyan(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 10;
            UpdateColor();
        }

        internal static void Magenta(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 11;
            UpdateColor();
        }

        internal static void Lime(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 12;
            UpdateColor();
        }

        #endregion Set ColorTheme
    }
}