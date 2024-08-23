using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.Calculations;
using PicView.Core.Config;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.UI;

public static class WindowHelper
{
    #region Window Dragging and size changing
    
    public static void WindowDragAndDoubleClickBehavior(Window window, PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2 && e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
        {
            _ = MaximizeRestore();
            return;
        }

        window.BeginMoveDrag(e);
    }

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
        // TODO: Add support for multiple screens
        
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
            var getWidth = window?.Bounds.Width == 0 ? window?.Width : window?.Bounds.Width ?? 0;
            if (!getWidth.HasValue)
            {
                return;
            }
            var width = getWidth.Value;
            width = double.IsNaN(width) ? window.MinWidth : width;
            var getHeight = window?.Bounds.Height == 0 ? window?.Height : window?.Bounds.Height;
            if (!getHeight.HasValue)
            {
                return;
            }
            var height = getHeight.Value;
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
        vm.ImageViewer.MainImage.InvalidateVisual();
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
        SetSize(vm);
        vm.ImageViewer.MainImage.InvalidateVisual();
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    public static async Task NormalWindow(MainViewModel vm)
    {
        vm.SizeToContent = SizeToContent.Manual;
        vm.CanResize = true;
        SettingsHelper.Settings.WindowProperties.AutoFit = false;
        SetSize(vm);
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
        SetSize(vm);
        vm.ImageViewer.MainImage.InvalidateVisual();
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }
    
    public static async Task Stretch(MainViewModel vm)
    {
        SettingsHelper.Settings.ImageScaling.StretchImage = true;
        vm.IsStretched = true;
        SetSize(vm);
        vm.ImageViewer.MainImage.InvalidateVisual();
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
            await Dispatcher.UIThread.InvokeAsync(() => 
                desktop.MainWindow.WindowState = WindowState.Normal);
            SettingsHelper.Settings.WindowProperties.Fullscreen = false;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                RestoreSize(desktop.MainWindow);
                Restore(vm, desktop);
            }
        }
        else
        {
            SaveSize(desktop.MainWindow);
            Fullscreen(vm, desktop);
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
                SaveSize(desktop.MainWindow);
            }
            Maximize();
        }
        
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    public static void Restore(MainViewModel vm, IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            vm.SizeToContent = SizeToContent.WidthAndHeight;
            vm.CanResize = false;
        }
        else
        {
            vm.SizeToContent = SizeToContent.Manual;
            vm.CanResize = true;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            vm.IsTopToolbarShown = true;
            vm.TitlebarHeight = SizeDefaults.TitlebarHeight;
            if (SettingsHelper.Settings.UIProperties.ShowBottomNavBar)
            {
                vm.IsBottomToolbarShown = true;
                vm.BottombarHeight = SizeDefaults.BottombarHeight;
            }
            vm.IsInterfaceShown = true;
        }
        Dispatcher.UIThread.InvokeAsync(() => 
            desktop.MainWindow.WindowState = WindowState.Normal);
        SettingsHelper.Settings.WindowProperties.Maximized = false;
        SettingsHelper.Settings.WindowProperties.Fullscreen = false;
        SetSize(vm);
        InitializeWindowSizeAndPosition(desktop.MainWindow);
    }

    public static void Maximize()
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
            SetSize(desktop.MainWindow.DataContext as MainViewModel);
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

        SettingsHelper.Settings.WindowProperties.Fullscreen = true;
            
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            vm.IsTopToolbarShown = false; // Hide interface in fullscreen. Remember to turn back when exiting fullscreen
            vm.IsBottomToolbarShown = false;
            vm.IsInterfaceShown = false;
            Dispatcher.UIThread.Post(() => CenterWindowOnScreen());
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

    #endregion Change window behavior

    #region Set and save Size

    public static void SetSize(MainViewModel vm)
    {
        if (!NavigationHelper.CanNavigate(vm))
        {
            return;
        }

        var preloadValue = vm.ImageIterator?.GetCurrentPreLoadValue();
        SetSize(preloadValue?.ImageModel?.PixelWidth ?? vm.ImageWidth, preloadValue?.ImageModel?.PixelHeight ?? vm.ImageHeight, vm.RotationAngle, vm);
    }

    public static void SetSize(double width, double height, double rotation, MainViewModel vm)
    {
        width = width == 0 ? vm.ImageWidth : width;
        height = height == 0 ? vm.ImageHeight : height;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var mainView = UIHelper.GetMainView;
        var screenSize = ScreenHelper.ScreenSize;
        double desktopMinWidth = 0, desktopMinHeight = 0, containerWidth = 0, containerHeight = 0;
        if (Dispatcher.UIThread.CheckAccess())
        {
            desktopMinWidth = desktop.MainWindow.MinWidth;
            desktopMinHeight = desktop.MainWindow.MinHeight;
            containerWidth = mainView.Bounds.Width;
            containerHeight = mainView.Bounds.Height;
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                desktopMinWidth = desktop.MainWindow.MinWidth;
                desktopMinHeight = desktop.MainWindow.MinHeight;
                containerWidth = mainView.Bounds.Width;
                containerHeight = mainView.Bounds.Height;
            }, DispatcherPriority.Normal).Wait();
        }

        if (double.IsNaN(containerWidth) || double.IsNaN(containerHeight) || double.IsNaN(width) || double.IsNaN(height))
        {
            return;
        }
        var size = ImageSizeCalculationHelper.GetImageSize(
            width,
            height,
            screenSize.WorkingAreaWidth,
            screenSize.WorkingAreaHeight,
            desktopMinWidth,
            desktopMinHeight,
            ImageSizeCalculationHelper.GetInterfaceSize(),
            rotation,
            padding: SettingsHelper.Settings.ImageScaling.StretchImage ? 2 : 45,
            screenSize.Scaling,
            vm.TitlebarHeight,
            vm.BottombarHeight,
            vm.GalleryHeight,
            containerWidth,
            containerHeight);
        
        vm.TitleMaxWidth = size.TitleMaxWidth;
        vm.ImageWidth = size.Width;
        vm.ImageHeight = size.Height;
        vm.GalleryMargin = new Thickness(0, 0, 0, size.Margin);

        if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            if (SettingsHelper.Settings.WindowProperties.Fullscreen || SettingsHelper.Settings.WindowProperties.Maximized)
            {
                vm.GalleryWidth = double.NaN;;
            }
            else
            {
                vm.GalleryWidth = Math.Max(size.Width, desktopMinWidth);;
            }
        }
        else
        {
            vm.GalleryWidth = double.NaN;;
        }
    }
    
    public static void SaveSize(Window window)
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
            var top = window.Position.Y;
            var left = window.Position.X;
            SettingsHelper.Settings.WindowProperties.Top = top;
            SettingsHelper.Settings.WindowProperties.Left = left;
            SettingsHelper.Settings.WindowProperties.Width = window.Width;
            SettingsHelper.Settings.WindowProperties.Height = window.Height;
        }
    }
    
    public static void RestoreSize(Window window)
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
            var x = (int)SettingsHelper.Settings.WindowProperties.Left;
            var y = (int)SettingsHelper.Settings.WindowProperties.Top;
            window.Position = new PixelPoint(x, y);
            window.Width = SettingsHelper.Settings.WindowProperties.Width;
            window.Height = SettingsHelper.Settings.WindowProperties.Height;
        }
    }

    #endregion

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

        if (!SettingsHelper.Settings.WindowProperties.Maximized || !SettingsHelper.Settings.WindowProperties.Fullscreen || SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            SaveSize(window);
        }
        var vm = window.DataContext as MainViewModel;
        SettingsHelper.Settings.StartUp.LastFile = vm?.FileInfo?.FullName ?? FileHistoryNavigation.GetLastFile();
        await SettingsHelper.SaveSettingsAsync();
        await KeybindingsHelper.UpdateKeyBindingsFile(); // Save keybindings
        FileDeletionHelper.DeleteTempFiles();
        FileHistoryNavigation.WriteToFile();
        Environment.Exit(0);
    }
}