using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.Navigation;

public static class QuickLoad
{
    public static async Task QuickLoadAsync(MainViewModel vm, string file)
    {
        var fileInfo = new FileInfo(file);
        if (!fileInfo.Exists) // If not file, try to load if URL, base64 or directory
        {
            // TODO - Handle URL, base64 and directory
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
        WindowHelper.SetSize(imageModel.PixelWidth, imageModel.PixelHeight, 0, vm);
        vm.IsLoading = false;
        vm.ImageIterator = new ImageIterator(fileInfo, vm);
        await vm.ImageIterator.AddAsync(vm.ImageIterator.Index, imageModel).ConfigureAwait(false);
        await vm.ImageIterator.LoadPicAtIndex(vm.ImageIterator.Index, vm).ConfigureAwait(false);
    }
}
