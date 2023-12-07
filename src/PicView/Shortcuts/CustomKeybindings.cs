using PicView.FileHandling;
using PicView.UILogic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace PicView.Shortcuts;

internal static class CustomKeybindings
{
    /// <summary>
    /// Dictionary to store custom shortcuts with keys and associated functions.
    /// </summary>
    internal static Dictionary<Key, Func<Task>>? CustomShortcuts;

    /// <summary>
    /// Asynchronously loads custom keybindings from a file.
    /// </summary>
    internal static async Task LoadKeybindings()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/keybindings.json");
            var text = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            await UpdateKeybindings(text).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            await CreateNewDefaultKeybindingFile().ConfigureAwait(false);
            await LoadKeybindings().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Handle other exceptions as needed
            Tooltip.ShowTooltipMessage($"Error loading keybindings: {ex.Message}");
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
            var function = await GetFunctionByName(kvp.Value).ConfigureAwait(false);
            // Add to the dictionary
            CustomShortcuts[key] = function;
        }
    }

    /// <summary>
    /// Updates the keybindings.json file with the current custom shortcuts.
    /// </summary>
    internal static async Task UpdateKeyBindingsFile()
    {
        try
        {
            // Serialize the CustomShortcuts dictionary to JSON
            var json =
                JsonSerializer.Serialize(CustomShortcuts.ToDictionary(kvp => kvp.Key.ToString(),
                    kvp => GetFunctionNameByFunction(kvp.Value)), new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            // Write the JSON to the keybindings.json file
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/keybindings.json");
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(UpdateKeyBindingsFile)} exception:\n{exception.Message}");
#endif
            Tooltip.ShowTooltipMessage(exception.Message);
        }
    }

    /// <summary>
    /// Resets the keybindings to the default settings.
    /// </summary>
    internal static async Task ResetToDefault()
    {
        await CreateNewDefaultKeybindingFile().ConfigureAwait(false);
        CustomShortcuts?.Clear();
        await LoadKeybindings().ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new default keybindings file
    /// </summary>
    internal static async Task CreateNewDefaultKeybindingFile()
    {
        // Create a default keybindings file
        var defaultKeybindings = @"
{
  ""D"": ""Next"",
  ""Right"": ""Next"",
  ""A"": ""Prev"",
  ""Left"": ""Prev"",
  ""W"": ""Up"",
  ""Up"": ""Up"",
  ""S"": ""Down"",
  ""Down"": ""Down"",
  ""PageUp"": ""ScrollUp"",
  ""PageDown"": ""ScrollDown"",
  ""Add"": ""ZoomIn"",
  ""OemPlus"": ""ZoomIn"",
  ""OemMinus"": ""ZoomOut"",
  ""Subtract"": ""ZoomOut"",
  ""Scroll"": ""ToggleScroll"",
  ""Home"": ""ScrollToTop"",
  ""End"": ""ScrollToBottom"",
  ""G"": ""ToggleGallery"",
  ""F"": ""Flip"",
  ""J"": ""ResizeImage"",
  ""C"": ""Crop"",
  ""E"": ""GalleryClick"",
  ""Enter"": ""GalleryClick"",
  ""Delete"": ""DeleteFile"",
  ""I"": ""ImageInfoWindow"",
  ""F6"": ""EffectsWindow"",
  ""F1"": ""AboutWindow"",
  ""F2"": ""Rename"",
  ""F3"": ""OpenInExplorer"",
  ""F4"": ""SettingsWindow"",
  ""F5"": ""Slideshow"",
  ""F11"": ""Fullscreen"",
  ""F12"": ""Fullscreen"",
  ""B"": ""ChangeBackground"",
  ""Space"": ""Center"",
  ""D0"": ""Set0Star"",
  ""D1"": ""Set1Star"",
  ""D2"": ""Set2Star"",
  ""D3"": ""Set3Star"",
  ""D4"": ""Set4Star"",
  ""D5"": ""Set5Star""
}";

        // Save the default keybindings to a new file
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/keybindings.json");
        try
        {
            await File.WriteAllTextAsync(path, defaultKeybindings).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(CreateNewDefaultKeybindingFile)} {path} exception:\n{exception.Message}");
#endif
        }
    }

    /// <summary>
    /// Gets the function associated with a given function name.
    /// </summary>
    /// <param name="functionName">Name of the function.</param>
    /// <returns>Task containing the function.</returns>
    internal static Task<Func<Task>> GetFunctionByName(string functionName)
    {
        // Remember to have exact matching names or it will be null
        return Task.FromResult<Func<Task>>(functionName switch
        {
            // Navigation values
            "Next" => UIHelper.Next,
            "Prev" => UIHelper.Prev,
            "Up" => UIHelper.Up,
            "Down" => UIHelper.Down,

            // Scroll
            "ScrollToTop" => UIHelper.ScrollToTop,
            "ScrollToBottom" => UIHelper.ScrollToBottom,

            // Zoom
            "ZoomIn" => UIHelper.ZoomIn,
            "ZoomOut" => UIHelper.ZoomOut,
            "ResetZoom" => UIHelper.ResetZoom,

            // Toggles
            "ToggleScroll" => UIHelper.ToggleScroll,
            "ToggleLooping" => UIHelper.ToggleLooping,
            "ToggleGallery" => UIHelper.ToggleGallery,

            // Scale Window
            "AutoFitWindow" => UIHelper.AutoFitWindow,
            "AutoFitWindowAndStretch" => UIHelper.AutoFitWindowAndStretch,
            "NormalWindow" => UIHelper.NormalWindow,
            "NormalWindowAndStretch" => UIHelper.NormalWindowAndStretch,

            // Window functions
            "Fullscreen" => UIHelper.Fullscreen,
            "SetTopMost" => UIHelper.SetTopMost,
            "Close" => UIHelper.Close,
            "ToggleInterface" => UIHelper.ToggleInterface,
            "NewWindow" => UIHelper.NewWindow,

            // Windows
            "AboutWindow" => UIHelper.AboutWindow,
            "EffectsWindow" => UIHelper.EffectsWindow,
            "ImageInfoWindow" => UIHelper.ImageInfoWindow,
            "ResizeWindow" => UIHelper.ResizeWindow,
            "SettingsWindow" => UIHelper.SettingsWindow,

            // Open functions
            "Open" => UIHelper.Open,
            "OpenWith" => UIHelper.OpenWith,
            "OpenInExplorer" => UIHelper.OpenInExplorer,
            "Save" => OpenSave.SaveFilesAsync,
            "Reload" => UIHelper.Reload,

            // Copy functions
            "CopyFile" => UIHelper.CopyFile,
            "CopyFilePath" => UIHelper.CopyFilePath,
            "CopyImage" => UIHelper.CopyImage,
            "CopyBase64" => UIHelper.CopyBase64,
            "DuplicateFile" => UIHelper.DuplicateFile,
            "CutFile" => UIHelper.CutFile,
            "Paste" => UIHelper.Paste,

            // File functions
            "DeleteFile" => UIHelper.DeleteFile,
            "Rename" => UIHelper.Rename,
            "ShowFileProperties" => UIHelper.ShowFileProperties,

            // Image functions
            "ResizeImage" => UIHelper.ResizeImage,
            "Crop" => UIHelper.Crop,
            "Flip" => UIHelper.Flip,
            "OptimizeImage" => UIHelper.OptimizeImage,
            "Stretch" => UIHelper.Stretch,

            // Set stars
            "Set0Star" => UIHelper.Set0Star,
            "Set1Star" => UIHelper.Set1Star,
            "Set2Star" => UIHelper.Set2Star,
            "Set3Star" => UIHelper.Set3Star,
            "Set4Star" => UIHelper.Set4Star,
            "Set5Star" => UIHelper.Set5Star,

            // Misc
            "ChangeBackground" => UIHelper.ChangeBackground,
            "GalleryClick" => UIHelper.GalleryClick,
            "Center" => UIHelper.Center,
            "Slideshow" => UIHelper.Slideshow,
            "ColorPicker" => UIHelper.ColorPicker,

            _ => null
        });
    }

    /// <summary>
    /// Gets the function name associated with a given function.
    /// </summary>
    /// <param name="function">The function.</param>
    /// <returns>The function name.</returns>
    internal static string? GetFunctionNameByFunction(Func<Task> function)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (function == null)
            return "";
        return CustomShortcuts.FirstOrDefault(x => x.Value == function).Value.Method.Name ?? "";
    }
}