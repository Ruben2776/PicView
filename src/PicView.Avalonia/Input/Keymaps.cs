using System.Text;
using PicView.Core.DebugTools;
using PicView.Core.Favorites;
using PicView.Core.IPlatform;
using PicView.Core.Keybindings;

namespace PicView.Avalonia.Input;

/// <summary>
/// Holds the application's current <see cref="Keymap"/> and owns reading and writing the keybindings file.
/// </summary>
public static class Keymaps
{
    private static Keymap _current = Keymap.Empty;

    public static Keymap Current => Volatile.Read(ref _current);

    public static void Replace(Keymap keymap) => Volatile.Write(ref _current, keymap);

    /// <summary>
    /// Loads the user's keybindings file, falling back to the platform defaults when it is missing or unreadable.
    /// </summary>
    public static void Load(IPlatformSpecificService? platform)
    {
        var bytes = KeybindingFunctions.SetKeyBindingsFileAndReturnBytes();
        Replace(Keymap.FromJson(bytes is null ? null : DecodeUtf8(bytes), DefaultJson(platform)));
    }

    public static void ResetToDefaults(IPlatformSpecificService? platform) =>
        Replace(Keymap.FromJson(null, DefaultJson(platform)));

    public static async ValueTask SaveAsync()
    {
        try
        {
            await KeybindingFunctions.SaveKeyBindingsFile(Current.ToJson()).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(Keymaps), nameof(SaveAsync), exception);
        }
    }

    /// <summary>
    /// Returns the owner of <paramref name="keybind"/>. Favorites are checked before actions.
    /// </summary>
    public static KeymapResolution Resolve(Keybind keybind)
    {
        var favoritePath = GetFavoritePath(keybind);
        return string.IsNullOrWhiteSpace(favoritePath)
            ? Current.Resolve(keybind)
            : KeymapResolution.Favorite(favoritePath);
    }

    /// <summary>
    /// Runs what <paramref name="resolution"/> points at. Does nothing until the mapper is available.
    /// </summary>
    public static async ValueTask ExecuteAsync(KeymapResolution resolution, IFunctionsMapper? mapper)
    {
        if (mapper is null)
        {
            return;
        }

        switch (resolution.Owner)
        {
            case KeymapOwner.Favorite:
                await mapper.OpenPathAsync(resolution.Value).ConfigureAwait(false);
                break;
            case KeymapOwner.Action when mapper.GetFunctionByName(resolution.Value) is { } function:
                await function.Invoke().ConfigureAwait(false);
                break;
        }
    }

    private static string? GetFavoritePath(Keybind keybind)
    {
        var entries = FavoritesManager.AllEntries;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (string.IsNullOrWhiteSpace(entry.HotKey))
            {
                continue;
            }

            var parsed = Keybind.Parse(entry.HotKey);
            if (parsed.HasValue && parsed.Value == keybind)
            {
                return entry.FilePath;
            }
        }

        return null;
    }

    private static string DefaultJson(IPlatformSpecificService? platform) =>
        platform?.DefaultJsonKeyMap() ?? "{}";

    private static string DecodeUtf8(byte[] bytes)
    {
        var preamble = Encoding.UTF8.Preamble;
        var span = bytes.AsSpan();
        return Encoding.UTF8.GetString(span.StartsWith(preamble) ? span[preamble.Length..] : span);
    }
}
