using System.Runtime.InteropServices;
using PicView.Core.Models;

namespace PicView.Core.Navigation;

/// <summary>
/// How <see cref="TabLoad.LoadAsync"/> loads a source, beyond the source itself.
/// </summary>
/// <param name="RootDirectory">
/// For a file, the directory to list files from instead of the file's own, as when browsing subdirectories from the
/// remembered start-up directory. Used only when the file is inside it; otherwise the file's own folder is listed.
/// </param>
/// <param name="ArchiveEntry">
/// For an archive, the entry to open it at, which is extracted and shown before the rest. Null, or an entry the archive
/// doesn't have, opens it at its first entry.
/// </param>
/// <param name="Relist">
/// Lists the shown file's folder again without showing anything, keeping the shown file at its place, as after
/// toggling subdirectories.
/// </param>
/// <param name="Model">
/// For a file, its already decoded model, which is shown as it is instead of being decoded again.
/// </param>
/// <param name="Files">
/// For a file, the files around it that the caller already listed, such as the startup path's, which the Load browses
/// instead of listing them again. <paramref name="RootDirectory"/> is then not listed.
/// </param>
/// <param name="SecondaryModel">
/// Side by side, with <paramref name="Model"/>, the already decoded model of the file beside it, which shows with it
/// instead of being fetched.
/// </param>
/// <param name="ListedWith">
/// The ordering <paramref name="Files"/> were listed with, which the tab's file list keeps, so the tab sorts them again
/// when their directory's order changed since. Null for files in the order of the directory they are of.
/// </param>
/// <param name="ListedDirectory">
/// The directory <paramref name="Files"/> were listed from, which becomes the tab's Listed Directory. Null for the
/// file's own folder.
/// </param>
/// <param name="FollowedOrder">
/// The order the caller took from the platform's file manager for <paramref name="ListedDirectory"/> before it listed
/// <paramref name="Files"/>, which the directory starts with when no tab lists it yet. Null when it took none.
/// </param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct LoadOptions(
    string? RootDirectory = null,
    string? ArchiveEntry = null,
    bool Relist = false,
    ImageModel? Model = null,
    IReadOnlyList<FileInfo>? Files = null,
    ImageModel? SecondaryModel = null,
    FileSorting.FileOrdering? ListedWith = null,
    string? ListedDirectory = null,
    FileSorting.FileOrder? FollowedOrder = null);

/// <summary>
/// How a <see cref="TabLoad"/> ended.
/// </summary>
public enum LoadOutcome
{
    /// <summary>The source shows in the tab.</summary>
    Loaded,

    /// <summary>A newer load of the tab began, or the tab closed, before this one finished, so it stopped.</summary>
    Superseded,

    /// <summary>The source could not be loaded.</summary>
    Failed
}
