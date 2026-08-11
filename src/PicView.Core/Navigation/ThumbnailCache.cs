using System.Collections.Concurrent;
using PicView.Core.Navigation.Interfaces;

namespace PicView.Core.Navigation;

public class ThumbnailCache : IThumbnailCache
{
    private readonly ConcurrentDictionary<string, object> _thumbnails = new(StringComparer.OrdinalIgnoreCase);
    
    // Path -> Set of Owners
    private readonly Dictionary<string, HashSet<uint>> _ownersByFile = new(StringComparer.OrdinalIgnoreCase);
    
    // Owner -> Set of Paths
    private readonly Dictionary<uint, HashSet<string>> _filesByOwner = new();
    
    private readonly Lock _lock = new();

    public void Add(uint ownerId, string path, object thumbnail)
    {
        lock (_lock)
        {
            _thumbnails[path] = thumbnail;

            if (!_ownersByFile.TryGetValue(path, out var owners))
            {
                owners = [];
                _ownersByFile[path] = owners;
            }
            owners.Add(ownerId);

            if (!_filesByOwner.TryGetValue(ownerId, out var files))
            {
                files = [];
                _filesByOwner[ownerId] = files;
            }
            files.Add(path);
        }
    }

    public bool TryGet(string path, out object? thumbnail) =>
        _thumbnails.TryGetValue(path, out thumbnail);

    public void Remove(string path)
    {
        lock (_lock)
        {
            _thumbnails.TryRemove(path, out _);

            if (!_ownersByFile.TryGetValue(path, out var owners))
            {
                return;
            }

            foreach (var owner in owners)
            {
                if (_filesByOwner.TryGetValue(owner, out var files))
                {
                    files.Remove(path);
                }
            }
            _ownersByFile.Remove(path);
        }
    }

    public void RemoveOwner(uint ownerId)
    {
        lock (_lock)
        {
            if (!_filesByOwner.TryGetValue(ownerId, out var files))
            {
                return;
            }

            foreach (var path in files)
            {
                if (!_ownersByFile.TryGetValue(path, out var owners))
                {
                    continue;
                }

                owners.Remove(ownerId);
                if (owners.Count != 0)
                {
                    continue;
                }

                _ownersByFile.Remove(path);
                _thumbnails.TryRemove(path, out _);
            }
            
            _filesByOwner.Remove(ownerId);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _thumbnails.Clear();
            _ownersByFile.Clear();
            _filesByOwner.Clear();
        }
    }

    public bool IsEmpty => _thumbnails.IsEmpty;
}
