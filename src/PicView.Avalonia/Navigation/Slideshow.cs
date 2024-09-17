using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.Navigation;
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
            WindowHelper.Restore(vm, Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);
            if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                WindowHelper.CenterWindowOnScreen();
            }
        }
        
        _timer.Stop();
        _timer = null;
        vm.PlatformService.EnableScreensaver();
    }
    
    static bool InitiateAndStart(MainViewModel vm)
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
                await vm.ImageIterator.NextIteration(NavigateTo.Next);
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
            await WindowHelper.ToggleFullscreen(vm, false);
        }
        // TODO: add animation
        await vm.ImageIterator.NextIteration(NavigateTo.Next);
    }
}