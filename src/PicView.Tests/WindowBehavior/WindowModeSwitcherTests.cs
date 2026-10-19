using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Tests.WindowBehavior;

[Collection("Sequential")]
public class WindowModeSwitcherTests
{
    private readonly CoreViewModel _core;
    private int _saves;

    public WindowModeSwitcherTests()
    {
        SetDefaults();
        TranslationManager.Init();
        _core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
    }

    [AvaloniaFact]
    public async Task ToggleFullscreen_EntersNativeFullscreenHidesInterfaceAndSaves()
    {
        var (switcher, adapter, vm, _) = CreateWindow();

        await switcher.ToggleFullscreen();

        Assert.Equal([WindowMode.Fullscreen], adapter.Entered);
        Assert.True(vm.IsFullscreen.Value);
        Assert.False(vm.IsUIShown.Value);
        Assert.True(Settings.WindowProperties.Fullscreen);
        Assert.Equal(1, _saves);
    }

    [AvaloniaFact]
    public async Task ToggleFullscreen_FromMaximized_LeavingGivesBackMaximizedAndTheInterface()
    {
        Settings.WindowProperties.Maximized = true;
        var (switcher, adapter, vm, _) = CreateWindow();
        switcher.Start(isAppStart: true);

        await switcher.ToggleFullscreen();
        await switcher.ToggleFullscreen();

        Assert.Equal([WindowMode.Maximized, WindowMode.Fullscreen, WindowMode.Maximized], adapter.Entered);
        Assert.True(vm.IsMaximized.Value);
        Assert.True(vm.IsUIShown.Value);
        Assert.True(Settings.WindowProperties.Maximized);
        Assert.False(Settings.WindowProperties.Fullscreen);
    }

    [AvaloniaFact]
    public async Task ToggleMaximized_UpdatesTheTitleBarButtons()
    {
        var (switcher, _, vm, _) = CreateWindow();

        await switcher.ToggleMaximized();

        Assert.True(vm.ShouldRestoreBeShown.Value);
        Assert.False(vm.ShouldMaximizeBeShown.Value);
    }

    [AvaloniaFact]
    public void Start_RememberedMode_IsAppliedWithoutSaving()
    {
        Settings.WindowProperties.Fullscreen = true;
        var (switcher, adapter, _, _) = CreateWindow();

        switcher.Start(isAppStart: true);

        Assert.Equal([WindowMode.Fullscreen], adapter.Entered);
        Assert.Equal(0, _saves);
    }

    [AvaloniaFact]
    public void Start_WindowOpenedWhileRunning_NeverStartsFullscreen()
    {
        Settings.WindowProperties.Fullscreen = true;
        var (switcher, adapter, vm, _) = CreateWindow();

        switcher.Start(isAppStart: false);

        Assert.Empty(adapter.Entered);
        Assert.Equal(WindowMode.Normal, vm.WindowModeState.Value.Mode);
    }

    [AvaloniaFact]
    public async Task OnNativeState_MinimizedAndRestored_GivesBackTheModeWithoutSaving()
    {
        Settings.WindowProperties.Maximized = true;
        var (switcher, adapter, vm, _) = CreateWindow();
        switcher.Start(isAppStart: true);

        await switcher.OnNativeStateAsync(WindowState.Minimized);
        Assert.Equal(WindowMode.Minimized, vm.WindowModeState.Value.Mode);

        await switcher.OnNativeStateAsync(WindowState.Normal);

        Assert.Equal([WindowMode.Maximized, WindowMode.Maximized], adapter.Entered);
        Assert.Equal(WindowMode.Maximized, vm.WindowModeState.Value.Mode);
        Assert.Equal(0, _saves);
    }

    [AvaloniaFact]
    public async Task OnNativeState_FullscreenFromTheOperatingSystem_IsSaved()
    {
        var (switcher, _, vm, _) = CreateWindow();

        await switcher.OnNativeStateAsync(WindowState.FullScreen);

        Assert.True(vm.IsFullscreen.Value);
        Assert.True(Settings.WindowProperties.Fullscreen);
        Assert.Equal(1, _saves);
    }

    [AvaloniaFact]
    public async Task Slideshow_FromMaximized_IsNotSavedAndGivesBackMaximized()
    {
        Settings.WindowProperties.Maximized = true;
        var (switcher, _, vm, _) = CreateWindow();
        switcher.Start(isAppStart: true);

        await switcher.EnterSlideshow();
        Assert.True(vm.IsFullscreen.Value);
        await switcher.LeaveSlideshow();

        Assert.True(vm.IsMaximized.Value);
        Assert.False(Settings.WindowProperties.Fullscreen);
        Assert.Equal(0, _saves);
    }

    [AvaloniaFact]
    public async Task SetAutoFit_NormalWindow_FollowsItsContentAndIsSaved()
    {
        var (switcher, _, vm, window) = CreateWindow();

        await switcher.SetAutoFit(true);

        Assert.True(vm.IsAutoFit.Value);
        Assert.Equal(SizeToContent.WidthAndHeight, window.SizeToContent);
        Assert.True(Settings.WindowProperties.AutoFit);
        Assert.Equal(1, _saves);
    }

    [AvaloniaFact]
    public async Task SetAutoFit_WhileMaximized_AppliesOnceTheWindowIsNormalAgain()
    {
        Settings.WindowProperties.Maximized = true;
        var (switcher, _, _, window) = CreateWindow();
        switcher.Start(isAppStart: true);

        await switcher.SetAutoFit(true);
        Assert.Equal(SizeToContent.Manual, window.SizeToContent);

        await switcher.LeaveTopMode();

        Assert.Equal(SizeToContent.WidthAndHeight, window.SizeToContent);
    }

    [AvaloniaFact]
    public async Task ResizedByHand_TurnsAutoFitOffForOnlyThatWindow()
    {
        Settings.WindowProperties.AutoFit = true;
        var (resized, _, resizedVm, resizedWindow) = CreateWindow();
        var (_, _, otherVm, _) = CreateWindow();

        await resized.ResizedByHand();

        Assert.False(resizedVm.IsAutoFit.Value);
        Assert.Equal(SizeToContent.Manual, resizedWindow.SizeToContent);
        Assert.True(otherVm.IsAutoFit.Value);
        Assert.False(Settings.WindowProperties.AutoFit);
    }

    [AvaloniaFact]
    public async Task Remembered_IsTheLastChangeInAnyWindow()
    {
        var (first, _, _, _) = CreateWindow();
        var (second, _, _, _) = CreateWindow();

        await first.ToggleFullscreen();
        await second.ToggleMaximized();

        Assert.False(Settings.WindowProperties.Fullscreen);
        Assert.True(Settings.WindowProperties.Maximized);
    }

    private (WindowModeSwitcher Switcher, RecordingAdapter Adapter, MainWindowViewModel Vm, MainWindow Window)
        CreateWindow()
    {
        var adapter = new RecordingAdapter();
        var window = new TestMainWindow(adapter);
        var vm = new MainWindowViewModel(_core, _core.Translation, window, _core.GlobalSettings!,
            _core.GallerySettings);
        window.DataContext = vm;
        var switcher = new WindowModeSwitcher(window, adapter, () =>
        {
            _saves++;
            return ValueTask.FromResult(true);
        });
        return (switcher, adapter, vm, window);
    }

    private sealed class RecordingAdapter : IWindowModeAdapter
    {
        public List<WindowMode> Entered { get; } = [];

        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state)
        {
            Entered.Add(state.Mode);
            WindowModeSwitcher.SetNativeState(window, state.Mode);
        }
    }
}
