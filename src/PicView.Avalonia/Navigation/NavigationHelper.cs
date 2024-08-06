using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Gallery;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
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

    public static async Task IterateButton(bool next, bool arrow, MainViewModel vm)
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
            string buttonName;
            if (arrow)
            {
                buttonName = next ? "ClickArrowRight" : "ClickArrowLeft";
            }
            else
            {
                buttonName = next ? "NextButton" : "PreviousButton";
            }
            Control control;
            if (arrow)
            {
                control = UIHelper.GetMainView.GetControl<UserControl>(buttonName);
            }
            else
            {
                control = UIHelper.GetBottomBar.GetControl<Button>(buttonName);
            }
            var point = arrow ? next ? new Point(65, 95) : new Point(15, 95) : new Point(50, 10);
            var p = control.PointToScreen(point);
            vm.PlatformService?.SetCursorPos(p.X, p.Y);
        });
    }
    
    public static async Task LoadPicFromStringAsync(string source, MainViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(source) || vm is null)
        {
            return;
        }
        
        vm.CurrentView = vm.ImageViewer;
        UIHelper.CloseMenus(vm);
        
        var fileInfo = new FileInfo(source);
        if (fileInfo.Exists)
        {
            if (fileInfo.IsSupported())
            {
                await LoadPicFromFile(fileInfo.FullName, vm, fileInfo).ConfigureAwait(false);
            }
            else if (fileInfo.IsArchive())
            {
                //await LoadPicFromArchiveAsync(path).ConfigureAwait(false);
            }
        }
        else if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
        {
            if (Directory.Exists(fileInfo.FullName))
            {
                await LoadPicFromDirectoryAsync(fileInfo.FullName, vm).ConfigureAwait(false);
            }
            if (source is not null && !string.IsNullOrWhiteSpace(source.GetURL()) ||
                !string.IsNullOrWhiteSpace(fileInfo.LinkTarget.GetURL()))
            {
                await LoadPicFromUrlAsync(source, vm);
            }
        }
    }
    
    public static async Task LoadPicFromFile(string fileName, MainViewModel vm, FileInfo? fileInfo = null)
    {
        if (vm is null)
        {
            return;
        }
        fileInfo ??= new FileInfo(fileName);
        if (!fileInfo.Exists)
        {
            return;
        }
        if (vm.ImageIterator is not null)
        {
            if (fileInfo.DirectoryName == vm.ImageIterator.FileInfo.DirectoryName)
            {
                var index = vm.ImageIterator.Pics.IndexOf(fileName);
                if (index != -1)
                {
                   await vm.ImageIterator.LoadPicAtIndex(index);
                   return;
                }
            }
        }
        var imageModel = await ImageHelper.GetImageModelAsync(fileInfo).ConfigureAwait(false);
        ExifHandling.SetImageModel(imageModel, vm);
        vm.ImageSource = imageModel;
        vm.ImageType = imageModel.ImageType;
        WindowHelper.SetSize(imageModel.PixelWidth, imageModel.PixelHeight, imageModel.Rotation, vm);
        vm.ImageIterator = new ImageIterator(fileInfo, vm);
        await vm.ImageIterator.LoadPicAtIndex(vm.ImageIterator.Pics.IndexOf(fileName));
        GalleryFunctions.Clear(vm);
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            if (vm.GalleryMode is GalleryMode.BottomToClosed or GalleryMode.FullToClosed or GalleryMode.Closed)
            {
                vm.GalleryMode = GalleryMode.ClosedToBottom;
            }
            await GalleryLoad.ReloadGalleryAsync(vm, fileInfo.DirectoryName);
        }
    }
    
    public static async Task GoToNextFolder(bool next, MainViewModel vm)
    {
        if (!CanNavigate(vm))
        {
            return;
        }
        SetTitleHelper.SetLoadingTitle(vm);
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
            SetTitleHelper.SetTitle(vm);
            return;
        }
        var fileInfo = new FileInfo(fileList[0]);
        vm.ImageIterator?.Dispose();
        vm.ImageIterator = new ImageIterator(fileInfo, fileList,0, vm);
        await vm.ImageIterator.LoadPicAtIndex(0).ConfigureAwait(false);
        GalleryFunctions.Clear(vm);
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            await GalleryLoad.ReloadGalleryAsync(vm, fileInfo.DirectoryName);
        }
    }
    
    public static async Task LoadPicFromUrlAsync(string url, MainViewModel vm)
    {
        string destination;

        try
        {
            var httpDownload = HttpNavigation.GetDownloadClient(url);
            using var client = httpDownload.Client;
            client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) => 
            {
                var displayProgress = HttpNavigation.GetProgressDisplay(totalFileSize, totalBytesDownloaded,
                    progressPercentage);
                vm.Title = displayProgress;
                vm.TitleTooltip = displayProgress;
                vm.WindowTitle = displayProgress;
            };
            await client.StartDownloadAsync();
            destination = httpDownload.DownloadPath;
        }
        catch (Exception e)
        {
#if DEBUG
            Console.WriteLine("LoadPicFromUrlAsync exception = \n" + e.Message);
#endif
            ErrorHandling.ShowStartUpMenu(vm);

            return;
        }

        var check = ErrorHelper.CheckIfLoadableString(destination);
        switch (check)
        {
            default:
                var fileInfo = new FileInfo(check);
                var imageModel = await ImageHelper.GetImageModelAsync(fileInfo).ConfigureAwait(false);
                SetSingleImage(imageModel.Image as Bitmap, url, vm);
                vm.FileInfo = fileInfo;
                ExifHandling.SetImageModel(imageModel, vm);
                ExifHandling.UpdateExifValues(imageModel, vm);
                //FileHistoryNavigation.Add(url);
            break;
            case "base64":
                // TOD - base64
                break;

            case "zip":
                // TOD - zip
                //FileHistoryNavigation.Add(url);
                break;

            case "directory":
            case "":
                ErrorHandling.ShowStartUpMenu(vm);
                return;
        }
    }
    
    public static async Task LoadPicFromBase64Async(string base64, MainViewModel vm)
    {
        vm.ImageIterator = null;
        vm.ImageSource = null;
        vm.IsLoading = true;
        SetTitleHelper.SetLoadingTitle(vm);
        vm.FileInfo = null;
        await Task.Run(async () =>
        {
            var magickImage = await ImageDecoder.Base64ToMagickImage(base64).ConfigureAwait(false);
            magickImage.Format = MagickFormat.Png;
            await using var memoryStream = new MemoryStream();
            await magickImage.WriteAsync(memoryStream);
            memoryStream.Position = 0;
            var bitmap = new Bitmap(memoryStream);
            var imageModel = new ImageModel
            {
                Image = bitmap,
                PixelWidth = bitmap?.PixelSize.Width ?? 0,
                PixelHeight = bitmap?.PixelSize.Height ?? 0,
                ImageType = ImageType.Bitmap
            };
            SetSingleImage(imageModel.Image as Bitmap, TranslationHelper.Translation.Base64Image, vm);
            ExifHandling.SetImageModel(imageModel, vm);
            ExifHandling.UpdateExifValues(imageModel, vm);
        });
        vm.IsLoading = false;
    }

    public static async Task LoadPicFromDirectoryAsync(string file, MainViewModel vm, FileInfo? fileInfo = null)
    {
        fileInfo ??= new FileInfo(file);
        var imageModel = await ImageHelper.GetImageModelAsync(fileInfo).ConfigureAwait(false);
        ExifHandling.SetImageModel(imageModel, vm);
        vm.ImageSource = imageModel;
        vm.ImageType = imageModel.ImageType;
        WindowHelper.SetSize(imageModel.PixelWidth, imageModel.PixelHeight, imageModel.Rotation, vm);
        vm.ImageIterator = new ImageIterator(fileInfo, vm);
        await vm.ImageIterator.LoadPicAtIndex(vm.ImageIterator.Index);
        GalleryFunctions.Clear(vm);
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            await GalleryLoad.ReloadGalleryAsync(vm, fileInfo.DirectoryName);
        }
    }
    
    public static void SetSingleImage(Bitmap bitmap, string name, MainViewModel vm)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (vm.CurrentView != vm.ImageViewer)
            {
                vm.CurrentView = vm.ImageViewer;
            }
        }, DispatcherPriority.Render);

        vm.ImageIterator = null;
        vm.ImageSource = bitmap;
        vm.ImageType = ImageType.Bitmap;
        var width = bitmap.PixelSize.Width;
        var height = bitmap.PixelSize.Height;

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
        WindowHelper.SetSize(width, height, vm);
        if (vm.RotationAngle != 0)
        {
            vm.ImageViewer.Rotate(vm.RotationAngle);
        }
        var titleString = TitleHelper.TitleString(width, height, name, 1);
        vm.WindowTitle = titleString[0];
        vm.Title = titleString[1];
        vm.TitleTooltip = titleString[1];
    }


}