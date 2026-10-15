using PicView.Core.Keybindings;

namespace PicView.Avalonia.Input;

/// <summary>
/// An editable copy of a <see cref="Keymap"/> with undo/redo. Nothing reaches the live keymap until
/// <see cref="Current"/> is given to <see cref="Keymaps.Replace"/>.
/// </summary>
public sealed class KeymapDraft(Keymap saved, Keymap defaults)
{
    private readonly List<Keymap> _undoStack = [];
    private readonly List<Keymap> _redoStack = [];

    /// <summary>
    /// The keymap the draft was last saved as.
    /// </summary>
    public Keymap Saved { get; private set; } = saved;

    public Keymap Current { get; private set; } = saved;

    public bool IsDirty => !Current.Equals(Saved);

    public bool IsDefault => Current.Equals(DefaultsKeepingFavorites());

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Gives <paramref name="action"/> exactly <paramref name="keybinds"/>, taking them from any other action.
    /// </summary>
    /// <returns><c>true</c> when the draft changed.</returns>
    public bool SetKeybinds(string action, IReadOnlyCollection<Keybind> keybinds) =>
        Push(Current.WithKeybinds(action, keybinds));

    /// <summary>
    /// Returns to the saved keymap.
    /// </summary>
    /// <returns><c>true</c> when the draft changed.</returns>
    public bool Revert() => Push(Saved);

    /// <summary>
    /// Returns to the defaults, keeping the draft's favorite Keybinds.
    /// </summary>
    /// <returns><c>true</c> when the draft changed.</returns>
    public bool ResetToDefaults() => Push(DefaultsKeepingFavorites());

    /// <returns><c>true</c> when there was an edit to undo.</returns>
    public bool Undo() => Step(_undoStack, _redoStack);

    /// <returns><c>true</c> when there was an edit to redo.</returns>
    public bool Redo() => Step(_redoStack, _undoStack);

    /// <summary>
    /// Makes <see cref="Current"/> the saved keymap and clears the history.
    /// </summary>
    public void MarkSaved()
    {
        Saved = Current;
        _undoStack.Clear();
        _redoStack.Clear();
    }

    private bool Push(Keymap next)
    {
        if (next.Equals(Current))
        {
            return false;
        }

        _undoStack.Add(Current);
        _redoStack.Clear();
        Current = next;
        return true;
    }

    private bool Step(List<Keymap> from, List<Keymap> to)
    {
        if (from.Count == 0)
        {
            return false;
        }

        to.Add(Current);
        Current = from[^1];
        from.RemoveAt(from.Count - 1);
        return true;
    }

    private Keymap DefaultsKeepingFavorites()
    {
        var keymap = defaults;
        foreach (var action in KeybindCategoryDefinition.FavoriteActionNames)
        {
            keymap = keymap.WithKeybinds(action, Current.KeybindsFor(action));
        }

        return keymap;
    }
}
