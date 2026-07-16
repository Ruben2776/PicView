using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.Win32.Views;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Win32.WindowImpl;

public static class Win32Window
{
    public static async Task Fullscreen(MainWindow window, MainWindowViewModel vm, bool saveSettings = true)
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

    /// <summary>
    /// Maximizes the window
    /// </summary>
    public static async Task Maximize(MainWindow window, MainWindowViewModel vm, bool saveSettings = true)
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

    public static async Task Restore(MainWindow window, MainWindowViewModel vm, bool saveSettings = true)
    {
        window.IsChangingWindowState = true;
        
        var wasFullscreen = window.WindowState == WindowState.FullScreen || Settings.WindowProperties.Fullscreen;
        
        if (Settings.WindowProperties.AutoFit)
        {
            window.SizeToContent = SizeToContent.WidthAndHeight;
        }
        
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
            window.IsChangingWindowState = false;
            if (Settings.WindowProperties.AutoFit)
            {
                window.SizeToContent = SizeToContent.WidthAndHeight;
            }
        }, DispatcherPriority.SystemIdle);
        
        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }

    public static async Task ToggleFullscreen(MainWindow window, MainWindowViewModel vm, bool saveSettings = true)
    {
        if (Settings.WindowProperties.Fullscreen)
        {
            await Restore(window, vm, saveSettings);
        }
        else
        {
            await Fullscreen(window, vm, saveSettings);
        }

        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }

    public static async Task ToggleMaximize(MainWindow window, MainWindowViewModel vm, bool saveSettings = true)
    {
        if (Settings.WindowProperties.Maximized)
        {
            await Restore(window, vm, saveSettings);
        }
        else
        {
            await Maximize(window, vm, saveSettings);
        }

        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }
    
    public static void Minimize(WinMainWindow? mainWindow)
    {
        mainWindow.WindowState = WindowState.Minimized;
    }
}