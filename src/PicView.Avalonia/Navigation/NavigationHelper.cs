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
        if (GalleryFunctions.IsFullGalleryOpen)
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

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            GalleryNavigation.NavigateGallery(next ? Direction.Right : Direction.Left, vm);
            return;
        }

        if (!CanNavigate(vm))
        {
            return;
        }
        await Navigate(next, vm);
    }

    public static async Task IterateButton(bool next, MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            GalleryNavigation.NavigateGallery(next ? Direction.Right : Direction.Left, vm);
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
    
    public static async Task LoadPicFromString(string source, MainViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(source) || vm is null)
        {
            return;
        }
        
        vm.CurrentView = vm.ImageViewer;
        
        var fileInfo = new FileInfo(source);
        if (!fileInfo.Exists)
        {
            if (Directory.Exists(fileInfo.FullName))
            {
                await Start();
                return;
            }
            // TODO load from URL or base64 if not a file
            return;
        }
        
        await Start();
        return;

        async Task Start()
        {
            vm.ImageIterator ??= new ImageIterator(fileInfo, vm);
            await vm.ImageIterator.LoadPicFromString(source, vm).ConfigureAwait(false);
        }
    }

    public static async Task LoadingPreview(int index, MainViewModel vm)
    {
        vm.SelectedGalleryItemIndex = index;
        using var image = new MagickImage();
        image.Ping(vm.ImageIterator.Pics[index]);
        var thumb = image.GetExifProfile()?.CreateThumbnail();
        if (thumb is null)
        {
            return;
        }

        var byteArray = await Task.FromResult(thumb.ToByteArray());
        var stream = new MemoryStream(byteArray);
        vm.ImageViewer.SetImage(new Bitmap(stream), ImageType.Bitmap);
    }
}