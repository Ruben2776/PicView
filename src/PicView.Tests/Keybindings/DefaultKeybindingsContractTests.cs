#if MACOS || WINDOWS
using System.Text.Json;
using PicView.Core.Keybindings;

namespace PicView.Tests.Keybindings;

public class DefaultKeybindingsContractTests
{
#if MACOS
    private const string DefaultKeybindings = PicView.Core.MacOS.MacOsKeybindings.DefaultKeybindings;

    // Cmd+Q is bound to "Exit", which the mapper does not know. Out of scope for now.
    private static readonly string[] KnownUnresolved = ["Exit"];
#else
    private const string DefaultKeybindings = PicView.Core.WindowsNT.WindowsKeybindings.DefaultKeybindings;

    private static readonly string[] KnownUnresolved = [];
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
            .Except(KnownUnresolved)
            .ToList();

        Assert.Empty(unknown);
    }
}
#endif
