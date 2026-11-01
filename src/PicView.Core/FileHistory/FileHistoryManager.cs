using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.Config.ConfigFileManagement;
using PicView.Core.DebugTools;

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
    private static readonly Lock _lock = new();
    private static List<Entry>? _entries;
    private static FileHistoryConfiguration? _fileHistoryConfiguration;

    // The paths Add was given before the first load finished, the newest last. App Start shows the launch file before the
    // history file is read, so without these it would never reach the history. They are added when the load finishes.
    private static readonly List<string> _earlyPaths = [];

    // The list holds this many entries at most, so an older early path would be evicted by the load anyway
    private const int MaxEarlyPaths = FileHistoryConfiguration.MaxHistoryEntries + FileHistoryConfiguration.MaxPinnedEntries;

    /// <summary>
    ///     Gets the number of entries in the file history.
    /// </summary>
    public static int Count
    {
        get
        {
            lock (_lock)
            {
                return _entries?.Count ?? 0;
            }
        }
    }

    /// <summary>
    ///     Gets or sets whether the history is sorted in descending order.
    /// </summary>
    public static bool IsSortingDescending { get; set; }

    /// <summary>
    ///     Gets a copy of all history entries, the one viewed longest ago first.
    /// </summary>
    public static Entry[] AllEntries
    {
        get
        {
            lock (_lock)
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
            lock (_lock)
            {
                return _entries?.Where(e => e.IsPinned).ToArray() ?? [];
            }
        }
    }

    /// <summary>
    /// Gets or sets the index of the current file entry in the history.
    /// The setter clamps the value between -1 and the total count of entries minus one.
    /// A value of -1 indicates no valid current entry.
    /// </summary>
    public static int CurrentIndex
    {
        get;
        // Its callers hold the lock, so it reads _entries rather than taking the lock again through Count
        private set => field = Math.Clamp(value, -1, (_entries?.Count ?? 0) - 1);
    } = -1;

    /// <summary>
    ///     Indicates whether there is a previous entry available in history (older entry).
    /// </summary>
    public static bool HasPrevious => CurrentIndex > 0;

    /// <summary>
    ///     Indicates whether there is a next entry available in history (newer entry).
    /// </summary>
    public static bool HasNext
    {
        get
        {
            lock (_lock)
            {
                return CurrentIndex < Count - 1 && Count > 0;
            }
        }
    }

    /// <summary>
    ///     Gets the current entry at the current index.
    /// </summary>
    public static string? CurrentEntry
    {
        get
        {
            lock (_lock)
            {
                return _entries is not null && CurrentIndex >= 0 && CurrentIndex < Count ? _entries[CurrentIndex].Path : null;
            }
        }
    }

    /// <summary>
    ///     Gets the file history file path with normalized slashes.
    /// </summary>
    public static string CurrentFileHistoryFile =>
        (_fileHistoryConfiguration?.TryGetCurrentUserConfigPath is { Length: > 0 } userPath
            ? userPath
            : _fileHistoryConfiguration?.CorrectPath ?? string.Empty).Replace("/", "\\");

    /// <summary>
    ///     Initializes the file history by loading entries from the history file. Paths added before the load finished go
    ///     after the loaded entries.
    /// </summary>
    public static void Initialize()
    {
        _fileHistoryConfiguration = new FileHistoryConfiguration();
        _fileHistoryConfiguration.CorrectPath = ConfigFileManager.ResolveDefaultConfigPath(_fileHistoryConfiguration);
        ReplaceEntries(LoadFromFile(_fileHistoryConfiguration.TryGetCurrentUserConfigPath));
    }

    /// <summary>
    ///     Initializes the file history using an explicit configuration file path (local-substitutable seam for testing).
    /// </summary>
    internal static void Initialize(string configPath)
    {
        _fileHistoryConfiguration = new FileHistoryConfiguration
        {
            CorrectPath = configPath
        };
        ReplaceEntries(LoadFromFile(configPath));
    }

    /// <summary>
    ///     Returns to the state before the first load: nothing is loaded and no path waits for it (local-substitutable
    ///     seam for testing).
    /// </summary>
    internal static void Unload()
    {
        lock (_lock)
        {
            _entries = null;
            _earlyPaths.Clear();
            CurrentIndex = -1;
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

        lock (_lock)
        {
            _entries = entries;

            // Set the current index to the most recent entry.
            CurrentIndex = entries.Count > 0 ? entries.Count - 1 : -1;

            // They were viewed after everything in the file, so they go last, by the same rules as any other path
            foreach (var path in _earlyPaths)
            {
                AddTo(entries, path);
            }

            _earlyPaths.Clear();
        }
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
        lock (_lock)
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

        lock (_lock)
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
                var index = i;
                if (!entry.IsPinned)
                {
                    // Entries are ordered by when each was last viewed, so this one goes last
                    entries.RemoveAt(i);
                    entries.Add(entry);
                    index = entries.Count - 1;
                }

                CurrentIndex = index;
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

        // If we'll exceed the maximum unpinned entries, remove the oldest unpinned entry
        if (unpinnedCount >= FileHistoryConfiguration.MaxHistoryEntries)
        {
            entries.RemoveAt(oldestUnpinned);
        }

        entries.Add(new Entry { Path = path, IsPinned = false });
        CurrentIndex = entries.Count - 1;
    }

    // Callers hold the lock. Only runs before the first load, so it may allocate
    private static void KeepUntilLoaded(string path)
    {
        // A path seen again moves to the newest end and keeps the spelling it had first, as an entry in the list does
        var kept = path;
        for (var i = 0; i < _earlyPaths.Count; i++)
        {
            if (string.Equals(_earlyPaths[i], path, StringComparison.OrdinalIgnoreCase))
            {
                kept = _earlyPaths[i];
                _earlyPaths.RemoveAt(i);
                break;
            }
        }

        _earlyPaths.Add(kept);
        if (_earlyPaths.Count > MaxEarlyPaths)
        {
            _earlyPaths.RemoveAt(0);
        }
    }

    /// <summary>
    ///     Gets the next entry in history (newer entry).
    /// </summary>
    public static string? GetNextEntry()
    {
        lock (_lock)
        {
            if (!HasNext)
            {
                return null;
            }

            CurrentIndex++;
            return CurrentEntry;
        }
    }

    /// <summary>
    ///     Gets the previous entry in history (older entry).
    /// </summary>
    public static string? GetPreviousEntry()
    {
        lock (_lock)
        {
            if (!HasPrevious)
            {
                return null;
            }

            CurrentIndex--;
            return CurrentEntry;
        }
    }

    /// <summary>
    ///     Gets an entry at the specified index.
    /// </summary>
    public static Entry? GetEntry(int index)
    {
        lock (_lock)
        {
            if (_entries is null || index < 0 || index >= _entries.Count)
            {
                return null;
            }

            return _entries[index];
        }
    }

    /// <summary>
    ///     Gets the first entry in history (oldest).
    /// </summary>
    public static string? GetFirstEntry()
    {
        lock (_lock)
        {
            return _entries is { Count: > 0 } ? _entries[0].Path : null;
        }
    }

    /// <summary>
    ///     Gets the last entry in history (newest).
    /// </summary>
    public static string? GetLastEntry()
    {
        lock (_lock)
        {
            return _entries is { Count: > 0 } ? _entries[^1].Path : null;
        }
    }

    /// <summary>
    ///     Tries to find an entry that matches or contains the given string.
    /// </summary>
    public static Entry? GetEntryByString(string searchString)
    {
        lock (_lock)
        {
            if (_entries is null || string.IsNullOrWhiteSpace(searchString))
            {
                return null;
            }

            // First try exact match.
            var exactMatch = _entries.FirstOrDefault(e =>
                string.Equals(e.Path, searchString, StringComparison.Ordinal));

            if (exactMatch != null)
            {
                return exactMatch;
            }

            // Then try contains.
            return _entries.Find(e => e.Path.Contains(searchString, StringComparison.Ordinal));
        }
    }

    /// <summary>
    ///     Clears all history entries.
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _entries?.Clear();
            CurrentIndex = -1;
        }
    }

    /// <summary>
    ///     Removes a specific entry from history.
    /// </summary>
    public static bool Remove(string path)
    {
        lock (_lock)
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

            // Adjust current index if necessary.
            if (index <= CurrentIndex)
            {
                CurrentIndex = Math.Max(-1, CurrentIndex - 1);
            }

            return true;
        }
    }

    // TODO: Rename the entries of the files in a renamed folder too. FileWatcherService only renames single files here,
    // so a folder rename (FolderChangeKind.FolderRenamed) leaves its files' entries under the old paths.
    /// <summary>
    ///     Renames a file in the history, replacing the old entry with the new one.
    /// </summary>
    public static void Rename(string oldName, string newName)
    {
        try
        {
            lock (_lock)
            {
                if (_entries is null || string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
                {
                    return;
                }

                var entry = GetEntryByString(oldName);
                if (string.IsNullOrWhiteSpace(entry?.Path) || _entries.TrueForAll(x => !string.Equals(x.Path, entry?.Path, StringComparison.Ordinal)))
                {
                    return;
                }

                var index = _entries.FindIndex(x => string.Equals(x.Path, entry.Path, StringComparison.Ordinal));
                if (index >= 0)
                {
                    _entries[index].Path = newName;
                }
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(FileHistoryManager), nameof(Rename), e);
        }
    }

    /// <summary>
    ///     Saves the history to the history file.
    /// </summary>
    public static async Task SaveToFileAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_fileHistoryConfiguration?.CorrectPath))
            {
                _fileHistoryConfiguration ??= new FileHistoryConfiguration();
                _fileHistoryConfiguration.CorrectPath = ConfigFileManager.ResolveDefaultConfigPath(_fileHistoryConfiguration);
            }

            // Create a new sorted list with pinned entries first (max 5), then unpinned entries (max MaxHistoryEntries)
            var sortedEntries = new List<Entry>();

            // Copy under the lock, so the file is saved without holding it
            lock (_lock)
            {
                var entries = _entries ?? [];

                // Add pinned entries (max 5)
                sortedEntries.AddRange(entries.Where(e => e.IsPinned).Take(FileHistoryConfiguration.MaxPinnedEntries));
                // Add unpinned entries (max MaxHistoryEntries)
                sortedEntries.AddRange(entries.Where(e => !e.IsPinned)
                    .Take(FileHistoryConfiguration.MaxHistoryEntries));
            }

            var historyEntries = new FileHistoryEntries
            {
                Entries = sortedEntries,
                IsSortingDescending = IsSortingDescending
            };
            _fileHistoryConfiguration.CorrectPath = 
                await ConfigFileManager.SaveConfigFileAndReturnPathAsync(_fileHistoryConfiguration,
                _fileHistoryConfiguration.CorrectPath, historyEntries, typeof(FileHistoryEntries), FileHistoryGenerationContext.Default)
                    .ConfigureAwait(false);
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