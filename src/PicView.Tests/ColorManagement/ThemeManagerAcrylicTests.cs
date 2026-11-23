using Avalonia.Controls;
using Avalonia.Media;
using PicView.Avalonia.ColorManagement;

namespace PicView.Tests.ColorManagement;

/// <summary>
/// Without the blur, a tool window's background view is painted with the tool window background; with it, the view
/// gets back what it was given itself.
/// </summary>
[Collection("Sequential")]
public class ThemeManagerAcrylicTests
{
    public ThemeManagerAcrylicTests() => SetDefaults();

    [AvaloniaFact]
    public void BlurOff_PaintsTheViewWithTheToolWindowBackground()
    {
        Settings.Theme.UseAcrylic = false;
        var view = new Border { Background = Brushes.Red };
        var window = new Window { Content = view };

        ThemeManager.ApplyAcrylic(window, view);

        var expected = (SolidColorBrush)BackgroundManager.GetToolWindowBg();
        Assert.Equal(expected.Color, Assert.IsType<SolidColorBrush>(view.Background).Color);
        Assert.Contains(ThemeManager.NoAcrylicClass, window.Classes);
    }

    [AvaloniaFact]
    public void BlurOnAgain_GivesTheViewBackItsOwnBackground()
    {
        Settings.Theme.UseAcrylic = false;
        var view = new Border { Background = Brushes.Red };
        var window = new Window { Content = view };
        ThemeManager.ApplyAcrylic(window, view);

        Settings.Theme.UseAcrylic = true;
        ThemeManager.ApplyAcrylic(window);

        Assert.Same(Brushes.Red, view.Background);
        Assert.DoesNotContain(ThemeManager.NoAcrylicClass, window.Classes);
    }

    [AvaloniaFact]
    public void BlurOff_KeepsWhatTheViewSetsLaterUnderneath()
    {
        Settings.Theme.UseAcrylic = false;
        var view = new Border { Background = Brushes.Red };
        var window = new Window { Content = view };
        ThemeManager.ApplyAcrylic(window, view);

        // As a window's code-behind does for the light or glass theme
        view.Background = Brushes.Blue;
        Settings.Theme.UseAcrylic = true;
        ThemeManager.ApplyAcrylic(window);

        Assert.Same(Brushes.Blue, view.Background);
    }

    [AvaloniaFact]
    public void GlassTheme_KeepsTheBlurWhateverTheSetting()
    {
        Settings.Theme.UseAcrylic = false;
        Settings.Theme.GlassTheme = true;
        var view = new Border { Background = Brushes.Red };
        var window = new Window { Content = view };

        ThemeManager.ApplyAcrylic(window, view);

        Assert.Same(Brushes.Red, view.Background);
        Assert.DoesNotContain(ThemeManager.NoAcrylicClass, window.Classes);
    }
}
