using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Views.UC;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Input;

/// <summary>
/// Handles keyboard shortcuts and tracks key modifier states for the new architecture.
/// </summary>
public static class MainKeyboardShortcuts
{
    /// <summary>
    /// Indicates whether a key is held down.
    /// </summary>
    public static bool IsKeyHeldDown { get; private set; }

    /// <summary>
    /// The current modifiers being pressed.
    /// </summary>
    public static KeyModifiers CurrentModifiers { get; private set; }

    /// <summary>
    /// Stores the current key gesture, including the key and its modifiers.
    /// </summary>
    public static KeyGesture? CurrentKeys { get; private set; }

    /// <summary>
    /// Gets or sets whether keyboard shortcuts are enabled.
    /// </summary>
    public static bool IsKeysEnabled { get; set; } = true;
    
    public static bool IsEscKeyEnabled { get; set; } = true;

    private static int _keyRepeatCount;
    private const int KeyRepeatThreshold = 1;
    
    public static bool ShiftDown => (CurrentModifiers & KeyModifiers.Shift) == KeyModifiers.Shift;

    /// <summary>
    /// Processes the KeyDown event for the main window.
    /// </summary>
    public static async ValueTask MainWindow_KeysDownAsync(KeyEventArgs e, MainWindowViewModel? mainWindowViewModel, MainWindow mainWindow)
    {
        if (KeybindingManager.CustomShortcuts is null || !IsKeysEnabled)
        {
            return;
        }

        UpdateModifierState(e.Key, true);
        
#if DEBUG
        // Handle special debug keys first
        if (HandleDebugKeys(e.Key))
        {
            return;
        }
#endif

        // Create key gesture from current state
        // Use e.KeyModifiers (from the event args) instead of the manually tracked
        // CurrentModifiers, because the manual tracking can be out of sync if a
        // modifier key-down event was missed (e.g. focus changes, window deactivation,
        // or the modifier was pressed before the window had focus). This ensures that
        // a bare-key binding such as "S" (rotate) only fires when no modifiers are
        // actually held, so "Ctrl+S" (save) is never mistakenly dispatched as "S".
        CurrentKeys = new KeyGesture(e.Key, e.KeyModifiers);

        // Track key repeat for held down state
        _keyRepeatCount++;
        IsKeyHeldDown = _keyRepeatCount > KeyRepeatThreshold;

        // Handle special cases before processing shortcuts
        if (await HandleSpecialCases(e, mainWindowViewModel, mainWindow))
        {
            return;
        }
        
        // If it's a modifier key only, nothing more to do
        if (IsModifierKey(e.Key))
        {
            return;
        }

        // Handle registered shortcuts
        await ExecuteShortcutIfRegistered(mainWindowViewModel);
    }

    /// <summary>
    /// Processes the KeyUp event for the main window.
    /// </summary>
    /// <param name="e">The key event arguments.</param>
    /// <param name="mainWindowViewModel">The main window view model.</param>
    public static async ValueTask MainWindow_KeysUpAsync(KeyEventArgs e, MainWindowViewModel? mainWindowViewModel)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (CurrentKeys.Key is Key.LeftAlt or Key.RightAlt)
            {
                mainWindowViewModel.TopTitlebarViewModel.ToggleMenu();
            }
        }
        await mainWindowViewModel.Mapper.StopRepeatedNavigation();
        UpdateModifierState(e.Key, false);
        Reset();
    }

    /// <summary>
    /// Updates the state of a modifier key.
    /// </summary>
    /// <param name="key">The key that changed state.</param>
    /// <param name="isDown">Whether the key is being pressed down.</param>
    private static void UpdateModifierState(Key key, bool isDown)
    {
        CurrentModifiers = key switch
        {
            Key.LeftShift or Key.RightShift => isDown
                ? CurrentModifiers | KeyModifiers.Shift
                : CurrentModifiers & ~KeyModifiers.Shift,
            Key.LeftCtrl or Key.RightCtrl => isDown
                ? CurrentModifiers | KeyModifiers.Control
                : CurrentModifiers & ~KeyModifiers.Control,
            Key.LeftAlt or Key.RightAlt => isDown
                ? CurrentModifiers | KeyModifiers.Alt
                : CurrentModifiers & ~KeyModifiers.Alt,
            Key.LWin or Key.RWin when RuntimeInformation.IsOSPlatform(OSPlatform.OSX) => isDown
                ? CurrentModifiers | KeyModifiers.Meta
                : CurrentModifiers & ~KeyModifiers.Meta,
            _ => CurrentModifiers
        };
    }

    /// <summary>
    /// Checks if a key is a modifier key.
    /// </summary>
    private static bool IsModifierKey(Key key) => key switch
    {
        Key.LeftShift or Key.RightShift or
        Key.LeftCtrl or Key.RightCtrl or
        Key.LeftAlt or Key.RightAlt or
        Key.LWin or Key.RWin => true,
        _ => false
    };

    /// <summary>
    /// Handles debug-specific key commands.
    /// </summary>
    /// <returns>True if the key was handled as a debug key.</returns>
    private static bool HandleDebugKeys(Key key)
    {
#if DEBUG
        switch (key)
        {
            case Key.F12: // Show Avalonia DevTools in DEBUG mode
                return true;
            // Removed F9 and F7 as they were accessing static FunctionsMapper
        }
#endif
        return false;
    }

    /// <summary>
    /// Handles special cases like cropping, dialog handling, and escape key.
    /// </summary>
    /// <returns>True if the key event was handled by a special case handler.</returns>
    private static async ValueTask<bool> HandleSpecialCases(KeyEventArgs e, MainWindowViewModel? vm, MainWindow mainWindow)
    {
        // Handle cropping mode
        if (vm.WindowTabs.ActiveTab.CurrentValue.CropService is not null)
        {
            if (vm.WindowTabs.ActiveTab.CurrentValue.CropService.IsCropping)
            {
                if (e.Key is Key.Escape)
                {
                    vm.WindowTabs.ActiveTab.CurrentValue.CropService.CloseCropControl();
                }
                return true;
            }
        }
        
        if (vm.IsEditableTitlebarOpen.CurrentValue)
        {
            return true;
        }

        // Motion photo playback: Space plays/pauses, Escape stops an active playback
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is ImageViewer imageViewer &&
            imageViewer.IsMotionPhotoActive)
        {
            if (e.Key is Key.Space)
            {
                imageViewer.ToggleMotionPhotoPlayPause();
                return true;
            }

            if (e.Key is Key.Escape && imageViewer.StopMotionPhotoIfPlaying())
            {
                return true;
            }
        }

        // Handle open dialog
        if (mainWindow.IsDialogOpen)
        {
            if (e.Key is not Key.Escape)
            {
                return true;
            }

            if (vm.TopTitlebarViewModel.DropDownMenu.IsDropDownMenuVisible.CurrentValue)
            {
                vm.TopTitlebarViewModel.DropDownMenu.IsDropDownMenuVisible.Value = false;
                return true;
            }
                
            var animatedPopUp = mainWindow.UIHelper.GetMainView.MainPanel.Children.OfType<AnimatedPopUp>().FirstOrDefault();
            if (animatedPopUp is not null)
            {
                await animatedPopUp.AnimatedClosing();
            }

            return true;
        }
        
        // Handle escape key
        if (e.Key == Key.Escape)
        {
            if (vm.TopTitlebarViewModel.IsMainMenuVisible.CurrentValue)
            {
                vm.TopTitlebarViewModel.CloseMenu();
                return true;
            }
            
            if (Slideshow.IsRunning)
            {
                Slideshow.StopSlideshow(vm);
                return true;
            }
            
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { Windows.Count: > 1 } desktop)
            {
                 // Check if the current window (associated with vm) is one of the secondary windows
                 // This logic might need refinement to identify WHICH window is closing
                 // For now, mirroring legacy behavior of checking count
                desktop.Windows[^1].Close();
                IsKeyHeldDown = true; // If closing the last window, make sure not to call Close()
                return true;
            }

            if (!IsKeyHeldDown && IsEscKeyEnabled)
            {
                if (vm.Mapper != null)
                {
                    await vm.Mapper.Close();
                }
            }
        }
        // Don't interrupt navigating main menu with keyboard
        else if (vm.TopTitlebarViewModel.IsMainMenuVisible.CurrentValue)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Executes the registered shortcut action for the current key combination.
    /// </summary>
    private static async ValueTask ExecuteShortcutIfRegistered(MainWindowViewModel vm)
    {
        // Get the action string name quickly via dictionary lookup
        var actionName = KeybindingManager.GetActionName(CurrentKeys);
        if (string.IsNullOrEmpty(actionName))
        {
            // Pressed key(s) have no associated function
            return;
        }

        // Map the string to the instance-specific function using the view model's mapper
        var function = vm.Mapper.GetFunctionByName(actionName);
        if (function is not null)
        {
            await function.Invoke().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Resets the keyboard state tracking.
    /// </summary>
    public static void Reset()
    {
        IsKeyHeldDown = false;
        IsEscKeyEnabled = true;
        CurrentKeys = null;
        _keyRepeatCount = 0;
        ClearKeyDownModifiers();
    }

    /// <summary>
    /// Clears the states of all modifier keys.
    /// </summary>
    public static void ClearKeyDownModifiers()
    {
        CurrentModifiers = KeyModifiers.None;
    }
}
