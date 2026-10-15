using Avalonia.Input;
using PicView.Avalonia.Input;

namespace PicView.Tests.Input;

public class ContextualKeymapTests
{
    private static readonly Keybind Enter = new(Key.Enter);
    private static readonly Keybind Escape = new(Key.Escape);
    private static readonly Keybind Space = new(Key.Space);

    private const string Defaults = """
                                    {
                                      "Right": "Next",
                                      "Enter": "ToggleFullscreen",
                                      "Contextual": {
                                        "Enter": "Confirm",
                                        "Escape": "Cancel"
                                      }
                                    }
                                    """;

    [Fact]
    public void FromJson_ContextualSection_BindsContextualActions()
    {
        var keymap = Keymap.FromJson(null, Defaults);

        Assert.Equal("Confirm", keymap.ResolveContextual(Enter));
        Assert.Equal("Cancel", keymap.ResolveContextual(Escape));
        Assert.Null(keymap.ResolveContextual(new Keybind(Key.Right)));
    }

    [Fact]
    public void FromJson_SameKeybindInBothLayers_KeepsBoth()
    {
        var keymap = Keymap.FromJson(null, Defaults);

        Assert.Equal(KeymapResolution.Action("ToggleFullscreen"), keymap.Resolve(Enter));
        Assert.Equal("Confirm", keymap.ResolveContextual(Enter));
    }

    [Fact]
    public void FromJson_UserFileWithoutContextualSection_UsesDefaultContextualKeybinds()
    {
        var keymap = Keymap.FromJson("""{ "Left": "Prev" }""", Defaults);

        Assert.Equal(KeymapResolution.Action("Prev"), keymap.Resolve(new Keybind(Key.Left)));
        Assert.Equal(KeymapResolution.None, keymap.Resolve(new Keybind(Key.Right)));
        Assert.Equal("Confirm", keymap.ResolveContextual(Enter));
    }

    [Fact]
    public void FromJson_UserFileWithEmptyContextualSection_HasNoContextualKeybinds()
    {
        var keymap = Keymap.FromJson("""{ "Left": "Prev", "Contextual": {} }""", Defaults);

        Assert.Empty(keymap.ContextualBindings);
    }

    [Fact]
    public void FromJson_ContextualActionInOrdinarySection_GoesToContextualLayer()
    {
        var keymap = Keymap.FromJson("""{ "Space": "PlayPause", "Contextual": {} }""", "{}");

        Assert.Equal(KeymapResolution.None, keymap.Resolve(Space));
        Assert.Equal("PlayPause", keymap.ResolveContextual(Space));
    }

    [Fact]
    public void Assign_ContextualAction_DoesNotTakeOrdinaryKeybind()
    {
        var keymap = Keymap.FromJson(null, Defaults).Assign(new Keybind(Key.Right), "PlayPause");

        Assert.Equal(KeymapResolution.Action("Next"), keymap.Resolve(new Keybind(Key.Right)));
        Assert.Equal("PlayPause", keymap.ResolveContextual(new Keybind(Key.Right)));
    }

    [Fact]
    public void Assign_OrdinaryAction_DoesNotTakeContextualKeybind()
    {
        var keymap = Keymap.FromJson(null, Defaults).Assign(Escape, "Close");

        Assert.Equal(KeymapResolution.Action("Close"), keymap.Resolve(Escape));
        Assert.Equal("Cancel", keymap.ResolveContextual(Escape));
    }

    [Fact]
    public void Assign_ContextualKeybindOfAnotherContextualAction_TakesIt()
    {
        var keymap = Keymap.FromJson(null, Defaults).Assign(Enter, "PlayPause");

        Assert.Equal("PlayPause", keymap.ResolveContextual(Enter));
        Assert.Empty(keymap.KeybindsFor("Confirm"));
    }

    [Fact]
    public void WithKeybinds_ContextualAction_LeavesOrdinaryLayerAlone()
    {
        var keymap = Keymap.FromJson(null, Defaults).WithKeybinds("Confirm", [Space]);

        Assert.Equal([Space], keymap.KeybindsFor("Confirm"));
        Assert.Null(keymap.ResolveContextual(Enter));
        Assert.Equal(KeymapResolution.Action("ToggleFullscreen"), keymap.Resolve(Enter));
    }

    [Fact]
    public void ToJson_RoundTripsBothLayers()
    {
        var keymap = Keymap.FromJson(null, Defaults);

        var json = keymap.ToJson();
        var reloaded = Keymap.FromJson(json, "{}");

        Assert.Contains("\"Contextual\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u002B", json, StringComparison.Ordinal);
        Assert.Equal(keymap, reloaded);
    }

    [Fact]
    public void Equals_ComparesContextualLayer()
    {
        var keymap = Keymap.FromJson(null, Defaults);

        Assert.NotEqual(keymap, keymap.Assign(Space, "PlayPause"));
    }
}
