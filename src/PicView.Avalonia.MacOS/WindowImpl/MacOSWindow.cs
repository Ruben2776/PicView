using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.MacOS.WindowImpl;

public static class MacOSWindow
{
    public static async Task ToggleFullscreen(MacMainWindow window, MainWindowViewModel? vm, bool saveSettings)
    {
        if (Settings.WindowProperties.Fullscreen)
        {
            Settings.WindowProperties.Fullscreen = false;
            await Restore(window, vm, saveSettings);
        }
        else
        {
            await Fullscreen(window, vm, saveSettings);
        }
    }
    
    public static async Task ToggleMaximize(MacMainWindow window, MainWindowViewModel? vm, bool saveSettings = true)
    {
        if (window.WindowState == WindowState.Maximized || Settings.WindowProperties.Maximized)
        {
            Settings.WindowProperties.Maximized = false;
            await Restore(window, vm, saveSettings); 
        }
        else
        {
            await Maximize(window, vm, saveSettings);
        }
    }

    public static async Task Restore(MacMainWindow window, MainWindowViewModel vm, bool saveSettings = true)
    {
        window.IsChangingWindowState = true;
        
        var wasFullscreen = window.WindowState == WindowState.FullScreen || Settings.WindowProperties.Fullscreen;
        
        if (Settings.WindowProperties.AutoFit)
        {
            window.SizeToContent = SizeToContent.WidthAndHeight;
        }
        
        window.ExtendClientAreaToDecorationsHint = false;
        
        // Update settings
        Settings.WindowProperties.Maximized = false;
        Settings.WindowProperties.Fullscreen = false;
        
        // Update UI state
        vm.IsMaximized.Value = false;
        vm.IsFullscreen.Value = false;
        
        vm.ShouldMaximizeBeShown.Value = true;
        vm.ShouldRestoreBeShown.Value = false;

        if (wasFullscreen)
        {
            ToggleUIVisibility.RestoreInterface(vm);
        }
        
        window.WindowState = WindowState.Normal;
        
        WindowResizing.SetSize(window, WindowResizeReason.Application);
        
        Dispatcher.UIThread.Post(() =>
        {
            // Disabling ExtendClientAreaToDecorationsHint and enabling it again fixes not being able to draw in title bar
            window.ExtendClientAreaToDecorationsHint = true;
            
            if (Settings.WindowProperties.AutoFit)
            {
                window.SizeToContent = SizeToContent.WidthAndHeight;
                WindowResizing.FastCenterWindow(window);
            }
        });
        
        Dispatcher.UIThread.Post(() =>
        {
            window.IsChangingWindowState = false;
        }, DispatcherPriority.SystemIdle);
        
        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }

    public static async Task Fullscreen(MacMainWindow window, MainWindowViewModel? vm, bool saveSettings = true)
    {
        window.IsChangingWindowState = true;

        if (!Slideshow.IsRunning)
        {
            // Don't save the user setting when entering fullscreen from slideshow
            Settings.WindowProperties.Fullscreen = true;
        }

        Settings.WindowProperties.Maximized = false;
        vm.IsMaximized.Value = false;
        vm.IsFullscreen.Value = true;
        
        vm.ShouldMaximizeBeShown.Value = true;
        vm.ShouldRestoreBeShown.Value = true;
        
        vm.WindowMaxWidth.Value = vm.WindowMaxHeight.Value = double.NaN;
        window.SizeToContent = SizeToContent.Manual;
        if (window.WindowState != WindowState.FullScreen)
        {
            window.WindowState = WindowState.FullScreen;
        }
        
        ToggleUIVisibility.HideInterface(vm);
        
        WindowResizing.SetSize(window, WindowResizeReason.Application);
        Dispatcher.UIThread.Post(() => window.IsChangingWindowState = false, DispatcherPriority.SystemIdle);
        
        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }

    public static async Task Maximize(MacMainWindow window, MainWindowViewModel vm, bool saveSettings = true)
    {
        window.IsChangingWindowState = true;
        
        Settings.WindowProperties.Maximized = true;
        vm.IsMaximized.Value = true;
        vm.IsFullscreen.Value = false;
        
        vm.ShouldMaximizeBeShown.Value = false;
        vm.ShouldRestoreBeShown.Value = true;
        
        window.SizeToContent = SizeToContent.Manual;
        vm.WindowMaxWidth.Value = vm.WindowMaxHeight.Value = double.NaN;
        if (window.WindowState != WindowState.Maximized)
        {
            window.WindowState = WindowState.Maximized;
        }
        
        WindowResizing.SetSize(window, WindowResizeReason.Application);
        Dispatcher.UIThread.Post(() => window.IsChangingWindowState = false, DispatcherPriority.SystemIdle);
        
        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }
    
    public static void Minimize(MacMainWindow window)
    {
        window.WindowState = WindowState.Minimized;
    }
}