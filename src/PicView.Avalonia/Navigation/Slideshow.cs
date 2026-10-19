using Avalonia;
using PicView.Avalonia.Input;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using Timer = System.Timers.Timer;

namespace PicView.Avalonia.Navigation;

public static class Slideshow
{
    public static bool IsRunning { get; private set; }
    
    private static Timer? _timer;
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
        
        if (_timer is null)
        {
            return;
        }

        _ = LeaveSlideshowAsync(vm);
        
        _timer.Stop();
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
            if (!MainKeyboardShortcuts.IsKeyHeldDown)
            {
                _timer = null;
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
            await vm.WindowModeSwitcher.EnterSlideshow();
        }
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
            WindowFunctions.CenterWindowOnScreen();
        }
    }
}