using R3;

namespace PicView.Core.Navigation.Interfaces;

/// <summary>
/// Reports what happens to the files in a folder: created, changed, renamed and deleted.
/// </summary>
public interface IFolderEvents
{
    /// <summary>
    /// Starts watching <paramref name="directory"/>, and its subdirectories when
    /// <paramref name="includeSubdirectories"/>. Events may arrive on any thread until the watch is disposed.
    /// </summary>
    IFolderWatch Watch(string directory, bool includeSubdirectories);
}

/// <summary>
/// One folder being watched. Disposing it stops the events.
/// </summary>
public interface IFolderWatch : IDisposable
{
    Observable<FileSystemEventArgs> Created { get; }
    Observable<FileSystemEventArgs> Changed { get; }
    Observable<RenamedEventArgs> Renamed { get; }
    Observable<FileSystemEventArgs> Deleted { get; }

    /// <summary>
    /// The folder itself was deleted or moved into another folder, which raises no event for its files when it goes
    /// to the Recycle Bin.
    /// </summary>
    Observable<Unit> Gone { get; }

    /// <summary>
    /// The folder itself was renamed, to the path given. Its files' events still name the old path until the folder
    /// is watched under the new one.
    /// </summary>
    Observable<string> Moved { get; }
}
