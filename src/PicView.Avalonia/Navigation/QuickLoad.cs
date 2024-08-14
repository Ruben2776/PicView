using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.Navigation;

public static class QuickLoad
{
    public static async Task QuickLoadAsync(MainViewModel vm, string file)
    {
        var fileInfo = new FileInfo(file);
        if (!fileInfo.Exists) // If not file, try to load if URL, base64 or directory
        {
            if (Directory.Exists(fileInfo.DirectoryName))
            {
                await Load(true).ConfigureAwait(false);
                return;
            }
            // TODO - Handle URL, base64 and directory
            await NavigationHelper.LoadPicFromUrlAsync(file, vm);
            return;
        }

        if (file.IsArchive()) // Handle if file exist and is an archive
        {
            // TODO - Handle archive
            return;
        }
        
        await Load(false).ConfigureAwait(false);
        return;

        async Task Load(bool isDirectory)
        {
            vm.CurrentView = vm.ImageViewer;
            if (isDirectory)
            {
                vm.ImageIterator = new ImageIterator(fileInfo, vm);
                await vm.ImageIterator.IterateToIndex(0).ConfigureAwait(false);
            }
            else
            {
                vm.FileInfo ??= fileInfo;
                var imageModel = await ImageHelper.GetImageModelAsync(fileInfo).ConfigureAwait(false);
                vm.ImageSource = imageModel.Image;
                vm.ImageType = imageModel.ImageType;
                WindowHelper.SetSize(imageModel.PixelWidth, imageModel.PixelHeight, imageModel.Rotation, vm);
                vm.IsLoading = false;
                imageModel.EXIFOrientation = ImageHelper.GetExifOrientation(vm);
                ExifHandling.SetImageModel(imageModel, vm);
                var changed = false; // Need to recalculate size if changed
                if (vm.ScaleX != 1)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        vm.ImageViewer.SetScaleX();
                    });
                    changed = true;
                }
                if (vm.RotationAngle != 0)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        vm.ImageViewer.Rotate(vm.RotationAngle);
                    });
                    changed = true;
                }
                if (changed)
                {
                    WindowHelper.SetSize(imageModel.PixelWidth, imageModel.PixelHeight, imageModel.Rotation, vm);
                }
                
                ExifHandling.UpdateExifValues(imageModel, vm);
                vm.ImageIterator = new ImageIterator(fileInfo, vm);
                
                SetTitleHelper.SetTitle(vm, imageModel);
                vm.GetIndex = vm.ImageIterator.CurrentIndex + 1;
                if (SettingsHelper.Settings.WindowProperties.KeepCentered)
                {
                    WindowHelper.CenterWindowOnScreen(false);
                }
                
                _ = vm.ImageIterator.AddAsync(vm.ImageIterator.CurrentIndex, imageModel);
                _ = vm.ImageIterator.Preload();
            }

            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                if (!SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI && !vm.IsInterfaceShown)
                {
                    return;
                }
                await GalleryLoad.LoadGallery(vm, fileInfo.DirectoryName);
            }

        }

    }
}
