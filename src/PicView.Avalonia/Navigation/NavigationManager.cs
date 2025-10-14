using System.Runtime.InteropServices;
using Avalonia.Threading;
using PicView.Avalonia.Crop;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.FileHistory;
using PicView.Core.Gallery;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Preloading;

namespace PicView.Avalonia.Navigation;

/// <summary>
///     Manages image navigation within the application.
/// </summary>
public static class NavigationManager
{
    public static TiffManager.TiffNavigationInfo? TiffNavigationInfo { get; private set; }

    // Should be updated to handle multiple iterators in the future when adding tab support
    public static ImageIterator? ImageIterator { get; private set; }

    #region Navigation

    /// <summary>
    /// Loads a picture from a given file, reloads the ImageIterator and loads the corresponding gallery from the file's
    /// directory.
    /// </summary>
    /// <param name="fileInfo">The FileInfo object representing the file to load.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <param name="files">
    /// Optional: The list of file paths to load. If null, the list is loaded from the given file's
    /// directory.
    /// </param>
    /// <param name="index">Optional: The index at which to start the navigation. Defaults to 0.</param>
    public static async ValueTask LoadWithoutImageIterator(FileInfo fileInfo, MainViewModel vm, List<FileInfo>? files = null,
        int index = 0)
    {
        _ = Task.Run(GalleryLoad.CancelGalleryLoadAsync);
        
        var imageModel = await GetImageModel.GetImageModelAsync(fileInfo).ConfigureAwait(false);
        ImageModel? nextImageModel = null;
        vm.PicViewer.ImageSource.Value = imageModel.Image;
        vm.PicViewer.ImageType.Value = imageModel.ImageType;
        if (!Settings.ImageScaling.ShowImageSideBySide)
        {
            var size = WindowResizing.GetSize(imageModel.PixelWidth, imageModel.PixelHeight, 0, 0, imageModel.Rotation, vm );
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                vm.ImageViewer.SetTransform(imageModel.Orientation, imageModel.Format);
                if (size.HasValue)
                {
                    WindowResizing.SetSize(size.Value,
                        vm);
                }
                else
                {
                    WindowResizing.GetSize(vm);
                }
            });
        }

        await DisposeImageIteratorAsync();

        if (files is null)
        {
            ImageIterator = new ImageIterator(fileInfo, vm);
            index = ImageIterator.CurrentIndex;
            if (index == -1)
            {
                await UpdateImage.SetSingleImageAsync(imageModel.Image, imageModel.ImageType,
                    TranslationManager.Translation.ClipboardImage, vm);
                return;
            }
        }
        else
        {
            ImageIterator = new ImageIterator(fileInfo, files, index, vm);
        }

        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            nextImageModel = (await ImageIterator.GetNextPreLoadValueAsync()).ImageModel;
            vm.PicViewer.SecondaryImageSource.Value = nextImageModel.Image;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                WindowResizing.SetSize(imageModel.PixelWidth, imageModel.PixelHeight, nextImageModel.PixelWidth,
                    nextImageModel.PixelHeight, imageModel.Rotation, vm);
            });

            TitleManager.SetSideBySideTitle(vm, imageModel, nextImageModel);
            UpdateImage.SetStats(vm, imageModel);

            // Fixes incorrect rendering in the side by side view
            // TODO: Improve and fix side by side and remove this hack 
            Dispatcher.UIThread.Post(() => { vm.ImageViewer?.MainImage?.InvalidateVisual(); });
        }
        else
        {
            var isTiffUpdated = await CheckIfTiffAndUpdate(vm, fileInfo, index);
            if (!isTiffUpdated)
            {
                if (Settings.ImageScaling.ShowImageSideBySide)
                {
                    TitleManager.SetSideBySideTitle(vm, imageModel, nextImageModel);
                }
                else
                {
                    TitleManager.SetTitle(vm, imageModel);
                }

                UpdateImage.SetStats(vm, imageModel);
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            await WindowFunctions.ResizeAndFixRenderingError(vm);
        }

        vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        if (ImageIterator.ImagePaths.Count > 0 && index > ImageIterator.ImagePaths.Count)
        {
            FileHistoryManager.Add(ImageIterator.ImagePaths[index].FullName);
            if (Settings.ImageScaling.ShowImageSideBySide)
            {
                FileHistoryManager.Add(
                    ImageIterator.ImagePaths[ImageIterator.GetIteration(index, NavigateTo.Next)].FullName);
            }
        }

        await GalleryLoad.CheckAndReloadGallery(fileInfo, vm);
    }


    /// <summary>
    /// Determines whether navigation is possible based on the current state of the <see cref="MainViewModel" />.
    /// </summary>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>True if navigation is possible, otherwise false.</returns>
    public static bool CanNavigate(MainViewModel vm) =>
        ImageIterator?.ImagePaths is not null &&
        ImageIterator.ImagePaths.Count > 0 && !CropFunctions.IsCropping &&
        !DialogManager.IsDialogOpen && vm is { MainWindow.IsEditableTitlebarOpen.CurrentValue: false, PicViewer.FileInfo.CurrentValue: not null };

    /// <summary>
    /// Navigates to the next or previous image based on the specified direction, ensuring all conditions for navigation are met.
    /// </summary>
    /// <param name="next">Indicates the navigation direction. True for next image and false for previous image.</param>
    /// <param name="vm">The main view model instance used for managing the application's state and data.</param>
    /// <param name="cancellationToken">Optional: A cancellation token to handle task cancellation during navigation operations.</param>
    /// <returns>A ValueTask representing the asynchronous navigation operation.</returns>
    public static async ValueTask Navigate(bool next, MainViewModel vm, CancellationToken? cancellationToken)
    {
        if (!CanNavigate(vm))
        {
            if (vm.PicViewer.FileInfo is null && ImageIterator is not null)
            {
                // Fixes issue that shouldn't happen. Should investigate.
                vm.PicViewer.FileInfo.Value = ImageIterator.ImagePaths[0];
            }
            else
            {
                return;
            }
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            await GalleryNavigation.ScrollGallery(next);
            return;
        }

        var navigateTo = next ? NavigateTo.Next : NavigateTo.Previous;
        int nextIteration;

        if (ImageIterator.CurrentIndex < 0 || ImageIterator.CurrentIndex >= ImageIterator.ImagePaths.Count)
        {
            if (vm.PicViewer.FileInfo is not null)
            {
                var newIndex =
                    ImageIterator.ImagePaths.FindIndex(x => x.FullName.Equals(vm.PicViewer.FileInfo.CurrentValue.FullName));
                if (newIndex == -1)
                {
                    ErrorHandling.ShowStartUpMenu(vm);
                    return;
                }

                nextIteration = newIndex;
            }
            else
            {
                ErrorHandling.ShowStartUpMenu(vm);
                return;
            }
        }
        else
        {
            nextIteration = ImageIterator.GetIteration(ImageIterator.CurrentIndex, navigateTo);
        }

        var currentFileName = ImageIterator.ImagePaths[ImageIterator.CurrentIndex].FullName;
        if (TiffManager.IsTiff(currentFileName))
        {
            await TiffNavigation(vm, currentFileName, nextIteration).ConfigureAwait(false);
        }
        else
        {
            await ImageLoader.CheckCancellationAndStartIterateToIndex(nextIteration, ImageIterator, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async ValueTask TiffNavigation(MainViewModel vm, string currentFileName, int nextIteration)
    {
        if (TiffNavigationInfo is null && !ImageIterator.IsReversed)
        {
            var tiffPages = await Task.FromResult(TiffManager.LoadTiffPages(currentFileName)).ConfigureAwait(false);
            if (tiffPages.Count < 1)
            {
                await ImageLoader
                    .CheckCancellationAndStartIterateToIndex(nextIteration, ImageIterator, CancellationToken.None)
                    .ConfigureAwait(false);
                return;
            }

            TiffNavigationInfo = new TiffManager.TiffNavigationInfo
            {
                CurrentPage = 0,
                PageCount = tiffPages.Count,
                Pages = tiffPages
            };
        }

        if (TiffNavigationInfo is null)
        {
            await ImageLoader
                .CheckCancellationAndStartIterateToIndex(nextIteration, ImageIterator, CancellationToken.None)
                .ConfigureAwait(false);
        }
        else
        {
            if (ImageIterator.IsReversed)
            {
                if (TiffNavigationInfo.CurrentPage - 1 < 0)
                {
                    await ExitTiffNavigationAndNavigate().ConfigureAwait(false);
                    return;
                }

                TiffNavigationInfo.CurrentPage -= 1;
            }
            else
            {
                TiffNavigationInfo.CurrentPage += 1;
            }

            if (TiffNavigationInfo.CurrentPage >= TiffNavigationInfo.PageCount || TiffNavigationInfo.CurrentPage < 0)
            {
                await ExitTiffNavigationAndNavigate().ConfigureAwait(false);
            }
            else
            {
                await UpdateImage.SetTiffImageAsync(TiffNavigationInfo, ImageIterator.CurrentIndex,
                    vm.PicViewer.FileInfo.CurrentValue, vm);
            }
        }

        return;

        async ValueTask ExitTiffNavigationAndNavigate()
        {
            await ImageLoader
                .CheckCancellationAndStartIterateToIndex(nextIteration, ImageIterator, CancellationToken.None)
                .ConfigureAwait(false);
            TiffNavigationInfo?.Dispose();
            TiffNavigationInfo = null;
        }
    }

    public static async ValueTask<bool> CheckIfTiffAndUpdate(MainViewModel vm, FileInfo fileInfo, int index)
    {
        if (!TiffManager.IsTiff(fileInfo))
        {
            return false;
        }

        var tiffPages = await Task.FromResult(TiffManager.LoadTiffPages(fileInfo.FullName)).ConfigureAwait(false);
        if (tiffPages.Count < 1)
        {
            return false;
        }

        TiffNavigationInfo = new TiffManager.TiffNavigationInfo
        {
            CurrentPage = 0,
            PageCount = tiffPages.Count,
            Pages = tiffPages
        };
        await UpdateImage.SetTiffImageAsync(TiffNavigationInfo, index, fileInfo, vm);
        return true;
    }

    public static async ValueTask Navigate(int index, MainViewModel vm)
    {
        if (!CanNavigate(vm))
        {
            return;
        }

        await ImageLoader.CheckCancellationAndStartIterateToIndex(index, ImageIterator, CancellationToken.None)
            .ConfigureAwait(false);
    }

    public static async ValueTask Navigate(FileInfo fileInfo, MainViewModel vm)
    {
        if (!CanNavigate(vm))
        {
            return;
        }

        var index = ImageIterator.ImagePaths.FindIndex(x => x.FullName.Equals(fileInfo.FullName));
        if (index < 0 || index >= ImageIterator.ImagePaths.Count)
        {
            return;
        }

        await ImageLoader.CheckCancellationAndStartIterateToIndex(index, ImageIterator, CancellationToken.None)
            .ConfigureAwait(false);
    }
    
    public static async ValueTask NavigateIncrements(bool next, bool is10, bool is100) =>
        await NavigateIncrements(UIHelper.GetMainView.DataContext as MainViewModel, next, is10, is100).ConfigureAwait(false);

    public static async ValueTask NavigateIncrements(MainViewModel vm, bool next, bool is10, bool is100)
    {
        if (!CanNavigate(vm))
        {
            return;
        }

        var currentIndex = ImageIterator.CurrentIndex;
        var direction = next ? NavigateTo.Next : NavigateTo.Previous;
        var index = ImageIterator.GetIteration(currentIndex, direction, false, is10, is100);

        await ImageLoader.CheckCancellationAndStartIterateToIndex(index, ImageIterator, CancellationToken.None)
            .ConfigureAwait(false);
    }

    public static async ValueTask LoadLastFileAsync(MainViewModel vm)
    {
        var lastFile = Settings.StartUp.LastFile;
        if (!string.IsNullOrEmpty(lastFile))
        {
            await LoadPicFromStringAsync(lastFile, vm).ConfigureAwait(false);
        }
        else
        {
            var lastEntry = FileHistoryManager.GetLastEntry();
            if (lastEntry != null)
            {
                await LoadPicFromStringAsync(lastEntry, vm).ConfigureAwait(false);
            }
        }
    }

    public static ValueTask Next10(MainViewModel vm) => NavigateIncrements(vm, true, true, false);
    public static ValueTask Next100(MainViewModel vm) => NavigateIncrements(vm, true, false, true);
    public static ValueTask Prev10(MainViewModel vm) => NavigateIncrements(vm, false, true, false);
    public static ValueTask Prev100(MainViewModel vm) => NavigateIncrements(vm, false, false, true);

    /// <summary>
    ///     Navigates to the first or last image in the collection.
    /// </summary>
    /// <param name="last">True to navigate to the last image, false to navigate to the first image.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async ValueTask NavigateFirstOrLast(bool last, MainViewModel vm)
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
            if (last)
            {
                await ImageLoader.LastIterationAsync(ImageIterator).ConfigureAwait(false);
            }
            else
            {
                await ImageLoader.FirstIterationAsync(ImageIterator).ConfigureAwait(false);
            }

            await UIHelper.ScrollToEndIfNecessary(last);
        }
    }
    
    /// <inheritdoc cref="NavigateFirstOrLast(bool last, MainViewModel vm)"/>
    public static async ValueTask NavigateFirstOrLast(bool last) =>
        await NavigateFirstOrLast(last, UIHelper.GetMainView.DataContext as MainViewModel);

    /// <summary>
    /// Iterates through the gallery or navigates between images depending on the current state.
    /// If the full gallery is open, it navigates through the gallery. Otherwise, it navigates between images.
    /// </summary>
    /// <param name="next">Indicates the direction of iteration. If true, iterates to the next item; otherwise, iterates to the previous item.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async ValueTask Iterate(bool next, MainViewModel vm, CancellationToken? cancellationToken)
    {
        if (GalleryFunctions.IsFullGalleryOpen)
        {
            GalleryNavigation.NavigateGallery(next ? Direction.Right : Direction.Left, vm);
        }
        else
        {
            await Navigate(next, vm, cancellationToken);
        }
    }

    public static async ValueTask Iterate(bool next, CancellationToken cancellationToken) =>
        await Iterate(next, UIHelper.GetMainView.DataContext as MainViewModel, cancellationToken).ConfigureAwait(false);

    /// <summary>
    ///     Navigates to the next or previous folder and loads the first image in that folder.
    /// </summary>
    /// <param name="next">True to navigate to the next folder, false for the previous folder.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>

    public static async ValueTask NavigateBetweenDirectories(bool next, MainViewModel vm)
        => await DirectoryNavigator.NavigateBetweenDirectories(next, ImageIterator, LoadWithoutImageIterator, vm);
    
    /// <inheritdoc cref="NavigateBetweenDirectories(bool next, MainViewModel vm)"/>
    public static async ValueTask NavigateBetweenDirectories(bool next) =>
        await NavigateBetweenDirectories(next, UIHelper.GetMainView.DataContext as MainViewModel);
    
    #endregion

    #region Load pictures from string, file or url

    /// <inheritdoc cref="ImageLoader.LoadPicFromStringAsync(string, MainViewModel, Navigation.ImageIterator)" />
    public static async ValueTask LoadPicFromStringAsync(string source, MainViewModel vm) =>
        await ImageLoader.LoadPicFromStringAsync(source, vm, ImageIterator).ConfigureAwait(false);

    /// <inheritdoc cref="ImageLoader.LoadPicFromFile(string, MainViewModel, Navigation.ImageIterator, FileInfo)" />
    public static async Task LoadPicFromFile(string fileName, MainViewModel vm, FileInfo? fileInfo = null) =>
        await ImageLoader.LoadPicFromFile(fileName, vm, ImageIterator, fileInfo).ConfigureAwait(false);

    /// <inheritdoc cref="ImageLoader.LoadPicFromArchiveAsync(string, MainViewModel, Navigation.ImageIterator)" />
    public static async ValueTask LoadPicFromArchiveAsync(string path, MainViewModel vm) =>
        await ImageLoader.LoadPicFromArchiveAsync(path, vm, ImageIterator).ConfigureAwait(false);

    /// <inheritdoc cref="ImageLoader.LoadPicFromUrlAsync(string, MainViewModel, Navigation.ImageIterator)" />
    public static async ValueTask LoadPicFromUrlAsync(string url, MainViewModel vm) =>
        await ImageLoader.LoadPicFromUrlAsync(url, vm, ImageIterator).ConfigureAwait(false);

    /// <inheritdoc cref="ImageLoader.LoadPicFromBase64Async(string, MainViewModel, Navigation.ImageIterator)" />
    public static async ValueTask LoadPicFromBase64Async(string base64, MainViewModel vm) =>
        await ImageLoader.LoadPicFromBase64Async(base64, vm, ImageIterator).ConfigureAwait(false);

    /// <inheritdoc cref="ImageLoader.LoadPicFromDirectoryAsync(string, MainViewModel, FileInfo)"/>
    public static async ValueTask LoadPicFromDirectoryAsync(string file, MainViewModel vm, FileInfo? fileInfo = null) =>
        await ImageLoader.LoadPicFromDirectoryAsync(file, vm, fileInfo).ConfigureAwait(false);

    #endregion

    #region ImageIterator

    public static void InitializeImageIterator(MainViewModel vm, bool setInitial = true)
    {
        ImageIterator ??= new ImageIterator(vm.PicViewer.FileInfo.CurrentValue, vm,  setInitial);
    }

    public static async ValueTask DisposeImageIteratorAsync()
    {
        if (ImageIterator is null)
        {
            return;
        }

        await ImageIterator.ClearAsync();
        ImageIterator.ImagePaths.Clear();
        await ImageIterator.DisposeAsync();
    }
    
    public static void DisableWatcher() => ImageIterator.IsWatcherEnabled = false;
    public static void EnableWatcher() => ImageIterator.IsWatcherEnabled = true;

    public static bool IsCollectionEmpty => ImageIterator?.ImagePaths is null || ImageIterator?.ImagePaths?.Count < 0;
    public static List<FileInfo>? GetCollection => ImageIterator?.ImagePaths;

    public static void UpdateFileListAndIndex(List<FileInfo> fileList, int index) =>
        ImageIterator?.UpdateFileListAndIndex(fileList, index);
        
    public static async Task AddFile(string fileName) => await ImageIterator.AddFile(fileName);

    /// <summary>
    ///     Returns the file name at a given index in the image collection.
    /// </summary>
    /// <param name="index">The index of the file to retrieve.</param>
    /// <returns>The file name at the given index.</returns>
    public static string? GetFileNameAt(int index)
    {
        if (IsCollectionEmpty)
        {
            return null;
        }

        if (index < 0 || index >= ImageIterator.ImagePaths.Count)
        {
            return null;
        }

        return ImageIterator.ImagePaths[index].FullName;
    }

    /// <summary>
    ///     Gets the current file name.
    /// </summary>
    public static string? GetCurrentFileName => GetFileNameAt(ImageIterator?.CurrentIndex ?? -1);

    /// <summary>
    ///     Gets the next file name.
    /// </summary>
    public static string? GetNextFileName => GetFileNameAt(ImageIterator?.NextIndex ?? -1);

    public static int GetCurrentIndex => ImageIterator?.CurrentIndex ?? -1;

    public static int GetNextIndex => ImageIterator?.NextIndex ?? -1;

    public static int GetNonZeroIndex => ImageIterator?.GetNonZeroIndex ?? -1;

    public static int GetCount => ImageIterator?.GetCount ?? -1;

    public static FileInfo? GetInitialFileInfo => ImageIterator?.InitialFileInfo;

    public static PreLoadValue? TryGetPreLoadValue(int index) =>
        ImageIterator?.GetPreLoadValue(index) ?? null;

    public static PreLoadValue? TryGetPreLoadValue(FileInfo fileInfo) =>
        ImageIterator?.GetPreLoadValue(fileInfo) ?? null;

    public static async ValueTask<PreLoadValue?> GetPreLoadValueAsync(int index) =>
        await ImageIterator.GetOrLoadPreLoadValueAsync(index) ?? null;

    public static async ValueTask<PreLoadValue?> GetPreLoadValueAsync(FileInfo fileInfo) =>
        await ImageIterator.GetOrLoadPreLoadValueAsync(fileInfo) ?? null;

    public static PreLoadValue? GetCurrentPreLoadValue() =>
        ImageIterator?.GetCurrentPreLoadValue() ?? null;

    public static async Task<PreLoadValue?> GetCurrentPreLoadValueAsync() =>
        await ImageIterator?.GetCurrentPreLoadValueAsync() ?? null;

    public static PreLoadValue? GetNextPreLoadValue() =>
        ImageIterator?.GetNextPreLoadValue() ?? null;

    public static async ValueTask<PreLoadValue?> GetNextPreLoadValueAsync() =>
        await ImageIterator?.GetNextPreLoadValueAsync() ?? null;

    public static async ValueTask ReloadFileListAsync() =>
        await ImageIterator.ReloadFileListAsync();

    public static void AddToPreloader(int index, ImageModel imageModel) =>
        ImageIterator?.Add(index, imageModel);

    public static bool AddToPreloader(FileInfo file, ImageModel imageModel) =>
        ImageIterator?.Add(file, imageModel) ?? false;

    public static async ValueTask PreloadAsync() =>
        await ImageIterator.PreloadAsync();

    public static async ValueTask QuickReload() =>
        await ImageIterator.QuickReload();

    #endregion
}