using PicView.Avalonia.ColorManagement;
using PicView.Core.ISettings;

namespace PicView.Avalonia.Services;

public class ThemeService : IThemeService
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

    public void SetBackground(int backgroundIndex)
    {
        BackgroundManager.SetBackground(backgroundIndex);
    }
}
