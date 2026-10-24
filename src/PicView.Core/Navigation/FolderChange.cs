
namespace PicView.Core.Navigation;

public enum FolderChangeKind
{
    Created,
    Changed,
    Renamed,
    Deleted,

    /// <summary>
    /// A subfolder was deleted or moved away with its files, which raises no event for each of them when it goes to
    /// the Recycle Bin. <see cref="FolderChange.File"/> is the subfolder.
    /// </summary>
    SubfolderDeleted
}

/// <summary>
/// One file event in a folder, as every tab watching the folder receives it.
/// </summary>
/// <param name="Kind">What happened to the file.</param>
/// <param name="File">The file, under its new name when renamed.</param>
/// <param name="OldFullPath">The file's path before it was renamed, else null.</param>
/// <param name="Details">The file's details, read once for every tab that needs them.</param>
/// <param name="CompareNames">How the file lists are sorted, to insert the file in its place.</param>
public sealed record FolderChange(
    FolderChangeKind Kind,
    FileInfo File,
    string? OldFullPath,
    FileDetailsSource Details,
    Func<string, string, int> CompareNames);
