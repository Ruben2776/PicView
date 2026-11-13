namespace PicView.Core.Thumbnailing;

/// <summary>
/// The thumbnails <see cref="Thumbnails"/> keeps: one per path, with the height it was made for, held while any
/// owner keeps it.
/// </summary>
internal sealed class ThumbnailStore
{
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<uint, HashSet<string>> _pathsByOwner = new();
    private readonly Lock _lock = new();

    /// <summary>
    /// The kept thumbnail of <paramref name="path"/> and the height it was made for.
    /// </summary>
    public bool TryGet(string path, out object? thumbnail, out uint height)
    {
        lock (_lock)
        {
            if (_entries.TryGetValue(path, out var entry))
            {
                thumbnail = entry.Thumbnail;
                height = entry.Height;
                return true;
            }
        }

        thumbnail = null;
        height = 0;
        return false;
    }

    /// <summary>
    /// Keeps <paramref name="thumbnail"/> for <paramref name="owner"/>. A thumbnail made for a taller height than the
    /// one kept takes its place; a shorter one only adds the owner, so the tallest is kept.
    /// </summary>
    public void Add(uint owner, string path, object thumbnail, uint height)
    {
        lock (_lock)
        {
            if (!_entries.TryGetValue(path, out var entry))
            {
                entry = new Entry(thumbnail, height);
                _entries[path] = entry;
            }
            else if (height > entry.Height)
            {
                entry.Thumbnail = thumbnail;
                entry.Height = height;
            }

            entry.Owners.Add(owner);
            if (!_pathsByOwner.TryGetValue(owner, out var paths))
            {
                paths = [];
                _pathsByOwner[owner] = paths;
            }

            paths.Add(path);
        }
    }

    /// <summary>
    /// Has <paramref name="owner"/> keep the thumbnail of <paramref name="path"/> as well, if one is still kept.
    /// </summary>
    public void AddOwner(uint owner, string path)
    {
        lock (_lock)
        {
            if (!_entries.TryGetValue(path, out var entry))
            {
                return;
            }

            entry.Owners.Add(owner);
            if (!_pathsByOwner.TryGetValue(owner, out var paths))
            {
                paths = [];
                _pathsByOwner[owner] = paths;
            }

            paths.Add(path);
        }
    }

    /// <summary>
    /// Drops the thumbnail of <paramref name="path"/> whoever keeps it, e.g., after the file changed.
    /// </summary>
    public void Remove(string path)
    {
        lock (_lock)
        {
            if (!_entries.Remove(path, out var entry))
            {
                return;
            }

            foreach (var owner in entry.Owners)
            {
                if (_pathsByOwner.TryGetValue(owner, out var paths))
                {
                    paths.Remove(path);
                }
            }
        }
    }

    /// <summary>
    /// Drops <paramref name="owner"/>'s claim on every thumbnail it keeps, and each thumbnail no other owner keeps.
    /// </summary>
    public void RemoveOwner(uint owner)
    {
        lock (_lock)
        {
            if (!_pathsByOwner.Remove(owner, out var paths))
            {
                return;
            }

            foreach (var path in paths)
            {
                if (_entries.TryGetValue(path, out var entry) && entry.Owners.Remove(owner) && entry.Owners.Count is 0)
                {
                    _entries.Remove(path);
                }
            }
        }
    }

    private sealed class Entry(object thumbnail, uint height)
    {
        public object Thumbnail { get; set; } = thumbnail;
        public uint Height { get; set; } = height;
        public HashSet<uint> Owners { get; } = [];
    }
}
