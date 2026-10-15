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
    public static async ValueTask DispatchAsync(
        Keybind keybind,
        InputModeState state,
        Keymap keymap,
        IContextualActionHandler handler,
        Func<KeymapResolution, ValueTask> runOrdinary)
    {
        if (keymap.ResolveContextual(keybind) is { } contextualAction)
        {
            foreach (var mode in state.ContextualChain())
            {
                // Stay on the UI thread: modes and ordinary Actions work on the window
                if (await handler.TryRunAsync(mode, contextualAction))
                {
                    return;
                }
            }
        }

        var resolution = keymap.Resolve(keybind, state.Resolve());
        if (resolution.Owner is not KeymapOwner.None)
        {
            await runOrdinary(resolution);
        }
    }
}
