namespace PicView.Core.ISettings;

public interface IThemeService
{
    void SetTheme(int themeIndex);
    void SetColorTheme(int colorIndex);

    /// <summary>
    /// Sets the Active Main Window's background choice; does nothing when no Main Window is active.
    /// </summary>
    void SetBackground(int backgroundIndex);
}
