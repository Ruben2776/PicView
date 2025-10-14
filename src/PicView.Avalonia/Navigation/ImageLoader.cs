using ImageMagick;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.ArchiveHandling;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.Gallery;
using PicView.Core.Http;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;

namespace PicView.Avalonia.Navigation;

public static class ImageLoader
{
    #region Load Pic From String

    /// <summary>
    ///     Loads a picture from a given string source, which can be a file path, directory path, or URL.
    /// </summary>
    public static async ValueTask LoadPicFromStringAsync(string source, MainViewModel vm, ImageIterator imageIterator)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }
        
        vm.MainWindow.IsLoadingIndicatorShown.Value = true;
        TitleManager.SetLoadingTitle(vm);

        var check = FileTypeResolver.CheckIfLoadableString(source);

        if (check == null)
        {
            await ErrorHandling.ReloadAsync(vm).ConfigureAwait(false);
            vm.MainWindow.IsLoadingIndicatorShown.Value = false;
            ArchiveExtraction.Cleanup();
            return;
        }

        switch (check.Value.Type)
        {
            case FileTypeResolver.LoadAbleFileType.File:
                vm.MainWindow.CurrentView.Value = vm.ImageViewer;
                await LoadPicFromFile(check.Value.Data, vm, imageIterator).ConfigureAwait(false);
                vm.MainWindow.IsLoadingIndicatorShown.Value = false;
                ArchiveExtraction.Cleanup();
                return;
            case FileTypeResolver.LoadAbleFileType.Directory:
                vm.MainWindow.CurrentView.Value = vm.ImageViewer;
                await LoadPicFromDirectoryAsync(check.Value.Data, vm).ConfigureAwait(false);
                vm.MainWindow.IsLoadingIndicatorShown.Value = false;
                ArchiveExtraction.Cleanup();
                return;
            case FileTypeResolver.LoadAbleFileType.Web:
                vm.MainWindow.CurrentView.Value = vm.ImageViewer;
                await LoadPicFromUrlAsync(check.Value.Data, vm, imageIterator).ConfigureAwait(false);
                vm.MainWindow.IsLoadingIndicatorShown.Value = false;
                ArchiveExtraction.Cleanup();
                return;
            case FileTypeResolver.LoadAbleFileType.Base64:
                vm.MainWindow.CurrentView.Value = vm.ImageViewer;
                await LoadPicFromBase64Async(check.Value.Data, vm, imageIterator).ConfigureAwait(false);
                vm.MainWindow.IsLoadingIndicatorShown.Value = false;
                ArchiveExtraction.Cleanup();
                return;
            case FileTypeResolver.LoadAbleFileType.Zip:
                vm.MainWindow.CurrentView.Value = vm.ImageViewer;
                await LoadPicFromArchiveAsync(check.Value.Data, vm, imageIterator).ConfigureAwait(false);
                vm.MainWindow.IsLoadingIndicatorShown.Value = false;
                return;
            default:
                await ErrorHandling.ReloadAsync(vm).ConfigureAwait(false);
                vm.MainWindow.IsLoadingIndicatorShown.Value = false;
                ArchiveExtraction.Cleanup();
                return;
        }
    }

    #endregion

    #region Load Pic From File

    /// <summary>
    /// Loads an image from a specified file and manages navigation within the directory or recreates the iterator.
    /// </summary>
    /// <param name="fileName">The full path of the file to load.</param>
    /// <param name="vm">The main view model instance associated with the application context.</param>
    /// <param name="imageIterator">An iterator for navigating through images in the directory.</param>
    /// <param name="fileInfo">Optional file information, defaults to a new <c>FileInfo</c> instance for the given file name if not provided.</param>
    public static async ValueTask LoadPicFromFile(string fileName, MainViewModel vm, ImageIterator imageIterator,
        FileInfo? fileInfo = null)
    {
        fileInfo ??= new FileInfo(fileName);
        if (!fileInfo.Exists)
        {
            return;
        }

        await CancelAsync().ConfigureAwait(false);

        if (imageIterator is not null)
        {
            // If image is in same directory as is being browsed, navigate to it. Otherwise, load without iterator.
            if (fileInfo.DirectoryName == imageIterator.InitialFileInfo.DirectoryName)
            {
                var index = imageIterator.ImagePaths.FindIndex(x => x.FullName.Equals(fileName));
                if (index != -1)
                {
                    _cancellationTokenSource ??= new CancellationTokenSource();
                    await imageIterator.IterateToIndex(index, _cancellationTokenSource).ConfigureAwait(false);
                    await NavigationManager.CheckIfTiffAndUpdate(vm, fileInfo, index);
                    if (Settings.Gallery.IsBottomGalleryShown && NavigationManager.GetCount > 0)
                    {
                        vm.Gallery.GalleryMode.Value = GalleryMode.ClosedToBottom;
                    }
                }
                else
                {
                    await LoadWithoutIterator();
                }
            }
            else
            {
                await LoadWithoutIterator();
            }
        }
        else
        {
            await LoadWithoutIterator();
        }

        return;

        async Task LoadWithoutIterator()
        {
            if (Settings.UIProperties.IsTaskbarProgressEnabled)
            {
                vm.PlatformService.StopTaskbarProgress();
            }

            await NavigationManager.LoadWithoutImageIterator(fileInfo, vm).ConfigureAwait(false);
        }
    }

    #endregion

    #region Load Pic From Directory

    /// <summary>
    /// Loads a picture from a directory.
    /// </summary>
    /// <param name="file">The path to the directory containing the picture.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <param name="fileInfo">Optional: FileInfo object for the directory.</param>
    public static async ValueTask LoadPicFromDirectoryAsync(string file, MainViewModel vm, FileInfo? fileInfo = null)
    {
        vm.MainWindow.IsLoadingIndicatorShown.Value = true;
        TitleManager.SetLoadingTitle(vm);

        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        }

        _cancellationTokenSource = new CancellationTokenSource();

        if (Settings.UIProperties.IsTaskbarProgressEnabled)
        {
            vm.PlatformService.StopTaskbarProgress();
        }

        fileInfo ??= new FileInfo(file);

        var newFileList = await Task.Run(() =>
        {
            var fileList = vm.PlatformService.GetFiles(fileInfo);
            if (fileList.Count > 0)
            {
                return fileList;
            }

            // Attempt to reload with subdirectories and reset the setting
            if (Settings.Sorting.IncludeSubDirectories)
            {
                return null;
            }

            Settings.Sorting.IncludeSubDirectories = true;
            fileList = vm.PlatformService.GetFiles(fileInfo);
            if (fileList.Count <= 0)
            {
                return null;
            }

            Settings.Sorting.IncludeSubDirectories = false;
            return fileList;
        }).ConfigureAwait(false);

        if (newFileList is null)
        {
            await ErrorHandling.ReloadAsync(vm).ConfigureAwait(false);
            return;
        }

        var firstFileInfo = newFileList[0];
        await NavigationManager.LoadWithoutImageIterator(firstFileInfo, vm, newFileList);
    }

    #endregion

    #region Load Pic From Archive

    /// <summary>
    ///     Asynchronously loads pictures from the specified archive file.
    /// </summary>
    /// <param name="path">The path to the archive file containing the picture(s) to load.</param>
    /// <param name="vm">The main view model instance used to manage UI state and operations.</param>
    /// <param name="imageIterator">The image iterator to use for navigation.</param>
    public static async ValueTask LoadPicFromArchiveAsync(string path, MainViewModel vm, ImageIterator imageIterator)
    {
        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        }

        vm.MainWindow.IsLoadingIndicatorShown.Value = true;
        TitleManager.SetLoadingTitle(vm);

        string? prevArchiveLocation = null;
        var previousArchiveExist = ArchiveExtraction.IsArchived;
        if (previousArchiveExist)
        {
            prevArchiveLocation = ArchiveExtraction.TempZipDirectory;
        }

        var extraction = await ArchiveExtraction
            .ExtractArchiveAsync(path, vm.PlatformService.ExtractWithLocalSoftwareAsync).ConfigureAwait(false);
        if (!extraction)
        {
            await ErrorHandling.ReloadAsync(vm);
            Clean();
            return;
        }

        if (Directory.Exists(ArchiveExtraction.TempZipDirectory))
        {
            var dirInfo = new DirectoryInfo(ArchiveExtraction.TempZipDirectory);
            if (dirInfo.EnumerateDirectories().Any())
            {
                var firstDir = dirInfo.EnumerateDirectories().First();
                var firstFile = firstDir.EnumerateFiles().First();
                await LoadPicFromFile(firstFile.FullName, vm, imageIterator, firstFile).ConfigureAwait(false);
            }
            else
            {
                await LoadPicFromDirectoryAsync(ArchiveExtraction.TempZipDirectory, vm).ConfigureAwait(false);
            }

            FileHistoryManager.Add(path);
            MainKeyboardShortcuts.ClearKeyDownModifiers(); // Fix possible modifier key state issue
            if (previousArchiveExist)
            {
                try
                {
                    Directory.Delete(prevArchiveLocation, true);
                }
                catch (Exception e)
                {
                    DebugHelper.LogDebug(nameof(ImageLoader), nameof(LoadPicFromArchiveAsync), e);
                }
            }
        }
        else
        {
            await imageIterator.DisposeAsync();
            await ErrorHandling.ReloadAsync(vm);
            Clean();
        }
        
        return;

        void Clean()
        {
            TempFileHelper.DeleteTempFiles();
            ArchiveExtraction.Cleanup();
        }
    }

    #endregion

    #region Load Pic From URL

    /// <summary>
    ///     Loads a picture from a given URL.
    /// </summary>
    /// <param name="url">The URL of the picture to load.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <param name="imageIterator">The image iterator to use for navigation.</param>
    public static async ValueTask LoadPicFromUrlAsync(string url, MainViewModel vm, ImageIterator imageIterator)
    {
        var tasks = new List<Task>();
        if (_cancellationTokenSource is not null)
        {
            tasks.Add(_cancellationTokenSource.CancelAsync());
        }

        string destination;

        try
        {
            vm.PlatformService.StopTaskbarProgress();

            var httpDownload = HttpManager.GetDownloadClient(url);
            using var client = httpDownload.Client;
            var fileName = Path.GetFileName(url);
            client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                if (totalFileSize is null || totalBytesDownloaded is null || progressPercentage is null)
                {
                    return;
                }

                var displayProgress = HttpManager.GetProgressDisplay(totalFileSize, totalBytesDownloaded,
                    progressPercentage);
                var title = $"{fileName} {TranslationManager.Translation.Downloading} {displayProgress}";
                vm.PicViewer.Title.Value = title;
                vm.PicViewer.TitleTooltip.Value = title;
                vm.PicViewer.WindowTitle.Value = title;
                if (Settings.UIProperties.IsTaskbarProgressEnabled)
                {
                    vm.PlatformService.SetTaskbarProgress((ulong)totalBytesDownloaded, (ulong)totalFileSize);
                }
            };
            tasks.Add(client.StartDownloadAsync());
            if (imageIterator is not null)
            {
                tasks.Add(imageIterator.DisposeAsync().AsTask());
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            destination = httpDownload.DownloadPath;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ImageLoader), nameof(LoadPicFromUrlAsync), e);
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
        await UpdateImage.SetSingleImageAsync(imageModel.Image, imageModel.ImageType, url, vm);

        vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        vm.PicViewer.FileInfo.Value = fileInfo;
        vm.PicViewer.ExifOrientation.Value = imageModel.Orientation;
        FileHistoryManager.Add(url);

        await NavigationManager.DisposeImageIteratorAsync();
        TempFileHelper.TempFilePath = destination;
    }

    #endregion

    #region Load Pic From Base64

    /// <summary>
    ///     Loads a picture from a Base64-encoded string.
    /// </summary>
    /// <param name="base64">The Base64-encoded string representing the picture.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <param name="imageIterator">The image iterator to use for navigation.</param>
    public static async ValueTask LoadPicFromBase64Async(string base64, MainViewModel vm, ImageIterator imageIterator)
    {
        TitleManager.SetLoadingTitle(vm);
        vm.MainWindow.IsLoadingIndicatorShown.Value = true;
        vm.PicViewer.ImageSource.Value = null;
        vm.PicViewer.FileInfo.Value = null;

        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        }

        await NavigationManager.DisposeImageIteratorAsync().ConfigureAwait(false);

        try
        {
            var magickImage = Base64Decoder.Base64ToMagickImage(base64);
            magickImage.Format = MagickFormat.Png;
            var bitmap = magickImage.ToWriteableBitmap();
            var imageModel = new ImageModel
            {
                Image = bitmap,
                PixelWidth = bitmap?.PixelSize.Width ?? 0,
                PixelHeight = bitmap?.PixelSize.Height ?? 0,
                ImageType = ImageType.Bitmap
            };
            await UpdateImage.SetSingleImageAsync(imageModel.Image, imageModel.ImageType,
                TranslationManager.Translation.Base64Image, vm);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ImageLoader), nameof(LoadPicFromBase64Async), e);
            await imageIterator.DisposeAsync();
            await ErrorHandling.ReloadAsync(vm);
        }
        vm.MainWindow.IsLoadingIndicatorShown.Value = false;
    }

    #endregion

    #region Cancellation

    private static CancellationTokenSource? _cancellationTokenSource;

    public static async ValueTask CancelAsync()
    {
        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        _cancellationTokenSource = new CancellationTokenSource();
    }

    #endregion

    #region Image Iterator Loading

    /// <inheritdoc cref="ImageIterator.NextIteration(NavigateTo, CancellationTokenSource)" />
    public static async ValueTask LastIterationAsync(ImageIterator imageIterator) =>
        await imageIterator
            .NextIteration(NavigateTo.Last, _cancellationTokenSource)
            .ConfigureAwait(false);

    /// <inheritdoc cref="ImageIterator.NextIteration(NavigateTo, CancellationTokenSource)" />
    public static async ValueTask FirstIterationAsync(ImageIterator imageIterator) =>
        await imageIterator
            .NextIteration(NavigateTo.First, _cancellationTokenSource)
            .ConfigureAwait(false);

    public static async ValueTask CheckCancellationAndStartIterateToIndex(int index, ImageIterator imageIterator,
        CancellationToken? cancellationToken)
    {
        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        }

        _cancellationTokenSource = new CancellationTokenSource();
        if (cancellationToken is not null)
        {
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Value, _cancellationTokenSource.Token);
        }
        await imageIterator.NextIteration(index, _cancellationTokenSource).ConfigureAwait(false);
    }

    public static async ValueTask IterateToIndexAsync(int index, ImageIterator imageIterator) =>
        await imageIterator.NextIteration(index, _cancellationTokenSource).ConfigureAwait(false);

    #endregion
}