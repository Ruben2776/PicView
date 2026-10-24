using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.MotionPhoto;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Core.Gallery;

public class GalleryCoordinator(TabViewModel tab, IThumbnailLoader thumbnailLoader, IThumbnailCache thumbnailCache)
    : IDisposable
{
    /// <summary>
    /// Shared by every gallery, so thumbnail loading never puts more than a few reads on the disk at once.
    /// Without a cap each realized item starts its own read, the thread pool grows to match, and the disk thrashes.
    /// </summary>
    private static readonly SemaphoreSlim ThumbnailSlots = new(Math.Clamp(Environment.ProcessorCount / 2, 2, 4));

    private CancellationTokenSource? _cts;

    public void LoadGallery(IReadOnlyList<FileInfo> files, CoreViewModel core)
    {
        if (tab.Gallery.LoadingState is GalleryLoadingState.Loading or GalleryLoadingState.Loaded)
        {
            return;
        }

        if (tab.Gallery.GalleryItems.Count > 0)
        {
            tab.Gallery.GalleryItems.Clear();
        }

        tab.Gallery.LoadingState = GalleryLoadingState.Loading;
        var cts = new CancellationTokenSource();
        _cts = cts;

        var ct = cts.Token;
        
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

                // 1. Parallelize the metadata extraction (Massive speed boost)
                Parallel.For(0, currentBatchSize, parallelOptions, j =>
                {
                    var file = files[i + j];
                    // Array assignment ensures perfect sorting order
                    batchVms[j] = CreateItem(file, GalleryFileDetails.Read(file), maxHeight, core);
                });

                // 3. Add chunk directly to the UI, unless a restart cleared the items meanwhile
                ct.ThrowIfCancellationRequested();
                tab.Gallery.GalleryItems.AddRange(batchVms);
            }
        }
        catch (OperationCanceledException)
        {
            if (_cts is not null && !ReferenceEquals(_cts, cts))
            {
                // A restart already started a newer load, which owns the items and the state
                return;
            }

            if (tab.Gallery.LoadingState is GalleryLoadingState.Restarting && tab.Gallery.GalleryItems.Count > 0)
            {
                tab.Gallery.GalleryItems.Clear();
            }

            tab.Gallery.LoadingState = GalleryLoadingState.NotLoaded;
            return;
        }

        tab.Gallery.LoadingState = GalleryLoadingState.Loaded;
    }

    /// <summary>
    /// The one way to make a gallery item: its file's details now, and its thumbnail when the item is shown.
    /// </summary>
    private GalleryItemViewModel CreateItem(FileInfo file, GalleryFileDetails? details, double maxHeight,
        CoreViewModel? core)
    {
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
            if (thumbnailCache.TryGet(file.FullName, out var cached) && cached is not null)
            {
                return cached;
            }

            // Items that scroll out of view cancel while they wait, so the slots go to what is visible
            await ThumbnailSlots.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await Task.Run(async () =>
                {
                    var thumb = await thumbnailLoader.GetThumbnailAsync(file, (uint)maxHeight, core)
                        .ConfigureAwait(false);
                    if (thumb is not null)
                    {
                        thumbnailCache.Add(tab.Id, file.FullName, thumb);
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

    #region File changes

    // The gallery follows its tab's files once it has been used: a loaded gallery changes its items, and a loading
    // one starts again from the changed list, since it reads that list as it goes.

    /// <summary>
    /// Adds an item for a file put at <paramref name="index"/> of the tab's files.
    /// </summary>
    public async ValueTask FileInsertedAsync(int index, FileInfo file, FileDetailsSource details,
        CancellationToken ct = default)
    {
        if (RestartIfLoading())
        {
            return;
        }

        if (tab.Gallery.LoadingState is not GalleryLoadingState.Loaded || index < 0 ||
            index > tab.Gallery.GalleryItems.Count)
        {
            return;
        }

        var item = CreateItem(file, await details.GetAsync(ct).ConfigureAwait(false), ThumbnailHeight(), Core);
        tab.Gallery.GalleryItems.Insert(Math.Min(index, tab.Gallery.GalleryItems.Count), item);
    }

    /// <summary>
    /// Builds the item at <paramref name="index"/> again, for a file whose contents changed.
    /// </summary>
    public async ValueTask FileReplacedAsync(int index, FileInfo file, FileDetailsSource details,
        CancellationToken ct = default)
    {
        if (tab.Gallery.LoadingState is not (GalleryLoadingState.Loading or GalleryLoadingState.Loaded) ||
            index < 0 || index >= tab.Gallery.GalleryItems.Count)
        {
            return;
        }

        var item = CreateItem(file, await details.GetAsync(ct).ConfigureAwait(false), ThumbnailHeight(), Core);
        if (index < tab.Gallery.GalleryItems.Count)
        {
            tab.Gallery.GalleryItems[index] = item;
        }
    }

    /// <summary>
    /// Removes the item of a file taken out of <paramref name="index"/> of the tab's files.
    /// </summary>
    public void FileRemoved(int index)
    {
        if (RestartIfLoading())
        {
            return;
        }

        if (tab.Gallery.LoadingState is GalleryLoadingState.Loaded && index >= 0 &&
            index < tab.Gallery.GalleryItems.Count)
        {
            tab.Gallery.GalleryItems.RemoveAt(index);
        }
    }

    private CoreViewModel? Core => tab.ParentWindowContext?.Core;

    private bool RestartIfLoading()
    {
        if (tab.Gallery.LoadingState is not GalleryLoadingState.Loading || tab.ImageIterator is not { } iterator)
        {
            return false;
        }

        var core = Core;
        _ = Task.Run(() => ReloadGallery(iterator.Files, core!));
        return true;
    }

    #endregion

    public void ReloadGallery(IReadOnlyList<FileInfo> files, CoreViewModel core)
    {
        tab.Gallery.LoadingState = GalleryLoadingState.Restarting;
        tab.Gallery.GalleryItems.Clear();
        Cancel();
        LoadGallery(files, core);
    }

    public async ValueTask ToggleGalleryAndLoadItem(int index)
    {
        var gallery = tab.Gallery;
        gallery.SelectedGalleryItemIndex.Value = -1;
        if (gallery.IsGalleryExpanded.Value)
        {
            GalleryManager.ToggleGallery(gallery);
        }

        await tab.ImageIterator!.SkipToIndexAsync(index, tab.GetTabCancellation())
            .ConfigureAwait(false);
    }

    public void SortLoadedGallery(IReadOnlyList<FileInfo> files)
    {
        if (tab.Gallery.GalleryItems is null || tab.Gallery.GalleryItems.Count <= 1 || files is null ||
            files.Count is 0)
        {
            return;
        }

        var orderMap = new Dictionary<string, int>(files.Count, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < files.Count; i++)
        {
            orderMap.TryAdd(files[i].FullName, i);
        }

        var comparer = Comparer<GalleryItemViewModel>.Create((x, y) =>
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return 1;
            }

            if (y is null)
            {
                return -1;
            }

            var xPath = x.FileInfo?.FullName;
            var yPath = y.FileInfo?.FullName;

            var indexX = -1;
            var indexY = -1;
            var hasX = xPath is not null && orderMap.TryGetValue(xPath, out indexX);
            var hasY = yPath is not null && orderMap.TryGetValue(yPath, out indexY);

            return hasX switch
            {
                true when hasY => indexX.CompareTo(indexY),
                true => -1,
                _ => hasY ? 1 : 0
            };
        });

        tab.Gallery.GalleryItems.Sort(comparer);
    }

    public void Cancel()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    /// <summary>
    /// Forgets the gallery's items when the tab browses no files anymore, so the files it browses next load their own.
    /// </summary>
    public void Clear()
    {
        Cancel();
        tab.Gallery.LoadingState = GalleryLoadingState.NotLoaded;
        tab.Gallery.GalleryItems.Clear();
    }

    public void Dispose()
    {
        Cancel();
    }
}
