using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Sizing;

namespace PicView.Avalonia.Win32.WindowImpl;

public static class Win32Window
{
    public static bool IsChangingWindowState { get; private set; }

    public static async Task Fullscreen(Window window, MainViewModel vm, bool saveSettings = true)
    {
        // Need to set changing state to true, to prevent image resize subscription from firing
        IsChangingWindowState = true;

        // Save window size, so that restoring it will return to the same size and position
        WindowResizing.SaveSize(window);

        MenuManager.CloseMenus(vm);
        
        // Update settings
        Settings.WindowProperties.Fullscreen = true;

        // Update view model properties
        vm.MainWindow.SizeToContent.Value = SizeToContent.Manual;
        vm.MainWindow.IsFullscreen.Value = true;
        vm.MainWindow.IsMaximized.Value = false;
        vm.MainWindow.CanResize.Value = false;
        
        // Hide interface in fullscreen
        HideInterface(vm);
        
        // Gallery needs to take up all space 
        vm.PicViewer.GalleryWidth.Value = double.NaN;

        // Apply fullscreen state
        await Dispatcher.UIThread.InvokeAsync(() => window.WindowState = WindowState.FullScreen, DispatcherPriority.Send);

        var size = WindowResizing.GetSize(vm);
        if (size.HasValue)
        {
            Dispatcher.UIThread.Post(() => WindowResizing.SetSize(size.Value, vm),  DispatcherPriority.Send);
        }

        // Reset changing state flag so subscription can fire again. Need to be delayed by dispatcher to not be misfired. 
        Dispatcher.UIThread.Post(() => IsChangingWindowState = false, DispatcherPriority.SystemIdle);

        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Maximizes the window
    /// </summary>
    public static async Task Maximize(Window window, MainViewModel vm, bool saveSettings = true)
    {
        IsChangingWindowState = true;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (Settings.WindowProperties.AutoFit)
            {
                vm.MainWindow.SizeToContent.Value = SizeToContent.Manual;
            }

            // Save window size, so that restoring it will return to the same size and position
            WindowResizing.SaveSize(window);

            window.WindowState = WindowState.Maximized;
            Settings.WindowProperties.Maximized = true;
            WindowResizing.SetSize(vm);
        });

        vm.MainWindow.IsMaximized.Value = true;
        vm.MainWindow.IsFullscreen.Value = false;
        vm.MainWindow.CanResize.Value = false;

        Dispatcher.UIThread.Post(() => IsChangingWindowState = false, DispatcherPriority.SystemIdle);

        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }

    public static async Task Restore(Window window, MainViewModel vm, bool saveSettings = true)
    {
        IsChangingWindowState = true;

        // Update settings
        Settings.WindowProperties.Maximized = false;
        Settings.WindowProperties.Fullscreen = false;

        // Update UI state
        vm.MainWindow.IsMaximized.Value = false;
        vm.MainWindow.IsFullscreen.Value = false;

        RestoreInterface(vm);

        // Update window state
        await Dispatcher.UIThread.InvokeAsync(() => window.WindowState = WindowState.Normal, DispatcherPriority.Send);
        
        if (Settings.WindowProperties.AutoFit)
        {
            vm.MainWindow.SizeToContent.Value = SizeToContent.WidthAndHeight;
            vm.MainWindow.CanResize.Value = false;
            vm.GlobalSettings.IsAutoFit.Value = true;
            if (Settings.WindowProperties.KeepCentered)
            {
                WindowFunctions.CenterWindowOnScreen();
            }
            else
            {
                WindowFunctions.InitializeWindowSizeAndPosition(window);
            }

            await WindowFunctions.ResizeAndFixRenderingError(vm); // Fixes incorrect render size
        }
        else
        {
            vm.MainWindow.SizeToContent.Value = SizeToContent.Manual;
            vm.MainWindow.CanResize.Value = true;
            WindowFunctions.InitializeWindowSizeAndPosition(window);
        }

        await WindowResizing.SetSizeAsync(vm);

        Dispatcher.UIThread.Post(() => IsChangingWindowState = false, DispatcherPriority.SystemIdle);


        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }

    public static async Task ToggleFullscreen(Window window, MainViewModel vm, bool saveSettings = true)
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

    public static async Task ToggleMaximize(Window window, MainViewModel vm, bool saveSettings = true)
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


    #region Helpers

    /// <summary>
    /// Restores the interface based on settings
    /// </summary>
    private static void RestoreInterface(MainViewModel vm)
    {
        vm.MainWindow.IsUIShown.Value = Settings.UIProperties.ShowInterface;

        if (!Settings.UIProperties.ShowInterface)
        {
            return;
        }

        vm.MainWindow.IsTopToolbarShown.Value = true;
        vm.MainWindow.TitlebarHeight.Value = SizeDefaults.MainTitlebarHeight;

        if (!Settings.UIProperties.ShowBottomNavBar)
        {
            return;
        }

        vm.MainWindow.IsBottomToolbarShown.Value = true;
        vm.MainWindow.BottombarHeight.Value = SizeDefaults.BottombarHeight;
    }

    /// <summary>
    /// Hides interface elements for fullscreen mode
    /// </summary>
    private static void HideInterface(MainViewModel vm)
    {
        vm.MainWindow.IsTopToolbarShown.Value = false;
        vm.MainWindow.IsBottomToolbarShown.Value = false;
        vm.MainWindow.IsUIShown.Value = false;
    }

    #endregion Helpers
}