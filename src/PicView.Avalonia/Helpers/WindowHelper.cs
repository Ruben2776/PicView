using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.Calculations;
using PicView.Core.Config;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.Helpers;

public static class WindowHelper
{
    #region Window Dragging and size changing

    public static void InitializeWindowSizeAndPosition(Window window)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            window.Position = new PixelPoint((int)SettingsHelper.Settings.WindowProperties.Left, (int)SettingsHelper.Settings.WindowProperties.Top);
            window.Width = SettingsHelper.Settings.WindowProperties.Width;
            window.Height = SettingsHelper.Settings.WindowProperties.Height;
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                window.Position = new PixelPoint((int)SettingsHelper.Settings.WindowProperties.Left, (int)SettingsHelper.Settings.WindowProperties.Top);
                window.Width = SettingsHelper.Settings.WindowProperties.Width;
                window.Height = SettingsHelper.Settings.WindowProperties.Height;
            });
        }
    }

    public static void HandleWindowResize(Window window, AvaloniaPropertyChangedEventArgs<Size> size)
    {
        if (!SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            return;
        }

        if (!size.OldValue.HasValue || !size.NewValue.HasValue)
        {
            return;
        }

        if (size.OldValue.Value.Width == 0 || size.OldValue.Value.Height == 0 ||
            size.NewValue.Value.Width == 0 || size.NewValue.Value.Height == 0)
        {
            return;
        }

        if (size.Sender != window)
        {
            return;
        }

        var x = (size.OldValue.Value.Width - size.NewValue.Value.Width) / 2;
        var y = (size.OldValue.Value.Height - size.NewValue.Value.Height) / 2;

        window.Position = new PixelPoint(window.Position.X + (int)x, window.Position.Y + (int)y);
    }

    public static void CenterWindowOnScreen(bool horizontal = true)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var screen = ScreenHelper.GetScreen(desktop.MainWindow);
        if (screen is null)
        {
            return;
        }
        var window = desktop.MainWindow;

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var width = window.Bounds.Width == 0 ? window.Width : window.Bounds.Width;
            width = double.IsNaN(width) ? window.MinWidth : width;
            var height = window.Bounds.Height == 0 ? window.Height : window.Bounds.Height;
            height = double.IsNaN(height) ? window.MinHeight : height;
            var verticalPos = Math.Max(screen.WorkingArea.Y, screen.WorkingArea.Y + (screen.WorkingArea.Height * screen.Scaling - height) / 2);
            if (horizontal)
            {
                window.Position = new PixelPoint(
                    x: (int)Math.Max(screen.WorkingArea.X, screen.WorkingArea.X + (screen.WorkingArea.Width * screen.Scaling - width) / 2),
                    y: (int)verticalPos
                );
            }
            else
            {
                window.Position = new PixelPoint(window.Position.X, (int)verticalPos);
            }
        });
    }

    #endregion Window Dragging and size changing

    #region Change window behavior

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
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

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
        SetSize(vm);
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task ToggleFullscreen(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (SettingsHelper.Settings.WindowProperties.Fullscreen)
        {
            vm.IsFullscreen = false;
            desktop.MainWindow.WindowState = WindowState.Normal;
            SettingsHelper.Settings.WindowProperties.Fullscreen = false;
        }
        else
        {
            vm.IsFullscreen = true;
            desktop.MainWindow.WindowState = WindowState.Maximized;
            SettingsHelper.Settings.WindowProperties.Fullscreen = true;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
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

        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }
    
    public static async Task MaximizeRestore()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (SettingsHelper.Settings.WindowProperties.Maximized)
        {
            await Dispatcher.UIThread.InvokeAsync(() => 
                desktop.MainWindow.WindowState = WindowState.Normal);
            SettingsHelper.Settings.WindowProperties.Maximized = false;
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() => 
                desktop.MainWindow.WindowState = WindowState.Maximized);
            SettingsHelper.Settings.WindowProperties.Maximized = true;
        }
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task Minimize()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() => 
            desktop.MainWindow.WindowState = WindowState.Maximized);
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

    public static async Task ToggleUI(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (SettingsHelper.Settings.UIProperties.ShowInterface)
        {
            vm.IsInterfaceShown = false;
            SettingsHelper.Settings.UIProperties.ShowInterface = false;
            vm.IsTopToolbarShown = false;
            vm.IsBottomToolbarShown = false;
        }
        else
        {
            vm.IsInterfaceShown = true;
            vm.IsTopToolbarShown = true;
            if (SettingsHelper.Settings.UIProperties.ShowBottomNavBar)
            {
                vm.IsBottomToolbarShown = true;
            }
            SettingsHelper.Settings.UIProperties.ShowInterface = true;
        }
        
        SetSize(vm);
        await FunctionsHelper.CloseMenus();
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
        
    }

    public static async Task ToggleBottomToolbar(MainViewModel vm)
    {
        if (SettingsHelper.Settings.UIProperties.ShowBottomNavBar)
        {
            vm.IsBottomToolbarShown = false;
            SettingsHelper.Settings.UIProperties.ShowBottomNavBar = false;
        }
        else
        {
            vm.IsBottomToolbarShown = true;
            SettingsHelper.Settings.UIProperties.ShowBottomNavBar = true;
        }
        SetSize(vm);
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    #endregion Change window behavior

    #region SetSize

    public static void SetSize(MainViewModel vm)
    {
        if (!NavigationHelper.CanNavigate(vm))
        {
            return;
        }
        var preloadValue = vm.ImageIterator?.PreLoader.Get(vm.ImageIterator.Index, vm.ImageIterator.Pics);
        SetSize(preloadValue?.ImageModel?.PixelWidth ?? (int)vm.ImageWidth, preloadValue?.ImageModel?.PixelHeight ?? (int)vm.ImageHeight, vm.RotationAngle, vm);
    }

    public static void SetSize(double width, double height, double rotation, MainViewModel vm)
    {
        width = width == 0 ? vm.ImageWidth : width;
        height = height == 0 ? vm.ImageHeight : height;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var monitor = ScreenHelper.GetScreen(desktop.MainWindow);
        double desktopMinWidth = 0, desktopMinHeight = 0, containerWidth = 0, containerHeight = 0;
        var uiTopSize = SettingsHelper.Settings.UIProperties.ShowInterface ? 32 : 0; // Height of the titlebar, TODO get actual size
        var uiBottomSize =
            SettingsHelper.Settings.UIProperties.ShowInterface || SettingsHelper.Settings.UIProperties.ShowBottomNavBar
                ? 28 : 0;
        var galleryHeight = GalleryFunctions.IsBottomGalleryOpen ? vm.GalleryItemSize + 36 : 0;
        if (Dispatcher.UIThread.CheckAccess())
        {
            desktopMinWidth = desktop.MainWindow.MinWidth;
            desktopMinHeight = desktop.MainWindow.MinHeight;
            containerWidth = desktop.MainWindow.Width;
            containerHeight = desktop.MainWindow.Height - (uiTopSize + uiBottomSize);
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                desktopMinWidth = desktop.MainWindow.MinWidth;
                desktopMinHeight = desktop.MainWindow.MinHeight;
                containerWidth = desktop.MainWindow.Width;
                containerHeight = desktop.MainWindow.Height - (uiTopSize + uiBottomSize);
            }, DispatcherPriority.Normal).Wait();
        }
        var size = ImageSizeCalculationHelper.GetImageSize(
            width,
            height,
            monitor.Bounds.Width,
            monitor.Bounds.Height,
            desktopMinWidth,
            desktopMinHeight,
            ImageSizeCalculationHelper.GetInterfaceSize(),
            rotation,
            vm.IsStretched,
            75,
            monitor.Scaling,
            SettingsHelper.Settings.WindowProperties.Fullscreen,
            uiTopSize,
            uiBottomSize,
            galleryHeight,
            vm.IsAutoFit,
            containerWidth,
            containerHeight,
            vm.IsScrollingEnabled);

        vm.TitleMaxWidth = size.TitleMaxWidth;
        vm.ImageWidth = size.Width;
        vm.ImageHeight = size.Height;
        vm.ImageMargin = new Thickness(0, 0, 0, size.Margin);
    }

    #endregion SetSize

    public static async Task WindowClosingBehavior(Window window)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            window.Hide();
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(window.Hide);
        }
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            Environment.Exit(0);
            return;
        }
        var monitor = ScreenHelper.GetScreen(desktop.MainWindow);
        var top = window.Position.Y + (monitor.WorkingArea.Height - monitor.Bounds.Height);
        var left = window.Position.X + (monitor.WorkingArea.Width - monitor.Bounds.Width);
        SettingsHelper.Settings.WindowProperties.Top = top;
        SettingsHelper.Settings.WindowProperties.Left = left;
        SettingsHelper.Settings.WindowProperties.Width = window.Width;
        SettingsHelper.Settings.WindowProperties.Height = window.Height;

        await SettingsHelper.SaveSettingsAsync();
        FileDeletionHelper.DeleteTempFiles();
        Environment.Exit(0);
    }
}