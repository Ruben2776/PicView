using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.DebugTools;
using PicView.Core.Sizing;

namespace PicView.Avalonia.WindowBehavior;

public static class WindowResizing
{
    #region Window Resize Handling

    public static bool KeepWindowSize(Window window, AvaloniaPropertyChangedEventArgs<Size> size)
    {
        if (!size.OldValue.HasValue || !size.NewValue.HasValue ||
            size.Sender != window || size.OldValue.Value.Width == 0 || size.OldValue.Value.Height == 0 ||
            size.NewValue.Value.Width == 0 || size.NewValue.Value.Height == 0)
        {
            return false;
        }
        
        var oldSize = size.OldValue.Value;
        var newSize = size.NewValue.Value;

        var x = (oldSize.Width - newSize.Width) / 2;
        var y = (oldSize.Height - newSize.Height) / 2;

        window.Position = new PixelPoint(window.Position.X + (int)x, window.Position.Y + (int)y);
        
        return true;
    }

    public static void HandleWindowResize(Window window, AvaloniaPropertyChangedEventArgs<Size> size)
    {
        if (!Settings.WindowProperties.AutoFit || window.DataContext is not MainViewModel vm)
        {
            return;
        }

        var isWindowResized = KeepWindowSize(window, size);
        if (!isWindowResized)
        {
            return;
        }

        RepositionCursorIfTriggered(vm, vm.MainWindow.IsNavigationButtonLeftClicked,
            clicked => vm.MainWindow.IsNavigationButtonLeftClicked = clicked,
            () => UIHelper.GetBottomBar.GetControl<Button>("PreviousButton"),
            new Point(50, 10));

        RepositionCursorIfTriggered(vm, vm.MainWindow.IsNavigationButtonRightClicked,
            clicked => vm.MainWindow.IsNavigationButtonRightClicked = clicked,
            () => UIHelper.GetBottomBar.GetControl<Button>("NextButton"),
            new Point(50, 10));

        RepositionCursorIfTriggered(vm, vm.HoverbarViewModel.IsHoverNavigationButtonNextClicked,
            clicked => vm.HoverbarViewModel.IsHoverNavigationButtonNextClicked = clicked,
            () => UIHelper.GetHoverBar.GetControl<Button>("NextButton"),
            new Point(50, 10));

        RepositionCursorIfTriggered(vm, vm.HoverbarViewModel.IsHoverNavigationButtonPreviousClicked,
            clicked => vm.HoverbarViewModel.IsHoverNavigationButtonPreviousClicked = clicked,
            () => UIHelper.GetHoverBar.GetControl<Button>("PreviousButton"),
            new Point(50, 10));

        RepositionCursorIfTriggered(vm, vm.MainWindow.IsClickArrowLeftClicked,
            clicked => vm.MainWindow.IsClickArrowLeftClicked = clicked,
            () => UIHelper.GetMainView.GetControl<UserControl>("ClickArrowLeft"),
            new Point(15, 95));

        RepositionCursorIfTriggered(vm, vm.MainWindow.IsClickArrowRightClicked,
            clicked => vm.MainWindow.IsClickArrowRightClicked = clicked,
            () => UIHelper.GetMainView.GetControl<UserControl>("ClickArrowRight"),
            new Point(65, 95));

        RepositionCursorIfTriggered(vm, vm.MainWindow.IsBottomToolbarRotationClicked,
            clicked => vm.MainWindow.IsBottomToolbarRotationClicked = clicked,
            () => UIHelper.GetBottomBar.GetControl<IconButton>("RotateRightButton"),
            new Point(11, 7));

        RepositionCursorIfTriggered(vm, vm.HoverbarViewModel.IsHoverRotateRightClicked,
            clicked => vm.HoverbarViewModel.IsHoverRotateRightClicked = clicked,
            () => UIHelper.GetHoverBar.GetControl<IconButton>("RotateRightButton"),
            new Point(11, 7));

        RepositionCursorIfTriggered(vm, vm.HoverbarViewModel.IsHoverRotateLeftClicked,
            clicked => vm.HoverbarViewModel.IsHoverRotateLeftClicked = clicked,
            () => UIHelper.GetHoverBar.GetControl<IconButton>("RotateLeftButton"),
            new Point(11, 7));
        
        RepositionCursorIfTriggered(vm, vm.MainWindow.IsTitlebarRotationClicked,
            clicked => vm.MainWindow.IsTitlebarRotationClicked = clicked,
            () => UIHelper.GetTitlebar.GetControl<IconButton>("RotateRightButton"),
            new Point(11, 7));
    }

    private static void RepositionCursorIfTriggered(
        MainViewModel vm,
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
        if (control is not null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var screenPoint = control.PointToScreen(offset);
                    vm.PlatformService?.SetCursorPos(screenPoint.X, screenPoint.Y);
                }, DispatcherPriority.Render);

            }
            else
            {
                var screenPoint = control.PointToScreen(offset);
                vm.PlatformService?.SetCursorPos(screenPoint.X, screenPoint.Y);
            }
        }

        setTrigger(false);
    }

    #endregion
    
    #region Set Window Size

    public static void SetSize(MainViewModel vm)
    {
        var size = GetSize(vm);

        if (size is null)
        {
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            SetSize(size.Value, vm);
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(() => SetSize(size.Value, vm));
        }
    }

    public static async Task SetSizeAsync(MainViewModel vm)
    {
        var size = GetSize(vm);

        if (size is null)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => SetSize(size.Value, vm));
    }

    public static void SetSize(double width, double height, MainViewModel vm)
        => SetSize(width, height, 0, 0, vm.PicViewer.RotationAngle.CurrentValue, vm);

    public static void SetSize(double width, double height, double secondWidth, double secondHeight, double rotation,
        MainViewModel vm)
    {
        var size = GetSize(width, height, secondWidth, secondHeight, rotation, vm);

        if (size is null)
        {
            return;
        }

        SetSize(size.Value, vm);
    }

    public static void SetSize(ImageSize size, MainViewModel vm)
    {
        vm.MainWindow.TitleMaxWidth.Value = size.TitleMaxWidth;
        vm.PicViewer.ImageWidth.Value = size.Width;
        vm.PicViewer.SecondaryImageWidth.Value = size.SecondaryWidth;
        vm.PicViewer.ImageHeight.Value = size.Height;

        vm.PicViewer.ScrollViewerWidth.Value = size.ScrollViewerWidth;
        vm.PicViewer.ScrollViewerHeight.Value = size.ScrollViewerHeight;

        vm.PicViewer.AspectRatio.Value = size.AspectRatio;

        if (vm.Gallery is not { } gallery)
        {
            return;
        }

        gallery.GalleryMargin.Value = new Thickness(0, 0, 0, size.Margin);
        if (Settings.WindowProperties.AutoFit)
        {
            if (Settings.WindowProperties.Fullscreen ||
                Settings.WindowProperties.Maximized)
            {
                vm.PicViewer.GalleryWidth.Value = double.NaN;
            }
            else
            {
                var scrollbarSize = Settings.Zoom.ScrollEnabled ? SizeDefaults.ScrollbarSize : 0;
                vm.PicViewer.GalleryWidth.Value = vm.PicViewer.RotationAngle.CurrentValue is 90 or 270
                    ? Math.Max(size.Height + scrollbarSize, SizeDefaults.WindowMinSize + scrollbarSize)
                    : Math.Max(size.Width + scrollbarSize, SizeDefaults.WindowMinSize + scrollbarSize);
            }
        }
        else
        {
            vm.PicViewer.GalleryWidth.Value = double.NaN;
        }
    }

    public static ImageSize? GetSize(MainViewModel vm)
    {
        double firstWidth, firstHeight;
        var preloadValue = NavigationManager.GetCurrentPreLoadValue();
        if (preloadValue == null)
        {
            if (vm.PicViewer.FileInfo is null)
            {
                if (vm.PicViewer.ImageSource.CurrentValue is Bitmap bitmap)
                {
                    firstWidth = bitmap.PixelSize.Width;
                    firstHeight = bitmap.PixelSize.Height;
                }
                else
                {
                    return null;
                }
            }
            else if (vm.PicViewer.FileInfo?.CurrentValue?.Exists != null)
            {
                try
                {
                    var magickImage = new MagickImage();
                    magickImage.Ping(vm.PicViewer.FileInfo.CurrentValue);
                    firstWidth = magickImage.Width;
                    firstHeight = magickImage.Height;
                }
                catch (Exception e)
                {
                    DebugHelper.LogDebug(nameof(WindowBehavior), nameof(GetSize), e);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        else
        {
            firstWidth = preloadValue.ImageModel?.PixelWidth ?? vm.PicViewer.ImageWidth.CurrentValue;
            firstHeight = preloadValue.ImageModel?.PixelHeight ?? vm.PicViewer.ImageHeight.CurrentValue;
        }

        if (!Settings.ImageScaling.ShowImageSideBySide)
        {
            return GetSize(firstWidth, firstHeight, 0, 0, vm.PicViewer.RotationAngle.CurrentValue, vm);
        }

        var secondaryPreloadValue = NavigationManager.GetNextPreLoadValue();
        double secondWidth, secondHeight;
        if (secondaryPreloadValue is not null)
        {
            secondWidth = secondaryPreloadValue.ImageModel.PixelWidth;
            secondHeight = secondaryPreloadValue.ImageModel.PixelHeight;
        }
        else if (NavigationManager.CanNavigate(vm))
        {
            var nextFileName = NavigationManager.GetNextFileName;
            var magickImage = new MagickImage();
            magickImage.Ping(nextFileName);
            secondWidth = magickImage.Width;
            secondHeight = magickImage.Height;
        }
        else
        {
            secondWidth = 0;
            secondHeight = 0;
        }

        return GetSize(firstWidth, firstHeight, secondWidth, secondHeight, vm.PicViewer.RotationAngle.CurrentValue,
            vm);
    }

    public static ImageSize? GetSize(double width, double height, double secondWidth, double secondHeight,
        double rotation,
        MainViewModel vm)
    {
        width = width == 0 ? vm.PicViewer.ImageWidth.CurrentValue : width;
        height = height == 0 ? vm.PicViewer.ImageHeight.CurrentValue : height;

        var screenSize = ScreenHelper.ScreenSize;
        var (containerWidth, containerHeight) = GetContainerSize();

        if (double.IsNaN(width) || double.IsNaN(height))
        {
            return null;
        }

        var (minWidth, minHeight) = MainWindowViewModel.GetAndSetWindowMinSize(vm);
        
        ImageSize size;
        if (Settings.ImageScaling.ShowImageSideBySide && secondWidth > 0 && secondHeight > 0)
        {
            size = ImageSizeCalculationHelper.GetSideBySideImageSize(
                width,
                height,
                secondWidth,
                secondHeight,
                screenSize,
                minWidth,
                minHeight,
                vm.PlatformWindowService.CombinedTitleButtonsWidth,
                rotation,
                screenSize.Scaling,
                vm.MainWindow.TitlebarHeight.CurrentValue,
                vm.MainWindow.BottombarHeight.CurrentValue,
                GalleryFunctions.GetGalleryHeight(vm),
                containerWidth,
                containerHeight);
        }
        else
        {
            size = ImageSizeCalculationHelper.GetImageSize(
                width,
                height,
                screenSize,
                minWidth,
                minHeight,
                vm.PlatformWindowService.CombinedTitleButtonsWidth,
                rotation,
                screenSize.Scaling,
                vm.MainWindow.TitlebarHeight.CurrentValue,
                vm.MainWindow.BottombarHeight.CurrentValue,
                GalleryFunctions.GetGalleryHeight(vm),
                containerWidth,
                containerHeight);
        }

        return size;

        (double containerWidth, double containerHeight) GetContainerSize()
        {
            return Dispatcher.UIThread.CheckAccess() ? Get() : Dispatcher.UIThread.Invoke(Get, DispatcherPriority.Send);

            (double containerWidth, double containerHeight) Get()
            {
                var mainView = UIHelper.GetMainView;

                if (mainView is null)
                {
                    return default;
                }

                containerWidth = mainView.Bounds.Width;
                containerHeight = mainView.Bounds.Height;

                if (double.IsNaN(containerWidth))
                {
                    containerWidth = mainView.Bounds.Width;
                }

                if (double.IsNaN(containerHeight))
                {
                    containerHeight = mainView.Bounds.Height;
                }

                return (containerWidth, containerHeight);
            }
        }
    }

    public static void SaveSize(Window window)
    {
        if (Settings.WindowProperties.Maximized || Settings.WindowProperties.Fullscreen)
        {
            return;
        }

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
            Settings.WindowProperties.Top = top;
            Settings.WindowProperties.Left = left;
            Settings.WindowProperties.Width = window.Width;
            Settings.WindowProperties.Height = window.Height;
        }
    }

    #endregion
}