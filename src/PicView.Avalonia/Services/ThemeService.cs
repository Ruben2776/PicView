using PicView.Avalonia.ColorManagement;
using PicView.Core.ISettings;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Services;

/// <param name="mainWindows">The Main Windows, whose active one the background is set for; null sets none.</param>
public class ThemeService(MainWindowOverviewViewModel? mainWindows = null) : IThemeService
{
    public void SetTheme(int themeIndex)
    {
        if (Enum.IsDefined(typeof(ThemeManager.Theme), themeIndex))
        {
            ThemeManager.SetTheme((ThemeManager.Theme)themeIndex);
        }
    }

    public void SetColorTheme(int colorIndex)
    {
        ThemeManager.SetColorTheme(colorIndex);
    }

    /// <inheritdoc cref="BackgroundManager.SetBackground(MainWindowOverviewViewModel, int)"/>
    public void SetBackground(int backgroundIndex)
    {
        BackgroundManager.SetBackground(mainWindows, backgroundIndex);
    }
}
