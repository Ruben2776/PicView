using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.Config.ConfigFileManagement;
using PicView.Core.DebugTools;

namespace PicView.Core.Favorites;

/// <summary>
///     JSON context for favorites serialization.
/// </summary>
[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(FavoritesEntries))]
[JsonSerializable(typeof(List<FavoriteEntry>))]
[JsonSerializable(typeof(FavoriteEntry))]
internal partial class FavoritesGenerationContext : JsonSerializerContext;

/// <summary>
///     Manages the list of favorite folders.
/// </summary>
public static class FavoritesManager
{
    /// <summary>
    ///     The number of favorite slots. Only favorites with a slot have an OpenFavorite action.
    /// </summary>
    public const int MaxSlots = 10;

    private static List<FavoriteEntry>? _entries;
    private static FavoritesConfiguration? _favoritesConfiguration;
    /// <summary>
    ///     Gets the number of entries in the favorites list.
    /// </summary>
    public static int Count => _entries?.Count ?? 0;

    /// <summary>
    ///     Gets all favorite entries.
    /// </summary>
    public static IReadOnlyList<FavoriteEntry> AllEntries => _entries?.AsReadOnly() ?? (IReadOnlyList<FavoriteEntry>)[];

    /// <summary>
    ///     Gets the favorites file path with normalized slashes.
    /// </summary>
    public static string CurrentFavoritesFile =>
        (_favoritesConfiguration?.TryGetCurrentUserConfigPath is { Length: > 0 } userPath
            ? userPath
            : _favoritesConfiguration?.CorrectPath ?? string.Empty).Replace('/', '\\');

    /// <summary>
    ///     Initializes the favorites by loading entries from the default favorites configuration file.
    /// </summary>
    public static void Initialize()
    {
        _favoritesConfiguration = new FavoritesConfiguration();
        _favoritesConfiguration.CorrectPath = ConfigFileManager.ResolveDefaultConfigPath(_favoritesConfiguration);
        _entries = [];
        LoadFromFile(_favoritesConfiguration.TryGetCurrentUserConfigPath);
    }

    /// <summary>
    ///     Initializes the favorites using an explicit configuration file path (local-substitutable seam for testing).
    /// </summary>
    internal static void Initialize(string configPath)
    {
        _favoritesConfiguration = new FavoritesConfiguration
        {
            CorrectPath = configPath
        };
        _entries = [];
        if (File.Exists(configPath))
        {
            LoadFromFile(configPath);
        }
    }

    /// <summary>
    ///     Normalizes a path into an existing directory path without trailing directory separators.
    ///     If a file path is provided, resolves its parent directory.
    ///     Returns <c>null</c> if the path is empty or the resolved directory does not exist on disk.
    /// </summary>
    public static string? NormalizeDirectoryPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var trimmed = path.Trim();
        try
        {
            if (Directory.Exists(trimmed))
            {
                return Path.TrimEndingDirectorySeparator(Path.GetFullPath(trimmed));
            }

            if (File.Exists(trimmed))
            {
                var parentDir = Path.GetDirectoryName(Path.GetFullPath(trimmed));
                if (!string.IsNullOrWhiteSpace(parentDir) && Directory.Exists(parentDir))
                {
                    return Path.TrimEndingDirectorySeparator(parentDir);
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FavoritesManager), nameof(NormalizeDirectoryPath), ex);
        }

        return null;
    }

    /// <summary>
    ///     Adds a folder to the favorites in the lowest free slot, or without a slot when all are used.
    ///     A folder that is already a favorite is left as it is.
    /// </summary>
    /// <returns>The favorite's slot, or <c>null</c> when it has none or the path is not a folder.</returns>
    public static async Task<int?> AddAsync(string filePath)
    {
        if (_entries is null)
        {
            Initialize();
        }

        var normalizedPath = NormalizeDirectoryPath(filePath);
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return null;
        }

        var existingIndex = IndexOf(normalizedPath);
        if (existingIndex >= 0)
        {
            return _entries![existingIndex].Slot;
        }

        var slot = NextFreeSlot();
        _entries!.Add(new FavoriteEntry(normalizedPath, slot));
        await SaveToFileAsync().ConfigureAwait(false);
        return slot;
    }

    /// <summary>
    ///     Returns the lowest slot no favorite has, or <c>null</c> when all are used.
    /// </summary>
    public static int? NextFreeSlot()
    {
        for (var slot = 1; slot <= MaxSlots; slot++)
        {
            if (PathForSlot(slot) is null)
            {
                return slot;
            }
        }

        return null;
    }

    /// <summary>
    ///     Returns the folder of the favorite in <paramref name="slot"/>, or <c>null</c> when the slot is free.
    /// </summary>
    public static string? PathForSlot(int slot)
    {
        if (_entries is null)
        {
            return null;
        }

        foreach (var entry in _entries)
        {
            if (entry.Slot == slot)
            {
                return entry.FilePath;
            }
        }

        return null;
    }

    /// <summary>
    ///     Returns the slot of the favorite at <paramref name="path"/>, or <c>null</c> when it has none
    ///     or is not a favorite.
    /// </summary>
    public static int? SlotOf(string? path)
    {
        if (_entries is null || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var index = IndexOf(NormalizeDirectoryPath(path) ?? Path.TrimEndingDirectorySeparator(path.Trim()));
        return index >= 0 ? _entries[index].Slot : null;
    }

    /// <summary>
    ///     Removes a specific entry from favorites. Its slot stays free until a favorite is added.
    /// </summary>
    public static async Task<bool> RemoveAsync(string path)
    {
        if (_entries is null || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var index = IndexOf(NormalizeDirectoryPath(path) ?? Path.TrimEndingDirectorySeparator(path.Trim()));
        if (index < 0)
        {
            return false;
        }

        _entries.RemoveAt(index);
        await SaveToFileAsync().ConfigureAwait(false);
        return true;
    }

    /// <summary>
    ///     Clears all favorites entries.
    /// </summary>
    public static async Task ClearAsync()
    {
        _entries?.Clear();
        await SaveToFileAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Checks if a folder path is in the favorites.
    /// </summary>
    public static bool Contains(string path)
    {
        if (_entries is null)
        {
            Initialize();
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return IndexOf(NormalizeDirectoryPath(path) ?? Path.TrimEndingDirectorySeparator(path.Trim())) >= 0;
    }

    /// <summary>
    ///     Saves the favorites to the favorites file.
    /// </summary>
    public static async Task SaveToFileAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_favoritesConfiguration?.CorrectPath))
            {
                _favoritesConfiguration ??= new FavoritesConfiguration();
                _favoritesConfiguration.CorrectPath = ConfigFileManager.ResolveDefaultConfigPath(_favoritesConfiguration);
            }

            var favoritesEntries = new FavoritesEntries
            {
                Entries = _entries ?? []
            };
            _favoritesConfiguration.CorrectPath =
                await ConfigFileManager.SaveConfigFileAndReturnPathAsync(_favoritesConfiguration,
                _favoritesConfiguration.CorrectPath, favoritesEntries, typeof(FavoritesEntries), FavoritesGenerationContext.Default)
                    .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FavoritesManager), nameof(SaveToFileAsync), ex);
        }
    }

    private static int IndexOf(string normalizedPath) =>
        _entries?.FindIndex(e => string.Equals(e.FilePath, normalizedPath, StringComparison.Ordinal)) ?? -1;

    /// <summary>
    ///     Loads the favorites from the specified favorites file path. Slots outside 1..<see cref="MaxSlots"/>
    ///     or already taken by an earlier entry are cleared.
    /// </summary>
    private static void LoadFromFile(string? configPath)
    {
        try
        {
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
            {
                return;
            }

            var bytes = File.ReadAllBytes(configPath);

            if (JsonSerializer.Deserialize(bytes, typeof(FavoritesEntries),
                    FavoritesGenerationContext.Default)
                is not FavoritesEntries entries)
            {
                DebugHelper.LogDebug(nameof(FavoritesManager), nameof(LoadFromFile), "Failed to deserialize favorites");
                return;
            }

            _entries?.Clear();
            if (entries.Entries is null || _entries is null)
            {
                return;
            }

            var usedSlots = new HashSet<int>();
            foreach (var entry in entries.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.FilePath))
                {
                    continue;
                }

                var normalized = NormalizeDirectoryPath(entry.FilePath)
                                 ?? Path.TrimEndingDirectorySeparator(entry.FilePath.Trim());
                var slot = entry.Slot;
                if (slot is { } number && (number < 1 || number > MaxSlots || !usedSlots.Add(number)))
                {
                    DebugHelper.LogDebug(nameof(FavoritesManager), nameof(LoadFromFile),
                        $"Clearing invalid slot {number} of '{normalized}'");
                    slot = null;
                }

                _entries.Add(new FavoriteEntry(normalized, slot));
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FavoritesManager), nameof(LoadFromFile), ex);
        }
    }
}
