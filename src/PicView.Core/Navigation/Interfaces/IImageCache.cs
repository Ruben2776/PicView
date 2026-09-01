using PicView.Core.Models;
using PicView.Core.Preloading;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation.Interfaces;

/// <summary>
/// Acts as the central station for acquiring and managing cached <see cref="ImageModel"/> resources.
/// <para>
/// This class coordinates between the storage container (multiple <see cref="EvictingDictionary{TValue}"/>) 
/// and the background worker (<see cref="Preloader"/>) to ensure images are loaded, retrieved, 
/// and evicted efficiently across multiple tab owners.
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
    /// Attempts to retrieve a cached <see cref="PreLoadValue"/> by its <see cref="FileInfo"/>.
    /// </summary>
    /// <param name="f">The file info key.</param>
    /// <param name="value">Contains the cached item if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if found in cache; otherwise, <see langword="false"/>.</returns>
    bool TryGet(FileInfo f, out PreLoadValue? value);

    /// <summary>
    /// Attempts to retrieve a cached <see cref="PreLoadValue"/> using a high-performance span lookup.
    /// </summary>
    /// <param name="f">The character span representing the file path.</param>
    /// <param name="value">Contains the cached item if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if found in cache; otherwise, <see langword="false"/>.</returns>
    public bool TryGet(ReadOnlySpan<char> f, out PreLoadValue? value);
    
    /// <summary>
    /// Checks if the specified <see cref="PreLoadValue"/> instance is registered in the global lookup.
    /// </summary>
    /// <param name="value">The preloaded image instance to check.</param>
    /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
    public bool Contains(PreLoadValue value);

    /// <summary>
    /// Checks if an image matching the given file path is currently registered in the global lookup.
    /// </summary>
    /// <param name="fileName">The full file path to look up.</param>
    /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
    public bool Contains(string fileName);

    /// <summary>
    /// Checks if an image matching the given <see cref="FileInfo"/> is currently registered in the global lookup.
    /// </summary>
    /// <param name="fileInfo">The file info to look up.</param>
    /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
    public bool Contains(FileInfo fileInfo);

    /// <summary>
    /// Clears cache items associated specifically with the given owner ID.
    /// </summary>
    void Clear(uint ownerId);

    /// <summary>
    /// Adds a preloaded value to the specified owner's cache dictionary at a given index.
    /// </summary>
    /// <param name="ownerId">The unique owner identifier.</param>
    /// <param name="index">The position index in the owner's file list.</param>
    /// <param name="preLoadValue">The preloaded image item to add.</param>
    /// <param name="listCount">Total count of items in the owner's current list.</param>
    /// <param name="isReverse">Indicates direction of movement for eviction evaluation.</param>
    void Add(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse);

    /// <summary>
    /// Attempts to add a preloaded value to an owner's dictionary, incrementing reference count 
    /// if new and processing eviction logic if capacity is exceeded.
    /// </summary>
    /// <param name="ownerId">The unique owner identifier.</param>
    /// <param name="index">The position index in the owner's file list.</param>
    /// <param name="preLoadValue">The preloaded image item to add.</param>
    /// <param name="listCount">Total count of items in the owner's current list.</param>
    /// <param name="isReverse">Indicates direction of movement for eviction evaluation.</param>
    /// <param name="value">Contains the evicted item if capacity was exceeded, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the owner dictionary was found and modified; otherwise, <see langword="false"/>.</returns>
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
    
    /// <summary>
    /// Forces the complete removal of a cached item.
    /// </summary>
    /// <remarks>This should be called when an image file has e.g., been flipped/rotated.</remarks>
    /// <param name="fileName"></param>
    void DeleteFromCache(string fileName);
}