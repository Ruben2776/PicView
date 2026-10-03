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

public class KeyBindingsService
{
    public Dictionary<Keybind, string>? CustomShortcuts { get; set; }

    public void LoadKeybindings(IPlatformSpecificService? platform)
    {
        var keybindingsBytes = KeybindingFunctions.SetKeyBindingsFileAndReturnBytes();
        if (keybindingsBytes == null)
        {
            SetDefaultKeybindings(platform);
        }
        else
        {
            UpdateKeybindings(keybindingsBytes);
        }
    }

    public void UpdateKeybindings(byte[] bytes)
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

    public async ValueTask UpdateKeyBindingsFile()
    {
        if (CustomShortcuts == null)
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
            DebugHelper.LogDebug(nameof(KeyBindingsService), nameof(UpdateKeyBindingsFile), exception);
        }
    }

    private Keybind? ParseToKeybind(KeyValuePair<string, string> kvp)
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
            DebugHelper.LogDebug(nameof(KeyBindingsService), nameof(PopulateCustomShortcuts), exception);
        }

        return null;
    }

    public void PopulateCustomShortcuts(Dictionary<string, string> keyValues)
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

    public void SetDefaultKeybindings(IPlatformSpecificService platform)
    {
        if (CustomShortcuts is not null)
        {
            CustomShortcuts.Clear();
        }
        else
        {
            CustomShortcuts = new Dictionary<Keybind, string>();
        }

        var defaultKeybindings = platform.DefaultJsonKeyMap();

        if (JsonSerializer.Deserialize(
                defaultKeybindings, typeof(Dictionary<string, string>), SourceGenerationContext.Default)
            is Dictionary<string, string> keyValues)
        {
            PopulateCustomShortcuts(keyValues);
        }
    }

    public string? GetActionName(Keybind keybind) =>
        CustomShortcuts?.GetValueOrDefault(keybind);

    /// <summary>
    /// Builds and returns a dictionary of default keybindings for the current platform.
    /// </summary>
    public Dictionary<Keybind, string>? GetDefaultShortcuts(IPlatformSpecificService platform)
    {
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

    /// <summary>
    /// Checks whether the current custom shortcuts match the platform defaults exactly.
    /// </summary>
    public bool AreKeybindsDefault(IPlatformSpecificService platformSpecificService)
    {
        if (CustomShortcuts is null)
        {
            return true;
        }

        var defaults = GetDefaultShortcuts(platformSpecificService);
        if (defaults is null)
        {
            return CustomShortcuts.Count == 0;
        }

        if (CustomShortcuts.Count != defaults.Count)
        {
            return false;
        }

        foreach (var kvp in defaults)
        {
            if (!CustomShortcuts.TryGetValue(kvp.Key, out var value) ||
                !string.Equals(value, kvp.Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public static Dictionary<string, List<Keybind>> GetDefaultsByFunction(CoreViewModel core)
    {
        var defaultsByFunction = new Dictionary<string, List<Keybind>>(StringComparer.OrdinalIgnoreCase);
        var defaults = KeybindingManager.GetDefaultShortcuts(core.PlatformService);

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
