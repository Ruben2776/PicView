using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using PicView.Core.DebugTools;
using PicView.Core.Models;
using PicView.Core.Preloading;

namespace PicView.Core.Navigation;

/// <summary>
/// The image cache shared by every tab. An owner (a tab) asks for the <see cref="ImageModel"/> of a file and gets it
/// from memory, from a load already in flight, or from a new load. The cache keeps the images near each owner's
/// position, preloads around it in the direction of travel, and disposes images no owner references anymore.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>An owner is registered by its first <see cref="SetFiles"/> and forgotten by <see cref="ReleaseOwner"/>.</item>
/// <item>A failed decode yields the loader's error model; <see cref="GetAsync"/> returns <see langword="null"/> only
/// when cancelled.</item>
/// <item>Cancelling a request only abandons that wait: the load completes and stays cached for other owners.</item>
/// <item>Images no owner references are disposed after <see cref="DisposalDelayInSeconds"/>, so a file that is
/// asked for again shortly after is served from memory.</item>
/// </list>
/// </remarks>
public sealed class SharedImageCache
{
    /// <summary>
    /// Time delay in seconds before an image no owner references is disposed.
    /// </summary>
    private const int DisposalDelayInSeconds = 15;

    private readonly Func<FileInfo, ValueTask<ImageModel>> _imageLoader;

    /// <summary>
    /// Every cached or loading image by full path, whether or not an owner references it.
    /// </summary>
    private readonly ConcurrentDictionary<string, PreLoadValue> _pathLookup;

    private readonly ConcurrentDictionary<uint, Owner> _owners = new();

    /// <summary>
    /// Images whose last reference was released, ordered by when they may be disposed.
    /// </summary>
    private readonly PriorityQueue<PreLoadValue, DateTime> _disposalQueue = new();

    private readonly Lock _disposalLock = new();

    private readonly Preloader _preloader;

    /// <param name="imageLoader">Loads an <see cref="ImageModel"/> from disk; returns an error model on failure.</param>
    public SharedImageCache(Func<FileInfo, ValueTask<ImageModel>> imageLoader)
    {
        _imageLoader = imageLoader;
        _pathLookup = new ConcurrentDictionary<string, PreLoadValue>(StringComparer.Ordinal);
        // Waiting without a token keeps the preloader's degree of parallelism honest; the load is not cancelled anyway.
        _preloader = new Preloader((ownerId, file) => Acquire(ownerId, file).WaitForLoadingCompleteAsync());
    }

    #region Requests

    /// <summary>
    /// Gets the model of <paramref name="file"/> for <paramref name="ownerId"/>: from memory, by joining a load
    /// already in flight, or by starting a new load. The owner holds a reference to it while the file is in its list.
    /// </summary>
    /// <returns>The model, or <see langword="null"/> if <paramref name="ct"/> was cancelled first.</returns>
    public async Task<ImageModel?> GetAsync(uint ownerId, FileInfo file, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
        {
            return null;
        }

        var value = Acquire(ownerId, file);
        try
        {
            await value.WaitForLoadingCompleteAsync().WaitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        return value.ImageModel;
    }

    /// <summary>
    /// Gets the model of <paramref name="file"/> if it is in memory and finished loading, without loading it or
    /// taking a reference.
    /// </summary>
    public bool TryGetReady(FileInfo file, [NotNullWhen(true)] out ImageModel? model)
    {
        if (_pathLookup.TryGetValue(file.FullName, out var value) && !value.IsLoading)
        {
            model = value.ImageModel;
            return true;
        }

        model = null;
        return false;
    }

    /// <summary>
    /// Stores an already loaded <paramref name="model"/> for <paramref name="file"/>, replacing any cached image of
    /// the file, and gives <paramref name="ownerId"/> a reference to it.
    /// </summary>
    public void Put(uint ownerId, FileInfo file, ImageModel model)
    {
        var path = file.FullName;
        if (_pathLookup.TryGetValue(path, out var existing))
        {
            if (ReferenceEquals(existing.ImageModel, model))
            {
                AddReference(ownerId, path, existing);
                return;
            }
            Detach(existing);
        }

        var value = new PreLoadValue(model);
        _pathLookup[path] = value;
        AddReference(ownerId, path, value);
    }

    /// <summary>
    /// Drops the cached image of a file, e.g., after it was changed on disk, so the next request loads it again.
    /// </summary>
    public void Invalidate(string path)
    {
        if (!_pathLookup.TryRemove(path, out var value))
        {
            return;
        }

        Detach(value);
    }

    #endregion

    #region Owners

    /// <summary>
    /// Sets the file list of <paramref name="ownerId"/>, registering the owner on first use. Cached images keep their
    /// reference when their file is still in the list and are released otherwise. Call again after every change to
    /// the list; the cache keeps its own copy.
    /// </summary>
    public void SetFiles(uint ownerId, IReadOnlyList<FileInfo> files)
    {
        var owner = _owners.GetOrAdd(ownerId, static _ => new Owner());
        var positions = new Dictionary<string, int>(files.Count, StringComparer.Ordinal);
        for (var i = 0; i < files.Count; i++)
        {
            positions[files[i].FullName] = i;
        }
        var list = new FileList(files.ToArray(), positions);

        var released = new List<PreLoadValue>();
        lock (owner.Gate)
        {
            owner.Files = list;
            var items = owner.Images.ToArray();
            var anyMoved = Array.Exists(items, item =>
                !positions.TryGetValue(item.Value.ImageModel.FileInfo!.FullName, out var index) || index != item.Key);
            if (anyMoved)
            {
                owner.Images.Clear();
                foreach (var item in items)
                {
                    if (positions.TryGetValue(item.Value.ImageModel.FileInfo!.FullName, out var index))
                    {
                        // Moving within the same owner keeps its single reference.
                        owner.Images.TryAdd(index, item.Value, files.Count, owner.IsReversed, out var evicted, out _);
                        if (evicted is not null)
                        {
                            released.Add(evicted);
                        }
                    }
                    else
                    {
                        released.Add(item.Value);
                    }
                }
            }
        }

        foreach (var value in released)
        {
            Release(value);
        }
    }

    /// <summary>
    /// Releases every image <paramref name="ownerId"/> references, keeping the owner and its file list.
    /// </summary>
    public void ReleaseImages(uint ownerId)
    {
        if (_owners.TryGetValue(ownerId, out var owner))
        {
            ReleaseImages(owner);
        }
    }

    /// <summary>
    /// Releases every image <paramref name="ownerId"/> references and forgets the owner. Call when a tab closes.
    /// </summary>
    public void ReleaseOwner(uint ownerId)
    {
        if (_owners.TryRemove(ownerId, out var owner))
        {
            ReleaseImages(owner);
        }
    }

    /// <summary>
    /// Releases every image of every owner, keeping the owners and their file lists.
    /// </summary>
    public void ReleaseAll()
    {
        foreach (var owner in _owners.Values)
        {
            ReleaseImages(owner);
        }
    }

    #endregion

    #region Preloading

    /// <summary>
    /// Loads the images around <paramref name="file"/> in the background, ahead in the direction of travel first.
    /// Does nothing while a preload for the same owner is still running.
    /// </summary>
    public void Preload(uint ownerId, FileInfo file, bool reversed, CancellationToken ct)
    {
        if (!_owners.TryGetValue(ownerId, out var owner))
        {
            return;
        }

        owner.IsReversed = reversed;
        var list = owner.Files;
        if (list.Positions.TryGetValue(file.FullName, out var index))
        {
            _preloader.Preload(ownerId, list.Files, index, reversed, ct);
        }
    }

    #endregion

    #region Loading and references

    /// <summary>
    /// Returns the cached or loading value of <paramref name="file"/>, starting a load if there is none, and gives
    /// <paramref name="ownerId"/> a reference to it.
    /// </summary>
    private PreLoadValue Acquire(uint ownerId, FileInfo file)
    {
        var path = file.FullName;
        if (!_pathLookup.TryGetValue(path, out var value))
        {
            var pending = new PreLoadValue(new ImageModel { FileInfo = file }, isLoading: true);
            value = _pathLookup.GetOrAdd(path, pending);
            if (ReferenceEquals(value, pending))
            {
                _ = LoadAsync(file, pending);
            }
        }

        AddReference(ownerId, path, value);
        return value;
    }

    private async Task LoadAsync(FileInfo file, PreLoadValue pending)
    {
        ImageModel model;
        try
        {
            model = await _imageLoader(file).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(SharedImageCache), nameof(LoadAsync), ex);
            model = new ImageModel { FileInfo = file };
        }

        pending.ImageModel = model;
        pending.IsLoading = false;

        // A load nobody holds (or one invalidated while loading) was not queued while loading; queue it now.
        if (pending.ReferenceCount is 0)
        {
            ScheduleDisposal(pending);
        }
    }

    /// <summary>
    /// Gives <paramref name="ownerId"/> a reference to <paramref name="value"/> at the file's position in its list.
    /// A file outside the owner's list stays unreferenced and is queued for disposal.
    /// </summary>
    private void AddReference(uint ownerId, string path, PreLoadValue value)
    {
        if (_owners.TryGetValue(ownerId, out var owner))
        {
            PreLoadValue? evicted = null;
            var added = false;
            lock (owner.Gate)
            {
                var list = owner.Files;
                if (list.Positions.TryGetValue(path, out var index))
                {
                    owner.Images.TryAdd(index, value, list.Files.Length, owner.IsReversed, out evicted, out var isNewReference);
                    if (isNewReference)
                    {
                        value.AddReference();
                    }
                    added = true;
                }
            }

            if (evicted is not null)
            {
                Release(evicted);
            }
            if (added)
            {
                return;
            }
        }

        if (value.ReferenceCount is 0 && !value.IsLoading)
        {
            ScheduleDisposal(value);
        }
    }

    /// <summary>
    /// Removes <paramref name="value"/> from every owner, releasing their references.
    /// </summary>
    private void Detach(PreLoadValue value)
    {
        var released = 0;
        foreach (var owner in _owners.Values)
        {
            lock (owner.Gate)
            {
                foreach (var item in owner.Images)
                {
                    if (ReferenceEquals(item.Value, value) && owner.Images.Remove(item.Key))
                    {
                        released++;
                        break;
                    }
                }
            }
        }

        for (var i = 0; i < released; i++)
        {
            Release(value);
        }
        if (released is 0 && value.ReferenceCount is 0 && !value.IsLoading)
        {
            ScheduleDisposal(value);
        }
    }

    private void ReleaseImages(Owner owner)
    {
        PreLoadValue[] values;
        lock (owner.Gate)
        {
            values = owner.Images.Values.ToArray();
            owner.Images.Clear();
        }

        foreach (var value in values)
        {
            Release(value);
        }
    }

    private void Release(PreLoadValue value)
    {
        if (value.ReleaseReference() > 0)
        {
            return;
        }

        ScheduleDisposal(value);
    }

    #endregion

    #region Disposal

    private void ScheduleDisposal(PreLoadValue value)
    {
        lock (_disposalLock)
        {
            _disposalQueue.Enqueue(value, DateTime.UtcNow.AddSeconds(DisposalDelayInSeconds));
        }

        SweepExpiredDisposals();
    }

    private void SweepExpiredDisposals()
    {
        var now = DateTime.UtcNow;
        lock (_disposalLock)
        {
            while (_disposalQueue.TryPeek(out var value, out var expiration) && expiration <= now)
            {
                _disposalQueue.Dequeue();
                TryDispose(value);
            }
        }
    }

    /// <summary>
    /// Disposes <paramref name="value"/> if nothing references it anymore, removing it from the lookup unless a newer
    /// image of the same file took its place.
    /// </summary>
    private void TryDispose(PreLoadValue value)
    {
        if (value.ReferenceCount is not 0 || value.IsLoading)
        {
            return;
        }

        var path = value.ImageModel.FileInfo?.FullName;
        if (path is not null)
        {
            _pathLookup.TryRemove(new KeyValuePair<string, PreLoadValue>(path, value));
            if (_pathLookup.TryGetValue(path, out var current) && ReferenceEquals(current, value))
            {
                return;
            }
        }

        if (value.TryMarkDisposed())
        {
            value.ImageModel.Dispose();
        }
    }

    /// <summary>
    /// Immediately disposes every image no owner references, then runs a full garbage collection.
    /// </summary>
    public void ForceDisposalQueue()
    {
        lock (_disposalLock)
        {
            if (_disposalQueue.Count is 0)
            {
                return;
            }

            while (_disposalQueue.TryDequeue(out var value, out _))
            {
                TryDispose(value);
            }
        }
        GC.Collect();
    }

    #endregion

    private sealed class Owner
    {
        public Lock Gate { get; } = new();
        public EvictingDictionary<PreLoadValue> Images { get; } = new(PreLoaderConfig.MaxCount);
        public FileList Files { get; set; } = FileList.Empty;
        public bool IsReversed { get; set; }
    }

    private sealed record FileList(FileInfo[] Files, Dictionary<string, int> Positions)
    {
        public static FileList Empty { get; } = new([], new Dictionary<string, int>(StringComparer.Ordinal));
    }
}
