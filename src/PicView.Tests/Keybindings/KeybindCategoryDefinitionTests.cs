using System.Globalization;
using PicView.Core.Keybindings;
using PicView.Core.Localization;

namespace PicView.Tests.Keybindings;

public class KeybindCategoryDefinitionTests
{
    [Fact]
    public void GetNavigationEntries_ContainsExpectedFunctionNames()
    {
        var languageModel = TranslationManager.GetLanguageModel(new CultureInfo("en-US"));
        var entries = KeybindCategoryDefinition.GetNavigationEntries(languageModel);
        Assert.NotNull(entries);
        Assert.NotEmpty(entries);

        var functionNames = entries.Select(e => e.FunctionName).ToList();

        Assert.Contains("Next", functionNames);
        Assert.Contains("Prev", functionNames);
        Assert.Contains("Last", functionNames);
        Assert.Contains("First", functionNames);
        Assert.Contains("NextFolder", functionNames);
        Assert.Contains("GalleryClick", functionNames);
    }

    [Fact]
    public void GetNavigationEntries_DisplayNamesFromTranslation()
    {
        var languageModel = TranslationManager.GetLanguageModel(new CultureInfo("en-US"));
        var entries = KeybindCategoryDefinition.GetNavigationEntries(languageModel);
        Assert.NotNull(entries);
        Assert.NotEmpty(entries);

        var nextEntry = entries.First(e => e.FunctionName == "Next");
        Assert.Equal("Next image", nextEntry.DisplayName);

        var prevEntry = entries.First(e => e.FunctionName == "Prev");
        Assert.Equal("Previous image", prevEntry.DisplayName);
    }
    
    [Fact]
    public void GetAllCategories_EntriesAreNotEmpty()
    {
        var languageModel = TranslationManager.GetLanguageModel(new CultureInfo("en-US"));
        var categories = KeybindCategoryDefinition.GetAllCategories(languageModel);

        foreach (var (categoryName, entries) in categories)
        {
            foreach (var (functionName, displayName) in entries)
            {
                Assert.False(string.IsNullOrEmpty(functionName),
                    $"Category '{categoryName}' has an entry with empty function name");
                Assert.False(string.IsNullOrEmpty(displayName),
                    $"Category '{categoryName}', function '{functionName}' has an empty display name");
            }
        }
    }

    [Fact]
    public void GetAllCategories_NoDuplicateFunctionNamesWithinCategory()
    {
        var languageModel = TranslationManager.GetLanguageModel(new CultureInfo("en-US"));
        var categories = KeybindCategoryDefinition.GetAllCategories(languageModel);

        foreach (var (categoryName, entries) in categories)
        {
            var functionNames = entries.Select(e => e.FunctionName).ToList();
            var unique = functionNames.Distinct().ToList();
            Assert.True(functionNames.Count == unique.Count,
                $"Category '{categoryName}' has duplicate function names");
        }
    }

    [Fact]
    public void NullTranslationProperties_FallBackToEmptyString()
    {
        // LanguageModel with all null properties
        var t = new LanguageModel();
        var entries = KeybindCategoryDefinition.GetNavigationEntries(t);

        // Should not throw and all display names should be empty strings
        foreach (var (_, displayName) in entries)
        {
            Assert.NotNull(displayName);
        }
    }

    [Fact]
    public void GetCopyEntries_CopyBase64HasConcatenatedDisplayName()
    {
        var languageModel = TranslationManager.GetLanguageModel(new CultureInfo("en-US"));
        var entries = KeybindCategoryDefinition.GetCopyEntries(languageModel);
        var base64Entry = entries.First(e => e.FunctionName == "CopyBase64");
        Assert.Equal("Copy base64", base64Entry.DisplayName);
    }

    [Fact]
    public void GetCopyEntries_PlatformSpecificCutFile()
    {
        var languageModel = TranslationManager.GetLanguageModel(new CultureInfo("en-US"));
        var entries = KeybindCategoryDefinition.GetCopyEntries(languageModel);
        var functionNames = entries.Select(e => e.FunctionName).ToList();

        if (OperatingSystem.IsMacOS())
        {
            Assert.DoesNotContain("CutFile", functionNames);
        }
        else
        {
            Assert.Contains("CutFile", functionNames);
        }
    }
}
