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

    public static async ValueTask DispatchAsync(
        Keybind keybind,
        InputModeState state,
        Keymap keymap,
        IContextualActionHandler handler,
        Func<KeymapResolution, ValueTask> runOrdinary)
    {
        if (ContextualActionOf(keybind, keymap) is { } contextualAction)
        {
            foreach (var mode in state.ContextualChain())
            {
                // Stay on the UI thread: modes and ordinary Actions work on the window
                if (await handler.TryRunAsync(mode, contextualAction).ConfigureAwait(false))
                {
                    return;
                }
            }
        }

        var resolution = keymap.Resolve(keybind, state.Resolve());
        if (resolution.Owner is not KeymapOwner.None)
        {
            await runOrdinary(resolution).ConfigureAwait(false);
        }
    }
}
