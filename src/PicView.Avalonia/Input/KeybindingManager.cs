using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.Keybindings;

namespace PicView.Avalonia.Input;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class SourceGenerationContext : JsonSerializerContext;

/// <summary>
/// Default keybinding lookups for the keybindings view. Live keybindings are held by <see cref="Keymaps"/>.
/// </summary>
public static class KeybindingManager
{
    private static Keybind? ParseToKeybind(KeyValuePair<string, string> kvp)
    {
        try
        {
            if (kvp.Value is null)
            {
                return null;
            }
            var keybind = Keybind.Parse(kvp.Key);
            if (!keybind.HasValue)
            {
                return null;
            }

            var value = keybind.Value;
            if (string.Equals(kvp.Key, nameof(MouseButton.Middle), StringComparison.OrdinalIgnoreCase)) 
            {
                return new Keybind(MouseButton.Middle, value.Modifiers);
            }
            if (string.Equals(kvp.Key, nameof(MouseButton.XButton1), StringComparison.OrdinalIgnoreCase))
            {
                return new Keybind(MouseButton.XButton1, value.Modifiers);
            }
            if (string.Equals(kvp.Key, nameof(MouseButton.XButton2), StringComparison.OrdinalIgnoreCase))
            {
                return new Keybind(MouseButton.XButton2, value.Modifiers);
            }
            if (string.Equals(kvp.Key, nameof(MouseButton.WheelUp), StringComparison.OrdinalIgnoreCase)) 
            {
                return new Keybind(MouseButton.WheelUp, value.Modifiers);
            }
            if (string.Equals(kvp.Key, nameof(MouseButton.WheelDown), StringComparison.OrdinalIgnoreCase)) 
            {
                return new Keybind(MouseButton.WheelDown, value.Modifiers);
            }

            return value;
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(KeybindingManager), nameof(ParseToKeybind), exception);
        }

        return null;
    }

    /// <summary>
    /// Builds and returns a dictionary of default keybindings for the current platform.
    /// </summary>
    public static Dictionary<Keybind, string>? GetDefaultShortcuts(IPlatformSpecificService? platform)
    {
        if (platform is null) return null;
        
        var defaultJson = platform.DefaultJsonKeyMap();
        if (JsonSerializer.Deserialize(
                defaultJson, typeof(Dictionary<string, string>), SourceGenerationContext.Default)
            is not Dictionary<string, string> keyValues)
        {
            return null;
        }

        var defaults = new Dictionary<Keybind, string>();
        foreach (var kvp in keyValues)
        {
            var parsedKey = ParseToKeybind(kvp);
            if (parsedKey.HasValue)
            {
                defaults[parsedKey.Value] = KeybindCategoryDefinition.Canonicalize(kvp.Value);
            }
        }

        return defaults;
    }
}
