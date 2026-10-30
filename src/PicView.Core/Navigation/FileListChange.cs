namespace PicView.Core.Navigation;

public enum FileListChangeKind
{
    /// <summary>
    /// A whole new list: another folder, or the same files sorted again. <see cref="FileListChange.Index"/> is the
    /// iterator's current index in it.
    /// </summary>
    Listed,

    /// <summary>
    /// <see cref="FileListChange.File"/> was put at <see cref="FileListChange.Index"/>.
    /// </summary>
    Inserted,

    /// <summary>
    /// The file at <see cref="FileListChange.Index"/> of the list before the change was taken out.
    /// </summary>
    Removed,

    /// <summary>
    /// The file at <see cref="FileListChange.Index"/> changed on disk; <see cref="FileListChange.File"/> is its new entry.
    /// </summary>
    Replaced,

    /// <summary>
    /// The file at <see cref="FileListChange.Index"/> is the same file in a renamed folder, now at
    /// <see cref="FileListChange.File"/>.
    /// </summary>
    Relocated
}

/// <summary>
/// One change to a tab's file list, as its iterator reports it.
/// </summary>
/// <param name="Files">The list after the change.</param>
/// <param name="Details">The changed file's details, read once for every tab, when the change has them.</param>
public readonly record struct FileListChange(
    FileListChangeKind Kind,
    IReadOnlyList<FileInfo> Files,
    int Index = -1,
    FileInfo? File = null,
    FileDetailsSource? Details = null);
