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
    /// Stores the current keybind, including the key and its modifiers.
    /// </summary>
    public static Keybind CurrentKeys { get; internal set; }

    public static bool IsEscKeyEnabled { get; set; } = true;

    private static int _keyRepeatCount;
    private const int KeyRepeatThreshold = 1;
    
    public static bool ShiftDown => CurrentModifiers.HasFlag(KeyModifiers.Shift);

    /// <summary>
    /// Processes the KeyDown event for the main window.
    /// </summary>
    public static async ValueTask MainWindow_KeysDownAsync(KeyEventArgs e, MainWindowViewModel? mainWindowViewModel, MainWindow mainWindow)
    {
        if (Keymaps.Current.Bindings.Count is 0)
        {
            return;
        }

        var mode = InputModes.Of(mainWindow);

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
        CurrentKeys = new Keybind(e.Key, e.KeyModifiers);

        // Track key repeat for held down state
        _keyRepeatCount++;
        IsKeyHeldDown = _keyRepeatCount > KeyRepeatThreshold;

        // Handle special cases before processing shortcuts
        if (await HandleSpecialCases(e, mode, mainWindowViewModel, mainWindow).ConfigureAwait(false))
        {
            return;
        }
        
        // If it's a modifier key only, nothing more to do
        if (IsModifierKey(e.Key))
        {
            return;
        }

        // Handle registered shortcuts
        await ExecuteShortcutIfRegistered(mainWindowViewModel, mode).ConfigureAwait(false);
    }

    /// <summary>
    /// Processes the KeyUp event for the main window.
    /// </summary>
    /// <param name="e">The key event arguments.</param>
    /// <param name="mainWindowViewModel">The main window view model.</param>
    public static async ValueTask MainWindow_KeysUpAsync(KeyEventArgs e, MainWindowViewModel? mainWindowViewModel)
    {
        if (!OperatingSystem.IsMacOS())
        {
            if (CurrentKeys.Key is Key.LeftAlt or Key.RightAlt)
            {
                mainWindowViewModel.TopTitlebarViewModel.ToggleMenu();
            }
        }
        await mainWindowViewModel.Mapper.StopRepeatedNavigation().ConfigureAwait(false);
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
            Key.LWin or Key.RWin when OperatingSystem.IsMacOS() => isDown
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
    /// Handles the keys of the window's <see cref="InputMode"/>, and the escape key.
    /// </summary>
    /// <returns>True if the key event was handled by a special case handler.</returns>
    private static async ValueTask<bool> HandleSpecialCases(KeyEventArgs e, InputMode mode, MainWindowViewModel? vm,
        MainWindow mainWindow)
    {
        var activeTab = vm.WindowTabs.ActiveTab.CurrentValue;
        switch (mode)
        {
            case InputMode.Dialog:
                if (e.Key is Key.Escape)
                {
                    await CloseTopDialogAsync(vm, mainWindow).ConfigureAwait(false);
                }

                return true;

            case InputMode.Renaming:
                return true;

            case InputMode.Crop:
                switch (e.Key)
                {
                    case Key.Escape:
                        activeTab.CropService.CloseCropControl();
                        break;
                    case Key.Enter:
                        await activeTab.CropService.SaveCropAsync().ConfigureAwait(false);
                        break;
                    case Key.C:
                        // Cmd+C on macOS, Ctrl+C elsewhere
                        if (e.KeyModifiers == (OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control))
                        {
                            await activeTab.CropService.CopyCroppedImageAsync().ConfigureAwait(false);
                        }
                        break;
                }

                return true;

            // Motion photo playback: Space plays/pauses, Escape stops an active playback
            case InputMode.MotionPhoto when activeTab.CurrentView.CurrentValue is ImageViewer imageViewer:
                if (e.Key is Key.Space)
                {
                    imageViewer.ToggleMotionPhotoPlayPause();
                    return true;
                }

                if (e.Key is Key.Escape && imageViewer.StopMotionPhotoIfPlaying())
                {
                    return true;
                }

                break;
        }

        // Handle escape key
        if (e.Key == Key.Escape)
        {
            // With the UI hidden the main menu is not a dialog, but Escape still closes it
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
                    await vm.Mapper.Close().ConfigureAwait(false);
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Closes the drop-down menu, else the open pop-up, else the main menu.
    /// </summary>
    private static async ValueTask CloseTopDialogAsync(MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (vm.TopTitlebarViewModel.DropDownMenu?.IsDropDownMenuVisible.CurrentValue ?? false)
        {
            vm.TopTitlebarViewModel.CloseDropDownMenu();
            return;
        }

        var animatedPopUp = mainWindow.UIHelper.GetMainView?.MainPanel.Children.OfType<AnimatedPopUp>()
            .FirstOrDefault(popUp => popUp.IsVisible);
        if (animatedPopUp is not null)
        {
            await animatedPopUp.AnimatedClosing().ConfigureAwait(false);
            return;
        }

        vm.TopTitlebarViewModel.CloseMenu();
    }

    /// <summary>
    /// Executes the registered shortcut action for the current key combination, as it acts in <paramref name="mode"/>.
    /// </summary>
    internal static ValueTask ExecuteShortcutIfRegistered(MainWindowViewModel? vm, InputMode mode) =>
        Keymaps.ExecuteAsync(Keymaps.Resolve(CurrentKeys, mode), vm?.Mapper);

    /// <summary>
    /// Resets the keyboard state tracking.
    /// </summary>
    public static void Reset()
    {
        IsKeyHeldDown = false;
        IsEscKeyEnabled = true;
        CurrentKeys = default;
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
