using PicView.Core.Models;
using PicView.Core.Preloading;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation.Interfaces;

/// <summary>
/// Defines the contract for the central caching station.
/// <para>
/// Implementations of this interface serve as the primary access point for acquiring <see cref="ImageModel"/> data, 
/// managing the lifecycle of cached items across different owners (tabs), and coordinating 
/// with the <see cref="IPreloader"/> worker.
/// </para>
/// </summary>
public interface IImageCache
{
    /// <summary>
    /// Retrieves an image from the cache or triggers a load if it is missing.
    /// </summary>
    /// <param name="ownerId">The unique ID of the tab requesting the image.</param>
    /// <param name="index">The index of the image in the current file list.</param>
    /// <param name="list">The list of files to resolve the index against.</param>
    /// <param name="ct">Token to cancel the load operation.</param>
    /// <returns>The loaded or cached <see cref="ImageModel"/>, or null if loading failed.</returns>
    Task<ImageModel?> LoadAsync(uint ownerId, int index, IReadOnlyList<FileInfo> list,
        CancellationToken ct = default);
    
    /// <summary>
    /// Attempts to retrieve a preload value by the file info.
    /// </summary>
    bool TryGet(FileInfo f, out PreLoadValue? value);

    /// <summary>
    /// Clears cache items associated specifically with the given owner ID.
    /// </summary>
    void Clear(uint ownerId);

    /// <summary>
    /// Adds a value to the cache, potentially triggering eviction of distant items.
    /// </summary>
    void Add(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse);

    /// <summary>
    /// Tries to add a value to the cache, returning the evicted value if capacity was exceeded.
    /// </summary>
    bool TryAdd(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse, out PreLoadValue? value);

    /// <summary>
    /// Initiates the background predictive loading (pre-fetching) process.
    /// </summary>
    void Preload(uint ownerId, int currentIndex, bool reversed, IReadOnlyList<FileInfo> files, CancellationToken token);
    
    /// <summary>
    /// Removes an owner from the cache tracking. 
    /// Should be called when a Tab is closed to free up reserved capacity.
    /// </summary>
    void RemoveOwner(uint ownerId);

    /// <summary>
    /// Registers a new owner (tab) to allow it to reserve capacity in the cache.
    /// </summary>
    void RegisterOwner(uint ownerId);

    /// <summary>
    /// Helper to clear resources specifically for a <see cref="TabViewModel"/>.
    /// Transfers relevant cache items to another eligible tab if possible before removing.
    /// </summary>
    void Clear(TabViewModel tab, string directory);

    /// <summary>
    /// Resynchronizes the cache for a specific owner when the file list changes (e.g., sorting).
    /// </summary>
    void Resynchronize(uint ownerId, IReadOnlyList<FileInfo> files);

    ValueTask<bool> WaitForLoadingCompleteAsync(uint ownerId, int index, IReadOnlyList<FileInfo> list,
        CancellationToken ct = default);
}