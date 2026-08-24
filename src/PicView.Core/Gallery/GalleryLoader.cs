using PicView.Core.DebugTools;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Core.Gallery;

public static class GalleryLoader
{
    private static CancellationTokenSource? _cts;
    public static async Task LoadGalleryAsync(TabViewModel tab, IReadOnlyList<FileInfo> files, IThumbnailLoader thumbnailLoader, IThumbnailCache thumbnailCache, CancellationToken ct)
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

        const int batchSize = 20;
        var batchList = new List<GalleryItemViewModel>(batchSize);

        // Populate items with metadata
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            
            var item = new GalleryItemViewModel
            {
                FileInfo = file
            };
            
            var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(file);
            item.FileName.Value = thumbData.FileName;
            item.FileSize.Value = thumbData.FileSize;
            item.FileDate.Value = thumbData.FileDate;
            item.FileLocation.Value = thumbData.FileLocation;
            
            batchList.Add(item);

            if (batchList.Count >= batchSize)
            {
                tab.Gallery.GalleryItems.AddRange(batchList);
                batchList.Clear();
            }
        }
        
        // Add any remaining items in the final batch
        if (batchList.Count > 0)
        {
            tab.Gallery.GalleryItems.AddRange(batchList);
        }

        // Load thumbnails asynchronously
        var parallelOptions = new ParallelOptions 
        { 
            CancellationToken = _cts.Token, 
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2)
        };
        
        try 
        {
            if (thumbnailCache.IsEmpty)
            {
                await Parallel.ForAsync(0, tab.Gallery.GalleryItems.Count, parallelOptions,
                async (i, _) =>
                {
                    ct.ThrowIfCancellationRequested();
                    if (_cts is null || ct.IsCancellationRequested || _cts.IsCancellationRequested)
                    {
                        parallelOptions.CancellationToken.ThrowIfCancellationRequested();
                        throw new OperationCanceledException();
                    }
                    var item = tab.Gallery.GalleryItems[i];
                    await LoadItem(item).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            else
            {
                await Parallel.ForAsync(0, tab.Gallery.GalleryItems.Count, parallelOptions,
                async (i, _) =>
                {
                    ct.ThrowIfCancellationRequested();
                    if (_cts is null || ct.IsCancellationRequested || _cts.IsCancellationRequested)
                    {
                        parallelOptions.CancellationToken.ThrowIfCancellationRequested();
                        throw new OperationCanceledException();
                    }
                    var item = tab.Gallery.GalleryItems[i];
                    await CheckAndLoad(item).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            if (tab.Gallery.LoadingState is GalleryLoadingState.Restarting)
            {
                if(tab.Gallery.GalleryItems.Count > 0)
                {
                    tab.Gallery.GalleryItems.Clear();
                }
            }
            tab.Gallery.LoadingState = GalleryLoadingState.NotLoaded;
            return;
        }
        
        tab.Gallery.LoadingState = GalleryLoadingState.Loaded;
        return;

        async ValueTask CheckAndLoad(GalleryItemViewModel item)
        {
            if (item.FileInfo is null)
            {
                DebugHelper.LogDebug(nameof(GalleryLoader), nameof(LoadGalleryAsync), "Invalid file");
                return;
            }
            
            object? thumb;
            if (thumbnailCache.TryGet(item.FileInfo.FullName, out var cached))
            {
                thumb = cached;
            }
            else
            {
                thumb = await thumbnailLoader.GetThumbnailAsync(item.FileInfo, (uint)maxHeight).ConfigureAwait(false);
            }

            if (thumb is not null)
            {
                thumbnailCache.Add(tab.Id, item.FileInfo.FullName, thumb);
            }
            item.Image.Value = thumb;
        }
        
        async ValueTask LoadItem(GalleryItemViewModel item)
        {
            if (item.FileInfo is null)
            {
                DebugHelper.LogDebug(nameof(GalleryLoader), nameof(LoadGalleryAsync), "Invalid file");
                return;
            }
            
            var thumb = await thumbnailLoader.GetThumbnailAsync(item.FileInfo, (uint)maxHeight).ConfigureAwait(false);
            if (thumb is not null)
            {
                thumbnailCache.Add(tab.Id, item.FileInfo.FullName, thumb);
            }
            item.Image.Value = thumb;
        }
    }

    public static async Task ReloadGallery(TabViewModel tab, IReadOnlyList<FileInfo> files, IThumbnailLoader thumbnailLoader, IThumbnailCache thumbnailCache, CancellationToken ct)
    {
        tab.Gallery.LoadingState = GalleryLoadingState.Restarting;
        tab.Gallery.GalleryItems.Clear();
        await _cts.CancelAsync().ConfigureAwait(false);
        _cts.Dispose();
        _cts = null;
        await LoadGalleryAsync(tab, files, thumbnailLoader, thumbnailCache, ct).ConfigureAwait(false);
    }
    
    public static async ValueTask LoadGalleryIfDockedOrExpanded(TabViewModel tabViewModel, GalleryMode mode, IThumbnailCache thumbnailCache, IThumbnailLoader thumbnailLoader)
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

        await tabViewModel.ImageIterator.SkipToIndexAsync(index, tabViewModel.GetTabCancellation()).ConfigureAwait(false);
    }

    public static void SortLoadedGallery(TabViewModel tab, IReadOnlyList<FileInfo> files)
    {
        if (tab.Gallery.GalleryItems is null || tab.Gallery.GalleryItems.Count <= 1 || files is null || files.Count is 0)
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