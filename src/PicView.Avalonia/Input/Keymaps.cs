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
    private const int FavoritesStale = int.MinValue;

    private static State _state = new(Keymap.Empty, FavoritesStale);

    /// <summary>
    /// The current keymap, including the Keybinds of the current favorites.
    /// </summary>
    public static Keymap Current
    {
        get
        {
            var state = Volatile.Read(ref _state);
            var favoritesVersion = FavoritesManager.Version;
            if (state.FavoritesVersion == favoritesVersion)
            {
                return state.Keymap;
            }

            var keymap = state.Keymap.WithFavorites(FavoritesManager.AllEntries);
            Interlocked.CompareExchange(ref _state, new State(keymap, favoritesVersion), state);
            return keymap;
        }
    }

    public static void Replace(Keymap keymap) => Volatile.Write(ref _state, new State(keymap, FavoritesStale));

    /// <summary>
    /// Loads the user's keybindings file, falling back to the platform defaults when it is missing or unreadable.
    /// </summary>
    public static void Load(IPlatformSpecificService? platform)
    {
        var bytes = KeybindingFunctions.SetKeyBindingsFileAndReturnBytes();
        Replace(Keymap.FromJson(bytes is null ? null : DecodeUtf8(bytes), DefaultJson(platform)));
    }

    public static void ResetToDefaults(IPlatformSpecificService? platform) =>
        Replace(Defaults(platform));

    /// <summary>
    /// The platform's default keymap, without favorites.
    /// </summary>
    public static Keymap Defaults(IPlatformSpecificService? platform) =>
        Keymap.FromJson(null, DefaultJson(platform));

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

    public static KeymapResolution Resolve(Keybind keybind) => Current.Resolve(keybind);

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

    private static string DefaultJson(IPlatformSpecificService? platform) =>
        platform?.DefaultJsonKeyMap() ?? "{}";

    private static string DecodeUtf8(byte[] bytes)
    {
        var preamble = Encoding.UTF8.Preamble;
        var span = bytes.AsSpan();
        return Encoding.UTF8.GetString(span.StartsWith(preamble) ? span[preamble.Length..] : span);
    }

    private sealed record State(Keymap Keymap, int FavoritesVersion);
}
