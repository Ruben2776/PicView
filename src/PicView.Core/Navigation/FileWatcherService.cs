using System.Collections.Concurrent;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.Navigation;

public class FileWatcherService(
    Func<string, string, int> stringComparer,
    SharedImageCache cache,
    IThumbnailCache? thumbnailCache = null,
    IFolderEvents? folderEvents = null)
    : IFileWatcherService, IDisposable
{
    private readonly IFolderEvents _folderEvents = folderEvents ?? new FileSystemFolderEvents();
    private readonly SharedImageCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly Lock _lock = new();
    private readonly Func<string, string, int> _stringComparer = stringComparer ?? throw new ArgumentNullException(nameof(stringComparer));

    // Maps Directory Path -> (Watch, Subscribers)
    private readonly
        ConcurrentDictionary<string, (IFolderWatch Watch, IDisposable Subscription,
            List<WeakReference<TabViewModel>> Subscribers)> _watchers = new(StringComparer.OrdinalIgnoreCase);

    public void Watch(TabViewModel tab, string? directory = null)
    {
        if (tab?.ImageIterator is null)
        {
            return;
        }

        if (tab.Model is null)
        {
            return;
        }
        
        if (string.IsNullOrEmpty(directory))
        {
            var fileInfo = tab.Model?.FileInfo;
            if (fileInfo is null || string.IsNullOrEmpty(fileInfo.DirectoryName))
            {
                return;
            }

            directory = fileInfo.DirectoryName;
        }

        lock (_lock)
        {
            // If we are already watching this directory, just add the subscriber
            if (_watchers.TryGetValue(directory, out var entry))
            {
                // Remove dead references first
                entry.Subscribers.RemoveAll(wr => !wr.TryGetTarget(out _));

                // Add if not exists
                if (!entry.Subscribers.Exists(wr => wr.TryGetTarget(out var t) && ReferenceEquals(t, tab)))
                {
                    entry.Subscribers.Add(new WeakReference<TabViewModel>(tab));
                }

                return;
            }

            var (watch, subscription) = StartWatch(directory);
            _watchers[directory] = (watch, subscription, [new WeakReference<TabViewModel>(tab)]);
        }
    }

    /// <summary>
    /// Starts every folder watch again with the current Include Subdirectories setting, keeping the tabs watching it.
    /// </summary>
    public void RestartWatches()
    {
        lock (_lock)
        {
            foreach (var directory in _watchers.Keys.ToArray())
            {
                var (watch, subscription, subscribers) = _watchers[directory];
                subscription.Dispose();
                watch.Dispose();

                var (newWatch, newSubscription) = StartWatch(directory);
                _watchers[directory] = (newWatch, newSubscription, subscribers);
            }
        }
    }

    private (IFolderWatch Watch, IDisposable Subscription) StartWatch(string directory)
    {
        var watch = _folderEvents.Watch(directory, Settings.Sorting.IncludeSubDirectories);

        // AwaitOperation.Sequential ensures we don't process two file events for the same folder at the exact same time, 
        // which protects the Integrity of the 'files' list and the CurrentIndex.

        // Each event goes to every tab watching the folder when it arrives, not only the tab that started the watch
        var fileCreatedSub = watch.Created.SubscribeAwait(async (e, ct) =>
                await HandleAsync(directory, FolderChangeKind.Created, e.FullPath, null, ct).ConfigureAwait(false),
            DebugHelper.LogError(nameof(FileWatcherService), nameof(FolderChangeKind.Created)));

        var fileDeletedSub = watch.Deleted.SubscribeAwait(async (e, ct) =>
                await HandleAsync(directory, FolderChangeKind.Deleted, e.FullPath, null, ct).ConfigureAwait(false),
            DebugHelper.LogError(nameof(FileWatcherService), nameof(FolderChangeKind.Deleted)));

        var fileRenamedSub = watch.Renamed.SubscribeAwait(async (e, ct) =>
                await HandleAsync(directory, FolderChangeKind.Renamed, e.FullPath, e.OldFullPath, ct).ConfigureAwait(false),
            DebugHelper.LogError(nameof(FileWatcherService), nameof(FolderChangeKind.Renamed)));

        var fileChangedSub = watch.Changed.SubscribeAwait(async (e, ct) =>
                await HandleAsync(directory, FolderChangeKind.Changed, e.FullPath, null, ct).ConfigureAwait(false),
            DebugHelper.LogError(nameof(FileWatcherService), nameof(FolderChangeKind.Changed)));

        return (watch, Disposable.Combine(fileCreatedSub, fileDeletedSub, fileRenamedSub, fileChangedSub));
    }

    /// <summary>
    /// Handles one file event: forgets the file's stale image and thumbnail, then lets each tab still watching
    /// <paramref name="directory"/> follow it, one tab at a time, so a failure in one tab does not keep it from the
    /// others. The file's details are read at most once, for whichever tabs need them.
    /// </summary>
    private async ValueTask HandleAsync(string directory, FolderChangeKind kind, string fullPath, string? oldFullPath,
        CancellationToken ct)
    {
        if (!fullPath.IsSupported())
        {
            return;
        }

        var stalePath = kind is FolderChangeKind.Renamed ? oldFullPath ?? fullPath : fullPath;
        thumbnailCache?.Remove(stalePath);
        _cache.Invalidate(stalePath);

        var file = new FileInfo(fullPath);
        var change = new FolderChange(kind, file, oldFullPath,
            new FileDetailsSource(file, waitUntilReady: kind is not FolderChangeKind.Deleted), _stringComparer);

        TabViewModel[] subscribers;
        lock (_lock)
        {
            if (!_watchers.TryGetValue(directory, out var entry))
            {
                return;
            }
            subscribers = entry.Subscribers
                .Select(static reference => reference.TryGetTarget(out var tab) ? tab : null)
                .OfType<TabViewModel>()
                .ToArray();
        }

        foreach (var tab in subscribers)
        {
            try
            {
                await tab.OnFolderChangedAsync(change, ct).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                DebugHelper.LogDebug(nameof(FileWatcherService), kind.ToString(), exception);
            }
        }

        if (kind is FolderChangeKind.Renamed && oldFullPath is not null)
        {
            FileHistoryManager.Rename(oldFullPath, fullPath);
        }
    }

    public void Unwatch(TabViewModel tab)
    {
        lock (_lock)
        {
            // iterate all watchers to find the tab
            var keysToRemove = new List<string>();

            foreach (var kvp in _watchers)
            {
                var (watch, subscription, subscribers) = kvp.Value;

                // Remove the tab
                subscribers.RemoveAll(wr => !wr.TryGetTarget(out var t) || ReferenceEquals(t, tab));

                if (subscribers.Count != 0)
                {
                    continue;
                }

                // Dispose the R3 subscription and the folder watch
                subscription.Dispose();
                watch.Dispose();
                keysToRemove.Add(kvp.Key);
            }

            foreach (var key in keysToRemove)
            {
                _watchers.TryRemove(key, out _);
            }
        }
    }

    #region IDispose

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var (_, (watch, subscription, _)) in _watchers)
            {
                subscription.Dispose();
                watch.Dispose();
            }

            _watchers.Clear();
        }

        GC.SuppressFinalize(this);
    }

    #endregion
}