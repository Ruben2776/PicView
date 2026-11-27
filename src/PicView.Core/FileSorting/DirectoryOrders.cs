using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.FileSorting;

/// <summary>
/// The File Order of each Listed Directory, shared by every tab that lists it, in any Main Window. A directory has an
/// order while a tab lists it; one no tab lists has the Default File Order.
/// </summary>
public sealed class DirectoryOrders
{
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
