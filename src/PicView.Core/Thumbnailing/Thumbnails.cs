using PicView.Core.Gallery;
using PicView.Core.ImageDecoding;
using PicView.Core.Navigation;

namespace PicView.Core.Thumbnailing;

/// <summary>
/// Makes every Thumbnail PicView shows: a bitmap of a file no taller than a requested height, never the full image.
/// It keeps the thumbnails it makes for their owners, scales down a still image already in memory instead of reading
/// the file again, and decodes at most a few files at a time, never on the caller's thread. One serves the whole app
/// (ADR-0031).
/// </summary>
public sealed class Thumbnails
{
    /// <summary>
    /// The owner of thumbnails that belong to no tab, such as File History's. No tab has this id, so closing a tab
    /// never drops them.
    /// </summary>
    public const uint AppOwner = 0;

    /// <summary>
    /// How many bytes of Tile Thumbnails are kept before the least recently used are let go of.
    /// </summary>
    public const long TileBudgetInBytes = 256L * 1024 * 1024;

    private readonly IThumbnailDecoder _decoder;
    private readonly SharedImageCache _images;
    private readonly ThumbnailStore _store = new();
    private readonly TileThumbnailStore _tiles;
    private readonly SemaphoreSlim _slots;

    // Counts the files forgotten, so a thumbnail made from a file's old image while it was forgotten is not kept
    private long _forgotten;

    /// <param name="decoder">Turns files and images into bitmaps.</param>
    /// <param name="images">The images already decoded, whose still images are scaled down instead of decoded.</param>
    /// <param name="slots">How many files may decode at once; by default half the processors, from 2 to 4.</param>
    /// <param name="tileBudgetInBytes">How many bytes of Tile Thumbnails are kept.</param>
    public Thumbnails(IThumbnailDecoder decoder, SharedImageCache images, int slots = 0,
        long tileBudgetInBytes = TileBudgetInBytes)
    {
        _decoder = decoder;
        _images = images;
        _tiles = new TileThumbnailStore(tileBudgetInBytes);
        _slots = new SemaphoreSlim(slots > 0 ? slots : Math.Clamp(Environment.ProcessorCount / 2, 2, 4));
    }

    /// <summary>
    /// The height a gallery thumbnail is made for: the taller of the docked and expanded gallery item sizes, so one
    /// thumbnail serves both.
    /// </summary>
    public static uint GalleryHeight
    {
        get
        {
            var height = Math.Max(Settings.Gallery.DockedGalleryItemSize, Settings.Gallery.ExpandedGalleryItemSize);
            return (uint)(height <= 0 ? GalleryDefaults.DefaultDockedGalleryHeight : height);
        }
    }

    /// <summary>
    /// A Quick Thumbnail of <paramref name="file"/>: a kept thumbnail of any height, or else a ready-made one from the
    /// OS or the file's EXIF data. It never decodes the image and keeps nothing, so it may be absent but is quick.
    /// </summary>
    public ValueTask<object?> GetQuickAsync(FileInfo file, uint height, CancellationToken ct = default)
    {
        if (_store.TryGet(file.FullName, out var kept, out _))
        {
            return ValueTask.FromResult(kept);
        }

        return new ValueTask<object?>(Task.Run(() => _decoder.GetReadyMade(file, height), ct));
    }

    /// <summary>
    /// A thumbnail of <paramref name="file"/> no taller than <paramref name="height"/>, made if none is kept that tall.
    /// </summary>
    /// <param name="owner">Who keeps the thumbnail: a tab's id, <see cref="AppOwner"/>, or null to keep nothing.</param>
    /// <param name="ct">Cancels a request still waiting for its turn to decode.</param>
    /// <returns>The thumbnail, or null when the file gives none.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled before decoding started.</exception>
    public ValueTask<object?> GetAsync(FileInfo file, uint height, uint? owner, CancellationToken ct = default)
    {
        if (_store.TryGet(file.FullName, out var kept, out var keptHeight) && keptHeight >= height)
        {
            if (owner is { } id)
            {
                _store.AddOwner(id, file.FullName);
            }

            return ValueTask.FromResult(kept);
        }

        return new ValueTask<object?>(MakeAsync(file, height, owner, false, ct));
    }

    /// <summary>
    /// A Tile Thumbnail of <paramref name="file"/>: a thumbnail made at a gallery tile's size, no taller than
    /// <paramref name="height"/>, made if none is kept that was made for at least that height. Tile Thumbnails are
    /// kept for the whole app up to <see cref="TileBudgetInBytes"/>, and never in an owner's store.
    /// </summary>
    /// <param name="ct">Cancels a request still waiting for its turn to decode.</param>
    /// <returns>The thumbnail, or null when the file gives none.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled before decoding started.</exception>
    public ValueTask<object?> GetTileAsync(FileInfo file, uint height, CancellationToken ct = default)
    {
        if (_tiles.TryGet(file.FullName, out var kept, out var keptHeight) && keptHeight >= height)
        {
            return ValueTask.FromResult(kept);
        }

        return new ValueTask<object?>(MakeAsync(file, height, null, true, ct));
    }

    /// <summary>
    /// The kept Tile Thumbnail of <paramref name="file"/>, of any height, with the height it was made for. Failing
    /// that, the kept small thumbnail with <paramref name="tileHeight"/> 0. Never decodes and takes no slot.
    /// </summary>
    /// <returns>Whether any thumbnail is kept.</returns>
    public bool TryGetKept(FileInfo file, out object? thumbnail, out uint tileHeight)
    {
        var path = file.FullName;
        if (_tiles.TryGet(path, out thumbnail, out tileHeight))
        {
            return true;
        }

        tileHeight = 0;
        return _store.TryGet(path, out thumbnail, out _);
    }

    /// <summary>
    /// Keeps the Tile Thumbnail of <paramref name="oldPath"/> under <paramref name="newPath"/>, and the small
    /// thumbnail too, for <paramref name="owner"/>, e.g., for a file whose folder was renamed.
    /// </summary>
    public void Relocate(uint owner, string oldPath, string newPath)
    {
        _tiles.Move(oldPath, newPath);
        if (_store.TryGet(oldPath, out var small, out var height) && small is not null)
        {
            _store.Add(owner, newPath, small, height);
        }
    }

    /// <summary>
    /// Keeps <paramref name="thumbnail"/>, made for <paramref name="height"/>, as the thumbnail of
    /// <paramref name="path"/>, e.g., for a file whose folder was renamed.
    /// </summary>
    public void Keep(uint owner, string path, object thumbnail, uint height) =>
        _store.Add(owner, path, thumbnail, height);

    /// <summary>
    /// Drops the thumbnail of <paramref name="path"/> whoever keeps it, e.g., after the file changed or was deleted.
    /// </summary>
    public void Forget(string path)
    {
        Interlocked.Increment(ref _forgotten);
        _store.Remove(path);
        _tiles.Remove(path);
    }

    /// <summary>
    /// Drops every thumbnail only <paramref name="owner"/> keeps, e.g., when its tab closes.
    /// </summary>
    public void Release(uint owner) => _store.RemoveOwner(owner);

    private async Task<object?> MakeAsync(FileInfo file, uint height, uint? owner, bool tile, CancellationToken ct)
    {
        await _slots.WaitAsync(ct).ConfigureAwait(false);
        var forgotten = Interlocked.Read(ref _forgotten);
        object? thumbnail;
        try
        {
            // Reading a file can block for seconds, and the caller may be the UI thread
            thumbnail = await Task.Run(() => Make(file, height, tile), CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _slots.Release();
        }

        // A file forgotten while this one was made may be this file, changed on disk: what was read is then stale.
        // Forgetting is rare, so any one skips keeping rather than tracking which file it was
        if (thumbnail is not null && Interlocked.Read(ref _forgotten) == forgotten)
        {
            if (tile)
            {
                _tiles.Add(file.FullName, thumbnail, height, _decoder.SizeInBytes(thumbnail));
            }
            else if (owner is { } id)
            {
                _store.Add(id, file.FullName, thumbnail, height);
            }
        }

        return thumbnail;
    }

    private object? Make(FileInfo file, uint height, bool tile)
    {
        // A still image already in memory scales down far faster than the file decodes. An animation or a drawing
        // would need its first frame or a raster, which decoding the file makes anyway
        if (_images.TryUseReady(file, model => model is { ImageType: ImageType.Bitmap, Image: { } image }
                ? _decoder.ScaleDown(image, height)
                : null, out var scaled) && scaled is not null)
        {
            return scaled;
        }

        return tile ? _decoder.DecodeWithoutReadyMade(file, height) : _decoder.Decode(file, height);
    }
}
