using Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PicView.Avalonia.Keybindings;

[JsonSourceGenerationOptions(AllowTrailingCommas = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class SourceGenerationContext : JsonSerializerContext;

public static class KeybindingsHelper
{
    private const string DefaultKeybindings = """
                                              {
                                                "D": "Next",
                                                "Right": "Next",
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
                                                "F2": "Rename",
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
                                                "Escape": "Close"
                                              }
                                              """;

    public static Dictionary<Key, Func<Task>>? CustomShortcuts { get; private set; }

    public static async Task LoadKeybindings(MainViewModel vm)
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/keybindings.json");
            if (File.Exists(path))
            {
                var text = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                await UpdateKeybindings(text).ConfigureAwait(false);
            }
            else
            {
                await SetDefaultKeybindings().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            // Handle other exceptions as needed
            //Tooltip.ShowTooltipMessage($"Error loading keybindings: {ex.Message}", true, TimeSpan.FromSeconds(5));
        }
    }

    public static async Task UpdateKeybindings()
    {
        var json = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/keybindings.json");
        if (!File.Exists(json))
        {
#if DEBUG
            Trace.WriteLine($"{nameof(UpdateKeybindings)} no json found");
#endif
            return;
        }

        await UpdateKeybindings(json);
    }

    public static async Task UpdateKeybindings(string json)
    {
        // Deserialize JSON into a dictionary of string keys and string values
        var keyValues = JsonSerializer.Deserialize(
                json, typeof(Dictionary<string, string>), SourceGenerationContext.Default)
            as Dictionary<string, string>;

        CustomShortcuts ??= new Dictionary<Key, Func<Task>>();
        await Loop(keyValues).ConfigureAwait(false);
    }

    public static async Task UpdateKeyBindingsFile()
    {
        try
        {
            var json = JsonSerializer.Serialize(
                CustomShortcuts.ToDictionary(kvp => kvp.Key.ToString(),
                    kvp => GetFunctionNameByFunction(kvp.Value)), typeof(Dictionary<string, string>), SourceGenerationContext.Default);

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/keybindings.json");
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
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
            var checkKey = Enum.TryParse<Key>(kvp.Key, out var key);
            if (!checkKey)
            {
                continue;
            }
            var function = await GetFunctionByName(kvp.Value).ConfigureAwait(false);
            // Add to the dictionary
            CustomShortcuts[key] = function;
        }
    }

    internal static async Task SetDefaultKeybindings()
    {
        if (CustomShortcuts is not null)
        {
            CustomShortcuts?.Clear();
        }
        else
        {
            CustomShortcuts = new Dictionary<Key, Func<Task>>();
        }
        var keyValues = JsonSerializer.Deserialize(
                DefaultKeybindings, typeof(Dictionary<string, string>), SourceGenerationContext.Default)
            as Dictionary<string, string>;

        await Loop(keyValues).ConfigureAwait(false);
    }

    internal static string? GetFunctionNameByFunction(Func<Task> function)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (function == null)
            return "";
        return CustomShortcuts.FirstOrDefault(x => x.Value == function).Value.Method.Name ?? "";
    }

    public static Task<Func<Task>> GetFunctionByName(string functionName)
    {
        // Remember to have exact matching names or it will be null
        return Task.FromResult<Func<Task>>(functionName switch
        {
            // Navigation values
            "Next" => UIFunctions.Next,
            "Prev" => UIFunctions.Prev,
            "Up" => UIFunctions.Up,
            "Down" => UIFunctions.Down,

            // Scroll
            "ScrollToTop" => UIFunctions.ScrollToTop,
            "ScrollToBottom" => UIFunctions.ScrollToBottom,

            // Zoom
            "ZoomIn" => UIFunctions.ZoomIn,
            "ZoomOut" => UIFunctions.ZoomOut,
            "ResetZoom" => UIFunctions.ResetZoom,

            // Toggles
            "ToggleScroll" => UIFunctions.ToggleScroll,
            "ToggleLooping" => UIFunctions.ToggleLooping,
            "ToggleGallery" => UIFunctions.ToggleGallery,

            // Scale Window
            "AutoFitWindow" => UIFunctions.AutoFitWindow,
            "AutoFitWindowAndStretch" => UIFunctions.AutoFitWindowAndStretch,
            "NormalWindow" => UIFunctions.NormalWindow,
            "NormalWindowAndStretch" => UIFunctions.NormalWindowAndStretch,

            // Window functions
            "Fullscreen" => UIFunctions.Fullscreen,
            "SetTopMost" => UIFunctions.SetTopMost,
            "Close" => UIFunctions.Close,
            "ToggleInterface" => UIFunctions.ToggleInterface,
            "NewWindow" => UIFunctions.NewWindow,
            "Center" => UIFunctions.Center,

            // Windows
            "AboutWindow" => UIFunctions.AboutWindow,
            "EffectsWindow" => UIFunctions.EffectsWindow,
            "ImageInfoWindow" => UIFunctions.ImageInfoWindow,
            "ResizeWindow" => UIFunctions.ResizeWindow,
            "SettingsWindow" => UIFunctions.SettingsWindow,
            "KeybindingsWindow" => UIFunctions.KeybindingsWindow,

            // Open functions
            "Open" => UIFunctions.Open,
            "OpenWith" => UIFunctions.OpenWith,
            "OpenInExplorer" => UIFunctions.OpenInExplorer,
            "Save" => UIFunctions.Save,
            "Print" => UIFunctions.Print,
            "Reload" => UIFunctions.Reload,

            // Copy functions
            "CopyFile" => UIFunctions.CopyFile,
            "CopyFilePath" => UIFunctions.CopyFilePath,
            "CopyImage" => UIFunctions.CopyImage,
            "CopyBase64" => UIFunctions.CopyBase64,
            "DuplicateFile" => UIFunctions.DuplicateFile,
            "CutFile" => UIFunctions.CutFile,
            "Paste" => UIFunctions.Paste,

            // File functions
            "DeleteFile" => UIFunctions.DeleteFile,
            "Rename" => UIFunctions.Rename,
            "ShowFileProperties" => UIFunctions.ShowFileProperties,

            // Image functions
            "ResizeImage" => UIFunctions.ResizeImage,
            "Crop" => UIFunctions.Crop,
            "Flip" => UIFunctions.Flip,
            "OptimizeImage" => UIFunctions.OptimizeImage,
            "Stretch" => UIFunctions.Stretch,

            // Set stars
            "Set0Star" => UIFunctions.Set0Star,
            "Set1Star" => UIFunctions.Set1Star,
            "Set2Star" => UIFunctions.Set2Star,
            "Set3Star" => UIFunctions.Set3Star,
            "Set4Star" => UIFunctions.Set4Star,
            "Set5Star" => UIFunctions.Set5Star,

            // Misc
            "ChangeBackground" => UIFunctions.ChangeBackground,
            "GalleryClick" => UIFunctions.GalleryClick,
            "Slideshow" => UIFunctions.Slideshow,
            "ColorPicker" => UIFunctions.ColorPicker,

            _ => null
        });
    }
}