namespace PicView.Core.Thumbnailing;

/// <summary>
/// The thumbnails made at tile size that <see cref="Thumbnails"/> keeps: one per path, with the height it was made
/// for, up to a budget in bytes. When the budget is passed, the least recently used are let go of.
/// </summary>
internal sealed class TileThumbnailStore(long budgetInBytes)
{
    private readonly Dictionary<string, LinkedListNode<Entry>> _nodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly LinkedList<Entry> _order = new(); // first is the most recently used
    private readonly Lock _lock = new();
    private long _bytes;

    /// <summary>
    /// The bytes the kept thumbnails are counted as.
    /// </summary>
    public long Bytes
    {
        get
        {
            lock (_lock)
            {
                return _bytes;
            }
        }
    }

    /// <summary>
    /// The number of kept thumbnails.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _nodes.Count;
            }
        }
    }

    /// <summary>
    /// The kept thumbnail of <paramref name="path"/> and the height it was made for. A hit makes it the most
    /// recently used.
    /// </summary>
    public bool TryGet(string path, out object? thumbnail, out uint height)
    {
        lock (_lock)
        {
            if (_nodes.TryGetValue(path, out var node))
            {
                if (node != _order.First)
                {
                    _order.Remove(node);
                    _order.AddFirst(node);
                }

                thumbnail = node.Value.Thumbnail;
                height = node.Value.Height;
                return true;
            }
        }

        thumbnail = null;
        height = 0;
        return false;
    }

    /// <summary>
    /// Keeps <paramref name="thumbnail"/> as the most recently used, in place of the one kept for
    /// <paramref name="path"/>. Then lets go of the least recently used while over the budget, but never the one just
    /// added.
    /// </summary>
    public void Add(string path, object thumbnail, uint height, long bytes)
    {
        lock (_lock)
        {
            AddNode(path, thumbnail, height, bytes);
        }
    }

    /// <summary>
    /// Like <see cref="Add"/>, but keeps nothing when the thumbnail kept for <paramref name="path"/> was made for at
    /// least <paramref name="height"/>, so the tallest is kept.
    /// </summary>
    /// <returns>Whether <paramref name="thumbnail"/> was kept.</returns>
    public bool AddIfTaller(string path, object thumbnail, uint height, long bytes)
    {
        lock (_lock)
        {
            if (_nodes.TryGetValue(path, out var existing) && existing.Value.Height >= height)
            {
                return false;
            }

            AddNode(path, thumbnail, height, bytes);
            return true;
        }
    }

    // The caller holds the lock
    private void AddNode(string path, object thumbnail, uint height, long bytes)
    {
        RemoveNode(path);

        var node = new LinkedListNode<Entry>(new Entry(path, thumbnail, height, bytes));
        _order.AddFirst(node);
        _nodes[path] = node;
        _bytes += bytes;
        if (bytes > 0)
        {
            GC.AddMemoryPressure(bytes);
        }

        while (_bytes > budgetInBytes && _order.Last is { } last && last != node)
        {
            RemoveNode(last.Value.Path);
        }
    }

    /// <summary>
    /// Lets go of the thumbnail of <paramref name="path"/>, e.g., after the file changed.
    /// </summary>
    public void Remove(string path)
    {
        lock (_lock)
        {
            RemoveNode(path);
        }
    }

    /// <summary>
    /// Keeps the thumbnail of <paramref name="oldPath"/> under <paramref name="newPath"/>, in the same place in the
    /// order. One already kept for <paramref name="newPath"/> is let go of.
    /// </summary>
    public void Move(string oldPath, string newPath)
    {
        lock (_lock)
        {
            if (!_nodes.TryGetValue(oldPath, out var node))
            {
                return;
            }

            if (_nodes.TryGetValue(newPath, out var existing) && existing != node)
            {
                RemoveNode(newPath);
            }

            _nodes.Remove(oldPath);
            node.Value.Path = newPath;
            _nodes[newPath] = node;
        }
    }

    private void RemoveNode(string path)
    {
        if (!_nodes.Remove(path, out var node))
        {
            return;
        }

        _order.Remove(node);
        _bytes -= node.Value.Bytes;
        if (node.Value.Bytes > 0)
        {
            GC.RemoveMemoryPressure(node.Value.Bytes);
        }
    }

    private sealed class Entry(string path, object thumbnail, uint height, long bytes)
    {
        public string Path { get; set; } = path;
        public object Thumbnail { get; } = thumbnail;
        public uint Height { get; } = height;
        public long Bytes { get; } = bytes;
    }
}
