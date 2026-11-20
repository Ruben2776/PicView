using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Tests.WindowBehavior;

[Collection("Sequential")]
public class WindowModeSwitcherTests
{
    private readonly CoreViewModel _core;

    public WindowModeSwitcherTests()
    {
        SetDefaults();
        TranslationManager.Init();
        _core = new CoreViewModel(null, _ => new ImageModel());
    }

    [AvaloniaFact]
    public void ToggleFullscreen_EntersNativeFullscreenHidesInterface()
    {
        var (switcher, adapter, vm, _) = CreateWindow();

        switcher.ToggleFullscreen();

        Assert.Equal([WindowMode.Fullscreen], adapter.Entered);
        Assert.True(vm.IsFullscreen.Value);
        Assert.False(vm.IsUIShown.Value);
        Assert.False(Settings.WindowProperties.Fullscreen);
    }

    [AvaloniaFact]
    public void ToggleFullscreen_FromMaximized_LeavingGivesBackMaximizedAndTheInterface()
    {
        Settings.WindowProperties.Maximized = true;
        var (switcher, adapter, vm, _) = CreateWindow();
        switcher.Start(isAppStart: true);

        switcher.ToggleFullscreen();
        switcher.ToggleFullscreen();

        Assert.Equal([WindowMode.Maximized, WindowMode.Fullscreen, WindowMode.Maximized], adapter.Entered);
        Assert.True(vm.IsMaximized.Value);
        Assert.True(vm.IsUIShown.Value);
        Assert.True(vm.WindowModeState.CurrentValue.Remembered.Maximized);
        Assert.False(vm.WindowModeState.CurrentValue.Remembered.Fullscreen);
    }

    [AvaloniaFact]
    public void ToggleMaximized_UpdatesTheTitleBarButtons()
    {
        var (switcher, _, vm, _) = CreateWindow();

        switcher.ToggleMaximized();

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
    }

    [AvaloniaFact]
    public void Start_FullscreenAtAppStart_PlacesTheWindowAtItsScreensOriginOnceShown()
    {
        Settings.WindowProperties.Fullscreen = true;
        var (switcher, _, _, window) = CreateWindow();
        window.Position = new PixelPoint(300, 200);

        switcher.Start(isAppStart: true);
        Dispatcher.UIThread.RunJobs();
        var beforeShowing = window.Position;
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Windows leaves the taskbar over a window that went fullscreen while hidden, so the shown window is placed
        // again once idle, at the origin of its screen, which the headless screen has at 0, 0
        Assert.Equal(new PixelPoint(300, 200), beforeShowing);
        Assert.Equal(new PixelPoint(0, 0), window.Position);
    }

    [AvaloniaFact]
    public void Start_Maximized_LeavesTheNormalBoundsInItsWindowSettings()
    {
        Settings.WindowProperties.Maximized = true;
        Settings.WindowProperties.Left = 123;
        Settings.WindowProperties.Top = 45;
        Settings.WindowProperties.Width = 900;
        Settings.WindowProperties.Height = 700;
        var (switcher, _, vm, _) = CreateWindow();

        switcher.Start(isAppStart: true);

        // The window was never given its Normal size, so it has none to remember
        Assert.Equal(WindowMode.Maximized, vm.WindowModeState.Value.Mode);
        Assert.Equal(new NormalBounds(123, 45, 900, 700), vm.WindowSettings.Bounds);
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
    public void OnNativeState_MinimizedAndRestored_GivesBackTheModeWithoutSaving()
    {
        Settings.WindowProperties.Maximized = true;
        var (switcher, adapter, vm, _) = CreateWindow();
        switcher.Start(isAppStart: true);

        switcher.OnNativeStateAsync(WindowState.Minimized);
        Assert.Equal(WindowMode.Minimized, vm.WindowModeState.Value.Mode);

        switcher.OnNativeStateAsync(WindowState.Normal);

        Assert.Equal([WindowMode.Maximized, WindowMode.Maximized], adapter.Entered);
        Assert.Equal(WindowMode.Maximized, vm.WindowModeState.Value.Mode);
    }

    [AvaloniaFact]
    public void LeaveMinimized_AMinimizedWindow_GivesBackTheModeItHadWithoutSaving()
    {
        Settings.WindowProperties.Maximized = true;
        var (switcher, adapter, vm, _) = CreateWindow();
        switcher.Start(isAppStart: true);
        switcher.OnNativeStateAsync(WindowState.Minimized);

        switcher.LeaveMinimized();

        Assert.Equal([WindowMode.Maximized, WindowMode.Maximized], adapter.Entered);
        Assert.Equal(WindowMode.Maximized, vm.WindowModeState.Value.Mode);
    }

    [AvaloniaFact]
    public void LeaveMinimized_AWindowThatIsNotMinimized_IsLeftAsItIs()
    {
        Settings.WindowProperties.Maximized = true;
        var (switcher, adapter, vm, _) = CreateWindow();
        switcher.Start(isAppStart: true);

        switcher.LeaveMinimized();

        Assert.Equal([WindowMode.Maximized], adapter.Entered);
        Assert.Equal(WindowMode.Maximized, vm.WindowModeState.Value.Mode);
    }

    [AvaloniaFact]
    public void OnNativeState_FullscreenFromTheOperatingSystem_IsRemembered()
    {
        var (switcher, _, vm, _) = CreateWindow();

        switcher.OnNativeStateAsync(WindowState.FullScreen);

        Assert.True(vm.IsFullscreen.Value);
        Assert.True(vm.WindowModeState.CurrentValue.Remembered.Fullscreen);
    }

    [AvaloniaFact]
    public void Slideshow_FromMaximized_IsNotSavedAndGivesBackMaximized()
    {
        Settings.WindowProperties.Maximized = true;
        var (switcher, _, vm, _) = CreateWindow();
        switcher.Start(isAppStart: true);

        switcher.EnterSlideshow();
        Assert.True(vm.IsFullscreen.Value);
        switcher.LeaveSlideshow();

        Assert.True(vm.IsMaximized.Value);
        Assert.True(vm.WindowModeState.CurrentValue.Remembered.Maximized);
        Assert.False(vm.WindowModeState.CurrentValue.Remembered.Fullscreen);
    }

    [AvaloniaFact]
    public void SetAutoFit_NormalWindow_FollowsItsContentAndWritesNothing()
    {
        var (switcher, _, vm, window) = CreateWindow();

        switcher.SetAutoFit(true);

        Assert.True(vm.IsAutoFit.Value);
        Assert.Equal(SizeToContent.WidthAndHeight, window.SizeToContent);
        Assert.False(Settings.WindowProperties.AutoFit);
    }

    [AvaloniaFact]
    public void SetAutoFit_WhileMaximized_AppliesOnceTheWindowIsNormalAgain()
    {
        Settings.WindowProperties.Maximized = true;
        var (switcher, _, _, window) = CreateWindow();
        switcher.Start(isAppStart: true);

        switcher.SetAutoFit(true);
        Assert.Equal(SizeToContent.Manual, window.SizeToContent);

        switcher.LeaveTopMode();

        Assert.Equal(SizeToContent.WidthAndHeight, window.SizeToContent);
    }

    [AvaloniaFact]
    public void ResizedByHand_TurnsAutoFitOffForOnlyThatWindow()
    {
        Settings.WindowProperties.AutoFit = true;
        var (resized, _, resizedVm, resizedWindow) = CreateWindow();
        var (_, _, otherVm, _) = CreateWindow();

        resized.ResizedByHand();

        Assert.False(resizedVm.IsAutoFit.Value);
        Assert.Equal(SizeToContent.Manual, resizedWindow.SizeToContent);
        Assert.True(otherVm.IsAutoFit.Value);
        Assert.True(Settings.WindowProperties.AutoFit);
    }

    [AvaloniaFact]
    public void Remembered_StaysInItsWindow()
    {
        var (first, _, _, _) = CreateWindow();
        var (_, _, secondVm, _) = CreateWindow();
        var secondBefore = secondVm.WindowModeState.CurrentValue;

        first.ToggleMaximized();

        Assert.Equal(secondBefore, secondVm.WindowModeState.CurrentValue);
        Assert.False(Settings.WindowProperties.Maximized);
        Assert.False(Settings.WindowProperties.Fullscreen);
    }

    private (WindowModeSwitcher Switcher, RecordingAdapter Adapter, MainWindowViewModel Vm, MainWindow Window)
        CreateWindow()
    {
        var adapter = new RecordingAdapter();
        var window = new TestMainWindow(adapter);
        var vm = new MainWindowViewModel(_core, _core.Translation, _core.GlobalSettings!,
            _core.GallerySettings);
        window.DataContext = vm;
        var switcher = new WindowModeSwitcher(window, adapter);
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
