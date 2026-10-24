using PicView.Core.Navigation.Interfaces;
using R3;

namespace PicView.Core.Navigation;

/// <summary>
/// Watches folders on disk with a <see cref="FileSystemWatcher"/> each, and another on the folder's parent that sees
/// the folder itself go.
/// </summary>
public sealed class FileSystemFolderEvents : IFolderEvents
{
    public IFolderWatch Watch(string directory, bool includeSubdirectories)
    {
        var watcher = new FileSystemWatcher(directory)
        {
            EnableRaisingEvents = true,
            Filter = "*.*",
            IncludeSubdirectories = includeSubdirectories,
            // Subfolders are browsed too, so a deleted one reports its files gone
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size |
                           (includeSubdirectories ? NotifyFilters.DirectoryName : 0)
        };
        var folderPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        return new FolderWatch(watcher, WatchParent(directory), folderPath);
    }

    /// <summary>
    /// Watches the parent of <paramref name="directory"/> for the folder itself, or nothing for a drive's root.
    /// </summary>
    private static FileSystemWatcher? WatchParent(string directory)
    {
        var folder = new DirectoryInfo(directory);
        if (folder.Parent is not { } parent)
        {
            return null;
        }

        return new FileSystemWatcher(parent.FullName)
        {
            Filter = folder.Name,
            NotifyFilter = NotifyFilters.DirectoryName,
            EnableRaisingEvents = true
        };
    }

    private sealed class FolderWatch(FileSystemWatcher watcher, FileSystemWatcher? parentWatcher, string folderPath)
        : IFolderWatch
    {
        // Observable.FromEvent bridges the watcher's .NET events to R3
        public Observable<FileSystemEventArgs> Created { get; } =
            Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                h => (_, e) => h(e),
                h => watcher.Created += h,
                h => watcher.Created -= h);

        public Observable<FileSystemEventArgs> Changed { get; } =
            Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                h => (_, e) => h(e),
                h => watcher.Changed += h,
                h => watcher.Changed -= h);

        public Observable<RenamedEventArgs> Renamed { get; } =
            Observable.FromEvent<RenamedEventHandler, RenamedEventArgs>(
                h => (_, e) => h(e),
                h => watcher.Renamed += h,
                h => watcher.Renamed -= h);

        public Observable<FileSystemEventArgs> Deleted { get; } =
            Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                h => (_, e) => h(e),
                h => watcher.Deleted += h,
                h => watcher.Deleted -= h);

        // The parent's filter matches the folder's name, so a rename away from it or a deletion is the folder going
        public Observable<Unit> Gone { get; } = parentWatcher is null
            ? Observable.Never<Unit>()
            : Observable.Merge(
                Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                    h => (_, e) => h(e),
                    h => parentWatcher.Deleted += h,
                    h => parentWatcher.Deleted -= h).Select(static _ => Unit.Default),
                Observable.FromEvent<RenamedEventHandler, RenamedEventArgs>(
                        h => (_, e) => h(e),
                        h => parentWatcher.Renamed += h,
                        h => parentWatcher.Renamed -= h)
                    .Where(e => string.Equals(Path.TrimEndingDirectorySeparator(e.OldFullPath), folderPath,
                        StringComparison.OrdinalIgnoreCase))
                    .Select(static _ => Unit.Default));

        public void Dispose()
        {
            watcher.Dispose();
            parentWatcher?.Dispose();
        }
    }
}
