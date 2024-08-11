﻿using PicView.Core.Config;
using PicView.WPF.Animations;
using PicView.WPF.PicGallery;
using PicView.WPF.Themes.Resources;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.DragAndDrop;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PicView.WPF.ImageHandling;

namespace PicView.WPF.ConfigureSettings;

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
        ConfigureWindows.GetMainWindow?.Dispatcher?.Invoke(() =>
        {
            var getColor = AnimationHelper.GetPreferredColor();
            var getColorBrush = new SolidColorBrush(getColor);
            Application.Current.Resources["ChosenColor"] = getColor;
            Application.Current.Resources["ChosenColorBrush"] = getColorBrush;

            var getAccentColor = GetSecondaryAccentColor;
            var getAccentColorBrush = new SolidColorBrush(getAccentColor);
            Application.Current.Resources["ChosenAccentColor"] = getAccentColor;
            Application.Current.Resources["ChosenAccentColorBrush"] = getAccentColorBrush;
        });
    }

    /// <summary>
    /// Apply color variables from themes
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

    internal static async Task MainWindowUnfocusOrFocus(bool isFocused)
    {
        var w = ConfigureWindows.GetMainWindow;

        var foregroundColor = isFocused
            ? (SolidColorBrush)Application.Current.Resources["MainColorBrush"]
            : (SolidColorBrush)Application.Current.Resources["MainColorFadedBrush"];

        w.TitleText.InnerTextBox.Foreground = foregroundColor;

        if (SettingsHelper.Settings.Theme.Dark)
        {
            w.BorderBrush = isFocused
                ? (SolidColorBrush)Application.Current.Resources["BorderBrush"]
                : (SolidColorBrush)Application.Current.Resources["BorderBrushAlt"];

            w.TitleBar.Background =
                isFocused
                    ? (SolidColorBrush)Application.Current.Resources["SubtleFadeBrush"]
                    : (SolidColorBrush)Application.Current.Resources["BackgroundColorBrush"];
            w.LowerBar.Background = isFocused
                ? (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushAlt"]
                : (SolidColorBrush)Application.Current.Resources["BackgroundColorBrush"];
        }
        else
        {
            w.LeftButtonContainer.Background =
                w.Logo.Background =
                    w.CloseButton.Background =
                        w.MinButton.Background =
                            w.FullscreenButton.Background =
                                isFocused
                                    ? (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushAlt"]
                                    : (SolidColorBrush)Application.Current.Resources[
                                        "BackgroundHoverHighlightBrush"];

            w.LowerBar.Background = isFocused
                ? (SolidColorBrush)Application.Current.Resources["AltInterfaceWBrush"]
                : (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushAlt"];
        }

        // Delay to fix inactive window being hit
        await Task.Delay(20);
        if (isFocused)
        {
            w.MainImage.MouseLeftButtonDown += DragToExplorer.DragFile;
        }
        else
        {
            w.MainImage.MouseLeftButtonDown -= DragToExplorer.DragFile;
        }
    }

    internal static void WindowUnfocusOrFocus(Panel titleBar, TextBlock? title, Border? expander, bool isFocused)
    {
        var foregroundColor = isFocused
            ? (SolidColorBrush)Application.Current.Resources["MainColorBrush"]
            : (SolidColorBrush)Application.Current.Resources["MainColorFadedBrush"];

        if (title is not null)
        {
            title.Foreground = foregroundColor;
        }

        if (SettingsHelper.Settings.Theme.Dark)
        {
            titleBar.Background =
                isFocused
                    ? (SolidColorBrush)Application.Current.Resources["SubtleFadeBrush"]
                    : (SolidColorBrush)Application.Current.Resources["BorderBrush"];
        }
        else
        {
            titleBar.Background =
                isFocused
                    ? (SolidColorBrush)Application.Current.Resources["WindowBackgroundColorBrush"]
                    : (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushFadeSubtle"];
        }

        if (expander is not null)
        {
            expander.Background =
                isFocused
                    ? (SolidColorBrush)Application.Current.Resources["BackgroundColorBrushFade"]
                    : (SolidColorBrush)Application.Current.Resources["BorderBrush"];
        }
    }

    #endregion Window LostFocus style change

    #region Change background

    /// <summary>
    /// Changes the background color of the displayed image.
    /// If the image has a transparent background, it increments the BgColorChoice setting,
    /// which is used to determine the background color, and sets the background color of the main window
    /// to the BackgroundColorBrush brush.
    /// </summary>
    internal static void ChangeBackground()
    {
        if (GalleryFunctions.IsGalleryOpen)
        {
            return;
        }

        // Get the main window and check if it has a valid image border and source
        var mainWindow = ConfigureWindows.GetMainWindow;
        if (mainWindow.MainImageBorder is null || mainWindow.MainImage.Source is null) return;

        // Check if the main image has a transparent background
        if (ImageFunctions.HasTransparentBackground(mainWindow.MainImage.Source as BitmapSource) is false) return;

        // Increment the BgColorChoice setting
        SettingsHelper.Settings.UIProperties.BgColorChoice = (SettingsHelper.Settings.UIProperties.BgColorChoice + 1) % 5;

        // Set the background color of the main window to the BackgroundColorBrush brush
        mainWindow.MainImageBorder.Background = BackgroundColorBrush;

        // Save the changes to the settings
        _ = SettingsHelper.SaveSettingsAsync();
    }

    /// <summary>
    /// Returns a Brush object based on the BgColorChoice setting.
    /// The BgColorChoice is used to determine the background color of the main window.
    /// The method returns different brushes based on the value of BgColorChoice.
    /// </summary>
    internal static Brush BackgroundColorBrush => SettingsHelper.Settings.UIProperties.BgColorChoice switch
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
            Source = new Uri(@"/PicView;component/Themes/Styles/ColorThemes/Light.xaml", UriKind.Relative)
        };
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
        SettingsHelper.Settings.Theme.ColorTheme = (int)colorOption;
        UpdateColor();
    }

    internal static Color GetSecondaryAccentColor => SettingsHelper.Settings.Theme.ColorTheme switch
    {
        0 => Color.FromRgb(255, 240, 90), // Blue
        2 => Color.FromRgb(255, 237, 38), // Pink
        3 => Color.FromRgb(248, 175, 60), // Orange
        4 => Color.FromRgb(209, 237, 93), // Green
        5 => Color.FromRgb(250, 192, 92), // Red
        6 => Color.FromRgb(254, 172, 150), // Teal
        7 => Color.FromRgb(228, 209, 17), // Aqua
        8 => Color.FromRgb(255, 253, 42), // Golden
        9 => Color.FromRgb(237, 184, 135), // Purple
        10 => Color.FromRgb(255, 253, 66), // Cyan
        11 => Color.FromRgb(255, 237, 38), // Magenta
        12 => Color.FromRgb(255, 253, 42), // Lime
        _ => throw new ArgumentOutOfRangeException(nameof(GetSecondaryAccentColor)),
    };

    #endregion Set ColorTheme
}