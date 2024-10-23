using System.Diagnostics;
using Avalonia.Input;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.Input;

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
    /// Indicates whether the Alt key is pressed.
    /// </summary>
    public static bool AltDown { get; private set; }

    /// <summary>
    /// Indicates whether the Shift key is pressed.
    /// </summary>
    public static bool ShiftDown { get; private set; }

    public static KeyGesture? CurrentKeys { get; private set; }
    
    public static bool IsKeysEnabled { get; set; } = true;

    private static ushort _x;

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
            
            case Key.Escape:
                await FunctionsHelper.Close().ConfigureAwait(false);
                return;

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
                AltDown = true;
                return;
            case Key.LWin:
            case Key.RWin:
                return;
        }

        if (CtrlDown)
        {
            if (ShiftDown)
            {
                if (AltDown)
                {
                    CurrentKeys = new KeyGesture(e.Key, KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Shift);
                }
                else
                {
                    CurrentKeys = new KeyGesture(e.Key, KeyModifiers.Control | KeyModifiers.Shift);
                }
            }
            if (AltDown)
            {
                CurrentKeys = new KeyGesture(e.Key, KeyModifiers.Alt | KeyModifiers.Control);
            }
            else
            {
                CurrentKeys = new KeyGesture(e.Key, KeyModifiers.Control);
            }
        }
        else if (AltDown)
        {
            if (CtrlDown)
            {
                if (ShiftDown)
                {
                    CurrentKeys = new KeyGesture(e.Key, KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt);
                }
                else
                {
                    CurrentKeys = new KeyGesture(e.Key, KeyModifiers.Shift | KeyModifiers.Alt);
                }
            }

            if (ShiftDown)
            {
                CurrentKeys = new KeyGesture(e.Key, KeyModifiers.Shift | KeyModifiers.Alt);
            }
            else
            {
                CurrentKeys = new KeyGesture(e.Key, KeyModifiers.Alt);
            }
        }
        else if (ShiftDown)
        {
            if (CtrlDown)
            {
                if (AltDown)
                {
                    CurrentKeys = new KeyGesture(e.Key, KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt);
                }
                else
                {
                    CurrentKeys = new KeyGesture(e.Key, KeyModifiers.Control | KeyModifiers.Shift);
                }
            }

            if (AltDown)
            {
                CurrentKeys = new KeyGesture(e.Key, KeyModifiers.Shift | KeyModifiers.Alt);
            }
            else
            {
                CurrentKeys = new KeyGesture(e.Key, KeyModifiers.Shift);
            }
        }
        else
        {
            CurrentKeys = new KeyGesture(e.Key);
        }
        
        // Note Ctrl + Alt + key is sometimes not working properly

        if (_x >= ushort.MaxValue - 1)
        {
            _x = 1;
        }
        _x++;
        IsKeyHeldDown = _x > 1;

        if (KeybindingManager.CustomShortcuts.TryGetValue(CurrentKeys, out var func))
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (func is null)
            {
                try
                {
                    await KeybindingManager.SetDefaultKeybindings().ConfigureAwait(false);
                    if (KeybindingManager.CustomShortcuts.TryGetValue(CurrentKeys, out var retryFunc))
                    {
                        await retryFunc.Invoke().ConfigureAwait(false);
                    }
                    else
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    // TODO: Display error to user
#if DEBUG
                    Trace.WriteLine($"[{nameof(MainWindow_KeysDownAsync)}] error \n{e}");
#endif
                }
                return;
            }
            // Execute the associated action
            await func.Invoke().ConfigureAwait(false);
            ClearKeyDownModifiers();
        }
    }

    private static void Reset()
    {
        ClearKeyDownModifiers();
        IsKeyHeldDown = false;
        CurrentKeys = null;
        _x = 0;
    }

    public static void MainWindow_KeysUp(KeyEventArgs e)
    {
        Reset();
    }

    public static void ClearKeyDownModifiers()
    {
        CtrlDown = false;
        AltDown = false;
        ShiftDown = false;
    }
}