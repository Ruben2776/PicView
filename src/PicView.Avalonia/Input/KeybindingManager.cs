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

public static class KeybindingManager
{
    public static Dictionary<Keybind, string>? CustomShortcuts { get; private set; }

    public static async ValueTask LoadKeybindings(IPlatformSpecificService platformSpecificService)
    {
        var keybindings = await KeybindingFunctions.LoadKeyBindingsFile().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(keybindings))
        {
            SetDefaultKeybindings(platformSpecificService);
        }
        else
        {
            UpdateKeybindings(keybindings);
        }
    }

    private static void UpdateKeybindings(string json)
    {
        var keyValues = JsonSerializer.Deserialize(
                json, typeof(Dictionary<string, string>), SourceGenerationContext.Default)
            as Dictionary<string, string>;

        CustomShortcuts ??= new Dictionary<Keybind, string>();
        if (keyValues != null)
        {
            PopulateCustomShortcuts(keyValues);
        }
    }

    public static async ValueTask UpdateKeyBindingsFile()
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
            DebugHelper.LogDebug(nameof(KeybindingManager), nameof(UpdateKeyBindingsFile), exception);
        }
    }

    public static void PopulateCustomShortcuts(Dictionary<string, string> keyValues)
    {
        foreach (var kvp in keyValues)
        {
            try
            {
                // Use KeyGesture.Parse to correctly resolve Avalonia key name aliases
                // (e.g. "Esc" → Key.Escape, "Enter" → Key.Return, "Add", "Scroll", etc.)
                // then convert to a Keybind so lookups via new Keybind(e.Key, e.KeyModifiers) match.
                var gesture = KeyGesture.Parse(kvp.Key);
                if (gesture.Key is Key.None || kvp.Value is null)
                {
                    continue;
                }

                var keybind = new Keybind(gesture.Key, gesture.KeyModifiers);
                CustomShortcuts[keybind] = kvp.Value;
            }
            catch (Exception exception)
            {
                DebugHelper.LogDebug(nameof(KeybindingManager), nameof(PopulateCustomShortcuts), exception);
            }
        }
    }

    public static void SetDefaultKeybindings(IPlatformSpecificService platformSpecificService)
    {
        if (CustomShortcuts is not null)
        {
            CustomShortcuts.Clear();
        }
        else
        {
            CustomShortcuts = new Dictionary<Keybind, string>();
        }

        var defaultKeybindings = platformSpecificService.DefaultJsonKeyMap();

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
    public static Dictionary<Keybind, string>? GetDefaultShortcuts(IPlatformSpecificService platformSpecificService)
    {
        var defaultJson = platformSpecificService.DefaultJsonKeyMap();
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
    public static bool AreKeybindsDefault(IPlatformSpecificService platformSpecificService)
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