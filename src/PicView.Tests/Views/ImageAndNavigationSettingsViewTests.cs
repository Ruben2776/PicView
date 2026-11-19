using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.Config;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using PicView.Tests.Config;

namespace PicView.Tests.Views;

/// <summary>
/// The Side-by-Side, looping, scrolling and Zoom to Fit controls of the Settings window, which show the Active Main
/// Window's own settings.
/// </summary>
[Collection("Sequential")]
public class ImageAndNavigationSettingsViewTests
{
    public ImageAndNavigationSettingsViewTests()
    {
        SetDefaults();
    }

    [AvaloniaFact]
    public void SideBySide_And_Looping_ShowTheActiveMainWindowsValueAndText_AndFollowASwitch()
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()), StartedSettings.New());
        core.Translation.LoopingEnabled.Value = "on";
        core.Translation.LoopingDisabled.Value = "off";
        var a = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        var b = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        core.MainWindows.MainWindows.Add(a);
        core.MainWindows.MainWindows.Add(b);
        a.WindowSettings.IsSideBySide.Value = true;
        a.WindowSettings.IsLooping.Value = true;
        core.MainWindows.ActiveWindow.Value = a;
        var image = new ImageSettingsView { DataContext = core };
        var navigation = new NavigationView { DataContext = core };
        var window = new Window
        {
            Width = 900, Height = 600, Content = new StackPanel { Children = { image, navigation } }
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var sideBySide = image.FindControl<TextToggleButton>("SideBySideToggleButton")!;
        var looping = navigation.FindControl<TextToggleButton>("LoopingToggleButton")!;

        Assert.True(sideBySide.IsChecked);
        Assert.True(looping.IsChecked);
        Assert.Equal("on", looping.Text);

        core.MainWindows.ActiveWindow.Value = b;
        Dispatcher.UIThread.RunJobs();
        Assert.False(sideBySide.IsChecked);
        Assert.False(looping.IsChecked);
        Assert.Equal("off", looping.Text);

        // A change in the active window shows at once
        b.WindowSettings.IsLooping.Value = true;
        Dispatcher.UIThread.RunJobs();
        Assert.True(looping.IsChecked);
        Assert.Equal("on", looping.Text);

        core.MainWindows.ActiveWindow.Value = a;
        Dispatcher.UIThread.RunJobs();
        Assert.True(sideBySide.IsChecked);
        Assert.True(a.WindowSettings.IsSideBySide.Value);
        Assert.False(b.WindowSettings.IsSideBySide.Value);
        Assert.False(Settings.ImageScaling.ShowImageSideBySide);
        Assert.False(Settings.UIProperties.Looping);
        window.Close();
    }

    [AvaloniaFact]
    public void Scrolling_And_ZoomToFit_ShowTheActiveMainWindowsValueAndText_AndFollowASwitch()
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()), StartedSettings.New());
        core.Translation.ScrollEnabled.Value = "on";
        core.Translation.ScrollDisabled.Value = "off";
        var a = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        var b = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        core.MainWindows.MainWindows.Add(a);
        core.MainWindows.MainWindows.Add(b);
        a.WindowSettings.IsScrollingEnabled.Value = true;
        a.WindowSettings.IsZoomedToFit.Value = true;
        core.MainWindows.ActiveWindow.Value = a;
        var image = new ImageSettingsView { DataContext = core };
        var zoom = new ZoomSettingsView { DataContext = core };
        var window = new Window
        {
            Width = 900, Height = 600, Content = new StackPanel { Children = { image, zoom } }
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var scrolling = image.FindControl<TextToggleButton>("ScrollToggleButton")!;
        var zoomToFit = zoom.FindControl<TextToggleButton>("ZoomToFitToggleButton")!;

        Assert.True(scrolling.IsChecked);
        Assert.Equal("on", scrolling.Text);
        Assert.True(zoomToFit.IsChecked);

        core.MainWindows.ActiveWindow.Value = b;
        Dispatcher.UIThread.RunJobs();
        Assert.False(scrolling.IsChecked);
        Assert.Equal("off", scrolling.Text);
        Assert.False(zoomToFit.IsChecked);

        // A change in the active window shows at once
        b.WindowSettings.IsScrollingEnabled.Value = true;
        b.WindowSettings.IsZoomedToFit.Value = true;
        Dispatcher.UIThread.RunJobs();
        Assert.True(scrolling.IsChecked);
        Assert.Equal("on", scrolling.Text);
        Assert.True(zoomToFit.IsChecked);

        b.WindowSettings.IsZoomedToFit.Value = false;
        core.MainWindows.ActiveWindow.Value = a;
        Dispatcher.UIThread.RunJobs();
        Assert.True(zoomToFit.IsChecked);
        Assert.True(a.WindowSettings.IsZoomedToFit.Value);
        Assert.False(b.WindowSettings.IsZoomedToFit.Value);
        Assert.False(Settings.ImageScaling.ZoomToFit);
        Assert.False(Settings.Zoom.ScrollEnabled);
        window.Close();
    }
}
