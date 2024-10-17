using Avalonia;
using Avalonia.Media;
using PicView.Core.Config;

namespace PicView.Avalonia.ColorManagement;

/// <summary>
/// Manages accent colors based on the selected color theme.
/// </summary>
public static class ColorManager
{
    /// <summary>
    /// Gets the logo accent color based on the current color theme.
    /// </summary>
    /// <value>
    /// A <see cref="Color"/> value representing the logo accent color.
    /// </value>
    public static Color GetLogoAccentColor => SettingsHelper.Settings.Theme.ColorTheme switch
    {
        0 => SettingsHelper.Settings.Theme.Dark ? Color.FromRgb(255, 240, 90) : Color.FromRgb(225, 210, 80), // Blue
        2 => SettingsHelper.Settings.Theme.Dark ? Color.FromRgb(255, 237, 38) : Color.FromRgb(250, 180, 38), // Pink
        3 => Color.FromRgb(248, 175, 60), // Orange
        4 => SettingsHelper.Settings.Theme.Dark ? Color.FromRgb(209, 237, 93) : Color.FromRgb(175, 157, 38), // Green
        5 => Color.FromRgb(250, 192, 92), // Red
        6 => Color.FromRgb(254, 172, 150), // Teal
        7 => Color.FromRgb(228, 209, 17), // Aqua
        8 => SettingsHelper.Settings.Theme.Dark ? Color.FromRgb(255, 253, 42) : Color.FromRgb(226, 180, 224), // Golden
        9 => SettingsHelper.Settings.Theme.Dark ? Color.FromRgb(237, 184, 135) : Color.FromRgb(226, 141, 223), // Purple
        10 => SettingsHelper.Settings.Theme.Dark ? Color.FromRgb(255, 253, 66) : Color.FromRgb(215, 200, 70), // Cyan
        11 => SettingsHelper.Settings.Theme.Dark ? Color.FromRgb(255, 237, 38) : Color.FromRgb(226, 141, 223), // Magenta
        12 => Color.FromRgb(255, 253, 42), // Lime
        _ => throw new ArgumentOutOfRangeException(nameof(GetLogoAccentColor))
    };

    /// <summary>
    /// Gets the secondary accent color based on the current color theme.
    /// </summary>
    /// <value>
    /// A <see cref="Color"/> value representing the secondary accent color.
    /// </value>
    public static Color GetSecondaryAccentColor => SettingsHelper.Settings.Theme.ColorTheme switch
    {
        0 => Color.FromArgb(0xF2, 0x0D, 0x80, 0xEE),  // Blue -> #F200ADEE
        2 => Color.FromArgb(0xF2, 0xFF, 0x86, 0xDB),  // Pink -> #F2FF86DB
        3 => Color.FromArgb(0xF2, 0xFF, 0x64, 0x41),  // Orange -> #F2FF6441
        4 => Color.FromArgb(0xF2, 0x0D, 0x80, 0x39),  // Green -> #F20D8039
        5 => Color.FromArgb(0xF2, 0xF3, 0x53, 0x53),  // Red -> #F2F35353
        6 => Color.FromArgb(0xF2, 0x1F, 0xAE, 0x98),  // Teal -> #F21FAE98
        7 => Color.FromArgb(0xF2, 0x09, 0xA6, 0x8E),  // Aqua -> #F209A68E
        8 => Color.FromArgb(0xF2, 0xFE, 0xA9, 0x55),  // Golden -> #F2FEA955
        9 => Color.FromArgb(0xF2, 0xA9, 0x53, 0xF5),  // Purple -> #F2A953F5
        10 => Color.FromArgb(0xF2, 0x59, 0xBA, 0xE9), // Cyan -> #F259BAE9
        11 => Color.FromArgb(0xF2, 0xFF, 0x6C, 0xD4), // Magenta -> #F2FF6CD4
        12 => Color.FromArgb(0xF2, 0x22, 0xCB, 0x97), // Lime -> #F222CB97
        _ => throw new ArgumentOutOfRangeException(nameof(GetSecondaryAccentColor))
    };

    /// <summary>
    /// Gets the primary accent color based on the current color theme.
    /// </summary>
    /// <value>
    /// A <see cref="Color"/> value representing the primary accent color.
    /// </value>
    public static Color GetPrimaryAccentColor => SettingsHelper.Settings.Theme.ColorTheme switch
    {
        0 => Color.FromRgb(26, 140, 240),  // Blue 
        2 => Color.FromRgb(255, 53, 197),  // Pink
        3 => Color.FromRgb(219, 91, 61),   // Orange
        4 => Color.FromRgb(34, 203, 151),  // Green
        5 => Color.FromRgb(249, 17, 16),   // Red
        6 => Color.FromRgb(68, 161, 160),  // Teal
        7 => Color.FromRgb(54, 230, 204),  // Aqua
        8 => Color.FromRgb(254, 169, 85),  // Golden
        9 => Color.FromRgb(151, 56, 235),  // Purple
        10 => Color.FromRgb(27, 161, 226), // Cyan
        11 => Color.FromRgb(230, 139, 238),// Magenta
        12 => Color.FromRgb(32, 231, 107), // Lime
        _ => throw new ArgumentOutOfRangeException(nameof(GetPrimaryAccentColor))
    };

    /// <summary>
    /// Updates the accent colors in the application resources based on the selected color theme.
    /// </summary>
    /// <param name="colorTheme">The color theme index to apply.</param>
    public static void UpdateAccentColors(int colorTheme)
    {
        SettingsHelper.Settings.Theme.ColorTheme = colorTheme;

        var primaryAccentColor = GetPrimaryAccentColor;
        var secondaryAccentColor = GetSecondaryAccentColor;
        var logoAccentColor = GetLogoAccentColor;

        var primaryBrush = new SolidColorBrush(primaryAccentColor);
        var secondaryBrush = new SolidColorBrush(secondaryAccentColor);
        var logoAccentBrush = new SolidColorBrush(logoAccentColor);
        
        if (SettingsHelper.Settings.Theme.GlassTheme)
        {
            ThemeManager.GlassThemeUpdates();
        }

        // Retrieve existing brushes and replace them
        if (Application.Current.TryGetResource("AccentColor", Application.Current.RequestedThemeVariant, out _))
        {
            Application.Current.Resources["AccentColor"] = primaryBrush;
        }

        if (Application.Current.TryGetResource("SecondaryAccentColor", Application.Current.RequestedThemeVariant, out _))
        {
            Application.Current.Resources["SecondaryAccentColor"] = secondaryBrush;
        }

        if (Application.Current.TryGetResource("LogoAccentColor", Application.Current.RequestedThemeVariant, out _))
        {
            Application.Current.Resources["LogoAccentColor"] = logoAccentBrush;
        }
        
        
    }
}
