using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.ArchiveHandling;
using PicView.Core.Calculations;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Navigation;

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
        
        var currentScreen = ScreenHelper.ScreenSize;
        window.BeginMoveDrag(e);
        var screen = window.Screens.ScreenFromVisual(window);
        if (screen != null)
        {
            if (screen.WorkingArea.Width != currentScreen.WorkingAreaWidth ||
                screen.WorkingArea.Height != currentScreen.WorkingAreaHeight || screen.Scaling != currentScreen.Scaling)
            {
                ScreenHelper.UpdateScreenSize(window);
                SetSize(window.DataContext as MainViewModel);
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
                SetSize(window.DataContext as MainViewModel);
            }
        }
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
            window.Position = horizontal ?
                new PixelPoint((int)centeredX, (int)centeredY) :
                new PixelPoint(window.Position.X, (int)centeredY);
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
        SetSize(vm);
        await Dispatcher.UIThread.InvokeAsync(() => CenterWindowOnScreen(false));
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
                RestoreSize(desktop.MainWindow);
                Restore(vm, desktop);
            }
        }
        else
        {
            SaveSize(desktop.MainWindow);
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
                SaveSize(desktop.MainWindow);
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
        SetSize(vm);
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

    #endregion Change window behavior

    #region Set and save Size

    public static void SetSize(MainViewModel vm)
    {
        double firstWidth, firstHeight;
        var preloadValue = vm.ImageIterator?.GetCurrentPreLoadValue();
        if (preloadValue == null)
        {
            if (vm.FileInfo is null)
            {
                if (vm.ImageSource is Bitmap bitmap)
                {
                    firstWidth = bitmap.PixelSize.Width;
                    firstHeight = bitmap.PixelSize.Height;
                }
                else 
                    return;
            }
            else
            {
                var magickImage = new MagickImage();
                magickImage.Ping(vm.FileInfo);
                firstWidth = magickImage.Width;
                firstHeight = magickImage.Height;
            }
        }
        else
        {
            firstWidth = GetWidth(preloadValue);
            firstHeight = GetHeight(preloadValue);
        }
        if (SettingsHelper.Settings.ImageScaling.ShowImageSideBySide)
        {
            var secondaryPreloadValue = vm.ImageIterator?.GetNextPreLoadValue();
            double secondWidth, secondHeight;
            if (secondaryPreloadValue != null)
            {
                secondWidth = GetWidth(secondaryPreloadValue);
                secondHeight = GetHeight(secondaryPreloadValue);
            }
            else if (vm.ImageIterator is not null)
            {
                var nextIndex = vm.ImageIterator.GetIteration(vm.ImageIterator.CurrentIndex, vm.ImageIterator.IsReversed ? NavigateTo.Previous : NavigateTo.Next);
                var magickImage = new MagickImage();
                magickImage.Ping(vm.ImageIterator.ImagePaths[nextIndex]);
                secondWidth = magickImage.Width;
                secondHeight = magickImage.Height;
            }
            else
            {
                secondWidth = 0;
                secondHeight = 0;
            }

            if (Dispatcher.UIThread.CheckAccess())
            {
                SetSize(firstWidth, firstHeight, secondWidth, secondHeight, vm.RotationAngle, vm);
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(() => SetSize(firstWidth, firstHeight, secondWidth, secondHeight, vm.RotationAngle, vm));
            }
        }
        else
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                SetSize(firstWidth, firstHeight, 0, 0, vm.RotationAngle, vm);
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(() => SetSize(firstWidth, firstHeight, 0, 0, vm.RotationAngle, vm));
            }
        }

        return;
        double GetWidth(PreLoader.PreLoadValue preloadValue)
        {
            return preloadValue?.ImageModel?.PixelWidth ?? vm.ImageWidth;
        }
        
        double GetHeight(PreLoader.PreLoadValue preloadValue)
        {
            return preloadValue?.ImageModel?.PixelHeight ?? vm.ImageHeight;
        }
    }
    
    public static void SetSize(double width, double height, double secondWidth, double secondHeight, double rotation, MainViewModel vm)
    {
        width = width == 0 ? vm.ImageWidth : width;
        height = height == 0 ? vm.ImageHeight : height;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var mainView = UIHelper.GetMainView;
        if (mainView is null)
        {
            return;
        }

        const int padding = 45;
        var screenSize = ScreenHelper.ScreenSize;
        double desktopMinWidth = 0, desktopMinHeight = 0, containerWidth = 0, containerHeight = 0;
        desktopMinWidth = desktop.MainWindow.MinWidth;
        desktopMinHeight = desktop.MainWindow.MinHeight;
        containerWidth = mainView.Bounds.Width;
        containerHeight = mainView.Bounds.Height;

        if (double.IsNaN(containerWidth) || double.IsNaN(containerHeight) || double.IsNaN(width) || double.IsNaN(height))
        {
            return;
        }
        ImageSizeCalculationHelper.ImageSize size;
        if (SettingsHelper.Settings.ImageScaling.ShowImageSideBySide && secondWidth > 0 && secondHeight > 0)
        {
            size = ImageSizeCalculationHelper.GetImageSize(
                width,
                height,
                secondWidth,
                secondHeight,
                screenSize.WorkingAreaWidth,
                screenSize.WorkingAreaHeight,
                desktopMinWidth,
                desktopMinHeight,
                ImageSizeCalculationHelper.GetInterfaceSize(),
                rotation,
                padding,
                screenSize.Scaling,
                vm.TitlebarHeight,
                vm.BottombarHeight,
                vm.GalleryHeight,
                containerWidth,
                containerHeight);
        }
        else
        {
            size = ImageSizeCalculationHelper.GetImageSize(
                width,
                height,
                screenSize.WorkingAreaWidth,
                screenSize.WorkingAreaHeight,
                desktopMinWidth,
                desktopMinHeight,
                ImageSizeCalculationHelper.GetInterfaceSize(),
                rotation,
                padding,
                screenSize.Scaling,
                vm.TitlebarHeight,
                vm.BottombarHeight,
                vm.GalleryHeight,
                containerWidth,
                containerHeight);
        }
        
        vm.TitleMaxWidth = size.TitleMaxWidth;
        vm.ImageWidth = size.Width;
        vm.SecondaryImageWidth = size.SecondaryWidth;
        vm.ImageHeight = size.Height;
        vm.GalleryMargin = new Thickness(0, 0, 0, size.Margin);
        
        vm.ScrollViewerWidth = size.ScrollViewerWidth;
        vm.ScrollViewerHeight = size.ScrollViewerHeight;

        if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            if (SettingsHelper.Settings.WindowProperties.Fullscreen || SettingsHelper.Settings.WindowProperties.Maximized)
            {
                vm.GalleryWidth = double.NaN;
            }
            else
            {
                vm.GalleryWidth = vm.RotationAngle is 90 or 270 ?
                    Math.Max(size.Height, desktopMinHeight) :
                    Math.Max(size.Width, desktopMinWidth);
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
        string lastFile;
        if (NavigationHelper.CanNavigate(vm))
        {
            lastFile = vm?.FileInfo?.FullName ?? FileHistoryNavigation.GetLastFile();
        }
        else
        {
            var url = vm?.Title.GetURL();
            lastFile = !string.IsNullOrWhiteSpace(url) ? url : FileHistoryNavigation.GetLastFile();
        }
        SettingsHelper.Settings.StartUp.LastFile = lastFile;
        await SettingsHelper.SaveSettingsAsync();
        await KeybindingsHelper.UpdateKeyBindingsFile(); // Save keybindings
        FileDeletionHelper.DeleteTempFiles();
        FileHistoryNavigation.WriteToFile();
        ArchiveExtraction.Cleanup();
        Environment.Exit(0);
    }
}