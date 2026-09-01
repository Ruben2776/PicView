using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.Config.ConfigFileManagement;
using PicView.Core.DebugTools;
using ZLinq;
using ZLinq.Linq;

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
    private static List<Entry>? _entries;
    private static FileHistoryConfiguration? _fileHistoryConfiguration;

    /// <summary>
    ///     Gets the number of entries in the file history.
    /// </summary>
    public static int Count => _entries.Count;

    /// <summary>
    ///     Gets or sets whether the history is sorted in descending order.
    /// </summary>
    public static bool IsSortingDescending { get; set; }

    /// <summary>
    ///     Gets all history entries.
    /// </summary>
    public static IReadOnlyList<Entry> AllEntries => _entries.AsReadOnly();

    public static ValueEnumerable<FromEnumerable<Entry>, Entry> PinnedEntries => _entries.Where(e => e.IsPinned).AsValueEnumerable();
    public static ValueEnumerable<FromEnumerable<Entry>, Entry> UnPinnedEntries => _entries.Where(e => !e.IsPinned).AsValueEnumerable();

    /// <summary>
    /// Gets or sets the index of the current file entry in the history.
    /// The setter clamps the value between -1 and the total count of entries minus one.
    /// A value of -1 indicates no valid current entry.
    /// </summary>
    public static int CurrentIndex
    {
        get;
        private set => field = Math.Clamp(value, -1, Count - 1);
    } = -1;

    /// <summary>
    ///     Indicates whether there is a previous entry available in history (older entry).
    /// </summary>
    public static bool HasPrevious => CurrentIndex > 0;

    /// <summary>
    ///     Indicates whether there is a next entry available in history (newer entry).
    /// </summary>
    public static bool HasNext => CurrentIndex < Count - 1 && Count > 0;

    /// <summary>
    ///     Gets the current entry at the current index.
    /// </summary>
    public static string? CurrentEntry =>
        CurrentIndex >= 0 && CurrentIndex < Count ? _entries[CurrentIndex].Path : null;

    /// <summary>
    ///     Gets the file history file path with normalized slashes.
    /// </summary>
    public static string CurrentFileHistoryFile => _fileHistoryConfiguration?.TryGetCurrentUserConfigPath.Replace("/", "\\") ?? string.Empty;

    /// <summary>
    ///     Initializes the file history by loading entries from the history file.
    /// </summary>
    public static void Initialize()
    {
        _fileHistoryConfiguration = new FileHistoryConfiguration();
        _fileHistoryConfiguration.CorrectPath = ConfigFileManager.ResolveDefaultConfigPath(_fileHistoryConfiguration);
        _entries = new List<Entry>(FileHistoryConfiguration.MaxHistoryEntries);
        LoadFromFile();

        // Set the current index to the most recent entry.
        CurrentIndex = Count > 0 ? Count - 1 : -1;
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
        var entryIndex = _entries.FindIndex(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));
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

    /// <summary>
    ///     Adds an entry to the history.
    /// </summary>
    public static void Add(string path)
    {
        if (!Settings.Navigation.IsFileHistoryEnabled || _entries is null)
        {
            return;
        }

        var existingIndex = _entries.FindIndex(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            // If entry already exists, update the current index to point to it.
            CurrentIndex = existingIndex;
            return;
        }

        // Count unpinned entries
        var unpinnedCount = _entries.Count(e => !e.IsPinned);

        // If we'll exceed the maximum unpinned entries, remove the oldest unpinned entry
        if (unpinnedCount >= FileHistoryConfiguration.MaxHistoryEntries)
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].IsPinned)
                {
                    continue;
                }

                _entries.RemoveAt(i);

                // Adjust the current index since we removed an item.
                if (CurrentIndex > i)
                {
                    CurrentIndex--;
                }

                break;
            }
        }

        _entries.Add(new Entry { Path = path, IsPinned = false });
        CurrentIndex = _entries.Count - 1;
    }

    /// <summary>
    ///     Gets the next entry in history (newer entry).
    /// </summary>
    public static string? GetNextEntry()
    {
        if (!HasNext)
        {
            return null;
        }

        CurrentIndex++;
        return CurrentEntry;
    }

    /// <summary>
    ///     Gets the previous entry in history (older entry).
    /// </summary>
    public static string? GetPreviousEntry()
    {
        if (!HasPrevious)
        {
            return null;
        }

        CurrentIndex--;
        return CurrentEntry;
    }

    /// <summary>
    ///     Gets an entry at the specified index.
    /// </summary>
    public static Entry? GetEntry(int index)
    {
        if (index < 0 || index >= _entries.Count)
        {
            return null;
        }

        return _entries[index];
    }

    /// <summary>
    ///     Gets the first entry in history (oldest).
    /// </summary>
    public static string? GetFirstEntry() =>
        _entries.Count > 0 ? _entries[0].Path : null;

    /// <summary>
    ///     Gets the last entry in history (newest).
    /// </summary>
    public static string? GetLastEntry() =>
        _entries.Count > 0 ? _entries[^1].Path : null;

    /// <summary>
    ///     Tries to find an entry that matches or contains the given string.
    /// </summary>
    public static Entry? GetEntryByString(string searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString))
        {
            return null;
        }

        // First try exact match.
        var exactMatch = _entries.FirstOrDefault(e =>
            string.Equals(e.Path, searchString, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
        {
            return exactMatch;
        }

        // Then try contains.
        return _entries.Find(e => e.Path.Contains(searchString, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Clears all history entries.
    /// </summary>
    public static void Clear()
    {
        _entries.Clear();
        CurrentIndex = -1;
    }

    /// <summary>
    ///     Removes a specific entry from history.
    /// </summary>
    public static bool Remove(string path)
    {
        var index = _entries.FindIndex(e => string.Equals(e.Path, path, StringComparison.OrdinalIgnoreCase));
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

    /// <summary>
    ///     Renames a file in the history, replacing the old entry with the new one.
    /// </summary>
    public static void Rename(string oldName, string newName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            var entry = GetEntryByString(oldName);
            if (string.IsNullOrWhiteSpace(entry?.Path) || _entries.TrueForAll(x => !string.Equals(x.Path, entry?.Path, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var index = _entries.FindIndex(x => string.Equals(x.Path, entry.Path, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                _entries[index].Path = newName;
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
            if (string.IsNullOrWhiteSpace(_fileHistoryConfiguration.CorrectPath))
            {
                _fileHistoryConfiguration.CorrectPath = ConfigFileManager.ResolveDefaultConfigPath(_fileHistoryConfiguration);
            }

            // Create a new sorted list with pinned entries first (max 5), then unpinned entries (max MaxHistoryEntries)
            var sortedEntries = new List<Entry>();

            // Add pinned entries (max 5)
            sortedEntries.AddRange(_entries.Where(e => e.IsPinned).Take(FileHistoryConfiguration.MaxPinnedEntries));
            // Add unpinned entries (max MaxHistoryEntries)
            sortedEntries.AddRange(_entries.Where(e => !e.IsPinned)
                .Take(FileHistoryConfiguration.MaxHistoryEntries));

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
    ///     Loads the history from the history file.
    /// </summary>
    private static void LoadFromFile()
    {
        if (!Settings.Navigation.IsFileHistoryEnabled)
        {
            return;
        }
        
        try
        {
            var bytes = File.ReadAllBytes(_fileHistoryConfiguration.TryGetCurrentUserConfigPath);

            if (JsonSerializer.Deserialize(bytes, typeof(FileHistoryEntries),
                    FileHistoryGenerationContext.Default)
                is not FileHistoryEntries entries)
            {
                DebugHelper.LogDebug(nameof(FileHistoryManager), nameof(LoadFromFile), "Failed to deserialize settings");
                return;
            }

            IsSortingDescending = entries.IsSortingDescending;
            _entries.Clear();
            foreach (var entry in entries.Entries)
            {
                _entries.Add(entry);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileHistoryManager), nameof(LoadFromFile), ex);
        }
    }
}