using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Input;
using PicView.Avalonia.Crop;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;

namespace PicView.Avalonia.Input;

/// <summary>
/// Handles keyboard shortcuts and tracks key modifier states (Ctrl, Alt/Option, Shift, Command).
/// </summary>
public static class MainKeyboardShortcuts
{
    /// <summary>
    /// Indicates whether a key is held down.
    /// </summary>
    public static bool IsKeyHeldDown { get; private set; }

    /// <summary>
    /// Indicates whether the Ctrl key is pressed.
    /// </summary>
    public static bool CtrlDown { get; private set; }

    /// <summary>
    /// Indicates whether the Alt key (or Option key on macOS) is pressed.
    /// </summary>
    public static bool AltOrOptionDown { get; private set; }

    /// <summary>
    /// Indicates whether the Shift key is pressed.
    /// </summary>
    public static bool ShiftDown { get; private set; }

    /// <summary>
    /// Indicates whether the Command key (on macOS) is pressed.
    /// </summary>
    public static bool CommandDown { get; private set; }

    /// <summary>
    /// Stores the current key gesture, including the key and its modifiers.
    /// </summary>
    public static KeyGesture? CurrentKeys { get; private set; }

    /// <summary>
    /// Gets or sets whether keyboard shortcuts are enabled.
    /// </summary>
    public static bool IsKeysEnabled { get; set; } = true;

    private static ushort _x;

    /// <summary>
    /// Processes the KeyDown event for the main window, tracking which modifier keys are pressed and handling custom keyboard shortcuts.
    /// </summary>
    /// <param name="e">The key event arguments, containing information about the key that was pressed.</param>
    public static async Task MainWindow_KeysDownAsync(KeyEventArgs e)
    {
        if (KeybindingManager.CustomShortcuts is null || !IsKeysEnabled)
        {
            return;
        }
        
        switch (e.Key)
        {
#if DEBUG
            case Key.F12:
                // Show Avalonia DevTools in DEBUG mode
                return;
            case Key.F8:
                await FunctionsHelper.Invalidate();
                return;
            case Key.F9:
                await FunctionsHelper.ShowStartUpMenu();
                return;
#endif

            case Key.LeftShift:
            case Key.RightShift:
                ShiftDown = true;
                return;
            case Key.LeftCtrl:
            case Key.RightCtrl:
                CtrlDown = true;
                return;
            case Key.LeftAlt:
            case Key.RightAlt:
                AltOrOptionDown = true;
                return;
            case Key.LWin:
            case Key.RWin:
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    CommandDown = true;
                }
                return;
        }

        // Build the key gesture based on the pressed modifiers
        if (CtrlDown || AltOrOptionDown || ShiftDown || CommandDown)
        {
            var modifiers = KeyModifiers.None;

            if (CtrlDown) modifiers |= KeyModifiers.Control;
            if (AltOrOptionDown) modifiers |= KeyModifiers.Alt;
            if (ShiftDown) modifiers |= KeyModifiers.Shift;
            if (CommandDown && RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) modifiers |= KeyModifiers.Meta;

            CurrentKeys = new KeyGesture(e.Key, modifiers);
        }
        else
        {
            CurrentKeys = new KeyGesture(e.Key);
        }
        
        // Handle custom keybindings
        if (_x >= ushort.MaxValue - 1)
        {
            _x = 1;
        }
        _x++;
        IsKeyHeldDown = _x > 1;
        
        if (CropFunctions.IsCropping)
        {
            await UIHelper.GetMainView.MainGrid.Children.OfType<CropControl>().FirstOrDefault().KeyDownHandler(null,e);
            return;
        }

        if (UIHelper.IsDialogOpen)
        {
            UIHelper.GetMainView.MainGrid.Children.OfType<AnimatedPopUp>().FirstOrDefault().KeyDownHandler(null,e);
            return;
        }

        if (e.Key == Key.Escape)
        {
            await FunctionsHelper.Close().ConfigureAwait(false);
            return;
        }

        if (KeybindingManager.CustomShortcuts.TryGetValue(CurrentKeys, out var func))
        {
            if (func is null)
            {
                // TODO: Display error to user
#if DEBUG
                Trace.WriteLine($"[{nameof(MainWindow_KeysDownAsync)}] error \n{e}");
#endif
                return;
            }
            // Execute the associated action
            await func.Invoke().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Resets the state of the modifier keys and current key gesture.
    /// </summary>
    private static void Reset()
    {
        ClearKeyDownModifiers();
        IsKeyHeldDown = false;
        CurrentKeys = null;
        _x = 0;
    }

    /// <summary>
    /// Processes the KeyUp event for the main window, resetting modifier states when keys are released.
    /// </summary>
    /// <param name="e">The key event arguments, containing information about the key that was released.</param>
    public static void MainWindow_KeysUp(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.LeftShift:
            case Key.RightShift:
                ShiftDown = false;
                break;
            case Key.LeftCtrl:
            case Key.RightCtrl:
                CtrlDown = false;
                break;
            case Key.LeftAlt:
            case Key.RightAlt:
                AltOrOptionDown = false;
                break;
            case Key.LWin:
            case Key.RWin:
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    CommandDown = false;
                }
                break;
        }

        Reset();
    }

    /// <summary>
    /// Clears the states of all modifier keys (Ctrl, Alt/Option, Shift, Command).
    /// </summary>
    public static void ClearKeyDownModifiers()
    {
        CtrlDown = false;
        AltOrOptionDown = false;
        ShiftDown = false;
        CommandDown = false;
    }
}
