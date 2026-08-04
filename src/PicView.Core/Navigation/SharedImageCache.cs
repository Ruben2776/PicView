using System.Collections.Concurrent;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Preloading;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

/// <summary>
/// Acts as the central station for acquiring and managing cached <see cref="ImageModel"/> resources.
/// <para>
/// This class coordinates between the storage container (multiple <see cref="EvictingDictionary{TValue}"/>) 
/// and the background worker (<see cref="Preloader"/>) to ensure images are loaded, retrieved, 
/// and evicted efficiently across multiple tab owners.
/// </para>
/// </summary>
public class SharedImageCache : IImageCache
{
    /// <summary>
    /// A private dictionary that maps an owner identifier (string) to an instance
    /// of <see cref="EvictingDictionary{TValue}"/> containing preloaded image data.
    /// </summary>
    /// <remarks>
    /// This dictionary is used to store image cache data for individual owners,
    /// enabling separate and efficient management of cached data for different contexts.
    /// Each owner is associated with an instance of <see cref="EvictingDictionary{TValue}"/>
    /// which provides eviction capabilities to limit memory usage.
    /// </remarks>
    private readonly ConcurrentDictionary<uint, EvictingDictionary<PreLoadValue>> _ownerDictionaries = new();
    private readonly ConcurrentDictionary<uint, (string Directory, IReadOnlyList<FileInfo> Files, int CurrentIndex)> _ownerContexts = new();
    private readonly ConcurrentDictionary<string, PreLoadValue> _pathLookup;

    private readonly PriorityQueue<string, DateTime> _disposalQueue = new();
    private const int DisposalDelayInSeconds = 15;
    private readonly Lock _disposalLock = new();

    // The worker
    private readonly Preloader _preLoader;

    public SharedImageCache(Func<FileInfo, ValueTask<ImageModel>> imageLoader)
    {
        var pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
            
        _pathLookup = new ConcurrentDictionary<string, PreLoadValue>(pathComparer);
        _preLoader = new Preloader(imageLoader, this);
    }

    #region Resynchronize and owner registration
    
    public void RegisterOwner(uint ownerId)
    {
        _ownerDictionaries.TryAdd(ownerId, new EvictingDictionary<PreLoadValue>(PreLoaderConfig.MaxCount));
    }

    public void RemoveOwner(uint ownerId)
    {
        _ownerDictionaries.TryRemove(ownerId, out _);
        _ownerContexts.TryRemove(ownerId, out _);
    }

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
        dict.Clear();

        var newFileMap = new Dictionary<string, int>(_pathLookup.Comparer);
        for (var i = 0; i < files.Count; i++)
        {
            newFileMap[files[i].FullName] = i;
        }

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
    
    public void Add(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse) =>
        TryAdd(ownerId, index, preLoadValue, listCount, isReverse, out _);

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
    
    public bool Contains(string fileName) => _pathLookup.TryGetValue(fileName, out _);
    public bool Contains(FileInfo fileInfo) => _pathLookup.TryGetValue(fileInfo.FullName, out _);
    public bool Contains(PreLoadValue value) => _pathLookup.TryGetValue(value.ImageModel.FileInfo.FullName, out _);
    
    public bool TryGet(FileInfo f, out PreLoadValue? value) => _pathLookup.TryGetValue(f.FullName, out value);
    public bool TryGet(ReadOnlySpan<char> f, out PreLoadValue? value) => _pathLookup.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(f, out value);

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
                if (kvp.Key == id || kvp.Value.Directory != directory)
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

    public async Task<ImageModel?> LoadAsync(uint ownerId, int index, IReadOnlyList<FileInfo> list, CancellationToken ct = default) =>
        await _preLoader.AddAsync(ownerId, index, list, false, ct).ConfigureAwait(false);

    public void Preload(uint ownerId, int currentIndex, bool reversed, IReadOnlyList<FileInfo> files, CancellationToken token) =>
        _preLoader.Preload(ownerId, currentIndex, reversed, files, token);

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
            await LoadAsync(ownerId, index, list, ct);
            return true;
        }

        await value.WaitForLoadingCompleteAsync();
        return true;
    }

    #endregion

    #region Disposal logic

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

    public void ForceDisposalQueue()
    {
        lock (_disposalLock)
        {
            _disposalQueue.Clear();
            
            foreach (var kvp in _pathLookup)
            {
                if (kvp.Value.ReferenceCount == 0 && _pathLookup.TryRemove(kvp.Key, out var removed))
                {
                    removed.ImageModel.Dispose();
                }
            }
        }
        GC.Collect();
    }

    #endregion
}