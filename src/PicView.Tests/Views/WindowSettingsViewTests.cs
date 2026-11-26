using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.Config;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using PicView.Tests.Config;

namespace PicView.Tests.Views;

[Collection("Sequential")]
public class WindowSettingsViewTests
{
    public WindowSettingsViewTests()
    {
        SetDefaults();
    }

    private static (CoreViewModel Core, MainWindowViewModel A, MainWindowViewModel B, TextToggleButton StayCentered,
        Window Window) Create()
    {
        var core = new CoreViewModel(null, _ => new ImageModel(), StartedSettings.New());
        var a = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        var b = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        core.MainWindows.MainWindows.Add(a);
        core.MainWindows.MainWindows.Add(b);
        a.WindowSettings.IsStayingCentered.Value = true;
        b.WindowSettings.IsStayingCentered.Value = false;
        core.MainWindows.ActiveWindow.Value = a;
        var view = new WindowSettingsView { DataContext = core };
        var window = new Window { Width = 900, Height = 600, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var button = view.FindControl<TextToggleButton>("StayCenteredToggleButton")!;
        return (core, a, b, button, window);
    }

    [AvaloniaFact]
    public void StayCentered_FollowsTheActiveMainWindow_AndSwitchingWritesNothing()
    {
        var (core, a, b, button, window) = Create();
        Assert.True(button.IsChecked);

        core.MainWindows.ActiveWindow.Value = b;
        Dispatcher.UIThread.RunJobs();
        Assert.False(button.IsChecked);

        core.MainWindows.ActiveWindow.Value = a;
        Dispatcher.UIThread.RunJobs();
        Assert.True(button.IsChecked);
        Assert.True(a.WindowSettings.IsStayingCentered.Value);
        Assert.False(b.WindowSettings.IsStayingCentered.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void StayCentered_Clicked_ChangesOnlyTheActiveMainWindow()
    {
        var (core, a, b, button, window) = Create();
        core.MainWindows.ActiveWindow.Value = b;
        Dispatcher.UIThread.RunJobs();

        button.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(b.WindowSettings.IsStayingCentered.Value);
        Assert.True(a.WindowSettings.IsStayingCentered.Value);

        button.IsChecked = false;
        Dispatcher.UIThread.RunJobs();

        Assert.False(b.WindowSettings.IsStayingCentered.Value);
        Assert.True(a.WindowSettings.IsStayingCentered.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void ZoomResizesWindow_Clicked_ChangesOnlyTheActiveMainWindow()
    {
        var (core, a, b, _, window) = Create();
        var button = ((WindowSettingsView)window.Content!).FindControl<TextToggleButton>(
            "ZoomResizesWindowToggleButton")!;
        Assert.False(button.IsChecked);

        button.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(a.WindowSettings.IsZoomResizingWindow.Value);
        Assert.False(b.WindowSettings.IsZoomResizingWindow.Value);

        core.MainWindows.ActiveWindow.Value = b;
        Dispatcher.UIThread.RunJobs();

        Assert.False(button.IsChecked);
        Assert.True(a.WindowSettings.IsZoomResizingWindow.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void ZoomResizesWindow_WithoutAutoFit_IsDisabled()
    {
        var (_, a, _, _, window) = Create();
        var button = ((WindowSettingsView)window.Content!).FindControl<TextToggleButton>(
            "ZoomResizesWindowToggleButton")!;
        Assert.False(button.IsEnabled);

        a.WindowModeState.Value = a.WindowModeState.Value.SetAutoFit(true);
        Dispatcher.UIThread.RunJobs();

        Assert.True(button.IsEnabled);
        window.Close();
    }

    [AvaloniaFact]
    public void StayCentered_NoActiveMainWindow_DoesNothing()
    {
        var (core, a, b, button, window) = Create();
        core.MainWindows.ActiveWindow.Value = null;
        Dispatcher.UIThread.RunJobs();

        var exception = Record.Exception(() =>
        {
            button.IsChecked = !button.IsChecked;
            Dispatcher.UIThread.RunJobs();
        });

        Assert.Null(exception);
        Assert.True(a.WindowSettings.IsStayingCentered.Value);
        Assert.False(b.WindowSettings.IsStayingCentered.Value);
        window.Close();
    }
}
