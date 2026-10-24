using System.Text;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.Keybindings;

namespace PicView.Avalonia.Input;

/// <summary>
/// Holds the application's current <see cref="Keymap"/> and owns reading and writing the keybindings file.
/// </summary>
public static class Keymaps
{
    /// <summary>
    /// The settings version that introduced mouse Keybinds.
    /// </summary>
    private const double MouseKeybindsVersion = 3.0;

    private static Keymap _current = Keymap.Empty;

    /// <summary>
    /// The current keymap.
    /// </summary>
    public static Keymap Current => 
        Volatile.Read(ref _current);

    public static void Replace(Keymap keymap) => 
        Volatile.Write(ref _current, keymap);

    /// <summary>
    /// Loads the user's keybindings file, falling back to the platform defaults when it is missing or unreadable.
    /// </summary>
    public static void Load(IPlatformSpecificService? platform)
    {
        var bytes = KeybindingFunctions.SetKeyBindingsFileAndReturnBytes();
        var keymap = Keymap.FromJson(bytes is null ? null : DecodeUtf8(bytes), DefaultJson(platform));
        if (bytes is not null && LoadedSettingsVersion < MouseKeybindsVersion)
        {
            // Keybindings files from before mouse Keybinds existed get the default ones
            keymap = keymap.WithDefaultMouseKeybinds(Defaults(platform));
        }

        Replace(keymap);
    }

    /// <summary>
    /// The platform's default keymap.
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

    #if DEBUG
    /// <inheritdoc cref="Keymap.Resolve(Keybind, InputMode)" />
    public static KeymapResolution Resolve(Keybind keybind, InputMode mode) => Current.Resolve(keybind, mode);
    #endif

    /// <summary>
    /// Makes <paramref name="keybind"/> the only Keybind of the favorite action for <paramref name="slot"/>,
    /// taking it from any other action, and saves the keybindings file. <c>null</c> unbinds the slot.
    /// </summary>
    public static async ValueTask SetFavoriteKeybindAsync(int slot, Keybind? keybind)
    {
        var action = KeybindCategoryDefinition.FavoriteAction(slot);
        Replace(Current.WithKeybinds(action, keybind is { } bound ? [bound] : []));
        await SaveAsync().ConfigureAwait(false);
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

        if (resolution.Owner is KeymapOwner.Action)
        {
            await mapper.Run(resolution.Value).ConfigureAwait(false);
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
}
