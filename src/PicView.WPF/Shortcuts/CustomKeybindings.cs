using PicView.Core.Keybindings;
using PicView.WPF.UILogic;
using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace PicView.WPF.Shortcuts;

internal static class CustomKeybindings
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
                                                "D0": "Set0Star",
                                                "D1": "Set1Star",
                                                "D2": "Set2Star",
                                                "D3": "Set3Star",
                                                "D4": "Set4Star",
                                                "D5": "Set5Star"
                                              }
                                              """;

    /// <summary>
    /// Dictionary to store custom shortcuts with keys and associated functions.
    /// </summary>
    internal static Dictionary<Key, Func<Task>>? CustomShortcuts;

    /// <summary>
    /// Asynchronously loads custom keybindings from a file.
    /// </summary>
    public static async Task LoadKeybindings()
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
            Tooltip.ShowTooltipMessage($"Error loading keybindings: {ex.Message}", true, TimeSpan.FromSeconds(5));
        }
    }

    /// <summary>
    /// Updates the custom keybindings from the provided JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing key-value pairs of shortcuts and functions.</param>
    internal static async Task UpdateKeybindings(string json)
    {
        // Deserialize JSON into a dictionary of string keys and string values
        var keyValues = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        if (CustomShortcuts is null)
        {
            CustomShortcuts = new Dictionary<Key, Func<Task>>();
        }
        else if (CustomShortcuts.Count > 0)
        {
            CustomShortcuts.Clear();
        }

        foreach (var kvp in keyValues)
        {
            var key = Enum.Parse<Key>(kvp.Key);
            var function = await UIHelper.GetFunctionByName(kvp.Value).ConfigureAwait(false);
            // Add to the dictionary
            CustomShortcuts[key] = function;
        }
    }

    /// <summary>
    /// Updates the keybindings.json file with the current custom shortcuts.
    /// </summary>
    internal static async Task UpdateKeyBindingsFile()
    {
        // Serialize the CustomShortcuts dictionary to JSON
        var json =
            JsonSerializer.Serialize(CustomShortcuts.ToDictionary(kvp => kvp.Key.ToString(),
                kvp => UIHelper.GetFunctionNameByFunction(kvp.Value)), new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await KeybindingFunctions.SaveKeyBindingsFile(json).ConfigureAwait(false);
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

        var keyValues = JsonSerializer.Deserialize<Dictionary<string, string>>(DefaultKeybindings);
        foreach (var kvp in keyValues)
        {
            var key = Enum.Parse<Key>(kvp.Key);
            var function = await UIHelper.GetFunctionByName(kvp.Value).ConfigureAwait(false);
            // Add to the dictionary
            CustomShortcuts[key] = function;
        }
    }
}