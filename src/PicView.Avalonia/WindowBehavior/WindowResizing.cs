using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Preloading;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Calculations;
using PicView.Core.Config;
using PicView.Core.Navigation;

namespace PicView.Avalonia.WindowBehavior;

public static class WindowResizing
{
    #region Window Resize Handling

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

    #endregion

    #region Set Window Size

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
                {
                    return;
                }
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
                var nextIndex = vm.ImageIterator.GetIteration(vm.ImageIterator.CurrentIndex,
                    vm.ImageIterator.IsReversed ? NavigateTo.Previous : NavigateTo.Next);
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
                Dispatcher.UIThread.InvokeAsync(() =>
                    SetSize(firstWidth, firstHeight, secondWidth, secondHeight, vm.RotationAngle, vm));
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

        double GetWidth(PreLoadValue preloadValue)
        {
            return preloadValue?.ImageModel?.PixelWidth ?? vm.ImageWidth;
        }

        double GetHeight(PreLoadValue preloadValue)
        {
            return preloadValue?.ImageModel?.PixelHeight ?? vm.ImageHeight;
        }
    }

    public static void SetSize(double width, double height, double secondWidth, double secondHeight, double rotation,
        MainViewModel vm)
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
        var desktopMinWidth = desktop.MainWindow.MinWidth;
        var desktopMinHeight = desktop.MainWindow.MinHeight;
        var containerWidth = mainView.Bounds.Width;
        var containerHeight = mainView.Bounds.Height;

        if (double.IsNaN(containerWidth) || double.IsNaN(containerHeight) || double.IsNaN(width) ||
            double.IsNaN(height))
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
            if (SettingsHelper.Settings.WindowProperties.Fullscreen ||
                SettingsHelper.Settings.WindowProperties.Maximized)
            {
                vm.GalleryWidth = double.NaN;
            }
            else
            {
                vm.GalleryWidth = vm.RotationAngle is 90 or 270
                    ? Math.Max(size.Height, desktopMinHeight)
                    : Math.Max(size.Width, desktopMinWidth);
            }
        }
        else
        {
            vm.GalleryWidth = double.NaN;
        }
        
        vm.AspectRatio = size.AspectRatio;
    }

    public static void SaveSize(Window window)
    {
        if (SettingsHelper.Settings.WindowProperties.Maximized || SettingsHelper.Settings.WindowProperties.Fullscreen || SettingsHelper.Settings.WindowProperties.AutoFit)
            return;
            
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
}