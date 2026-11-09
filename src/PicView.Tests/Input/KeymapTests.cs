using Avalonia.Input;
using PicView.Avalonia.Input;
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
    public void WithKeybinds_ReplacesActionsKeybindsAndTakesThemFromOtherActions()
    {
        var right = new Keybind(Key.Right);
        var f7 = new Keybind(Key.F7);
        var keymap = Keymap.FromJson(null, Defaults).Assign(f7, "Prev");

        var updated = keymap.WithKeybinds("Prev", [right]);

        Assert.Equal([right], updated.KeybindsFor("Prev"));
        Assert.Empty(updated.KeybindsFor("Next"));
        Assert.Equal(KeymapResolution.None, updated.Resolve(new Keybind(Key.Left)));
        Assert.Equal(KeymapResolution.None, updated.Resolve(f7));
    }

    [Fact]
    public void WithKeybinds_Empty_UnassignsEveryKeybindOfAction()
    {
        var updated = Keymap.FromJson(null, Defaults).WithKeybinds("Next", []);

        Assert.Empty(updated.KeybindsFor("Next"));
        Assert.Equal(KeymapResolution.Action("Prev"), updated.Resolve(new Keybind(Key.Left)));
    }

    [Fact]
    public void Equals_ComparesBindingsNotHistory()
    {
        var right = new Keybind(Key.Right);
        var original = Keymap.FromJson(null, Defaults);

        var roundTrip = original.Assign(right, "Last").Assign(right, "Next");
        var changed = original.Assign(right, "Last");

        Assert.Equal(original, roundTrip);
        Assert.NotEqual(original, changed);
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

    [Theory]
    [InlineData(InputMode.Dialog)]
    [InlineData(InputMode.Renaming)]
    [InlineData(InputMode.Crop)]
    public void Resolve_InBlockingMode_ResolvesNothing(InputMode mode)
    {
        var keymap = Keymap.FromJson(null, Defaults);

        Assert.Equal(KeymapResolution.None, keymap.Resolve(new Keybind(Key.Right), mode));
    }

    [Theory]
    [InlineData(InputMode.Normal)]
    [InlineData(InputMode.MotionPhoto)]
    [InlineData(InputMode.Slideshow)]
    public void Resolve_InNonBlockingMode_ResolvesAction(InputMode mode)
    {
        var keymap = Keymap.FromJson(null, Defaults);

        Assert.Equal(KeymapResolution.Action("Next"), keymap.Resolve(new Keybind(Key.Right), mode));
    }

    [Theory]
    [InlineData(InputMode.MotionPhoto)]
    [InlineData(InputMode.AnimatedImage)]
    [InlineData(InputMode.Slideshow)]
    public void Resolve_InNonBlockingMode_DisallowsActionsThatNeedAStillImage(InputMode mode)
    {
        var keymap = Keymap.FromJson(null, Defaults).Assign(new Keybind(Key.C), "Crop").Assign(new Keybind(Key.R), "RotateRight");

        Assert.Equal(KeymapResolution.None, keymap.Resolve(new Keybind(Key.C), mode));
        Assert.Equal(KeymapResolution.Action("RotateRight"), keymap.Resolve(new Keybind(Key.R), mode));
    }

    [Fact]
    public void Resolve_InNormal_AllowsActionsThatNeedAStillImage()
    {
        var keymap = Keymap.FromJson(null, Defaults).Assign(new Keybind(Key.C), "Crop");

        Assert.Equal(KeymapResolution.Action("Crop"), keymap.Resolve(new Keybind(Key.C), InputMode.Normal));
    }

    private const string DefaultsWithMouse = """
                                             {
                                               "Right": "Next",
                                               "WheelUp": "Prev",
                                               "Ctrl+WheelDown": "ZoomOut",
                                               "Contextual": {
                                                 "Return": "Confirm",
                                                 "XButton1": "Confirm"
                                               }
                                             }
                                             """;

    [Fact]
    public void WithDefaultMouseKeybinds_NoMouseKeybinds_AddsDefaultMouseKeybinds()
    {
        var defaults = Keymap.FromJson(null, DefaultsWithMouse);
        var keymap = Keymap.FromJson("""{ "Left": "Next", "Contextual": { "Return": "Confirm" } }""", DefaultsWithMouse)
            .WithDefaultMouseKeybinds(defaults);

        Assert.Equal(KeymapResolution.Action("Next"), keymap.Resolve(new Keybind(Key.Left)));
        Assert.Equal(KeymapResolution.Action("Prev"), keymap.Resolve(new Keybind(MouseButton.WheelUp)));
        Assert.Equal(KeymapResolution.Action("ZoomOut"),
            keymap.Resolve(new Keybind(MouseButton.WheelDown, KeyModifiers.Control)));
        Assert.Equal("Confirm", keymap.ResolveContextual(new Keybind(MouseButton.XButton1)));
        // Key Keybinds of the defaults stay out
        Assert.Equal(KeymapResolution.None, keymap.Resolve(new Keybind(Key.Right)));
    }

    [Fact]
    public void WithDefaultMouseKeybinds_HasMouseKeybind_ReturnsSameKeymap()
    {
        var defaults = Keymap.FromJson(null, DefaultsWithMouse);
        var keymap = Keymap.FromJson("""{ "Left": "Next", "XButton2": "Next" }""", DefaultsWithMouse);

        Assert.Same(keymap, keymap.WithDefaultMouseKeybinds(defaults));
    }

    private const string DefaultsWithArrows = """
                                              {
                                                "Up": "RotateRight",
                                                "W": "RotateRight",
                                                "Down": "RotateLeft",
                                                "S": "RotateLeft",
                                                "Left": "Prev",
                                                "A": "Prev",
                                                "Right": "Next",
                                                "D": "Next",
                                                "Contextual": {
                                                  "Return": "Confirm",
                                                  "Up": "Up",
                                                  "W": "Up",
                                                  "Down": "Down",
                                                  "S": "Down",
                                                  "Left": "Left",
                                                  "A": "Left",
                                                  "Right": "Right",
                                                  "D": "Right"
                                                }
                                              }
                                              """;

    private static Keymap Migrated(string userJson) =>
        Keymap.FromJson(userJson, DefaultsWithArrows)
            .WithContextualNavigation(Keymap.FromJson(null, DefaultsWithArrows));

    [Fact]
    public void WithContextualNavigation_Version30DefaultFile_KeepsRotateAndAddsLeftRight()
    {
        var keymap = Migrated("""
            {
              "Up": "Up", "W": "Up", "Down": "Down", "Left": "Prev", "Right": "Next",
              "Contextual": { "Return": "Confirm" }
            }
            """);

        Assert.Equal("Up", keymap.ResolveContextual(new Keybind(Key.Up)));
        Assert.Equal(KeymapResolution.Action("RotateRight"), keymap.Resolve(new Keybind(Key.Up)));
        Assert.Equal(KeymapResolution.Action("RotateRight"), keymap.Resolve(new Keybind(Key.W)));
        Assert.Equal(KeymapResolution.Action("RotateLeft"), keymap.Resolve(new Keybind(Key.Down)));
        Assert.Equal("Left", keymap.ResolveContextual(new Keybind(Key.Left)));
        Assert.Equal(KeymapResolution.Action("Prev"), keymap.Resolve(new Keybind(Key.Left)));
        Assert.Equal("Right", keymap.ResolveContextual(new Keybind(Key.D)));
    }

    [Fact]
    public void WithContextualNavigation_CustomUpBinding_GainsRotateRight()
    {
        var keymap = Migrated("""{ "NumPad8": "Up", "Contextual": {} }""");

        Assert.Equal("Up", keymap.ResolveContextual(new Keybind(Key.NumPad8)));
        Assert.Equal(KeymapResolution.Action("RotateRight"), keymap.Resolve(new Keybind(Key.NumPad8)));
    }

    [Fact]
    public void WithContextualNavigation_KeybindWithOrdinaryAction_KeepsIt()
    {
        var keymap = Migrated("""{ "Up": "Next", "Contextual": { "Up": "Up" } }""");

        Assert.Equal(KeymapResolution.Action("Next"), keymap.Resolve(new Keybind(Key.Up)));
    }

    [Fact]
    public void WithContextualNavigation_LeftAlreadyContextual_IsLeftAlone()
    {
        var keymap = Migrated("""{ "Contextual": { "Left": "Confirm" } }""");

        Assert.Equal("Confirm", keymap.ResolveContextual(new Keybind(Key.Left)));
        Assert.Equal("Left", keymap.ResolveContextual(new Keybind(Key.A)));
    }

    [Fact]
    public void WithContextualNavigation_FileWithoutContextualSection_RotatesOnUpAndW()
    {
        var keymap = Migrated("""{ "Up": "Up", "W": "Up" }""");

        Assert.Equal(KeymapResolution.Action("RotateRight"), keymap.Resolve(new Keybind(Key.Up)));
        Assert.Equal(KeymapResolution.Action("RotateRight"), keymap.Resolve(new Keybind(Key.W)));
    }

    [Fact]
    public void WithContextualNavigation_AlreadyMigrated_ReturnsSameKeymap()
    {
        var defaults = Keymap.FromJson(null, DefaultsWithArrows);

        Assert.Same(defaults, defaults.WithContextualNavigation(defaults));
    }

    [Fact]
    public void FromJson_ContextualSectionWins_OverContextualNameInOrdinarySection()
    {
        var keymap = Keymap.FromJson("""{ "Up": "Up", "Contextual": { "Up": "Confirm" } }""", DefaultsWithArrows);

        Assert.Equal("Confirm", keymap.ResolveContextual(new Keybind(Key.Up)));
    }

    [Fact]
    public void FromJson_FileWithoutContextualSection_LeavesDefaultArrowOffAReboundKey()
    {
        // A file from before the arrows were contextual keeps what the user bound W and Left to
        var keymap = Migrated("""{ "W": "ToggleFullscreen", "Left": "ToggleFullscreen", "Up": "Up" }""");

        Assert.Null(keymap.ResolveContextual(new Keybind(Key.W)));
        Assert.Equal(KeymapResolution.Action("ToggleFullscreen"), keymap.Resolve(new Keybind(Key.W)));
        Assert.Null(keymap.ResolveContextual(new Keybind(Key.Left)));
        Assert.Equal("Up", keymap.ResolveContextual(new Keybind(Key.Up)));
        Assert.Equal("Left", keymap.ResolveContextual(new Keybind(Key.A)));
    }

    [Fact]
    public void WithContextualNavigation_LeftOnAReboundKey_IsNotAdded()
    {
        var keymap = Migrated("""{ "A": "ToggleFullscreen", "Contextual": { "Return": "Confirm" } }""");

        Assert.Null(keymap.ResolveContextual(new Keybind(Key.A)));
        Assert.Equal("Left", keymap.ResolveContextual(new Keybind(Key.Left)));
    }
}
