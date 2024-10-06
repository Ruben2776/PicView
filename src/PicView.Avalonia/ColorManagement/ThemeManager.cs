using Avalonia;
using Avalonia.Styling;
using PicView.Core.Config;

namespace PicView.Avalonia.ColorManagement;
public static class ThemeManager
{
    public enum Theme
    {
        Dark = 0,
        Light = 1,
        Glass = 2
    }
    
    public static void SetTheme(Theme theme)
    {
        var application = Application.Current;
        if (application is null)
            return;
        
        // StyleInclude breaks trimming and AOT

        switch (theme)
        {
            default:
                SettingsHelper.Settings.Theme.Dark = true;
                SettingsHelper.Settings.Theme.GlassTheme = false;
                application.RequestedThemeVariant = ThemeVariant.Dark;
                break;
            case Theme.Light:
                SettingsHelper.Settings.Theme.Dark = false;
                SettingsHelper.Settings.Theme.GlassTheme = false;
                application.RequestedThemeVariant = ThemeVariant.Light;
                break;
            case Theme.Glass:
                SettingsHelper.Settings.Theme.GlassTheme = true;
                application.RequestedThemeVariant = ThemeVariant.Light;
                break;
        }
        
        ColorManager.UpdateAccentColors(SettingsHelper.Settings.Theme.ColorTheme);
    }
}
