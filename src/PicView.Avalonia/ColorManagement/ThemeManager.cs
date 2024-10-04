using Avalonia;
using Avalonia.Styling;
using PicView.Core.Config;

namespace PicView.Avalonia.ColorManagement;
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

        if (dark)
        {
            SettingsHelper.Settings.Theme.Dark = true;
            application.RequestedThemeVariant = ThemeVariant.Dark;
        }
        else
        {
            SettingsHelper.Settings.Theme.Dark = false;
            application.RequestedThemeVariant = ThemeVariant.Light;
        }
        
        ColorManager.UpdateAccentColors(SettingsHelper.Settings.Theme.ColorTheme);
    }
}
