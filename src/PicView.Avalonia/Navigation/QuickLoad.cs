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
                await NavigationHelper.LoadPicFromDirectoryAsync(file, vm);
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
        var imageModel = await ImageHelper.GetImageModelAsync(fileInfo).ConfigureAwait(false);
        vm.CurrentView = vm.ImageViewer;
        vm.ImageSource = imageModel.Image;
        vm.ImageType = imageModel.ImageType;
        WindowHelper.SetSize(imageModel.PixelWidth, imageModel.PixelHeight, imageModel.Rotation, vm);
        vm.IsLoading = false;
        vm.ImageIterator = new ImageIterator(fileInfo, vm);
        await vm.ImageIterator.AddAsync(vm.ImageIterator.Index, imageModel).ConfigureAwait(false);
        var preloadValue = vm.ImageIterator.PreLoader.Get(vm.ImageIterator.Index, vm.ImageIterator.Pics);
        await vm.ImageIterator.UpdateSource(preloadValue);
        _ = vm.ImageIterator.Preload();
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            await GalleryLoad.LoadGallery(vm, fileInfo.DirectoryName);
        }
    }
}
