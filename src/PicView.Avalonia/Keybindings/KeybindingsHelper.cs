using Avalonia.Input;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.ViewModels;
using PicView.Core.Keybindings;
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
                                                "Ctrl+D": "Last",
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
                                                "Escape": "Close",
                                                "Ctrl+O": "Open",
                                              }
                                              """;

    public static Dictionary<KeyGesture, Func<Task>>? CustomShortcuts { get; private set; }

    public static async Task LoadKeybindings(MainViewModel vm)
    {
        try
        {
            var keybindings = await KeybindingFunctions.LoadKeyBindingsFile().ConfigureAwait(false);
            await UpdateKeybindings(keybindings).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await SetDefaultKeybindings().ConfigureAwait(false);
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
                var function = await GetFunctionByName(kvp.Value).ConfigureAwait(false);
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
            CustomShortcuts?.Clear();
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
            "Next" => FunctionsHelper.Next,
            "Prev" => FunctionsHelper.Prev,
            "Up" => FunctionsHelper.Up,
            "Down" => FunctionsHelper.Down,
            "Last" => FunctionsHelper.Last,
            "First" => FunctionsHelper.First,

            // Scroll
            "ScrollToTop" => FunctionsHelper.ScrollToTop,
            "ScrollToBottom" => FunctionsHelper.ScrollToBottom,

            // Zoom
            "ZoomIn" => FunctionsHelper.ZoomIn,
            "ZoomOut" => FunctionsHelper.ZoomOut,
            "ResetZoom" => FunctionsHelper.ResetZoom,

            // Toggles
            "ToggleScroll" => FunctionsHelper.ToggleScroll,
            "ToggleLooping" => FunctionsHelper.ToggleLooping,
            "ToggleGallery" => FunctionsHelper.ToggleGallery,

            // Scale Window
            "AutoFitWindow" => FunctionsHelper.AutoFitWindow,
            "AutoFitWindowAndStretch" => FunctionsHelper.AutoFitWindowAndStretch,
            "NormalWindow" => FunctionsHelper.NormalWindow,
            "NormalWindowAndStretch" => FunctionsHelper.NormalWindowAndStretch,

            // Window functions
            "Fullscreen" => FunctionsHelper.Fullscreen,
            "SetTopMost" => FunctionsHelper.SetTopMost,
            "Close" => FunctionsHelper.Close,
            "ToggleInterface" => FunctionsHelper.ToggleInterface,
            "NewWindow" => FunctionsHelper.NewWindow,
            "Center" => FunctionsHelper.Center,

            // Windows
            "AboutWindow" => FunctionsHelper.AboutWindow,
            "EffectsWindow" => FunctionsHelper.EffectsWindow,
            "ImageInfoWindow" => FunctionsHelper.ImageInfoWindow,
            "ResizeWindow" => FunctionsHelper.ResizeWindow,
            "SettingsWindow" => FunctionsHelper.SettingsWindow,
            "KeybindingsWindow" => FunctionsHelper.KeybindingsWindow,

            // Open functions
            "Open" => FunctionsHelper.Open,
            "OpenWith" => FunctionsHelper.OpenWith,
            "OpenInExplorer" => FunctionsHelper.OpenInExplorer,
            "Save" => FunctionsHelper.Save,
            "Print" => FunctionsHelper.Print,
            "Reload" => FunctionsHelper.Reload,

            // Copy functions
            "CopyFile" => FunctionsHelper.CopyFile,
            "CopyFilePath" => FunctionsHelper.CopyFilePath,
            "CopyImage" => FunctionsHelper.CopyImage,
            "CopyBase64" => FunctionsHelper.CopyBase64,
            "DuplicateFile" => FunctionsHelper.DuplicateFile,
            "CutFile" => FunctionsHelper.CutFile,
            "Paste" => FunctionsHelper.Paste,

            // File functions
            "DeleteFile" => FunctionsHelper.DeleteFile,
            "Rename" => FunctionsHelper.Rename,
            "ShowFileProperties" => FunctionsHelper.ShowFileProperties,

            // Image functions
            "ResizeImage" => FunctionsHelper.ResizeImage,
            "Crop" => FunctionsHelper.Crop,
            "Flip" => FunctionsHelper.Flip,
            "OptimizeImage" => FunctionsHelper.OptimizeImage,
            "Stretch" => FunctionsHelper.Stretch,

            // Set stars
            "Set0Star" => FunctionsHelper.Set0Star,
            "Set1Star" => FunctionsHelper.Set1Star,
            "Set2Star" => FunctionsHelper.Set2Star,
            "Set3Star" => FunctionsHelper.Set3Star,
            "Set4Star" => FunctionsHelper.Set4Star,
            "Set5Star" => FunctionsHelper.Set5Star,

            // Misc
            "ChangeBackground" => FunctionsHelper.ChangeBackground,
            "GalleryClick" => FunctionsHelper.GalleryClick,
            "Slideshow" => FunctionsHelper.Slideshow,
            "ColorPicker" => FunctionsHelper.ColorPicker,

            _ => null
        });
    }
}