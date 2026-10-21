using PicView.Core.Navigation.Interfaces;
using R3;

namespace PicView.Core.Navigation;

/// <summary>
/// Watches folders on disk with a <see cref="FileSystemWatcher"/> each.
/// </summary>
public sealed class FileSystemFolderEvents : IFolderEvents
{
    public IFolderWatch Watch(string directory, bool includeSubdirectories) =>
        new FolderWatch(new FileSystemWatcher(directory)
        {
            EnableRaisingEvents = true,
            Filter = "*.*",
            IncludeSubdirectories = includeSubdirectories,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        });

    private sealed class FolderWatch(FileSystemWatcher watcher) : IFolderWatch
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

        public void Dispose() => watcher.Dispose();
    }
}
