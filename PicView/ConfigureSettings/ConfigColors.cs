using PicView.Animations;
using PicView.Properties;
using PicView.Themes.Resources;
using PicView.UILogic;
using PicView.Views.Windows;
using System.Windows;
using System.Windows.Media;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

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


        internal static void UpdateColor()
        {
            var getColor = AnimationHelper.GetPrefferedColor();
            var getColorBrush = new SolidColorBrush(getColor);

            Application.Current.Resources["ChosenColor"] = getColor;
            Application.Current.Resources["ChosenColorBrush"] = getColorBrush;

            ConfigureWindows.GetMainWindow.Logo.ChangeColor();
            ConfigureWindows.GetSettingsWindow?.Logo.ChangeColor();
            ConfigureWindows.GetInfoWindow?.ChangeColor();

            Settings.Default.Save();
        }

        /// <summary>
        /// Apply color varaibles from themes
        /// </summary>
        internal static void SetColors()
        {
            var mainWindow = ConfigureWindows.GetMainWindow;
            MainColor = (Color)Application.Current.Resources["MainColor"];
            BackgroundBorderColor = (Color)Application.Current.Resources["BackgroundColorAlt"];

            if (mainWindow.MainImageBorder == null) return;

            mainWindow.MainImageBorder.Background = BackgroundColorBrush;
        }

        #endregion Update and set colors

        #region Window LostFocus style change

        internal static void MainWindowUnfocusOrFocus(bool isFocused)
        {
            var w = ConfigureWindows.GetMainWindow;

            var foregroundColor = isFocused ? (SolidColorBrush)Application.Current.Resources["MainColorBrush"]
                : (SolidColorBrush)Application.Current.Resources["IconColorBrush"];

            if (Properties.Settings.Default.DarkTheme)
            {
                w.TitleText.InnerTextBox.Foreground = foregroundColor;

                w.LeftButtonContainer.Background =
                w.Logo.Background =
                w.CloseButton.Background =
                w.MinButton.Background =
                w.FullscreenButton.Background =
                    isFocused ? (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushAlt"]
                                                : (SolidColorBrush)Application.Current.Resources["BackgroundColorBrush"];

                w.TitleBar.Background =
                    isFocused ? (SolidColorBrush)Application.Current.Resources["SubtleFadeBrush"]
                                                : (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushFadeSubtle"];
                w.LowerBar.Background = isFocused ? (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushAlt"]
                                                : (SolidColorBrush)Application.Current.Resources["BackgroundColorBrush"];
            }
            else
            {
                w.TitleText.InnerTextBox.Foreground = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];

                w.LeftButtonContainer.Background =
                w.Logo.Background =
                w.CloseButton.Background =
                w.MinButton.Background =
                w.FullscreenButton.Background =
                w.GalleryButton.Background =
                w.RotateButton.Background =
                w.FlipButton.Background =
                    isFocused ? (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushAlt"]
                                : (SolidColorBrush)Application.Current.Resources["BackgroundColorBrush"];

                w.TitleBar.Background = isFocused ? (SolidColorBrush)Application.Current.Resources["BackgroundSubtleHighlightBrush"]
                                                : (SolidColorBrush)Application.Current.Resources["BackgroundColorBrush"];
                w.LowerBar.Background = isFocused ? (SolidColorBrush)Application.Current.Resources["BackgroundSubtleHighlightBrush"]
                                                : (SolidColorBrush)Application.Current.Resources["BackgroundColorBrush"];
            }
        }

        #endregion Window LostFocus style change

        #region Change background

        /// <summary>
        /// changes the background color of the main window.
        /// It increments the BgColorChoice setting, which is used to determine the background color,
        /// and sets the background color of the main window to the BackgroundColorBrush brush.
        /// </summary>
        internal static void ChangeBackground()
        {
            var mainWindow = ConfigureWindows.GetMainWindow;
            if (mainWindow.MainImageBorder == null) return;

            Settings.Default.BgColorChoice = (Settings.Default.BgColorChoice + 1) % 5;

            mainWindow.MainImageBorder.Background = BackgroundColorBrush;

            Settings.Default.Save();
        }

        /// <summary>
        /// Returns a Brush object based on the BgColorChoice setting.
        /// The BgColorChoice is used to determine the background color of the main window.
        /// The method returns different brushes based on the value of BgColorChoice.
        /// </summary>
        internal static Brush BackgroundColorBrush => Settings.Default.BgColorChoice switch
        {
            0 => Brushes.Transparent,
            1 => Brushes.White,
            2 => new SolidColorBrush(Color.FromRgb(15, 15, 15)),
            3 => DrawingBrushes.CheckerboardDrawingBrush(Colors.White),
            4 => DrawingBrushes.CheckerboardDrawingBrush(Color.FromRgb(235, 235, 235), Color.FromRgb(40, 40, 40), 60),
            _ => Brushes.Transparent,
        };

        #endregion Change background

        #region Change Theme

        /// <summary>
        /// changes the UI theme of the application to a light or dark theme. 
        /// It updates the resource dictionary of the application with the specified theme.
        /// </summary>
        /// <param name="useDarkTheme"></param>
        internal static void ChangeTheme(bool useDarkTheme)
        {
            Application.Current.Resources.MergedDictionaries[1] = new ResourceDictionary
            {
                Source = new Uri(useDarkTheme ? 
                @"/PicView;component/Themes/Styles/ColorThemes/Dark.xaml" :
                @"/PicView;component/Themes/Styles/ColorThemes/Light.xaml", UriKind.Relative)
            };

            Settings.Default.DarkTheme = useDarkTheme;
        }

        #endregion Change Theme

        #region Set ColorTheme

        public enum ColorOption
        {
            Blue = 0,
            Pink = 2,
            Orange = 3,
            Green = 4,
            Red = 5,
            Teal = 6,
            Aqua = 7,
            Golden = 8,
            Purple = 9,
            Cyan = 10,
            Magenta = 11,
            Lime = 12
        }

        /// <summary>
        /// Changes the color theme of the application. 
        /// It takes a ColorOption value and updates the ColorTheme setting of the application to the corresponding value.
        /// The method then calls the UpdateColor method, which updates the application's color scheme to match the new theme.
        /// </summary>
        /// <param name="colorOption"></param>
        internal static void UpdateColorThemeTo(ColorOption colorOption)
        {
            Settings.Default.ColorTheme = (int)colorOption;
            UpdateColor();
        }

        internal static Color GetSecondaryAccentColor => Properties.Settings.Default.ColorTheme switch
        {
            0 => Color.FromRgb(182, 251, 95), // Blue
            2 => Color.FromRgb(255, 237, 38), // Pink
            3 => Color.FromRgb(248, 175, 60), // Orange
            4 => Color.FromRgb(209, 237, 93), // Green
            5 => Color.FromRgb(250, 192, 92), // Red
            6 => Color.FromRgb(254, 172, 150), // Teal
            7 => Color.FromRgb(79, 209, 117), // Aqua
            8 => Color.FromRgb(252, 139, 111), // Golden
            9 => Color.FromRgb(214, 232, 97), // Purple
            10 => Color.FromRgb(180, 246, 88), // Cyan
            11 => Color.FromRgb(255, 237, 38), // Magenta
            12 => Color.FromRgb(202, 253, 82), // Lime
            _ => throw new ArgumentOutOfRangeException(nameof(Properties.Settings.Default.ColorTheme)),
        };

        #endregion Set ColorTheme
    }
}