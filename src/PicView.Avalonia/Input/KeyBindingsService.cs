using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Input;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.Keybindings;

namespace PicView.Avalonia.Input;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class SourceGenerationContext : JsonSerializerContext;

public class KeyBindingsService(IPlatformSpecificService? specificService = null)
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

    public void PopulateCustomShortcuts(Dictionary<string, string> keyValues)
    {
        CustomShortcuts ??= new Dictionary<Keybind, string>(keyValues.Count);
        foreach (var kvp in keyValues)
        {
            try
            {
                var keybind = Keybind.Parse(kvp.Key);
                if (kvp.Value is null)
                {
                    continue;
                }
                if (string.Equals(kvp.Key, nameof(MouseButton.Middle), StringComparison.Ordinal)) 
                {
                    var mouseKeybind = new Keybind(MouseButton.Middle, keybind.Modifiers);
                    CustomShortcuts[mouseKeybind] = kvp.Value;
                    continue;
                }
                if (string.Equals(kvp.Key, nameof(MouseButton.XButton1), StringComparison.Ordinal))
                {
                    var mouseKeybind = new Keybind(MouseButton.XButton1, keybind.Modifiers);
                    CustomShortcuts[mouseKeybind] = kvp.Value;
                    continue;
                }
                if (string.Equals(kvp.Key, nameof(MouseButton.XButton2), StringComparison.Ordinal))
                {
                    var mouseKeybind = new Keybind(MouseButton.XButton2, keybind.Modifiers);
                    CustomShortcuts[mouseKeybind] = kvp.Value;
                    continue;
                }

                CustomShortcuts[keybind] = kvp.Value;
            }
            catch (Exception exception)
            {
                DebugHelper.LogDebug(nameof(KeyBindingsService), nameof(PopulateCustomShortcuts), exception);
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
    public Dictionary<Keybind, string>? GetDefaultShortcuts(IPlatformSpecificService? platformSpecificService = null)
    {
        var platform = platformSpecificService ?? specificService;
        if (platform is null)
        {
            return null;
        }

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
            try
            {
                var gesture = KeyGesture.Parse(kvp.Key);
                if (gesture.Key is Key.None || kvp.Value is null)
                {
                    continue;
                }

                var keybind = new Keybind(gesture.Key, gesture.KeyModifiers);
                defaults[keybind] = kvp.Value;
            }
            catch
            {
                // Skip invalid entries
            }
        }

        return defaults;
    }

    /// <summary>
    /// Checks whether the current custom shortcuts match the platform defaults exactly.
    /// </summary>
    public bool AreKeybindsDefault(IPlatformSpecificService? platformSpecificService = null)
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
}
