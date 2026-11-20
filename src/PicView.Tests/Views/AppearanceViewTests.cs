using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Services;
using PicView.Avalonia.Views.Config;
using PicView.Core.ColorHandling;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using PicView.Tests.Config;
using R3;

namespace PicView.Tests.Views;

/// <summary>
/// The background palette and the constrain toggle of the Settings window, which show and edit the Active Main
/// Window's own background.
/// </summary>
[Collection("Sequential")]
public class AppearanceViewTests
{
    private const string Active = "activeColorAccentBtn";

    public AppearanceViewTests()
    {
        // The Settings view model follows its other settings frame by frame
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        Settings.UIProperties.BgColorChoice = (int)BackgroundType.Transparent;
        Settings.UIProperties.IsConstrainBackgroundColorEnabled = false;
    }

    private static (CoreViewModel Core, AppearanceView View, Window Window) Create()
    {
        var core = new CoreViewModel(null, _ => new ImageModel(), StartedSettings.New());
        core.SettingsViewModel = new SettingsViewModel(core.Translation, core.GlobalSettings);
        core.SettingsViewModel.Initialize(new ThemeService(core.MainWindows), new LanguageService(),
            new ImageSettingsService());
        var view = new AppearanceView { DataContext = core };
        var window = new Window { Width = 900, Height = 900, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (core, view, window);
    }

    private static MainWindowViewModel AddWindow(CoreViewModel core, BackgroundType choice, bool constrains)
    {
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        vm.WindowSettings.BackgroundChoice.Value = (int)choice;
        vm.WindowSettings.IsConstrainingBackgroundColor.Value = constrains;
        core.MainWindows.MainWindows.Add(vm);
        return vm;
    }

    // Review Focus 3
    [AvaloniaFact]
    public void Palette_ShowsAndSetsTheActiveMainWindowsChoice_AndFollowsASwitch()
    {
        var (core, view, window) = Create();
        var a = AddWindow(core, BackgroundType.White, constrains: true);
        var b = AddWindow(core, BackgroundType.DarkGray, constrains: false);
        core.MainWindows.ActiveWindow.Value = a;
        Dispatcher.UIThread.RunJobs();
        var white = view.FindControl<Button>("WhiteBgButton")!;
        var darkGray = view.FindControl<Button>("DarkGrayBgButton")!;
        var gray = view.FindControl<Button>("GrayBgButton")!;
        var constrain = view.FindControl<TextToggleButton>("ConstrainingBackgroundColorToggleButton")!;

        Assert.True(white.Classes.Contains(Active));
        Assert.False(darkGray.Classes.Contains(Active));
        Assert.True(constrain.IsChecked);

        core.MainWindows.ActiveWindow.Value = b;
        Dispatcher.UIThread.RunJobs();

        Assert.False(white.Classes.Contains(Active));
        Assert.True(darkGray.Classes.Contains(Active));
        Assert.False(constrain.IsChecked);
        // Switching wrote nothing
        Assert.Equal((int)BackgroundType.White, a.WindowSettings.BackgroundChoice.Value);
        Assert.Equal((int)BackgroundType.DarkGray, b.WindowSettings.BackgroundChoice.Value);
        Assert.True(a.WindowSettings.IsConstrainingBackgroundColor.Value);
        Assert.False(b.WindowSettings.IsConstrainingBackgroundColor.Value);

        gray.Command!.Execute(gray.CommandParameter);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal((int)BackgroundType.LightGray, b.WindowSettings.BackgroundChoice.Value);
        Assert.Equal((int)BackgroundType.White, a.WindowSettings.BackgroundChoice.Value);
        Assert.True(gray.Classes.Contains(Active));
        Assert.False(darkGray.Classes.Contains(Active));
        Assert.Equal((int)BackgroundType.Transparent, Settings.UIProperties.BgColorChoice);
        Assert.False(Settings.UIProperties.IsConstrainBackgroundColorEnabled);
        window.Close();
        core.SettingsViewModel.Dispose();
    }

    [AvaloniaFact]
    public void Palette_NoActiveMainWindow_DoesNothing()
    {
        var (core, view, window) = Create();
        var a = AddWindow(core, BackgroundType.White, constrains: false);
        core.MainWindows.ActiveWindow.Value = null;
        Dispatcher.UIThread.RunJobs();
        var white = view.FindControl<Button>("WhiteBgButton")!;
        var gray = view.FindControl<Button>("GrayBgButton")!;

        Assert.False(white.Classes.Contains(Active));

        gray.Command!.Execute(gray.CommandParameter);
        new ThemeService(core.MainWindows).SetBackground((int)BackgroundType.NearBlack);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal((int)BackgroundType.White, a.WindowSettings.BackgroundChoice.Value);
        Assert.False(gray.Classes.Contains(Active));
        Assert.Equal((int)BackgroundType.Transparent, Settings.UIProperties.BgColorChoice);
        window.Close();
        core.SettingsViewModel.Dispose();
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
