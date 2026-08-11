namespace PicView.Core.Navigation.Interfaces;

public interface IThumbnailCache
{
    /// <summary>
    /// Adds a thumbnail to the cache for a specific owner.
    /// If the thumbnail already exists, it registers the owner for that thumbnail.
    /// </summary>
    void Add(uint ownerId, string path, object thumbnail);

    /// <summary>
    /// Retrieves a thumbnail from the cache if it exists.
    /// </summary>
    bool TryGet(string path, out object? thumbnail);

    /// <summary>
    /// Removes a specific file from the cache, regardless of owners (e.g., file deleted).
    /// </summary>
    void Remove(string path);

    /// <summary>
    /// Removes an owner. If a thumbnail has no more owners, it is removed from memory.
    /// </summary>
    void RemoveOwner(uint ownerId);

    /// <summary>
    /// Clears the entire cache.
    /// </summary>
    void Clear();

    bool IsEmpty { get; }
}
