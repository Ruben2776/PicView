using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.UI;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.WindowBehavior;

public static class WindowResizing
{
    #region Window Resize Handling

    private static bool KeepWindowSize(Window window, AvaloniaPropertyChangedEventArgs<Size> size)
    {
        var oldSize = size.OldValue.Value;
        var newSize = size.NewValue.Value;
        
        if (!size.OldValue.HasValue || !size.NewValue.HasValue || 
            size.Sender != window || oldSize.Width is 0 || oldSize.Height is 0 ||
            newSize.Width is 0 || newSize.Height is 0)
        {
            return false;
        }

        var x = (oldSize.Width - newSize.Width) / 2;
        var y = (oldSize.Height - newSize.Height) / 2;

        window.Position = new PixelPoint(window.Position.X + (int)x, window.Position.Y + (int)y);
        
        return true;
    }
    
    public static void FastCenterWindow(Window window)
    {
        var screen = ScreenHelper.ScreenSize;

        // Get the size of the window
        var windowSize = window.ClientSize;

        var x = screen.X;
        var y = screen.Y;

        // Calculate the position to center the window on the screen
        var centeredX = x + (screen.WorkingAreaWidth - windowSize.Width) / 2;
        var centeredY = y + (screen.WorkingAreaHeight - windowSize.Height) / 2;

        // Set the window's new position
        window.Position = new PixelPoint((int)centeredX, (int)centeredY);
    }

    public static void HandleWindowResize(MainWindow mainWindow, AvaloniaPropertyChangedEventArgs<Size> size)
    {
        if (!Settings.WindowProperties.AutoFit)
        {
            return;
        }

        if (Settings.WindowProperties.KeepCentered)
        {
            FastCenterWindow(mainWindow);
        }
        else
        {
            var isWindowResized = KeepWindowSize(mainWindow, size);
            if (!isWindowResized)
            {
                return;
            }
        }
        
        if (mainWindow.DataContext is not MainWindowViewModel mainWindowVm)
        {
            return;
        }
        
        var nextNavX = size.NewValue.Value.Width > SizeDefaults.SearchResetAndRotateBtnBp ? 50 : 15;

        RepositionCursorIfTriggered(mainWindowVm.IsNavigationButtonLeftClicked,
            clicked => mainWindowVm.IsNavigationButtonLeftClicked = clicked,
            () => mainWindow.UIHelper.GetBottomBar.PreviousButton,
            new Point(nextNavX, 10));

        RepositionCursorIfTriggered(mainWindowVm.IsNavigationButtonRightClicked,
            clicked => mainWindowVm.IsNavigationButtonRightClicked = clicked,
            () => mainWindow.UIHelper.GetBottomBar.NextButton,
            new Point(nextNavX, 10));

        RepositionCursorIfTriggered(mainWindowVm.IsBottomToolbarRightRotationClicked,
            clicked => mainWindowVm.IsBottomToolbarRightRotationClicked = clicked,
            () => mainWindow.UIHelper.GetBottomBar.RotateRightButton,
            new Point(20, 10));

        RepositionCursorIfTriggered(mainWindowVm.IsBottomToolbarLeftRotationClicked,
            clicked => mainWindowVm.IsBottomToolbarLeftRotationClicked = clicked,
            () => mainWindow.UIHelper.GetBottomBar.RotateLeftButton,
            new Point(20, 10));

        RepositionCursorIfTriggered(mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverNavigationButtonNextClicked,
            clicked => mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverNavigationButtonNextClicked = clicked,
            () => UIHelper.GetHoverBar().NextButton,
            new Point(50, 10));

        RepositionCursorIfTriggered(mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverNavigationButtonPreviousClicked,
            clicked => mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverNavigationButtonPreviousClicked = clicked,
            () => UIHelper.GetHoverBar().PreviousButton,
            new Point(50, 10));

        RepositionCursorIfTriggered(mainWindowVm.IsClickArrowLeftClicked,
            clicked => mainWindowVm.IsClickArrowLeftClicked = clicked,
            () => UIHelper.GetClickArrowLeft(mainWindowVm),
            new Point(15, 95));
        
        RepositionCursorIfTriggered(mainWindowVm.IsClickArrowRightClicked,
            clicked => mainWindowVm.IsClickArrowRightClicked = clicked,
            () => UIHelper.GetClickArrowRight(mainWindowVm),
            new Point(65, 95));

        RepositionCursorIfTriggered(mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverRotateRightClicked,
            clicked => mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverRotateRightClicked = clicked,
            () => UIHelper.GetHoverBar().RotateRightButton,
            new Point(11, 7));

        RepositionCursorIfTriggered(mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverRotateLeftClicked,
            clicked => mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverRotateLeftClicked = clicked,
            () => UIHelper.GetHoverBar().RotateLeftButton,
            new Point(11, 7));
    }

    private static void RepositionCursorIfTriggered(
        bool isTriggered,
        Action<bool> setTrigger,
        Func<Control?> controlProvider,
        Point offset)
    {
        if (!isTriggered)   
        {
            return;
        }
        var control = controlProvider();
        if (control is not null && Application.Current.DataContext is CoreViewModel core)
        {
            Dispatcher.CurrentDispatcher.Post(() =>
            {
                var screenPoint = control.PointToScreen(offset);
                core.PlatformService.SetCursorPos(screenPoint.X, screenPoint.Y);
            }, DispatcherPriority.Loaded + 1);
        }

        setTrigger(false);
    }

    #endregion
    
    #region Set Window Size

    public static void SetSize(MainWindow mainWindow, WindowResizeReason reason)
    {
        var vm = mainWindow.DataContext as MainWindowViewModel;
        var size = GetSize(mainWindow, vm);

        if (size is null)
        {
            return;
        }

        SetSize(size.Value, reason, mainWindow, vm);
    }

    public static void SetSize(double width, double height, double secondWidth, double secondHeight, WindowResizeReason reason, MainWindow mainWindow, MainWindowViewModel vm)
    {
        var size = GetSize(width, height, secondWidth, secondHeight, vm.WindowTabs.ActiveTab.CurrentValue.RotationAngle.CurrentValue, mainWindow, vm);

        if (size is null || size.Value.WindowWidth is 0 || size.Value.WindowHeight is 0)
        {
            return;
        }

        SetSize(size.Value, reason, mainWindow, vm);
    }

    public static void SetSize(ImageSize size, WindowResizeReason reason, MainWindow mainWindow, MainWindowViewModel vm)
    {
        vm.WindowTabs.ActiveTab.CurrentValue.InitialZoom.Value = size.InitialZoom;
        vm.ScrollViewerWidth.Value = size.ScrollViewerWidth;
        vm.ScrollViewerHeight.Value = size.ScrollViewerHeight;
        
        vm.ImageWidth.Value = size.Width;
        vm.ImageHeight.Value = size.Height;

        if (Settings.WindowProperties.Fullscreen)
        {
            vm.WindowMaxWidth.Value = ScreenHelper.ScreenSize.Width;
            vm.WindowMaxHeight.Value = ScreenHelper.ScreenSize.Height;
        }
        else if (Settings.WindowProperties.Maximized)
        {
            vm.WindowMaxWidth.Value = ScreenHelper.ScreenSize.WorkingAreaWidth;
            vm.WindowMaxHeight.Value = ScreenHelper.ScreenSize.WorkingAreaHeight;
        }
        else if (Settings.WindowProperties.AutoFit)
        {
            if (reason is WindowResizeReason.User)
            {
                vm.WindowMaxWidth.Value = vm.WindowMaxHeight.Value = double.NaN;
            }
            else
            {
                vm.WindowMaxWidth.Value = size.WindowWidth;
                vm.WindowMaxHeight.Value = size.WindowHeight;
            }
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Fixes weird window size bug where the window width is at a fixed value
                mainWindow.Width = mainWindow.Height = double.NaN;
            });
        }
        else
        {
            vm.WindowMaxWidth.Value =
                vm.WindowMaxHeight.Value = double.NaN;
        }
    }

    public static ImageSize? GetSize(MainWindow mainWindow, MainWindowViewModel vm)
    {
        if (vm?.WindowTabs.ActiveTab?.CurrentValue is not { } tab)
        {
            return null;
        }
        double width, height, secondaryWidth, secondaryHeight;
        if (tab.Model.FileInfo is not null)
        {
            if (tab.Model.PixelHeight is not 0 && tab.Model.PixelWidth is not 0)
            {
                width = tab.Model.PixelWidth;
                height = tab.Model.PixelHeight;
            }
            else if (tab.Model.Image is Bitmap bitmap)
            {
                width = bitmap.PixelSize.Width;
                height = bitmap.PixelSize.Height;
            }
            else if (vm.WindowTabs.SharedCache?.TryGet(tab.Model.FileInfo, out var preloadValue) ?? false)
            {
                width = preloadValue.ImageModel.PixelWidth;
                height = preloadValue.ImageModel.PixelHeight;
            }
            else
            {
                return null;
            }
        }
        else if (tab.Model?.Image is Bitmap bitmap)
        {
            width = bitmap.PixelSize.Width;
            height = bitmap.PixelSize.Height;
        }
        else
        {
            return null;
        }

        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            if (vm.WindowTabs.SharedCache?.TryGet(tab.SecondaryModel.FileInfo, out var secondaryPreloadValue) ?? false)
            {
                secondaryWidth = secondaryPreloadValue.ImageModel.PixelWidth;
                secondaryHeight = secondaryPreloadValue.ImageModel.PixelHeight;
            }
            else
            {
                if (tab.Model.Image is Bitmap bitmap)
                {
                    secondaryWidth = bitmap.PixelSize.Width;
                    secondaryHeight = bitmap.PixelSize.Height;
                }
                else
                {
                    return null;
                }
            }
        }
        else
        {
            secondaryWidth = secondaryHeight = 0;
        }
        
        return GetSize(width, height, secondaryWidth, secondaryHeight, tab.RotationAngle.CurrentValue, mainWindow, vm);
    }

    public static ImageSize? GetSize(double width, double height, double secondWidth, double secondHeight,
        double rotation, MainWindow mainWindow, MainWindowViewModel vm)
    {
        var screenSize = ScreenHelper.ScreenSize;
        var (uiBottomSize, uiTopSize, galleryWidth, galleryHeight) = GetContainerSize();

        if (double.IsNaN(width) || double.IsNaN(height))
        {
            return null;
        }
        var (containerWidth, containerHeight) = GetWindowSize();
        if (Settings.ImageScaling.ShowImageSideBySide && secondWidth > 0 && secondHeight > 0)
        {
            return ImageSizeCalculationHelper.GetSideBySideImageSize(
                width,
                height,
                secondWidth,
                secondHeight,
                screenSize,
                containerWidth,
                containerHeight,
                rotation,
                uiTopSize,
                uiBottomSize,
                galleryWidth,
                galleryHeight);
        }
        return ImageSizeCalculationHelper.GetImageSize(
                width,
                height,
                screenSize,
                containerWidth,
                containerHeight,
                rotation,
                uiTopSize,
                uiBottomSize,
                galleryWidth,
                galleryHeight);

        (double, double, double, double) GetContainerSize()
        {
            var (gW, gH) = GalleryHelper.GetGallerySize(vm);
            if (vm.WindowTabs.Tabs.CurrentValue.Count > 1)
            {
                uiTopSize = SizeDefaults.TabHeight + vm.TitlebarHeight.CurrentValue + 2;
            }
            else
            {
                uiTopSize = vm.TitlebarHeight.CurrentValue + 2;
            }

            uiBottomSize = Settings.UIProperties.ShowBottomNavBar ? SizeDefaults.BottombarHeight : 0;
            return (uiBottomSize, uiTopSize, gW, gH);
        }

        (double, double) GetWindowSize()
        {
            return Dispatcher.CurrentDispatcher.CheckAccess() ? Get() : Dispatcher.CurrentDispatcher.Invoke(Get, DispatcherPriority.Send);

            (double, double) Get()
            {
                return (mainWindow.UIHelper.GetMainView.Bounds.Width, mainWindow.UIHelper.GetMainView.Bounds.Height);
            }
        }
    }

    public static void SaveSize(Window window)
    {
        if (Dispatcher.CurrentDispatcher.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.CurrentDispatcher.Invoke(Set);
        }

        return;

        void Set()
        {
            var top = window.Position.Y;
            var left = window.Position.X;
            Settings.WindowProperties.Top = top;
            Settings.WindowProperties.Left = left;
            if (Settings.WindowProperties.Maximized || Settings.WindowProperties.Fullscreen)
            {
                return;
            }
            Settings.WindowProperties.Width = window.Bounds.Width;
            Settings.WindowProperties.Height = window.Bounds.Height;
        }
    }

    #endregion
}