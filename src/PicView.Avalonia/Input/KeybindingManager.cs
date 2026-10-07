using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.Keybindings;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Input;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class SourceGenerationContext : JsonSerializerContext;

public static class KeybindingManager
{
    public static Dictionary<Keybind, string>? CustomShortcuts { get; private set; }

    public static void LoadKeybindings(IPlatformSpecificService? platform)
    {
        var keybindingsBytes = KeybindingFunctions.SetKeyBindingsFileAndReturnBytes();
        if (keybindingsBytes is null)
        {
            SetDefaultKeybindings(platform);
        }
        else
        {
            UpdateKeybindings(keybindingsBytes);
        }
    }

    public static void UpdateKeybindings(byte[] bytes)
    {
        var keyValues = JsonSerializer.Deserialize(
                bytes, typeof(Dictionary<string, string>), SourceGenerationContext.Default)
            as Dictionary<string, string>;

        CustomShortcuts ??= new Dictionary<Keybind, string>();
        if (keyValues != null)
        {
            PopulateCustomShortcuts(keyValues);
        }
    }

    public static async ValueTask UpdateKeyBindingsFile()
    {
        if (CustomShortcuts is null)
        {
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(
                CustomShortcuts.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value, StringComparer.OrdinalIgnoreCase),
                typeof(Dictionary<string, string>),
                SourceGenerationContext.Default).Replace("\\u002B", "+", StringComparison.Ordinal); // Fix plus sign encoded to Unicode

            await KeybindingFunctions.SaveKeyBindingsFile(json).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(KeybindingManager), nameof(UpdateKeyBindingsFile), exception);
        }
    }

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

            return value;
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(KeybindingManager), nameof(ParseToKeybind), exception);
        }

        return null;
    }

    public static void PopulateCustomShortcuts(Dictionary<string, string> keyValues)
    {
        CustomShortcuts ??= new Dictionary<Keybind, string>(keyValues.Count);
        foreach (var kvp in keyValues)
        {
            var parsedKey = ParseToKeybind(kvp);
            if (parsedKey.HasValue)
            {
                CustomShortcuts[parsedKey.Value] = kvp.Value;
            }
        }
    }

    public static void SetDefaultKeybindings(IPlatformSpecificService? platform)
    {
        if (CustomShortcuts is not null)
        {
            CustomShortcuts.Clear();
        }
        else
        {
            CustomShortcuts = new Dictionary<Keybind, string>();
        }

        if (platform is null) return;

        var defaultKeybindings = platform.DefaultJsonKeyMap();

        if (JsonSerializer.Deserialize(
                defaultKeybindings, typeof(Dictionary<string, string>), SourceGenerationContext.Default)
            is Dictionary<string, string> keyValues)
        {
            PopulateCustomShortcuts(keyValues);
        }
    }

    public static string? GetActionName(Keybind keybind) =>
        CustomShortcuts?.GetValueOrDefault(keybind);

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
                defaults[parsedKey.Value] = kvp.Value;
            }
        }

        return defaults;
    }

    public static Dictionary<string, List<Keybind>> GetDefaultsByFunction(CoreViewModel core)
    {
        var defaultsByFunction = new Dictionary<string, List<Keybind>>(StringComparer.OrdinalIgnoreCase);
        var defaults = GetDefaultShortcuts(core.PlatformService);

        if (defaults is null)
        {
            return defaultsByFunction;
        }

        foreach (var kvp in defaults)
        {
            if (!defaultsByFunction.TryGetValue(kvp.Value, out var list))
            {
                list = [];
                defaultsByFunction[kvp.Value] = list;
            }

            list.Add(kvp.Key);
        }

        return defaultsByFunction;
    }
}
