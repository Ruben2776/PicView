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
            : _favoritesConfiguration?.CorrectPath ?? string.Empty).Replace("/", "\\");

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
    ///     Adds a folder entry to the favorites, evicting any duplicate Keybind across other entries.
    /// </summary>
    public static async Task AddAsync(string filePath, string keybind = "")
    {
        if (_entries is null)
        {
            Initialize();
        }

        var normalizedPath = NormalizeDirectoryPath(filePath);
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return;
        }

        var normalizedKeybind = keybind?.Trim() ?? string.Empty;
        EvictDuplicateKeybind(normalizedPath, normalizedKeybind);

        var existingIndex = _entries!.FindIndex(x => string.Equals(x.FilePath, normalizedPath, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            _entries[existingIndex] = new FavoriteEntry(normalizedPath, normalizedKeybind);
        }
        else
        {
            _entries.Add(new FavoriteEntry(normalizedPath, normalizedKeybind));
        }

        await SaveToFileAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Adds an entry to the favorites.
    /// </summary>
    public static Task AddAsync(FavoriteEntry entry) => AddAsync(entry.FilePath, entry.Keybind);

    /// <summary>
    ///     Sets the Keybind for an existing favorite entry and evicts any duplicate Keybind across other entries.
    /// </summary>
    public static void SetKeybind(string filePath, string keybind)
    {
        if (_entries is null)
        {
            Initialize();
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var targetPath = NormalizeDirectoryPath(filePath) ?? Path.TrimEndingDirectorySeparator(filePath.Trim());
        var existingIndex = _entries.FindIndex(x => string.Equals(x.FilePath, targetPath, StringComparison.Ordinal));
        if (existingIndex < 0)
        {
            return;
        }

        var normalizedKeybind = keybind?.Trim() ?? string.Empty;
        EvictDuplicateKeybind(targetPath, normalizedKeybind);
        _entries[existingIndex] = new FavoriteEntry(_entries[existingIndex].FilePath, normalizedKeybind);
    }

    /// <summary>
    ///     Removes a specific entry from favorites.
    /// </summary>
    public static async Task<bool> RemoveAsync(string path)
    {
        if (_entries is null || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var targetPath = NormalizeDirectoryPath(path) ?? Path.TrimEndingDirectorySeparator(path.Trim());
        var index = _entries.FindIndex(e => string.Equals(e.FilePath, targetPath, StringComparison.Ordinal));
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

        var targetPath = NormalizeDirectoryPath(path) ?? Path.TrimEndingDirectorySeparator(path.Trim());
        return _entries.Exists(e => string.Equals(e.FilePath, targetPath, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Saves the favorites to the favorites file, evicting duplicate Keybinds first.
    /// </summary>
    public static async Task SaveToFileAsync()
    {
        try
        {
            DeduplicateKeybinds();

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

    private static void EvictDuplicateKeybind(string targetFilePath, string keybind)
    {
        if (_entries is null || string.IsNullOrWhiteSpace(keybind))
        {
            return;
        }

        for (var i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (!string.Equals(entry.FilePath, targetFilePath, StringComparison.Ordinal) &&
                string.Equals(entry.Keybind, keybind, StringComparison.Ordinal))
            {
                _entries[i] = new FavoriteEntry(entry.FilePath, string.Empty);
            }
        }
    }

    private static void DeduplicateKeybinds()
    {
        if (_entries is not { Count: > 1 })
        {
            return;
        }

        var seenKeybinds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (string.IsNullOrWhiteSpace(entry.Keybind))
            {
                continue;
            }

            if (!seenKeybinds.Add(entry.Keybind))
            {
                _entries[i] = new FavoriteEntry(entry.FilePath, string.Empty);
            }
        }
    }

    /// <summary>
    ///     Loads the favorites from the specified favorites file path.
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
            if (entries.Entries is not null && _entries is not null)
            {
                foreach (var entry in entries.Entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.FilePath))
                    {
                        continue;
                    }

                    var normalized = NormalizeDirectoryPath(entry.FilePath)
                                     ?? Path.TrimEndingDirectorySeparator(entry.FilePath.Trim());
                    _entries.Add(new FavoriteEntry(normalized, entry.Keybind));
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FavoritesManager), nameof(LoadFromFile), ex);
        }
    }
}
