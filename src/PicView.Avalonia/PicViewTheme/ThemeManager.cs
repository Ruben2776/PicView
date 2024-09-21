using Avalonia;
using Avalonia.Styling;
using PicView.Core.Config;

namespace PicView.Avalonia.PicViewTheme;
public static class ThemeManager
{
    // TODO: Implement changing Dark/Light theme
    public static void ChangeTheme()
    {
        SetTheme(SettingsHelper.Settings.Theme.Dark);
    }
    
    public static void SetTheme(bool dark)
    {
        var application = Application.Current;
        if (application is null)
            return;
        
        // https://www.codeproject.com/Articles/5317972/Theming-and-Localization-Functionality-for-Multipl

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
