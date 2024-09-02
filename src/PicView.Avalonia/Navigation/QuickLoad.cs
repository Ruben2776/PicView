using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Gallery;

namespace PicView.Avalonia.Navigation;

public static class QuickLoad
{
    public static async Task QuickLoadAsync(MainViewModel vm, string file)
    {
        var fileInfo = new FileInfo(file);
        if (!fileInfo.Exists) // If not file, try to load if URL, base64 or directory
        {
            await NavigationHelper.LoadPicFromStringAsync(file, vm).ConfigureAwait(false);
            return;
        }

        if (file.IsArchive()) // Handle if file exist and is an archive
        {
            await NavigationHelper.LoadPicFromArchiveAsync(file, vm).ConfigureAwait(false);
            return;
        }
        vm.CurrentView = vm.ImageViewer;
        vm.FileInfo ??= fileInfo;
        var imageModel = await ImageHelper.GetImageModelAsync(fileInfo).ConfigureAwait(false);
        if (imageModel.ImageType is ImageType.AnimatedGif or ImageType.AnimatedWebp)
        {
            vm.ImageViewer.MainImage.InitialAnimatedSource = file;
        }
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
        
        var tasks = new List<Task>
        {
            vm.ImageIterator.AddAsync(vm.ImageIterator.CurrentIndex, imageModel),
            vm.ImageIterator.Preload()
        };

        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            if (vm.IsInterfaceShown)
            {
                vm.GalleryMode = GalleryMode.BottomNoAnimation;
                tasks.Add(GalleryLoad.LoadGallery(vm, fileInfo.DirectoryName));
            }
            else if (SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI)
            {
                vm.GalleryMode = GalleryMode.BottomNoAnimation;
                tasks.Add(GalleryLoad.LoadGallery(vm, fileInfo.DirectoryName));
            }
            else if (SettingsHelper.Settings.WindowProperties.Fullscreen)
            {
                vm.GalleryMode = GalleryMode.BottomNoAnimation;
                tasks.Add(GalleryLoad.LoadGallery(vm, fileInfo.DirectoryName));
            }
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
