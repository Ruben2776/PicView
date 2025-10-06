using System.Diagnostics;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.ArchiveHandling;
using PicView.Core.DebugTools;
using PicView.Core.Exif;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.Gallery;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Preloading;
using Timer = System.Timers.Timer;

namespace PicView.Avalonia.Navigation;

public class ImageIterator : IAsyncDisposable
{
    #region Properties

    private bool _disposed;

    public List<FileInfo> ImagePaths { get; private set; }
    public int CurrentIndex { get; private set; }
    
    public bool IsWatcherEnabled { get; set; } = Settings.Navigation.IsFileWatcherEnabled;

    public int GetNonZeroIndex => CurrentIndex + 1 > GetCount ? 1 : CurrentIndex + 1;

    public int NextIndex => GetIteration(CurrentIndex, NavigateTo.Next);

    public int GetCount => ImagePaths.Count;

    public FileInfo InitialFileInfo { get; private set; } = null!;
    public bool IsReversed { get; private set; }
    private PreLoader PreLoader { get; } = new(GetImageModel.GetImageModelAsync);

    private static FileSystemWatcher? _watcher;

    private bool _isRunning;

    private readonly MainViewModel? _vm;

    #endregion

    #region Constructors

    public ImageIterator(FileInfo fileInfo, MainViewModel vm, bool setInitial = true)
    {
#if DEBUG
        ArgumentNullException.ThrowIfNull(fileInfo);
#endif
        _vm = vm;
        FileInfo initialDirectory;
        
        // If setInitial is true, we want to continue from where we left off
        if (Settings.Sorting.IncludeSubDirectories && setInitial)
        {
            if (!string.IsNullOrWhiteSpace(Settings.StartUp.StartUpDirectory) && !ArchiveExtraction.IsArchived)
            {
                if (fileInfo.FullName.Contains(Settings.StartUp.StartUpDirectory))
                {
                    initialDirectory = new FileInfo(Settings.StartUp.StartUpDirectory);
                }
                else
                {
                    initialDirectory = new FileInfo(fileInfo.DirectoryName);
                }
            }
            else
            {
                initialDirectory = new FileInfo(fileInfo.DirectoryName);
            }
        }
        else
        {
            initialDirectory = new FileInfo(fileInfo.DirectoryName);
        }
        ImagePaths = vm.PlatformService.GetFiles(initialDirectory);
        CurrentIndex = ImagePaths.FindIndex(x => x.FullName.Equals(fileInfo.FullName));
        InitiateFileSystemWatcher(fileInfo);
        Settings.StartUp.StartUpDirectory = initialDirectory.FullName;
        vm.PicViewer.Maximum.Value = ImagePaths.Count;
    }

    public ImageIterator(FileInfo fileInfo, List<FileInfo> imagePaths, int currentIndex, MainViewModel vm)
    {
#if DEBUG
        ArgumentNullException.ThrowIfNull(fileInfo);
#endif
        _vm = vm;
        vm.PicViewer.Maximum.Value = imagePaths.Count;
        ImagePaths = imagePaths;
        CurrentIndex = currentIndex;
        InitiateFileSystemWatcher(fileInfo);
    }

    #endregion

    #region File Watcher

    private void InitiateFileSystemWatcher(FileInfo fileInfo)
    {
        InitialFileInfo = fileInfo;
        if (!fileInfo.FullName.Contains(Settings.StartUp.StartUpDirectory))
        {
            Settings.StartUp.StartUpDirectory = fileInfo.DirectoryName;
        }
        
        if (_watcher is not null)
        {
            _watcher.Dispose();
            _watcher = null;
        }

        _watcher?.Dispose();
        
        _watcher = new FileSystemWatcher(fileInfo.DirectoryName!)
        {
            EnableRaisingEvents = true,
            Filter = "*.*",
            IncludeSubdirectories = Settings.Sorting.IncludeSubDirectories,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };

        _watcher.Created += (_, e) =>
        {
            if (!e.FullPath.IsSupported() || !IsWatcherEnabled)
            {
                return; // Early exit
            }

            if (_vm.MainWindow.IsEditableTitlebarOpen.CurrentValue)
            {
                // Don't react to changes when renaming
                return;
            }

            Task.Run(() => OnFileAdded(e)).ContinueWith(t =>
            {
                if (t.Exception == null)
                {
                    return;
                }

                DebugHelper.LogDebug(nameof(ImageIterator), nameof(OnFileAdded), t.Exception);
            });
        };
        _watcher.Deleted += (_, e) =>
        {
            if (!e.FullPath.IsSupported() || !IsWatcherEnabled)
            {
                return; // Early exit
            }

            if (_vm.MainWindow.IsEditableTitlebarOpen.Value)
            {
                // Don't react to changes when renaming
                return;
            }

            Task.Run(() => OnFileDeleted(e)).ContinueWith(t =>
            {
                if (t.Exception == null)
                {
                    return;
                }

                DebugHelper.LogDebug(nameof(ImageIterator), nameof(OnFileDeleted), t.Exception);
            });
        };
        _watcher.Renamed += (_, e) =>
        {
            if (!e.FullPath.IsSupported() || !IsWatcherEnabled)
            {
                return; // Early exit
            }

            if (_vm.MainWindow.IsEditableTitlebarOpen.CurrentValue)
            {
                // Don't react to changes when renaming
                return;
            }

            Task.Run(() => OnFileRenamed(e)).ContinueWith(t =>
            {
                if (t.Exception == null)
                {
                    return;
                }

                DebugHelper.LogDebug(nameof(ImageIterator), nameof(OnFileRenamed), t.Exception);
            });
        };
    }

    private async ValueTask OnFileAdded(FileSystemEventArgs e)
    {
        try
        {
            _isRunning = true;
            await AddFile(e.FullPath);
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(ImageIterator), nameof(OnFileAdded), exception);
        }
        finally
        {
            _isRunning = false;
        }
    }

    public async ValueTask AddFile(string fileName)
    {
        var fileInfo = new FileInfo(fileName);
        if (!fileInfo.Exists)
        {
            return;
        }
        var sourceFileInfo = Settings.Sorting.IncludeSubDirectories
            ? new FileInfo(_watcher.Path)
            : fileInfo;

        var newList = await Task.FromResult(_vm.PlatformService.GetFiles(sourceFileInfo));
        if (newList.Count == 0)
        {
            return;
        }

        ImagePaths = newList;
        
        TitleManager.SetTitle(_vm);

        var index = ImagePaths.FindIndex(x => x.FullName.Equals(fileName));
        if (index < 0)
        {
            PreLoader.Resynchronize(ImagePaths);
            _isRunning = false;
            return;
        }

        if (Settings.Gallery.IsBottomGalleryShown || GalleryFunctions.IsFullGalleryOpen)
        {
            var isGalleryItemAdded = await GalleryFunctions.AddGalleryItem(index, fileInfo, _vm);
            if (isGalleryItemAdded)
            {
                if (Settings.Gallery.IsBottomGalleryShown && ImagePaths.Count > 1)
                {
                    if (_vm.Gallery.GalleryMode.CurrentValue is GalleryMode.BottomToClosed or GalleryMode.FullToClosed)
                    {
                        _vm.Gallery.GalleryMode.Value = GalleryMode.ClosedToBottom;
                    }
                }

                GalleryNavigation.CenterScrollToSelectedItem(_vm);
            }
        }

        PreLoader.Resynchronize(ImagePaths);
    }

    private async ValueTask OnFileDeleted(FileSystemEventArgs e)
    {
        try
        {
            _isRunning = true;

            var index = ImagePaths.FindIndex(x => x.FullName.Equals(e.FullPath));
            if (index < 0)
            {
                return;
            }

            var currentIndex = CurrentIndex;
            var isSameFile = currentIndex == index;

            ImagePaths.RemoveAt(index);

            if (isSameFile)
            {
                if (ImagePaths.Count <= 0)
                {
                    ErrorHandling.ShowStartUpMenu(_vm);
                    return;
                }

                RemoveCurrentItemFromPreLoader();
                PreLoader.Resynchronize(ImagePaths);
                var newIndex = Settings.Navigation.IsNavigatingBackwardsWhenDeleting
                    ? GetIteration(index, NavigateTo.Previous)
                    : GetIteration(index, NavigateTo.Next);
                CurrentIndex = newIndex;
                _vm.PicViewer.FileInfo.Value = ImagePaths[CurrentIndex];
                await IterateToIndex(CurrentIndex, new CancellationTokenSource());
            }
            else
            {
                RemoveItemFromPreLoader(index);
                TitleManager.SetTitle(_vm);
            }

            var removed = GalleryFunctions.RemoveGalleryItem(index, _vm);
            if (removed)
            {
                if (Settings.Gallery.IsBottomGalleryShown)
                {
                    if (ImagePaths.Count == 1)
                    {
                        _vm.Gallery.GalleryMode.Value = GalleryMode.BottomToClosed;
                    }
                }

                var indexOf = ImagePaths.FindIndex(x => x.FullName.Equals(_vm.PicViewer.FileInfo.CurrentValue.FullName));
                _vm.PicViewer.Index.Value = indexOf; // Fixes deselection bug 
                CurrentIndex = indexOf;
                if (isSameFile)
                {
                    GalleryNavigation.CenterScrollToSelectedItem(_vm);
                }
            }

            if (!isSameFile)
            {
                PreLoader.Resynchronize(ImagePaths);
            }

            FileHistoryManager.Remove(e.FullPath);
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(ImageIterator), nameof(OnFileDeleted), exception);
        }
        finally
        {
            _isRunning = false;
        }
    }

    private async ValueTask OnFileRenamed(RenamedEventArgs e)
    {
        try
        {
            if (!e.FullPath.IsSupported())
            {
                return;
            }
            
            var oldIndex = ImagePaths.FindIndex(x => x.FullName.Equals(e.OldFullPath));
            if (oldIndex < 0)
            {
                return;
            }

            _isRunning = true;
            
            var sameFile = CurrentIndex == oldIndex;
            var newFileInfo = new FileInfo(e.FullPath);
            if (newFileInfo.Exists == false)
            {
                return;
            }

            var newList = _vm.PlatformService.GetFiles(newFileInfo);
            if (newList.Count == 0)
            {
                return;
            }

            if (!newFileInfo.Exists)
            {
                return;
            }

            ImagePaths = newList;
            var newIndex = ImagePaths.FindIndex(x => x.FullName.Equals(e.FullPath));

            if (sameFile)
            {
                _vm.PicViewer.FileInfo.Value = newFileInfo;
                CurrentIndex = newIndex;
            }

            TitleManager.SetTitle(_vm);

            PreLoader.RefreshFileInfo(newIndex, newFileInfo, ImagePaths);
            Resynchronize();

            _isRunning = false;
            FileHistoryManager.Rename(e.OldFullPath, e.FullPath);

            await Dispatcher.UIThread.InvokeAsync(() =>
                GalleryFunctions.RenameGalleryItem(oldIndex, newIndex, Path.GetFileNameWithoutExtension(e.Name),
                    e.FullPath));
            if (sameFile)
            {
                _vm.PicViewer.Index.Value = newIndex;
                GalleryFunctions.CenterGallery(_vm);
            }
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(ImageIterator), nameof(OnFileRenamed), exception);
        }
        finally
        {
            _isRunning = false;
        }
    }

    #endregion

    #region Preloader

    public async ValueTask ClearAsync() =>
        await PreLoader.ClearAsync().ConfigureAwait(false);

    public async ValueTask PreloadAsync() =>
        await PreLoader.PreLoadAsync(CurrentIndex, IsReversed, ImagePaths).ConfigureAwait(false);

    public void Add(int index, ImageModel imageModel) =>
        PreLoader.Add(index, ImagePaths, imageModel, IsReversed);

    public bool Add(FileInfo file, ImageModel imageModel) =>
        PreLoader.Add(ImagePaths.FindIndex(x => x.FullName.Equals(file.FullName)), ImagePaths, imageModel, IsReversed);

    public PreLoadValue? GetPreLoadValue(int index)
    {
        if (index < 0 || index >= ImagePaths.Count)
        {
            return null;
        }

        return _isRunning
            ? PreLoader.Get(ImagePaths[index], ImagePaths)
            : PreLoader.Get(index, ImagePaths);
    }

    public PreLoadValue? GetPreLoadValue(FileInfo file) =>
        PreLoader.Get(file, ImagePaths);


    public async ValueTask<PreLoadValue?> GetOrLoadPreLoadValueAsync(int index) =>
        await PreLoader.GetOrLoadAsync(index, ImagePaths);
    
    public async ValueTask<PreLoadValue?> GetOrLoadPreLoadValueAsync(FileInfo file) =>
        await PreLoader.GetOrLoadAsync(file, ImagePaths);

    public PreLoadValue? GetCurrentPreLoadValue() =>
        _isRunning
            ? PreLoader.Get(_vm.PicViewer.FileInfo.CurrentValue, ImagePaths)
            : PreLoader.Get(CurrentIndex, ImagePaths);

    public async Task<PreLoadValue?> GetCurrentPreLoadValueAsync() =>
        _isRunning
            ? await PreLoader.GetOrLoadAsync(_vm.PicViewer.FileInfo.CurrentValue, ImagePaths)
            : await PreLoader.GetOrLoadAsync(CurrentIndex, ImagePaths);

    public PreLoadValue? GetNextPreLoadValue()
    {
        var nextIndex = GetIteration(CurrentIndex, IsReversed ? NavigateTo.Previous : NavigateTo.Next);
        return _isRunning ? PreLoader.Get(ImagePaths[nextIndex], ImagePaths) : PreLoader.Get(nextIndex, ImagePaths);
    }

    public async Task<PreLoadValue?>? GetNextPreLoadValueAsync()
    {
        var nextIndex = GetIteration(CurrentIndex, NavigateTo.Next);
        return _isRunning
            ? await PreLoader.GetOrLoadAsync(ImagePaths[nextIndex], ImagePaths)
            : await PreLoader.GetOrLoadAsync(nextIndex, ImagePaths);
    }

    public void RemoveItemFromPreLoader(int index) => PreLoader.Remove(index, ImagePaths);
    public void RemoveItemFromPreLoader(string fileName) => PreLoader.Remove(fileName, ImagePaths);

    public void RemoveCurrentItemFromPreLoader() => PreLoader.Remove(CurrentIndex, ImagePaths);

    public void Resynchronize() => PreLoader.Resynchronize(ImagePaths);

    #endregion

    #region Navigation

    public async ValueTask ReloadFileListAsync()
    {
        try
        {
            _isRunning = true;
            var fileList = await Task.FromResult(_vm.PlatformService.GetFiles(_vm.PicViewer.FileInfo.CurrentValue))
                .ConfigureAwait(false);
            var oldList = ImagePaths;
            ImagePaths = fileList;
            CurrentIndex = ImagePaths.FindIndex(x => x.FullName.Equals(_vm.PicViewer.FileInfo.CurrentValue.FullName));
            TitleManager.SetTitle(_vm);
            await ClearAsync().ConfigureAwait(false);
            await PreloadAsync().ConfigureAwait(false);
            Resynchronize();
            _isRunning = false;
            if (fileList.Count > oldList.Count)
            {
                for (var i = 0; i < oldList.Count; i++)
                {
                    if (i < fileList.Count && !oldList[i].FullName.Equals(fileList[i].FullName))
                    {
                        await GalleryFunctions.AddGalleryItem(fileList.FindIndex(x => x.FullName.Equals(fileList[i].FullName)), fileList[i],
                            _vm, DispatcherPriority.Background);
                    }
                }
            }
            else if (fileList.Count < oldList.Count)
            {
                for (var i = 0; i < fileList.Count; i++)
                {
                    if (i < oldList.Count && fileList[i].FullName.Equals(oldList[i].FullName))
                    {
                        GalleryFunctions.RemoveGalleryItem(i, _vm);
                    }
                }
            }
        }
        finally
        {
            _isRunning = false;
        }
    }

    public async ValueTask QuickReload()
    {
        RemoveCurrentItemFromPreLoader();
        var newFileInfo = new FileInfo(_vm.PicViewer.FileInfo.CurrentValue.FullName);
        ImagePaths[CurrentIndex] = newFileInfo;
        _vm.PicViewer.FileInfo.Value = newFileInfo;
        await IterateToIndex(CurrentIndex, new CancellationTokenSource()).ConfigureAwait(false);
    }

    public int GetIteration(int index, NavigateTo navigateTo, bool skip1 = false, bool skip10 = false,
        bool skip100 = false)
    {
        int next;

        if (skip100)
        {
            if (ImagePaths.Count > PreLoaderConfig.MaxCount)
            {
                PreLoader.Clear();
            }
        }

        // Determine skipAmount based on input flags
        var skipAmount = skip100 ? 100 : skip10 ? 10 : skip1 ? 2 : 1;

        switch (navigateTo)
        {
            case NavigateTo.Next:
            case NavigateTo.Previous:
                var indexChange = navigateTo == NavigateTo.Next ? skipAmount : -skipAmount;
                IsReversed = navigateTo == NavigateTo.Previous;

                if (Settings.UIProperties.Looping)
                {
                    // Calculate new index with looping
                    next = (index + indexChange + ImagePaths.Count) % ImagePaths.Count;
                }
                else
                {
                    // Calculate new index without looping and ensure bounds
                    var newIndex = index + indexChange;
                    if (newIndex < 0)
                    {
                        return 0;
                    }

                    if (newIndex >= ImagePaths.Count)
                    {
                        return ImagePaths.Count - 1;
                    }

                    next = newIndex;
                }

                break;

            case NavigateTo.First:
            case NavigateTo.Last:
                if (ImagePaths.Count > PreLoaderConfig.MaxCount)
                {
                    PreLoader.Clear();
                }

                next = navigateTo == NavigateTo.First ? 0 : ImagePaths.Count - 1;
                break;

            default:
#if DEBUG
                Console.WriteLine($"{nameof(ImageIterator)}: {navigateTo} is not a valid NavigateTo value.");
#endif
                return -1;
        }

        return next;
    }

    public async ValueTask NextIteration(NavigateTo navigateTo, CancellationTokenSource? cts)
    {
        var index = GetIteration(CurrentIndex, navigateTo,
            Settings.ImageScaling.ShowImageSideBySide);
        if (index < 0)
        {
            return;
        }

        await NextIteration(index, cts).ConfigureAwait(false);
    }

    public async ValueTask NextIteration(int iteration, CancellationTokenSource? cts)
    {
        // Handle side-by-side navigation
        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            // Handle properly navigating first or last image
            if (iteration == GetCount - 1)
            {
                if (!Settings.UIProperties.Looping)
                {
                    return;
                }

                var targetIndex = IsReversed ? GetCount - 2 < 0 ? 0 : GetCount - 2 : 0;
                await IterateToIndex(targetIndex, cts).ConfigureAwait(false);
                return;
            }

            // Determine the next index based on navigation direction
            var nextIndex = GetIteration(iteration, IsReversed ? NavigateTo.Previous : NavigateTo.Next);
            await IterateToIndex(nextIndex, cts).ConfigureAwait(false);
            return;
        }

        // When not showing side-by-side, decide based on keyboard state
        if (!MainKeyboardShortcuts.IsKeyHeldDown)
        {
            await IterateToIndex(iteration, cts).ConfigureAwait(false);
        }
        else
        {
            await TimerIteration(iteration, cts).ConfigureAwait(false);
        }
    }

    public async ValueTask IterateToIndexSlim(int index, bool isReverse, CancellationToken token)
    {
        CurrentIndex = index;
        IsReversed = isReverse;

        object? imageSource;
        int width;
        int height;
        var preloadValue = PreLoader.Get(index, ImagePaths);
        if (preloadValue is not null)
        {
            imageSource = preloadValue.ImageModel.Image;
            if (imageSource is Bitmap bmp)
            {
                width = (int)bmp.Size.Width;
                height = (int)bmp.Size.Height;
            }
            else
            {
                width = height = 0;
            }
        }
        else
        {
            imageSource = await GetImage.GetImageCore(ImagePaths[index]).ConfigureAwait(false);
            if (imageSource is Bitmap bmp)
            {
                width = (int)bmp.Size.Width;
                height = (int)bmp.Size.Height;
            }
            else
            {
                width = height = 0;
            }
        }

        await UpdateImage.UpdateSourceSlim(_vm, index, imageSource, width, height, ImagePaths, token);
    }

    public async Task SlimUpdate(int index, object? imageSource)
    {
        var magickImage = GetImage.CreateAndPingMagickImage(ImagePaths[index]);
        var imageModel = new ImageModel
        {
            Image = imageSource,
            Orientation = ExifOrientationHelper.GetImageOrientation(magickImage)
        };
        switch (imageSource)
        {
            case Bitmap bmp:
                GetImageModel.SetBitmapProperties(bmp, imageModel, magickImage.Format);
                imageModel.ImageType = magickImage.Format switch
                {
                    MagickFormat.WebP => ImageAnalyzer.IsAnimated(ImagePaths[index])
                        ? ImageType.AnimatedWebp
                        : ImageType.Bitmap,
                    MagickFormat.Gif or MagickFormat.Gif87 => ImageAnalyzer.IsAnimated(ImagePaths[index])
                        ? ImageType.AnimatedGif
                        : ImageType.Bitmap,
                    _ => ImageType.Bitmap
                };
                break;
            case string:
                imageModel.ImageType = ImageType.Svg;
                break;
            default:
                imageModel.ImageType = ImageType.Invalid;
                break;
        }

        PreLoader.Add(index, ImagePaths, imageModel, IsReversed);
        var preloadValue = new PreLoadValue(imageModel);
        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            var nextIndex = GetIteration(index, IsReversed ? NavigateTo.Previous : NavigateTo.Next);
            var nextPreloadValue = await GetOrLoadPreLoadValueAsync(nextIndex).ConfigureAwait(false);
            await UpdateImage.UpdateSource(_vm, index, ImagePaths, preloadValue,
                    nextPreloadValue)
                .ConfigureAwait(false);
        }
        else
        {
            await UpdateImage.UpdateSource(_vm, index, ImagePaths, preloadValue)
                .ConfigureAwait(false);
        }
        
        if (ImagePaths.Count > 1)
        {
            if (Settings.UIProperties.IsTaskbarProgressEnabled)
            {
                Dispatcher.UIThread.Invoke(
                    () => { _vm.PlatformService.SetTaskbarProgress((ulong)CurrentIndex, (ulong)ImagePaths.Count); },
                    DispatcherPriority.Render);
            }

            // We shouldn't wait for preloading to finish, since this should complete as soon as image changed. 
            // Awaiting preloader will cause delay, in E.G., moving the cursor after the image has changed.
            _ = Task.Run(() => PreLoader.PreLoadAsync(index, IsReversed, ImagePaths)
                .ConfigureAwait(false));
        }

        // Add recent files
        if (!Settings.Navigation.IsFileHistoryEnabled)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(TempFileHelper.TempFilePath) && ImagePaths.Count > CurrentIndex)
        {
            FileHistoryManager.Add(ImagePaths[CurrentIndex].FullName);
            if (Settings.ImageScaling.ShowImageSideBySide)
            {
                FileHistoryManager.Add(
                    ImagePaths[GetIteration(CurrentIndex, IsReversed ? NavigateTo.Previous : NavigateTo.Next)].FullName);
            }
        }
    }

    public async ValueTask IterateToIndex(int index, CancellationToken token)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        await IterateToIndex(index, cts);
    }

    /// <summary>
    ///     Iterates to the given index in the image list, shows the corresponding image and preloads the next/previous images.
    /// </summary>
    /// <param name="index">The index to iterate to.</param>
    /// <param name="cts">The cancellation token source.</param>
    public async ValueTask IterateToIndex(int index, CancellationTokenSource? cts)
    {
        if (index < 0 || index >= ImagePaths.Count)
        {
            // Invalid index. Probably a race condition? Do nothing and report
#if DEBUG
            Trace.WriteLine($"Invalid index {index} in {nameof(ImageIterator)}:{nameof(IterateToIndex)}");
#endif
            return;
        }

        try
        {
            CurrentIndex = index;

            // Get cached preload value first, if available
            // ReSharper disable once MethodHasAsyncOverload
            var preloadValue = GetPreLoadValue(index);
            if (preloadValue is not null)
            {
                // Wait for image to load if it's still loading
                if (preloadValue is { IsLoading: true, ImageModel.Image: null })
                {
                    LoadingPreview();

                    cts ??= CancellationTokenSource.CreateLinkedTokenSource();
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                    linkedCts.CancelAfter(TimeSpan.FromMinutes(1));

                    try
                    {
                        // Wait for the loading to complete or timeout
                        await preloadValue.WaitForLoadingCompleteAsync().WaitAsync(linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (!cts.IsCancellationRequested)
                    {
                        // This is a timeout, not cancellation from navigation
                        preloadValue =
                            new PreLoadValue(
                                await GetImageModel.GetImageModelAsync(ImagePaths[CurrentIndex]))
                            {
                                IsLoading = false
                            };
                    }

                    // Check if user navigated away during loading
                    if (CurrentIndex != index)
                    {
                        await cts.CancelAsync();
                        return;
                    }
                }
            }
            else
            {
                var imageModel = await GetImageModel.GetImageModelAsync(ImagePaths[index])
                    .ConfigureAwait(false);
                preloadValue = new PreLoadValue(imageModel);
            }

            if (CurrentIndex != index)
            {
                if (cts is not null)
                {
                    // Skip loading if user went to next value
                    await cts.CancelAsync();
                }
                return;
            }

            if (Settings.ImageScaling.ShowImageSideBySide)
            {
                var nextIndex = GetIteration(index, IsReversed ? NavigateTo.Previous : NavigateTo.Next);
                if (CurrentIndex != index)
                {
                    // Skip loading if user went to next value
                    await cts.CancelAsync();
                    return;
                }
                var nextPreloadValue = await GetOrLoadPreLoadValueAsync(nextIndex).ConfigureAwait(false);
                if (cts is null || !cts.IsCancellationRequested && index == CurrentIndex)
                {
                    await UpdateImage.UpdateSource(_vm, index, ImagePaths, preloadValue,
                            nextPreloadValue)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                if (cts is null || !cts.IsCancellationRequested && index == CurrentIndex)
                {
                    await UpdateImage.UpdateSource(_vm, index, ImagePaths, preloadValue, null)
                        .ConfigureAwait(false);
                }
            }

            if (ImagePaths.Count > 1)
            {
                if (Settings.UIProperties.IsTaskbarProgressEnabled)
                {
                    Dispatcher.UIThread.Invoke(
                        () => { _vm.PlatformService.SetTaskbarProgress((ulong)CurrentIndex, (ulong)ImagePaths.Count); },
                        DispatcherPriority.Render);
                }

                // We shouldn't wait for preloading to finish, since this should complete as soon as image changed. 
                // Awaiting preloader will cause delay, in E.G., moving the cursor after the image has changed.
                _ = Task.Run(() => PreLoader.PreLoadAsync(index, IsReversed, ImagePaths)
                    .ConfigureAwait(false));
            }

            PreLoader.Add(index, ImagePaths, preloadValue?.ImageModel, IsReversed);

            // Add recent files
            if (!Settings.Navigation.IsFileHistoryEnabled)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(TempFileHelper.TempFilePath) && ImagePaths.Count > CurrentIndex)
            {
                FileHistoryManager.Add(ImagePaths[CurrentIndex].FullName);
                if (Settings.ImageScaling.ShowImageSideBySide)
                {
                    FileHistoryManager.Add(
                        ImagePaths[GetIteration(CurrentIndex, IsReversed ? NavigateTo.Previous : NavigateTo.Next)].FullName);
                }
            }
        }
        catch (OperationCanceledException){}
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ImageIterator), nameof(IterateToIndex), e);
        }
        finally
        {
            if (index == CurrentIndex)
            {
                _vm.MainWindow.IsLoadingIndicatorShown.Value = false;
            }
        }

        return;

        void LoadingPreview()
        {
            TitleManager.SetLoadingTitle(_vm);

            _vm.PicViewer.Index.Value = index;
            if (Settings.Gallery.IsBottomGalleryShown)
            {
                GalleryNavigation.CenterScrollToSelectedItem(_vm);
            }

            var thumb = GetThumbnails.GetExifThumb(NavigationManager.GetFileNameAt(index));

            if (index != CurrentIndex)
            {
                return;
            }

            if (!Settings.ImageScaling.ShowImageSideBySide)
            {
                if (thumb is not null)
                {
                    _vm.PicViewer.ImageSource.Value = thumb;
                }
            }
            else
            {
                var secondaryThumb = GetThumbnails.GetExifThumb(NavigationManager.GetNextFileName);
                if (index != CurrentIndex)
                {
                    return;
                }

                _vm.PicViewer.ImageSource.Value = thumb;
                _vm.PicViewer.SecondaryImageSource.Value = secondaryThumb;
                _vm.MainWindow.IsLoadingIndicatorShown.Value = thumb is null || secondaryThumb is null;
            }
        }
    }

    private static Timer? _timer;


    private async ValueTask TimerIteration(int index, CancellationTokenSource? cts)
    {
        if (_timer is null)
        {
            _timer = new Timer
            {
                AutoReset = false,
                Enabled = true
            };
        }
        else if (_timer.Enabled)
        {
            if (!MainKeyboardShortcuts.IsKeyHeldDown)
            {
                _timer = null;
            }

            return;
        }

        _timer.Interval = TimeSpan.FromSeconds(Settings.UIProperties.NavSpeed).TotalMilliseconds;
        _timer.Start();
        await IterateToIndex(index, cts).ConfigureAwait(false);
    }

    public void UpdateFileListAndIndex(List<FileInfo> fileList, int index)
    {
        ImagePaths = fileList;
        CurrentIndex = index;
    }

    #endregion

    #region IDisposable

    public async ValueTask DisposeAsync()
    {
        await ClearAsync().ConfigureAwait(false);
        Dispose(true, true);
    }

    private void Dispose(bool disposing, bool cleared = false)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _watcher?.Dispose();
            if (!cleared)
            {
                PreLoader.Clear();
            }

            _timer?.Dispose();
            PreLoader.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}