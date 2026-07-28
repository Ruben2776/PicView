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
/// and the background worker (<see cref="Preloader2"/>) to ensure images are loaded, retrieved, 
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
    
    /// Fast lookup by file path (using OS-specific string comparer)
    private readonly ConcurrentDictionary<string, PreLoadValue> _pathLookup;
    /// Lazy disposal list: FilePath -> (PreLoadValue, ExpirationTime)
    private readonly ConcurrentDictionary<string, (PreLoadValue Item, DateTime Expiration)> _disposalList;
    /// Keep track of which directories and file lists each owner has for transfer logic
    private readonly ConcurrentDictionary<uint, (string Directory, IReadOnlyList<FileInfo> Files, int CurrentIndex)> _ownerContexts = new();
    
    // The worker
    private readonly Preloader _preLoader;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedImageCache"/> class.
    /// </summary>
    /// <param name="imageLoader">The function used to load an ImageModel from a FileInfo.</param>
    public SharedImageCache(Func<FileInfo, ValueTask<ImageModel>> imageLoader)
    {
        var pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
            
        _pathLookup = new ConcurrentDictionary<string, PreLoadValue>(pathComparer);
        _disposalList = new ConcurrentDictionary<string, (PreLoadValue, DateTime)>(pathComparer);

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
                // Put it back at new index
                dict.TryAdd(newIndex, item.Value, files.Count, false, out var evicted);
                if (evicted != null)
                {
                    CheckAndDisposeIfNotReferenced(evicted);
                }
            }
            else
            {
                // File no longer exists in the list
                CheckAndDisposeIfNotReferenced(item.Value);
            }
        }
        
        // Update context
        if (files.Count <= 0)
        {
            return;
        }

        if (_ownerContexts.TryGetValue(ownerId, out var ctx))
        {
            _ownerContexts[ownerId] = (files[0].DirectoryName ?? string.Empty, files, ctx.CurrentIndex);
        }
    }
    
    #endregion

    #region Add, Get, Remove, and Clear
    
    public void Add(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse)
    {
        TryAdd(ownerId, index, preLoadValue, listCount, isReverse, out _);
    }

    public bool TryAdd(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse, out PreLoadValue? value)
    {
        value = null;
        if (!_ownerDictionaries.TryGetValue(ownerId, out var dict))
        {
            return false;
        }

        var path = preLoadValue.ImageModel.FileInfo.FullName;
        
        // Ensure path lookup has it
        _pathLookup.TryAdd(path, preLoadValue);
        
        // Add to owner dictionary
        var evicted = dict.TryAdd(index, preLoadValue, listCount, isReverse, out var evictedValue);

        if (!evicted || evictedValue == null)
        {
            return evicted;
        }

        CheckAndDisposeIfNotReferenced(evictedValue);
        value = evictedValue;

        return evicted;
    }
    
    public bool Contains(ReadOnlySpan<char> span, out PreLoadValue? value) =>
        TryGet(span, out value);

    public bool Contains(PreLoadValue value) =>
        TryGet(value.ImageModel.FileInfo.FullName, out var preLoadValue) && preLoadValue == value;
    
    public bool TryGet(FileInfo f, out PreLoadValue? value)
    {
        if (_pathLookup.TryGetValue(f.FullName, out value))
        {
            return true;
        }
        return TryResurrect(f.FullName, out value);
    }

    public bool TryGet(uint ownerId, int index, out PreLoadValue? value)
    {
        if (_ownerDictionaries.TryGetValue(ownerId, out var dict))
        {
            return dict.TryGetValue(index, out value);
        }
        value = null;
        return false;
    }

    public bool TryGet(ReadOnlySpan<char> f, out PreLoadValue? value)
    {
        var lookup = _pathLookup.GetAlternateLookup<ReadOnlySpan<char>>();
        if (lookup.TryGetValue(f, out value))
        {
            return true;
        }
        return TryResurrect(f.ToString(), out value);
    }

    public void TryRemove(uint ownerId, int index)
    {
        if (!_ownerDictionaries.TryGetValue(ownerId, out var dict))
        {
            return;
        }

        if (!dict.Remove(index, out var removedValue))
        {
            return;
        }

        if (removedValue == null)
        {
            return;
        }
        if (removedValue.ImageModel.Image is IDisposable disposable)
        {
            disposable.Dispose();
        }
        removedValue.ImageModel.Image = null;
    }

    public void Clear()
    {
        foreach (var dict in _ownerDictionaries.Values)
        {
            dict.Clear();
        }
        _pathLookup.Clear();
        
        foreach (var kvp in _disposalList)
        {
            kvp.Value.Item.ImageModel.Dispose();
        }
        ForceDisposalQueue();
    }

    public void Clear(uint ownerId)
    {
        if (!_ownerDictionaries.TryGetValue(ownerId, out var dict))
        {
            return;
        }

        var values = dict.Values.ToList();
        dict.Clear();
        _pathLookup.Clear();
            
        foreach (var value in values)
        {
            value.ImageModel.Dispose();
        }
        ForceDisposalQueue();
    }

    public void Clear(TabViewModel tab, string directory)
    {
        var id = tab.Id;
        
        if (_ownerDictionaries.TryGetValue(id, out var dict))
        {
            var closingItems = dict.Values.ToList();
            dict.Clear();
            
            // Find another eligible tab
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
            
            if (targetFiles != null && _ownerDictionaries.TryGetValue(targetOwnerId, out var targetDict))
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
                        // Calculate distance
                        var distForward = (targetIndex - targetCurrentIndex + count) % count;
                        var distBackward = (targetCurrentIndex - targetIndex + count) % count;
                        
                        if (distForward <= PreLoaderConfig.PositiveIterations || distBackward <= PreLoaderConfig.NegativeIterations)
                        {
                            // Transfer
                            if (targetDict.TryAdd(targetIndex, item, count, false, out var evictedTargetItem))
                            {
                                if (evictedTargetItem != null)
                                {
                                    CheckAndDisposeIfNotReferenced(evictedTargetItem);
                                }
                            }
                            continue;
                        }
                    }
                    
                    // Not transferred
                    CheckAndDisposeIfNotReferenced(item);
                }
            }
            else
            {
                // No eligible tab found
                foreach (var item in closingItems)
                {
                    CheckAndDisposeIfNotReferenced(item);
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
            var model = await LoadAsync(ownerId, index, list, ct);
            Add(ownerId, index, new PreLoadValue(model), list.Count, false);
            return true;
        }

        await value.WaitForLoadingCompleteAsync();
        return true;
    }

    #endregion

    #region Disposal and ressurection
    
    private bool TryResurrect(string path, out PreLoadValue? value)
    {
        // Check if it's in the disposal list
        if (_disposalList.TryRemove(path, out var pendingDisposal))
        {
            value = pendingDisposal.Item;
            // Add it back to path lookup
            _pathLookup.TryAdd(path, value);
            return true;
        }
        
        value = null;
        return false;
    }

    internal void CheckAndDisposeIfNotReferenced(PreLoadValue item)
    {
        var isReferenced = false;
        foreach (var dict in _ownerDictionaries.Values)
        {
            foreach (var value in dict.Values)
            {
                if (value != item)
                {
                    continue;
                }

                isReferenced = true;
                break;
            }
            if (isReferenced) break;
        }

        if (isReferenced)
        {
            return;
        }

        var path = item.ImageModel.FileInfo.FullName;
        _pathLookup.TryRemove(path, out _);
        
        // Add to disposal queue instead of immediately disposing with a 30-second delay
        _disposalList.AddOrUpdate(path, 
            (item, DateTime.UtcNow.AddSeconds(30)), 
            (_, existing) => existing.Item == item ? (existing.Item, DateTime.UtcNow.AddSeconds(30)) : existing);
            
        // Process any expired items lazily
        ProcessDisposalQueue();
    }

    private void ProcessDisposalQueue()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _disposalList)
        {
            if (now >= kvp.Value.Expiration)
            {
                if (_disposalList.TryRemove(kvp.Key, out var removed))
                {
                    removed.Item.ImageModel.Dispose();
                }
            }
        }
    }

    public void ForceDisposalQueue()
    {
        foreach (var kvp in _disposalList)
        {
            if (_disposalList.TryRemove(kvp.Key, out var removed))
            {
                removed.Item.ImageModel.Dispose();
            }
        }
    }

    #endregion
}