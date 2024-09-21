using System.Diagnostics;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Gallery;
using PicView.Core.Navigation;
using Timer = System.Timers.Timer;

namespace PicView.Avalonia.Navigation;

public sealed class ImageIterator : IDisposable
{
    #region Properties

    private bool _disposed;

    public List<string> ImagePaths { get; private set; }

    public bool IsRenamingInProgress { get; set; }

    public int CurrentIndex { get; private set; }

    public FileInfo InitialFileInfo { get; private set; } = null!;
    public bool IsReversed { get; private set; }
    private PreLoader PreLoader { get; } = new();

    private static FileSystemWatcher? _watcher;
    private static bool _isRunning;
    private readonly MainViewModel? _vm;
    private readonly Lock _lock = new();

    #endregion

    #region Constructors

    public ImageIterator(FileInfo fileInfo, MainViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);
        _vm = vm;
        ImagePaths = vm.PlatformService.GetFiles(fileInfo);
        CurrentIndex = Directory.Exists(fileInfo.FullName) ? 0 : ImagePaths.IndexOf(fileInfo.FullName);
        InitiateFileSystemWatcher(fileInfo);
    }

    public ImageIterator(FileInfo fileInfo, List<string> imagePaths, int currentIndex, MainViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);
        _vm = vm;
        ImagePaths = imagePaths;
        CurrentIndex = currentIndex;
        InitiateFileSystemWatcher(fileInfo);
    }

    #endregion

    #region File Watcher

    private void InitiateFileSystemWatcher(FileInfo fileInfo)
    {
        InitialFileInfo = fileInfo;
        if (_watcher is not null)
        {
            _watcher.Dispose();
            _watcher = null;
        }

        _watcher = new FileSystemWatcher();
#if DEBUG
        Debug.Assert(fileInfo.DirectoryName != null);
#endif
        _watcher.Path = fileInfo.DirectoryName;
        _watcher.EnableRaisingEvents = true;
        _watcher.Filter = "*.*";
        _watcher.IncludeSubdirectories = SettingsHelper.Settings.Sorting.IncludeSubDirectories;
        _watcher.Created += async (_, e) => await OnFileAdded(e);
        _watcher.Deleted += async (_, e) => await OnFileDeleted(e);
        _watcher.Renamed += async (_, e) => await OnFileRenamed(e);
    }

    private async Task OnFileAdded(FileSystemEventArgs e)
    {
        if (IsRenamingInProgress)
        {
            return;
        }

        if (ImagePaths.Contains(e.FullPath))
        {
            return;
        }

        if (e.FullPath.IsSupported() == false)
        {
            return;
        }

        var fileInfo = new FileInfo(e.FullPath);
        if (fileInfo.Exists == false)
        {
            return;
        }

        var retries = 0;
        while (_isRunning && retries < 10)
        {
            await Task.Delay(200);
            retries++;
        }

        _isRunning = true;

        var newList = await Task.FromResult(_vm.PlatformService.GetFiles(fileInfo));
        if (newList.Count == 0)
        {
            return;
        }

        if (newList.Count == ImagePaths.Count)
        {
            return;
        }

        if (fileInfo.Exists == false)
        {
            return;
        }

        ImagePaths = newList;

        _isRunning = false;

        var index = ImagePaths.IndexOf(e.FullPath);
        if (index < 0)
        {
            return;
        }

        var nextIndex = index + 1;
        if (index >= ImagePaths.Count)
        {
            nextIndex = 0;
        }

        var prevIndex = index - 1;
        if (prevIndex < 0)
        {
            prevIndex = ImagePaths.Count - 1;
        }

        var cleared = false;
        if (PreLoader.Contains(index, ImagePaths) || PreLoader.Contains(nextIndex, ImagePaths) ||
            PreLoader.Contains(prevIndex, ImagePaths))
        {
            PreLoader.Clear();
            cleared = true;
        }

        SetTitleHelper.SetTitle(_vm);

        var isGalleryItemAdded = await GalleryFunctions.AddGalleryItem(index, fileInfo, _vm);
        if (isGalleryItemAdded)
        {
            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown && ImagePaths.Count > 1)
            {
                if (_vm.GalleryMode is GalleryMode.BottomToClosed or GalleryMode.FullToClosed)
                {
                    _vm.GalleryMode = GalleryMode.ClosedToBottom;
                }
            }

            var indexOf = ImagePaths.IndexOf(_vm.FileInfo.FullName);
            _vm.SelectedGalleryItemIndex = indexOf; // Fixes deselection bug
            CurrentIndex = indexOf;
            GalleryNavigation.CenterScrollToSelectedItem(_vm);
        }


        if (cleared)
        {
            await Preload();
        }
    }

    private async Task OnFileDeleted(FileSystemEventArgs e)
    {
        if (IsRenamingInProgress)
        {
            return;
        }

        if (e.FullPath.IsSupported() == false)
        {
            return;
        }

        if (ImagePaths.Contains(e.FullPath) == false)
        {
            return;
        }

        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        var index = ImagePaths.IndexOf(e.FullPath);
        if (index < 0)
        {
            return;
        }

        var nextIndex = index + 1;
        if (index >= ImagePaths.Count)
        {
            nextIndex = 0;
        }

        var prevIndex = index - 1;
        if (prevIndex < 0)
        {
            prevIndex = ImagePaths.Count - 1;
        }

        var cleared = false;
        if (PreLoader.Contains(index, ImagePaths) || PreLoader.Contains(nextIndex, ImagePaths) ||
            PreLoader.Contains(prevIndex, ImagePaths))
        {
            PreLoader.Clear();
            cleared = true;
        }
        else
        {
            PreLoader.Remove(index, ImagePaths);
        }

        var sameFile = CurrentIndex == index;
        if (!ImagePaths.Remove(e.FullPath))
        {
            return;
        }

        if (sameFile)
        {
            if (ImagePaths.Count <= 0)
            {
                ErrorHandling.ShowStartUpMenu(_vm);
                return;
            }

            await NavigationHelper.Iterate(false, _vm);
        }
        else
        {
            SetTitleHelper.SetTitle(_vm);
        }

        var removed = GalleryFunctions.RemoveGalleryItem(index, _vm);
        if (removed)
        {
            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                if (ImagePaths.Count == 1)
                {
                    _vm.GalleryMode = GalleryMode.BottomToClosed;
                }
            }

            var indexOf = ImagePaths.IndexOf(_vm.FileInfo.FullName);
            _vm.SelectedGalleryItemIndex = indexOf; // Fixes deselection bug
            CurrentIndex = indexOf;
            GalleryNavigation.CenterScrollToSelectedItem(_vm);
        }


        FileHistoryNavigation.Remove(e.FullPath);
        _isRunning = false;

        SetTitleHelper.SetTitle(_vm);
        if (cleared)
        {
            await Preload();
        }
    }

    private async Task OnFileRenamed(RenamedEventArgs e)
    {
        if (IsRenamingInProgress)
        {
            return;
        }

        if (e.FullPath.IsSupported() == false)
        {
            if (ImagePaths.Contains(e.OldFullPath))
            {
                ImagePaths.Remove(e.OldFullPath);
            }

            return;
        }

        if (_isRunning)
        {
            return;
        }

        _isRunning = true;

        var oldIndex = ImagePaths.IndexOf(e.OldFullPath);

        var fileInfo = new FileInfo(e.FullPath);
        if (fileInfo.Exists == false)
        {
            return;
        }

        var newList = FileListHelper.RetrieveFiles(fileInfo).ToList();
        if (newList.Count == 0)
        {
            return;
        }

        if (fileInfo.Exists == false)
        {
            return;
        }

        ImagePaths = newList;

        var index = ImagePaths.IndexOf(e.FullPath);
        if (index < 0)
        {
            return;
        }

        if (fileInfo.Exists == false)
        {
            return;
        }

        SetTitleHelper.SetTitle(_vm);

        await PreLoader.RefreshFileInfo(oldIndex, ImagePaths);

        _isRunning = false;
        FileHistoryNavigation.Rename(e.OldFullPath, e.FullPath);
        GalleryFunctions.RemoveGalleryItem(oldIndex, _vm);
        await GalleryFunctions.AddGalleryItem(index, fileInfo, _vm);
        await GalleryFunctions.SortGalleryItems(ImagePaths, _vm);
    }

    #endregion

    #region Preloader

    public void Clear()
    {
        PreLoader.Clear();
    }

    public async Task Preload()
    {
        await PreLoader.PreLoadAsync(CurrentIndex, ImagePaths.Count, IsReversed, ImagePaths).ConfigureAwait(false);
    }

    public async Task AddAsync(int index, ImageModel imageModel)
    {
        await PreLoader.AddAsync(index, ImagePaths, imageModel).ConfigureAwait(false);
    }

    public PreLoader.PreLoadValue? GetPreLoadValue(int index)
    {
        return PreLoader.Get(index, ImagePaths);
    }

    public PreLoader.PreLoadValue? GetCurrentPreLoadValue()
    {
        return PreLoader.Get(CurrentIndex, ImagePaths);
    }

    public async Task<PreLoader.PreLoadValue?> GetCurrentPreLoadValueAsync()
    {
        return await PreLoader.GetAsync(CurrentIndex, ImagePaths);
    }

    public PreLoader.PreLoadValue? GetNextPreLoadValue()
    {
        var nextIndex = GetIteration(CurrentIndex, NavigateTo.Next);
        return PreLoader.Get(nextIndex, ImagePaths);
    }

    public async Task<PreLoader.PreLoadValue?>? GetNextPreLoadValueAsync()
    {
        var nextIndex = GetIteration(CurrentIndex, NavigateTo.Next);
        return await PreLoader.GetAsync(nextIndex, ImagePaths);
    }

    public void RemoveItemFromPreLoader(int index)
    {
        PreLoader.Remove(index, ImagePaths);
    }

    public void RemoveCurrentItemFromPreLoader()
    {
        PreLoader.Remove(CurrentIndex, ImagePaths);
    }

    #endregion

    #region Navigation

    public async Task ReloadFileList()
    {
        ImagePaths = await Task.FromResult(_vm.PlatformService.GetFiles(InitialFileInfo)).ConfigureAwait(false);
        CurrentIndex = ImagePaths.IndexOf(_vm.FileInfo.FullName);

        InitiateFileSystemWatcher(InitialFileInfo);
    }

    public int GetIteration(int index, NavigateTo navigateTo, bool skip1 = false)
    {
        int next;
        var skipAmount = skip1 ? 2 : 1;

        switch (navigateTo)
        {
            case NavigateTo.Next:
            case NavigateTo.Previous:
                var indexChange = navigateTo == NavigateTo.Next ? skipAmount : -skipAmount;
                IsReversed = navigateTo == NavigateTo.Previous;

                if (SettingsHelper.Settings.UIProperties.Looping)
                {
                    next = (index + indexChange + ImagePaths.Count) % ImagePaths.Count;
                }
                else
                {
                    var newIndex = index + indexChange;

                    // Ensure the new index doesn't go out of bounds
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
                if (ImagePaths.Count > PreLoader.MaxCount)
                {
                    PreLoader.Clear();
                }

                next = navigateTo == NavigateTo.First ? 0 : ImagePaths.Count - 1;
                break;

            default:
                return -1;
        }

        return next;
    }

    public async Task NextIteration(NavigateTo navigateTo)
    {
        var index = GetIteration(CurrentIndex, navigateTo, SettingsHelper.Settings.ImageScaling.ShowImageSideBySide);
        if (index < 0)
        {
            return;
        }

        if (!MainKeyboardShortcuts.IsKeyHeldDown)
        {
            await IterateToIndex(index);
        }
        else
        {
            await TimerIteration(index);
        }
    }

    public async Task IterateToIndex(int index)
    {
        if (index < 0 || index >= ImagePaths.Count)
        {
            ErrorHandling.ShowStartUpMenu(_vm);
            return;
        }

        await Task.Run(async () =>
        {
            try
            {
                lock (_lock)
                {
                    CurrentIndex = index;
                }

                // ReSharper disable once MethodHasAsyncOverload
                var preloadValue = PreLoader.Get(index, ImagePaths);
                if (preloadValue is not null)
                {
                    if (preloadValue.IsLoading)
                    {
                        TryShowPreview(preloadValue);
                    }

                    while (preloadValue.IsLoading)
                    {
                        await Task.Delay(20);
                        lock (_lock)
                        {
                            if (CurrentIndex != index)
                            {
                                // Skip loading if user went to next value
                                return;
                            }
                        }
                    }
                }
                else
                {
                    TryShowPreview(preloadValue);
                    preloadValue = await PreLoader.GetAsync(CurrentIndex, ImagePaths).ConfigureAwait(false);
                }

                lock (_lock)
                {
                    if (CurrentIndex != index)
                    {
                        // Skip loading if user went to next value
                        return;
                    }
                }

                if (SettingsHelper.Settings.ImageScaling.ShowImageSideBySide)
                {
                    var nextPreloadValue = await GetNextPreLoadValueAsync().ConfigureAwait(false);
                    lock (_lock)
                    {
                        if (CurrentIndex != index)
                        {
                            // Skip loading if user went to next value
                            return;
                        }
                    }

                    _vm.SecondaryImageSource = nextPreloadValue.ImageModel.Image;
                    await UpdateSource(index, preloadValue, nextPreloadValue).ConfigureAwait(false);
                }
                else
                {
                    await UpdateSource(index, preloadValue).ConfigureAwait(false);
                }

                if (ImagePaths.Count > 1)
                {
                    if (SettingsHelper.Settings.UIProperties.IsTaskbarProgressEnabled)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            _vm.PlatformService.SetTaskbarProgress((ulong)CurrentIndex, (ulong)ImagePaths.Count);
                        });
                    }

                    await PreLoader.PreLoadAsync(CurrentIndex, ImagePaths.Count, IsReversed, ImagePaths)
                        .ConfigureAwait(false);
                }

                await AddAsync(index, preloadValue.ImageModel).ConfigureAwait(false);

                // Add recent files, except when browsing archive
                if (string.IsNullOrWhiteSpace(TempFileHelper.TempFilePath) && ImagePaths.Count > index)
                {
                    FileHistoryNavigation.Add(ImagePaths[index]);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine($"{nameof(IterateToIndex)} exception: \n{e.Message}");
                await TooltipHelper.ShowTooltipMessageAsync(e.Message);
#endif
            }
            finally
            {
                _vm.IsLoading = false;
            }

            return;

            void TryShowPreview(PreLoader.PreLoadValue preloadValue)
            {
                if (preloadValue is null)
                {
                    return;
                }

                if (!preloadValue.IsLoading)
                {
                    return;
                }

                if (index != CurrentIndex)
                {
                    return;
                }

                if (SettingsHelper.Settings.ImageScaling.ShowImageSideBySide)
                {
                    SetTitleHelper.SetLoadingTitle(_vm);
                    _vm.IsLoading = true;
                    _vm.ImageSource = null;
                    _vm.SecondaryImageSource = null;
                }
                else
                {
                    LoadingPreview(index);
                }
            }
        });
    }

    private static Timer? _timer;

    internal async Task TimerIteration(int index)
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

        _timer.Interval = TimeSpan.FromSeconds(SettingsHelper.Settings.UIProperties.NavSpeed).TotalMilliseconds;
        _timer.Start();
        await IterateToIndex(index);
    }

    public void UpdateFileListAndIndex(List<string> fileList, int index)
    {
        ImagePaths = fileList;
        CurrentIndex = index;
    }

    #endregion

    #region Update Source and Preview

    private async Task UpdateSource(int index, PreLoader.PreLoadValue? preLoadValue,
        PreLoader.PreLoadValue? nextPreloadValue = null)
    {
        preLoadValue ??= await PreLoader.GetAsync(index, ImagePaths);
        if (preLoadValue.ImageModel?.Image is null)
        {
            var fileInfo = preLoadValue.ImageModel?.FileInfo ?? new FileInfo(ImagePaths[index]);
            preLoadValue.ImageModel = await GetImageModel.GetImageModelAsync(fileInfo).ConfigureAwait(false);
        }

        if (SettingsHelper.Settings.ImageScaling.ShowImageSideBySide)
        {
            nextPreloadValue ??= await GetNextPreLoadValueAsync();
            if (nextPreloadValue.ImageModel?.Image is null)
            {
                var fileInfo = nextPreloadValue.ImageModel?.FileInfo ?? new FileInfo(
                    ImagePaths[GetIteration(index, IsReversed ? NavigateTo.Previous : NavigateTo.Next, true)]);
                nextPreloadValue.ImageModel = await GetImageModel.GetImageModelAsync(fileInfo).ConfigureAwait(false);
            }
        }

        _vm.IsLoading = false;
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.ImageViewer.SetTransform(preLoadValue.ImageModel.EXIFOrientation);
            if (SettingsHelper.Settings.ImageScaling.ShowImageSideBySide)
            {
                _vm.SecondaryImageSource = nextPreloadValue.ImageModel.Image;
            }

            _vm.ImageSource = preLoadValue.ImageModel.Image;
            if (preLoadValue.ImageModel.ImageType is ImageType.AnimatedGif or ImageType.AnimatedWebp)
            {
                _vm.ImageViewer.MainImage.InitialAnimatedSource = preLoadValue.ImageModel.FileInfo.FullName;
            }

            _vm.ImageType = preLoadValue.ImageModel.ImageType;
        
            WindowHelper.SetSize(preLoadValue.ImageModel.PixelWidth, preLoadValue.ImageModel.PixelHeight,
                nextPreloadValue?.ImageModel?.PixelWidth ?? 0, nextPreloadValue?.ImageModel?.PixelHeight ?? 0,
                preLoadValue.ImageModel.Rotation, _vm);
        }, DispatcherPriority.Send);


        SetTitleHelper.SetTitle(_vm, preLoadValue.ImageModel);

        if (SettingsHelper.Settings.WindowProperties.KeepCentered)
        {
            await Dispatcher.UIThread.InvokeAsync(() => { WindowHelper.CenterWindowOnScreen(false); });
        }

        _vm.GetIndex = index + 1;
        if (_vm.SelectedGalleryItemIndex != index)
        {
            _vm.SelectedGalleryItemIndex = index;
            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                GalleryNavigation.CenterScrollToSelectedItem(_vm);
            }
        }

        await Dispatcher.UIThread.InvokeAsync(TooltipHelper.CloseToolTipMessage);

        ExifHandling.UpdateExifValues(preLoadValue.ImageModel, _vm);
    }

    public void LoadingPreview(int index)
    {
        if (index != CurrentIndex)
        {
            return;
        }

        SetTitleHelper.SetLoadingTitle(_vm);
        _vm.SelectedGalleryItemIndex = index;
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            GalleryNavigation.CenterScrollToSelectedItem(_vm);
        }

        using var image = new MagickImage();
        image.Ping(_vm.ImageIterator.ImagePaths[index]);
        var thumb = image.GetExifProfile()?.CreateThumbnail();
        if (thumb is null)
        {
            if (index == CurrentIndex)
            {
                _vm.IsLoading = true;
                _vm.ImageSource = null;
            }

            return;
        }

        var byteArray = thumb.ToByteArray();
        if (byteArray is null)
        {
            if (index == CurrentIndex)
            {
                _vm.IsLoading = true;
                _vm.ImageSource = null;
            }

            return;
        }

        var stream = new MemoryStream(byteArray);
        if (index != CurrentIndex)
        {
            return;
        }

        _vm.ImageSource = new Bitmap(stream);
        _vm.ImageType = ImageType.Bitmap;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _watcher?.Dispose();
            Clear();
            _timer?.Dispose();
        }

        _disposed = true;
    }

    ~ImageIterator()
    {
        Dispose(false);
    }

    #endregion
}