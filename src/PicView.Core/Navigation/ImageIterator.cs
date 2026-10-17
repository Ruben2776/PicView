using PicView.Core.FileSorting;
using PicView.Core.FileHistory;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

public class ImageIterator(SharedImageCache cache, IThumbnailCache thumbCache, IThumbnailLoader thumbnailLoader, TabViewModel tab) : IImageIterator
{
    public SharedImageCache Cache { get; } = cache ?? throw new ArgumentNullException(nameof(cache));
    public string? CurrentDirectory => Files.Count > 0 ? Files[0].DirectoryName : null;

    private readonly IThumbnailCache _thumbCache = thumbCache ?? throw new ArgumentNullException(nameof(thumbCache));
    private readonly TabViewModel _tab = tab ?? throw new ArgumentNullException(nameof(tab));
    private readonly IThumbnailLoader _thumbnailLoader = thumbnailLoader ?? throw new ArgumentNullException(nameof(thumbnailLoader));
    private DateTime _lastRepeatTime = DateTime.MinValue;

    private readonly Lock _filesLock = new();
    private FileInfo[] _files = [];

    /// <inheritdoc />
    public IReadOnlyList<FileInfo> Files => Volatile.Read(ref _files);
    public int CurrentIndex { get; private set; } = -1;
    public int SecondaryCurrentIndex { get; private set; } = -1;
    public bool IsReversed { get; private set; }

    public void Initialize(IReadOnlyList<FileInfo> files, int initialIndex = 0)
    {
        lock (_filesLock)
        {
            CurrentIndex = initialIndex;
            Publish(files?.ToArray() ?? []);
        }
        UpdateNavigationProperties();
    }

    #region File list changes

    /// <inheritdoc />
    public int Insert(FileInfo file, Func<string, string, int> compareNames)
    {
        int index;
        lock (_filesLock)
        {
            var list = new List<FileInfo>(_files);
            index = FileSortOrder.InsertSorted(list, file, compareNames);
            if (CurrentIndex >= 0 && index <= CurrentIndex)
            {
                CurrentIndex++;
            }
            Publish(list.ToArray());
        }
        UpdateNavigationProperties();
        return index;
    }

    /// <inheritdoc />
    public int Remove(string path)
    {
        int index;
        lock (_filesLock)
        {
            index = IndexOf(_files, path);
            if (index < 0)
            {
                return -1;
            }

            var remaining = new FileInfo[_files.Length - 1];
            Array.Copy(_files, 0, remaining, 0, index);
            Array.Copy(_files, index + 1, remaining, index, remaining.Length - index);
            if (index < CurrentIndex)
            {
                CurrentIndex--;
            }
            else if (CurrentIndex >= remaining.Length)
            {
                CurrentIndex = remaining.Length - 1;
            }
            Publish(remaining);
        }
        UpdateNavigationProperties();
        return index;
    }

    /// <inheritdoc />
    public int Replace(FileInfo file)
    {
        lock (_filesLock)
        {
            var index = IndexOf(_files, file.FullName);
            if (index < 0)
            {
                return -1;
            }

            var files = (FileInfo[])_files.Clone();
            files[index] = file;
            Publish(files);
            return index;
        }
    }

    /// <inheritdoc />
    public (int Removed, int Inserted) Move(string oldPath, FileInfo newFile, Func<string, string, int> compareNames)
    {
        int removed, inserted;
        lock (_filesLock)
        {
            var currentPath = CurrentIndex >= 0 && CurrentIndex < _files.Length ? _files[CurrentIndex].FullName : null;
            var list = new List<FileInfo>(_files);
            removed = IndexOf(_files, oldPath);
            if (removed >= 0)
            {
                list.RemoveAt(removed);
            }
            inserted = FileSortOrder.InsertSorted(list, newFile, compareNames);

            if (currentPath is not null)
            {
                // Follow the current file, under its new name if it was the one moved
                var followed = IsSamePath(currentPath, oldPath) ? newFile.FullName : currentPath;
                var index = list.FindIndex(x => IsSamePath(x.FullName, followed));
                if (index >= 0)
                {
                    CurrentIndex = index;
                }
            }
            Publish(list.ToArray());
        }
        UpdateNavigationProperties();
        return (removed, inserted);
    }

    /// <summary>
    /// Makes <paramref name="files"/> the current list and tells the cache. Callers hold <see cref="_filesLock"/>.
    /// </summary>
    private void Publish(FileInfo[] files)
    {
        Volatile.Write(ref _files, files);
        Cache.SetFiles(_tab.Id, files);
    }

    private static int IndexOf(FileInfo[] files, string path) =>
        Array.FindIndex(files, x => IsSamePath(x.FullName, path));

    private static bool IsSamePath(string a, string b) =>
        a.AsSpan().Equals(b.AsSpan(), StringComparison.OrdinalIgnoreCase);

    #endregion
    
    #region Core Navigation Logic
    
    public void UpdateNavigationProperties()
        => UpdateNavigationProperties(CurrentIndex, Files.Count);

    private void UpdateNavigationProperties(int index, int count)
    {
        if (count <= 1 && !_tab.IsArchiveExtracting.Value)
        {
            _tab.CanNavigateForwards.Value = false;
            _tab.CanNavigateBackwards.Value = false;
        }
        else
        {
            var isLooping = Settings.UIProperties.Looping;
            var isExtracting = _tab.IsArchiveExtracting.Value;
            if (Settings.ImageScaling.ShowImageSideBySide)
            {
                _tab.CanNavigateForwards.Value = isLooping || isExtracting || index < count - 2;
            }
            else
            {
                _tab.CanNavigateForwards.Value = isLooping || isExtracting || index < count - 1;
            }

            _tab.CanNavigateBackwards.Value = isLooping || index > 0;
        }
        _tab.NavigationIndex.Value = index;
        _tab.MaxIndex.Value = count;
    }

    public async ValueTask NavigateAsync(NavigateTo navigateTo, SkipAmount skipAmount, CancellationTokenSource ct)
    {
        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            var (currentIndex, secondaryIndex, isReversed) = IterationHelper.GetIterations(CurrentIndex, Files.Count, navigateTo, skipAmount);
            IsReversed = isReversed;
            await IterateToIndicesAsync(currentIndex, secondaryIndex, ct).ConfigureAwait(false);
        }
        else
        {
            int iteration;
            bool isReversed;
            if (navigateTo == NavigateTo.Next && CurrentIndex >= Files.Count - 1 && _tab.IsArchiveExtracting.Value)
            {
                iteration = CurrentIndex + 1;
                isReversed = false;
            }
            else
            {
                (iteration, isReversed) = IterationHelper.GetIteration(CurrentIndex, Files.Count, navigateTo, skipAmount);
            }
            IsReversed = isReversed;
            await IterateToIndexAsync(iteration, ct).ConfigureAwait(false);
        }
    }

    public async ValueTask IterateToIndexAsync(int index, CancellationTokenSource ct)
    {
        if (index < 0)
        {
            return;
        }

        if (index >= Files.Count)
        {
            if (_tab.IsArchiveExtracting.Value)
            {
                _tab.SetLoading();
                var fileAppeared = await WaitForFileIndexAsync(index, ct.Token).ConfigureAwait(false);
                if (!fileAppeared || index >= Files.Count)
                {
                    UpdateNavigationProperties();
                    return;
                }
            }
            else
            {
                return;
            }
        }
        
        // Handle internal TIFF navigation
        if (_tab.Model?.TiffNavigation is not null && ShouldNavigateTiffEntry(_tab.Model, IsReversed))
        {
            return;
        }

        CurrentIndex = index;
        var targetFile = Files[index];
        var request = Cache.GetAsync(_tab.Id, targetFile, ct.Token);
        if (!request.IsCompleted)
        {
            // Still loading, show thumbnail while waiting
            _tab.SetLoading();
            await Task.Run(() =>
            {
                var thumb = GetThumbnail(targetFile);
                if (IsTargetActive(targetFile, index))
                {
                    _tab.Image.Value = thumb;
                }
            }).ConfigureAwait(false);
        }

        var model = await request.ConfigureAwait(false);
        if (model is not null && IsTargetActive(targetFile, index))
        {
            UpdateModel(model);
        }
    }
    
    public async ValueTask IterateToIndicesAsync(int index, int secondaryIndex, CancellationTokenSource ct)
    {
        if (index < 0 || index >= Files.Count || secondaryIndex < 0 || secondaryIndex >= Files.Count)
        {
            return;
        }

        // Handle internal TIFF navigation
        // TODO: Figure out how to handle multi-page TIFF files when side-by-side is enabled
        if (_tab.Model.TiffNavigation is not null && ShouldNavigateTiffEntry(_tab.Model, IsReversed))
        {
            return;
        }

        CurrentIndex = index;
        SecondaryCurrentIndex = secondaryIndex;
        var firstFile = Files[index];
        var secondaryFile = Files[secondaryIndex];
        var firstRequest = Cache.GetAsync(_tab.Id, firstFile, ct.Token);
        var secondRequest = Cache.GetAsync(_tab.Id, secondaryFile, ct.Token);
        if (!firstRequest.IsCompleted || !secondRequest.IsCompleted)
        {
            // Still loading, show thumbnail while waiting
            _tab.SetLoading();
            await Task.Run(() =>
            {
                var thumb = GetThumbnail(firstRequest.IsCompleted ? secondaryFile : firstFile);
                if (IsCurrent())
                {
                    _tab.Image.Value = thumb;
                }
            }).ConfigureAwait(false);
        }

        var firstModel = await firstRequest.ConfigureAwait(false);
        var secondModel = await secondRequest.ConfigureAwait(false);
        if (firstModel is null || secondModel is null || !IsCurrent())
        {
            return;
        }

        // We need to update the secondary model first, because updating the first model will trigger reactive subscription,
        // where the secondary model need to be valid beforehand.
        _tab.SecondaryModel = secondModel;
        _tab.Model = firstModel;
        UpdateNavigationProperties();
        TriggerPreload();

        if (!_tab.ArchiveExtractionService.IsArchived)
        {
            FileHistoryManager.Add(firstModel.FileInfo.FullName);
            FileHistoryManager.Add(secondModel.FileInfo.FullName);
        }

        return;

        bool IsCurrent() => index == CurrentIndex && secondaryIndex == SecondaryCurrentIndex;
    }

    public async ValueTask SkipToIndexAsync(int index, CancellationTokenSource ct)
    {
        if (index < 0 || index >= Files.Count) return;

        var file = Files[index];

        if (!Cache.TryGetReady(file, out _))
        {
            // Jumping out of the preloaded range, free the images around the old position
            Cache.ReleaseImages(_tab.Id);
        }

        await IterateToIndexAsync(index, ct).ConfigureAwait(false);
    }

    public async ValueTask NavigateByIncrementsAsync(SkipAmount skipAmount, bool forwards, CancellationTokenSource ct)
    {
        var (iteration, isReversed) = IterationHelper.GetIteration(CurrentIndex, Files.Count, forwards ? NavigateTo.Next : NavigateTo.Previous, skipAmount);
        IsReversed = isReversed;
        await SkipToIndexAsync(iteration, ct).ConfigureAwait(false);
    }

    public void SetCurrentIndex(int index)
    {
        CurrentIndex = index;
        UpdateNavigationProperties();
    }

    public async ValueTask ReloadAsync(bool clearCache = true)
    {
        if (clearCache)
        {
            Cache.ReleaseImages(_tab.Id);
        }
        var ct = _tab.GetTabCancellation();
        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            // Need to update SecondaryCurrentIndex
            var (_, nextIteration, _) = IterationHelper.GetIterations(CurrentIndex, Files.Count, NavigateTo.Next, SkipAmount.None);
            SecondaryCurrentIndex = nextIteration;
            await IterateToIndicesAsync(CurrentIndex, SecondaryCurrentIndex, ct).ConfigureAwait(false);
        }
        else
        {
            await IterateToIndexAsync(CurrentIndex, ct).ConfigureAwait(false);
        }
    }

    #endregion

    #region Repeated Navigation (Throttling)

    public async ValueTask RepeatNavigateAsync(NavigateTo to, TimeSpan repeatInterval, CancellationTokenSource ct)
    {
        var now = DateTime.UtcNow;
        if (now - _lastRepeatTime < repeatInterval)
        {
            return;
        }

        _lastRepeatTime = now;

        int iteration;
        bool isReversed;
        if (to == NavigateTo.Next && CurrentIndex >= Files.Count - 1 && _tab.IsArchiveExtracting.Value)
        {
            iteration = CurrentIndex + 1;
            isReversed = false;
        }
        else
        {
            (iteration, isReversed) = IterationHelper.GetIteration(CurrentIndex, Files.Count, to, SkipAmount.One);
        }
        IsReversed = isReversed;
        await IterateToIndexAsync(iteration, ct).ConfigureAwait(false);
    }

    public void StopRepeatedNavigation()
    {
        // Reset the throttle so the next click reacts instantly.
        _lastRepeatTime = DateTime.MinValue; 
    }

    private readonly Lock _fileWaitLock = new();
    private TaskCompletionSource<bool>? _fileAddedTcs;

    public void NotifyFileAdded()
    {
        lock (_fileWaitLock)
        {
            _fileAddedTcs?.TrySetResult(true);
        }
    }

    private async Task<bool> WaitForFileIndexAsync(int targetIndex, CancellationToken ct)
    {
        while (targetIndex >= Files.Count && _tab.IsArchiveExtracting.Value && !ct.IsCancellationRequested)
        {
            TaskCompletionSource<bool> tcs;
            lock (_fileWaitLock)
            {
                _fileAddedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                tcs = _fileAddedTcs;
            }

            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

            try
            {
                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(3000, ct)).ConfigureAwait(false);
                if (completedTask != tcs.Task)
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        return targetIndex < Files.Count;
    }

    private bool IsTargetActive(FileInfo targetFile, int targetIndex)
    {
        if (CurrentIndex == targetIndex)
        {
            return true;
        }
        if (CurrentIndex >= 0 && CurrentIndex < Files.Count)
        {
            return string.Equals(Files[CurrentIndex].FullName, targetFile.FullName, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    #endregion

    #region Update model & Loading

    private void UpdateModel(ImageModel newModel)
    {
        if (newModel.TiffNavigation is not null)
        {
            newModel.TiffNavigation.CurrentPage = IsReversed ? newModel.TiffNavigation.PageCount - 1 : 0;
            UpdateImageFromPage(newModel);
        }

        _tab.Image.Value = newModel.Image;
        _tab.FileInfo.Value = newModel.FileInfo;
        _tab.Model = newModel;
        UpdateNavigationProperties();
        TriggerPreload();

        if (!_tab.ArchiveExtractionService.IsArchived)
        {
            // Update the file history
            FileHistoryManager.Add(newModel.FileInfo.FullName);
        }
    }

    private void TriggerPreload()
    {
        if (CurrentIndex < 0 || CurrentIndex >= Files.Count)
        {
            return;
        }
        Cache.Preload(_tab.Id, Files[CurrentIndex], IsReversed, _tab.GetTabCancellation().Token);
    }

    private object? GetThumbnail(FileInfo file) =>
        !_thumbCache.IsEmpty && _thumbCache.TryGet(file.FullName, out var cachedThumb)
            ? cachedThumb
            : _thumbnailLoader.GetThumbQuick(file, _tab.ParentWindowContext?.Core);

    #endregion

    #region TIFF Handling & Helpers

    private static bool ShouldNavigateTiffEntry(ImageModel model, bool isPrevious)
    {
        if (model.TiffNavigation is null)
        {
            return false;
        }
        if (isPrevious)
        {
            var prev = model.TiffNavigation.CurrentPage - 1;
            if (prev < 0)
            {
                return false;
            }
            model.TiffNavigation.CurrentPage = prev;
        }
        else
        {
            var next = model.TiffNavigation.CurrentPage + 1;
            if (next >= model.TiffNavigation.PageCount)
            {
                return false;
            }
            model.TiffNavigation.CurrentPage = next;
        }

        UpdateImageFromPage(model);
        return true;
    }

    private static void UpdateImageFromPage(ImageModel model)
    {
        if (model.TiffNavigation is { Pages: not null, CurrentPage: >= 0 } &&
            model.TiffNavigation.CurrentPage < model.TiffNavigation.Pages.Length)
        {
            model.Image = model.TiffNavigation.Pages[model.TiffNavigation.CurrentPage];
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Cache.ReleaseOwner(_tab.Id);
        Volatile.Write(ref _files, []);
        GC.SuppressFinalize(this);
    }

    #endregion
}