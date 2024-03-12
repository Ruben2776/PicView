using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Core.Config;

namespace PicView.Avalonia.Keybindings;

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

    /// <summary>
    /// Gets the currently pressed key.
    /// </summary>
    public static Key CurrentKey { get; private set; }

    private static short _x;

    public static async Task MainWindow_KeysDownAsync(KeyEventArgs e)
    {
        _x++;
        CtrlDown = e.KeyModifiers == KeyModifiers.Control;
        AltDown = e.KeyModifiers == KeyModifiers.Alt;
        ShiftDown = e.KeyModifiers == KeyModifiers.Shift;
        CurrentKey = e.Key;
        IsKeyHeldDown = _x > 1;

        if (KeybindingsHelper.CustomShortcuts is null)
        {
            return;
        }

        if (KeybindingsHelper.CustomShortcuts.TryGetValue(CurrentKey, out var func))
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (func is null)
            {
                try
                {
                    await KeybindingsHelper.SetDefaultKeybindings().ConfigureAwait(false);
                    // ReSharper disable once TailRecursiveCall
                    await MainWindow_KeysDownAsync(e).ConfigureAwait(false);
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
        }
        else
        {
            //await UIHelper.CheckModifierFunctionAsync().ConfigureAwait(false);
        }
    }

    public static async Task MainWindow_KeysUpAsync(KeyEventArgs e)
    {
        CtrlDown = false;
        AltDown = false;
        ShiftDown = false;
        CurrentKey = e.Key;
        IsKeyHeldDown = false;
        _x = 0;
    }
}