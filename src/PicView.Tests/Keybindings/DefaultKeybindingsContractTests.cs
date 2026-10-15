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
        Assert.Equal(KeybindCategoryDefinition.ContextualActionNames.Count, keymap.ContextualBindings.Count);
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
}
#endif
