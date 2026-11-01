
using System.Runtime.InteropServices;

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
    SubfolderDeleted,

    /// <summary>
    /// A folder the tab browses files in was renamed: the watched folder, or one of its subfolders.
    /// <see cref="FolderChange.File"/> is the folder under its new name, and <see cref="FolderChange.OldFullPath"/>
    /// its old path.
    /// </summary>
    FolderRenamed
}

/// <summary>
/// One file event in a folder, as every tab watching the folder receives it.
/// </summary>
/// <param name="Kind">What happened to the file.</param>
/// <param name="File">The file, under its new name when renamed.</param>
/// <param name="OldFullPath">The file's path before it was renamed, else null.</param>
/// <param name="Details">The file's details, read once for every tab that needs them.</param>
/// <param name="CompareNames">How the file lists are sorted, to insert the file in its place.</param>
[StructLayout(LayoutKind.Auto)] 
public sealed record FolderChange(
    FolderChangeKind Kind,
    FileInfo File,
    string? OldFullPath,
    FileDetailsSource Details,
    Func<string, string, int> CompareNames)
{
    /// <summary>
    /// Where <paramref name="path"/> is once the folder <paramref name="oldFolder"/> is renamed to
    /// <paramref name="newFolder"/>, or null when the path is not in that folder.
    /// </summary>
    public static string? PathInRenamedFolder(string path, string oldFolder, string newFolder)
    {
        var prefix = Path.TrimEndingDirectorySeparator(oldFolder) + Path.DirectorySeparatorChar;
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? Path.Join(newFolder.AsSpan(), path.AsSpan(prefix.Length))
            : null;
    }
}
