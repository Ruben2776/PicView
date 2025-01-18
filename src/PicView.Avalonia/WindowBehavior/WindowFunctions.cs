using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.Input;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.ArchiveHandling;
using PicView.Core.Calculations;
using PicView.Core.Config;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.WindowBehavior;

public static class WindowFunctions
{
    public static async Task WindowClosingBehavior()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        await WindowClosingBehavior(desktop.MainWindow);
    }
    public static async Task WindowClosingBehavior(Window window)
    {
        WindowResizing.SaveSize(window);

        if (Dispatcher.UIThread.CheckAccess())
        {
            window.Hide();
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(window.Hide);
        }

        var vm = window.DataContext as MainViewModel;
        string lastFile;
        if (NavigationHelper.CanNavigate(vm))
        {
            if (!string.IsNullOrEmpty(ArchiveExtraction.LastOpenedArchive))
            {
                lastFile = ArchiveExtraction.LastOpenedArchive;
            }
            else
            {
                lastFile = vm?.FileInfo?.FullName ?? FileHistoryNavigation.GetLastFile();
            }
        }
        else
        {
            var url = vm?.Title.GetURL();
            lastFile = !string.IsNullOrWhiteSpace(url) ? url : FileHistoryNavigation.GetLastFile();
        }

        SettingsHelper.Settings.StartUp.LastFile = lastFile;
        await SettingsHelper.SaveSettingsAsync();
        await KeybindingManager.UpdateKeyBindingsFile(); // Save keybindings
        FileDeletionHelper.DeleteTempFiles();
        FileHistoryNavigation.WriteToFile();
        ArchiveExtraction.Cleanup();
        Environment.Exit(0);
    }

    #region Window State

    public static void ShowMinimizedWindow(Window window)
    {
        window.BringIntoView();
        window.WindowState = WindowState.Normal;
        window.Activate();
        window.Focus();
    }

    public static async Task ToggleTopMost(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (SettingsHelper.Settings.WindowProperties.TopMost)
        {
            vm.IsTopMost = false;
            desktop.MainWindow.Topmost = false;
            SettingsHelper.Settings.WindowProperties.TopMost = false;
        }
        else
        {
            vm.IsTopMost = true;
            desktop.MainWindow.Topmost = true;
            SettingsHelper.Settings.WindowProperties.TopMost = true;
        }

        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task ToggleAutoFit(MainViewModel vm)
    {
        if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            vm.SizeToContent = SizeToContent.Manual;
            vm.CanResize = true;
            SettingsHelper.Settings.WindowProperties.AutoFit = false;
            vm.IsAutoFit = false;
        }
        else
        {
            vm.SizeToContent = SizeToContent.WidthAndHeight;
            vm.CanResize = false;
            SettingsHelper.Settings.WindowProperties.AutoFit = true;
            vm.IsAutoFit = true;
        }

        WindowResizing.SetSize(vm);
        await Dispatcher.UIThread.InvokeAsync(() => CenterWindowOnScreen(false));
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task AutoFitAndStretch(MainViewModel vm)
    {
        if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            vm.SizeToContent = SizeToContent.Manual;
            vm.CanResize = true;
            SettingsHelper.Settings.WindowProperties.AutoFit = false;
            SettingsHelper.Settings.ImageScaling.StretchImage = false;
            vm.IsStretched = false;
            vm.IsAutoFit = false;
        }
        else
        {
            vm.SizeToContent = SizeToContent.WidthAndHeight;
            vm.CanResize = false;
            SettingsHelper.Settings.WindowProperties.AutoFit = true;
            SettingsHelper.Settings.ImageScaling.StretchImage = true;
            vm.IsAutoFit = true;
            vm.IsStretched = true;
        }

        WindowResizing.SetSize(vm);
        await Dispatcher.UIThread.InvokeAsync(() => CenterWindowOnScreen(false));
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task NormalWindow(MainViewModel vm)
    {
        vm.SizeToContent = SizeToContent.Manual;
        vm.CanResize = true;
        SettingsHelper.Settings.WindowProperties.AutoFit = false;
        WindowResizing.SetSize(vm);
        vm.ImageViewer.MainImage.InvalidateVisual();
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task NormalWindowStretch(MainViewModel vm)
    {
        vm.SizeToContent = SizeToContent.Manual;
        vm.CanResize = true;
        SettingsHelper.Settings.WindowProperties.AutoFit = false;
        SettingsHelper.Settings.ImageScaling.StretchImage = true;
        vm.IsStretched = true;
        WindowResizing.SetSize(vm);
        vm.ImageViewer.MainImage.InvalidateVisual();
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task Stretch(MainViewModel vm)
    {
        SettingsHelper.Settings.ImageScaling.StretchImage = true;
        vm.IsStretched = true;
        WindowResizing.SetSize(vm);
        vm.ImageViewer.MainImage.InvalidateVisual();
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task ToggleFullscreen(MainViewModel vm, bool saveSettings = true)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (SettingsHelper.Settings.WindowProperties.Fullscreen)
        {
            vm.IsFullscreen = false;
            await Dispatcher.UIThread.InvokeAsync(() =>
                desktop.MainWindow.WindowState = WindowState.Normal);
            if (saveSettings)
            {
                SettingsHelper.Settings.WindowProperties.Fullscreen = false;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowResizing.RestoreSize(desktop.MainWindow);
                Restore(vm, desktop);
            }
        }
        else
        {
            WindowResizing.SaveSize(desktop.MainWindow);
            Fullscreen(vm, desktop);
            if (saveSettings)
            {
                SettingsHelper.Settings.WindowProperties.Fullscreen = true;
            }
        }

        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task MaximizeRestore()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var vm = desktop.MainWindow.DataContext as MainViewModel;
        // Restore
        if (desktop.MainWindow.WindowState is WindowState.Maximized or WindowState.FullScreen)
        {
            Restore(vm, desktop);
        }
        // Maximize
        else
        {
            if (!SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                WindowResizing.SaveSize(desktop.MainWindow);
            }

            Maximize();
        }

        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    public static void Restore(MainViewModel vm, IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (SettingsHelper.Settings.UIProperties.ShowInterface)
            {
                vm.IsTopToolbarShown = true;
                vm.TitlebarHeight = SizeDefaults.TitlebarHeight;
                if (SettingsHelper.Settings.UIProperties.ShowBottomNavBar)
                {
                    vm.IsBottomToolbarShown = true;
                    vm.BottombarHeight = SizeDefaults.BottombarHeight;
                }

                vm.IsUIShown = true;
            }
        }

        Dispatcher.UIThread.InvokeAsync(() =>
            desktop.MainWindow.WindowState = WindowState.Normal);
        SettingsHelper.Settings.WindowProperties.Maximized = false;
        SettingsHelper.Settings.WindowProperties.Fullscreen = false;
        vm.IsUIShown = SettingsHelper.Settings.UIProperties.ShowInterface;
        InitializeWindowSizeAndPosition(desktop.MainWindow);
        WindowResizing.SetSize(vm);
        if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            vm.SizeToContent = SizeToContent.WidthAndHeight;
            vm.CanResize = false;
            CenterWindowOnScreen();
        }
        else
        {
            vm.SizeToContent = SizeToContent.Manual;
            vm.CanResize = true;
        }
    }

    public static void Maximize()
    {
        // TODO: Fix incorrect size for bottom button bar

        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Set);
        }

        return;

        void Set()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            var vm = desktop.MainWindow.DataContext as MainViewModel;
            if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                vm.SizeToContent = SizeToContent.Manual;
                vm.CanResize = true;
            }

            desktop.MainWindow.WindowState = WindowState.Maximized;
            WindowResizing.SetSize(desktop.MainWindow.DataContext as MainViewModel);
            SettingsHelper.Settings.WindowProperties.Maximized = true;
        }
    }

    public static void Fullscreen(MainViewModel vm, IClassicDesktopStyleApplicationLifetime desktop)
    {
        vm.SizeToContent = SizeToContent.Manual;
        vm.IsFullscreen = true;
        vm.CanResize = false;
        if (Dispatcher.UIThread.CheckAccess())
        {
            desktop.MainWindow.WindowState = WindowState.FullScreen;
        }
        else
        {
            Dispatcher.UIThread.Invoke(() => desktop.MainWindow.WindowState = WindowState.FullScreen);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            vm.IsTopToolbarShown = false; // Hide interface in fullscreen. Remember to turn back when exiting fullscreen
            vm.IsBottomToolbarShown = false;
            vm.IsUIShown = false;
            Dispatcher.UIThread.Post(() =>
            {
                // Get the screen that the window is currently on
                var window = desktop.MainWindow;
                var screens = desktop.MainWindow.Screens;
                var screen = screens.ScreenFromVisual(window);

                if (screen == null)
                {
                    return; // No screen found (edge case)
                }

                // Get the scaling factor of the screen (DPI scaling)
                var scalingFactor = screen.Scaling;

                // Get the current screen's bounds (in physical pixels, not adjusted for scaling)
                var screenBounds = screen.Bounds;

                // Calculate the actual bounds in logical units (adjusting for scaling)
                var screenWidth = screenBounds.Width / scalingFactor;
                var screenHeight = screenBounds.Height / scalingFactor;

                // Get the size of the window
                var windowSize = window.ClientSize;

                // Calculate the position to center the window on the screen
                var centeredX = screenBounds.X + (screenWidth - windowSize.Width) / 2;
                var centeredY = screenBounds.Y + (screenHeight - windowSize.Height) / 2;

                // Set the window's new position
                window.Position = new PixelPoint((int)centeredX, (int)centeredY);
            });
            // TODO: Add Fullscreen mode for Windows (and maybe for Linux?)
            // macOS fullscreen version already works nicely
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                // TODO go to macOS fullscreen mode when auto fit is on
            }
        }

        vm.GalleryWidth = double.NaN;
    }

    public static async Task Minimize()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
            desktop.MainWindow.WindowState = WindowState.Minimized);
    }

    public static async Task Close()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
            desktop.MainWindow.Close());
    }

    #endregion

    #region Window Size and Position

    public static void CenterWindowOnScreen(bool horizontal = true)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            var window = desktop.MainWindow;

            // Get the screen that the window is currently on
            var screens = window.Screens;
            var screen = screens.ScreenFromVisual(window);

            if (screen == null)
            {
                return; // No screen found (edge case)
            }

            // Get the scaling factor of the screen (DPI scaling)
            var scalingFactor = screen.Scaling;

            // Get the current screen's bounds (in physical pixels, not adjusted for scaling)
            var screenBounds = screen.WorkingArea;

            // Calculate the actual bounds in logical units (adjusting for scaling)
            var screenWidth = screenBounds.Width / scalingFactor;
            var screenHeight = screenBounds.Height / scalingFactor;

            // Get the size of the window
            var windowSize = window.ClientSize;

            // Calculate the position to center the window on the screen
            var centeredX = screenBounds.X + (screenWidth - windowSize.Width) / 2;
            var centeredY = screenBounds.Y + (screenHeight - windowSize.Height) / 2;

            // Set the window's new position
            window.Position = horizontal
                ? new PixelPoint((int)centeredX, (int)centeredY)
                : new PixelPoint(window.Position.X, (int)centeredY);
        });
    }

    public static void InitializeWindowSizeAndPosition(Window window)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            window.Position = new PixelPoint((int)SettingsHelper.Settings.WindowProperties.Left,
                (int)SettingsHelper.Settings.WindowProperties.Top);
            window.Width = SettingsHelper.Settings.WindowProperties.Width;
            window.Height = SettingsHelper.Settings.WindowProperties.Height;
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                window.Position = new PixelPoint((int)SettingsHelper.Settings.WindowProperties.Left,
                    (int)SettingsHelper.Settings.WindowProperties.Top);
                window.Width = SettingsHelper.Settings.WindowProperties.Width;
                window.Height = SettingsHelper.Settings.WindowProperties.Height;
            });
        }
    }

    #endregion

    #region Window Drag and Behavior

    public static void WindowDragAndDoubleClickBehavior(Window window, PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2 && e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
        {
            _ = MaximizeRestore();
            return;
        }

        var currentScreen = ScreenHelper.ScreenSize;
        window.BeginMoveDrag(e);
        var screen = window.Screens.ScreenFromVisual(window);
        if (screen != null)
        {
            if (screen.WorkingArea.Width != currentScreen.WorkingAreaWidth ||
                screen.WorkingArea.Height != currentScreen.WorkingAreaHeight || screen.Scaling != currentScreen.Scaling)
            {
                ScreenHelper.UpdateScreenSize(window);
                WindowResizing.SetSize(window.DataContext as MainViewModel);
            }
        }
    }

    public static void WindowDragBehavior(Window window, PointerPressedEventArgs e)
    {
        var currentScreen = ScreenHelper.ScreenSize;
        window.BeginMoveDrag(e);
        var screen = window.Screens.ScreenFromVisual(window);
        if (screen != null)
        {
            if (screen.WorkingArea.Width != currentScreen.WorkingAreaWidth ||
                screen.WorkingArea.Height != currentScreen.WorkingAreaHeight || screen.Scaling != currentScreen.Scaling)
            {
                ScreenHelper.UpdateScreenSize(window);
                WindowResizing.SetSize(window.DataContext as MainViewModel);
            }
        }
    }

    #endregion
}