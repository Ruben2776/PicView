using System.Globalization;
using PicView.Avalonia.Functions;
using PicView.Core.Favorites;
using PicView.Core.Keybindings;
using PicView.Core.Localization;

namespace PicView.Tests.Keybindings;

public class ActionListContractTests
{
    // The constructor only builds delegates, so its dependencies are never touched.
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
    [InlineData("GalleryClick", "Confirm")]
    [InlineData("ToggleFullscreen", "ToggleFullscreen")]
    [InlineData("Next", "Next")]
    [InlineData("NotAnAction", "NotAnAction")]
    public void Canonicalize_MapsAliasesAndPassesOtherNamesThrough(string name, string expected)
    {
        Assert.Equal(expected, KeybindCategoryDefinition.Canonicalize(name));
    }

    [Fact]
    public void FavoriteActions_AreOneActionPerSlot()
    {
        var expected = Enumerable.Range(1, FavoritesManager.MaxSlots).Select(slot => $"OpenFavorite{slot}");

        Assert.Equal(expected, KeybindCategoryDefinition.FavoriteActionNames);
        Assert.All(expected, name => Assert.True(KeybindCategoryDefinition.ActionNames.Contains(name), name));
    }

    [Theory]
    [InlineData("OpenFavorite1", 1)]
    [InlineData("OpenFavorite10", 10)]
    [InlineData("OpenFavorite0", null)]
    [InlineData("OpenFavorite11", null)]
    [InlineData("OpenFavorite", null)]
    [InlineData("Next", null)]
    public void FavoriteSlot_ParsesOnlyFavoriteActions(string action, int? expected)
    {
        Assert.Equal(expected, KeybindCategoryDefinition.FavoriteSlot(action));
    }

    [Fact]
    public void ContextualActions_AreConfirmAndPlayPause()
    {
        Assert.Equal(["Confirm", "PlayPause"], KeybindCategoryDefinition.ContextualActionNames);
        Assert.All(KeybindCategoryDefinition.ContextualActionNames,
            name => Assert.True(KeybindCategoryDefinition.IsContextualAction(name), name));
        Assert.False(KeybindCategoryDefinition.IsContextualAction("Next"));
        Assert.False(KeybindCategoryDefinition.IsContextualAction("Copy"));
        Assert.False(KeybindCategoryDefinition.IsContextualAction("Cancel"));
    }

    [Theory]
    [InlineData("Crop", true)]
    [InlineData("RotateRight", false)]
    [InlineData("DeleteFile", false)]
    [InlineData("EffectsWindow", false)]
    public void NeedsStillImage_OnlyCrop(string action, bool expected)
    {
        Assert.Equal(expected, KeybindCategoryDefinition.NeedsStillImage(action));
    }
}
