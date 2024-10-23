using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Input;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Core.Keybindings;

namespace PicView.Avalonia.Input;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class SourceGenerationContext : JsonSerializerContext;

public static class KeybindingManager
{
    // TODO move to an interface, use this as default for Windows and make a macOS default
    

    public static Dictionary<KeyGesture, Func<Task>>? CustomShortcuts { get; private set; }

    public static async Task LoadKeybindings(IPlatformSpecificService platformSpecificService)
    {
        try
        {
            var keybindings = await KeybindingFunctions.LoadKeyBindingsFile().ConfigureAwait(false);
            await UpdateKeybindings(keybindings).ConfigureAwait(false);
        }
        catch (Exception)
        {
            await SetDefaultKeybindings(platformSpecificService).ConfigureAwait(false);
        }
    }

    public static async Task UpdateKeybindings(string json)
    {
        // Deserialize JSON into a dictionary of string keys and string values
        var keyValues = JsonSerializer.Deserialize(
                json, typeof(Dictionary<string, string>), SourceGenerationContext.Default)
            as Dictionary<string, string>;

        CustomShortcuts ??= new Dictionary<KeyGesture, Func<Task>>();
        await Loop(keyValues).ConfigureAwait(false);
    }

    public static async Task UpdateKeyBindingsFile()
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
#if DEBUG
            Trace.WriteLine($"{nameof(UpdateKeyBindingsFile)} exception:\n{exception.Message}");
#endif
        }
    }

    private static async Task Loop(Dictionary<string, string> keyValues)
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
                var function = await FunctionsHelper.GetFunctionByName(kvp.Value).ConfigureAwait(false);
                // Add to the dictionary
                CustomShortcuts[gesture] = function;
            }
            catch (Exception exception)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(Loop)} exception:\n{exception.Message}");
#endif
            }
        }
    }

    internal static async Task SetDefaultKeybindings(IPlatformSpecificService platformSpecificService)
    {
        if (CustomShortcuts is not null)
        {
            CustomShortcuts.Clear();
        }
        else
        {
            CustomShortcuts = new Dictionary<KeyGesture, Func<Task>>();
        }
        var defaultKeybindings = platformSpecificService.DefaultJsonKeyMap();
        var keyValues = JsonSerializer.Deserialize(
                defaultKeybindings, typeof(Dictionary<string, string>), SourceGenerationContext.Default)
            as Dictionary<string, string>;

        await Loop(keyValues).ConfigureAwait(false);
    }
    
    private static string? GetFunctionNameByFunction(Func<Task> function)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (function == null)
            return "";
        return CustomShortcuts.FirstOrDefault(x => x.Value == function).Value.Method.Name ?? "";
    }
}