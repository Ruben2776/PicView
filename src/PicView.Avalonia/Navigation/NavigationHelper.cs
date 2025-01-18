using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Crop;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.ArchiveHandling;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Gallery;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Navigation;

namespace PicView.Avalonia.Navigation;

/// <summary>
/// Helper class for navigation and image loading functionalities in the application.
/// </summary>
public static class NavigationHelper
{
    private static CancellationTokenSource? _cancellationTokenSource;

    #region Navigation

    /// <summary>
    /// Determines whether navigation is possible based on the current state of the <see cref="MainViewModel"/>.
    /// </summary>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>True if navigation is possible, otherwise false.</returns>
    public static bool CanNavigate(MainViewModel vm)
    {
        return vm?.ImageIterator?.ImagePaths is not null &&
               vm.ImageIterator.ImagePaths.Count > 0 && !CropFunctions.IsCropping &&
               !UIHelper.IsDialogOpen && !vm.IsEditableTitlebarOpen;
        // TODO: should probably turn this into CanExecute observable for ReactiveUI
    }

    /// <summary>
    /// Navigates to the next or previous image based on the <paramref name="next"/> parameter.
    /// </summary>
    /// <param name="next">True to navigate to the next image, false for the previous image.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task Navigate(bool next, MainViewModel vm)
    {
        if (!CanNavigate(vm))
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            await ScrollGallery(next);
        }
        else
        {
            var navigateTo = next ? NavigateTo.Next : NavigateTo.Previous;
            await CheckCancellationAndStartNextIteration(navigateTo, vm).ConfigureAwait(false);
        }
    }

    public static async Task Navigate(int index, MainViewModel vm)
    {
        if (!CanNavigate(vm))
        {
            return;
        }
        
        await CheckCancellationAndStartIterateToIndex(index, vm).ConfigureAwait(false);
    }

    public static async Task Next10(MainViewModel vm)
    {
        if (!CanNavigate(vm))
        {
            return;
        }
        var currentIndex = vm.ImageIterator.CurrentIndex;
        var index = vm.ImageIterator.GetIteration(currentIndex, NavigateTo.Next, false, true);
        await CheckCancellationAndStartIterateToIndex(index, vm).ConfigureAwait(false);
    }

    public static async Task Next100(MainViewModel vm)
    {
        if (!CanNavigate(vm))
        {
            return;
        }
        var currentIndex = vm.ImageIterator.CurrentIndex;
        var index = vm.ImageIterator.GetIteration(currentIndex, NavigateTo.Next, false, false, true);
        await CheckCancellationAndStartIterateToIndex(index, vm).ConfigureAwait(false);
    }

    public static async Task Prev10(MainViewModel vm)
    {
        if (!CanNavigate(vm))
        {
            return;
        }
        var currentIndex = vm.ImageIterator.CurrentIndex;
        var index = vm.ImageIterator.GetIteration(currentIndex, NavigateTo.Previous, false, true);
        await CheckCancellationAndStartIterateToIndex(index, vm).ConfigureAwait(false);
    }

    public static async Task Prev100(MainViewModel vm)
    {
        if (!CanNavigate(vm))
        {
            return;
        }
        var currentIndex = vm.ImageIterator.CurrentIndex;
        var index = vm.ImageIterator.GetIteration(currentIndex, NavigateTo.Previous, false, false, true);
        await CheckCancellationAndStartIterateToIndex(index, vm).ConfigureAwait(false);
    }

    /// <summary>
    /// Navigates to the first or last image in the collection.
    /// </summary>
    /// <param name="last">True to navigate to the last image, false to navigate to the first image.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task NavigateFirstOrLast(bool last, MainViewModel vm)
    {
        if (!CanNavigate(vm))
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            GalleryNavigation.NavigateGallery(last, vm);
        }
        else
        {
            if (_cancellationTokenSource is not null)
            {
                await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
            }

            _cancellationTokenSource = new CancellationTokenSource();
            await vm.ImageIterator.NextIteration(last ? NavigateTo.Last : NavigateTo.First, _cancellationTokenSource)
                .ConfigureAwait(false);
            await ScrollToEndIfNecessary(last);
        }
    }

    /// <summary>
    /// Iterates to the next or previous image based on the <paramref name="next"/> parameter.
    /// </summary>
    /// <param name="next">True to iterate to the next image, false for the previous image.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task Iterate(bool next, MainViewModel vm)
    {
        if (!CanNavigate(vm))
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            GalleryNavigation.NavigateGallery(next ? Direction.Right : Direction.Left, vm);
        }
        else
        {
            await Navigate(next, vm);
        }
    }

    /// <summary>
    /// Navigates and moves the cursor to the corresponding button.
    /// </summary>
    /// <param name="next">True to navigate to the next image, false for the previous image.</param>
    /// <param name="arrow">True to move cursor to the arrow, false for the button.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task NavigateAndPositionCursor(bool next, bool arrow, MainViewModel vm)
    {
        if (!CanNavigate(vm))
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            await ScrollGallery(next);
        }
        else
        {
            await Navigate(next, vm);
            await MoveCursorOnButtonClick(next, arrow, vm);
        }
    }

    /// <summary>
    /// Navigates to the next or previous folder and loads the first image in that folder.
    /// </summary>
    /// <param name="next">True to navigate to the next folder, false for the previous folder.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task GoToNextFolder(bool next, MainViewModel vm)
    {
        if (!CanNavigate(vm))
        {
            return;
        }

        SetTitleHelper.SetLoadingTitle(vm);
        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync();
        }

        var fileList = await GetNextFolderFileList(next, vm).ConfigureAwait(false);

        if (fileList is null)
        {
            SetTitleHelper.SetTitle(vm);
        }
        else
        {
            vm.PlatformService.StopTaskbarProgress();
            await PreviewPicAndLoadGallery(new FileInfo(fileList[0]), vm, fileList);
        }
    }

    public static async Task DuplicateAndNavigate(MainViewModel vm)
    {
        if (!CanNavigate(vm))
        {
            return;
        }
        vm.IsLoading = true;
        var oldPath = vm.FileInfo.FullName;
        await Task.FromResult(FileHelper.DuplicateAndReturnFileName(oldPath)).ConfigureAwait(false);
    }

    #endregion

    #region Load pictures from string, file or url

    /// <summary>
    /// Loads a picture from a given string source, which can be a file path, directory path, or URL.
    /// </summary>
    /// <param name="source">The string source to load the picture from.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task LoadPicFromStringAsync(string source, MainViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(source) || vm is null)
        {
            return;
        }

        UIHelper.CloseMenus(vm);
        vm.IsLoading = true;
        SetTitleHelper.SetLoadingTitle(vm);

        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        }

        _cancellationTokenSource = new CancellationTokenSource();

        // Starting in new task makes it more responsive and works better
        await Task.Run(async () =>
        {
            var check = ErrorHelper.CheckIfLoadableString(source);

            switch (check)
            {
                default:
                    // Navigate to the image if it exists in the image iterator
                    if (vm.ImageIterator is not null)
                    {
                        if (vm.ImageIterator.ImagePaths.Contains(check))
                        {
                            await vm.ImageIterator.IterateToIndex(vm.ImageIterator.ImagePaths.IndexOf(check),
                                    _cancellationTokenSource)
                                .ConfigureAwait(false);
                            return;
                        }
                    }

                    vm.CurrentView = vm.ImageViewer;
                    await LoadPicFromFile(check, vm).ConfigureAwait(false);
                    vm.IsLoading = false;
                    ArchiveExtraction.Cleanup();
                    return;

                case "web":
                    vm.CurrentView = vm.ImageViewer;
                    await LoadPicFromUrlAsync(source, vm).ConfigureAwait(false);
                    vm.IsLoading = false;
                    ArchiveExtraction.Cleanup();
                    return;

                case "base64":
                    vm.CurrentView = vm.ImageViewer;
                    await LoadPicFromBase64Async(source, vm).ConfigureAwait(false);
                    vm.IsLoading = false;
                    ArchiveExtraction.Cleanup();
                    return;

                case "directory":
                    vm.CurrentView = vm.ImageViewer;
                    await LoadPicFromDirectoryAsync(source, vm).ConfigureAwait(false);
                    vm.IsLoading = false;
                    ArchiveExtraction.Cleanup();
                    return;

                case "zip":
                    vm.CurrentView = vm.ImageViewer;
                    await LoadPicFromArchiveAsync(source, vm).ConfigureAwait(false);
                    vm.IsLoading = false;
                    return;

                case "":
                    await ErrorHandling.ReloadAsync(vm).ConfigureAwait(false);
                    vm.IsLoading = false;
                    ArchiveExtraction.Cleanup();
                    return;
            }
        });
    }

    /// <summary>
    /// Loads a picture from a given file.
    /// </summary>
    /// <param name="fileName">The file name of the picture to load.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <param name="fileInfo">Optional: FileInfo object for the file.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        }

        _cancellationTokenSource = new CancellationTokenSource();

        if (vm.ImageIterator is not null)
        {
            if (fileInfo.DirectoryName == vm.ImageIterator.InitialFileInfo.DirectoryName)
            {
                var index = vm.ImageIterator.ImagePaths.IndexOf(fileInfo.FullName);
                if (index != -1)
                {
                    await vm.ImageIterator.IterateToIndex(index, _cancellationTokenSource).ConfigureAwait(false);
                }
                else
                {
                    await ErrorHandling.ReloadAsync(vm);
                }
            }
            else
            {
                await PreviewPicAndLoadGallery(fileInfo, vm);
            }
        }
        else
        {
            if (SettingsHelper.Settings.UIProperties.IsTaskbarProgressEnabled)
            {
                vm.PlatformService.StopTaskbarProgress();
            }
            await PreviewPicAndLoadGallery(fileInfo, vm);
        }
    }

    /// <summary>
    /// Asynchronously loads a picture from a specified archive file.
    /// </summary>
    /// <param name="path">The path to the archive file containing the picture(s) to load.</param>
    /// <param name="vm">The main view model instance used to manage UI state and operations.</param>
    /// <returns>
    /// A task representing the asynchronous operation. This task completes when the picture is loaded
    /// from the archive or when an error occurs during the extraction or loading process.
    /// </returns>
    public static async Task LoadPicFromArchiveAsync(string path, MainViewModel vm)
    {
        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync();
        }

        var extraction = await ArchiveExtraction
            .ExtractArchiveAsync(path, vm.PlatformService.ExtractWithLocalSoftwareAsync).ConfigureAwait(false);
        if (!extraction)
        {
            await ErrorHandling.ReloadAsync(vm);
            return;
        }

        if (Directory.Exists(ArchiveExtraction.TempZipDirectory))
        {
            var dirInfo = new DirectoryInfo(ArchiveExtraction.TempZipDirectory);
            if (dirInfo.EnumerateDirectories().Any())
            {
                var firstDir = dirInfo.EnumerateDirectories().First();
                var firstFile = firstDir.EnumerateFiles().First();
                await LoadPicFromFile(firstFile.FullName, vm, firstFile).ConfigureAwait(false);
            }
            else
            {
                await LoadPicFromDirectoryAsync(ArchiveExtraction.TempZipDirectory, vm).ConfigureAwait(false);
            }

            MainKeyboardShortcuts.ClearKeyDownModifiers(); // Fix possible modifier key state issue
        }
        else
        {
            await ErrorHandling.ReloadAsync(vm);
        }
    }

    /// <summary>
    /// Loads a picture from a given URL.
    /// </summary>
    /// <param name="url">The URL of the picture to load.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task LoadPicFromUrlAsync(string url, MainViewModel vm)
    {
        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync();
        }

        string destination;

        try
        {
            vm.PlatformService.StopTaskbarProgress();

            var httpDownload = HttpNavigation.GetDownloadClient(url);
            using var client = httpDownload.Client;
            client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                if (totalFileSize is null || totalBytesDownloaded is null || progressPercentage is null)
                {
                    return;
                }

                var displayProgress = HttpNavigation.GetProgressDisplay(totalFileSize, totalBytesDownloaded,
                    progressPercentage);
                vm.Title = displayProgress;
                vm.TitleTooltip = displayProgress;
                vm.WindowTitle = displayProgress;
                if (SettingsHelper.Settings.UIProperties.IsTaskbarProgressEnabled)
                {
                    vm.PlatformService.SetTaskbarProgress((ulong)totalBytesDownloaded, (ulong)totalFileSize);
                }
            };
            await client.StartDownloadAsync().ConfigureAwait(false);
            destination = httpDownload.DownloadPath;
        }
        catch (Exception e)
        {
#if DEBUG
            Console.WriteLine("LoadPicFromUrlAsync exception = \n" + e.Message);
#endif
            await TooltipHelper.ShowTooltipMessageAsync(e.Message, true);
            await ErrorHandling.ReloadAsync(vm);

            return;
        }

        var fileInfo = new FileInfo(destination);
        if (!fileInfo.Exists)
        {
            await ErrorHandling.ReloadAsync(vm);
            return;
        }

        var imageModel = await GetImageModel.GetImageModelAsync(fileInfo).ConfigureAwait(false);
        UpdateImage.SetSingleImage(imageModel.Image, imageModel.ImageType, url, vm);
        vm.FileInfo = fileInfo;
        vm.ExifOrientation = imageModel.EXIFOrientation;
        FileHistoryNavigation.Add(url);

        vm.IsLoading = false;
    }

    /// <summary>
    /// Loads a picture from a Base64-encoded string.
    /// </summary>
    /// <param name="base64">The Base64-encoded string representing the picture.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task LoadPicFromBase64Async(string base64, MainViewModel vm)
    {
        vm.ImageIterator = null;
        vm.ImageSource = null;
        vm.IsLoading = true;
        SetTitleHelper.SetLoadingTitle(vm);
        vm.FileInfo = null;
        await Task.Run(async () =>
        {
            // TODO: Handle base64 if it's SVG image
            try
            {
                var magickImage = ImageDecoder.Base64ToMagickImage(base64);
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
                UpdateImage.SetSingleImage(imageModel.Image, imageModel.ImageType,
                    TranslationHelper.Translation.Base64Image, vm);
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine("LoadPicFromBase64Async exception = \n" + e.Message);
#endif
                if (vm.FileInfo is not null && vm.FileInfo.Exists)
                {
                    await LoadPicFromFile(vm.FileInfo.FullName, vm, vm.FileInfo);
                }
                else
                {
                    ErrorHandling.ShowStartUpMenu(vm);
                }
            }
        });
        vm.IsLoading = false;
    }

    /// <summary>
    /// Loads a picture from a directory.
    /// </summary>
    /// <param name="file">The path to the directory containing the picture.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <param name="fileInfo">Optional: FileInfo object for the directory.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task LoadPicFromDirectoryAsync(string file, MainViewModel vm, FileInfo? fileInfo = null)
    {
        vm.IsLoading = true;
        SetTitleHelper.SetLoadingTitle(vm);

        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        }

        _cancellationTokenSource = new CancellationTokenSource();

        if (SettingsHelper.Settings.UIProperties.IsTaskbarProgressEnabled)
        {
            vm.PlatformService.StopTaskbarProgress();
        }

        fileInfo ??= new FileInfo(file);
        vm.ImageIterator?.Dispose();
        var fileList = await Task.FromResult(vm.PlatformService.GetFiles(fileInfo)).ConfigureAwait(false);
        if (fileList.Count <= 0)
        {
            // Attempt to reload with subdirectories and reset the setting
            if (!SettingsHelper.Settings.Sorting.IncludeSubDirectories)
            {
                SettingsHelper.Settings.Sorting.IncludeSubDirectories = true;
                fileList = await Task.FromResult(vm.PlatformService.GetFiles(fileInfo)).ConfigureAwait(false);
                if (fileList.Count <= 0)
                {
                    await ErrorHandling.ReloadAsync(vm).ConfigureAwait(false);
                    return;
                }

                SettingsHelper.Settings.Sorting.IncludeSubDirectories = false;
            }
            else
            {
                await ErrorHandling.ReloadAsync(vm).ConfigureAwait(false);
                return;
            }
        }

        vm.ImageIterator = new ImageIterator(fileInfo, fileList, 0, vm);
        await vm.ImageIterator.IterateToIndex(0, _cancellationTokenSource).ConfigureAwait(false);
        await CheckAndReloadGallery(fileInfo, vm);
    }

    #endregion

    #region Private helpers
    
    private static async Task CheckCancellationAndStartNextIteration(NavigateTo navigateTo, MainViewModel vm)
    {
        await Task.Run(async () =>
        {
            if (_cancellationTokenSource is not null)
            {
                await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
            }

            _cancellationTokenSource = new CancellationTokenSource();
            await vm.ImageIterator.NextIteration(navigateTo, _cancellationTokenSource).ConfigureAwait(false);
            _cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(5));
        }).ConfigureAwait(false);
    }
    
    private static async Task CheckCancellationAndStartIterateToIndex(int index, MainViewModel vm)
    {
        await Task.Run(async () =>
        {
            if (_cancellationTokenSource is not null)
            {
                await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
            }

            _cancellationTokenSource = new CancellationTokenSource();
            await vm.ImageIterator.IterateToIndex(index, _cancellationTokenSource).ConfigureAwait(false);
            _cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(5));
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the list of files in the next or previous folder.
    /// </summary>
    /// <param name="next">True to get the next folder, false for the previous folder.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation that returns a list of file paths.</returns>
    private static async Task<List<string>?> GetNextFolderFileList(bool next, MainViewModel vm)
    {
        return await Task.Run(() =>
        {
            var indexChange = next ? 1 : -1;
            var currentFolder = Path.GetDirectoryName(vm.ImageIterator?.ImagePaths[vm.ImageIterator.CurrentIndex]);
            var parentFolder = Path.GetDirectoryName(currentFolder);
            var directories = Directory.GetDirectories(parentFolder, "*", SearchOption.TopDirectoryOnly);
            var directoryIndex = Array.IndexOf(directories, currentFolder);
            if (SettingsHelper.Settings.UIProperties.Looping)
            {
                directoryIndex = (directoryIndex + indexChange + directories.Length) % directories.Length;
            }
            else
            {
                directoryIndex += indexChange;
                if (directoryIndex < 0 || directoryIndex >= directories.Length)
                {
                    return null;
                }
            }

            for (var i = directoryIndex; i < directories.Length; i++)
            {
                var fileInfo = new FileInfo(directories[i]);
                var fileList = vm.PlatformService.GetFiles(fileInfo);
                if (fileList is { Count: > 0 })
                {
                    return fileList;
                }
            }

            return null;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Previews the picture and loads the gallery with the specified files.
    /// </summary>
    /// <param name="fileInfo">The file info of the picture to preview.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <param name="files">Optional: List of file paths in the gallery.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task PreviewPicAndLoadGallery(FileInfo fileInfo, MainViewModel vm, List<string>? files = null)
    {
        var imageModel = await GetImageModel.GetImageModelAsync(fileInfo).ConfigureAwait(false);
        vm.ImageSource = imageModel.Image;
        vm.ImageType = imageModel.ImageType;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            WindowResizing.SetSize(imageModel.PixelWidth, imageModel.PixelHeight, 0, 0, imageModel.Rotation, vm);
        });

        if (files is null)
        {
            vm.ImageIterator = new ImageIterator(fileInfo, vm);
            await vm.ImageIterator.IterateToIndex(vm.ImageIterator.ImagePaths.IndexOf(fileInfo.FullName),
                _cancellationTokenSource);
        }
        else
        {
            vm.ImageIterator = new ImageIterator(fileInfo, files, 0, vm);
            await vm.ImageIterator.IterateToIndex(0, _cancellationTokenSource);
        }

        await CheckAndReloadGallery(fileInfo, vm);
    }

    /// <summary>
    /// Checks and reloads the gallery if necessary based on the provided file info.
    /// </summary>
    /// <param name="fileInfo">The file info to check.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task CheckAndReloadGallery(FileInfo fileInfo, MainViewModel vm)
    {
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown || GalleryFunctions.IsFullGalleryOpen)
        {
            GalleryFunctions.Clear();

            // Check if the bottom gallery should be shown
            if (!GalleryFunctions.IsFullGalleryOpen)
            {
                if (vm.GalleryMode is GalleryMode.BottomToClosed or GalleryMode.FullToClosed or GalleryMode.Closed)
                {
                    // Trigger animation to show it
                    vm.GalleryMode = GalleryMode.ClosedToBottom;
                }
            }

            await GalleryLoad.ReloadGalleryAsync(vm, fileInfo.DirectoryName);
        }
    }

    /// <summary>
    /// Scrolls the gallery to the next or previous page.
    /// </summary>
    /// <param name="next">True to scroll to the next page, false for the previous page.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task ScrollGallery(bool next)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (next)
            {
                UIHelper.GetGalleryView.GalleryListBox.PageRight();
            }
            else
            {
                UIHelper.GetGalleryView.GalleryListBox.PageLeft();
            }
        });
    }

    /// <summary>
    /// Scrolls to the end of the gallery if the <paramref name="last"/> parameter is true.
    /// </summary>
    /// <param name="last">True to scroll to the end of the gallery.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task ScrollToEndIfNecessary(bool last)
    {
        if (last && SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            await Dispatcher.UIThread.InvokeAsync(() => { UIHelper.GetGalleryView.GalleryListBox.ScrollToEnd(); });
        }
    }

    /// <summary>
    /// Moves the cursor on the navigation button.
    /// </summary>
    /// <param name="next">True to move the cursor to the next button, false for the previous button.</param>
    /// <param name="arrow">True to move the cursor on the arrow, false to move the cursor on the button.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task MoveCursorOnButtonClick(bool next, bool arrow, MainViewModel vm)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var buttonName = GetNavigationButtonName(next, arrow);
            var control = GetButtonControl(buttonName, arrow);
            var point = GetClickPoint(next, arrow);
            var p = control.PointToScreen(point);
            vm.PlatformService?.SetCursorPos(p.X, p.Y);
        });
    }

    /// <summary>
    /// Gets the name of the navigation button based on input parameters.
    /// </summary>
    /// <param name="next">True for the next button, false for the previous button.</param>
    /// <param name="arrow">True if the navigation uses arrow keys.</param>
    /// <returns>The name of the navigation button.</returns>
    private static string GetNavigationButtonName(bool next, bool arrow)
    {
        return arrow
            ? next ? "ClickArrowRight" : "ClickArrowLeft"
            : next
                ? "NextButton"
                : "PreviousButton";
    }

    /// <summary>
    /// Gets the control associated with the specified button name.
    /// </summary>
    /// <param name="buttonName">The name of the button.</param>
    /// <param name="arrow">True if the control is an arrow button.</param>
    /// <returns>The control associated with the button.</returns>
    private static Control GetButtonControl(string buttonName, bool arrow)
    {
        return arrow
            ? UIHelper.GetMainView.GetControl<UserControl>(buttonName)
            : UIHelper.GetBottomBar.GetControl<Button>(buttonName);
    }

    /// <summary>
    /// Gets the point to click on the button based on the input parameters.
    /// </summary>
    /// <param name="next">True for the next button, false for the previous button.</param>
    /// <param name="arrow">True if the navigation uses arrow keys.</param>
    /// <returns>The point to click on the button.</returns>
    private static Point GetClickPoint(bool next, bool arrow)
    {
        return arrow
            ? next ? new Point(65, 95) : new Point(15, 95)
            : new Point(50, 10);
    }

    #endregion
}