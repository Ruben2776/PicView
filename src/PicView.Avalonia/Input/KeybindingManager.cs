using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Input;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Interfaces;
using PicView.Core.DebugTools;
using PicView.Core.Keybindings;

namespace PicView.Avalonia.Input;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class SourceGenerationContext : JsonSerializerContext;

public static class KeybindingManager
{
    public static Dictionary<KeyGesture, Func<ValueTask>>? CustomShortcuts { get; private set; }

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
        // Deserialize JSON into a dictionary of string keys and string values
        var keyValues = JsonSerializer.Deserialize(
                json, typeof(Dictionary<string, string>), SourceGenerationContext.Default)
            as Dictionary<string, string>;

        CustomShortcuts ??= new Dictionary<KeyGesture, Func<ValueTask>>();
        PopulateCustomShortcuts(keyValues);
    }

    public static async ValueTask UpdateKeyBindingsFile()
    {
        try
        {
            var json = JsonSerializer.Serialize(
                CustomShortcuts.ToDictionary(kvp => kvp.Key.ToString(),
                    kvp => GetFunctionNameByFunction(kvp.Value)), typeof(Dictionary<string, string>),
                SourceGenerationContext.Default).Replace("\\u002B", "+"); // Fix plus sign encoded to Unicode
            await KeybindingFunctions.SaveKeyBindingsFile(json).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(KeybindingManager), nameof(UpdateKeyBindingsFile), exception);
        }
    }

    private static void PopulateCustomShortcuts(Dictionary<string, string> keyValues)
    {
        foreach (var kvp in keyValues)
        {
            try
            {
                var gesture = KeyGesture.Parse(kvp.Key);
                if (gesture is null)
                {
                    continue;
                }

                var function = FunctionsMapper.GetFunctionByName(kvp.Value);
                // Add to the dictionary
                if (function != null)
                {
                    CustomShortcuts[gesture] = function;
                }
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
            CustomShortcuts = new Dictionary<KeyGesture, Func<ValueTask>>();
        }
        var defaultKeybindings = platformSpecificService.DefaultJsonKeyMap();
        var keyValues = JsonSerializer.Deserialize(
                defaultKeybindings, typeof(Dictionary<string, string>), SourceGenerationContext.Default)
            as Dictionary<string, string>;

        PopulateCustomShortcuts(keyValues);
    }
    
    private static string GetFunctionNameByFunction(Func<ValueTask> function) =>
        function == null ? "" : CustomShortcuts.FirstOrDefault(x => x.Value == function).Value.Method.Name;
}