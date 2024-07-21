using Avalonia.Input;
using PicView.Core.Keybindings;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.Keybindings;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class SourceGenerationContext : JsonSerializerContext;

public static class KeybindingsHelper
{
    // TODO move to an interface, use this as default for Windows and make a macOS default
    private const string DefaultKeybindings = """
                                              {
                                                "D": "Next",
                                                "Right": "Next",
                                                "Ctrl+Right": "Last",
                                                "Ctrl+D": "Last",
                                                "Ctrl+Left": "First",
                                                "Ctrl+A": "First",
                                                "Shift+D": "NextFolder",
                                                "Shift+Right": "NextFolder",
                                                "Shift+A": "PrevFolder",
                                                "Shift+Left": "PrevFolder",
                                                "A": "Prev",
                                                "Left": "Prev",
                                                "W": "Up",
                                                "Up": "Up",
                                                "S": "Down",
                                                "Down": "Down",
                                                "PageUp": "ScrollUp",
                                                "PageDown": "ScrollDown",
                                                "Add": "ZoomIn",
                                                "OemPlus": "ZoomIn",
                                                "OemMinus": "ZoomOut",
                                                "Subtract": "ZoomOut",
                                                "Scroll": "ToggleScroll",
                                                "Home": "ScrollToTop",
                                                "End": "ScrollToBottom",
                                                "G": "ToggleGallery",
                                                "F": "Flip",
                                                "J": "ResizeImage",
                                                "L": "ToggleLooping",
                                                "C": "Crop",
                                                "E": "GalleryClick",
                                                "Enter": "GalleryClick",
                                                "I": "ImageInfoWindow",
                                                "F6": "EffectsWindow",
                                                "F1": "AboutWindow",
                                                "F3": "OpenInExplorer",
                                                "F4": "SettingsWindow",
                                                "F5": "Slideshow",
                                                "F11": "Fullscreen",
                                                "F12": "Fullscreen",
                                                "B": "ChangeBackground",
                                                "Space": "Center",
                                                "K": "KeybindingsWindow",
                                                "D0": "Set0Star",
                                                "D1": "Set1Star",
                                                "D2": "Set2Star",
                                                "D3": "Set3Star",
                                                "D4": "Set4Star",
                                                "D5": "Set5Star",
                                                "Ctrl+O": "Open",
                                                "Ctrl+E": "OpenWith",
                                                "Ctrl+R": "Reload",
                                                "Ctrl+S": "Save",
                                                "F2": "Rename",
                                                "Ctrl+C": "CopyFile",
                                                "Ctrl+Alt+V": "CopyFilePath",
                                                "Ctrl+Shift+C": "CopyImage",
                                                "Ctrl+X": "CutFile",
                                                "Ctrl+V": "Paste",
                                                "Ctrl+P": "Print",
                                                "Alt+Z": "ToggleInterface",
                                                "Delete": "DeleteFile"
                                              }
                                              """;

    public static Dictionary<KeyGesture, Func<Task>>? CustomShortcuts { get; private set; }

    public static async Task LoadKeybindings()
    {
        try
        {
            var keybindings = await KeybindingFunctions.LoadKeyBindingsFile().ConfigureAwait(false);
            await UpdateKeybindings(keybindings).ConfigureAwait(false);
        }
        catch (Exception)
        {
            await SetDefaultKeybindings().ConfigureAwait(false);
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
                SourceGenerationContext.Default).Replace("\\u002B", "+"); // Fix plus sign encoded to unicode
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

    internal static async Task SetDefaultKeybindings()
    {
        if (CustomShortcuts is not null)
        {
            CustomShortcuts.Clear();
        }
        else
        {
            CustomShortcuts = new Dictionary<KeyGesture, Func<Task>>();
        }
        var keyValues = JsonSerializer.Deserialize(
                DefaultKeybindings, typeof(Dictionary<string, string>), SourceGenerationContext.Default)
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