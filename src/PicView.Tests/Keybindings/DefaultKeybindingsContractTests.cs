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
        var defaults = JsonSerializer.Deserialize<Dictionary<string, string>>(
            DefaultKeybindings, new JsonSerializerOptions { AllowTrailingCommas = true });
        Assert.NotNull(defaults);

        var unknown = defaults.Values
            .Select(KeybindCategoryDefinition.Canonicalize)
            .Where(name => !KeybindCategoryDefinition.ActionNames.Contains(name))
            .ToList();

        Assert.Empty(unknown);
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
