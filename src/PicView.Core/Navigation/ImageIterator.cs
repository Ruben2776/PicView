using PicView.Core.FileSorting;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

/// <param name="filesChanged">Hears every change to the file list, in order, while the list is locked. The gallery
/// follows the list through it (ADR-0010), so it must not call back into the iterator.</param>
public class ImageIterator(SharedImageCache cache, IThumbnailCache thumbCache, IThumbnailLoader thumbnailLoader, TabViewModel tab,
    Action<FileListChange>? filesChanged = null) : IImageIterator
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
            var list = files?.ToArray() ?? [];
            Publish(list, new FileListChange(FileListChangeKind.Listed, list, CurrentIndex));
        }
        UpdateNavigationProperties();
    }

    #region File list changes

    /// <inheritdoc />
    public int Insert(FileInfo file, Func<string, string, int> compareNames, FileDetailsSource? details = null)
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
            var files = list.ToArray();
            Publish(files, new FileListChange(FileListChangeKind.Inserted, files, index, file, details));
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
            Publish(remaining, new FileListChange(FileListChangeKind.Removed, remaining, index));
        }
        UpdateNavigationProperties();
        return index;
    }

    /// <inheritdoc />
    public int Replace(FileInfo file, FileDetailsSource? details = null)
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
            Publish(files, new FileListChange(FileListChangeKind.Replaced, files, index, file, details));
            return index;
        }
    }

    /// <inheritdoc />
    public (int Removed, int Inserted) Move(string oldPath, FileInfo newFile, Func<string, string, int> compareNames,
        FileDetailsSource? details = null)
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
            var files = list.ToArray();
            Volatile.Write(ref _files, files);
            Cache.SetFiles(_tab.Id, files);
            // The gallery hears it as the two edits it is
            if (removed >= 0)
            {
                filesChanged?.Invoke(new FileListChange(FileListChangeKind.Removed, files, removed));
            }
            filesChanged?.Invoke(new FileListChange(FileListChangeKind.Inserted, files, inserted, newFile, details));
            SignalFilesChanged();
        }
        UpdateNavigationProperties();
        return (removed, inserted);
    }

    /// <inheritdoc />
    public int[] RenameFolder(string oldFolder, string newFolder)
    {
        lock (_filesLock)
        {
            var files = (FileInfo[])_files.Clone();
            var moved = new List<int>();
            for (var i = 0; i < files.Length; i++)
            {
                if (FolderChange.PathInRenamedFolder(files[i].FullName, oldFolder, newFolder) is { } newPath)
                {
                    files[i] = new FileInfo(newPath);
                    moved.Add(i);
                }
            }

            if (moved.Count > 0)
            {
                Volatile.Write(ref _files, files);
                Cache.SetFiles(_tab.Id, files);
                foreach (var index in moved)
                {
                    filesChanged?.Invoke(new FileListChange(FileListChangeKind.Relocated, files, index, files[index]));
                }
                SignalFilesChanged();
            }
            return moved.ToArray();
        }
    }

    /// <summary>
    /// Makes <paramref name="files"/> the current list and tells the cache and the gallery. Callers hold
    /// <see cref="_filesLock"/>, so the gallery hears the changes in the order they were made.
    /// </summary>
    private void Publish(FileInfo[] files, FileListChange change)
    {
        Volatile.Write(ref _files, files);
        Cache.SetFiles(_tab.Id, files);
        filesChanged?.Invoke(change);
        SignalFilesChanged();
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
            _tab.CanNavigateForwards.Value = SideBySidePair.CanNavigateForwards(index, count, isExtracting);

            _tab.CanNavigateBackwards.Value = isLooping || index > 0;
        }
        _tab.NavigationIndex.Value = index;
        _tab.MaxIndex.Value = count;
    }

    public async ValueTask NavigateAsync(NavigateTo navigateTo, SkipAmount skipAmount, CancellationTokenSource ct)
    {
        if (await WaitForNextAsync(navigateTo, skipAmount, ct.Token).ConfigureAwait(false) is not { } from)
        {
            return;
        }

        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            var (currentIndex, secondaryIndex, isReversed) = SideBySidePair.Navigate(from.Index, from.Count, navigateTo, skipAmount);
            IsReversed = isReversed;
            if (ShouldNavigateTiffEntry(navigateTo, skipAmount))
            {
                return;
            }

            if (secondaryIndex is { } secondary)
            {
                await IterateToIndicesAsync(currentIndex, secondary, ct).ConfigureAwait(false);
            }
            else
            {
                await IterateToIndexAsync(currentIndex, ct).ConfigureAwait(false);
            }
        }
        else
        {
            var (iteration, isReversed) = IterationHelper.GetIteration(from.Index, from.Count, navigateTo, skipAmount);
            IsReversed = isReversed;
            if (ShouldNavigateTiffEntry(navigateTo, skipAmount))
            {
                return;
            }

            await IterateToIndexAsync(iteration, ct).ConfigureAwait(false);
        }
    }

    public async ValueTask IterateToIndexAsync(int index, CancellationTokenSource ct)
    {
        if (index < 0 || index >= Files.Count)
        {
            return;
        }

        CurrentIndex = index;
        // A single image has no secondary, and a pair that is still loading is no longer the current one
        SecondaryCurrentIndex = -1;
        var targetFile = Files[index];
        var request = Cache.GetAsync(_tab.Id, targetFile, ct.Token);
        if (!request.IsCompleted)
        {
            // Still loading, show thumbnail while waiting
            _tab.SetLoading();
            await Task.Run(async () =>
            {
                var thumb = GetThumbnail(targetFile);
                if (IsTargetActive(targetFile, index))
                {
                    _tab.Image.Value = thumb;
                }
                var model = await request.ConfigureAwait(false);
                if (model is not null && IsTargetActive(targetFile, index))
                {
                    UpdateModel(model);
                }
            }, ct.Token).ConfigureAwait(false);
            return;
        }
        UpdateModel(await request.ConfigureAwait(false));
    }
    
    public async ValueTask IterateToIndicesAsync(int index, int secondaryIndex, CancellationTokenSource ct)
    {
        if (index < 0 || index >= Files.Count || secondaryIndex < 0 || secondaryIndex >= Files.Count)
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
            _tab.SetLoading();
            _tab.Image.Value = null;
            _tab.SecondaryImage.Value = null;
            
            await Task.Run(async () =>
            {
                await UpdateModels().ConfigureAwait(false);
            }, ct.Token).ConfigureAwait(false);
            return;
        }
        await UpdateModels().ConfigureAwait(false);

        return;

        async ValueTask UpdateModels()
        {
            var firstModel = await firstRequest.ConfigureAwait(false);
            var secondModel = await secondRequest.ConfigureAwait(false);
            if (firstModel is null || secondModel is null || !IsCurrent())
            {
                return;
            }

            _tab.Display(firstModel, secondModel);
            UpdateNavigationProperties();
            TriggerPreload();
        }

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

        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            var (iteration,  secondary, _) = SideBySidePair.Navigate(index, Files.Count, 
                NavigateTo.Next, SkipAmount.None);
            if (secondary.HasValue)
            {
                await IterateToIndicesAsync(iteration, secondary.Value, ct).ConfigureAwait(false);
            }
            return;
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
            var (iteration,  secondary, _) = SideBySidePair.Navigate(CurrentIndex, Files.Count, 
                NavigateTo.Next, SkipAmount.None);
            if (secondary.HasValue)
            {
                await IterateToIndicesAsync(iteration, secondary.Value, ct).ConfigureAwait(false);
            }
            return;
        }
        await IterateToIndexAsync(CurrentIndex, ct).ConfigureAwait(false);
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

        if (await WaitForNextAsync(to, SkipAmount.One, ct.Token).ConfigureAwait(false) is not { } from)
        {
            return;
        }

        int iteration;
        bool isReversed;
        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            (iteration, var secondary, isReversed) = SideBySidePair.Navigate(from.Index, from.Count, to, SkipAmount.One);
            IsReversed = isReversed;
            if (ShouldNavigateTiffEntry(to, SkipAmount.One))
            {
                return;
            }

            if (secondary.HasValue)
            {
                await IterateToIndicesAsync(iteration, secondary.Value, ct).ConfigureAwait(false);
            }
            return;
        }
        (iteration, isReversed) = IterationHelper.GetIteration(from.Index, from.Count, to, SkipAmount.One);
        IsReversed = isReversed;
        if (ShouldNavigateTiffEntry(to, SkipAmount.One))
        {
            return;
        }

        await IterateToIndexAsync(iteration, ct).ConfigureAwait(false);
    }

    public void StopRepeatedNavigation()
    {
        // Reset the throttle so the next click reacts instantly.
        _lastRepeatTime = DateTime.MinValue; 
    }

    private readonly Lock _fileWaitLock = new();
    private TaskCompletionSource? _filesChangedTcs;

    public void NotifyFileAdded() => SignalFilesChanged();

    /// <summary>
    /// Wakes everyone waiting for the file list to change.
    /// </summary>
    private void SignalFilesChanged()
    {
        lock (_fileWaitLock)
        {
            _filesChangedTcs?.TrySetResult();
            _filesChangedTcs = null;
        }
    }

    /// <summary>
    /// A task that completes the next time the file list changes, shared by everyone waiting for it.
    /// Get it before looking at <see cref="Files"/>, so a change in between is not missed.
    /// </summary>
    private Task FilesChanged()
    {
        lock (_fileWaitLock)
        {
            return (_filesChangedTcs ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).Task;
        }
    }

    /// <summary>
    /// Next shows the file after the shown one, or the pair after it. While an archive is still being extracted that may
    /// not be there yet, so Next waits for it, unless a multi-page TIFF turns its page instead.
    /// </summary>
    /// <returns>
    /// The shown file's index and the number of files, read together, which the step counts from. Null to not step: the
    /// wait for the file gave up.
    /// </returns>
    private ValueTask<(int Index, int Count)?> WaitForNextAsync(NavigateTo navigateTo, SkipAmount skipAmount,
        CancellationToken ct)
    {
        var (index, files) = ShownIndexAndFiles();
        if (navigateTo is not NavigateTo.Next || !_tab.IsArchiveExtracting.Value)
        {
            return ValueTask.FromResult<(int, int)?>((index, files.Length));
        }

        // How far past the shown file Next goes: to the next file, or to the second of the next pair
        var step = IterationHelper.SkipAmountToInt(skipAmount);
        var reach = Settings.ImageScaling.ShowImageSideBySide ? step * 2 + 1 : step;
        if (index + reach < files.Length || HasNextTiffPage(skipAmount))
        {
            return ValueTask.FromResult<(int, int)?>((index, files.Length));
        }

        return WaitForFileAfterShownAsync(reach, ct);
    }

    /// <summary>
    /// Waits until the file <paramref name="reach"/> places after the shown one is in the list, while the archive is still
    /// being extracted. It counts from the shown file each time the list changes, since a page extracted before it moves it
    /// along, so only a file after it ends the wait. The extraction finishing ends it too, and the step then goes over the
    /// whole list: it stops at the end, or goes round when looping.
    /// </summary>
    /// <returns>
    /// The shown file's index and the number of files as the wait last read them, which the step counts from, so two
    /// presses that the same file wakes both step from the shown file. Null when the wait gave up: another file was shown
    /// meanwhile, nothing came for a while, or it was cancelled.
    /// </returns>
    private async ValueTask<(int Index, int Count)?> WaitForFileAfterShownAsync(int reach, CancellationToken ct)
    {
        _tab.SetLoading();
        var (shownIndex, shownFiles) = ShownIndexAndFiles();
        var shown = FileAt(shownFiles, shownIndex);
        var timedOut = false;
        while (!ct.IsCancellationRequested)
        {
            // Taken before the list is read, so a change in between is not missed
            var changed = FilesChanged();
            var (index, files) = ShownIndexAndFiles();
            if (!IsSameFile(FileAt(files, index), shown))
            {
                // Another file was shown meanwhile, so this Next is out of date
                break;
            }

            if (index + reach < files.Length)
            {
                return (index, files.Length);
            }

            if (!_tab.IsArchiveExtracting.Value)
            {
                // The step may stop on the shown file, which brings no new title, so the title says what it shows again
                _tab.UpdateTabTitle();
                return (index, files.Length);
            }

            if (timedOut)
            {
                break;
            }

            try
            {
                await changed.WaitAsync(TimeSpan.FromSeconds(3), ct).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                // Nothing came for a while: one more look, then give up
                timedOut = true;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        // The title says what the tab shows again, instead of loading
        _tab.UpdateTabTitle();
        UpdateNavigationProperties();
        return null;
    }

    // The shown file's index with the list it is in, read together, as inserting a file before it changes both
    private (int Index, FileInfo[] Files) ShownIndexAndFiles()
    {
        lock (_filesLock)
        {
            return (CurrentIndex, _files);
        }
    }

    private static FileInfo? FileAt(FileInfo[] files, int index) =>
        index >= 0 && index < files.Length ? files[index] : null;

    private static bool IsSameFile(FileInfo? a, FileInfo? b) =>
        a is null ? b is null : b is not null && IsSamePath(a.FullName, b.FullName);

    /// <inheritdoc />
    public async ValueTask<bool> WaitForFileAsync(string path, TimeSpan timeout, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!ct.IsCancellationRequested)
        {
            var changed = FilesChanged();
            if (IndexOf(Volatile.Read(ref _files), path) >= 0)
            {
                return true;
            }

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return false;
            }

            try
            {
                await changed.WaitAsync(remaining, ct).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        return false;
    }

    private bool IsTargetActive(FileInfo targetFile, int targetIndex)
    {
        if (CurrentIndex == targetIndex)
        {
            return true;
        }
        if (CurrentIndex >= 0 && CurrentIndex < Files.Count)
        {
            return string.Equals(Files[CurrentIndex].FullName, targetFile.FullName, StringComparison.Ordinal);
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

        // Only single images reach here, so no secondary is shown; a pair goes through UpdateModels
        _tab.Display(newModel);
        UpdateNavigationProperties();
        TriggerPreload();
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

    /// <summary>
    /// Turns the multi-page TIFF the tab shows to its previous page when <see cref="IsReversed"/>, else to its next,
    /// instead of going to another file. Only stepping by one turns pages: a jump goes to the file it was given, and so
    /// does a step from the first or last page.
    /// </summary>
    /// <returns>Whether a page was turned.</returns>
    private bool ShouldNavigateTiffEntry(NavigateTo navigateTo, SkipAmount skipAmount)
    {
        if (navigateTo is not (NavigateTo.Next or NavigateTo.Previous) || skipAmount is not SkipAmount.One
            || _tab.Model?.TiffNavigation is not { } tiff)
        {
            return false;
        }

        var page = IsReversed ? tiff.CurrentPage - 1 : tiff.CurrentPage + 1;
        if (page < 0 || page >= tiff.PageCount)
        {
            return false;
        }

        _tab.ShowPage(page);
        return true;
    }

    /// <summary>
    /// Whether Next turns the shown multi-page TIFF to its next page, as <see cref="ShouldNavigateTiffEntry"/> does, rather
    /// than going to another file.
    /// </summary>
    private bool HasNextTiffPage(SkipAmount skipAmount) =>
        skipAmount is SkipAmount.One && _tab.Model?.TiffNavigation is { } tiff && tiff.CurrentPage + 1 < tiff.PageCount;

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