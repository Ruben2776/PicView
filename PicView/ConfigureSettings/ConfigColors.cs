using PicView.Library.Resources;
using PicView.UILogic.Animations;
using PicView.UILogic.Loading;
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
                Application.Current.Resources["WindowBorderColorBrush"] = Application.Current.Resources["WindowBackgroundColorBrush"];
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

            switch (Properties.Settings.Default.BgColorChoice)
            {
                case 0:
                    LoadWindows.GetMainWindow.MainImageBorder.Background = Brushes.Transparent;
                    break;

                case 1:
                    var brush = Properties.Settings.Default.DarkTheme ? Brushes.Black : Brushes.White;
                    LoadWindows.GetMainWindow.MainImageBorder.Background = brush;
                    break;

                case 2:
                    LoadWindows.GetMainWindow.MainImageBorder.Background = DrawingBrushes.CheckerboardDrawingBrush(Colors.White);
                    break;
            }

            backgroundBorderColor = (Color)Application.Current.Resources["BackgroundColorAlt"];
        }

        #endregion Update and set colors

        #region Change background

        internal static void ChangeBackground(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImageBorder == null)
            {
                return;
            }

            Properties.Settings.Default.BgColorChoice++;

            if (Properties.Settings.Default.BgColorChoice > 3)
            {
                Properties.Settings.Default.BgColorChoice = 0;
            }

            switch (Properties.Settings.Default.BgColorChoice)
            {
                case 0:
                    var x = new SolidColorBrush(Colors.Transparent);
                    if (LoadWindows.GetMainWindow.MainImageBorder.Background == x)
                    {
                        goto case 1;
                    }
                    LoadWindows.GetMainWindow.MainImageBorder.Background = x;
                    break;

                case 1:
                    var brush = Properties.Settings.Default.DarkTheme ? Brushes.Black : Brushes.White;
                    LoadWindows.GetMainWindow.MainImageBorder.Background = brush;
                    break;

                case 2:
                    var checkerboardBg = DrawingBrushes.CheckerboardDrawingBrush(Colors.White);
                    if (checkerboardBg != null)
                    {
                        LoadWindows.GetMainWindow.MainImageBorder.Background = checkerboardBg;
                    }
                    break;

                case 3:
                    var checkerboardBg2 = DrawingBrushes.CheckerboardDrawingBrush(Color.FromRgb(76, 76, 76));
                    if (checkerboardBg2 != null)
                    {
                        LoadWindows.GetMainWindow.MainImageBorder.Background = checkerboardBg2;
                    }
                    break;

                default:
                    LoadWindows.GetMainWindow.MainImageBorder.Background = Brushes.Transparent;
                    break;
            }
        }

        internal static Brush GetBackgroundColorBrush()
        {
            return Properties.Settings.Default.BgColorChoice switch
            {
                0 => Brushes.Transparent,
                1 => Properties.Settings.Default.DarkTheme ? Brushes.Black : Brushes.White,
                2 => DrawingBrushes.CheckerboardDrawingBrush(Colors.White),
                3 => DrawingBrushes.CheckerboardDrawingBrush(Color.FromRgb(76, 76, 76), Color.FromRgb(32, 32, 32), 55),
                _ => Brushes.Transparent,
            };
        }

        #endregion Change background

        #region Change Theme

        internal static void ChangeToLightTheme()
        {
            Application.Current.Resources.MergedDictionaries[1] = new ResourceDictionary
            {
                Source = new Uri(@"/PicView;component/Themes/Styles/ColorThemes/Light.xaml", UriKind.Relative)
            };

            Properties.Settings.Default.DarkTheme = true;
        }

        internal static void ChangeToDarkTheme()
        {
            Application.Current.Resources.MergedDictionaries[1] = new ResourceDictionary
            {
                Source = new Uri(@"/PicView;component/Themes/Styles/ColorThemes/Dark.xaml", UriKind.Relative)
            };

            Properties.Settings.Default.DarkTheme = false;
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