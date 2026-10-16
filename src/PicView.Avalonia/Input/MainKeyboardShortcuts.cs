using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using PicView.Avalonia.CustomControls;
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

        var state = InputModes.StateOf(mainWindow);

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

        // If it's a modifier key only, nothing more to do
        if (IsModifierKey(e.Key) || mainWindowViewModel is null)
        {
            return;
        }

        // A text box or Keybind box in a dialog keeps the keys it edits with, such as a Confirm bound to a letter
        if (state.Resolve() is InputMode.Dialog &&
            FocusedInput.OwnsKeybind(mainWindow.FocusManager?.GetFocusedElement(), CurrentKeys))
        {
            return;
        }

        await ExecuteShortcutIfRegistered(mainWindowViewModel, state,
            new ContextualActionHandler(mainWindow, mainWindowViewModel)).ConfigureAwait(false);
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
    /// What Escape does when nothing else claims it: closes the main menu (open while the UI is hidden),
    /// else the newest secondary window, else this window unless Escape is held down or disabled.
    /// </summary>
    internal static async ValueTask CloseOnEscapeAsync(MainWindowViewModel vm)
    {
        if (vm.TopTitlebarViewModel.IsMainMenuVisible.CurrentValue)
        {
            vm.TopTitlebarViewModel.CloseMenu();
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { Windows.Count: > 1 } desktop)
        {
            // Check if the current window (associated with vm) is one of the secondary windows
            // This logic might need refinement to identify WHICH window is closing
            // For now, mirroring legacy behavior of checking count
            desktop.Windows[^1].Close();
            IsKeyHeldDown = true; // If closing the last window, make sure not to call Close()
            return;
        }

        if (!IsKeyHeldDown && IsEscKeyEnabled && vm.Mapper is not null)
        {
            await vm.Mapper.Close().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Runs what the current key combination does in a window in <paramref name="state"/>.
    /// </summary>
    internal static ValueTask ExecuteShortcutIfRegistered(MainWindowViewModel? vm, InputModeState state,
        IContextualActionHandler handler) =>
        InputDispatch.DispatchAsync(CurrentKeys, state, Keymaps.Current, handler,
            resolution => Keymaps.ExecuteAsync(resolution, vm?.Mapper));

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
