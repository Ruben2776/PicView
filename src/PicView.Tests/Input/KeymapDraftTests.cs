using Avalonia.Input;
using PicView.Avalonia.Input;

namespace PicView.Tests.Input;

public class KeymapDraftTests
{
    private static readonly Keybind Right = new(Key.Right);
    private static readonly Keybind Left = new(Key.Left);
    private static readonly Keybind F7 = new(Key.F7);

    private static readonly Keymap Saved = Keymap.FromJson("""{ "Right": "Next", "Left": "Prev" }""", "{}");
    private static readonly Keymap Defaults = Keymap.FromJson(null, """{ "Right": "Prev" }""");

    private static KeymapDraft CreateDraft() => new(Saved, Defaults);

    [Fact]
    public void New_IsCleanAndHasNoHistory()
    {
        var draft = CreateDraft();

        Assert.Equal(Saved, draft.Current);
        Assert.False(draft.IsDirty);
        Assert.False(draft.CanUndo);
        Assert.False(draft.CanRedo);
    }

    [Fact]
    public void SetKeybinds_TakesKeybindFromOtherActionAndMarksDirty()
    {
        var draft = CreateDraft();

        var changed = draft.SetKeybinds("Prev", [Right]);

        Assert.True(changed);
        Assert.True(draft.IsDirty);
        Assert.Empty(draft.Current.KeybindsFor("Next"));
        Assert.Equal([Right], draft.Current.KeybindsFor("Prev"));
    }

    [Fact]
    public void SetKeybinds_ThatChangesNothing_DoesNotPushUndo()
    {
        var draft = CreateDraft();

        var changed = draft.SetKeybinds("Next", [Right]);

        Assert.False(changed);
        Assert.False(draft.CanUndo);
    }

    [Fact]
    public void Undo_AfterEdit_RestoresPreviousDraft()
    {
        var draft = CreateDraft();
        draft.SetKeybinds("Prev", [Right]);

        Assert.True(draft.Undo());

        Assert.Equal(Saved, draft.Current);
        Assert.False(draft.IsDirty);
        Assert.True(draft.CanRedo);
    }

    [Fact]
    public void Redo_AfterUndo_ReappliesEdit()
    {
        var draft = CreateDraft();
        draft.SetKeybinds("Prev", [Right]);
        var edited = draft.Current;
        draft.Undo();

        Assert.True(draft.Redo());

        Assert.Equal(edited, draft.Current);
        Assert.False(draft.CanRedo);
    }

    [Fact]
    public void Edit_AfterUndo_ClearsRedo()
    {
        var draft = CreateDraft();
        draft.SetKeybinds("Prev", [Right]);
        draft.Undo();

        draft.SetKeybinds("Next", [F7]);

        Assert.False(draft.CanRedo);
        Assert.False(draft.Redo());
    }

    [Fact]
    public void Undo_And_Redo_WithEmptyHistory_ReturnFalse()
    {
        var draft = CreateDraft();

        Assert.False(draft.Undo());
        Assert.False(draft.Redo());
    }

    [Fact]
    public void ResetToDefaults_KeepsFavoriteKeybinds()
    {
        var draft = new KeymapDraft(Saved.Assign(F7, "OpenFavorite1"), Defaults);

        Assert.True(draft.ResetToDefaults());

        Assert.Equal([F7], draft.Current.KeybindsFor("OpenFavorite1"));
        Assert.Equal([Right], draft.Current.KeybindsFor("Prev"));
        Assert.Empty(draft.Current.KeybindsFor("Next"));
        Assert.True(draft.IsDefault);
    }

    [Fact]
    public void ResetToDefaults_IsUndoable()
    {
        var draft = CreateDraft();
        draft.ResetToDefaults();

        draft.Undo();

        Assert.Equal(Saved, draft.Current);
    }

    [Fact]
    public void Revert_ReturnsToSavedAndIsUndoable()
    {
        var draft = CreateDraft();
        draft.SetKeybinds("Prev", [Right]);
        var edited = draft.Current;

        Assert.True(draft.Revert());
        Assert.Equal(Saved, draft.Current);
        Assert.False(draft.IsDirty);

        draft.Undo();
        Assert.Equal(edited, draft.Current);
    }

    [Fact]
    public void Revert_WhenClean_ReturnsFalse()
    {
        Assert.False(CreateDraft().Revert());
    }

    [Fact]
    public void MarkSaved_MakesCurrentTheSavedKeymapAndClearsHistory()
    {
        var draft = CreateDraft();
        draft.SetKeybinds("Prev", [Right]);

        draft.MarkSaved();

        Assert.False(draft.IsDirty);
        Assert.False(draft.CanUndo);
        Assert.False(draft.CanRedo);
        Assert.Equal(draft.Current, draft.Saved);
        Assert.Equal([Left], Saved.KeybindsFor("Prev"));
    }
}
