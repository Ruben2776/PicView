using System.Globalization;
using PicView.Avalonia.Functions;
using PicView.Core.Keybindings;
using PicView.Core.Localization;

namespace PicView.Tests.Keybindings;

public class ActionListContractTests
{
    // GetFunctionByName only builds delegates, so the constructor dependencies are never touched.
    private readonly FunctionsMapper _mapper = new(null!, null!);

    [Fact]
    public void CategoryEntries_AllResolveThroughFunctionsMapper()
    {
        var languageModel = TranslationManager.GetLanguageModel(new CultureInfo("en-US"));
        var categories = KeybindCategoryDefinition.GetAllCategories(languageModel);

        var unresolved = categories
            .SelectMany(c => c.Entries)
            .Select(e => e.FunctionName)
            .Where(name => _mapper.GetFunctionByName(name) is null)
            .ToList();

        Assert.Empty(unresolved);
    }

    [Fact]
    public void ActionNames_AllResolveThroughFunctionsMapper()
    {
        var unresolved = KeybindCategoryDefinition.ActionNames
            .Where(name => _mapper.GetFunctionByName(name) is null)
            .ToList();

        Assert.Empty(unresolved);
    }

    [Fact]
    public void ActionNames_ContainEveryCategoryEntry()
    {
        var languageModel = TranslationManager.GetLanguageModel(new CultureInfo("en-US"));
        var categories = KeybindCategoryDefinition.GetAllCategories(languageModel);

        var missing = categories
            .SelectMany(c => c.Entries)
            .Select(e => e.FunctionName)
            .Where(name => !KeybindCategoryDefinition.ActionNames.Contains(name))
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void CategoryEntries_UseCanonicalNames()
    {
        var languageModel = TranslationManager.GetLanguageModel(new CultureInfo("en-US"));
        var categories = KeybindCategoryDefinition.GetAllCategories(languageModel);

        var aliased = categories
            .SelectMany(c => c.Entries)
            .Select(e => e.FunctionName)
            .Where(name => KeybindCategoryDefinition.Canonicalize(name) != name)
            .ToList();

        Assert.Empty(aliased);
    }

    [Theory]
    [InlineData("Fullscreen", "ToggleFullscreen")]
    [InlineData("Stretch", "ZoomToFit")]
    [InlineData("ToggleFullscreen", "ToggleFullscreen")]
    [InlineData("Next", "Next")]
    [InlineData("NotAnAction", "NotAnAction")]
    public void Canonicalize_MapsAliasesAndPassesOtherNamesThrough(string name, string expected)
    {
        Assert.Equal(expected, KeybindCategoryDefinition.Canonicalize(name));
    }
}
