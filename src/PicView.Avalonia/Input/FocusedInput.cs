using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.Input;

/// <summary>
/// Decides which keys a focused input control in a dialog keeps for itself, instead of the window
/// treating them as Keybinds.
/// </summary>
public static class FocusedInput
{
    private const KeyModifiers ChordModifiers = KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta;

    /// <summary>
    /// Whether <paramref name="focused"/> uses <paramref name="keybind"/> itself: a recording
    /// <see cref="KeybindBox"/> records every key, and a text box edits text with every key except Enter, Escape, and
    /// chords, which stay Confirm, Cancel, and the window's Keybinds.
    /// </summary>
    public static bool OwnsKeybind(IInputElement? focused, Keybind keybind)
    {
        if (focused is Visual visual &&
            visual.GetSelfAndVisualAncestors().OfType<KeybindBox>().FirstOrDefault() is { } keybindBox)
        {
            return keybindBox.IsRecording;
        }

        if (focused is not TextBox textBox || (keybind.Modifiers & ChordModifiers) != KeyModifiers.None)
        {
            return false;
        }

        return keybind.Key switch
        {
            Key.Enter => textBox.AcceptsReturn,
            Key.Escape => false,
            _ => true
        };
    }
}
