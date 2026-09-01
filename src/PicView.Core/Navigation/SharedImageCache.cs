using System.Collections.Concurrent;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Preloading;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

/// <inheritdoc />
public class SharedImageCache : IImageCache
{
    /// <summary>
    /// A private dictionary that maps an owner identifier (<see cref="uint"/>) to an instance
    /// of <see cref="EvictingDictionary{TValue}"/> containing preloaded image data.
    /// </summary>
    /// <remarks>
    /// This dictionary is used to store image cache data for individual owners,
    /// enabling separate and efficient management of cached data for different contexts.
    /// Each owner is associated with an instance of <see cref="EvictingDictionary{TValue}"/>
    /// which provides eviction capabilities to limit memory usage.
    /// </remarks>
    private readonly ConcurrentDictionary<uint, EvictingDictionary<PreLoadValue>> _ownerDictionaries = new();

    /// <summary>
    /// Tracks context information (Directory, Files list, and CurrentIndex) for each active owner tab.
    /// Used to resolve transfer eligibility when tabs are closed.
    /// </summary>
    private readonly ConcurrentDictionary<uint, (string Directory, IReadOnlyList<FileInfo> Files, int CurrentIndex)> _ownerContexts = new();
    
    /// <summary>
    /// Fast lookup by full file path (using OS-specific string comparer).
    /// Acts as the unified global reference map for both active and expiring preloaded items.
    /// </summary>
    private readonly ConcurrentDictionary<string, PreLoadValue> _pathLookup;

    /// <summary>
    /// Priority queue used for lazy disposal, ordering file paths by their scheduled expiration timestamp.
    /// </summary>
    private readonly PriorityQueue<string, DateTime> _disposalQueue = new();

    /// <summary>
    /// Time delay in seconds before an evicted 0-reference image is fully disposed.
    /// </summary>
    private const int DisposalDelayInSeconds = 15;

    /// <summary>
    /// Synchronization lock object protecting thread-safe operations on <see cref="_disposalQueue"/>.
    /// </summary>
    private readonly Lock _disposalLock = new();

    /// <summary>
    /// Background worker responsible for look-ahead calculation and image loading.
    /// </summary>
    private readonly Preloader _preLoader;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedImageCache"/> class.
    /// </summary>
    /// <param name="imageLoader">The function used to load an <see cref="ImageModel"/> asynchronously from a <see cref="FileInfo"/>.</param>
    public SharedImageCache(Func<FileInfo, ValueTask<ImageModel>> imageLoader)
    {
        var pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
            
        _pathLookup = new ConcurrentDictionary<string, PreLoadValue>(pathComparer);
        _preLoader = new Preloader(imageLoader, this);
    }

    #region Resynchronize and owner registration
    
    /// <summary>
    /// Registers a new owner ID (tab) into the cache and initializes its dedicated <see cref="EvictingDictionary{TValue}"/>.
    /// </summary>
    /// <param name="ownerId">The unique owner identifier.</param>
    public void RegisterOwner(uint ownerId)
    {
        _ownerDictionaries.TryAdd(ownerId, new EvictingDictionary<PreLoadValue>(PreLoaderConfig.MaxCount));
    }

    /// <summary>
    /// Removes an owner ID (tab) and its associated context from cache tracking.
    /// </summary>
    /// <param name="ownerId">The unique owner identifier to unregister.</param>
    public void RemoveOwner(uint ownerId)
    {
        _ownerDictionaries.TryRemove(ownerId, out _);
        _ownerContexts.TryRemove(ownerId, out _);
    }

    /// <summary>
    /// Resynchronizes cached items for a specific owner when its file list or sorting order changes.
    /// Maps existing cached items to their new list indices and evicts items no longer present.
    /// </summary>
    /// <param name="ownerId">The unique owner identifier.</param>
    /// <param name="files">The new file list to resynchronize against.</param>
    public void Resynchronize(uint ownerId, IReadOnlyList<FileInfo> files)
    {
        if (!_ownerDictionaries.TryGetValue(ownerId, out var dict))
        {
            return;
        }
        
        var oldItems = dict.GetEnumerator();
        using IDisposable oldItems1 = oldItems;
        var currentItems = new List<KeyValuePair<int, PreLoadValue>>();
        while (oldItems.MoveNext())
        {
            currentItems.Add(oldItems.Current);
        }
        var newFileMap = new Dictionary<string, int>(_pathLookup.Comparer);
        for (var i = 0; i < files.Count; i++)
        {
            newFileMap[files[i].FullName] = i;
        }

        // Fast-path: if no existing cached item's index shifted and none were removed, avoid clearing the cache
        var anyIndexChanged = false;
        foreach (var item in currentItems)
        {
            if (!newFileMap.TryGetValue(item.Value.ImageModel.FileInfo.FullName, out var newIndex) || newIndex != item.Key)
            {
                anyIndexChanged = true;
                break;
            }
        }

        if (!anyIndexChanged)
        {
            if (files.Count > 0 && _ownerContexts.TryGetValue(ownerId, out var existingCtx))
            {
                _ownerContexts[ownerId] = (files[0].DirectoryName ?? string.Empty, files, existingCtx.CurrentIndex);
            }
            return;
        }

        dict.Clear();

        foreach (var item in currentItems)
        {
            if (newFileMap.TryGetValue(item.Value.ImageModel.FileInfo.FullName, out var newIndex))
            {
                // We do not care if it's a new reference here, because we are moving it 
                // from the cleared dictionary back into the same dictionary. 
                // The single reference count stays valid.
                dict.TryAdd(newIndex, item.Value, files.Count, false, out var evicted, out _);
                if (evicted != null)
                {
                    ProcessDisposalLogic(evicted);
                }
            }
            else
            {
                ProcessDisposalLogic(item.Value);
            }
        }
        
        if (files.Count > 0 && _ownerContexts.TryGetValue(ownerId, out var ctx))
        {
            _ownerContexts[ownerId] = (files[0].DirectoryName ?? string.Empty, files, ctx.CurrentIndex);
        }
    }
    
    #endregion

    #region Add, Get, Remove, and Clear
    
    /// <inheritdoc />
    public void Add(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse) =>
        TryAdd(ownerId, index, preLoadValue, listCount, isReverse, out _);

    /// <inheritdoc />
    public bool TryAdd(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse, out PreLoadValue? value)
    {
        value = null;
        if (!_ownerDictionaries.TryGetValue(ownerId, out var dict)) return false;

        dict.TryAdd(index, preLoadValue, listCount, isReverse, out var evictedValue, out bool isNewReference);

        if (isNewReference)
        {
            preLoadValue.AddReference();
            // Using the indexer forces the lookup to point to the newest wrapper instance 
            // if a duplicate was accidentally created.
            _pathLookup[preLoadValue.ImageModel.FileInfo.FullName] = preLoadValue; 
        }

        if (evictedValue is not null)
        {
            ProcessDisposalLogic(evictedValue);
            value = evictedValue;
        }

        return true;
    }
    
    /// <inheritdoc />
    public bool Contains(string fileName) =>
        _pathLookup.ContainsKey(fileName);

    /// <inheritdoc />
    public bool Contains(FileInfo fileInfo) =>
        _pathLookup.ContainsKey(fileInfo.FullName);

    /// <inheritdoc />
    public bool Contains(PreLoadValue value) =>
        _pathLookup.ContainsKey(value.ImageModel.FileInfo.FullName);
    
    /// <inheritdoc />
    public bool TryGet(FileInfo f, out PreLoadValue? value) =>
        _pathLookup.TryGetValue(f.FullName, out value);

    /// <inheritdoc />
    public bool TryGet(ReadOnlySpan<char> f, out PreLoadValue? value) =>
        _pathLookup.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(f, out value);

    /// <summary>
    /// Removes an item at a specific index from an owner's dictionary and releases its reference.
    /// </summary>
    /// <param name="ownerId">The unique owner identifier.</param>
    /// <param name="index">The position index to remove.</param>
    public void TryRemove(uint ownerId, int index)
    {
        if (!_ownerDictionaries.TryGetValue(ownerId, out var dict))
        {
            return;
        }
        if (!dict.Remove(index, out var removedValue) || removedValue is null)
        {
            return;
        }

        ProcessDisposalLogic(removedValue);
    }

    /// <summary>
    /// Clears all owner dictionaries and releases reference counts for all cached items.
    /// </summary>
    public void Clear()
    {
        var allValues = new List<PreLoadValue>();
        foreach (var dict in _ownerDictionaries.Values)
        {
            allValues.AddRange(dict.Values);
            dict.Clear();
        }
    
        foreach(var value in allValues)
        {
            ProcessDisposalLogic(value);
        }
    }

    /// <summary>
    /// Clears all cached items for a specific owner ID and releases their reference counts.
    /// </summary>
    /// <param name="ownerId">The unique owner identifier to clear.</param>
    public void Clear(uint ownerId)
    {
        if (!_ownerDictionaries.TryGetValue(ownerId, out var dict))
        {
            return;
        }

        var values = dict.Values.ToList();
        dict.Clear();
        
        foreach (var value in values)
        {
            ProcessDisposalLogic(value);
        }
    }

    public void DeleteFromCache(string fileName)
    {
        if (!_pathLookup.TryGetValue(fileName, out var cachedValue))
        {
            return;
        }

        // Remove from all active owner dictionaries
        foreach (var dict in _ownerDictionaries.Values)
        {
            int? indexToRemove = null;
            foreach (var kvp in dict)
            {
                if (ReferenceEquals(kvp.Value, cachedValue))
                {
                    indexToRemove = kvp.Key;
                    break;
                }
            }

            if (indexToRemove.HasValue && dict.Remove(indexToRemove.Value, out var removedValue) && removedValue != null)
            {
                removedValue.ReleaseReference();
            }
        }

        // If it was removed from any dictionary, its reference count should have dropped.
        // But regardless, we want to completely obliterate it from the cache instantly.
        if (_pathLookup.TryRemove(cachedValue.ImageModel.FileInfo.FullName, out var lookupValue))
        {
            // Reset reference to 0 just in case and dispose
            while (lookupValue.ReferenceCount > 0)
            {
                lookupValue.ReleaseReference();
            }
            lookupValue.ImageModel.Dispose();
        }
    }

    /// <summary>
    /// Clears cache items for a closing tab, attempting to transfer eligible nearby cached images 
    /// to another tab browsing the same directory before unregistering the owner.
    /// </summary>
    /// <param name="tab">The view model of the closing tab.</param>
    /// <param name="directory">The active directory path of the closing tab.</param>
    public void Clear(TabViewModel tab, string directory)
    {
        var id = tab.Id;
        
        if (_ownerDictionaries.TryGetValue(id, out var dict))
        {
            var closingItems = dict.Values.ToList();
            dict.Clear();
            
            uint targetOwnerId = 0;
            IReadOnlyList<FileInfo>? targetFiles = null;
            var targetCurrentIndex = 0;
            
            foreach (var kvp in _ownerContexts)
            {
                if (kvp.Key == id || !string.Equals(kvp.Value.Directory, directory, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                targetOwnerId = kvp.Key;
                targetFiles = kvp.Value.Files;
                targetCurrentIndex = kvp.Value.CurrentIndex;
                break;
            }
            
            if (targetFiles is not null && _ownerDictionaries.TryGetValue(targetOwnerId, out var targetDict))
            {
                var fileIndexMap = new Dictionary<string, int>(_pathLookup.Comparer);
                for (var i = 0; i < targetFiles.Count; i++)
                {
                    fileIndexMap[targetFiles[i].FullName] = i;
                }
                
                var count = targetFiles.Count;
                
                foreach (var item in closingItems)
                {
                    if (fileIndexMap.TryGetValue(item.ImageModel.FileInfo.FullName, out var targetIndex))
                    {
                        var distForward = (targetIndex - targetCurrentIndex + count) % count;
                        var distBackward = (targetCurrentIndex - targetIndex + count) % count;
                        
                        if (distForward <= PreLoaderConfig.PositiveIterations || distBackward <= PreLoaderConfig.NegativeIterations)
                        {
                            targetDict.TryAdd(targetIndex, item, count, false, out var evictedTargetItem, out bool isNewReference);
                            
                            if (isNewReference)
                            {
                                item.AddReference();
                                _pathLookup[item.ImageModel.FileInfo.FullName] = item;
                            }
                            
                            // We just removed it from the closing tab's dictionary, so we MUST 
                            // decrement its reference to represent the loss of the closing tab!
                            ProcessDisposalLogic(item);

                            if (evictedTargetItem != null)
                            {
                                ProcessDisposalLogic(evictedTargetItem);
                            }
                            continue;
                        }
                    }
                    ProcessDisposalLogic(item);
                }
            }
            else
            {
                foreach (var item in closingItems)
                {
                    ProcessDisposalLogic(item);
                }
            }
        }
        
        RemoveOwner(id);
    }

    #endregion

    #region Loading, preloading and wait for loading

    /// <summary>
    /// Loads an image asynchronously for an owner or returns it if cached via the background worker.
    /// </summary>
    /// <param name="ownerId">The unique owner identifier.</param>
    /// <param name="index">The index in the file list.</param>
    /// <param name="list">The list of files.</param>
    /// <param name="ct">Token to monitor for cancellation requests.</param>
    /// <returns>A task returning the loaded <see cref="ImageModel"/>, or <see langword="null"/> if failed.</returns>
    public async Task<ImageModel?> LoadAsync(uint ownerId, int index, IReadOnlyList<FileInfo> list, CancellationToken ct = default) =>
        await _preLoader.AddAsync(ownerId, index, list, false, ct).ConfigureAwait(false);

    /// <summary>
    /// Triggers predictive pre-fetching around the specified current index in the background.
    /// </summary>
    /// <param name="ownerId">The unique owner identifier.</param>
    /// <param name="currentIndex">The active index being viewed.</param>
    /// <param name="reversed">Indicates if iteration is moving backward.</param>
    /// <param name="files">The list of files to pre-fetch around.</param>
    /// <param name="token">Token to monitor for cancellation requests.</param>
    public void Preload(uint ownerId, int currentIndex, bool reversed, IReadOnlyList<FileInfo> files, CancellationToken token) =>
        _preLoader.Preload(ownerId, currentIndex, reversed, files, token);

    /// <summary>
    /// Waits for an image at a specific index to complete its background loading process.
    /// </summary>
    /// <param name="ownerId">The unique owner identifier.</param>
    /// <param name="index">The position index in the list.</param>
    /// <param name="list">The list of files.</param>
    /// <param name="ct">Token to monitor for cancellation requests.</param>
    /// <returns>A task completing with <see langword="true"/> if successfully loaded/awaited; otherwise, <see langword="false"/>.</returns>
    public async ValueTask<bool> WaitForLoadingCompleteAsync(uint ownerId, int index, IReadOnlyList<FileInfo> list, CancellationToken ct = default)
    {
        if (!TryGet(list[index], out var value))
        {
            return false;
        }

        if (value is null)
        {
            // The item is securely added to the cache internally during LoadAsync -> Preloader.AddAsync.
            // Do NOT create a duplicate wrapper wrapper here.
            await LoadAsync(ownerId, index, list, ct).ConfigureAwait(false);
            return true;
        }

        await value.WaitForLoadingCompleteAsync().ConfigureAwait(false);
        return true;
    }

    #endregion

    #region Disposal logic

    /// <summary>
    /// Decrements the reference count of a <see cref="PreLoadValue"/>. If references hit 0,
    /// enqueues the item for delayed lazy disposal and sweeps expired items.
    /// </summary>
    /// <param name="item">The preloaded value to process.</param>
    internal void ProcessDisposalLogic(PreLoadValue item)
    {
        if (item.ReleaseReference() > 0)
        {
            return;
        }

        var path = item.ImageModel.FileInfo.FullName;
        lock (_disposalLock)
        {
            _disposalQueue.Enqueue(path, DateTime.UtcNow.AddSeconds(DisposalDelayInSeconds));
        }

        SweepExpiredDisposals();
    }

    /// <summary>
    /// Inspects the priority queue and disposes of all images whose 0-reference hold 
    /// duration has exceeded <see cref="DisposalDelayInSeconds"/>.
    /// </summary>
    private void SweepExpiredDisposals()
    {
        var now = DateTime.UtcNow;
        lock (_disposalLock)
        {
            while (_disposalQueue.TryPeek(out var path, out var expiration) && expiration <= now)
            {
                _disposalQueue.Dequeue();

                if (!_pathLookup.TryGetValue(path, out var value) || value.ReferenceCount is not 0)
                {
                    continue;
                }
                if (_pathLookup.TryRemove(path, out _))
                {
                    value.ImageModel.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Immediately clears the disposal queue, removes all 0-reference orphan images 
    /// from the lookup table, disposes of them, and triggers a full Garbage Collection sweep.
    /// </summary>
    public void ForceDisposalQueue()
    {
        lock (_disposalLock)
        {
            _disposalQueue.Clear();
            
            foreach (var kvp in _pathLookup)
            {
                if (kvp.Value.ReferenceCount is 0 && _pathLookup.TryRemove(kvp.Key, out var removed))
                {
                    removed.ImageModel.Dispose();
                }
            }
        }
        GC.Collect();
    }

    #endregion
}