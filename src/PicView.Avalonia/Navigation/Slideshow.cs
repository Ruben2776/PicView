using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;
using Timer = System.Timers.Timer;

namespace PicView.Avalonia.Navigation;

public static class Slideshow
{
    public static bool IsRunning { get; private set; }
    
    private static Timer? _timer;
    private static IDisposable? _leavingFullscreen;
    public static async Task StartSlideshow(MainWindowViewModel vm)
    {
        if (!InitiateAndStart(vm))
        {
            return;
        }
        
        await Start(vm, TimeSpan.FromSeconds(Settings.UIProperties.SlideShowTimer).TotalMilliseconds);
    }

    public static async Task StartSlideshow(MainWindowViewModel vm, int milliseconds)
    {
        if (!InitiateAndStart(vm))
        {
            return;
        }

        if (milliseconds <= 0)
        {
            await Start(vm, TimeSpan.FromSeconds(Settings.UIProperties.SlideShowTimer).TotalMilliseconds);
        }
        else
        {
            await Start(vm, milliseconds);
        }
    }
    
    public static void StopSlideshow(MainWindowViewModel vm)
    {
        IsRunning = false;
        _leavingFullscreen?.Dispose();
        _leavingFullscreen = null;

        if (_timer is null)
        {
            return;
        }

        _ = LeaveSlideshowAsync(vm);
        
        _timer.Stop();
        _timer.Dispose();
        _timer = null;
        if (Application.Current.DataContext is CoreViewModel core)
        {
            core.PlatformService.EnableScreensaver();
        }
    }

    private static bool InitiateAndStart(MainWindowViewModel vm)
    {
        if (!vm.WindowTabs.ActiveTab.CurrentValue.CanNavigateForwards.CurrentValue)
        {
            return false;
        }
        
        if (_timer is null)
        {
            _timer = new Timer
            {
                Enabled = true,
            };
            _timer.Elapsed += async (_, _) =>
            {
                // TODO: add animation
                // E.g. https://codepen.io/arrive/pen/EOGyzK
                // https://docs.avaloniaui.net/docs/guides/graphics-and-animation/page-transitions/how-to-create-a-custom-page-transition
                // https://docs.avaloniaui.net/docs/guides/graphics-and-animation/page-transitions/page-slide-transition
                // https://docs.avaloniaui.net/docs/reference/controls/transitioningcontentcontrol
                await vm.WindowTabs.NextFile();
            };
        }
        else if (_timer.Enabled)
        {
            // Starting it again while it runs stops it, unless the key is only being held down
            if (!IsKeyHeldDown(vm))
            {
                StopSlideshow(vm);
            }

            return false;
        }
        
        return true;
    }

    private static bool IsKeyHeldDown(MainWindowViewModel vm) =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
        desktop.Windows.OfType<MainWindow>().FirstOrDefault(window => window.DataContext == vm)?.KeybindRouter
            .IsKeyHeldDown is true;

    private static async Task Start(MainWindowViewModel vm, double seconds)
    {
        _timer.Interval = seconds;
        _timer.Start();
        IsRunning = true;
        if (Application.Current.DataContext is CoreViewModel core)
        {
            core.PlatformService.DisableScreensaver();
        }

        if (vm.WindowModeSwitcher is not null)
        {
            await vm.WindowModeSwitcher.EnterSlideshow();
        }

        // Leaving the slideshow's fullscreen any other way, as by toggling fullscreen, ends the slideshow too
        _leavingFullscreen?.Dispose();
        _leavingFullscreen = vm.WindowModeState
            .Where(static state => state.Mode is not WindowMode.Fullscreen)
            .Take(1)
            .Subscribe(_ => Dispatcher.UIThread.Post(() => StopSlideshow(vm)));
    }

    private static async Task LeaveSlideshowAsync(MainWindowViewModel vm)
    {
        if (vm.WindowModeSwitcher is null)
        {
            return;
        }

        var before = vm.WindowModeState.CurrentValue.Mode;
        await vm.WindowModeSwitcher.LeaveSlideshow();
        if (before is not WindowMode.Normal &&
            vm.WindowModeState.CurrentValue is { Mode: WindowMode.Normal, AutoFit: true })
        {
            if (vm.PlatformWindowService is MainWindow window)
            {
                WindowFunctions.CenterWindowOnScreen(window);
            }
        }
    }
}