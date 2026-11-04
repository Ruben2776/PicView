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
    /// Loads the item's thumbnail: the cached one, or one made for <see cref="ThumbnailHeight"/> once a slot is free.
    /// </summary>
    /// <returns>The thumbnail, or null for an item outside a gallery or a file that gives none.</returns>
    public ValueTask<object?> LoadThumbnailAsync(CancellationToken cancellationToken) =>
        Coordinator is null
            ? ValueTask.FromResult<object?>(null)
            : Coordinator.LoadThumbnailAsync(this, cancellationToken);

    /// <summary>
    /// Replaces the item's <see cref="Details"/> and the size text that shows them.
    /// </summary>
    internal void SetDetails(GalleryFileDetails details)
    {
        Volatile.Write(ref _details, details);
        ImageSize.Value = StringExtensions.CombineSize(details.PixelWidth, details.PixelHeight);
    }
}
