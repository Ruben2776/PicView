using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.Config.ConfigFileManagement;
using PicView.Core.DebugTools;

namespace PicView.Core.FileHistory;

/// <summary>
///     JSON context for favorites serialization.
/// </summary>
[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(FavoritesEntries))]
[JsonSerializable(typeof(List<FavoriteEntry>))]
[JsonSerializable(typeof(FavoriteEntry))]
internal partial class FavoritesGenerationContext : JsonSerializerContext;

/// <summary>
///     Manages the list of favorite files.
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
    /// Gets the favorites file path with normalized slashes.
    /// </summary>
    public static string CurrentFavoritesFile => _favoritesConfiguration?.TryGetCurrentUserConfigPath.Replace("/", "\\") ?? string.Empty;

    /// <summary>
    ///     Initializes the favorites by loading entries from the favorites file.
    /// </summary>
    public static void Initialize()
    {
        _favoritesConfiguration = new FavoritesConfiguration();
        _favoritesConfiguration.CorrectPath = ConfigFileManager.ResolveDefaultConfigPath(_favoritesConfiguration);
        _entries = new List<FavoriteEntry>();
        LoadFromFile();
    }

    /// <summary>
    ///     Adds an entry to the favorites.
    /// </summary>
    public static async Task AddAsync(string filePath, string hotKey = "")
    {
        if (_entries is null)
        {
            Initialize();
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var existingIndex = _entries.FindIndex(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            _entries[existingIndex] = new FavoriteEntry(filePath, hotKey);
        }
        else
        {
            _entries.Add(new FavoriteEntry(filePath, hotKey));
        }

        await SaveToFileAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Adds an entry to the favorites.
    /// </summary>
    public static Task AddAsync(FavoriteEntry entry) => AddAsync(entry.FilePath, entry.HotKey);

    /// <summary>
    ///     Removes a specific entry from favorites.
    /// </summary>
    public static async Task<bool> RemoveAsync(string path)
    {
        if (_entries is null)
        {
            return false;
        }

        var index = _entries.FindIndex(e => string.Equals(e.FilePath, path, StringComparison.OrdinalIgnoreCase));
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
    ///     Checks if a path is in the favorites.
    /// </summary>
    public static bool Contains(string path)
    {
        if (_entries is null || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return _entries.Exists(e => string.Equals(e.FilePath, path, StringComparison.Ordinal));
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

    /// <summary>
    ///     Loads the favorites from the favorites file.
    /// </summary>
    private static void LoadFromFile()
    {
        try
        {
            var configPath = _favoritesConfiguration?.TryGetCurrentUserConfigPath;
            if (string.IsNullOrEmpty(configPath))
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
            if (entries.Entries != null)
            {
                _entries?.AddRange(entries.Entries);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FavoritesManager), nameof(LoadFromFile), ex);
        }
    }
}
