using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.FileSorting;

/// <summary>
/// The File Order of each Listed Directory, shared by every tab that lists it, in any Main Window. A directory has an
/// order while a tab lists it; one no tab lists has the Default File Order.
/// </summary>
/// <param name="fileManager">The order the platform's file manager shows a folder in; null where it is not asked.</param>
/// <param name="askLimit">How long a Load waits for the file manager's answer; half a second by default.</param>
public sealed class DirectoryOrders(IFolderOrderSource? fileManager = null, TimeSpan? askLimit = null)
{
    private readonly TimeSpan _askLimit = askLimit ?? TimeSpan.FromMilliseconds(500);

    // An ask that outran the limit and has not come back: while the file manager hangs, it is not asked again, so
    // one thread waits on it and not one per Load
    private Task? _unanswered;

    private readonly Lock _gate = new();
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Subject<Unit> _changed = new();

    private sealed class Entry(FileOrder order)
    {
        public FileOrder Order = order;
        public readonly List<WeakReference<TabViewModel>> Tabs = [];
    }

    /// <summary>
    /// Publishes when the order of any directory, or the Default File Order, may have changed. On the thread that
    /// changed it.
    /// </summary>
    public Observable<Unit> Changed => _changed;

    /// <summary>
    /// The Default File Order: the order a directory starts with, which the settings file keeps.
    /// </summary>
    public static FileOrder Default => FileOrder.FromAppSettings(Settings);

    /// <summary>
    /// The name a directory, or an archive file, is listed under: its full path without a trailing separator. Null
    /// for no path, or one that is not a path.
    /// </summary>
    public static string? KeyOf(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    /// <summary>
    /// The order of <paramref name="directory"/>: its own while a tab lists it, else the Default File Order.
    /// </summary>
    public FileOrder OrderOf(string? directory)
    {
        if (KeyOf(directory) is not { } key)
        {
            return Default;
        }

        lock (_gate)
        {
            return _entries.TryGetValue(key, out var entry) ? entry.Order : Default;
        }
    }

    /// <summary>
    /// Records that <paramref name="tab"/> lists <paramref name="directory"/>, and no longer
    /// <paramref name="previous"/>. A directory no tab listed starts with <paramref name="carried"/>, or the Default
    /// File Order.
    /// </summary>
    /// <returns>The name the directory is listed under, which the tab leaves it by.</returns>
    internal string? Enter(TabViewModel tab, string? directory, string? previous, FileOrder? carried = null)
    {
        var key = KeyOf(directory);
        lock (_gate)
        {
            if (previous is not null && !string.Equals(previous, key, StringComparison.OrdinalIgnoreCase))
            {
                Remove(tab, previous);
            }

            if (key is null)
            {
                return null;
            }

            if (!_entries.TryGetValue(key, out var entry))
            {
                entry = new Entry(carried ?? Default);
                _entries[key] = entry;
            }

            if (!Holds(entry, tab))
            {
                entry.Tabs.Add(new WeakReference<TabViewModel>(tab));
            }
        }

        _changed.OnNext(Unit.Default);
        return key;
    }

    /// <summary>
    /// Records that <paramref name="tab"/> no longer lists <paramref name="directory"/>, which loses its order with
    /// its last tab.
    /// </summary>
    internal void Leave(TabViewModel tab, string? directory)
    {
        if (directory is null)
        {
            return;
        }

        lock (_gate)
        {
            Remove(tab, directory);
        }

        _changed.OnNext(Unit.Default);
    }

    /// <summary>
    /// Sorts <paramref name="directory"/> by <paramref name="order"/>, which also becomes the Default File Order: every
    /// tab that lists the directory sorts its files again, and no tab of another directory does.
    /// </summary>
    /// <param name="reshuffle">Whether files already in a Random order are shuffled again.</param>
    /// <returns>A task that ends once every tab listing the directory has sorted its files again.</returns>
    public async ValueTask SortAsync(string? directory, FileOrder order, bool reshuffle = false)
    {
        order.StoreAsDefault(Settings);
        await SetAsync(directory, order, reshuffle).ConfigureAwait(false);
    }

    /// <summary>
    /// Gives <paramref name="directory"/> the <paramref name="order"/> without changing the Default File Order, as
    /// when it is taken from the platform's file manager. A directory no tab lists keeps none.
    /// </summary>
    /// <returns>A task that ends once every tab listing the directory has sorted its files again.</returns>
    public async ValueTask SetAsync(string? directory, FileOrder order, bool reshuffle = false)
    {
        TabViewModel[] tabs = [];
        if (KeyOf(directory) is { } key)
        {
            lock (_gate)
            {
                if (_entries.TryGetValue(key, out var entry))
                {
                    entry.Order = order;
                    tabs = Live(entry);
                }
            }
        }

        _changed.OnNext(Unit.Default);
        // Each starts sorting as it is asked, so the tabs sort side by side and the wait is for the slowest
        var sorting = new Task[tabs.Length];
        for (var i = 0; i < tabs.Length; i++)
        {
            sorting[i] = tabs[i].ResortAsync(reshuffle);
        }

        await Task.WhenAll(sorting).ConfigureAwait(false);
    }

    /// <summary>
    /// Gives <paramref name="directory"/> the order the file manager shows it in now, when following is on and it
    /// shows the folder in an order a File Order gives (ADR-0050): the tabs that list the directory start sorting
    /// their files again, and the Default File Order is not touched. Waits no longer than the ask limit.
    /// </summary>
    /// <returns>The order taken from the file manager, which a directory no tab lists yet starts with through
    /// <see cref="Enter"/>; null when nothing was taken, and the directory keeps the order it has.</returns>
    public ValueTask<FileOrder?> FollowAsync(string? directory, CancellationToken ct = default) =>
        Settings.Sorting.FollowExplorerSorting ? TakeAsync(directory, ct) : ValueTask.FromResult<FileOrder?>(null);

    /// <summary>
    /// Asks the file manager for every directory tabs list now, as when following is turned on. Archives are not
    /// asked for.
    /// </summary>
    public async ValueTask FollowAllAsync(CancellationToken ct = default)
    {
        foreach (var directory in Listed())
        {
            if (Directory.Exists(directory))
            {
                await TakeAsync(directory, ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Whether the file manager answers: false while an ask that outran the limit has not come back.
    /// </summary>
    internal bool IsAnswering
    {
        get
        {
            lock (_gate)
            {
                return _unanswered is not { IsCompleted: false };
            }
        }
    }

    private async ValueTask<FileOrder?> TakeAsync(string? directory, CancellationToken ct)
    {
        if (fileManager is null || KeyOf(directory) is not { } key)
        {
            return null;
        }

        Task<FileOrder?> asking;
        lock (_gate)
        {
            if (_unanswered is { IsCompleted: false })
            {
                return null;
            }

            _unanswered = null;
            asking = Task.Run(() => fileManager.OrderOf(key), CancellationToken.None);
        }

        FileOrder? order;
        try
        {
            order = await asking.WaitAsync(_askLimit, ct).ConfigureAwait(false);
        }
        catch (TimeoutException e)
        {
            lock (_gate)
            {
                _unanswered = asking;
            }

            DebugHelper.LogDebug(nameof(DirectoryOrders), nameof(FollowAsync), e);
            return null;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            DebugHelper.LogDebug(nameof(DirectoryOrders), nameof(FollowAsync), e);
            return null;
        }

        if (order is not { } followed)
        {
            return null;
        }

        TabViewModel[] tabs = [];
        lock (_gate)
        {
            if (_entries.TryGetValue(key, out var entry) && entry.Order != followed)
            {
                entry.Order = followed;
                tabs = Live(entry);
            }
        }

        if (tabs.Length > 0)
        {
            _changed.OnNext(Unit.Default);
            // Not waited for: the Load that asked lists in this order itself, and the others sort beside it
            foreach (var tab in tabs)
            {
                _ = tab.ResortAsync();
            }
        }

        return followed;
    }

    /// <summary>
    /// The tabs that list <paramref name="directory"/> now.
    /// </summary>
    internal TabViewModel[] TabsListing(string? directory)
    {
        if (KeyOf(directory) is not { } key)
        {
            return [];
        }

        lock (_gate)
        {
            return _entries.TryGetValue(key, out var entry) ? Live(entry) : [];
        }
    }

    /// <summary>
    /// The directories tabs list now.
    /// </summary>
    public string[] Listed()
    {
        lock (_gate)
        {
            return [.. _entries.Keys];
        }
    }

    // Under the gate
    private void Remove(TabViewModel tab, string key)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            return;
        }

        entry.Tabs.RemoveAll(reference => !reference.TryGetTarget(out var listed) || ReferenceEquals(listed, tab));
        if (entry.Tabs.Count is 0)
        {
            _entries.Remove(key);
        }
    }

    private static bool Holds(Entry entry, TabViewModel tab)
    {
        foreach (var reference in entry.Tabs)
        {
            if (reference.TryGetTarget(out var listed) && ReferenceEquals(listed, tab))
            {
                return true;
            }
        }

        return false;
    }

    private static TabViewModel[] Live(Entry entry)
    {
        var tabs = new List<TabViewModel>(entry.Tabs.Count);
        foreach (var reference in entry.Tabs)
        {
            if (reference.TryGetTarget(out var tab))
            {
                tabs.Add(tab);
            }
        }

        return [.. tabs];
    }
}
