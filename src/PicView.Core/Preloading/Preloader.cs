using System.Diagnostics;
using PicView.Core.DebugTools;
using PicView.Core.Models;
using ZLinq;
using static System.GC;

namespace PicView.Core.Preloading;

/// <summary>
/// Manages asynchronous preloading and caching of images for efficient retrieval.
/// It uses a fixed-size cache with a directional eviction strategy: when the cache is full,
/// it removes the item with the lowest index (when navigating forward) or the highest index (when navigating backward).
/// </summary>
public class PreLoader(Func<FileInfo, ValueTask<ImageModel>> imageModelLoader) : IAsyncDisposable
{
#if DEBUG
    // ReSharper disable once ConvertToConstant.Local
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private static bool _showAddRemove = false;
#endif

    private readonly Lock _disposeLock = new();

    private readonly EvictingDictionary<PreLoadValue> _preLoadList = new(PreLoaderConfig.MaxCount);

    private CancellationTokenSource? _cancellationTokenSource;

    private int _isRunningFlag; // 0 = idle, 1 = running

    #region Add

    /// <summary>
    /// Asynchronously adds and loads an image to the preload cache. If the cache is full,
    /// an existing item is evicted based on the navigation direction.
    /// </summary>
    /// <param name="index">The index of the image in the list.</param>
    /// <param name="list">The complete list of image file paths.</param>
    /// <param name="isReverse">The direction of navigation, which controls eviction strategy.
    /// <c>false</c> (forward) evicts the lowest-indexed item; <c>true</c> (backward) evicts the highest-indexed item.</param>
    /// <param name="ct">A cancellation token to observe.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that returns <c>true</c> if the image was added and loaded successfully; otherwise, <c>false</c>.
    /// </returns>
    public async ValueTask<bool> AddAsync(int index, List<FileInfo> list, bool isReverse = false, CancellationToken ct = default)
    {
        if (list == null || index < 0 || index >= list.Count)
        {
            DebugHelper.LogDebug(nameof(PreLoader), nameof(AddAsync), "invalid parameters");
            return false;
        }

        // Don't re-add at same index
        if (_preLoadList.ContainsKey(index))
        {
            return true;
        }

        // Pre-insert a placeholder marked as loading to avoid duplicate concurrent loads for same index
        var fileInfo = list[index];
        var preLoadValue = new PreLoadValue(new ImageModel { FileInfo = fileInfo }, isLoading: true);
        ct.ThrowIfCancellationRequested();
        var evicted = _preLoadList.TryAdd(index, preLoadValue, list.Count, isReverse, out var evictedValue);

        try
        {
            if (evicted)
            {
                ImageDisposalHelper(evictedValue);
            }

            var imageModel = await imageModelLoader(fileInfo).ConfigureAwait(false);
            
            ct.ThrowIfCancellationRequested();

            preLoadValue.ImageModel = imageModel;
#if DEBUG
            if (_showAddRemove)
            {
                Trace.WriteLine($"{imageModel?.FileInfo?.Name} added at {index}");
            }
#endif
            return true;
        }
        catch (Exception ex)
        {
            _preLoadList.Remove(index); // Remove failed entry
            DebugHelper.LogDebug(nameof(PreLoader), nameof(AddAsync), ex);
            return false;
        }
        finally
        {
            preLoadValue?.IsLoading = false; // This will trigger the TaskCompletionSource
        }
    }

    /// <summary>
    /// Adds a pre-loaded image model to the cache at the specified index. This method does not perform any loading.
    /// </summary>
    /// <param name="index">The index at which to add the image model.</param>
    /// <param name="list">The complete list of image file paths.</param>
    /// <param name="imageModel">The already-loaded image model to add to the cache.</param>
    /// <param name="isReverse">The direction of navigation, which controls eviction strategy if the cache is full.</param>
    /// <returns>
    /// <c>true</c> if the image model was successfully added; otherwise, <c>false</c>.
    /// </returns>
    public bool Add(int index, List<FileInfo> list, ImageModel imageModel, bool isReverse)
    {
        if (list == null || index < 0 || index >= list.Count)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(Add)} invalid parameters: \n{index}");
#endif
            return false;
        }

        if (imageModel is null)
        {
            DebugHelper.LogDebug(nameof(PreLoader), nameof(AddAsync), "ImageModel is null");
            return false;
        }

        var preLoadValue = new PreLoadValue(imageModel);
        if (!_preLoadList.TryAdd(index, preLoadValue,  list.Count, isReverse, out var evictedValue))
        {
            return false;
        }
        if (evictedValue?.ImageModel?.Image is IDisposable disposable)
        {
            disposable.Dispose();
        }
        return true;
    }

    #endregion

    #region Refresh and resynchronize

    /// <summary>
    /// Updates the <see cref="FileInfo"/> for a cached item at a specific index.
    /// This is useful if file metadata has changed.
    /// </summary>
    /// <param name="index">The index of the item to update.</param>
    /// <param name="fileInfo">The new file information to assign.</param>
    /// <param name="list">The complete list of image file paths.</param>
    public void RefreshFileInfo(int index, FileInfo fileInfo, List<FileInfo> list)
    {
        if (!Contains(index, list))
        {
            DebugHelper.LogDebug(nameof(PreLoader), nameof(RefreshFileInfo), "index not found: " + index);
            return;
        }

        _preLoadList[index].ImageModel.FileInfo = fileInfo;
    }

    /// <summary>
    /// Resynchronizes the cache with an updated list of files. This method adjusts, moves,
    /// or removes cached entries to match the new file order and content.
    /// </summary>
    /// <remarks>
    /// This should be called after file operations (like sorting, adding, or deleting)
    /// have modified the master list of files.
    /// </remarks>
    /// <param name="list">The new, authoritative list of image files.</param>
    public void Resynchronize(List<FileInfo> list)
    {
        if (list == null)
        {
            DebugHelper.LogDebug(nameof(PreLoader), nameof(Resynchronize), "list is null");
            return;
        }

        if (list.Count == 0 || _preLoadList.Count == 0)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();

        // Create a reverse lookup from file path to current index
        var reverseLookup = new Dictionary<string, int>(list.Count);
        
        for (var i = 0; i < list.Count; i++)
        {
            reverseLookup[list[i].FullName] = i;
        }

        // Snapshot of current keys to avoid modification during iteration
        var keys = _preLoadList.Keys.ToArray();

        foreach (var oldIndex in keys)
        {
            if (!_preLoadList.TryGetValue(oldIndex, out var preLoadValue))
            {
                continue;
            }

            var file = preLoadValue.ImageModel?.FileInfo;
            if (file is null)
            {
                Remove(oldIndex, list);
                continue;
            }

            if (!reverseLookup.TryGetValue(file.FullName, out var newIndex))
            {
                // File no longer exists in the list
                Remove(oldIndex, list);
                continue;
            }

            if (newIndex == oldIndex)
            {
                // Index is unchanged, no action needed
                continue;
            }

            if (newIndex < 0 || newIndex >= list.Count)
            {
                // Invalid new index, remove the entry
                Remove(oldIndex, list);
                continue;
            }

            // Attempt to move the entry to the new index
            if (!_preLoadList.Remove(oldIndex, out var removedValue))
            {
                continue;
            }

            if (_preLoadList.TryAdd(newIndex, removedValue, list.Count, false, out var evictedValue))
            {
                // An item was evicted during the move, so dispose of it
                ImageDisposalHelper(evictedValue);
#if DEBUG
                if (_showAddRemove)
                {
                    Trace.WriteLine($"Failed to resynchronize {file} to index {newIndex}");
                }
#endif
            }
#if DEBUG
            else if (_showAddRemove)
            {
                Trace.WriteLine($"Resynchronized {file} from index {oldIndex} to {newIndex}");
            }
#endif
        }
    }

    #endregion

    #region Get and Contains

    /// <summary>
    /// Checks if an item exists in the cache at the specified index.
    /// </summary>
    /// <param name="key">The index to check.</param>
    /// <param name="list">The complete list of image file paths to validate the index against.</param>
    /// <returns>
    /// <c>true</c> if the index is valid and the item is in the cache; otherwise, <c>false</c>.
    /// </returns>
    public bool Contains(int key, List<FileInfo> list) =>
        list != null && key >= 0 && key < list.Count && _preLoadList.ContainsKey(key);


    /// <summary>
    /// Retrieves a cached item by its index without triggering a load if it's missing.
    /// </summary>
    /// <param name="key">The index of the item to retrieve.</param>
    /// <param name="list">The complete list of image file paths.</param>
    /// <returns>The <see cref="PreLoadValue"/> if found in the cache; otherwise, <c>null</c>.</returns>
    public PreLoadValue? Get(int key, List<FileInfo> list)
    {
        if (list is null || key < 0 || key > list.Count)
        {
            DebugHelper.LogDebug(nameof(PreLoader), nameof(Get), "invalid parameters:" + key);
        }
        return _preLoadList.TryGetValue(key, out var value) ? value : null;
    }


    /// <summary>
    /// Gets the preloaded value for a specific file. Should only be used when resynchronizing.
    /// </summary>
    /// <param name="file">The <see cref="FileInfo"/> of the image file to retrieve the preloaded value for.</param>
    /// <param name="list">The list of image file paths.</param>
    /// <returns>
    /// The <see cref="PreLoadValue"/> if it exists; otherwise, <c>null</c>.
    /// </returns>
    public PreLoadValue? Get(FileInfo file, List<FileInfo> list)
    {
        if (list == null || file is null)
        {
            return null;
        }

        var index = list.FindIndex(x => x.FullName == file.FullName);
        if (index > -1 && index < list.Count)
        {
            return _preLoadList.TryGetValue(index, out var value) ? value : null;
        }
        return null;
    }


    /// <summary>
    /// Retrieves a cached item by index, or asynchronously loads it if it is not in the cache.
    /// Assumes forward navigation for eviction if a new item needs to be loaded.
    /// </summary>
    /// <param name="key">The index of the item to retrieve or load.</param>
    /// <param name="list">The complete list of image file paths.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that returns the <see cref="PreLoadValue"/> if found or loaded; otherwise, <c>null</c>.
    /// </returns>
    public async ValueTask<PreLoadValue?> GetOrLoadAsync(int key, List<FileInfo> list)
    {
        if (list == null || key < 0 || key >= list.Count)
        {
            DebugHelper.LogDebug(nameof(PreLoader), nameof(GetOrLoadAsync), "invalid parameters: " + key);
            return null;
        }

        if (Contains(key, list))
        {
            return _preLoadList[key];
        }

        var isAdded = await AddAsync(key, list);
        if (!isAdded)
        {
            return null;
        }

        return key >= list.Count ? null : _preLoadList[key];
    }

    /// <summary>
    /// Retrieves a cached item by <see cref="FileInfo"/>, or asynchronously loads it if it is not in the cache.
    /// </summary>
    /// <param name="fileInfo">The file information of the item to retrieve or load.</param>
    /// <param name="list">The complete list of image file paths.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that returns the <see cref="PreLoadValue"/> if found or loaded; otherwise, <c>null</c>.
    /// </returns>
    public async ValueTask<PreLoadValue?> GetOrLoadAsync(FileInfo fileInfo, List<FileInfo> list)
    {
        if (list == null || fileInfo == null) return null;

        // Find the entry without creating a new list
        var entry = _preLoadList.
            AsValueEnumerable()
            .FirstOrDefault(kvp => kvp.Value.ImageModel?.FileInfo?.FullName == fileInfo.FullName);

        if (entry.Value != null)
        {
            return await GetOrLoadAsync(entry.Key, list);
        }

        // If not found in cache, find its index in the master list and load
        var index = list.FindIndex(f => f.FullName == fileInfo.FullName);
        if (index != -1)
        {
            return await GetOrLoadAsync(index, list);
        }

        return null;
    }
    
    /// <summary>
    /// Retrieves a cached item by index, or asynchronously loads it if it is not in the cache,
    /// specifying the navigation direction for potential evictions.
    /// </summary>
    /// <param name="key">The index of the item to retrieve or load.</param>
    /// <param name="list">The complete list of image file paths.</param>
    /// <param name="isReverse">The direction of navigation to determine eviction strategy.</param>
    /// <param name="ct">A cancellation token to observe.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that returns the <see cref="PreLoadValue"/> if found or loaded; otherwise, <c>null</c>.
    /// </returns>
    public async ValueTask<PreLoadValue?> GetOrLoadAsync(int key, List<FileInfo> list, bool isReverse, CancellationToken ct)
    {
        if (list == null || key < 0 || key >= list.Count)
        {
            DebugHelper.LogDebug(nameof(PreLoader), nameof(GetOrLoadAsync), "invalid parameters: " + key);
            return null;
        }

        if (_preLoadList.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var isAdded = await AddAsync(key, list, isReverse, ct).ConfigureAwait(false);
        return !isAdded ? null : _preLoadList.TryGetValue(key, out var value) ? value : null;
    }


    #endregion

    #region Remove and clear

    /// <summary>
    /// Removes an item from the cache by its index and disposes its associated resources.
    /// </summary>
    /// <param name="key">The index of the item to remove.</param>
    /// <param name="list">The complete list of image file paths.</param>
    /// <returns><c>true</c> if the item was successfully removed; otherwise, <c>false</c>.</returns>
    public bool Remove(int key, List<FileInfo> list)
    {
        if (list == null || key < 0 || key >= list.Count)
        {
            DebugHelper.LogDebug(nameof(PreLoader), nameof(Remove), "invalid parameters: " + key);
            return false;
        }

        if (!Contains(key, list))
        {
            DebugHelper.LogDebug(nameof(PreLoader), nameof(Remove), "key does not exist: " + key);
            return false;
        }

        try
        {
            if (_preLoadList.TryGetValue(key, out var item))
            {
                var removed = _preLoadList.Remove(key);
                ImageDisposalHelper(item);
                item.ImageModel.Image = null;
#if DEBUG
                if (!removed || !_showAddRemove)
                {
                    return removed;
                }

                var name = item.ImageModel?.FileInfo?.Name ?? "Unknown";
                Trace.WriteLine($"{name} removed at {key}");
#endif
                return removed;
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(PreLoader), nameof(Remove), e);
        }

        return false;
    }


    /// <summary>
    /// Removes an item from the cache by its full file name and disposes its associated resources.
    /// </summary>
    /// <param name="fileName">The full path of the file to remove from the cache.</param>
    /// <param name="list">The complete list of image file paths.</param>
    /// <returns><c>true</c> if the item was found and removed; otherwise, <c>false</c>.</returns>
    public bool Remove(string fileName, List<FileInfo> list)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        // Iterate the dictionary directly to find the matching entry
        return (from kvp in _preLoadList where kvp.Value.ImageModel?.FileInfo?.FullName == fileName 
            select Remove(kvp.Key, list))
            .AsValueEnumerable()
            .FirstOrDefault();
    }

    /// <summary>
    /// Synchronously clears the entire cache, disposing all cached images and canceling any ongoing preload operations.
    /// </summary>
    public void Clear()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        foreach (var item in _preLoadList.Values)
        {
            ImageDisposalHelper(item);
        }

        _preLoadList.Clear();

#if DEBUG
        Trace.WriteLine("Preloader cleared");
#endif
    }

    /// <summary>
    /// Asynchronously clears the entire cache, disposing all cached images and canceling any ongoing preload operations.
    /// </summary>
    public async Task ClearAsync()
    {
        try
        {
            if (_cancellationTokenSource is not null)
            {
                await _cancellationTokenSource?.CancelAsync();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(PreLoader), nameof(ClearAsync), e);
        }

        Clear();
    }

    private void ImageDisposalHelper(PreLoadValue item)
    {
        if (item is null)
        {
            return;
        }
        _disposeLock.Enter();
        try
        {
            if (item.ImageModel?.Image is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        finally
        {
            _disposeLock.Exit();
        }
    }

    #endregion

    #region Preload
    
    /// <summary>
    /// Initiates the asynchronous preloading process around a given index.
    /// It intelligently loads images ahead of and behind the current position based on the navigation direction.
    /// </summary>
    /// <param name="currentIndex">The index of the currently viewed image, which serves as the center point for preloading.</param>
    /// <param name="reverse">The direction of preloading. <c>false</c> prioritizes loading subsequent images; <c>true</c> prioritizes previous images.</param>
    /// <param name="list">The complete list of image file paths.</param>
    public async ValueTask PreLoadAsync(int currentIndex, bool reverse, List<FileInfo> list)
    {
        if (list == null)
        {
            DebugHelper.LogDebug(nameof(PreLoader), nameof(PreLoadAsync), $"list null \n{currentIndex}");
            return;
        }

        if (Interlocked.CompareExchange(ref _isRunningFlag, 1, 0) != 0)
        {
            return; // Already running
        }

#if DEBUG
        if (_showAddRemove)
        {
            var direction = reverse ? "backwards" : "forwards";
            Trace.WriteLine($"\nPreLoading started {direction} at {currentIndex}\n");
        }
#endif

        if (_cancellationTokenSource is not null)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }
        }
        else
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        try
        {
            await PreLoadInternalAsync(currentIndex, reverse, list, _cancellationTokenSource.Token)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(PreLoader), nameof(PreLoadAsync), exception);
        }
        finally
        {
            Interlocked.Exchange(ref _isRunningFlag, 0);
        }
    }

    
    /// <summary>
    /// Performs the internal logic for preloading images asynchronously.
    /// </summary>
    /// <param name="currentIndex">The current index for preloading.</param>
    /// <param name="reverse">Whether to preload in reverse order.</param>
    /// <param name="list">The list of image file paths.</param>
    /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for tasks to complete.</param>
    private async ValueTask PreLoadInternalAsync(int currentIndex, bool reverse, List<FileInfo> list,
        CancellationToken token)
    {
        var count = list.Count;
        var nextStartingIndex = (currentIndex + 1) % count;
        var prevStartingIndex = (currentIndex - 1 + count) % count;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = PreLoaderConfig.MaxParallelism,
            CancellationToken = token
        };

        if (reverse)
        {
            await LoopAsync(options, false);
            await LoopAsync(options, true);
        }
        else
        {
            await LoopAsync(options, true);
            await LoopAsync(options, false);
        }

        return;


        async Task LoopAsync(ParallelOptions parallelOptions, bool positive)
        {
            if (positive)
            {
                await Parallel.ForAsync(0, PreLoaderConfig.PositiveIterations, parallelOptions,
                    async (i, _) => { await AddAddition((nextStartingIndex + i) % count); });
            }
            else
            {
                await Parallel.ForAsync(0, PreLoaderConfig.NegativeIterations, parallelOptions,
                    async (i, _) => { await AddAddition((prevStartingIndex - i + count) % count); });
            }
        }

        async Task AddAddition(int index)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                await AddAsync(index, list, reverse, token);
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(PreLoader), nameof(PreLoadInternalAsync), e);
            }
        }
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    /// <summary>
    /// Disposes the preloader and its resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            Clear();
        }

        _disposed = true;

#if DEBUG
        if (_showAddRemove)
        {
            Trace.WriteLine("Preloader disposed");
        }
#endif
    }


    /// <summary>
    /// Asynchronously disposes the preloader, clearing the cache and canceling any ongoing operations.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _cancellationTokenSource?.CancelAsync();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        await ClearAsync().ConfigureAwait(false);
        Dispose(false);
        SuppressFinalize(this);
    }

    ~PreLoader()
    {
        Dispose(false);
    }

    #endregion
}