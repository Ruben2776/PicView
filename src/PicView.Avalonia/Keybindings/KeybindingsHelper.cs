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
    private static UIFunctions? _uiFunctions;

    public static async Task LoadKeybindings(MainViewModel vm)
    {
        _uiFunctions = new UIFunctions(vm);
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
            "Up" => _uiFunctions.Up,
            "Down" => _uiFunctions.Down,

            // Scroll
            "ScrollToTop" => _uiFunctions.ScrollToTop,
            "ScrollToBottom" => _uiFunctions.ScrollToBottom,

            // Zoom
            "ZoomIn" => _uiFunctions.ZoomIn,
            "ZoomOut" => _uiFunctions.ZoomOut,
            "ResetZoom" => _uiFunctions.ResetZoom,

            // Toggles
            "ToggleScroll" => _uiFunctions.ToggleScroll,
            "ToggleLooping" => _uiFunctions.ToggleLooping,
            "ToggleGallery" => _uiFunctions.ToggleGallery,

            // Scale Window
            "AutoFitWindow" => _uiFunctions.AutoFitWindow,
            "AutoFitWindowAndStretch" => _uiFunctions.AutoFitWindowAndStretch,
            "NormalWindow" => _uiFunctions.NormalWindow,
            "NormalWindowAndStretch" => _uiFunctions.NormalWindowAndStretch,

            // Window functions
            "Fullscreen" => _uiFunctions.Fullscreen,
            "SetTopMost" => _uiFunctions.SetTopMost,
            "Close" => _uiFunctions.Close,
            "ToggleInterface" => _uiFunctions.ToggleInterface,
            "NewWindow" => _uiFunctions.NewWindow,
            "Center" => _uiFunctions.Center,

            // Windows
            "AboutWindow" => _uiFunctions.AboutWindow,
            "EffectsWindow" => _uiFunctions.EffectsWindow,
            "ImageInfoWindow" => _uiFunctions.ImageInfoWindow,
            "ResizeWindow" => _uiFunctions.ResizeWindow,
            "SettingsWindow" => _uiFunctions.SettingsWindow,
            "KeybindingsWindow" => _uiFunctions.KeybindingsWindow,

            // Open functions
            "Open" => _uiFunctions.Open,
            "OpenWith" => _uiFunctions.OpenWith,
            "OpenInExplorer" => _uiFunctions.OpenInExplorer,
            "Save" => _uiFunctions.Save,
            "Print" => _uiFunctions.Print,
            "Reload" => _uiFunctions.Reload,

            // Copy functions
            "CopyFile" => _uiFunctions.CopyFile,
            "CopyFilePath" => _uiFunctions.CopyFilePath,
            "CopyImage" => _uiFunctions.CopyImage,
            "CopyBase64" => _uiFunctions.CopyBase64,
            "DuplicateFile" => _uiFunctions.DuplicateFile,
            "CutFile" => _uiFunctions.CutFile,
            "Paste" => _uiFunctions.Paste,

            // File functions
            "DeleteFile" => _uiFunctions.DeleteFile,
            "Rename" => _uiFunctions.Rename,
            "ShowFileProperties" => _uiFunctions.ShowFileProperties,

            // Image functions
            "ResizeImage" => _uiFunctions.ResizeImage,
            "Crop" => _uiFunctions.Crop,
            "Flip" => _uiFunctions.Flip,
            "OptimizeImage" => _uiFunctions.OptimizeImage,
            "Stretch" => _uiFunctions.Stretch,

            // Set stars
            "Set0Star" => _uiFunctions.Set0Star,
            "Set1Star" => _uiFunctions.Set1Star,
            "Set2Star" => _uiFunctions.Set2Star,
            "Set3Star" => _uiFunctions.Set3Star,
            "Set4Star" => _uiFunctions.Set4Star,
            "Set5Star" => _uiFunctions.Set5Star,

            // Misc
            "ChangeBackground" => _uiFunctions.ChangeBackground,
            "GalleryClick" => _uiFunctions.GalleryClick,
            "Slideshow" => _uiFunctions.Slideshow,
            "ColorPicker" => _uiFunctions.ColorPicker,

            _ => null
        });
    }
}