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
/// iterator, and loads the items the first time the gallery is shown. Nothing outside it starts, restarts, sorts or
/// clears a load.
/// </summary>
public class GalleryCoordinator : IDisposable
{
    /// <summary>
    /// Shared by every gallery, so thumbnail loading never puts more than a few reads on the disk at once.
    /// Without a cap each realized item starts its own read, the thread pool grows to match, and the disk thrashes.
    /// </summary>
    private static readonly SemaphoreSlim ThumbnailSlots = new(Math.Clamp(Environment.ProcessorCount / 2, 2, 4));

    private readonly TabViewModel _tab;
    private readonly IThumbnailLoader _thumbnailLoader;
    private readonly IThumbnailCache _thumbnailCache;
    private readonly IDisposable _shownSubscription;

    private readonly Lock _queueLock = new();

    /// <summary>
    /// Every change to the items, the loading state and <see cref="_files"/> runs here, one at a time and in the order
    /// it came. Nothing waits for it: the gallery view copies each change to the UI thread synchronously, so a caller
    /// on the UI thread that waited here could wait on itself.
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

    private GalleryViewModel Gallery => _tab.Gallery;

    #region Following the file list

    /// <summary>
    /// Applies one change to the tab's file list. The iterator calls this while it holds its list, so the changes queue
    /// in the order they were made and the items follow the list: a loaded gallery edits its items, and a loading one
    /// starts again from the changed list.
    /// </summary>
    internal void FilesChanged(FileListChange change) => Enqueue(() =>
    {
        if (change.Kind is FileListChangeKind.Listed)
        {
            Listed(change.Files);
            return;
        }

        _files = change.Files;
        switch (Gallery.LoadingState)
        {
            case GalleryLoadingState.Loading when change.Kind is FileListChangeKind.Replaced:
                Replace(change.Index, change.File!, change.Details);
                break;
            case GalleryLoadingState.Loading:
                StartLoad();
                break;
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

    private void Enqueue(Action work)
    {
        lock (_queueLock)
        {
            _queue = _queue.ContinueWith(_ =>
            {
                try
                {
                    work();
                }
                catch (Exception e)
                {
                    DebugHelper.LogDebug(nameof(GalleryCoordinator), nameof(Enqueue), e);
                }
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        }
    }

    private void Listed(IReadOnlyList<FileInfo> files)
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
                StartLoad();
            }
            return;
        }

        if (Gallery.LoadingState is GalleryLoadingState.Loaded && SameFiles(previous, files))
        {
            // Sorted again: the items keep their thumbnails
            SortItems(files);
            return;
        }

        Drop();
        if (_isShown)
        {
            StartLoad();
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
        var item = CreateItem(file, new GalleryFileDetails(old.PixelWidth, old.PixelHeight, null));
        item.Image.Value = old.Image.Value;
        item.IsMotionPhoto.Value = old.IsMotionPhoto.Value;
        if (old.Image.Value is { } thumbnail)
        {
            _thumbnailCache.Add(_tab.Id, file.FullName, thumbnail);
        }
        Gallery.GalleryItems[index] = item;
    }

    /// <summary>
    /// Puts the item of a changed file in place first, so the items keep matching the list, and swaps in the item
    /// with the file's details once they are read.
    /// </summary>
    private void FillDetails(GalleryItemViewModel placeholder, FileInfo file, FileDetailsSource? source)
    {
        _ = Task.Run(async () =>
        {
            var details = source is null
                ? GalleryFileDetails.Read(file)
                : await source.GetAsync().ConfigureAwait(false);
            if (details is null)
            {
                return;
            }

            var item = CreateItem(file, details);
            Enqueue(() =>
            {
                var index = Gallery.GalleryItems.IndexOf(placeholder);
                if (index >= 0)
                {
                    Gallery.GalleryItems[index] = item;
                }
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

    private void OnShownChanged(bool isShown) => Enqueue(() =>
    {
        _isShown = isShown;
        if (isShown && Gallery.LoadingState is GalleryLoadingState.NotLoaded && _files.Count > 0)
        {
            StartLoad();
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
    private void StartLoad()
    {
        Cancel();
        if (Gallery.GalleryItems.Count > 0)
        {
            Gallery.GalleryItems.Clear();
        }

        var cts = new CancellationTokenSource();
        _cts = cts;
        Gallery.LoadingState = GalleryLoadingState.Loading;
        var files = _files;
        _ = Task.Run(() => Load(files, cts.Token));
    }

    private void Load(IReadOnlyList<FileInfo> files, CancellationToken ct)
    {
        var maxHeight = ThumbnailHeight();
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
        };

        const int batchSize = 20;

        try
        {
            for (var i = 0; i < files.Count; i += batchSize)
            {
                ct.ThrowIfCancellationRequested();

                var currentBatchSize = Math.Min(batchSize, files.Count - i);
                var batchVms = new GalleryItemViewModel[currentBatchSize];

                // Read the files' details in parallel; the array keeps them in the files' order
                Parallel.For(0, currentBatchSize, parallelOptions, j =>
                {
                    var file = files[i + j];
                    batchVms[j] = CreateItem(file, GalleryFileDetails.Read(file), maxHeight);
                });

                // Cancellation happens on the queue too, so a change that started the load again owns the items
                Enqueue(() =>
                {
                    if (!ct.IsCancellationRequested)
                    {
                        Gallery.GalleryItems.AddRange(batchVms);
                    }
                });
            }

            Enqueue(() =>
            {
                if (!ct.IsCancellationRequested)
                {
                    Gallery.LoadingState = GalleryLoadingState.Loaded;
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Whatever canceled the load set the items and the state
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GalleryCoordinator), nameof(Load), e);
        }
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
        var item = new GalleryItemViewModel
        {
            FileInfo = file,
            PixelWidth = details?.PixelWidth ?? 0,
            PixelHeight = details?.PixelHeight ?? 0
        };
        var xmpPacket = details?.XmpPacket;

        var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(file, item.PixelWidth, item.PixelHeight);
        item.FileName.Value = thumbData.FileName;
        item.FileSize.Value = thumbData.FileSize;
        item.FileDate.Value = thumbData.FileDate;
        item.FileLocation.Value = thumbData.FileLocation;
        item.ImageSize.Value = thumbData.ImageSize;

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
                        item.IsMotionPhoto.Value =
                            MotionPhotoDetector.TryDetect(item.FileInfo, xmpPacket, fastMode: true) is not null;
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
        Enqueue(Cancel);
        GC.SuppressFinalize(this);
    }
}
