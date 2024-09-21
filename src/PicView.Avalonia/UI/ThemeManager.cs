using Avalonia;
using Avalonia.Styling;
using PicView.Core.Config;

namespace PicView.Avalonia.UI;
public static class ThemeManager
{
    // TODO: Implement changing Dark/Light theme
    public static void ChangeTheme()
    {
        var application = Application.Current;
        if (application is null)
            return;
        
        var styles = application.Styles;
        styles.Clear();
        
        if (SettingsHelper.Settings.Theme.Dark)
        {
            // Change to light theme
            styles.Add(new Styles
            {

            });
            SettingsHelper.Settings.Theme.Dark = false;
            application.RequestedThemeVariant = ThemeVariant.Light;
        }
        else
        {
            // Change to dark theme
            styles.Add(new Styles
            {

            });
            SettingsHelper.Settings.Theme.Dark = true;
            application.RequestedThemeVariant = ThemeVariant.Dark;
        }
    }
}
