using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
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
        
        await Start(vm, TimeSpan.FromSeconds(SettingsHelper.Settings.UIProperties.SlideShowTimer).TotalMilliseconds);
    }

    public static async Task StartSlideshow(MainViewModel vm, int milliseconds)
    {
        if (!InitiateAndStart(vm))
        {
            return;
        }

        await Start(vm, milliseconds);
    }
    
    public static void StopSlideshow(MainViewModel vm)
    {
        if (_timer is null)
        {
            return;
        }

        if (!SettingsHelper.Settings.WindowProperties.Fullscreen)
        {
            WindowFunctions.Restore(vm, Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);
            if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                WindowFunctions.CenterWindowOnScreen();
            }
        }

        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            vm.GalleryMode = GalleryMode.ClosedToBottom;
        }
        
        _timer.Stop();
        _timer = null;
        vm.PlatformService.EnableScreensaver();
    }

    private static bool InitiateAndStart(MainViewModel vm)
    {
        if (!NavigationHelper.CanNavigate(vm))
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
                await NavigationHelper.Navigate(true, vm).ConfigureAwait(false);
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

        UIHelper.CloseMenus(vm);

        if (!SettingsHelper.Settings.WindowProperties.Fullscreen)
        {
            await WindowFunctions.ToggleFullscreen(vm, false);
        }

        if (GalleryFunctions.IsFullGalleryOpen || SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            vm.GalleryMode = GalleryMode.BottomToClosed;
        }
        
        await NavigationHelper.Navigate(true, vm).ConfigureAwait(false);
    }
}