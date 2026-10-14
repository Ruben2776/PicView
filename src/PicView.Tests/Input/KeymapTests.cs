using Avalonia.Input;
using PicView.Avalonia.Input;
using PicView.Core.Favorites;
using MouseButton = PicView.Avalonia.Input.MouseButton;

namespace PicView.Tests.Input;

public class KeymapTests
{
    private const string Defaults = """
                                    {
                                      "Right": "Next",
                                      "Left": "Prev",
                                      "Ctrl+F": "ToggleFullscreen",
                                    }
                                    """;

    [Fact]
    public void FromJson_WithoutUserFile_ResolvesDefaults()
    {
        var keymap = Keymap.FromJson(null, Defaults);

        Assert.Equal(KeymapResolution.Action("Next"), keymap.Resolve(new Keybind(Key.Right)));
        Assert.Equal(KeymapResolution.Action("ToggleFullscreen"),
            keymap.Resolve(new Keybind(Key.F, KeyModifiers.Control)));
        Assert.Equal(KeymapResolution.None, keymap.Resolve(new Keybind(Key.Up)));
    }

    [Fact]
    public void FromJson_UserFileWithAlias_ResolvesCanonicalAction()
    {
        var keymap = Keymap.FromJson("""{ "F11": "Fullscreen" }""", Defaults);

        Assert.Equal(KeymapResolution.Action("ToggleFullscreen"), keymap.Resolve(new Keybind(Key.F11)));
    }

    [Fact]
    public void FromJson_MalformedUserFile_FallsBackToDefaults()
    {
        var keymap = Keymap.FromJson("""{ "Right": "Next" """, Defaults);

        Assert.Equal(KeymapResolution.Action("Prev"), keymap.Resolve(new Keybind(Key.Left)));
    }

    [Fact]
    public void FromJson_UnparsableKeys_AreSkipped()
    {
        var keymap = Keymap.FromJson("""{ "Nonsense": "Next", "Ctrl+Nonsense": "Last", "Right": "Prev" }""", Defaults);

        Assert.Equal(KeymapResolution.None, keymap.Resolve(default));
        Assert.Equal(KeymapResolution.None, keymap.Resolve(new Keybind(Key.None, KeyModifiers.Control)));
        Assert.Equal(KeymapResolution.Action("Prev"), keymap.Resolve(new Keybind(Key.Right)));
    }

    [Fact]
    public void Assign_KeybindOwnedByAnotherAction_TakesItAndLeavesOriginalUnchanged()
    {
        var right = new Keybind(Key.Right);
        var original = Keymap.FromJson(null, Defaults);

        var updated = original.Assign(right, "Last");

        Assert.Equal(KeymapResolution.Action("Last"), updated.Resolve(right));
        Assert.Equal([right], updated.KeybindsFor("Last"));
        Assert.Empty(updated.KeybindsFor("Next"));
        Assert.Equal(KeymapResolution.Action("Next"), original.Resolve(right));
    }

    [Fact]
    public void Unassign_FreesKeybind()
    {
        var right = new Keybind(Key.Right);

        var updated = Keymap.FromJson(null, Defaults).Unassign(right);

        Assert.Equal(KeymapResolution.None, updated.Resolve(right));
        Assert.Empty(updated.KeybindsFor("Next"));
    }

    [Fact]
    public void WithFavorites_ResolvesFavoriteKeybindToItsFolder()
    {
        var keymap = Keymap.FromJson(null, Defaults)
            .WithFavorites([new FavoriteEntry("/pics", "Ctrl+D1"), new FavoriteEntry("/docs", "")]);

        Assert.Equal(KeymapResolution.Favorite("/pics"),
            keymap.Resolve(new Keybind(Key.D1, KeyModifiers.Control)));
        Assert.Equal(KeymapResolution.Action("Next"), keymap.Resolve(new Keybind(Key.Right)));
    }

    [Fact]
    public void WithFavorites_FavoriteKeybindOwnedByAction_TakesItFromTheAction()
    {
        var right = new Keybind(Key.Right);

        var keymap = Keymap.FromJson(null, Defaults).WithFavorites([new FavoriteEntry("/pics", "Right")]);

        Assert.Equal(KeymapResolution.Favorite("/pics"), keymap.Resolve(right));
        Assert.Empty(keymap.KeybindsFor("Next"));
        Assert.Equal(KeymapResolution.None, Keymap.FromJson(keymap.ToJson(), Defaults).Resolve(right));
    }

    [Fact]
    public void Assign_KeybindOwnedByFavorite_TakesItFromTheFavorite()
    {
        var right = new Keybind(Key.Right);
        var keymap = Keymap.FromJson(null, Defaults).WithFavorites([new FavoriteEntry("/pics", "Right")]);

        var updated = keymap.Assign(right, "Last");

        Assert.Equal(KeymapResolution.Action("Last"), updated.Resolve(right));
    }

    [Fact]
    public void ToJson_RoundTripsAllBindings_IncludingUnknownActions()
    {
        var keymap = Keymap.FromJson(
            """{ "F9": "MadeUpAction", "Ctrl+Shift+Alt+A": "Next", "WheelDown": "Prev" }""", Defaults);

        var reloaded = Keymap.FromJson(keymap.ToJson(), Defaults);

        Assert.Equal(KeymapResolution.Action("MadeUpAction"), reloaded.Resolve(new Keybind(Key.F9)));
        Assert.Equal(KeymapResolution.Action("Next"),
            reloaded.Resolve(new Keybind(Key.A, KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt)));
        Assert.Equal(KeymapResolution.Action("Prev"), reloaded.Resolve(new Keybind(MouseButton.WheelDown)));
    }
}
