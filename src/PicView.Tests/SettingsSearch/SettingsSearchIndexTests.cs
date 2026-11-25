using PicView.Core.Localization;
using PicView.Core.SettingsSearch;
using PicView.Core.ViewModels;

namespace PicView.Tests.SettingsSearch;

[Collection("Sequential")]
public class SettingsSearchIndexTests
{
    private static SettingsSearchItem Setting(SettingKey key, string name, string keywords = "",
        SettingsCategory category = SettingsCategory.General, string categoryName = "General")
    {
        return new SettingsSearchItem(category, key, name, categoryName, keywords);
    }

    private static SettingsSearchItem Category(SettingsCategory category, string name)
    {
        return new SettingsSearchItem(category, null, name, string.Empty, string.Empty);
    }

    private static List<SettingsSearchItem> BuildIn(string suffix)
    {
        var previous = TranslationManager.Swap(TestLanguages.Suffixed(suffix));
        try
        {
            return SettingsSearchIndex.Build();
        }
        finally
        {
            TranslationManager.Swap(previous);
        }
    }

    [Fact]
    public void Build_EverySettingKey_HasExactlyOneSuggestion()
    {
        var items = BuildIn("a");

        foreach (var key in Enum.GetValues<SettingKey>())
        {
            Assert.Single(items, item => item.Key == key);
        }
    }

    [Fact]
    public void Build_EveryCategory_HasExactlyOneCategorySuggestion()
    {
        var items = BuildIn("a");

        foreach (var category in Enum.GetValues<SettingsCategory>())
        {
            Assert.Single(items, item => item.IsCategory && item.Category == category);
        }
    }

    [Fact]
    public void Build_SettingSuggestion_NamesItsCategory()
    {
        var items = BuildIn("a");

        var theme = Assert.Single(items, item => item.Key == SettingKey.Theme);
        Assert.Equal(SettingsCategory.Appearance, theme.Category);
        Assert.Equal("Appearance-a", theme.CategoryName);
    }

    [Theory]
    [InlineData("NavigateBackwards-a", SettingKey.WhenDeletingFile)]
    [InlineData("NavigateForwards-a", SettingKey.WhenDeletingFile)]
    [InlineData("DarkTheme-a", SettingKey.Theme)]
    [InlineData("OpenLastFile-a", SettingKey.ApplicationStartup)]
    [InlineData("OpenInNewTab-a", SettingKey.OpeningMode)]
    public void Search_AnOptionOfASetting_SuggestsOnlyThatSetting(string query, SettingKey expected)
    {
        var found = SettingsSearchIndex.Search(BuildIn("a"), query);

        var item = Assert.Single(found);
        Assert.Equal(expected, item.Key);
    }

    [Fact]
    public void Search_WordsInAnotherOrder_StillMatch()
    {
        List<SettingsSearchItem> items =
        [
            Setting(SettingKey.ConstrainBackground, "Constrain background color to image"),
            Setting(SettingKey.Theme, "Theme")
        ];

        var found = SettingsSearchIndex.Search(items, "image  BACKGROUND");

        Assert.Equal(SettingKey.ConstrainBackground, Assert.Single(found).Key);
    }

    [Fact]
    public void Search_AWordThatMatchesNothing_SuggestsNothing()
    {
        List<SettingsSearchItem> items = [Setting(SettingKey.Theme, "Theme", "Dark Light")];

        Assert.Empty(SettingsSearchIndex.Search(items, "theme purple"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Search_NoWords_SuggestsNothing(string query)
    {
        List<SettingsSearchItem> items = [Setting(SettingKey.Theme, "Theme")];

        Assert.Empty(SettingsSearchIndex.Search(items, query));
    }

    [Fact]
    public void Search_NameMatches_RankAboveKeywordMatches()
    {
        List<SettingsSearchItem> items =
        [
            Setting(SettingKey.GalleryThumbnailStretch, "Gallery thumbnails", "stretch fill"),
            Setting(SettingKey.ZoomToFit, "Stretch")
        ];

        var found = SettingsSearchIndex.Search(items, "stretch");

        Assert.Equal([SettingKey.ZoomToFit, SettingKey.GalleryThumbnailStretch], found.Select(x => x.Key));
    }

    [Fact]
    public void Search_AMatchingCategory_IsListedFirst()
    {
        List<SettingsSearchItem> items =
        [
            Setting(SettingKey.ZoomToFit, "Zoom to fit", category: SettingsCategory.Zoom, categoryName: "Zoom"),
            Category(SettingsCategory.Zoom, "Zoom")
        ];

        var found = SettingsSearchIndex.Search(items, "zoom");

        Assert.Equal(2, found.Count);
        Assert.True(found[0].IsCategory);
    }

    [Fact]
    public void Search_TheCategoryName_FindsItsSettings()
    {
        List<SettingsSearchItem> items =
        [
            Setting(SettingKey.ShowDockedGallery, "Show docked", category: SettingsCategory.Gallery,
                categoryName: "Gallery settings"),
            Setting(SettingKey.Theme, "Theme")
        ];

        var found = SettingsSearchIndex.Search(items, "gallery");

        Assert.Equal(SettingKey.ShowDockedGallery, Assert.Single(found).Key);
    }
}
