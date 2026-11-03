using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
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

    // The Main Window the running slideshow was started on, which leaving it centers
    private static MainWindow? _window;

    public static Task StartSlideshow(MainWindow window) => StartSlideshow(window, 0);

    /// <param name="window">The Main Window the slideshow runs in.</param>
    /// <param name="milliseconds">The time per image; zero or less uses the slideshow timer setting.</param>
    public static async Task StartSlideshow(MainWindow window, int milliseconds)
    {
        if (window.ViewModel is not { } vm || !InitiateAndStart(window, vm))
        {
            return;
        }

        _window = window;
        var interval = milliseconds <= 0
            ? TimeSpan.FromSeconds(Settings.UIProperties.SlideShowTimer).TotalMilliseconds
            : milliseconds;
        await Start(vm, interval).ConfigureAwait(false);
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

        _ = LeaveSlideshowAsync(vm, _window);
        _window = null;

        _timer.Stop();
        _timer.Dispose();
        _timer = null;
        if (Application.Current.DataContext is CoreViewModel core)
        {
            core.PlatformService.EnableScreensaver();
        }
    }

    private static bool InitiateAndStart(MainWindow window, MainWindowViewModel vm)
    {
        if (!vm.WindowTabs.ActiveTab.CurrentValue.CanNavigateForwards.Value)
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
                await vm.WindowTabs.NextFile().ConfigureAwait(false);
            };
        }
        else if (_timer.Enabled)
        {
            // Starting it again while it runs stops it, unless the key is only being held down
            if (!window.KeybindRouter.IsKeyHeldDown)
            {
                StopSlideshow(vm);
            }

            return false;
        }

        return true;
    }

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
            await vm.WindowModeSwitcher.EnterSlideshow().ConfigureAwait(false);
        }

        // Leaving the slideshow's fullscreen any other way, as by toggling fullscreen, ends the slideshow too
        _leavingFullscreen?.Dispose();
        _leavingFullscreen = vm.WindowModeState
            .Where(static state => state.Mode is not WindowMode.Fullscreen)
            .Take(1)
            .Subscribe(_ => Dispatcher.UIThread.Post(() => StopSlideshow(vm)));
    }

    private static async Task LeaveSlideshowAsync(MainWindowViewModel vm, MainWindow? window)
    {
        if (vm.WindowModeSwitcher is null)
        {
            return;
        }

        var before = vm.WindowModeState.CurrentValue.Mode;
        await vm.WindowModeSwitcher.LeaveSlideshow().ConfigureAwait(false);
        // Only the window the slideshow was started on, not one whose view model stopped it from elsewhere
        if (before is not WindowMode.Normal && window is not null && window.ViewModel == vm &&
            vm.WindowModeState.CurrentValue is { Mode: WindowMode.Normal, AutoFit: true })
        {
            WindowFunctions.CenterWindowOnScreen(window);
        }
    }
}
