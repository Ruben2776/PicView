using Avalonia;
using Avalonia.Styling;
using PicView.Core.Config;

namespace PicView.Avalonia.PicViewTheme;
public static class ThemeManager
{
    public static void ChangeTheme()
    {
        SetTheme(SettingsHelper.Settings.Theme.Dark);
    }
    
    public static void SetTheme(bool dark)
    {
        var application = Application.Current;
        if (application is null)
            return;
        
        // StyleInclude breaks trimming and AOT

        // Add the new theme
        if (dark)
        {
            // Load Dark theme
            SettingsHelper.Settings.Theme.Dark = true;
            application.RequestedThemeVariant = ThemeVariant.Dark;
        }
        else
        {
            // Load Light theme
            SettingsHelper.Settings.Theme.Dark = false;
            application.RequestedThemeVariant = ThemeVariant.Light;
        }
    }
}
