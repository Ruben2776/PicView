using PicView.Core.Models;

namespace PicView.Core.Navigation;

public enum ViewHistoryKind
{
    File,
    ArchivePage,
    SingleImage
}

/// <summary>
/// One thing a tab showed, as its <see cref="ViewHistory"/> keeps it.
/// </summary>
/// <param name="Kind">A file, a page of an archive, or a Single Image.</param>
/// <param name="Path">
/// The file, the archive, or the URL of a download. Null for a Single Image that isn't a URL download.
/// </param>
/// <param name="Page">The page of a multi-page TIFF, else -1.</param>
/// <param name="ArchiveEntry">The entry in the archive that the shown file was extracted from.</param>
/// <param name="Model">A Single Image's shown model, kept so that it shows again with no new download or paste.</param>
/// <param name="SingleImageType">How the Single Image came to the tab.</param>
/// <param name="Title">The title the Single Image was shown with.</param>
public readonly record struct ViewHistoryEntry(
    ViewHistoryKind Kind,
    string? Path,
    int Page = -1,
    string? ArchiveEntry = null,
    ImageModel? Model = null,
    SingleImageType SingleImageType = SingleImageType.None,
    string? Title = null)
{
    /// <summary>
    /// A file, or one page of a multi-page TIFF.
    /// </summary>
    public static ViewHistoryEntry ForFile(string path, int page = -1) =>
        new(ViewHistoryKind.File, path, page);

    /// <summary>
    /// A page of an archive. <paramref name="entry"/> is the archive entry that the shown file was extracted from.
    /// </summary>
    public static ViewHistoryEntry ForArchivePage(string archivePath, string? entry) =>
        new(ViewHistoryKind.ArchivePage, archivePath, ArchiveEntry: entry);

    /// <summary>
    /// A Single Image, which keeps its <paramref name="model"/>. <paramref name="url"/> is the URL of a download, else null.
    /// </summary>
    public static ViewHistoryEntry ForSingleImage(ImageModel model, SingleImageType type, string? title,
        string? url = null) =>
        new(ViewHistoryKind.SingleImage, url, Model: model, SingleImageType: type, Title: title);

    /// <summary>
    /// The path File History records: the file, the archive or the URL. Null for a Single Image that isn't a URL download.
    /// </summary>
    public string? HistoryPath =>
        Kind is ViewHistoryKind.SingleImage && SingleImageType is not SingleImageType.Url ? null : Path;

    /// <summary>
    /// The path of the file or archive on disk. Null for a Single Image, whose URL is not a file.
    /// </summary>
    public string? FilePath => Kind is ViewHistoryKind.SingleImage ? null : Path;

    /// <summary>
    /// Whether both entries show the same thing. Paths compare ignoring case, as the file list does.
    /// </summary>
    public bool Matches(in ViewHistoryEntry other)
    {
        if (Kind != other.Kind)
        {
            return false;
        }

        // The Path property hides System.IO.Path in here, so this compares strings directly
        return Kind switch
        {
            ViewHistoryKind.File => Page == other.Page &&
                                    string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase),
            ViewHistoryKind.ArchivePage => string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase) &&
                                           string.Equals(ArchiveEntry, other.ArchiveEntry, StringComparison.Ordinal),
            // A Single Image is the model it keeps, and a download is also the same one again at the same URL
            ViewHistoryKind.SingleImage => ReferenceEquals(Model, other.Model) ||
                                           (SingleImageType is SingleImageType.Url &&
                                            other.SingleImageType is SingleImageType.Url &&
                                            string.Equals(Path, other.Path, StringComparison.Ordinal)),
            _ => false
        };
    }
}
