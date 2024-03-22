using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Navigation;
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
    #region Keybindings logic

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

    public static Dictionary<Key, Func<Task>>? CustomShortcuts;
    private static MainViewModel? _vm;

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
        _vm = vm;
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

    #endregion Keybindings logic

    #region Functions list

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
            "Next" => Next,
            "Prev" => Prev,
            "Up" => Up,
            "Down" => Down,

            // Scroll
            "ScrollToTop" => ScrollToTop,
            "ScrollToBottom" => ScrollToBottom,

            // Zoom
            "ZoomIn" => ZoomIn,
            "ZoomOut" => ZoomOut,
            "ResetZoom" => ResetZoom,

            // Toggles
            "ToggleScroll" => ToggleScroll,
            "ToggleLooping" => ToggleLooping,
            "ToggleGallery" => ToggleGallery,

            // Scale Window
            "AutoFitWindow" => AutoFitWindow,
            "AutoFitWindowAndStretch" => AutoFitWindowAndStretch,
            "NormalWindow" => NormalWindow,
            "NormalWindowAndStretch" => NormalWindowAndStretch,

            // Window functions
            "Fullscreen" => Fullscreen,
            "SetTopMost" => SetTopMost,
            "Close" => Close,
            "ToggleInterface" => ToggleInterface,
            "NewWindow" => NewWindow,

            // Windows
            "AboutWindow" => AboutWindow,
            "EffectsWindow" => EffectsWindow,
            "ImageInfoWindow" => ImageInfoWindow,
            "ResizeWindow" => ResizeWindow,
            "SettingsWindow" => SettingsWindow,
            "KeybindingsWindow" => KeybindingsWindow,

            // Open functions
            "Open" => Open,
            "OpenWith" => OpenWith,
            "OpenInExplorer" => OpenInExplorer,
            "Save" => Save,
            "Print" => Print,
            "Reload" => Reload,

            // Copy functions
            "CopyFile" => CopyFile,
            "CopyFilePath" => CopyFilePath,
            "CopyImage" => CopyImage,
            "CopyBase64" => CopyBase64,
            "DuplicateFile" => DuplicateFile,
            "CutFile" => CutFile,
            "Paste" => Paste,

            // File functions
            "DeleteFile" => DeleteFile,
            "Rename" => Rename,
            "ShowFileProperties" => ShowFileProperties,

            // Image functions
            "ResizeImage" => ResizeImage,
            "Crop" => Crop,
            "Flip" => Flip,
            "OptimizeImage" => OptimizeImage,
            "Stretch" => Stretch,

            // Set stars
            "Set0Star" => Set0Star,
            "Set1Star" => Set1Star,
            "Set2Star" => Set2Star,
            "Set3Star" => Set3Star,
            "Set4Star" => Set4Star,
            "Set5Star" => Set5Star,

            // Misc
            "ChangeBackground" => ChangeBackground,
            "GalleryClick" => GalleryClick,
            "Center" => Center,
            "Slideshow" => Slideshow,
            "ColorPicker" => ColorPicker,

            _ => null
        });
    }

    private static Task Print()
    {
        throw new NotImplementedException();
    }

    private static async Task Next()
    {
        await NavigationHelper.Navigate(true, _vm);
    }

    private static async Task Prev()
    {
        await NavigationHelper.Navigate(false, _vm);
    }

    private static async Task Up()
    {
        if (_vm is null)
        {
            return;
        }

        if (_vm.IsScrollingEnabled)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ImageViewer.ImageScrollViewer.Offset.Y == 0)
                {
                    _vm.ImageViewer.Rotate(clockWise: true, animate: true);
                }
                else
                {
                    _vm.ImageViewer.ImageScrollViewer.LineUp();
                }
            });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _vm.ImageViewer.Rotate(clockWise: true, animate: true);
            });
        }
    }

    private static async Task Down()
    {
        if (_vm is null)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.ImageViewer.Rotate(clockWise: false, animate: true);
        });
    }

    private static async Task ScrollToTop()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.ImageViewer.ImageScrollViewer.ScrollToHome();
        });
    }

    private static async Task ScrollToBottom()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.ImageViewer.ImageScrollViewer.ScrollToEnd();
        });
    }

    private static async Task ZoomIn()
    {
        if (_vm is null)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(_vm.ImageViewer.ZoomIn);
    }

    private static async Task ZoomOut()
    {
        if (_vm is null)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(_vm.ImageViewer.ZoomOut);
    }

    private static Task ResetZoom()
    {
        throw new NotImplementedException();
    }

    private static Task ToggleScroll()
    {
        throw new NotImplementedException();
    }

    private static Task ToggleLooping()
    {
        throw new NotImplementedException();
    }

    private static Task ToggleGallery()
    {
        throw new NotImplementedException();
    }

    private static Task AutoFitWindow()
    {
        throw new NotImplementedException();
    }

    private static Task AutoFitWindowAndStretch()
    {
        throw new NotImplementedException();
    }

    private static Task NormalWindow()
    {
        throw new NotImplementedException();
    }

    private static Task NormalWindowAndStretch()
    {
        throw new NotImplementedException();
    }

    private static Task Fullscreen()
    {
#if DEBUG
        // Show Avalonia DevTools in DEBUG mode
        return Task.CompletedTask;
#endif
        throw new NotImplementedException();
    }

    private static Task SetTopMost()
    {
        throw new NotImplementedException();
    }

    private static async Task Close()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            desktop.MainWindow?.Close();
        });
    }

    private static Task ToggleInterface()
    {
        throw new NotImplementedException();
    }

    private static Task NewWindow()
    {
        throw new NotImplementedException();
    }

    private static Task AboutWindow()
    {
        throw new NotImplementedException();
    }

    private static Task KeybindingsWindow()
    {
        _vm?.ShowKeybindingsWindowCommand.Execute(null);
        return Task.CompletedTask;
    }

    private static Task EffectsWindow()
    {
        throw new NotImplementedException();
    }

    private static Task ImageInfoWindow()
    {
        throw new NotImplementedException();
    }

    private static Task ResizeWindow()
    {
        throw new NotImplementedException();
    }

    private static Task SettingsWindow()
    {
        _vm?.ShowSettingsWindowCommand.Execute(null);
        return Task.CompletedTask;
    }

    private static Task Open()
    {
        throw new NotImplementedException();
    }

    private static Task OpenWith()
    {
        throw new NotImplementedException();
    }

    private static Task OpenInExplorer()
    {
        throw new NotImplementedException();
    }

    private static Task Save()
    {
        throw new NotImplementedException();
    }

    private static Task Reload()
    {
        throw new NotImplementedException();
    }

    private static Task CopyFile()
    {
        throw new NotImplementedException();
    }

    private static Task CopyFilePath()
    {
        throw new NotImplementedException();
    }

    private static Task CopyImage()
    {
        throw new NotImplementedException();
    }

    private static Task CopyBase64()
    {
        throw new NotImplementedException();
    }

    private static Task DuplicateFile()
    {
        throw new NotImplementedException();
    }

    private static Task CutFile()
    {
        throw new NotImplementedException();
    }

    private static Task Paste()
    {
        throw new NotImplementedException();
    }

    private static Task DeleteFile()
    {
        throw new NotImplementedException();
    }

    private static Task Rename()
    {
        throw new NotImplementedException();
    }

    private static Task ShowFileProperties()
    {
        throw new NotImplementedException();
    }

    private static Task ResizeImage()
    {
        throw new NotImplementedException();
    }

    private static Task Crop()
    {
        throw new NotImplementedException();
    }

    private static async Task Flip()
    {
        if (_vm is null)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.ImageViewer.Flip(animate: true);
        });
    }

    private static Task OptimizeImage()
    {
        throw new NotImplementedException();
    }

    private static Task Stretch()
    {
        throw new NotImplementedException();
    }

    private static Task Set0Star()
    {
        throw new NotImplementedException();
    }

    private static Task Set1Star()
    {
        throw new NotImplementedException();
    }

    private static Task Set2Star()
    {
        throw new NotImplementedException();
    }

    private static Task Set3Star()
    {
        throw new NotImplementedException();
    }

    private static Task Set4Star()
    {
        throw new NotImplementedException();
    }

    private static Task Set5Star()
    {
        throw new NotImplementedException();
    }

    private static Task ChangeBackground()
    {
        throw new NotImplementedException();
    }

    private static Task GalleryClick()
    {
        throw new NotImplementedException();
    }

    private static async Task Center()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            WindowHelper.CenterWindowOnScreen();
        });
    }

    private static Task Slideshow()
    {
        throw new NotImplementedException();
    }

    private static Task ColorPicker()
    {
        throw new NotImplementedException();
    }

    #endregion Functions list
}