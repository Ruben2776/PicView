using PicView.Avalonia.Gallery;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Gallery;
using Timer = System.Timers.Timer;

namespace PicView.Avalonia.Navigation;

public static class Slideshow
{
    public static bool IsRunning => _timer is not null && _timer.Enabled;
    
    private static Timer? _timer;
    public static async Task StartSlideshow(MainViewModel vm)
    {
        if (!InitiateAndStart(vm))
        {
            return;
        }
        
        await Start(vm, TimeSpan.FromSeconds(Settings.UIProperties.SlideShowTimer).TotalMilliseconds);
    }

    public static async Task StartSlideshow(MainViewModel vm, int milliseconds)
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
    
    public static void StopSlideshow(MainViewModel vm)
    {
        if (_timer is null)
        {
            return;
        }

        if (!Settings.WindowProperties.Fullscreen)
        {
            vm.PlatformWindowService.Restore();
            if (Settings.WindowProperties.AutoFit)
            {
                WindowFunctions.CenterWindowOnScreen();
            }
        }

        if (Settings.Gallery.IsBottomGalleryShown)
        {
            vm.Gallery.GalleryMode.Value = GalleryMode.ClosedToBottom;
        }
        
        _timer.Stop();
        _timer = null;
        vm.PlatformService.EnableScreensaver();
    }

    private static bool InitiateAndStart(MainViewModel vm)
    {
        if (!NavigationManager.CanNavigate(vm))
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
                await NavigationManager.Navigate(true, vm, CancellationToken.None).ConfigureAwait(false);
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

    private static async Task Start(MainViewModel vm, double seconds)
    {
        _timer.Interval = seconds;
        _timer.Start();
        vm.PlatformService.DisableScreensaver();

        MenuManager.CloseMenus(vm);

        if (!Settings.WindowProperties.Fullscreen)
        {
            await vm.PlatformWindowService.ToggleFullscreen();
            Settings.WindowProperties.Fullscreen = false;
        }

        if (GalleryFunctions.IsFullGalleryOpen || Settings.Gallery.IsBottomGalleryShown)
        {
            vm.Gallery.GalleryMode.Value = GalleryMode.BottomToClosed;
        }
    }
}