using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.ViewModels;
using PicView.Core.Navigation;

namespace PicView.Avalonia.Navigation;

public static class NavigationHelper
{
    public static bool CanNavigate(MainViewModel vm)
    {
        if (vm?.ImageIterator?.Pics is null)
        {
            return false;
        }

        if (vm.ImageService is null)
        {
            return false;
        }

        return vm.ImageIterator.Pics.Count > 0 && vm.ImageIterator.Index > -1 && vm.ImageIterator.Index < vm.ImageIterator.Pics.Count;
    }

    public static async Task Navigate(bool next, MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }
        if (!CanNavigate(vm))
        {
            return;
        }

        if (MainKeyboardShortcuts.CtrlDown)
        {
            await vm.ImageIterator.LoadNextPic(next ? NavigateTo.Last : NavigateTo.First, vm).ConfigureAwait(false);
        }
        else
        {
            var navigateTo = next ? NavigateTo.Next : NavigateTo.Previous;
            await vm.ImageIterator.LoadNextPic(navigateTo, vm).ConfigureAwait(false);
        }
    }
    
    public static async Task NavigateFirstOrLast(bool last, MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }
        if (!CanNavigate(vm))
        {
            return;
        }

        await vm.ImageIterator.LoadNextPic(last ? NavigateTo.Last : NavigateTo.First, vm).ConfigureAwait(false);
    }

    public static void LoadingPreview(int index, MainViewModel vm)
    {
        using var image = new MagickImage();
        image.Ping(vm.ImageIterator.Pics[index]);
        var thumb = image.GetExifProfile()?.CreateThumbnail();
        if (thumb is null)
        {
            return;
        }

        var stream = new MemoryStream(thumb?.ToByteArray());
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            vm.ImageViewer.SetImage(new Bitmap(stream), ImageType.Bitmap);
        });
    }
}