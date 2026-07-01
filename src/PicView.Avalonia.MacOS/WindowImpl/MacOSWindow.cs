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
    public static async Task ToggleFullscreen(MacMainWindow? window, MainWindowViewModel? vm, bool saveSettings)
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
    
    public static async Task ToggleMaximize(MacMainWindow? window, MainWindowViewModel? vm, bool saveSettings = true)
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

    public static async Task Restore(MacMainWindow? window, MainWindowViewModel vm, bool saveSettings = true)
    {
        window.IsChangingWindowState = true;
        
        // Update settings
        Settings.WindowProperties.Maximized = false;
        Settings.WindowProperties.Fullscreen = false;
        
        vm.IsAutoFit.Value = Settings.WindowProperties.AutoFit;

        // Update UI state
        vm.IsMaximized.Value = false;
        vm.IsFullscreen.Value = false;
        vm.ShouldMaximizeBeShown.Value = true;
        vm.ShouldRestoreBeShown.Value = false;
        
        // Update window state
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Set the window size back again
            window.Width = 
                window.Height = 
                    window.MainView.Width = 
                        window.MainView.Height = double.NaN;
            window.WindowState = WindowState.Normal;
            window.SizeToContent = Settings.WindowProperties.AutoFit ? SizeToContent.WidthAndHeight : SizeToContent.Manual;
        });

        WindowFunctions.RestoreInterface(vm);
        
        WindowResizing.SetSize(window, WindowResizeReason.Application);
        
        if (Settings.WindowProperties.AutoFit && Settings.WindowProperties.KeepCentered)
        {
            WindowFunctions.CenterWindowOnScreen();
        }
        else if (!Settings.WindowProperties.AutoFit)
        {
            WindowFunctions.InitializeWindowSizeAndPosition(window);
        }
        
        Dispatcher.UIThread.Post(() => window.IsChangingWindowState = false, DispatcherPriority.SystemIdle);

        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }

    public static async Task Fullscreen(MacMainWindow? window, MainWindowViewModel? vm, bool saveSettings = true)
    {
        // Need to set changing state to true, to prevent image resize subscription from firing
        window.IsChangingWindowState = true;
        
        // Save window size, so that restoring it will return to the same size and position
        WindowResizing.SaveSize(window);
        
        Settings.WindowProperties.Maximized = false;
        if (!Slideshow.IsRunning)
        {
            // Don't save the user setting when entering fullscreen from slideshow
            Settings.WindowProperties.Fullscreen = true;
        }
        
        vm.IsTopToolbarShown.Value = false;
        vm.IsBottomToolbarShown.Value = false;
        vm.ShouldMaximizeBeShown.Value = true;
        vm.ShouldRestoreBeShown.Value = true;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            window.WindowState = WindowState.FullScreen;
        });
        
        vm.IsFullscreen.Value = true;
        vm.IsMaximized.Value = false;
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Set the window size to the screen size
            window.Width = ScreenHelper.ScreenSize.Width;
            window.Height = ScreenHelper.ScreenSize.Height;
        });
        
        // Sometimes the window is not centered properly, so center it again
        WindowFunctions.CenterWindowOnScreen(window);
        
        WindowResizing.SetSize(window, WindowResizeReason.Application);
        
        // Reset changing state flag so subscription can fire again. Need to be delayed by dispatcher to not be misfired. 
        Dispatcher.UIThread.Post(() => window.IsChangingWindowState = false, DispatcherPriority.SystemIdle);
        
        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }

    public static async Task Maximize(MacMainWindow? window, MainWindowViewModel vm, bool saveSettings = true)
    {
        window.IsChangingWindowState = true;
        Settings.WindowProperties.Maximized = true;
                    
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Save window size, so that restoring it will return to the same size and position
            WindowResizing.SaveSize(window);

            
            // Use WindowResizing to reset the max size of the window
            WindowResizing.SetSize(window, WindowResizeReason.Application);
            
            // Set the window size to the screen size
            window.Width = ScreenHelper.ScreenSize.WorkingAreaWidth;
            window.Height = ScreenHelper.ScreenSize.WorkingAreaHeight;

            window.WindowState = WindowState.Maximized;
        });

        vm.IsMaximized.Value = true;
        vm.IsFullscreen.Value = false;
        vm.ShouldMaximizeBeShown.Value = false;
        vm.ShouldRestoreBeShown.Value = true;
        
        Dispatcher.UIThread.Post(() => window.IsChangingWindowState = false, DispatcherPriority.SystemIdle);

        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }
    
    public static void Minimize(MacMainWindow? window)
    {
        window.WindowState = WindowState.Minimized;
    }
}