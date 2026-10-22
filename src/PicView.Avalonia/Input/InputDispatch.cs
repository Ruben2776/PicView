using Avalonia.Input;

namespace PicView.Avalonia.Input;

/// <summary>
/// Runs a Contextual Action in an Input Mode, reporting whether the mode gave it a meaning.
/// </summary>
public interface IContextualActionHandler
{
    /// <returns><c>false</c> when <paramref name="mode"/> gives <paramref name="contextualAction"/> no meaning.</returns>
    ValueTask<bool> TryRunAsync(InputMode mode, string contextualAction);
}

/// <summary>
/// Decides what a Keybind does in a window: its Contextual Action when a mode claims it, else its ordinary Action
/// unless the window is in a blocking Input Mode.
/// </summary>
public static class InputDispatch
{
    /// <summary>
    /// The platform's copy chord: Cmd+C on macOS, Ctrl+C elsewhere. It is not bindable; it acts as the fixed
    /// <see cref="ContextualActionHandler.Copy"/> that a mode may claim, such as copying the crop, and otherwise
    /// falls through to its ordinary Action.
    /// </summary>
    public static Keybind CopyKeybind { get; } =
        new(Key.C, OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control);

    /// <summary>
    /// Escape without modifiers. It is not bindable; it acts as the fixed <see cref="ContextualActionHandler.Cancel"/>.
    /// </summary>
    public static Keybind CancelKeybind { get; } = new(Key.Escape);

    /// <summary>
    /// Returns the Contextual Action <paramref name="keybind"/> stands for: the fixed one of the copy or cancel
    /// chord, else the one it is bound to in <paramref name="keymap"/>.
    /// </summary>
    private static string? ContextualActionOf(Keybind keybind, Keymap keymap)
    {
        if (keybind == CancelKeybind)
        {
            return ContextualActionHandler.Cancel;
        }

        return keybind == CopyKeybind ? ContextualActionHandler.Copy : keymap.ResolveContextual(keybind);
    }

    /// <summary>
    /// Runs the Contextual Action <paramref name="keybind"/> stands for when an Input Mode gives it a meaning.
    /// </summary>
    /// <returns>The ordinary Action to run, or <see cref="KeymapResolution.None"/> when a mode claimed
    /// <paramref name="keybind"/> or a blocking mode blocks it.</returns>
    public static async ValueTask<KeymapResolution> DispatchAsync(
        Keybind keybind,
        InputModeState state,
        Keymap keymap,
        IContextualActionHandler handler)
    {
        var mode = state.Resolve();
        if (ContextualActionOf(keybind, keymap) is { } contextualAction &&
            await ClaimAsync(contextualAction, mode, state, handler).ConfigureAwait(true))
        {
            return KeymapResolution.None;
        }

        return keymap.Resolve(keybind, mode);
    }

    /// <summary>
    /// Asks a blocking <paramref name="mode"/> alone, else every active non-blocking mode from highest rank down,
    /// then <see cref="InputMode.Normal"/>, whether it gives <paramref name="contextualAction"/> a meaning.
    /// </summary>
    private static async ValueTask<bool> ClaimAsync(
        string contextualAction,
        InputMode mode,
        InputModeState state,
        IContextualActionHandler handler)
    {
        // Stay on the UI thread: modes and ordinary Actions work on the window
        if (mode.IsBlocking())
        {
            return await handler.TryRunAsync(mode, contextualAction).ConfigureAwait(true);
        }

        if (state.IsMotionPhotoActive && await handler.TryRunAsync(InputMode.MotionPhoto, contextualAction).ConfigureAwait(true))
        {
            return true;
        }

        if (state.IsAnimatedImageActive && await handler.TryRunAsync(InputMode.AnimatedImage, contextualAction).ConfigureAwait(true))
        {
            return true;
        }

        if (state.IsSlideshowRunning && await handler.TryRunAsync(InputMode.Slideshow, contextualAction).ConfigureAwait(true))
        {
            return true;
        }

        return await handler.TryRunAsync(InputMode.Normal, contextualAction).ConfigureAwait(true);
    }
}
