using System.Diagnostics;
using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Input;
using PicView.Avalonia.Preloading;
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

    public int CurrentIndex { get; private set; }
    
    public int NextIndex => GetIteration(CurrentIndex, NavigateTo.Next);

    public FileInfo InitialFileInfo { get; private set; } = null!;
    public bool IsReversed { get; private set; }
    private PreLoader PreLoader { get; } = new();

    private static FileSystemWatcher? _watcher;
    public bool IsRunning { get; private set; }
    private readonly MainViewModel? _vm;

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
        while (IsRunning && retries < 10)
        {
            await Task.Delay(200);
            retries++;
        }

        IsRunning = true;
        
        var sourceFileInfo = SettingsHelper.Settings.Sorting.IncludeSubDirectories ? new FileInfo(_watcher.Path) : fileInfo;

        var newList = await Task.FromResult(_vm.PlatformService.GetFiles(sourceFileInfo));
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
        
        SetTitleHelper.RefreshTitle(_vm);

        var index = ImagePaths.IndexOf(e.FullPath);
        if (index < 0)
        {
            IsRunning = false;
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
            PreLoader.RefreshAllFileInfo(ImagePaths);
            cleared = true;
        }
        
        IsRunning = false;

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

            GalleryNavigation.CenterScrollToSelectedItem(_vm);
        }

        if (cleared)
        {
            await Preload();
        }
    }

    private async Task OnFileDeleted(FileSystemEventArgs e)
    {
        if (e.FullPath.IsSupported() == false)
        {
            return;
        }

        if (ImagePaths.Contains(e.FullPath) == false)
        {
            return;
        }
        
        var index = ImagePaths.IndexOf(e.FullPath);
        if (index < 0)
        {
            return;
        }
        var isSameFile = CurrentIndex == index;
        
        PreLoader.Remove(index, ImagePaths);
        
        if (!ImagePaths.Remove(e.FullPath))
        {
#if DEBUG
            Console.WriteLine($"Failed to remove {e.FullPath}");
#endif
            return;
        }

        if (isSameFile)
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
    }

    private async Task OnFileRenamed(RenamedEventArgs e)
    {
        if (e.FullPath.IsSupported() == false)
        {
            if (ImagePaths.Contains(e.OldFullPath))
            {
                ImagePaths.Remove(e.OldFullPath);
            }

            return;
        }

        if (IsRunning)
        {
            return;
        }

        IsRunning = true;

        var oldIndex = ImagePaths.IndexOf(e.OldFullPath);
        var sameFile = CurrentIndex == oldIndex;
        var fileInfo = new FileInfo(e.FullPath);
        if (fileInfo.Exists == false)
        {
            return;
        }

        var sourceFileInfo = SettingsHelper.Settings.Sorting.IncludeSubDirectories ? new FileInfo(_watcher.Path) : fileInfo;
        var newList = FileListHelper.RetrieveFiles(sourceFileInfo).ToList();
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

        await PreLoader.RefreshFileInfo(oldIndex, ImagePaths);
        if (sameFile)
        {
            _vm.FileInfo = fileInfo;
        }

        SetTitleHelper.SetTitle(_vm);

        IsRunning = false;
        FileHistoryNavigation.Rename(e.OldFullPath, e.FullPath);
        await Dispatcher.UIThread.InvokeAsync(() => GalleryFunctions.RenameGalleryItem(oldIndex, Path.GetFileNameWithoutExtension(e.Name), e.FullPath, _vm));
    }

    #endregion

    #region Preloader

    public void Clear()
    {
        PreLoader.Clear();
    }

    public async Task Preload()
    {
        await PreLoader.PreLoadAsync(CurrentIndex, IsReversed, ImagePaths).ConfigureAwait(false);
    }

    public async Task AddAsync(int index, ImageModel imageModel)
    {
        await PreLoader.AddAsync(index, ImagePaths, imageModel).ConfigureAwait(false);
    }

    public PreLoadValue? GetPreLoadValue(int index)
    {
        return PreLoader.Get(index, ImagePaths);
    }

    public async Task<PreLoadValue?> GetPreLoadValueAsync(int index)
    {
        return await PreLoader.GetAsync(index, ImagePaths);
    }

    public PreLoadValue? GetCurrentPreLoadValue()
    {
        return PreLoader.Get(CurrentIndex, ImagePaths);
    }

    public async Task<PreLoadValue?> GetCurrentPreLoadValueAsync()
    {
        return await PreLoader.GetAsync(CurrentIndex, ImagePaths);
    }

    public PreLoadValue? GetNextPreLoadValue()
    {
        var nextIndex = GetIteration(CurrentIndex, IsReversed ? NavigateTo.Previous : NavigateTo.Next);
        return PreLoader.Get(nextIndex, ImagePaths);
    }

    public async Task<PreLoadValue?>? GetNextPreLoadValueAsync()
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

    public bool RefreshAllFileInfo()
    {
        return PreLoader.RefreshAllFileInfo(ImagePaths);
    }

    #endregion

    #region Navigation

    public async Task ReloadFileList()
    {
        ImagePaths = await Task.FromResult(_vm.PlatformService.GetFiles(InitialFileInfo)).ConfigureAwait(false);
        CurrentIndex = ImagePaths.IndexOf(_vm.FileInfo.FullName);

        InitiateFileSystemWatcher(InitialFileInfo);
    }

    public async Task QuickReload()
    {
        RemoveCurrentItemFromPreLoader();
        await IterateToIndex(CurrentIndex, new CancellationTokenSource()).ConfigureAwait(false);
    }

    public int GetIteration(int index, NavigateTo navigateTo, bool skip1 = false, bool skip10 = false, bool skip100 = false)
    {
        int next;

        if (skip100)
        {
            if (ImagePaths.Count > PreLoader.MaxCount)
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

                if (SettingsHelper.Settings.UIProperties.Looping)
                {
                    // Calculate new index with looping
                    next = (index + indexChange + ImagePaths.Count) % ImagePaths.Count;
                }
                else
                {
                    // Calculate new index without looping and ensure bounds
                    var newIndex = index + indexChange;
                    if (newIndex < 0) return 0;
                    if (newIndex >= ImagePaths.Count) return ImagePaths.Count - 1;

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

    public async Task NextIteration(NavigateTo navigateTo, CancellationTokenSource cts)
    {
        var index = GetIteration(CurrentIndex, navigateTo, SettingsHelper.Settings.ImageScaling.ShowImageSideBySide);
        if (index < 0)
        {
            return;
        }

        if (!MainKeyboardShortcuts.IsKeyHeldDown)
        {
            await IterateToIndex(index, cts).ConfigureAwait(false);
        }
        else
        {
            await TimerIteration(index, cts).ConfigureAwait(false);
        }
    }

    public async Task IterateToIndex(int index, CancellationTokenSource cts)
    {
        if (index < 0 || index >= ImagePaths.Count)
        {
            ErrorHandling.ShowStartUpMenu(_vm);
            return;
        }
        
        // UI is more responsive when started in new thread
        var isMainThread = Dispatcher.UIThread.CheckAccess();
        if (isMainThread)
        {
            await Task.Run(async () => { await Iterate(); }, cts.Token).ConfigureAwait(false);
        }
        else
        {
            await Iterate().ConfigureAwait(false);
        }

        return;

        async Task Iterate()
        {
            try
            {
                CurrentIndex = index;

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
                        await Task.Delay(20, cts.Token).ConfigureAwait(false);
                        if (CurrentIndex != index)
                        {
                            // Skip loading if user went to next value
                            await cts.CancelAsync();
                            return;
                        }
                    }
                }
                else
                {
                    TryShowPreview(preloadValue);
                    preloadValue = await PreLoader.GetAsync(CurrentIndex, ImagePaths).ConfigureAwait(false);
                }

                if (CurrentIndex != index)
                {
                    // Skip loading if user went to next value
                    await cts.CancelAsync();
                    return;
                }

                if (SettingsHelper.Settings.ImageScaling.ShowImageSideBySide)
                {
                    var nextIndex = GetIteration(index, IsReversed ? NavigateTo.Previous : NavigateTo.Next);
                    var nextPreloadValue = await PreLoader.GetAsync(nextIndex, ImagePaths);
                    if (CurrentIndex != index)
                    {
                        // Skip loading if user went to next value
                        await cts.CancelAsync();
                        return;
                    }

                    if (nextPreloadValue is not null)
                    {
                        _vm.SecondaryImageSource = nextPreloadValue.ImageModel?.Image;
                    }
                    cts.Token.ThrowIfCancellationRequested();
                    await UpdateImage.UpdateSource(_vm, index, ImagePaths, IsReversed, preloadValue, nextPreloadValue)
                        .ConfigureAwait(false);
                }
                else
                {
                    cts.Token.ThrowIfCancellationRequested();
                    await UpdateImage.UpdateSource(_vm, index, ImagePaths, IsReversed, preloadValue)
                        .ConfigureAwait(false);
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

                    await PreLoader.PreLoadAsync(CurrentIndex, IsReversed, ImagePaths)
                        .ConfigureAwait(false);
                }

                await AddAsync(index, preloadValue?.ImageModel).ConfigureAwait(false);

                // Add recent files, except when browsing archive
                if (string.IsNullOrWhiteSpace(TempFileHelper.TempFilePath) && ImagePaths.Count > index)
                {
                    FileHistoryNavigation.Add(ImagePaths[index]);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
#if DEBUG
                Trace.WriteLine($"{nameof(IterateToIndex)} canceled");
#endif
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine($"{nameof(IterateToIndex)} exception: \n{e.Message}\n{e.StackTrace}");
                await TooltipHelper.ShowTooltipMessageAsync(e.Message);
#endif
            }
            finally
            {
                _vm.IsLoading = false;
            }

            return;

            void TryShowPreview(PreLoadValue preloadValue)
            {
                SetTitleHelper.SetLoadingTitle(_vm);
                
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

                _vm.IsLoading = true;
                _vm.ImageSource = null;
                _vm.SecondaryImageSource = null;
            }
        }
    }

    private static Timer? _timer;

    internal async Task TimerIteration(int index, CancellationTokenSource cts)
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
        await IterateToIndex(index, cts).ConfigureAwait(false);
    }

    public void UpdateFileListAndIndex(List<string> fileList, int index)
    {
        ImagePaths = fileList;
        CurrentIndex = index;
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
            PreLoader.Dispose();
        }

        _disposed = true;
    }

    ~ImageIterator()
    {
        Dispose(false);
    }

    #endregion
}