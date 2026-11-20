using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.Config;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using PicView.Tests.Config;

namespace PicView.Tests.Views;

/// <summary>
/// The interface, bottom bar and hover bar controls of the Settings window, which show the Active Main Window's own
/// settings.
/// </summary>
[Collection("Sequential")]
public class InterfaceConfigurationViewTests
{
    public InterfaceConfigurationViewTests()
    {
        SetDefaults();
    }

    // Review Focus 3
    [AvaloniaFact]
    public void InterfaceSettings_ShowTheActiveMainWindowsTexts_AndFollowASwitch_AndSwitchingWritesNothing()
    {
        Settings.UIProperties.ShowInterface = true;
        Settings.UIProperties.ShowBottomNavBar = true;
        Settings.UIProperties.ShowHoverNavigationBar = true;
        var core = new CoreViewModel(null, _ => new ImageModel(), StartedSettings.New());
        var t = core.Translation;
        t.HideUI.Value = "hide ui";
        t.ShowUI.Value = "show ui";
        t.HideBottomToolbar.Value = "hide bottom";
        t.ShowBottomToolbar.Value = "show bottom";
        t.HideHoverNavigationBar.Value = "hide hover";
        t.ShowHoverNavigationBar.Value = "show hover";
        var a = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        var b = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        core.MainWindows.MainWindows.Add(a);
        core.MainWindows.MainWindows.Add(b);
        a.WindowSettings.ShowInterface.Value = false;
        a.WindowSettings.ShowBottomBar.Value = false;
        a.WindowSettings.ShowHoverBar.Value = false;
        core.MainWindows.ActiveWindow.Value = a;
        var view = new InterfaceConfigurationView { DataContext = core };
        var window = new Window { Width = 900, Height = 600, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var bottomBar = view.FindControl<TextIconButton>("ShowBottomToolbarButton")!;
        var hideUi = view.FindControl<TextIconButton>("HideUIButton")!;
        var showUi = view.FindControl<TextIconButton>("ShowUIButton")!;
        var hoverBar = view.FindControl<TextIconButton>("ToggleHoverButton")!;

        Assert.Equal("show bottom", bottomBar.Text);
        Assert.Equal("show hover", hoverBar.Text);
        Assert.True(hideUi.IsVisible);
        Assert.False(showUi.IsVisible);
        Assert.Equal("show ui", hideUi.Text);

        core.MainWindows.ActiveWindow.Value = b;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("hide bottom", bottomBar.Text);
        Assert.Equal("hide hover", hoverBar.Text);
        Assert.False(hideUi.IsVisible);
        Assert.True(showUi.IsVisible);
        Assert.Equal("hide ui", showUi.Text);

        // A change in the active window shows at once
        b.WindowSettings.ShowBottomBar.Value = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("show bottom", bottomBar.Text);

        core.MainWindows.ActiveWindow.Value = a;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("show hover", hoverBar.Text);
        Assert.False(a.WindowSettings.ShowInterface.Value);
        Assert.False(a.WindowSettings.ShowBottomBar.Value);
        Assert.False(a.WindowSettings.ShowHoverBar.Value);
        Assert.True(b.WindowSettings.ShowInterface.Value);
        Assert.False(b.WindowSettings.ShowBottomBar.Value);
        Assert.True(b.WindowSettings.ShowHoverBar.Value);
        Assert.True(Settings.UIProperties.ShowInterface);
        Assert.True(Settings.UIProperties.ShowBottomNavBar);
        Assert.True(Settings.UIProperties.ShowHoverNavigationBar);
        window.Close();
    }

    [AvaloniaFact]
    public void ActiveMainWindowClosesUnderTheView_ControlsFollowTheRemainingWindow_OrGoQuiet()
    {
        var core = new CoreViewModel(null, _ => new ImageModel(), StartedSettings.New());
        var t = core.Translation;
        t.HideBottomToolbar.Value = "hide bottom";
        t.ShowBottomToolbar.Value = "show bottom";
        t.HideHoverNavigationBar.Value = "hide hover";
        t.ShowHoverNavigationBar.Value = "show hover";
        var a = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        var b = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        core.MainWindows.MainWindows.Add(a);
        core.MainWindows.MainWindows.Add(b);
        a.WindowSettings.ShowBottomBar.Value = false;
        a.WindowSettings.ShowHoverBar.Value = false;
        b.WindowSettings.ShowBottomBar.Value = true;
        b.WindowSettings.ShowHoverBar.Value = true;
        core.MainWindows.ActiveWindow.Value = a;
        var view = new InterfaceConfigurationView { DataContext = core };
        var window = new Window { Width = 900, Height = 600, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var bottomBar = view.FindControl<TextIconButton>("ShowBottomToolbarButton")!;
        var hideUi = view.FindControl<TextIconButton>("HideUIButton")!;
        var showUi = view.FindControl<TextIconButton>("ShowUIButton")!;
        var hoverBar = view.FindControl<TextIconButton>("ToggleHoverButton")!;
        Assert.Equal("show bottom", bottomBar.Text);

        // As CloseAsync does: the closing window's settings are disposed, and the Active Main Window re-pointed
        a.WindowSettings.Dispose();
        core.MainWindows.MainWindows.Remove(a);
        core.MainWindows.ActiveWindow.Value = b;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("hide bottom", bottomBar.Text);
        Assert.Equal("hide hover", hoverBar.Text);

        // The last one closes: nothing is active, and the controls do nothing
        b.WindowSettings.Dispose();
        core.MainWindows.MainWindows.Remove(b);
        core.MainWindows.ActiveWindow.Value = null;
        Dispatcher.UIThread.RunJobs();
        foreach (var button in new[] { bottomBar, hideUi, showUi, hoverBar })
        {
            var exception = Record.Exception(() =>
            {
                if (button.Command?.CanExecute(button.CommandParameter) is true)
                {
                    button.Command.Execute(button.CommandParameter);
                }
            });
            Assert.Null(exception);
        }

        Dispatcher.UIThread.RunJobs();
        Assert.False(a.WindowSettings.ShowBottomBar.Value);
        Assert.False(a.WindowSettings.ShowHoverBar.Value);
        Assert.True(b.WindowSettings.ShowBottomBar.Value);
        Assert.True(b.WindowSettings.ShowHoverBar.Value);
        window.Close();
    }
}
