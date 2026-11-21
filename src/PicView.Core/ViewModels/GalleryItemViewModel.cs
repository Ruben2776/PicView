using PicView.Core.Extensions;
using PicView.Core.Gallery;
using R3;

namespace PicView.Core.ViewModels;

public class GalleryItemViewModel : IDisposable
{
    private string? _fileName;
    private string? _fileSize;
    private string? _fileDate;
    private GalleryFileDetails? _details;

    public void Dispose()
    {
        Disposable.Dispose(
            Image,
            ImageSize,
            IsMotionPhoto);
    }

    public BindableReactiveProperty<object?> Image { get; } = new();
    /// <summary>The image's pixel size as text. Empty until <see cref="SetDetails"/> gives the item its details.</summary>
    public BindableReactiveProperty<string> ImageSize { get; } = new(string.Empty, StringComparer.Ordinal);
    /// <summary>Whether the file carries a motion photo video (drives the gallery badge).</summary>
    public BindableReactiveProperty<bool> IsMotionPhoto { get; } = new(false);

    private bool _isMotionPhotoChecked;

    /// <summary>
    /// Whether <see cref="IsMotionPhoto"/> was detected from the file, so it is not detected again. Detection waits for
    /// the item to be shown with the badges on, so most items are never checked.
    /// </summary>
    internal bool IsMotionPhotoChecked
    {
        get => Volatile.Read(ref _isMotionPhotoChecked);
        set => Volatile.Write(ref _isMotionPhotoChecked, value);
    }

    public FileInfo? FileInfo { get; set; }

    // The texts about the file are plain strings made the first time the tooltip reads them: an item's file never
    // changes, since a renamed file gets a new item, and most items are never hovered

    /// <summary>The file's full path, or empty for an item without a file.</summary>
    public string FileLocation => FileInfo?.FullName ?? string.Empty;

    /// <summary>The file's name without its extension, or empty for an item without a file.</summary>
    public string FileName =>
        _fileName ??= FileInfo is null ? string.Empty : Path.GetFileNameWithoutExtension(FileInfo.Name);

    /// <summary>The file's size as a labeled text, or empty for an item without a file.</summary>
    public string FileSize => _fileSize ??= FileInfo is null ? string.Empty : GalleryThumbInfo.FileSizeText(FileInfo);

    /// <summary>The file's modified date as a labeled text, or empty for an item without a file.</summary>
    public string FileDate => _fileDate ??= FileInfo is null ? string.Empty : GalleryThumbInfo.FileDateText(FileInfo);

    /// <summary>
    /// What the item's file said about itself, or null until it is read. <see cref="SetDetails"/> replaces it whole, so
    /// read it once and use that reference: its width and height are then from the same read.
    /// </summary>
    public GalleryFileDetails? Details => Volatile.Read(ref _details);

    /// <summary>The coordinator that loads the item's thumbnail, or null for an item outside a gallery.</summary>
    internal GalleryCoordinator? Coordinator { get; init; }

    /// <summary>The height the item's thumbnail is made for.</summary>
    internal uint ThumbnailHeight { get; init; }

    /// <summary>
    /// Loads the item's thumbnail: the kept one, or one made for <see cref="ThumbnailHeight"/> when its turn comes.
    /// </summary>
    /// <returns>The thumbnail, or null for an item outside a gallery or a file that gives none.</returns>
    public ValueTask<object?> LoadThumbnailAsync(CancellationToken cancellationToken) =>
        Coordinator is null
            ? ValueTask.FromResult<object?>(null)
            : Coordinator.LoadThumbnailAsync(this, cancellationToken);

    /// <summary>
    /// Detects <see cref="IsMotionPhoto"/> off the caller's thread, unless the badges are hidden, the item was checked
    /// before or <paramref name="shownToken"/> says it is no longer shown. For an item shown without
    /// <see cref="LoadThumbnailAsync"/>, which detects it along with the thumbnail.
    /// </summary>
    /// <param name="shownToken">Cancelled when the item scrolls out of view.</param>
    public void DetectMotionPhoto(CancellationToken shownToken)
    {
        if (Coordinator is not null)
        {
            GalleryCoordinator.DetectMotionPhotoInBackground(this, shownToken);
        }
    }

    /// <summary>
    /// The height the Tile Thumbnail in <see cref="Image"/> was made for; 0 while <see cref="Image"/> holds a small
    /// thumbnail or nothing. Written only on the UI thread, by the gallery item that sets <see cref="Image"/>.
    /// </summary>
    public uint TileHeightShown { get; set; }

    /// <summary>
    /// Looks up the thumbnail kept for the item's file: its Tile Thumbnail with the height it was made for, else its
    /// small thumbnail with <paramref name="tileHeight"/> 0. Never decodes.
    /// </summary>
    /// <returns>Whether any thumbnail is kept; false for an item outside a gallery.</returns>
    public bool TryGetKeptThumbnail(out object? thumbnail, out uint tileHeight)
    {
        if (Coordinator is null)
        {
            thumbnail = null;
            tileHeight = 0;
            return false;
        }

        return Coordinator.TryGetKeptThumbnail(this, out thumbnail, out tileHeight);
    }

    /// <summary>
    /// Loads a Tile Thumbnail of the item's file made at <paramref name="height"/>, or the kept one made for at least
    /// that height.
    /// </summary>
    /// <returns>The thumbnail, or null for an item outside a gallery or a file that gives none.</returns>
    public ValueTask<object?> LoadTileThumbnailAsync(uint height, CancellationToken cancellationToken) =>
        Coordinator is null
            ? ValueTask.FromResult<object?>(null)
            : Coordinator.LoadTileThumbnailAsync(this, height, cancellationToken);

    /// <summary>
    /// Replaces the item's <see cref="Details"/> and the size text that shows them.
    /// </summary>
    internal void SetDetails(GalleryFileDetails details)
    {
        Volatile.Write(ref _details, details);
        ImageSize.Value = StringExtensions.CombineSize(details.PixelWidth, details.PixelHeight);
    }
}
