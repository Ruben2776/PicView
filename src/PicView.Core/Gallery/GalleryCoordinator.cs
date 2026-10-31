using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.MotionPhoto;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.Gallery;

/// <summary>
/// Keeps a tab's Gallery Items following its file list (ADR-0010): it hears every change to the list from the tab's
/// iterator, and loads the items the first time the gallery is shown. A loading gallery edits its items like a loaded
/// one, and a load that has not added its items yet starts again. Nothing outside it starts, restarts, sorts or
/// clears a load.
/// </summary>
public class GalleryCoordinator : IDisposable
{
    /// <summary>
    /// Shared by every gallery, so thumbnail loading never puts more than a few reads on the disk at once.
    /// Without a cap each realized item starts its own read, the thread pool grows to match, and the disk thrashes.
    /// </summary>
    private static readonly SemaphoreSlim ThumbnailSlots = new(Math.Clamp(Environment.ProcessorCount / 2, 2, 4));

    /// <summary>
    /// How many files on each side of the current image a load reads before it adds the items.
    /// </summary>
    private const int NeighborsEachSide = 20;

    /// <summary>
    /// How many files a load reads in each batch after that.
    /// </summary>
    private const int BatchSize = 20;

    private readonly TabViewModel _tab;
    private readonly IThumbnailLoader _thumbnailLoader;
    private readonly IThumbnailCache _thumbnailCache;
    private readonly IDisposable _shownSubscription;

    private readonly Lock _queueLock = new();

    /// <summary>
    /// Every change to the items, the loading state and <see cref="_files"/> runs here, one at a time and in the order
    /// it came. Only a load waits for it, off the UI thread: the gallery view copies each change to the UI thread
    /// synchronously, so a caller on the UI thread that waited here could wait on itself.
    /// </summary>
    private Task _queue = Task.CompletedTask;

    /// <summary>
    /// The file list the items follow. It outlives the tab's iterator, which a Single Image disposes.
    /// </summary>
    private IReadOnlyList<FileInfo> _files = [];

    private CancellationTokenSource? _cts;
    private bool _isShown;

    public GalleryCoordinator(TabViewModel tab, IThumbnailLoader thumbnailLoader, IThumbnailCache thumbnailCache)
    {
        _tab = tab;
        _thumbnailLoader = thumbnailLoader;
        _thumbnailCache = thumbnailCache;
        _shownSubscription = tab.Gallery.Switcher.Mode
            .Select(mode => mode is not GalleryMode.Closed)
            .DistinctUntilChanged()
            .Subscribe(OnShownChanged, DebugHelper.LogError(nameof(GalleryCoordinator), nameof(OnShownChanged)));
    }

    /// <summary>
    /// Reads the details of one file. Tests replace it to record and pause the reads.
    /// </summary>
    internal Func<FileInfo, GalleryFileDetails?> ReadDetails { get; set; } = GalleryFileDetails.Read;

    private GalleryViewModel Gallery => _tab.Gallery;

    #region Following the file list

    /// <summary>
    /// Applies one change to the tab's file list. The iterator calls this while it holds its list, so the changes queue
    /// in the order they were made and the items follow the list: a loading gallery edits its items like a loaded one,
    /// and a load that has not added its items yet starts again from the changed list.
    /// </summary>
    internal void FilesChanged(FileListChange change) => _ = Enqueue(() =>
    {
        if (change.Kind is FileListChangeKind.Listed)
        {
            Listed(change.Files, change.Index);
            return;
        }

        _files = change.Files;
        switch (Gallery.LoadingState)
        {
            case GalleryLoadingState.Starting:
                StartLoad(_tab.NavigationIndex.CurrentValue);
                break;
            case GalleryLoadingState.Loading:
            case GalleryLoadingState.Loaded:
                Edit(change);
                break;
        }
    });

    /// <summary>
    /// Completes once the changes queued so far are applied. For tests.
    /// </summary>
    internal Task Idle
    {
        get
        {
            lock (_queueLock)
            {
                return _queue;
            }
        }
    }

    private Task Enqueue(Action work) => Enqueue<bool>(() =>
    {
        work();
        return true;
    });

    /// <summary>
    /// Runs <paramref name="work"/> on the queue. Awaiting the task never throws: a failure is logged, and the result
    /// is the default.
    /// </summary>
    private Task<T?> Enqueue<T>(Func<T> work)
    {
        lock (_queueLock)
        {
            var task = _queue.ContinueWith<T?>(_ =>
            {
                try
                {
                    return work();
                }
                catch (Exception e)
                {
                    DebugHelper.LogDebug(nameof(GalleryCoordinator), nameof(Enqueue), e);
                    return default;
                }
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            _queue = task;
            return task;
        }
    }

    private void Listed(IReadOnlyList<FileInfo> files, int currentIndex)
    {
        if (files.Count is 0)
        {
            // A new iterator starts empty before it lists its files, as after a Single Image. A tab never browses an
            // empty list: when its last file is deleted, the removal empties the items
            return;
        }

        var previous = _files;
        _files = files;
        if (SameOrder(previous, files))
        {
            if (Gallery.LoadingState is GalleryLoadingState.NotLoaded && _isShown)
            {
                StartLoad(currentIndex);
            }
            return;
        }

        var hasItems = Gallery.LoadingState is GalleryLoadingState.Loading or GalleryLoadingState.Loaded;
        if (hasItems && SameFiles(previous, files))
        {
            // Sorted again: the items keep their thumbnails, and a load carries on reading their sizes
            SortItems(files);
            return;
        }

        Drop();
        if (_isShown)
        {
            StartLoad(currentIndex);
        }
    }

    private void Edit(FileListChange change)
    {
        var items = Gallery.GalleryItems;
        switch (change.Kind)
        {
            case FileListChangeKind.Inserted when change.Index >= 0 && change.Index <= items.Count:
                var inserted = CreateItem(change.File!, null);
                items.Insert(change.Index, inserted);
                FillDetails(inserted, change.File!, change.Details);
                break;
            case FileListChangeKind.Removed when change.Index >= 0 && change.Index < items.Count:
                items.RemoveAt(change.Index);
                break;
            case FileListChangeKind.Replaced:
                Replace(change.Index, change.File!, change.Details);
                break;
            case FileListChangeKind.Relocated when change.Index >= 0 && change.Index < items.Count:
                Relocate(change.Index, change.File!);
                break;
        }
    }

    /// <summary>
    /// Builds the item at <paramref name="index"/> again, for a file whose contents changed, so its thumbnail loads
    /// again when it is shown.
    /// </summary>
    private void Replace(int index, FileInfo file, FileDetailsSource? details)
    {
        var items = Gallery.GalleryItems;
        if (index < 0 || index >= items.Count)
        {
            return;
        }

        var item = CreateItem(file, null);
        items[index] = item;
        FillDetails(item, file, details);
    }

    /// <summary>
    /// Points the item at <paramref name="index"/> to <paramref name="file"/>, the same file in a renamed folder. It
    /// keeps its image's size and its thumbnail, as the file itself did not change.
    /// </summary>
    private void Relocate(int index, FileInfo file)
    {
        var old = Gallery.GalleryItems[index];
        var item = CreateItem(file, old.Details);
        item.Image.Value = old.Image.Value;
        item.IsMotionPhoto.Value = old.IsMotionPhoto.Value;
        if (old.Image.Value is { } thumbnail)
        {
            _thumbnailCache.Add(_tab.Id, file.FullName, thumbnail);
        }
        Gallery.GalleryItems[index] = item;
    }

    /// <summary>
    /// Reads the details of a changed file, off the caller's thread, and writes them onto its item. The item is in
    /// place already, so the items keep matching the list. A file that cannot be read counts as read and stays square.
    /// </summary>
    private void FillDetails(GalleryItemViewModel item, FileInfo file, FileDetailsSource? source)
    {
        _ = Task.Run(async () =>
        {
            var details = source is null
                ? ReadDetails(file)
                : await source.GetAsync().ConfigureAwait(false);
            _ = Enqueue(() =>
            {
                item.SetDetails(details ?? GalleryFileDetails.Unreadable);
                Gallery.RaiseSizesChanged();
            });
        });
    }

    private void SortItems(IReadOnlyList<FileInfo> files)
    {
        var order = new Dictionary<string, int>(files.Count, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < files.Count; i++)
        {
            order.TryAdd(files[i].FullName, i);
        }

        Gallery.GalleryItems.Sort(Comparer<GalleryItemViewModel>.Create((x, y) =>
            Position(x).CompareTo(Position(y))));
        return;

        int Position(GalleryItemViewModel item) =>
            item.FileInfo is { } file && order.TryGetValue(file.FullName, out var index) ? index : int.MaxValue;
    }

    private static bool SameOrder(IReadOnlyList<FileInfo> a, IReadOnlyList<FileInfo> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i].FullName, b[i].FullName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        return true;
    }

    private static bool SameFiles(IReadOnlyList<FileInfo> a, IReadOnlyList<FileInfo> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        var paths = new HashSet<string>(a.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var file in a)
        {
            paths.Add(file.FullName);
        }
        foreach (var file in b)
        {
            if (!paths.Contains(file.FullName))
            {
                return false;
            }
        }
        return paths.Count == b.Count;
    }

    #endregion

    #region Loading

    private void OnShownChanged(bool isShown) => _ = Enqueue(() =>
    {
        _isShown = isShown;
        if (isShown && Gallery.LoadingState is GalleryLoadingState.NotLoaded && _files.Count > 0)
        {
            StartLoad(_tab.NavigationIndex.CurrentValue);
        }
    });

    /// <summary>
    /// Forgets the items and their thumbnails, for a list they no longer match.
    /// </summary>
    private void Drop()
    {
        Cancel();
        Gallery.LoadingState = GalleryLoadingState.NotLoaded;
        if (Gallery.GalleryItems.Count > 0)
        {
            Gallery.GalleryItems.Clear();
        }
        _thumbnailCache.RemoveOwner(_tab.Id);
    }

    /// <summary>
    /// Loads the items of <see cref="_files"/> again from the start, off the caller's thread.
    /// </summary>
    /// <param name="currentIndex">The index of the current image, which the load reads the sizes around first.</param>
    private void StartLoad(int currentIndex)
    {
        Cancel();
        if (Gallery.GalleryItems.Count > 0)
        {
            Gallery.GalleryItems.Clear();
        }

        var cts = new CancellationTokenSource();
        _cts = cts;
        Gallery.LoadingState = GalleryLoadingState.Starting;
        var files = _files;
        // Taken now, as a restart can dispose the source before the thread pool runs the load
        var ct = cts.Token;
        _ = Task.Run(() => LoadAsync(files, currentIndex, ct));
    }

    /// <summary>
    /// Makes an item for every file and adds them all at once, then reads the files' details onto them: first those
    /// around the current image, before the items are shared, then the rest in batches, nearest the current image
    /// first.
    /// </summary>
    private async Task LoadAsync(IReadOnlyList<FileInfo> files, int currentIndex, CancellationToken ct)
    {
        try
        {
            var maxHeight = ThumbnailHeight();
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = ct,
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
            };

            // The array keeps the items in the files' order
            var items = new GalleryItemViewModel[files.Count];
            Parallel.For(0, items.Length, parallelOptions, i =>
            {
                items[i] = CreateItem(files[i], null, maxHeight);
            });

            // The current image and the files around it come first. Nothing else sees these items yet, so their
            // details need no queue
            var center = Math.Clamp(currentIndex, 0, Math.Max(0, files.Count - 1));
            var start = Math.Max(0, center - NeighborsEachSide);
            var end = Math.Min(files.Count, center + NeighborsEachSide + 1);
            Parallel.For(start, end, parallelOptions, i =>
            {
                items[i].SetDetails(ReadDetails(files[i]) ?? GalleryFileDetails.Unreadable);
            });

            // Cancellation happens on the queue too, so a change that started the load again owns the items
            await Enqueue(() =>
            {
                if (!ct.IsCancellationRequested)
                {
                    Gallery.GalleryItems.AddRange(items);
                    Gallery.LoadingState = GalleryLoadingState.Loading;
                }
            }).ConfigureAwait(false);

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                // Chosen on the queue, from the items as they are then, so navigation and edits steer the next batch
                var batch = await Enqueue(() => NextUnread(BatchSize)).ConfigureAwait(false);
                if (batch is null)
                {
                    // Choosing the batch threw, and the queue logged it. The items still waiting stay square, and
                    // the state stays Loading
                    return;
                }

                if (batch.Length is 0)
                {
                    break;
                }

                // Read the batch's details in parallel; the array keeps them in the batch's order
                var details = new GalleryFileDetails[batch.Length];
                Parallel.For(0, batch.Length, parallelOptions, i =>
                {
                    details[i] = ReadDetails(batch[i].FileInfo!) ?? GalleryFileDetails.Unreadable;
                });

                await Enqueue(() =>
                {
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    for (var i = 0; i < batch.Length; i++)
                    {
                        // A changed file's details come from FillDetails, which reads the file once it is ready, so
                        // its write wins in either order. Only an item still without details takes this batch's read
                        if (batch[i].Details is null)
                        {
                            batch[i].SetDetails(details[i]);
                        }
                    }
                    Gallery.RaiseSizesChanged();
                }).ConfigureAwait(false);
            }

            await Enqueue(() =>
            {
                if (!ct.IsCancellationRequested)
                {
                    Gallery.LoadingState = GalleryLoadingState.Loaded;
                }
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Whatever canceled the load set the items and the state
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GalleryCoordinator), nameof(LoadAsync), e);
        }
    }

    /// <summary>
    /// The next <paramref name="count"/> items whose details are not read yet, nearest the current image first. It
    /// walks outward from the current image, one item after it and then one before it, until it has enough or has
    /// passed both ends of the list, so near an end the other side makes up the difference.
    /// </summary>
    private GalleryItemViewModel[] NextUnread(int count)
    {
        // Only the queue changes the items, and this runs on it, so the count holds while it walks
        var items = Gallery.GalleryItems;
        var total = items.Count;
        if (total is 0)
        {
            return [];
        }

        var center = Math.Clamp(_tab.NavigationIndex.CurrentValue, 0, total - 1);
        var batch = new List<GalleryItemViewModel>(count);
        for (var distance = 0; batch.Count < count; distance++)
        {
            var after = center + distance;
            var before = center - distance;
            if (after >= total && before < 0)
            {
                break;
            }

            if (after < total && items[after] is { Details: null } unreadAfter)
            {
                batch.Add(unreadAfter);
            }
            if (distance > 0 && before >= 0 && batch.Count < count && items[before] is { Details: null } unreadBefore)
            {
                batch.Add(unreadBefore);
            }
        }
        return [.. batch];
    }

    private void Cancel()
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    private GalleryItemViewModel CreateItem(FileInfo file, GalleryFileDetails? details) =>
        CreateItem(file, details, ThumbnailHeight());

    /// <summary>
    /// The one way to make a gallery item: its file's details now, and its thumbnail when the item is shown.
    /// </summary>
    private GalleryItemViewModel CreateItem(FileInfo file, GalleryFileDetails? details, double maxHeight)
    {
        var core = _tab.ParentWindowContext?.Core;
        var item = new GalleryItemViewModel { FileInfo = file };

        var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(file);
        item.FileName.Value = thumbData.FileName;
        item.FileSize.Value = thumbData.FileSize;
        item.FileDate.Value = thumbData.FileDate;
        item.FileLocation.Value = thumbData.FileLocation;
        // The size text comes with the details, so an item whose details aren't read yet shows no size rather than 0 x 0
        if (details is not null)
        {
            item.SetDetails(details);
        }

        // Assign the lazy-loading logic, but don't execute it!
        item.ThumbnailLoaderFunc = async cancellationToken =>
        {
            if (_thumbnailCache.TryGet(file.FullName, out var cached) && cached is not null)
            {
                return cached;
            }

            // Items that scroll out of view cancel while they wait, so the slots go to what is visible
            await ThumbnailSlots.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await Task.Run(async () =>
                {
                    var thumb = await _thumbnailLoader.GetThumbnailAsync(file, (uint)maxHeight, core)
                        .ConfigureAwait(false);
                    if (thumb is not null)
                    {
                        _thumbnailCache.Add(_tab.Id, file.FullName, thumb);
                    }

                    // Detection reads the file again up to three times, so skip it while the badges are hidden
                    if (Settings.Gallery.ShowMotionPhotoBadges)
                    {
                        item.IsMotionPhoto.Value = MotionPhotoDetector.TryDetect(
                            item.FileInfo, item.Details?.XmpPacket, fastMode: true) is not null;
                    }

                    return thumb;
                }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ThumbnailSlots.Release();
            }
        };

        return item;
    }

    private static double ThumbnailHeight()
    {
        var maxHeight = Math.Max(Settings.Gallery.DockedGalleryItemSize, Settings.Gallery.ExpandedGalleryItemSize);
        return maxHeight <= 0 ? GalleryDefaults.DefaultDockedGalleryHeight : maxHeight;
    }

    #endregion

    public async ValueTask ToggleGalleryAndLoadItem(int index)
    {
        var gallery = Gallery;
        gallery.SelectedGalleryItemIndex.Value = -1;
        if (gallery.IsGalleryExpanded.Value)
        {
            gallery.Switcher.ToggleExpanded();
        }

        await _tab.ImageIterator.SkipToIndexAsync(index, _tab.GetTabCancellation())
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        _shownSubscription.Dispose();
        _ = Enqueue(Cancel);
        GC.SuppressFinalize(this);
    }
}
