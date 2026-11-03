using PicView.Core.ArchiveHandling;
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
        PublishPosition();
        // A new listing at the same index and count changes nothing the titles subscribe to, yet the shown file's
        // place in it is new, so the titles are told here (ADR-0015)
        _tab.Titles.Mark();
    }

    #region File list changes

    /// <inheritdoc />
    public int Insert(FileInfo file, Func<string, string, int> compareNames, FileDetailsSource? details = null)
    {
        int index;
        lock (_filesLock)
        {
            // Listed once, whichever load, folder event or archive opening lists the file first
            index = IndexOf(_files, file.FullName);
            if (index >= 0)
            {
                ReplaceAt(index, file, details);
                return index;
            }

            index = InsertInOrder(file, compareNames, details);
        }
        PublishPosition();
        return index;
    }

    /// <inheritdoc />
    public int InsertMissing(IReadOnlyCollection<string> paths, string folder, Func<string, string, int> compareNames)
    {
        // Looked up before the lock, as it reads the disk: whether each file is there, which also reads its size and
        // dates, so a sort by those compares them under the lock without reading the disk. One set of the listed paths,
        // compared as IsSamePath does, keeps the look linear for an archive of thousands of pages
        var files = Volatile.Read(ref _files);
        var listed = new HashSet<string>(files.Length, StringComparer.OrdinalIgnoreCase);
        foreach (var listedFile in files)
        {
            listed.Add(listedFile.FullName);
        }

        List<FileInfo>? missing = null;
        foreach (var path in paths)
        {
            if (!listed.Contains(path) && new FileInfo(path) is { Exists: true } file)
            {
                (missing ??= []).Add(file);
            }
        }

        if (missing is null)
        {
            return 0;
        }

        var inserted = 0;
        lock (_filesLock)
        {
            if (_files.Length is 0 || !IsInFolder(_files[0].FullName, folder))
            {
                return 0;
            }

            foreach (var file in missing)
            {
                if (IndexOf(_files, file.FullName) < 0)
                {
                    InsertInOrder(file, compareNames, details: null);
                    inserted++;
                }
            }
        }

        if (inserted > 0)
        {
            PublishPosition();
            // The pages after the shown one are loaded ahead as they are listed, so a Next finds them loaded
            TriggerPreload();
        }
        return inserted;
    }

    // Inserts a file the list does not have at its sorted position. Callers hold _filesLock
    private int InsertInOrder(FileInfo file, Func<string, string, int> compareNames, FileDetailsSource? details)
    {
        var list = new List<FileInfo>(_files);
        var index = FileSortOrder.InsertSorted(list, file, compareNames);
        if (CurrentIndex >= 0 && index <= CurrentIndex)
        {
            CurrentIndex++;
        }
        // The file beside it stays the same too, as an archive's extraction inserts pages before the shown pair
        if (SecondaryCurrentIndex >= 0 && index <= SecondaryCurrentIndex)
        {
            SecondaryCurrentIndex++;
        }
        var files = list.ToArray();
        Publish(files, new FileListChange(FileListChangeKind.Inserted, files, index, file, details));
        return index;
    }

    // Callers hold _filesLock
    private void ReplaceAt(int index, FileInfo file, FileDetailsSource? details)
    {
        var files = (FileInfo[])_files.Clone();
        files[index] = file;
        Publish(files, new FileListChange(FileListChangeKind.Replaced, files, index, file, details));
    }

    // A file right in the folder, not in a subfolder of it
    private static bool IsInFolder(string path, string folder) =>
        Path.GetDirectoryName(path.AsSpan())
            .Equals(Path.TrimEndingDirectorySeparator(folder.AsSpan()), StringComparison.OrdinalIgnoreCase);

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
            if (index < SecondaryCurrentIndex)
            {
                SecondaryCurrentIndex--;
            }
            else if (SecondaryCurrentIndex >= remaining.Length)
            {
                SecondaryCurrentIndex = remaining.Length - 1;
            }
            Publish(remaining, new FileListChange(FileListChangeKind.Removed, remaining, index));
        }
        PublishPosition();
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

            ReplaceAt(index, file, details);
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
            var currentPath = FileAt(_files, CurrentIndex)?.FullName;
            var secondaryPath = FileAt(_files, SecondaryCurrentIndex)?.FullName;
            var list = new List<FileInfo>(_files);
            removed = IndexOf(_files, oldPath);
            if (removed >= 0)
            {
                list.RemoveAt(removed);
            }
            inserted = FileSortOrder.InsertSorted(list, newFile, compareNames);

            // Follow the current file, and the one beside it, under the new name if it was the one moved
            CurrentIndex = IndexFollowing(currentPath, CurrentIndex);
            SecondaryCurrentIndex = IndexFollowing(secondaryPath, SecondaryCurrentIndex);
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

            int IndexFollowing(string? path, int index)
            {
                if (path is null)
                {
                    return index;
                }

                var followed = IsSamePath(path, oldPath) ? newFile.FullName : path;
                var found = list.FindIndex(x => IsSamePath(x.FullName, followed));
                return found >= 0 ? found : index;
            }
        }
        PublishPosition();
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
    
    // Where the iterator is, told to the tab, which derives whether Next and Prev run from it (ADR-0017)
    private void PublishPosition() => PublishPosition(CurrentIndex, Files.Count);

    private void PublishPosition(int index, int count) => _tab.SetPosition(index, count);

    public async ValueTask NavigateAsync(NavigateTo navigateTo, SkipAmount skipAmount, CancellationTokenSource ct)
    {
        if (await WaitForNextAsync(navigateTo, skipAmount, ct.Token).ConfigureAwait(false) is not { } from)
        {
            return;
        }

        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            var (currentIndex, secondaryIndex, isReversed) = SideBySidePair.Navigate(from.Index, from.Files.Length, navigateTo, skipAmount);
            IsReversed = isReversed;
            if (ShouldNavigateTiffEntry(navigateTo, skipAmount))
            {
                return;
            }

            if (secondaryIndex is { } secondary)
            {
                await IterateToIndicesAsync(from.Files, currentIndex, secondary, ct).ConfigureAwait(false);
            }
            else
            {
                await IterateToIndexAsync(from.Files, currentIndex, ct).ConfigureAwait(false);
            }
        }
        else
        {
            var (iteration, isReversed) = IterationHelper.GetIteration(from.Index, from.Files.Length, navigateTo, skipAmount);
            IsReversed = isReversed;
            if (ShouldNavigateTiffEntry(navigateTo, skipAmount))
            {
                return;
            }

            await IterateToIndexAsync(from.Files, iteration, ct).ConfigureAwait(false);
        }
    }

    public ValueTask IterateToIndexAsync(int index, CancellationTokenSource ct) =>
        IterateToIndexAsync(Volatile.Read(ref _files), index, ct);

    /// <param name="files">The list <paramref name="index"/> counts in. The file there is shown wherever it is in the
    /// list now, as an archive's extraction inserts pages before it meanwhile.</param>
    private async ValueTask IterateToIndexAsync(FileInfo[] files, int index, CancellationTokenSource ct)
    {
        if (index < 0 || index >= files.Length)
        {
            return;
        }

        var targetFile = files[index];
        // A single image has no secondary, and a pair that is still loading is no longer the current one
        if (MakeCurrent(files, index, -1) is null)
        {
            return;
        }

        var request = Cache.GetAsync(_tab.Id, targetFile, ct.Token);
        if (!request.IsCompleted)
        {
            // Still loading, show thumbnail while waiting
            var ticket = _tab.SetLoading();
            try
            {
                await Task.Run(async () =>
                {
                    var thumb = GetThumbnail(targetFile);
                    if (IsTargetActive(targetFile))
                    {
                        _tab.ShowStandIn(thumb);
                    }
                    var model = await request.ConfigureAwait(false);
                    if (model is not null && IsTargetActive(targetFile))
                    {
                        UpdateModel(model);
                    }
                }, ct.Token).ConfigureAwait(false);
            }
            finally
            {
                // A step that showed nothing, as when it was cancelled or overtaken, leaves its Loading; one that showed
                // an image already ended every phase, and a newer load's phase has a newer ticket
                _tab.EndLoading(ticket);
            }
            return;
        }
        UpdateModel(await request.ConfigureAwait(false));
    }
    
    public ValueTask IterateToIndicesAsync(int index, int secondaryIndex, CancellationTokenSource ct) =>
        IterateToIndicesAsync(Volatile.Read(ref _files), index, secondaryIndex, ct);

    /// <param name="files">The list the indices count in, as <see cref="IterateToIndexAsync(FileInfo[], int, CancellationTokenSource)"/>
    /// takes it.</param>
    private async ValueTask IterateToIndicesAsync(FileInfo[] files, int index, int secondaryIndex, CancellationTokenSource ct)
    {
        if (index < 0 || index >= files.Length || secondaryIndex < 0 || secondaryIndex >= files.Length)
        {
            return;
        }

        var firstFile = files[index];
        var secondaryFile = files[secondaryIndex];
        if (MakeCurrent(files, index, secondaryIndex) is null)
        {
            return;
        }

        var firstRequest = Cache.GetAsync(_tab.Id, firstFile, ct.Token);
        var secondRequest = Cache.GetAsync(_tab.Id, secondaryFile, ct.Token);
        if (!firstRequest.IsCompleted || !secondRequest.IsCompleted)
        {
            var ticket = _tab.ShowLoading(null);

            try
            {
                await Task.Run(async () =>
                {
                    await UpdateModels().ConfigureAwait(false);
                }, ct.Token).ConfigureAwait(false);
            }
            finally
            {
                // As the single step: a pair that showed nothing leaves its Loading, one that showed already ended it
                _tab.EndLoading(ticket);
            }
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
            PublishPosition();
            TriggerPreload();
        }

        // Told by the files, as a page inserted before the pair while it loads moves both along
        bool IsCurrent()
        {
            lock (_filesLock)
            {
                return IsSameFile(FileAt(_files, CurrentIndex), firstFile) &&
                       IsSameFile(FileAt(_files, SecondaryCurrentIndex), secondaryFile);
            }
        }
    }

    /// <summary>
    /// Makes the file at <paramref name="index"/> in <paramref name="files"/> the current one, and the file at
    /// <paramref name="secondaryIndex"/> the one beside it, -1 for none. The files are found where they are in the list
    /// now, which differs from <paramref name="files"/> once a file was inserted or removed since.
    /// </summary>
    /// <returns>Their indices now, or null when one of them is gone.</returns>
    private (int Index, int Secondary)? MakeCurrent(FileInfo[] files, int index, int secondaryIndex)
    {
        lock (_filesLock)
        {
            // The same list, as on almost every step, needs no search
            var isSameList = ReferenceEquals(files, _files);
            var current = isSameList ? index : IndexOf(_files, files[index].FullName);
            var secondary = secondaryIndex < 0 || isSameList ? secondaryIndex : IndexOf(_files, files[secondaryIndex].FullName);
            if (current < 0 || (secondaryIndex >= 0 && secondary < 0))
            {
                return null;
            }

            CurrentIndex = current;
            SecondaryCurrentIndex = secondary;
            return (current, secondary);
        }
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
                KeepTheShownPage();
                await IterateToIndicesAsync(iteration, secondary.Value, ct).ConfigureAwait(false);
            }
            return;
        }
        KeepTheShownPage();
        await IterateToIndexAsync(CurrentIndex, ct).ConfigureAwait(false);
    }

    // A reload shows the page of a multi-page TIFF the tab shows again, so it records nothing. Without this the file would
    // show at its first page, as a new view that drops the entries after it
    private void KeepTheShownPage()
    {
        if (_tab.Model is { FileInfo: { } file, TiffNavigation: { PageCount: > 1 } tiff })
        {
            _tab.RequestPage(file.FullName, tiff.CurrentPage);
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

        if (await WaitForNextAsync(to, SkipAmount.One, ct.Token).ConfigureAwait(false) is not { } from)
        {
            return;
        }

        int iteration;
        bool isReversed;
        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            (iteration, var secondary, isReversed) = SideBySidePair.Navigate(from.Index, from.Files.Length, to, SkipAmount.One);
            IsReversed = isReversed;
            if (ShouldNavigateTiffEntry(to, SkipAmount.One))
            {
                return;
            }

            if (secondary.HasValue)
            {
                await IterateToIndicesAsync(from.Files, iteration, secondary.Value, ct).ConfigureAwait(false);
            }
            return;
        }
        (iteration, isReversed) = IterationHelper.GetIteration(from.Index, from.Files.Length, to, SkipAmount.One);
        IsReversed = isReversed;
        if (ShouldNavigateTiffEntry(to, SkipAmount.One))
        {
            return;
        }

        await IterateToIndexAsync(from.Files, iteration, ct).ConfigureAwait(false);
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
    /// The shown file's index and the list it is in, read together, which the step counts in. Null to not step: the wait
    /// for the file gave up.
    /// </returns>
    private ValueTask<(int Index, FileInfo[] Files)?> WaitForNextAsync(NavigateTo navigateTo, SkipAmount skipAmount,
        CancellationToken ct)
    {
        // The state before the list: an extraction leaves the extracting state once its last pages are listed, so a list
        // read after it has them all. The wait reads both again, so this one needs no FilesChanged
        var isExtracting = navigateTo is NavigateTo.Next && _tab.Archives.State.Value is ExtractionState.Extracting;
        var shown = ShownIndexAndFiles();
        if (!isExtracting)
        {
            return ValueTask.FromResult<(int, FileInfo[])?>(shown);
        }

        // How far past the shown file Next goes: to the next file, or to the second of the next pair
        var step = IterationHelper.SkipAmountToInt(skipAmount);
        var reach = Settings.ImageScaling.ShowImageSideBySide ? step * 2 + 1 : step;
        if (shown.Index + reach < shown.Files.Length || HasNextTiffPage(skipAmount))
        {
            return ValueTask.FromResult<(int, FileInfo[])?>(shown);
        }

        return WaitForFileAfterShownAsync(reach, ct);
    }

    /// <summary>
    /// Waits until the file <paramref name="reach"/> places after the shown one is in the list, while the archive is still
    /// being extracted, however long the page takes: the extraction may be starved while the gallery loads thumbnails. It
    /// counts from the shown file each time the list changes, since a page extracted before it moves it along, so only a
    /// file after it ends the wait. The extraction finishing ends it too, and the step then goes over the whole list: it
    /// stops at the end, or goes round when looping.
    /// </summary>
    /// <returns>
    /// The shown file's index and the list as the wait last read them, which the step counts in, so two presses that the
    /// same file wakes both step from the shown file. Null when the wait gave up: another file was shown meanwhile, the
    /// extraction was stopped, or it was cancelled.
    /// </returns>
    private async ValueTask<(int Index, FileInfo[] Files)?> WaitForFileAfterShownAsync(int reach, CancellationToken ct)
    {
        var ticket = _tab.SetLoading();
        var (shownIndex, shownFiles) = ShownIndexAndFiles();
        var shown = FileAt(shownFiles, shownIndex);
        while (!ct.IsCancellationRequested)
        {
            // Taken before the state and the list are read, so a change in between is not missed
            var changed = FilesChanged();
            // The state before the list, as in WaitForNextAsync: once it is not extracting, the list has every page
            var state = _tab.Archives.State.Value;
            var (index, files) = ShownIndexAndFiles();
            if (!IsSameFile(FileAt(files, index), shown))
            {
                // Another file was shown meanwhile, so this Next is out of date
                break;
            }

            if (index + reach < files.Length)
            {
                return (index, files);
            }

            if (state is not ExtractionState.Extracting)
            {
                // Stopped, not finished, as when another archive starts opening: the list stays as it is, so stepping
                // on it would go to the wrong page
                if (state is ExtractionState.Stopped)
                {
                    break;
                }

                // The step may stop on the shown file, which shows nothing new, so the Loading phase ends here
                _tab.EndLoading(ticket);
                return (index, files);
            }

            try
            {
                await changed.WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        // The wait gave up, so the titles say what the tab shows again, instead of Loading
        _tab.EndLoading(ticket);
        PublishPosition();
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

    // Told by the file, as the pair's IsCurrent is: a page inserted before the target while it loads moves it along, and
    // another step can then land on its old index
    private bool IsTargetActive(FileInfo targetFile)
    {
        var (index, files) = ShownIndexAndFiles();
        return IsSameFile(FileAt(files, index), targetFile);
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
        PublishPosition();
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