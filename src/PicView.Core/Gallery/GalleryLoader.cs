using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.MotionPhoto;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Core.Gallery;

public static class GalleryLoader
{
    private static CancellationTokenSource? _cts;

    public static async Task LoadGalleryAsync(TabViewModel tab, IReadOnlyList<FileInfo> files,
        IThumbnailLoader thumbnailLoader, IThumbnailCache thumbnailCache, CancellationToken ct)
    {
        if (tab.Gallery.LoadingState is GalleryLoadingState.Loading or GalleryLoadingState.Loaded)
        {
            return;
        }

        tab.Gallery.LoadingState = GalleryLoadingState.Loading;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var dockedHeight = Settings.Gallery.DockedGalleryItemSize;
        var expandedHeight = Settings.Gallery.ExpandedGalleryItemSize;
        var maxHeight = Math.Max(dockedHeight, expandedHeight);
        if (maxHeight <= 0)
        {
            maxHeight = GalleryDefaults.DefaultDockedGalleryHeight;
        }

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = _cts.Token,
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
        };

        const int batchSize = 20;

        try
        {
            for (var i = 0; i < files.Count; i += batchSize)
            {
                _cts.Token.ThrowIfCancellationRequested();

                var currentBatchSize = Math.Min(batchSize, files.Count - i);
                var batchVms = new GalleryItemViewModel[currentBatchSize];

                // 1. Parallelize the metadata extraction (Massive speed boost)
                await Parallel.ForAsync(0, currentBatchSize, parallelOptions, async (j, token) =>
                {
                    var file = files[i + j];
                    var item = new GalleryItemViewModel { FileInfo = file };

                    try
                    {
                        using var magick = new MagickImage();
                        await magick.PingAsync(file, token).ConfigureAwait(false);
                        item.PixelWidth = magick.Width;
                        item.PixelHeight = magick.Height;
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        DebugHelper.LogDebug(nameof(GalleryLoader), nameof(LoadGalleryAsync), ex);
#endif
                    }

                    var thumbData =
                        GalleryThumbInfo.GalleryThumbHolder.GetThumbData(file, item.PixelWidth, item.PixelHeight);
                    item.FileName.Value = thumbData.FileName;
                    item.FileSize.Value = thumbData.FileSize;
                    item.FileDate.Value = thumbData.FileDate;
                    item.FileLocation.Value = thumbData.FileLocation;
                    item.ImageSize.Value = thumbData.ImageSize;
                    item.IsMotionPhoto.Value = MotionPhotoDetector.TryDetect(item.FileInfo, null) is not null;

                    // 2. Assign the lazy-loading logic, but don't execute it!
                    item.ThumbnailLoaderFunc = async _ =>
                    {
                        if (thumbnailCache.TryGet(file.FullName, out var cached) && cached is not null)
                        {
                            return cached;
                        }

                        var thumb = await thumbnailLoader.GetThumbnailAsync(file, (uint)maxHeight)
                            .ConfigureAwait(false);
                        if (thumb is not null)
                        {
                            thumbnailCache.Add(tab.Id, file.FullName, thumb);
                        }

                        return thumb;
                    };

                    // Array assignment ensures perfect sorting order
                    batchVms[j] = item;
                }).ConfigureAwait(false);

                // 3. Add chunk directly to the UI
                tab.Gallery.GalleryItems.AddRange(batchVms);
            }
        }
        catch (OperationCanceledException)
        {
            if (tab.Gallery.LoadingState is GalleryLoadingState.Restarting && tab.Gallery.GalleryItems.Count > 0)
            {
                tab.Gallery.GalleryItems.Clear();
            }

            tab.Gallery.LoadingState = GalleryLoadingState.NotLoaded;
            return;
        }

        tab.Gallery.LoadingState = GalleryLoadingState.Loaded;
    }

    public static async Task ReloadGallery(TabViewModel tab, IReadOnlyList<FileInfo> files,
        IThumbnailLoader thumbnailLoader, IThumbnailCache thumbnailCache, CancellationToken ct)
    {
        tab.Gallery.LoadingState = GalleryLoadingState.Restarting;
        tab.Gallery.GalleryItems.Clear();
        await _cts.CancelAsync().ConfigureAwait(false);
        _cts.Dispose();
        _cts = null;
        await LoadGalleryAsync(tab, files, thumbnailLoader, thumbnailCache, ct).ConfigureAwait(false);
    }

    public static async ValueTask LoadGalleryIfDockedOrExpanded(TabViewModel tabViewModel, GalleryMode mode,
        IThumbnailCache thumbnailCache, IThumbnailLoader thumbnailLoader)
    {
        if (mode is GalleryMode.Docked or GalleryMode.Expanded)
        {
            if (tabViewModel.Gallery.LoadingState is GalleryLoadingState.NotLoaded)
            {
                await LoadGalleryAsync(tabViewModel,
                        tabViewModel.ImageIterator.Files,
                        thumbnailLoader,
                        thumbnailCache,
                        tabViewModel.GetTabCancellation().Token)
                    .ConfigureAwait(false);
            }
        }
    }

    public static async ValueTask ToggleGalleryAndLoadItem(TabViewModel tabViewModel, int index)
    {
        var gallery = tabViewModel.Gallery;
        if (gallery.IsGalleryExpanded.Value)
        {
            GalleryManager.ToggleGallery(gallery);
        }

        await tabViewModel.ImageIterator.SkipToIndexAsync(index, tabViewModel.GetTabCancellation())
            .ConfigureAwait(false);
    }

    public static void SortLoadedGallery(TabViewModel tab, IReadOnlyList<FileInfo> files)
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
}