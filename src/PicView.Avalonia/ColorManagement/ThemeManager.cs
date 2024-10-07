using Avalonia;
using Avalonia.Media;
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
                GlassThemeUpdates();
                break;
        }
        
        ColorManager.UpdateAccentColors(SettingsHelper.Settings.Theme.ColorTheme);
    }

    public static void GlassThemeUpdates()
    {
        if (!Application.Current.TryGetResource("MainTextColor",
                ThemeVariant.Dark, out var textColor))
        {
            return;
        }

        if (textColor is not Color mainColor)
        {
            return;
        }

        Application.Current.Resources["MainTextColor"] = mainColor;
        
        Application.Current.Resources["MainButtonBackgroundColor"] = Color.Parse("#E5F1F1F1");

        Application.Current.Resources["MainBorderColor"] = Colors.Transparent;
        Application.Current.Resources["SecondaryBorderColor"] = Colors.Transparent;
        Application.Current.Resources["TertiaryBorderColor"] = Colors.Transparent;
        
        Application.Current.Resources["ContextMenuTextColor"] = mainColor;
        Application.Current.Resources["ContextMenuBackgroundColor"] = Color.Parse("#A1464646");
            
        Application.Current.Resources["MenuBackgroundColor"] = Color.Parse("#D73E3E3E");
        Application.Current.Resources["MenuButtonColor"] = Color.Parse("#76909090");
    }
}
