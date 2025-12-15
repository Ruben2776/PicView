using System.Collections.ObjectModel;
using Avalonia.Input;
using PicView.Avalonia.Input;
using PicView.Core.DebugTools;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Functions;

public static class FunctionsKeyHelper
{
    private static bool _isInitialized;
    public static string GetFunctionKeyName(string methodName, bool isReadOnly, bool alt)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return string.Empty;
        }

        if (isReadOnly)
        {
            switch (methodName)
            {
                case "ScrollUpInternal":
                    var rotateRightKey = KeybindingManager.CustomShortcuts.Where(x => x.Value?.Method?.Name == "Up")
                        ?.Select(x => x.Key).ToList() ?? null;
                    return rotateRightKey is not { Count: > 0 } ? string.Empty :
                        alt ? rotateRightKey.LastOrDefault().ToString() : rotateRightKey.FirstOrDefault().ToString();

                case "ScrollDownInternal":
                    var rotateLeftKey = KeybindingManager.CustomShortcuts.Where(x => x.Value?.Method?.Name == "Down")
                        ?.Select(x => x.Key).ToList() ?? null;
                    return rotateLeftKey is not { Count: > 0 } ? string.Empty :
                        alt ? rotateLeftKey.LastOrDefault().ToString() : rotateLeftKey.FirstOrDefault().ToString();
            }
        }

        // Find the key associated with the specified function
        var keys = GetKeysFromFunction(methodName);

        var keyGestures = keys ?? keys.ToArray();
        return keyGestures.Length switch
        {
            <= 0 => string.Empty,
            1 => alt ? string.Empty : FormatPlus(keyGestures?.FirstOrDefault()?.ToString() ?? string.Empty),
            _ => alt ? FormatPlus(keyGestures.LastOrDefault()?.ToString() ?? string.Empty) : FormatPlus(keyGestures.FirstOrDefault().ToString())
        };
    }
    
    private static string FormatPlus(string value) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.Replace("+", " + ");

    public static KeyGesture[] GetKeysFromFunction(string methodName)
    {
        var keys = KeybindingManager.CustomShortcuts.Where(x => x.Value?.Method?.Name == methodName)?
            .Select(x => x.Key) ?? null;

        return keys as KeyGesture[] ?? keys.ToArray();
    }

    public static void ResetKeybindings(KeybindingsViewModel keybindings)
    {
        _isInitialized = false;
        LoadKeybindingsViewModel(keybindings);
    }

    public static void LoadKeybindingsViewModel(KeybindingsViewModel keybindings)
    {
        try
        {
            if (_isInitialized)
            {
                return;
            }
            LoadKeybindingsViewModelCore(keybindings);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(FunctionsKeyHelper), nameof(LoadKeybindingsViewModel), e);
        }
        finally
        {
            _isInitialized = true;
        }
    }

    private static void LoadKeybindingsViewModelCore(KeybindingsViewModel keybindings)
    {
        // TODO: Find a way to determine correct translations, so these can be added dynamically instead of hardcoding them
        
        // Navigation keys
        var navKeys = keybindings.NavigationKeys.Value;
        AddBinding(navKeys, "Next", TranslationManager.Translation.NextImage);
        AddBinding(navKeys, "Next10", TranslationManager.Translation.AdvanceBy10Images);
        AddBinding(navKeys, "Next100", TranslationManager.Translation.AdvanceBy100Images);
        AddBinding(navKeys, "Prev", TranslationManager.Translation.PrevImage);
        AddBinding(navKeys, "Prev10", TranslationManager.Translation.GoBackBy10Images);
        AddBinding(navKeys, "Prev100", TranslationManager.Translation.GoBackBy100Images);
        AddBinding(navKeys, "Last", TranslationManager.Translation.LastImage);
        AddBinding(navKeys, "First", TranslationManager.Translation.FirstImage);
        AddBinding(navKeys, "NextFolder", TranslationManager.Translation.NextFolder);
        AddBinding(navKeys, "PrevFolder", TranslationManager.Translation.PrevFolder);
        AddBinding(navKeys, "Search", TranslationManager.Translation.Search);
        AddBinding(navKeys, "GalleryClick", TranslationManager.Translation.SelectGalleryThumb);
        AddBinding(navKeys, "ToggleLooping", TranslationManager.Translation.ToggleLooping);
        
        // Scroll and rotate keys
        var scrollKeys = keybindings.ScrollAndRotateKeys.Value;

        // Special case: ScrollUpInternal is read-only and derived from 'Up' keys
        AddBinding(scrollKeys, "Up", TranslationManager.Translation.RotateRight);
        var upInternalCheck = KeybindingManager.CustomShortcuts.Where(x => x.Value?.Method?.Name == "Up")
            ?.Select(x => x.Key).ToList() ?? null;
        var scrollUpInternal = new KeyBindingsModel
        {
            MethodName = "ScrollUpInternal",
            FriendlyMethodName = TranslationManager.Translation.ScrollUp,
            Key = FormatPlus(upInternalCheck?.FirstOrDefault()?.ToString() ?? string.Empty),
            AltKey = FormatPlus(upInternalCheck?.LastOrDefault()?.ToString() ?? string.Empty),
            IsReadOnly = true
        };
        keybindings.ScrollAndRotateKeys.Value.Add(scrollUpInternal);
        AddBinding(scrollKeys, "ScrollUp", TranslationManager.Translation.ScrollUp);
        
        // Special case: ScrollDownInternal is read-only and derived from 'Down' keys
        AddBinding(scrollKeys, "Down", TranslationManager.Translation.RotateLeft);
        var downInternalCheck = KeybindingManager.CustomShortcuts.Where(x => x.Value?.Method?.Name == "Down")
            ?.Select(x => x.Key).ToList() ?? null;
        var scrollDownInternal = new KeyBindingsModel
        {
            MethodName = "ScrollDownInternal",
            FriendlyMethodName = TranslationManager.Translation.ScrollDown,
            Key = FormatPlus(downInternalCheck?.FirstOrDefault()?.ToString() ?? string.Empty),
            AltKey = FormatPlus(downInternalCheck?.LastOrDefault()?.ToString() ?? string.Empty),
            IsReadOnly = true
        };
        keybindings.ScrollAndRotateKeys.Value.Add(scrollDownInternal);
        
        AddBinding(scrollKeys, "ScrollDown", TranslationManager.Translation.ScrollDown);

        
        AddBinding(scrollKeys, "ScrollDown", TranslationManager.Translation.ScrollDown);
        AddBinding(scrollKeys, "ScrollToTop", TranslationManager.Translation.ScrollToTop);
        AddBinding(scrollKeys, "ScrollToBottom", TranslationManager.Translation.ScrollToBottom);
        AddBinding(scrollKeys, "ToggleScroll", TranslationManager.Translation.ToggleScroll);
        AddBinding(scrollKeys, "ChangeCtrlZoom", TranslationManager.Translation.CtrlToZoom);
        
        // Zoom keys
        var zoomKeys = keybindings.ZoomKeys.Value;
        AddBinding(zoomKeys, "ZoomIn", TranslationManager.Translation.ZoomIn);
        AddBinding(zoomKeys, "ZoomOut", TranslationManager.Translation.ZoomOut);
        AddBinding(zoomKeys, "ResetZoom", TranslationManager.Translation.ResetZoom);
        
        // Special case: Show pan as read only
        zoomKeys.Add(new KeyBindingsModel
        {
            FriendlyMethodName = TranslationManager.Translation.Pan,
            Key = TranslationManager.Translation.MouseDrag,
            IsReadOnly = true
        });
        
        var imageControl = keybindings.ImageControlKeys.Value;
        AddBinding(imageControl, "SideBySide", TranslationManager.Translation.SideBySide);
        AddBinding(imageControl, "Stretch", TranslationManager.Translation.Stretch);
        AddBinding(imageControl, "Flip", TranslationManager.Translation.Flip);
        AddBinding(imageControl, "Crop", TranslationManager.Translation.Crop);
        AddBinding(imageControl, "ChangeBackground", TranslationManager.Translation.ChangeBackground);
        AddBinding(imageControl, "OptimizeImage", TranslationManager.Translation.OptimizeImage);

        // Interface keys
        var interfaceConfiguration = keybindings.InterfaceConfigurationKeys.Value;
        interfaceConfiguration.Clear();
        AddBinding(interfaceConfiguration, "ToggleInterface", TranslationManager.Translation.HideUI);
        AddBinding(interfaceConfiguration, "Slideshow", TranslationManager.Translation.Slideshow);
        AddBinding(interfaceConfiguration, "ToggleGallery", TranslationManager.Translation.ShowImageGallery);

        // File management
        var fileManagement = keybindings.FileManagementKeys.Value;
        AddBinding(fileManagement, "Open", TranslationManager.Translation.Open);
        AddBinding(fileManagement, "OpenWith", TranslationManager.Translation.OpenWith);
        AddBinding(fileManagement, "Reload", TranslationManager.Translation.Reload);
        AddBinding(fileManagement, "Save", TranslationManager.Translation.Save);
        AddBinding(fileManagement, "SaveAs", TranslationManager.Translation.SaveAs);
        AddBinding(fileManagement, "Print", TranslationManager.Translation.Print);
        AddBinding(fileManagement, "DeleteFile", TranslationManager.Translation.DeleteFile);
        AddBinding(fileManagement, "DeleteFilePermanently", TranslationManager.Translation.PermanentlyDelete);
        AddBinding(fileManagement, "Rename", TranslationManager.Translation.RenameFile);
        AddBinding(fileManagement, "ShowFileProperties", TranslationManager.Translation.FileProperties);
        
        // Sorting
        var sortFiles = keybindings.SortFilesKeys.Value;
        AddBinding(sortFiles, "SortFilesByName", TranslationManager.Translation.FileName);
        AddBinding(sortFiles, "SortFilesBySize", TranslationManager.Translation.FileSize);
        AddBinding(sortFiles, "SortFilesByExtension", TranslationManager.Translation.FileExtension);
        AddBinding(sortFiles, "SortFilesByCreationTime", TranslationManager.Translation.Created);
        AddBinding(sortFiles, "SortFilesByLastAccessTime", TranslationManager.Translation.LastAccessTime);
        AddBinding(sortFiles, "SortFilesRandomly", TranslationManager.Translation.Random);
        AddBinding(sortFiles, "SortFilesAscending", TranslationManager.Translation.Ascending);
        AddBinding(sortFiles, "SortFilesDescending", TranslationManager.Translation.Descending);
        
        // Copy
        var copy = keybindings.CopyKeys.Value;
        AddBinding(copy, "CopyFile", TranslationManager.Translation.CopyFile);
        AddBinding(copy, "CopyFilePath", TranslationManager.Translation.FileCopyPath);
        AddBinding(copy, "CopyImage", TranslationManager.Translation.CopyImage);
        AddBinding(copy, "CopyBase64", TranslationManager.Translation.Copy + " base64");
        AddBinding(copy, "Paste", TranslationManager.Translation.FilePaste);
        AddBinding(copy, "DuplicateFile", TranslationManager.Translation.DuplicateFile);
        
        // Tool windows
        var toolWindows = keybindings.ToolWindowsKeys.Value;
        AddBinding(toolWindows, "AboutWindow", TranslationManager.Translation.About);
        AddBinding(toolWindows, "SettingsWindow", TranslationManager.Translation.Settings);
        AddBinding(toolWindows, "ImageInfoWindow", TranslationManager.Translation.ImageInfo);
        AddBinding(toolWindows, "ConvertWindow", TranslationManager.Translation.FileConversion);
        AddBinding(toolWindows, "KeybindingsWindow", TranslationManager.Translation.ApplicationShortcuts);
        AddBinding(toolWindows, "BatchResizeWindow", TranslationManager.Translation.BatchResize);
        AddBinding(toolWindows, "ResizeWindow", TranslationManager.Translation.Resize);
        
        // Window management
        var windowManagement = keybindings.WindowManagementKeys.Value;
        // Special case: Show close as read only
        windowManagement.Add(new KeyBindingsModel
        {
            FriendlyMethodName = TranslationManager.Translation.Close,
            Key = TranslationManager.Translation.Esc,
            IsReadOnly = true
        });
        AddBinding(windowManagement, "Close", TranslationManager.Translation.Close);
        AddBinding(windowManagement, "NewWindow", TranslationManager.Translation.NewWindow);
        AddBinding(windowManagement, "Center", TranslationManager.Translation.CenterWindow);
        AddBinding(windowManagement, "SetTopMost", TranslationManager.Translation.StayTopMost);
        // Special case: Show move window as read only
        windowManagement.Add(new KeyBindingsModel
        {
            FriendlyMethodName = TranslationManager.Translation.MoveWindow,
            Key = $"{TranslationManager.Translation.Shift} + {TranslationManager.Translation.MouseDrag}" ,
            IsReadOnly = true
        });
        AddBinding(windowManagement, "Fullscreen", TranslationManager.Translation.ToggleFullscreen);
        // TODO: Add minize
        // AddBinding(windowManagement, "Minimize", TranslationManager.Translation.Minimize);
        AddBinding(windowManagement, "Maximize", TranslationManager.Translation.Maximize);
        
        
        // Window scaling
        var windowScaling = keybindings.WindowScalingKeys.Value;
        AddBinding(windowScaling, "AutoFitWindow", TranslationManager.Translation.AutoFitWindow);
        AddBinding(windowScaling, "NormalWindow", TranslationManager.Translation.NormalWindow);
        
        // Set star rating
        var starRating = keybindings.StarRatingKeys.Value;
        AddBinding(starRating, "Set1Star", TranslationManager.Translation._1Star);
        AddBinding(starRating, "Set2Star", TranslationManager.Translation._2Star);
        AddBinding(starRating, "Set3Star", TranslationManager.Translation._3Star);
        AddBinding(starRating, "Set4Star", TranslationManager.Translation._4Star);
        AddBinding(starRating, "Set5Star", TranslationManager.Translation._5Star);
        AddBinding(starRating, "Set0Star", TranslationManager.Translation.RemoveStarRating);
        
    }

    private static void AddBinding(
        ObservableCollection<KeyBindingsModel> collection,
        string methodName,
        string friendlyName,
        System.Collections.IEnumerable? customGestures = null,
        bool isReadOnly = false)
    {
        // Use custom gestures if provided, otherwise fetch via method name
        // Cast<object> ensures we can treat any list type (List<KeyGesture>, etc.) uniformly
        var gestures = (customGestures ?? GetKeysFromFunction(methodName))?.Cast<object>().ToList();
        var model = new KeyBindingsModel
        {
            MethodName = methodName,
            FriendlyMethodName = friendlyName,
            Key = FormatPlus(gestures?.FirstOrDefault()?.ToString() ?? string.Empty),
            AltKey = FormatPlus(gestures?.LastOrDefault()?.ToString() ?? string.Empty),
            IsReadOnly = isReadOnly
        };

        if (!collection.Contains(model))
        {
            collection.Add(model);
        }
        else
        {
            DebugHelper.LogDebug(nameof(FunctionsKeyHelper), nameof(AddBinding),$"Keybinding for {methodName} already exists");
        }
    }

}
