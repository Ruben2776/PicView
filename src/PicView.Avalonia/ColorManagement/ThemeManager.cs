using Avalonia;
using Avalonia.Styling;

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
                Settings.Theme.Dark = true;
                Settings.Theme.GlassTheme = false;
                application.RequestedThemeVariant = ThemeVariant.Dark;
                break;
            case Theme.Light:
                Settings.Theme.Dark = false;
                Settings.Theme.GlassTheme = false;
                application.RequestedThemeVariant = ThemeVariant.Light;
                break;
            case Theme.Glass:
                Settings.Theme.GlassTheme = true;
                application.RequestedThemeVariant = ThemeVariant.Light;
                break;
        }
        
        ColorManager.UpdateAccentColors(Settings.Theme.ColorTheme);
    }

    public static void DetermineTheme(Application? application, bool settingsExists)
    {
        if (!settingsExists)
        {
            // Use dark theme by default, until I can make a proper good light theme
            application.RequestedThemeVariant = ThemeVariant.Dark;
            
            // application.RequestedThemeVariant = application.ActualThemeVariant;
            // Settings.Theme.Dark = application.RequestedThemeVariant == ThemeVariant.Dark;
        }
        else
        {
            if (Settings.Theme.GlassTheme)
            {
                application.RequestedThemeVariant = ThemeVariant.Light;
            }
            else if (Settings.Theme.UseSystemTheme)
            {
                application.RequestedThemeVariant = ThemeVariant.Dark; // TODO : Figure out how to get the system theme
            }
            else
            {
                application.RequestedThemeVariant = Settings.Theme.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
            }
        }
    }
    
    public static void SetColorTheme(int colorIndex)
    {
        ColorManager.UpdateAccentColors(colorIndex);
    }
}
