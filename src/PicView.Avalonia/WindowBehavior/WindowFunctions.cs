using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using PicView.Avalonia.Input;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.ArchiveHandling;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.Sizing;

// ReSharper disable CompareOfFloatsByEqualityOperator

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
        string? lastFile;
        if (NavigationManager.CanNavigate(vm))
        {
            if (!string.IsNullOrEmpty(ArchiveExtraction.LastOpenedArchive))
            {
                lastFile = ArchiveExtraction.LastOpenedArchive;
            }
            else
            {
                lastFile = vm?.PicViewer.FileInfo?.CurrentValue.FullName ?? FileHistoryManager.GetLastEntry();
            }
        }
        else
        {
            var url = vm?.PicViewer.Title?.CurrentValue?.GetURL();
            lastFile = !string.IsNullOrWhiteSpace(url) ? url : FileHistoryManager.GetLastEntry();
        }

        Settings.StartUp.LastFile = lastFile ?? "";
        await SaveSettingsAsync();
        await KeybindingManager.UpdateKeyBindingsFile(); // Save keybindings
        TempFileHelper.DeleteTempFiles();
        await FileHistoryManager.SaveToFileAsync();
        ArchiveExtraction.Cleanup();

        if (vm.Window.SettingsWindowConfig is not null)
        {
            await vm.Window.SettingsWindowConfig.SaveAsync();
        }

        if (vm.Window.ImageInfoWindowConfig is not null)
        {
            await vm.Window.ImageInfoWindowConfig.SaveAsync();
        }

        if (vm.Window.BatchResizeWindowConfig is not null)
        {
            await vm.Window.BatchResizeWindowConfig.SaveAsync();
        }

        Environment.Exit(0);
    }

    #region Window State

    /// <summary>
    /// Restores the interface based on settings
    /// </summary>
    public static void RestoreInterface(MainViewModel vm)
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

    public static async Task ResizeAndFixRenderingError(MainViewModel vm)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (Settings.WindowProperties.AutoFit)
            {
                if (vm.PicViewer.PixelWidth.Value > UIHelper.GetMainView.Bounds.Width ||
                    vm.PicViewer.PixelHeight.Value > UIHelper.GetMainView.Bounds.Height)
                {
                    vm.ImageViewer.MainBorder.Height = double.NaN;
                    vm.ImageViewer.MainBorder.Width = double.NaN;

                    WindowResizing.SetSize(1, 1, 0, 0, 0, vm);
                }
                else
                {
                    WindowResizing.SetSize(vm);
                }

                CenterWindowOnScreen(false);
            }
            else
            {
                WindowResizing.SetSize(vm);
            }

            if (Settings.WindowProperties.AutoFit)
            {
                if (Settings.ImageScaling.StretchImage)
                {
                    // Setting horizontal and vertical alignment fixes the rendering error
                    if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        return;
                    }

                    Dispatcher.UIThread.Post(() => WindowResizing.SetSize(vm), DispatcherPriority.Render);
                    desktop.MainWindow.HorizontalAlignment = HorizontalAlignment.Center;
                    desktop.MainWindow.VerticalAlignment = VerticalAlignment.Center;
                }
                else
                {
                    if (vm.PicViewer.PixelWidth.CurrentValue > UIHelper.GetMainView.Bounds.Width ||
                        vm.PicViewer.PixelHeight.CurrentValue > UIHelper.GetMainView.Bounds.Height)
                    {
                        Dispatcher.UIThread.Post(() => WindowResizing.SetSize(vm), DispatcherPriority.Render);
                    }
                }
            }
        }, DispatcherPriority.Send);
        if (Settings.ImageScaling.StretchImage)
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                WindowResizing.SetSize(vm);
                // Reset the horizontal and vertical alignment after fixing the rendering error
                desktop.MainWindow.HorizontalAlignment = HorizontalAlignment.Stretch;
                desktop.MainWindow.VerticalAlignment = VerticalAlignment.Stretch;
            }, DispatcherPriority.Render);
        }
    }

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

        if (Settings.WindowProperties.TopMost)
        {
            vm.GlobalSettings.IsTopMost.Value = false;
            desktop.MainWindow.Topmost = false;
            Settings.WindowProperties.TopMost = false;
        }
        else
        {
            vm.GlobalSettings.IsTopMost.Value = true;
            desktop.MainWindow.Topmost = true;
            Settings.WindowProperties.TopMost = true;
        }

        await SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task ToggleAutoFit(MainViewModel vm)
    {
        if (Settings.WindowProperties.AutoFit)
        {
            vm.MainWindow.SizeToContent.Value = SizeToContent.Manual;
            vm.MainWindow.CanResize.Value = true;
            Settings.WindowProperties.AutoFit = false;
            vm.GlobalSettings.IsAutoFit.Value = false;
        }
        else
        {
            vm.MainWindow.SizeToContent.Value = SizeToContent.WidthAndHeight;
            vm.MainWindow.CanResize.Value = false;
            Settings.WindowProperties.AutoFit = true;
            vm.GlobalSettings.IsAutoFit.Value = true;

            // Fix unpleasant window placement
            Dispatcher.UIThread.Post(() => { CenterWindowOnScreen(); }, DispatcherPriority.Background);
        }

        await ResizeAndFixRenderingError(vm);
        await SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task AutoFitAndStretch(MainViewModel vm)
    {
        if (Settings.WindowProperties.AutoFit)
        {
            vm.MainWindow.SizeToContent.Value = SizeToContent.Manual;
            vm.MainWindow.CanResize.Value = true;
            Settings.WindowProperties.AutoFit = false;
            Settings.ImageScaling.StretchImage = false;
            vm.GlobalSettings.IsStretched.Value = false;
            vm.GlobalSettings.IsAutoFit.Value = false;
        }
        else
        {
            vm.MainWindow.SizeToContent.Value = SizeToContent.WidthAndHeight;
            vm.MainWindow.CanResize.Value = false;
            Settings.WindowProperties.AutoFit = true;
            Settings.ImageScaling.StretchImage = true;
            vm.GlobalSettings.IsAutoFit.Value = true;
            vm.GlobalSettings.IsStretched.Value = true;
        }

        await ResizeAndFixRenderingError(vm);
        await SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task NormalWindow(MainViewModel vm)
    {
        vm.MainWindow.SizeToContent.Value = SizeToContent.Manual;
        vm.MainWindow.CanResize.Value = true;
        Settings.WindowProperties.AutoFit = false;
        await WindowResizing.SetSizeAsync(vm);
        vm.ImageViewer.MainImage.InvalidateVisual();
        await SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task NormalWindowStretch(MainViewModel vm)
    {
        vm.MainWindow.SizeToContent.Value = SizeToContent.Manual;
        vm.MainWindow.CanResize.Value = true;
        Settings.WindowProperties.AutoFit = false;
        Settings.ImageScaling.StretchImage = true;
        vm.GlobalSettings.IsStretched.Value = true;
        await WindowResizing.SetSizeAsync(vm);
        vm.ImageViewer.MainImage.InvalidateVisual();
        await SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task Stretch(MainViewModel vm)
    {
        if (Settings.ImageScaling.StretchImage)
        {
            Settings.ImageScaling.StretchImage = false;
            vm.GlobalSettings.IsStretched.Value = false;
        }
        else
        {
            Settings.ImageScaling.StretchImage = true;
            vm.GlobalSettings.IsStretched.Value = true;
        }

        //vm.ImageViewer.MainImage.InvalidateVisual();
        await WindowResizing.SetSizeAsync(vm);
        await SaveSettingsAsync().ConfigureAwait(false);
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

    public static void CenterWindowOnScreen(Window window)
    {
        CenterWindowOnScreen(horizontal: true, top: false, window: window);
    }

    public static void CenterWindowOnScreen(bool horizontal = true, bool top = false, Window? window = null)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            window ??= desktop.MainWindow;
            if (window.WindowState is WindowState.Maximized or WindowState.FullScreen)
            {
                return;
            }

            ScreenHelper.UpdateScreenSize(window);
            var screen = ScreenHelper.ScreenSize;

            // Get the size of the window
            var windowSize = window.ClientSize;

            var x = screen.X;
            var y = screen.Y;

            // Calculate the position to center the window on the screen
            var centeredX = x + (screen.WorkingAreaWidth - windowSize.Width) / 2;
            var centeredY = y + (top ? 0 : (screen.WorkingAreaHeight - windowSize.Height) / 2);

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
            window.Position = new PixelPoint((int)Settings.WindowProperties.Left,
                (int)Settings.WindowProperties.Top);
            window.Width = Settings.WindowProperties.Width;
            window.Height = Settings.WindowProperties.Height;
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                window.Position = new PixelPoint((int)Settings.WindowProperties.Left,
                    (int)Settings.WindowProperties.Top);
                window.Width = Settings.WindowProperties.Width;
                window.Height = Settings.WindowProperties.Height;
            });
        }
    }

    public static void InitializeWindowPosition(Window window, IWindowProperties properties)
    {
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
            if (properties is { Left: not null, Top: not null })
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Position = new PixelPoint(properties.Left.GetValueOrDefault(),
                    properties.Top.GetValueOrDefault());
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }
    }

    public static void InitializeWindowSizeAndPosition(Window window, IWindowProperties properties)
    {
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
            if (properties.Maximized)
            {
                window.WindowState = WindowState.Maximized;
            }
            else if (properties is { Left: not null, Top: not null })
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Position = new PixelPoint(properties.Left.GetValueOrDefault(),
                    properties.Top.GetValueOrDefault());
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            if (properties is not { Width: not null, Height: not null })
            {
                return;
            }

            if (properties.Width > window.MinWidth && properties.Width < window.MaxWidth &&
                properties.Height > window.MinHeight && properties.Height < window.MaxHeight)
            {
                window.Width = properties.Width.Value;
                window.Height = properties.Height.Value;
            }
            else
            {
                DebugHelper.LogDebug(nameof(WindowFunctions), nameof(InitializeWindowSizeAndPosition),
                    "Invalid width and height values");
            }
        }
    }

    public static void SetWindowSize(Window window, AvaloniaPropertyChangedEventArgs<Size> size,
        IWindowProperties properties)
    {
        if (!size.NewValue.HasValue)
        {
            return;
        }

        if (size.NewValue.Value == size.OldValue.Value)
        {
            return;
        }

        if (size.NewValue.Value.Width < window.MinWidth)
        {
            return;
        }

        if (size.NewValue.Value.Height < window.MinHeight)
        {
            return;
        }

        properties.Width = window.Bounds.Width;
        properties.Height = window.Bounds.Height;
    }

    #endregion

    #region Window Drag and Behavior

    public static void WindowDragAndDoubleClickBehavior(Window window, PointerPressedEventArgs e,
        IPlatformWindowService platformWindowService)
    {
        var currentScreen = ScreenHelper.ScreenSize;

        var screen = window.Screens.ScreenFromVisual(window);
        if (screen == null)
        {
            return;
        }

        if (e.ClickCount == 2 && e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
        {
            platformWindowService.MaximizeRestore();
            return;
        }

        window.BeginMoveDrag(e);

        if (screen.WorkingArea.Width == currentScreen.WorkingAreaWidth &&
            screen.WorkingArea.Height == currentScreen.WorkingAreaHeight && screen.Scaling == currentScreen.Scaling)
        {
            return;
        }

        ScreenHelper.UpdateScreenSize(window);
        WindowResizing.SetSize(window.DataContext as MainViewModel);
    }

    public static void WindowDragBehavior(Window window, PointerPressedEventArgs e)
    {
        var currentScreen = ScreenHelper.ScreenSize;
        window.BeginMoveDrag(e);
        var screen = window.Screens.ScreenFromVisual(window);
        if (screen == null)
        {
            return;
        }

        if (screen.WorkingArea.Width == currentScreen.WorkingAreaWidth &&
            screen.WorkingArea.Height == currentScreen.WorkingAreaHeight && screen.Scaling == currentScreen.Scaling)
        {
            return;
        }

        ScreenHelper.UpdateScreenSize(window);
        WindowResizing.SetSize(window.DataContext as MainViewModel);
    }

    #endregion
}