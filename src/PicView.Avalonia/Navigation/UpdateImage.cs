using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.Gallery;
using PicView.Core.Navigation;

namespace PicView.Avalonia.Navigation;

public static class UpdateImage
{
    #region Update Source and Preview

    public static async Task UpdateSource(MainViewModel vm, int index, List<string> imagePaths, bool isReversed,
        PreLoader.PreLoadValue? preLoadValue,
        PreLoader.PreLoadValue? nextPreloadValue = null)
    {
        preLoadValue ??= await vm.ImageIterator.GetPreLoadValueAsync(index);
        if (preLoadValue.ImageModel?.Image is null)
        {
            var fileInfo = preLoadValue.ImageModel?.FileInfo ?? new FileInfo(imagePaths[index]);
            preLoadValue.ImageModel = await GetImageModel.GetImageModelAsync(fileInfo).ConfigureAwait(false);
        }

        if (SettingsHelper.Settings.ImageScaling.ShowImageSideBySide)
        {
            nextPreloadValue ??= await vm.ImageIterator.GetNextPreLoadValueAsync();
            if (nextPreloadValue.ImageModel?.Image is null)
            {
                var fileInfo = nextPreloadValue.ImageModel?.FileInfo ?? new FileInfo(
                    imagePaths[
                        vm.ImageIterator.GetIteration(index, isReversed ? NavigateTo.Previous : NavigateTo.Next,
                            true)]);
                nextPreloadValue.ImageModel = await GetImageModel.GetImageModelAsync(fileInfo).ConfigureAwait(false);
            }
        }

        vm.IsLoading = false;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            vm.ImageViewer.SetTransform(preLoadValue.ImageModel.EXIFOrientation);
            if (SettingsHelper.Settings.ImageScaling.ShowImageSideBySide)
            {
                vm.SecondaryImageSource = nextPreloadValue.ImageModel.Image;
            }

            vm.ImageSource = preLoadValue.ImageModel.Image;
            if (preLoadValue.ImageModel.ImageType is ImageType.AnimatedGif or ImageType.AnimatedWebp)
            {
                vm.ImageViewer.MainImage.InitialAnimatedSource = preLoadValue.ImageModel.FileInfo.FullName;
            }

            vm.ImageType = preLoadValue.ImageModel.ImageType;

            WindowHelper.SetSize(preLoadValue.ImageModel.PixelWidth, preLoadValue.ImageModel.PixelHeight,
                nextPreloadValue?.ImageModel?.PixelWidth ?? 0, nextPreloadValue?.ImageModel?.PixelHeight ?? 0,
                preLoadValue.ImageModel.Rotation, vm);
        }, DispatcherPriority.Send);


        SetTitleHelper.SetTitle(vm, preLoadValue.ImageModel);

        if (SettingsHelper.Settings.WindowProperties.KeepCentered)
        {
            await Dispatcher.UIThread.InvokeAsync(() => { WindowHelper.CenterWindowOnScreen(); });
        }

        vm.GetIndex = index + 1;
        if (vm.SelectedGalleryItemIndex != index)
        {
            vm.SelectedGalleryItemIndex = index;
            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                GalleryNavigation.CenterScrollToSelectedItem(vm);
            }
        }

        vm.FileInfo = preLoadValue.ImageModel.FileInfo;
        vm.ZoomValue = 1;
        vm.PixelWidth = preLoadValue.ImageModel.PixelWidth;
        vm.PixelHeight = preLoadValue.ImageModel.PixelHeight;
        ExifHandling.UpdateExifValues(preLoadValue.ImageModel, vm);

        await Dispatcher.UIThread.InvokeAsync(TooltipHelper.CloseToolTipMessage);
    }

    /// <summary>
    /// Sets the given image as the single image displayed in the view.
    /// </summary>
    /// <param name="source">The source of the image to display.</param>
    /// <param name="imageType"></param>
    /// <param name="name">The name of the image.</param>
    /// <param name="vm">The main view model instance.</param>
    public static void SetSingleImage(object source, ImageType imageType, string name, MainViewModel vm)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (vm.CurrentView != vm.ImageViewer)
            {
                vm.CurrentView = vm.ImageViewer;
            }
        }, DispatcherPriority.Render);

        vm.ImageIterator = null;
        int width, height;
        if (imageType is ImageType.Svg)
        {
            var path = source as string;
            using var magickImage = new MagickImage();
            magickImage.Ping(path);
            vm.ImageSource = source;
            vm.ImageType = ImageType.Svg;
            width = (int)magickImage.Width;
            height = (int)magickImage.Height;
        }
        else
        {
            var bitmap = source as Bitmap;
            vm.ImageSource = source;
            vm.ImageType = imageType == ImageType.Invalid ? ImageType.Bitmap : imageType;
            width = bitmap?.PixelSize.Width ?? 0;
            height = bitmap?.PixelSize.Height ?? 0;
        }

        if (GalleryFunctions.IsBottomGalleryOpen)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Trigger animation
                vm.GalleryMode = GalleryMode.BottomToClosed;
            });
            // Set to closed to ensure next gallery mode changing is fired
            vm.GalleryMode = GalleryMode.Closed;
        }

        WindowHelper.SetSize(width, height, 0, 0, 0, vm);
        if (vm.RotationAngle != 0)
        {
            vm.ImageViewer.Rotate(vm.RotationAngle);
        }

        var singeImageWindowTitles = ImageTitleFormatter.GenerateTitleForSingleImage(width, height, name, 1);
        vm.WindowTitle = singeImageWindowTitles.BaseTitle;
        vm.Title = singeImageWindowTitles.TitleWithAppName;
        vm.TitleTooltip = singeImageWindowTitles.TitleWithAppName;
        vm.GalleryMargin = new Thickness(0, 0, 0, 0);

        vm.PlatformService.StopTaskbarProgress();
    }

    public static void LoadingPreview(MainViewModel vm, int index)
    {
        SetTitleHelper.SetLoadingTitle(vm);
        vm.SelectedGalleryItemIndex = index;
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        }

        using var image = new MagickImage();
        image.Ping(vm.ImageIterator.ImagePaths[index]);
        var thumb = image.GetExifProfile()?.CreateThumbnail();

        var byteArray = thumb?.ToByteArray();
        if (byteArray is null)
        {
            return;
        }

        var stream = new MemoryStream(byteArray);
        vm.ImageSource = new Bitmap(stream);
        vm.ImageType = ImageType.Bitmap;
    }

    #endregion
}