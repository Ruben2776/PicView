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
[JsonSerializable(typeof(LastSession))]
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

    // Held while loading. EnsureInitialized checks for a load under it, so callers of that load once; Initialize itself always loads
    private static readonly Lock InitLock = new();
    private static string? _lastFile;
    private static string? _startUpDirectory;
    private static LastSession? _lastSession;

    /// <summary>
    ///     Gets or sets the Last File. <see cref="PicView.Core.Navigation.LastFile" /> is the one writer. The setter is
    ///     ignored while file history is off. It runs on load threads for every image shown, so it is a flag check and a
    ///     volatile write: no lock, no allocation.
    /// </summary>
    public static string? LastFile
    {
        get => Volatile.Read(ref _lastFile);
        set
        {
            if (!Settings.Navigation.IsFileHistoryEnabled)
            {
                return;
            }

            Volatile.Write(ref _lastFile, value);
        }
    }

    /// <summary>
    ///     Gets or sets the root directory that sub-directory browsing started from. Ignored while file history is off.
    /// </summary>
    public static string? StartUpDirectory
    {
        get => Volatile.Read(ref _startUpDirectory);
        set
        {
            if (!Settings.Navigation.IsFileHistoryEnabled)
            {
                return;
            }

            Volatile.Write(ref _startUpDirectory, value);
        }
    }

    /// <summary>
    ///     Gets the Last Session, or null when none is recorded.
    /// </summary>
    public static LastSession? LastSession
    {
        get
        {
            lock (Lock)
            {
                return _lastSession;
            }
        }
    }

    /// <summary>
    ///     Records the Last Session. Ignored while file history is off. A session without sources is stored as null.
    /// </summary>
    public static void RecordSession(LastSession? session)
    {
        if (!Settings.Navigation.IsFileHistoryEnabled)
        {
            return;
        }

        lock (Lock)
        {
            // A copy, so the caller's list is not shared live
            _lastSession = session is { Sources.Count: > 0 }
                ? new LastSession { Sources = [..session.Sources], ActiveIndex = session.ActiveIndex }
                : null;
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
        lock (InitLock)
        {
            _fileHistoryConfiguration = new FileHistoryConfiguration();
            Load(_fileHistoryConfiguration.FilePath);
        }
    }

    /// <summary>
    ///     Initializes the file history only if nothing is loaded yet. Safe to call from several threads: the load runs
    ///     once, and a second call does nothing.
    /// </summary>
    public static void EnsureInitialized()
    {
        lock (InitLock)
        {
            lock (Lock)
            {
                if (_entries is not null)
                {
                    return;
                }
            }

            Initialize();
        }
    }

    /// <summary>
    ///     Initializes the file history using an explicit configuration file path (local-substitutable seam for testing).
    /// </summary>
    internal static void Initialize(string configPath)
    {
        lock (InitLock)
        {
            _fileHistoryConfiguration = new FileHistoryConfiguration
            {
                PathOverride = configPath
            };
            Load(configPath);
        }
    }

    // Callers hold InitLock
    private static void Load(string? configPath)
    {
        ReplaceEntries(LoadFromFile(configPath, out var migrated));
        if (migrated)
        {
            // The record is emptied by the next settings save, so the history file must hold them before it. Started
            // here, it takes the config gate ahead of any save made after the load
            _ = SaveToFileAsync();
        }
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
            _lastSession = null;
        }

        Volatile.Write(ref _lastFile, null);
        Volatile.Write(ref _startUpDirectory, null);
    }

    /// <summary>
    ///     Replaces the entries with those loaded from the file, then adds the paths that were added before the load
    ///     finished. The file is read before this, so the lock is not held for it.
    /// </summary>
    private static void ReplaceEntries(FileHistoryEntries? file)
    {
        var loaded = file?.Entries;
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

            // A value set before the load finished is newer than the file's, so the file only fills what is still empty
            _lastSession ??= file?.LastSession is { Sources.Count: > 0 } ? file.LastSession : null;
        }

        // Compare-exchange, so a launch file shown during the load is not overwritten
        Interlocked.CompareExchange(ref _lastFile, file?.LastFile, null);
        Interlocked.CompareExchange(ref _startUpDirectory, file?.StartUpDirectory, null);

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
            _lastSession = null;
        }

        Volatile.Write(ref _lastFile, null);
        Volatile.Write(ref _startUpDirectory, null);
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

        RenameMatching(path =>
            string.Equals(Path.TrimEndingDirectorySeparator(path), Path.TrimEndingDirectorySeparator(oldFolder),
                StringComparison.OrdinalIgnoreCase)
                ? newFolder
                : FolderChange.PathInRenamedFolder(path, oldFolder, newFolder));
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
            RenameSessionAndLastFile(newPathOf);

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

    // Callers hold the lock. The Last File and each source of the Last Session follow a rename by the same matching as the
    // entries
    private static void RenameSessionAndLastFile(Func<string, string?> newPathOf)
    {
        // Compare-exchange, so a newer value a load thread wrote since the read is not overwritten
        if (Volatile.Read(ref _lastFile) is { } lastFile && newPathOf(lastFile) is { } newLastFile)
        {
            Interlocked.CompareExchange(ref _lastFile, newLastFile, lastFile);
        }

        if (Volatile.Read(ref _startUpDirectory) is { } startUpDirectory &&
            newPathOf(startUpDirectory) is { } newStartUpDirectory)
        {
            Interlocked.CompareExchange(ref _startUpDirectory, newStartUpDirectory, startUpDirectory);
        }

        if (_lastSession is null)
        {
            return;
        }

        // Swapped for a new session, so a copy handed out earlier is not edited
        var sources = new List<string>(_lastSession.Sources.Count);
        foreach (var source in _lastSession.Sources)
        {
            sources.Add(newPathOf(source) ?? source);
        }

        _lastSession = new LastSession { Sources = sources, ActiveIndex = _lastSession.ActiveIndex };
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
            LastSession? session = null;

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

                if (_lastSession is { } lastSession)
                {
                    session = new LastSession { Sources = [..lastSession.Sources], ActiveIndex = lastSession.ActiveIndex };
                }
            }

            // Turning file history off after something was recorded must not leave it in the file
            var isHistoryOn = Settings.Navigation.IsFileHistoryEnabled;

            var historyEntries = new FileHistoryEntries
            {
                Entries = sortedEntries,
                IsSortingDescending = IsSortingDescending,
                LastFile = isHistoryOn ? LastFile : null,
                StartUpDirectory = isHistoryOn ? StartUpDirectory : null,
                LastSession = isHistoryOn ? session : null
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
    ///     Loads the history from the specified history file path. The Last File and start-up directory the settings
    ///     used to hold are migrated here: where the file has none, the record's is taken, and the record is emptied.
    /// </summary>
    /// <param name="migrated">Whether a value was taken from the record, which the history file does not hold yet.</param>
    /// <returns>What the file holds, or null when there is nothing to load.</returns>
    private static FileHistoryEntries? LoadFromFile(string? configPath, out bool migrated)
    {
        migrated = false;
        if (!Settings.Navigation.IsFileHistoryEnabled)
        {
            // Nothing is taken while history is off, but the record still drops them; the file is left alone
            Settings.StartUp.LastFile = null;
            Settings.StartUp.StartUpDirectory = null;
            return null;
        }

        FileHistoryEntries? entries = null;
        try
        {
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                var bytes = File.ReadAllBytes(configPath);

                entries = JsonSerializer.Deserialize(bytes, typeof(FileHistoryEntries),
                    FileHistoryGenerationContext.Default) as FileHistoryEntries;
                if (entries is null)
                {
                    DebugHelper.LogDebug(nameof(FileHistoryManager), nameof(LoadFromFile), "Failed to deserialize settings");
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileHistoryManager), nameof(LoadFromFile), ex);
        }

        if (entries is not null)
        {
            IsSortingDescending = entries.IsSortingDescending;
        }

        // Settings 3.1 kept these in the settings record. Take them once, then leave them out of the next settings save
        var legacyLastFile = Settings.StartUp.LastFile;
        var legacyDirectory = Settings.StartUp.StartUpDirectory;
        if (!string.IsNullOrWhiteSpace(legacyLastFile) && string.IsNullOrWhiteSpace(entries?.LastFile))
        {
            entries ??= new FileHistoryEntries();
            entries.LastFile = legacyLastFile;
            migrated = true;
        }

        if (!string.IsNullOrWhiteSpace(legacyDirectory) && string.IsNullOrWhiteSpace(entries?.StartUpDirectory))
        {
            entries ??= new FileHistoryEntries();
            entries.StartUpDirectory = legacyDirectory;
            migrated = true;
        }

        Settings.StartUp.LastFile = null;
        Settings.StartUp.StartUpDirectory = null;
        return entries;
    }
}
