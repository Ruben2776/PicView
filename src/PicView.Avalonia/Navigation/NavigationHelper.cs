using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Core.Config;
using PicView.Core.Gallery;
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

        var navigateTo = next ? NavigateTo.Next : NavigateTo.Previous;
        await vm.ImageIterator.LoadNextPic(navigateTo).ConfigureAwait(false);
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

        await vm.ImageIterator.LoadNextPic(last ? NavigateTo.Last : NavigateTo.First).ConfigureAwait(false);
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
        UIHelper.CloseMenus(vm);
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
            if (vm.ImageIterator is null)
            {
                var imageModel = await ImageHelper.GetImageModelAsync(fileInfo).ConfigureAwait(false);
                vm.SetImageModel(imageModel);
                vm.ImageSource = imageModel;
                vm.ImageType = imageModel.ImageType;
                WindowHelper.SetSize(imageModel.PixelWidth, imageModel.PixelHeight, 0, vm);
                vm.ImageIterator = new ImageIterator(fileInfo, vm);
                await vm.ImageIterator.LoadPicAtIndex(vm.ImageIterator.Index);
            }
            else
            {
                await vm.ImageIterator.LoadPicFromString(source).ConfigureAwait(false);
            }
        }
    }
    
    public static async Task GoToNextFolder(bool next, MainViewModel vm)
    {
        vm.SetLoadingTitle();
        var fileList = await Task.Run(()  =>
        {
            var indexChange = next ? 1 : -1;
            var currentFolder = Path.GetDirectoryName(vm.ImageIterator?.Pics[vm.ImageIterator.Index]);
            var parentFolder = Path.GetDirectoryName(currentFolder);
            var directories = Directory.GetDirectories(parentFolder, "*", SearchOption.TopDirectoryOnly);
            var directoryIndex = Array.IndexOf(directories, currentFolder);
            if (SettingsHelper.Settings.UIProperties.Looping)
                directoryIndex = (directoryIndex + indexChange + directories.Length) % directories.Length;
            else
            {
                directoryIndex += indexChange;
                if (directoryIndex < 0 || directoryIndex >= directories.Length)
                    return null;
            }

            for (var i = directoryIndex; i < directories.Length; i++)
            {
                var fileInfo = new FileInfo(directories[i]);
                var fileList = vm.PlatformService.GetFiles(fileInfo);
                if (fileList is { Count: > 0 })
                    return fileList;
            }
            return null;
        }).ConfigureAwait(false);

        if (fileList is null)
        {
            vm.SetTitle();
            return;
        }
        var fileInfo = new FileInfo(fileList[0]);
        vm.ImageIterator.Pics = fileList;
        vm.ImageIterator.Index = 0;
        vm.ImageIterator.InitiateWatcher(fileInfo);
        vm.ImageIterator.PreLoader.Clear();
        await vm.ImageIterator.LoadPicAtIndex(0).ConfigureAwait(false);
        if (GalleryFunctions.IsBottomGalleryOpen)
        {
            await GalleryLoad.LoadGallery(vm, fileInfo.DirectoryName);
        }
    }
}