using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.Config.ConfigFileManagement;
using PicView.Core.DebugTools;
using PicView.Core.Navigation;

namespace PicView.Core.FileHistory;

/// <summary>
///     JSON context for file history serialization.
/// </summary>
[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(FileHistoryEntries))]
[JsonSerializable(typeof(List<Entry>))]
internal partial class FileHistoryGenerationContext : JsonSerializerContext;

/// <summary>
///     Manages the history of recently accessed files.
/// </summary>
public static class FileHistoryManager
{
    // Load threads call Add while the UI thread reads the lists. Held for in-memory work only, never across disk access or an await.
    private static readonly Lock Lock = new();
    private static List<Entry>? _entries;
    private static FileHistoryConfiguration? _fileHistoryConfiguration;

    // The paths Add was given before the first load finished, the newest last. App Start shows the launch file before the
    // history file is read, so without these it would never reach the history. They are added when the load finishes.
    private static readonly List<string> EarlyPaths = [];

    // The list holds this many entries at most, so an older early path would be evicted by the load anyway
    private const int MaxEarlyPaths = FileHistoryConfiguration.MaxHistoryEntries + FileHistoryConfiguration.MaxPinnedEntries;

    /// <summary>
    ///     Gets the number of entries in the file history.
    /// </summary>
    public static int Count
    {
        get
        {
            lock (Lock)
            {
                return _entries?.Count ?? 0;
            }
        }
    }

    private static int _version;

    /// <summary>
    ///     Goes up each time the entries, their order, their pins or the sort order change, so a view of the history can
    ///     tell whether it has to look at them again.
    /// </summary>
    public static int Version => Volatile.Read(ref _version);

    // Lock-free, so it needs no lock held and does not allocate on a load thread
    private static void Changed() => Interlocked.Increment(ref _version);

    /// <summary>
    ///     Gets or sets whether the history is sorted in descending order.
    /// </summary>
    public static bool IsSortingDescending
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            Changed();
        }
    }

    /// <summary>
    ///     Gets a copy of all history entries, the one viewed longest ago first.
    /// </summary>
    public static Entry[] AllEntries
    {
        get
        {
            lock (Lock)
            {
                return _entries?.ToArray() ?? [];
            }
        }
    }

    /// <summary>
    ///     Gets a copy of the pinned history entries, in the same order as <see cref="AllEntries" />.
    /// </summary>
    public static Entry[] PinnedEntries
    {
        get
        {
            lock (Lock)
            {
                return _entries?.Where(e => e.IsPinned).ToArray() ?? [];
            }
        }
    }

    /// <summary>
    ///     Gets the path of the file history file in the config folder in use, or an empty string before the file
    ///     history is initialized.
    /// </summary>
    public static string CurrentFileHistoryFile => _fileHistoryConfiguration?.FilePath ?? string.Empty;

    /// <summary>
    ///     Initializes the file history by loading entries from the history file. Paths added before the load finished go
    ///     after the loaded entries.
    /// </summary>
    public static void Initialize()
    {
        _fileHistoryConfiguration = new FileHistoryConfiguration();
        ReplaceEntries(LoadFromFile(_fileHistoryConfiguration.FilePath));
    }

    /// <summary>
    ///     Initializes the file history using an explicit configuration file path (local-substitutable seam for testing).
    /// </summary>
    internal static void Initialize(string configPath)
    {
        _fileHistoryConfiguration = new FileHistoryConfiguration
        {
            PathOverride = configPath
        };
        ReplaceEntries(LoadFromFile(configPath));
    }

    /// <summary>
    ///     Returns to the state before the first load: nothing is loaded and no path waits for it (local-substitutable
    ///     seam for testing).
    /// </summary>
    internal static void Unload()
    {
        lock (Lock)
        {
            _entries = null;
            EarlyPaths.Clear();
        }
    }

    /// <summary>
    ///     Replaces the entries with those loaded from the file, then adds the paths that were added before the load
    ///     finished. The file is read before this, so the lock is not held for it.
    /// </summary>
    private static void ReplaceEntries(List<Entry>? loaded)
    {
        var entries = new List<Entry>(FileHistoryConfiguration.MaxHistoryEntries);
        if (loaded is not null)
        {
            entries.AddRange(loaded);
        }

        lock (Lock)
        {
            _entries = entries;

            // They were viewed after everything in the file, so they go last, by the same rules as any other path
            foreach (var path in EarlyPaths)
            {
                AddTo(entries, path);
            }

            EarlyPaths.Clear();
        }

        Changed();
    }

    /// <summary>
    ///     Pins a file entry in history.
    /// </summary>
    public static void Pin(string path) =>
        SetPinnedState(path, true);

    /// <summary>
    ///     Unpins a file entry in history.
    /// </summary>
    public static void UnPin(string path) =>
        SetPinnedState(path, false);

    private static void SetPinnedState(string path, bool isPinned)
    {
        lock (Lock)
        {
            if (_entries is null || string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var entryIndex = _entries.FindIndex(x => string.Equals(x.Path, path, StringComparison.Ordinal));
            if (entryIndex < 0 || _entries[entryIndex].IsPinned == isPinned)
            {
                return;
            }

            if (isPinned && _entries.Count(e => e.IsPinned) >= FileHistoryConfiguration.MaxPinnedEntries)
            {
                // Unpin the oldest pinned entry to make room
                var oldestPinned = _entries.Where(e => e.IsPinned).OrderBy(e => _entries.IndexOf(e)).FirstOrDefault();
                if (oldestPinned != null)
                {
                    var oldestIndex = _entries.IndexOf(oldestPinned);
                    _entries[oldestIndex].IsPinned = false;
                }
            }

            _entries[entryIndex].IsPinned = isPinned;
            Changed();
        }
    }

    /// <summary>
    ///     Adds an entry to the history. One that is already listed moves to the newest end, unless it is pinned. Before
    ///     the first load has finished there is no list yet, so the path waits and is added when the load finishes.
    /// </summary>
    public static void Add(string path)
    {
        if (!Settings.Navigation.IsFileHistoryEnabled || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        lock (Lock)
        {
            if (_entries is null)
            {
                KeepUntilLoaded(path);
                return;
            }

            AddTo(_entries, path);
        }
    }

    // Callers hold the lock
    private static void AddTo(List<Entry> entries, string path)
    {
        // A tab calls this for every image it shows, so a path that is already listed must not allocate:
        // no LINQ or lambdas here. While looking for the path, count the unpinned entries and note the oldest.
        var unpinnedCount = 0;
        var oldestUnpinned = -1;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (string.Equals(entry.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                // Entries are ordered by when each was last viewed, so this one goes last. Paging through the newest
                // one again, as a reload does, changes nothing
                if (!entry.IsPinned && i != entries.Count - 1)
                {
                    entries.RemoveAt(i);
                    entries.Add(entry);
                    Changed();
                }

                return;
            }

            if (entry.IsPinned)
            {
                continue;
            }

            unpinnedCount++;
            if (oldestUnpinned < 0)
            {
                oldestUnpinned = i;
            }
        }

        // The oldest unpinned entries go until the new one fits under the cap. That can be more than one, since unpinning
        // an entry at the cap leaves one more unpinned entry than the cap
        while (unpinnedCount >= FileHistoryConfiguration.MaxHistoryEntries)
        {
            entries.RemoveAt(oldestUnpinned);
            unpinnedCount--;
            oldestUnpinned = IndexOfUnpinned(entries, oldestUnpinned);
        }

        entries.Add(new Entry { Path = path, IsPinned = false });
        Changed();
    }

    // Callers hold the lock. The index of the first unpinned entry from start on, else -1
    private static int IndexOfUnpinned(List<Entry> entries, int start)
    {
        for (var i = start; i < entries.Count; i++)
        {
            if (!entries[i].IsPinned)
            {
                return i;
            }
        }

        return -1;
    }

    // Callers hold the lock. Only runs before the first load, so it may allocate
    private static void KeepUntilLoaded(string path)
    {
        // A path seen again moves to the newest end and keeps the spelling it had first, as an entry in the list does
        var kept = path;
        for (var i = 0; i < EarlyPaths.Count; i++)
        {
            if (string.Equals(EarlyPaths[i], path, StringComparison.OrdinalIgnoreCase))
            {
                kept = EarlyPaths[i];
                EarlyPaths.RemoveAt(i);
                break;
            }
        }

        EarlyPaths.Add(kept);
        if (EarlyPaths.Count > MaxEarlyPaths)
        {
            EarlyPaths.RemoveAt(0);
        }
    }

    /// <summary>
    ///     Clears all history entries.
    /// </summary>
    public static void Clear()
    {
        lock (Lock)
        {
            _entries?.Clear();
        }

        Changed();
    }

    /// <summary>
    ///     Removes a specific entry from history.
    /// </summary>
    public static bool Remove(string path)
    {
        lock (Lock)
        {
            if (_entries is null || string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var index = _entries.FindIndex(e => string.Equals(e.Path, path, StringComparison.Ordinal));
            if (index < 0)
            {
                return false;
            }

            _entries.RemoveAt(index);
            Changed();
            return true;
        }
    }

    /// <summary>
    ///     Renames a file in the history. The entry keeps its place and its pin. If the new path is already listed, the two
    ///     entries become one, in the newer place and pinned if either was. Paths compare ignoring case, and a path that
    ///     only contains the old one is not the old one.
    /// </summary>
    public static void Rename(string oldPath, string newPath)
    {
        if (string.IsNullOrWhiteSpace(oldPath) || string.IsNullOrWhiteSpace(newPath))
        {
            return;
        }

        RenameMatching(path => string.Equals(path, oldPath, StringComparison.OrdinalIgnoreCase) ? newPath : null);
    }

    /// <summary>
    ///     Renames the entries of the files in a renamed folder, subfolders included, by the same rules as
    ///     <see cref="Rename" />.
    /// </summary>
    public static void RenameFolder(string oldFolder, string newFolder)
    {
        if (string.IsNullOrWhiteSpace(oldFolder) || string.IsNullOrWhiteSpace(newFolder))
        {
            return;
        }

        RenameMatching(path => FolderChange.PathInRenamedFolder(path, oldFolder, newFolder));
    }

    // newPathOf gives the path a listed path is renamed to, or null for one that stays. A rename never runs on a load, so
    // it may allocate
    private static void RenameMatching(Func<string, string?> newPathOf)
    {
        lock (Lock)
        {
            // Before the first load the paths added so far wait in their own list, which the load adds to the entries. If
            // they kept their old names, the load would undo the rename
            RenameEarlyPaths(newPathOf);

            if (_entries is null)
            {
                return;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                if (newPathOf(_entries[i].Path) is not { } newPath)
                {
                    continue;
                }

                Changed();
                var listed = IndexOfPath(_entries, newPath, i);
                if (listed < 0)
                {
                    _entries[i].Path = newPath;
                    continue;
                }

                // The new path is listed already, so the two entries become one: the one nearer the newest end stays,
                // pinned if either was
                var kept = Math.Max(i, listed);
                var dropped = Math.Min(i, listed);
                _entries[kept].Path = newPath;
                _entries[kept].IsPinned |= _entries[dropped].IsPinned;
                _entries.RemoveAt(dropped);

                // The entries after the dropped one moved down, so the loop looks at this index again
                i--;
            }
        }
    }

    // Callers hold the lock. If the new path waits as well, the load lists the two as one, as it does any path added twice
    private static void RenameEarlyPaths(Func<string, string?> newPathOf)
    {
        for (var i = 0; i < EarlyPaths.Count; i++)
        {
            if (newPathOf(EarlyPaths[i]) is { } newPath)
            {
                EarlyPaths[i] = newPath;
            }
        }
    }

    // Callers hold the lock. The index of the entry for the path, not counting the one at except, else -1
    private static int IndexOfPath(List<Entry> entries, string path, int except)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (i != except && string.Equals(entries[i].Path, path, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    ///     Saves the history to the history file.
    /// </summary>
    public static async Task SaveToFileAsync()
    {
        try
        {
            _fileHistoryConfiguration ??= new FileHistoryConfiguration();

            // Create a new sorted list with pinned entries first (max 5), then unpinned entries (max MaxHistoryEntries)
            var sortedEntries = new List<Entry>();

            // Copy under the lock, so the file is saved without holding it
            lock (Lock)
            {
                var entries = _entries ?? [];

                // Add pinned entries (max 5)
                sortedEntries.AddRange(entries.Where(e => e.IsPinned).Take(FileHistoryConfiguration.MaxPinnedEntries));
                // Add the newest unpinned entries (max MaxHistoryEntries), in their order. The list holds one more after
                // an entry at the cap is unpinned
                sortedEntries.AddRange(entries.Where(e => !e.IsPinned)
                    .TakeLast(FileHistoryConfiguration.MaxHistoryEntries));
            }

            var historyEntries = new FileHistoryEntries
            {
                Entries = sortedEntries,
                IsSortingDescending = IsSortingDescending
            };
            await ConfigFileManager.SaveAsync(_fileHistoryConfiguration, historyEntries, typeof(FileHistoryEntries),
                FileHistoryGenerationContext.Default).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileHistoryManager), nameof(SaveToFileAsync), ex);
        }
    }

    /// <summary>
    ///     Loads the history from the specified history file path.
    /// </summary>
    /// <returns>The entries in the file, or null when there are none to load.</returns>
    private static List<Entry>? LoadFromFile(string? configPath)
    {
        if (!Settings.Navigation.IsFileHistoryEnabled)
        {
            return null;
        }
        
        try
        {
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
            {
                return null;
            }

            var bytes = File.ReadAllBytes(configPath);

            if (JsonSerializer.Deserialize(bytes, typeof(FileHistoryEntries),
                    FileHistoryGenerationContext.Default)
                is not FileHistoryEntries entries)
            {
                DebugHelper.LogDebug(nameof(FileHistoryManager), nameof(LoadFromFile), "Failed to deserialize settings");
                return null;
            }

            IsSortingDescending = entries.IsSortingDescending;
            return entries.Entries;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileHistoryManager), nameof(LoadFromFile), ex);
            return null;
        }
    }
}