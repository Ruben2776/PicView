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
/// <item>Images no owner references are disposed after <see cref="DisposalDelay"/>, so a file that is
/// asked for again shortly after is served from memory.</item>
/// </list>
/// </remarks>
public sealed class SharedImageCache
{
    /// <summary>
    /// How long an image no owner references waits before it is disposed. No more of them than
    /// <see cref="PreLoaderConfig.MaxCount"/> wait at once: one more disposes the one that waited longest.
    /// </summary>
    private static readonly TimeSpan DisposalDelay = TimeSpan.FromSeconds(15);

    private readonly Func<FileInfo, ImageModel> _imageLoader;

    /// <summary>
    /// Every cached or loading image by full path, whether or not an owner references it.
    /// </summary>
    private readonly ConcurrentDictionary<string, PreLoadValue> _pathLookup;

    private readonly ConcurrentDictionary<uint, Owner> _owners = new();

    /// <summary>
    /// Images whose last reference was released, in the order they were released, which is also the order they may be
    /// disposed in. An entry counts only while its ticket is the value's latest, so a value released again is disposed
    /// by its last release alone.
    /// </summary>
    private readonly Queue<(PreLoadValue Value, long Ticket, long Expiration)> _disposalQueue = new();

    private readonly Lock _disposalLock = new();

    // Read as timestamps, which only go forwards, so setting the system clock back holds no disposal up
    private readonly TimeProvider _clock;

    // The disposal delay in the clock's timestamp ticks
    private readonly long _disposalDelayTicks;

    // Wakes the sweep when the image that waited longest may be disposed, so none waits for another release
    private readonly ITimer _sweepTimer;

    private long _lastDisposalTicket;

    private readonly Preloader _preloader;

    /// <param name="imageLoader">Loads an <see cref="ImageModel"/> from disk; returns an error model on failure.</param>
    public SharedImageCache(Func<FileInfo, ImageModel> imageLoader) : this(imageLoader, TimeProvider.System)
    {
    }

    /// <param name="imageLoader">Loads an <see cref="ImageModel"/> from disk; returns an error model on failure.</param>
    /// <param name="clock">Times the disposal delay. A test seam: everything else uses the system clock.</param>
    internal SharedImageCache(Func<FileInfo, ImageModel> imageLoader, TimeProvider clock)
    {
        _clock = clock;
        _disposalDelayTicks = (long)(DisposalDelay.TotalSeconds * clock.TimestampFrequency);
        _sweepTimer = clock.CreateTimer(static state => ((SharedImageCache)state!).SweepDisposals(), this,
            Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _imageLoader = imageLoader;
        _pathLookup = new ConcurrentDictionary<string, PreLoadValue>(StringComparer.Ordinal);
        // Waiting without a token keeps the preloader's degree of parallelism honest; the load is not cancelled anyway.
        // The preloader is already on the thread pool, so its loads start inline.
        _preloader = new Preloader((ownerId, file) =>
            Acquire(ownerId, file, startLoadOnThreadPool: false).WaitForLoadingCompleteAsync());
    }

    #region Requests

    /// <summary>
    /// Gets the model of <paramref name="file"/> for <paramref name="ownerId"/>: from memory, by joining a load
    /// already in flight, or by starting a new load. The owner holds a reference to it while the file is in its list.
    /// </summary>
    /// <returns>The model, or <see langword="null"/> if <paramref name="ct"/> was cancelled first.</returns>
    /// <remarks>
    /// Completes synchronously, without allocating, when the image is already loaded, so a caller on the UI thread
    /// can check <see cref="ValueTask{TResult}.IsCompleted"/> and show it straight away. A load is never run on the
    /// caller's thread.
    /// </remarks>
    public ValueTask<ImageModel?> GetAsync(uint ownerId, FileInfo file, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
        {
            return new ValueTask<ImageModel?>((ImageModel?)null);
        }

        // The caller may be the UI thread, so a load this request starts goes to the thread pool
        var value = Acquire(ownerId, file, startLoadOnThreadPool: true);
        return value.IsLoading
            ? WaitForLoadAsync(value, ct)
            : new ValueTask<ImageModel?>(value.ImageModel);
    }

    private static async ValueTask<ImageModel?> WaitForLoadAsync(PreLoadValue value, CancellationToken ct)
    {
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
    /// Runs <paramref name="use"/> on the model of <paramref name="file"/> if it is in memory and finished loading,
    /// holding a reference to it for the length of the call so it is not disposed while <paramref name="use"/> reads
    /// it. <paramref name="use"/> must not keep the model or its image.
    /// </summary>
    /// <returns><see langword="false"/> when the file is not in memory or still loading.</returns>
    public bool TryUseReady<TResult>(FileInfo file, Func<ImageModel, TResult> use, out TResult? result)
    {
        PreLoadValue? value;
        // Disposal runs under this lock, so a value that is not disposed here stays undisposed while it is referenced
        lock (_disposalLock)
        {
            if (!_pathLookup.TryGetValue(file.FullName, out value) || value.IsLoading || value.IsDisposed)
            {
                result = default;
                return false;
            }

            value.AddReference();
        }

        try
        {
            result = use(value.ImageModel);
            return true;
        }
        finally
        {
            Release(value);
        }
    }

    /// <summary>
    /// Holds a reference to <paramref name="model"/> while it is the cached image of its file, so it is not disposed,
    /// whatever its owners do, until the returned lease is disposed. For a model shown outside its owner's preload
    /// window, such as a pinned comparison image.
    /// </summary>
    /// <returns>The lease, or <see langword="null"/> when <paramref name="model"/> is not a cached, loaded image.</returns>
    public IDisposable? Hold(ImageModel model)
    {
        if (model.FileInfo?.FullName is not { } path)
        {
            return null;
        }

        PreLoadValue? value;
        // Disposal runs under this lock, so a value that is not disposed here stays undisposed while it is referenced
        lock (_disposalLock)
        {
            if (!_pathLookup.TryGetValue(path, out value) || !ReferenceEquals(value.ImageModel, model) ||
                value.IsLoading || value.IsDisposed)
            {
                return null;
            }

            value.AddReference();
        }

        return new Lease(this, value);
    }

    /// <summary>
    /// Holds a reference to the model of <paramref name="file"/> if it is in memory and finished loading, so it is not
    /// disposed until the returned lease is disposed. For a reader that keeps the image past the call, where
    /// <see cref="TryGetReady"/> gives no such promise.
    /// </summary>
    /// <returns>The lease, or <see langword="null"/> when the file is not in memory or still loading.</returns>
    public IDisposable? HoldReady(FileInfo file, [NotNullWhen(true)] out ImageModel? model)
    {
        PreLoadValue? value;
        // Disposal runs under this lock, so a value that is not disposed here stays undisposed while it is referenced
        lock (_disposalLock)
        {
            if (!_pathLookup.TryGetValue(file.FullName, out value) || value.IsLoading || value.IsDisposed)
            {
                model = null;
                return null;
            }

            value.AddReference();
        }

        model = value.ImageModel;
        return new Lease(this, value);
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
    /// <remarks>
    /// Owners keep their reference to the old image, as a tab may still be showing it; it is released when the new
    /// image takes its place in the owner, or when the owner moves away from it, like any other image.
    /// </remarks>
    public void Invalidate(string path)
    {
        if (!_pathLookup.TryRemove(path, out var value))
        {
            return;
        }

        if (value.ReferenceCount is 0 && !value.IsLoading)
        {
            ScheduleDisposal(value);
        }
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
            ReleaseImages(owner, forget: true);
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
    /// Tells the cache that <paramref name="ownerId"/> stepped to another image, backwards when
    /// <paramref name="reversed"/>. The owner's direction of travel, which <see cref="Preload"/> loads deepest, turns
    /// on the second step in a row the other way, so a single step back is a look back.
    /// </summary>
    public void NoteStep(uint ownerId, bool reversed)
    {
        if (_owners.TryGetValue(ownerId, out var owner))
        {
            owner.NoteStep(reversed);
        }
    }

    /// <summary>
    /// Forgets which way <paramref name="ownerId"/> travels, so it travels forwards again. Call when the owner opens
    /// other files than the ones it stepped through.
    /// </summary>
    public void ResetTravel(uint ownerId)
    {
        if (_owners.TryGetValue(ownerId, out var owner))
        {
            owner.ResetTravel();
        }
    }

    /// <summary>
    /// Loads the images around <paramref name="file"/> in the background: the side <paramref name="reversed"/> names
    /// first, and the side the owner travels to deepest (see <see cref="NoteStep"/>). A preload asked for while one
    /// runs for the same owner follows it.
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
            _preloader.Preload(ownerId, new PreloadRequest(list.Files, index, reversed, owner.TravelsReversed, ct));
        }
    }

    #endregion

    #region Loading and references

    /// <summary>
    /// Returns the cached or loading value of <paramref name="file"/>, starting a load if there is none, and gives
    /// <paramref name="ownerId"/> a reference to it.
    /// </summary>
    /// <param name="startLoadOnThreadPool">Start a new load on the thread pool instead of on the calling thread, where
    /// the loader would run synchronously until its first await.</param>
    [SuppressMessage("Design", "MA0042")]
    private PreLoadValue Acquire(uint ownerId, FileInfo file, bool startLoadOnThreadPool)
    {
        var path = file.FullName;
        while (true)
        {
            if (!_pathLookup.TryGetValue(path, out var value))
            {
                var pending = new PreLoadValue(new ImageModel { FileInfo = file }, isLoading: true);
                value = _pathLookup.GetOrAdd(path, pending);
                if (ReferenceEquals(value, pending))
                {
                    if (startLoadOnThreadPool)
                    {
                        _ = Task.Run(() => Load(file, pending, queueWhenUnreferenced: true));
                    }
                    else
                    {
                        // Its reference comes right below, which also queues it when the owner does not take it.
                        // Queued here first, it would count as waiting and push out an image that does wait
                        Load(file, pending, queueWhenUnreferenced: false);
                    }
                }
            }

            if (TryAddReference(ownerId, path, value))
            {
                return value;
            }

            // Disposed between the lookup and the reference, so it is no use: load the file again. The disposal took
            // it out of the lookup; a value kept under another path than its file's is taken out here
            _pathLookup.TryRemove(new KeyValuePair<string, PreLoadValue>(path, value));
        }
    }

    private void Load(FileInfo file, PreLoadValue pending, bool queueWhenUnreferenced)
    {
        ImageModel model;
        try
        {
            model = _imageLoader(file);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(SharedImageCache), nameof(Load), ex);
            model = new ImageModel { FileInfo = file };
        }

        pending.ImageModel = model;
        pending.IsLoading = false;

        // A load nobody holds (or one invalidated while loading) was not queued while loading; queue it now.
        if (queueWhenUnreferenced && pending.ReferenceCount is 0)
        {
            ScheduleDisposal(pending);
        }
    }

    /// <summary>
    /// Gives <paramref name="ownerId"/> a reference to <paramref name="value"/> at the file's position in its list.
    /// A file outside the owner's list stays unreferenced and is queued for disposal.
    /// </summary>
    private void AddReference(uint ownerId, string path, PreLoadValue value) =>
        TryAddReference(ownerId, path, value);

    /// <summary>
    /// As <see cref="AddReference"/>, unless <paramref name="value"/> was disposed meanwhile, which a value taken from
    /// the lookup may be: it waits for disposal while nothing references it.
    /// </summary>
    /// <returns><see langword="false"/> when the value is disposed, and so got no reference.</returns>
    private bool TryAddReference(uint ownerId, string path, PreLoadValue value)
    {
        if (_owners.TryGetValue(ownerId, out var owner))
        {
            PreLoadValue? evicted = null;
            var added = false;
            var isDisposed = false;
            lock (owner.Gate)
            {
                var list = owner.Files;
                if (!owner.IsForgotten && list.Positions.TryGetValue(path, out var index))
                {
                    owner.Images.TryAdd(index, value, list.Files.Length, owner.IsReversed, out evicted, out var isNewReference);
                    if (isNewReference && !TryReference(value))
                    {
                        owner.Images.Remove(index);
                        isDisposed = true;
                    }
                    added = true;
                }
            }

            if (evicted is not null)
            {
                Release(evicted);
            }
            if (isDisposed)
            {
                return false;
            }
            if (added)
            {
                return true;
            }
        }

        if (value.IsDisposed)
        {
            return false;
        }

        if (value.ReferenceCount is 0 && !value.IsLoading)
        {
            ScheduleDisposal(value);
        }

        return true;
    }

    // Disposal runs under this lock, so a value that is not disposed here stays undisposed while it is referenced
    private bool TryReference(PreLoadValue value)
    {
        lock (_disposalLock)
        {
            if (value.IsDisposed)
            {
                return false;
            }

            value.AddReference();
            return true;
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

    /// <param name="forget">The owner is gone, so a request still under way gives it no reference anymore, which
    /// nothing would release.</param>
    private void ReleaseImages(Owner owner, bool forget = false)
    {
        PreLoadValue[] values;
        lock (owner.Gate)
        {
            owner.IsForgotten |= forget;
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
        // A load is not disposed and waits for nothing: its end queues it, if nothing references it by then
        if (value.IsLoading)
        {
            return;
        }

        lock (_disposalLock)
        {
            value.DisposalTicket = ++_lastDisposalTicket;
            _disposalQueue.Enqueue((value, value.DisposalTicket, _clock.GetTimestamp() + _disposalDelayTicks));
            SweepDisposalsLocked();
        }
    }

    private void SweepDisposals()
    {
        lock (_disposalLock)
        {
            SweepDisposalsLocked();
        }
    }

    /// <summary>
    /// Disposes the images whose delay ended and, beyond <see cref="PreLoaderConfig.MaxCount"/> waiting ones, those
    /// that waited longest, then sets the timer for the next. Callers hold <see cref="_disposalLock"/>.
    /// </summary>
    private void SweepDisposalsLocked()
    {
        var limit = PreLoaderConfig.MaxCount;
        if (_disposalQueue.Count > limit)
        {
            // Only images that still wait count towards the limit: one taken or released again since left its entry
            for (var i = _disposalQueue.Count; i > 0; i--)
            {
                var entry = _disposalQueue.Dequeue();
                if (IsWaiting(entry.Value, entry.Ticket))
                {
                    _disposalQueue.Enqueue(entry);
                }
            }
        }

        var now = _clock.GetTimestamp();
        while (_disposalQueue.TryPeek(out var entry) && (entry.Expiration <= now || _disposalQueue.Count > limit))
        {
            _disposalQueue.Dequeue();
            if (IsWaiting(entry.Value, entry.Ticket))
            {
                TryDispose(entry.Value);
            }
        }

        var wait = _disposalQueue.TryPeek(out var next)
            ? _clock.GetElapsedTime(now, next.Expiration)
            : Timeout.InfiniteTimeSpan;
        _sweepTimer.Change(wait, Timeout.InfiniteTimeSpan);
    }

    private static bool IsWaiting(PreLoadValue value, long ticket) =>
        value.DisposalTicket == ticket && value.ReferenceCount is 0;

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
        PreLoadValue? current = null;
        if (path is not null)
        {
            _pathLookup.TryRemove(new KeyValuePair<string, PreLoadValue>(path, value));
            if (_pathLookup.TryGetValue(path, out current) && ReferenceEquals(current, value))
            {
                return;
            }
        }

        // A model put again after its file was invalidated is the newer value's too, which keeps it alive
        var isKeptByNewerValue = current is not null && ReferenceEquals(current.ImageModel, value.ImageModel);
        if (value.TryMarkDisposed() && !isKeptByNewerValue)
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

            while (_disposalQueue.TryDequeue(out var entry))
            {
                TryDispose(entry.Value);
            }

            _sweepTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
        GC.Collect();
    }

    #endregion

    // A reference from Hold, released once
    private sealed class Lease(SharedImageCache cache, PreLoadValue value) : IDisposable
    {
        private int _isReleased;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isReleased, 1) is 0)
            {
                cache.Release(value);
            }
        }
    }

    private sealed class Owner
    {
        public Lock Gate { get; } = new();
        public EvictingDictionary<PreLoadValue> Images { get; } = new(PreLoaderConfig.MaxCount);
        public FileList Files { get; set; } = FileList.Empty;
        public bool IsReversed { get; set; }

        // Set under Gate once the owner was released for good
        public bool IsForgotten { get; set; }

        private bool _travelsReversed;

        // Whether the owner's last step went against its direction of travel
        private bool _lookedBack;

        /// <summary>
        /// Whether the owner travels backwards through its files. Unlike <see cref="IsReversed"/>, the direction of
        /// its last step alone, this turns only once two steps in a row went the other way.
        /// </summary>
        public bool TravelsReversed
        {
            get
            {
                lock (Gate)
                {
                    return _travelsReversed;
                }
            }
        }

        public void ResetTravel()
        {
            lock (Gate)
            {
                _travelsReversed = false;
                _lookedBack = false;
            }
        }

        /// <summary>
        /// Notes a step in a direction. A single step against the direction of travel is a look back, so the
        /// direction turns on the second one in a row.
        /// </summary>
        public void NoteStep(bool reversed)
        {
            lock (Gate)
            {
                if (reversed == _travelsReversed)
                {
                    _lookedBack = false;
                }
                else if (_lookedBack)
                {
                    _travelsReversed = reversed;
                    _lookedBack = false;
                }
                else
                {
                    _lookedBack = true;
                }
            }
        }
    }

    private sealed record FileList(FileInfo[] Files, Dictionary<string, int> Positions)
    {
        public static FileList Empty { get; } = new([], new Dictionary<string, int>(StringComparer.Ordinal));
    }
}
