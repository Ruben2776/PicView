using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
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
        if (GalleryFunctions.isFullGalleryOpen)
        {
            // TODO - Go to first or page image in gallery
            return;
        }

        await vm.ImageIterator.LoadNextPic(last ? NavigateTo.Last : NavigateTo.First, vm).ConfigureAwait(false);
    }

    public static async Task Iterate(bool next, MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }

        if (GalleryFunctions.isFullGalleryOpen)
        {
            // TODO - Implement gallery navigation
            return;
        }

        if (!CanNavigate(vm))
        {
            return;
        }
        await Navigate(false, vm);
    }

    public static async Task IterateButton(bool next, MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }

        if (GalleryFunctions.isFullGalleryOpen)
        {
            // TODO - Implement gallery navigation
            return;
        }

        if (!CanNavigate(vm))
        {
            return;
        }
        await Navigate(next, vm);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }
            var buttonName = next ? "NextButton" : "PreviousButton";
            var bottomBar = desktop.MainWindow.GetControl<BottomBar>("BottomBar");
            var button = bottomBar.GetControl<Button>(buttonName);
            var p = button.PointToScreen(new Point(50, 10));
            vm.PlatformService?.SetCursorPos(p.X, p.Y);
        });
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