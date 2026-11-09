#if MACOS || WINDOWS
using System.Text.Json;
using Avalonia.Input;
using PicView.Avalonia.Input;
using PicView.Core.Keybindings;

namespace PicView.Tests.Keybindings;

public class DefaultKeybindingsContractTests
{
#if MACOS
    private const string DefaultKeybindings = PicView.Core.MacOS.MacOsKeybindings.DefaultKeybindings;
#else
    private const string DefaultKeybindings = PicView.Core.WindowsNT.WindowsKeybindings.DefaultKeybindings;
#endif

    [Fact]
    public void DefaultKeybindings_AllActionsCanonicalizeToKnownActions()
    {
        using var defaults = JsonDocument.Parse(DefaultKeybindings,
            new JsonDocumentOptions { AllowTrailingCommas = true });

        var unknown = defaults.RootElement.EnumerateObject()
            .Where(entry => entry.Value.ValueKind is JsonValueKind.String)
            .Select(entry => KeybindCategoryDefinition.Canonicalize(entry.Value.GetString()!))
            .Where(name => !KeybindCategoryDefinition.ActionNames.Contains(name))
            .ToList();

        Assert.Empty(unknown);
    }

    [Fact]
    public void DefaultKeybindings_BindEveryContextualActionInTheContextualSection()
    {
        var keymap = Keymap.FromJson(null, DefaultKeybindings);

        Assert.All(KeybindCategoryDefinition.ContextualActionNames,
            name => Assert.NotEmpty(keymap.KeybindsFor(name)));
        Assert.All(keymap.ContextualBindings.Values,
            name => Assert.True(KeybindCategoryDefinition.IsContextualAction(name), name));
    }

    [Theory]
    [InlineData(-1.0, "Next")]
    [InlineData(1.0, "Prev")]
    public void DefaultKeybindings_WheelWithoutModifiers_Navigates(double deltaY, string expectedAction)
    {
        var keymap = Keymap.FromJson(null, DefaultKeybindings);

        Assert.Equal(KeymapResolution.Action(expectedAction),
            keymap.Resolve(Keybind.FromWheel(deltaY, KeyModifiers.None)));
    }

    [Theory]
    [InlineData(Key.Up, "Up", "RotateRight")]
    [InlineData(Key.W, "Up", "RotateRight")]
    [InlineData(Key.Down, "Down", "RotateLeft")]
    [InlineData(Key.S, "Down", "RotateLeft")]
    [InlineData(Key.Left, "Left", "Prev")]
    [InlineData(Key.A, "Left", "Prev")]
    [InlineData(Key.Right, "Right", "Next")]
    [InlineData(Key.D, "Right", "Next")]
    public void DefaultKeybindings_ArrowsAndWasd_CarryBothLayers(Key key, string contextual, string ordinary)
    {
        // The gallery and a tall image claim the Contextual Action; otherwise the key falls through
        var keymap = Keymap.FromJson(null, DefaultKeybindings);

        Assert.Equal(contextual, keymap.ResolveContextual(new Keybind(key)));
        Assert.Equal(KeymapResolution.Action(ordinary), keymap.Resolve(new Keybind(key)));
    }
}
#endif
