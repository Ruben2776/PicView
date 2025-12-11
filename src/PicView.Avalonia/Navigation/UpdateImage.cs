using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Exif;
using PicView.Core.Gallery;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.Preloading;
using PicView.Core.Titles;

// ReSharper disable RedundantAlwaysMatchSubpattern

namespace PicView.Avalonia.Navigation;

public static class UpdateImage
{
    #region Update source

    /// <summary>
    ///     Updates the image source in the main view model based on the specified index and preloaded values.
    /// </summary>
    /// <param name="vm">The main view model instance.</param>
    /// <param name="index">The index of the image to update.</param>
    /// <param name="imagePaths">The list of image paths to navigate through.</param>
    /// <param name="preLoadValue">The preloaded value of the current image.</param>
    /// <param name="nextPreloadValue">Optional: The preloaded value of the next image, used for side-by-side display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async ValueTask UpdateSource(MainViewModel vm, int index, IReadOnlyList<FileInfo> imagePaths,
        PreLoadValue? preLoadValue,
        PreLoadValue? nextPreloadValue = null)
    {
        preLoadValue ??= await NavigationManager.GetPreLoadValueAsync(index).ConfigureAwait(false);
        if (preLoadValue is null)
        {
            await ErrorHandling.ReloadAsync(vm).ConfigureAwait(false);
            return;
        }
        if (preLoadValue.ImageModel?.Image is null && index == NavigationManager.GetCurrentIndex)
        {
            var fileInfo = preLoadValue.ImageModel?.FileInfo ?? imagePaths[index];
            preLoadValue.ImageModel = await GetImageModel.GetImageModelAsync(fileInfo).ConfigureAwait(false);
        }
        
        if (preLoadValue.ImageModel?.FileInfo is null)
        {
            preLoadValue.ImageModel.FileInfo = new FileInfo(NavigationManager.GetCurrentFileName);
        }

        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            nextPreloadValue ??= await NavigationManager.GetNextPreLoadValueAsync().ConfigureAwait(false);
            if (nextPreloadValue.ImageModel?.Image is null && index == NavigationManager.GetCurrentIndex)
            {
                var nextFileInfo = nextPreloadValue.ImageModel?.FileInfo ?? new FileInfo(NavigationManager.GetNextFileName);
                nextPreloadValue.ImageModel = await GetImageModel.GetImageModelAsync(nextFileInfo).ConfigureAwait(false);
            }

            if (nextPreloadValue.ImageModel?.FileInfo is null)
            {
                // Sometimes the FileInfo is null, don't know why. This fixes it. Probably a race condition?
                nextPreloadValue.ImageModel.FileInfo = new FileInfo(NavigationManager.GetNextFileName);
            }
        }

        if (index != NavigationManager.GetCurrentIndex)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (index != NavigationManager.GetCurrentIndex)
            {
                return;
            }

            vm.ImageViewer.SetTransform(preLoadValue.ImageModel.Orientation, preLoadValue.ImageModel.Format);
            vm.ImageViewer.ZoomPanControl.ResetZoomSlim();

            if (Settings.ImageScaling.ShowImageSideBySide && nextPreloadValue is { ImageModel: not null })
            {
                vm.PicViewer.SecondaryImageSource.Value = nextPreloadValue.ImageModel.Image;
                if (preLoadValue is { ImageModel: not null})
                {
                    vm.PicViewer.ImageSource.Value = preLoadValue.ImageModel.Image;
                    vm.PicViewer.ImageType.Value = preLoadValue.ImageModel.ImageType;
                    vm.PicViewer.Format.Value = preLoadValue.ImageModel.Format;
                }
            }
            else if (preLoadValue is { ImageModel: not null})
            {
                if (preLoadValue.ImageModel.ImageType is ImageType.AnimatedGif or ImageType.AnimatedWebp)
                {
                    vm.ImageViewer.MainImage.InitialAnimatedSource = preLoadValue.ImageModel.FileInfo.FullName;
                }
                
                vm.PicViewer.ImageSource.Value = preLoadValue.ImageModel.Image;
                vm.PicViewer.SecondaryImageSource.Value = null;
                vm.PicViewer.ImageType.Value = preLoadValue.ImageModel.ImageType;
                vm.PicViewer.Format.Value = preLoadValue.ImageModel.Format;
            }
            else
            {
                return;
            }

            if (!Settings.Zoom.ScrollEnabled)
            {
                SetSize();
            }

            UIHelper.GetToolTipMessage.IsVisible = false;
        }, DispatcherPriority.Send);

        vm.MainWindow.IsLoadingIndicatorShown.Value = false;

        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            TitleManager.SetSideBySideTitle(vm, preLoadValue.ImageModel, nextPreloadValue?.ImageModel);
        }
        else
        {
            if (TiffManager.IsTiff(preLoadValue.ImageModel?.FileInfo?.FullName))
            {
                if (TiffManager.IsTiff(preLoadValue.ImageModel?.FileInfo?.FullName))
                {
                    TitleManager.TrySetTiffTitle(preLoadValue?.ImageModel, vm);
                }
                else
                {
                    TitleManager.SetTitle(vm, preLoadValue?.ImageModel);
                }
            }
            else
            {
                TitleManager.SetTitle(vm, preLoadValue?.ImageModel);
            }
        }
        
        if (Settings.Zoom.ScrollEnabled)
        {
            // Bad fix for scrolling
            // TODO: Implement proper scrolling fix
            Settings.Zoom.ScrollEnabled = false;
            await Dispatcher.UIThread.InvokeAsync(SetSize);
            Settings.Zoom.ScrollEnabled = true;
            await Dispatcher.UIThread.InvokeAsync(SetSize, DispatcherPriority.Send);
        }

        if (Settings.WindowProperties.KeepCentered)
        {
            await Dispatcher.UIThread.InvokeAsync(() => { WindowFunctions.CenterWindowOnScreen(); });
        }

        vm.PicViewer.Index.Value = index;
        if (Settings.Gallery.IsBottomGalleryShown)
        {
            GalleryNavigation.CenterScrollToItem(index);
        }
        
        SetStats(vm, preLoadValue.ImageModel);
        
        return;

        void SetSize()
        {
            WindowResizing.SetSize(preLoadValue.ImageModel.PixelWidth, preLoadValue.ImageModel.PixelHeight,
                    nextPreloadValue?.ImageModel?.PixelWidth ?? 0, nextPreloadValue?.ImageModel?.PixelHeight ?? 0,
                    vm.PicViewer.RotationAngle.CurrentValue, vm);
        }

    }

    public static async ValueTask UpdateSourceSlim(MainViewModel vm,
        int index,
        object? imageSource,
        int width,
        int height,
        IReadOnlyList<FileInfo> imagePaths,
        CancellationToken token)
    {
        if (index != NavigationManager.GetCurrentIndex)
        {
            return;
        }
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            vm.PicViewer.ImageSource.Value = imageSource;
            vm.PicViewer.SecondaryImageSource.Value = null;
        }, DispatcherPriority.Send, token);
        
        TitleManager.SetTitleSlim(vm, width, height, index, imagePaths);
    }

    #endregion

    #region TIFF
    
    /// <summary>
    ///     Sets the image displayed in the view to the given TIFF image based on the given navigation info.
    /// </summary>
    /// <param name="tiffNavigationInfo">The navigation info for the TIFF image.</param>
    /// <param name="index">The index of the image to display.</param>
    /// <param name="fileInfo">The FileInfo object representing the file containing the image.</param>
    /// <param name="vm">The main view model instance.</param>
    public static async Task SetTiffImageAsync(TiffManager.TiffNavigationInfo tiffNavigationInfo, int index, FileInfo fileInfo,
        MainViewModel vm)
    {
        var source = await Task.Run( () => tiffNavigationInfo.Pages[tiffNavigationInfo.CurrentPage].ToWriteableBitmap()).ConfigureAwait(false);
        vm.PicViewer.ImageSource.Value = source;
        vm.PicViewer.SecondaryImageSource.Value = null;
        vm.PicViewer.ImageType.Value = ImageType.Bitmap;
        var width = source?.PixelSize.Width ?? 0;
        var height = source?.PixelSize.Height ?? 0;
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (vm.MainWindow.CurrentView.CurrentValue != vm.ImageViewer)
            {
                vm.MainWindow.CurrentView.Value = vm.ImageViewer;
            }
            
            WindowResizing.SetSize(width, height, 0, 0, 0, vm);

            if (vm.PicViewer.RotationAngle.CurrentValue != 0)
            {
                vm.ImageViewer.Rotate(vm.PicViewer.RotationAngle.CurrentValue);
            }
        }, DispatcherPriority.Render);
        
        TitleManager.SetTiffTitle(tiffNavigationInfo, width, height, index, fileInfo, vm);

        var imageModel = new ImageModel
        {
            Orientation = ExifOrientationHelper.GetImageOrientation(fileInfo),
            ImageType = ImageType.Bitmap,
            FileInfo = fileInfo,
            Image = source,
            PixelWidth = width,
            PixelHeight = height
        };
        SetStats(vm, imageModel);
    }

    #endregion

    #region Single Image

    /// <summary>
    /// Updates the main view model to display a single image, based on the provided parameters, by setting image properties,
    /// updating window titles, and managing the gallery view and taskbar progress.
    /// </summary>
    /// <param name="source">The image source object to be displayed.</param>
    /// <param name="imageType">The type of the image (e.g., Bitmap, Svg, etc.) being handled.</param>
    /// <param name="name">The name or file name of the image used for display purposes.</param>
    /// <param name="vm">The main view model instance to update with the image information.</param>
    public static async ValueTask SetSingleImageAsync(
        object source,
        ImageType imageType,
        string name,
        MainViewModel vm)
    {
        
        int width, height;
        if (imageType is ImageType.Svg)
        {
            var path = source as string;
            using var magickImage = new MagickImage();
            magickImage.Ping(path);
            vm.PicViewer.ImageSource.Value = source;
            vm.PicViewer.ImageType.Value = ImageType.Svg;
            width = (int)magickImage.Width;
            height = (int)magickImage.Height;
        }
        else
        {
            var bitmap = source as Bitmap;
            vm.PicViewer.ImageSource.Value = source;
            vm.PicViewer.ImageType.Value = imageType == ImageType.Invalid ? ImageType.Bitmap : imageType;
            width = bitmap?.PixelSize.Width ?? 0;
            height = bitmap?.PixelSize.Height ?? 0;
        }

        vm.PicViewer.FileInfo.Value = null;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (vm.MainWindow.CurrentView.CurrentValue != vm.ImageViewer)
            {
                vm.MainWindow.CurrentView.Value = vm.ImageViewer;
            }
            WindowResizing.SetSize(width, height, 0, 0, 0, vm);
            vm.ImageViewer.ZoomPanControl.ResetZoomSlim();
        }, DispatcherPriority.Send);

        var singeImageWindowTitles = ImageTitleFormatter.GenerateTitleForSingleImage(width, height, name, 100);
        vm.PicViewer.WindowTitle.Value = singeImageWindowTitles.TitleWithAppName;
        vm.PicViewer.Title.Value = singeImageWindowTitles.BaseTitle;
        vm.PicViewer.TitleTooltip.Value = singeImageWindowTitles.BaseTitle;

        vm.PlatformService.StopTaskbarProgress();

        vm.PicViewer.PixelWidth.Value = width;
        vm.PicViewer.PixelHeight.Value = height;

        if (Settings.Gallery.IsBottomGalleryShown)
        {
            vm.Gallery.GalleryMode.Value = GalleryMode.Closed;
            vm.Gallery.GalleryMargin.Value = new Thickness(0);
        }

        vm.PicViewer.IsSingleImage.Value = true;
        await Dispatcher.UIThread.InvokeAsync(() => { UIHelper.GetGalleryView.IsVisible = false; }, DispatcherPriority.Render);
        await NavigationManager.DisposeImageIteratorAsync();
    }

    #endregion

    #region Set stats

    public static void SetStats(MainViewModel vm, ImageModel imageModel)
    {
        vm.PicViewer.IsSingleImage.Value = false;
        vm.PicViewer.PixelWidth.Value = imageModel.PixelWidth;
        vm.PicViewer.PixelHeight.Value = imageModel.PixelHeight;
        vm.PicViewer.GetIndex.Value = NavigationManager.GetNonZeroIndex;
        vm.PicViewer.ExifOrientation.Value = imageModel.Orientation;
        vm.PicViewer.FileInfo.Value = imageModel.FileInfo;
        vm.PicViewer.ZoomValue.Value = 100;

        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            // Fixes incorrect rendering in the side by side view
            // TODO: Improve and fix side by side and remove this hack 
            Dispatcher.UIThread.Post(() => { vm.ImageViewer?.MainImage?.InvalidateVisual(); });
        }

        // Reset effects
        vm.PicViewer.EffectConfig.Value = null;
    }

    #endregion
}