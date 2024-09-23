using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.Navigation;

namespace PicView.Avalonia.Navigation;

public static class UpdateImage
{
    #region Update Source and Preview

    public static async Task UpdateSource(MainViewModel vm, int index, List<string> imagePaths, bool isReversed, PreLoader.PreLoadValue? preLoadValue,
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
                    imagePaths[vm.ImageIterator.GetIteration(index, isReversed ? NavigateTo.Previous : NavigateTo.Next, true)]);
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

        await Dispatcher.UIThread.InvokeAsync(TooltipHelper.CloseToolTipMessage);

        vm.FileInfo = preLoadValue.ImageModel.FileInfo;
        vm.ZoomValue = 1;
        vm.PixelWidth = preLoadValue.ImageModel.PixelWidth;
        vm.PixelHeight = preLoadValue.ImageModel.PixelHeight;
        ExifHandling.UpdateExifValues(preLoadValue.ImageModel, vm);
    }

    public static void LoadingPreview(MainViewModel vm, int index, int currentIndex)
    {
        if (index != currentIndex)
        {
            return;
        }

        SetTitleHelper.SetLoadingTitle(vm);
        vm.SelectedGalleryItemIndex = index;
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        }

        using var image = new MagickImage();
        image.Ping(vm.ImageIterator.ImagePaths[index]);
        var thumb = image.GetExifProfile()?.CreateThumbnail();
        if (thumb is null)
        {
            if (index == currentIndex)
            {
                vm.IsLoading = true;
                vm.ImageSource = null;
            }

            return;
        }

        var byteArray = thumb.ToByteArray();
        if (byteArray is null)
        {
            if (index == currentIndex)
            {
                vm.IsLoading = true;
                vm.ImageSource = null;
            }

            return;
        }

        var stream = new MemoryStream(byteArray);
        if (index != currentIndex)
        {
            return;
        }

        vm.ImageSource = new Bitmap(stream);
        vm.ImageType = ImageType.Bitmap;
    }

    #endregion
}
