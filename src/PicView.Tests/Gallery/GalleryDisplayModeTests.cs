using System.Text.Json;
using PicView.Core.Config;
using PicView.Core.Gallery;

namespace PicView.Tests.Gallery;

/// <summary>
/// The Gallery Display Mode: its stored numbers, the switch between Grid and the last used tile mode, and how
/// it loads from the settings file.
/// </summary>
public class GalleryDisplayModeTests
{
    [Fact]
    public void GalleryDisplayMode_Values_KeepTheirStoredNumbers()
    {
        Assert.Equal(0, (int)GalleryDisplayMode.Grid);
        Assert.Equal(1, (int)GalleryDisplayMode.Masonry);
        Assert.Equal(2, (int)GalleryDisplayMode.Rows);
    }

    [Theory]
    [InlineData(GalleryDisplayMode.Grid, false, GalleryDisplayMode.Masonry)]
    [InlineData(GalleryDisplayMode.Grid, true, GalleryDisplayMode.Rows)]
    [InlineData(GalleryDisplayMode.Masonry, false, GalleryDisplayMode.Grid)]
    [InlineData(GalleryDisplayMode.Masonry, true, GalleryDisplayMode.Grid)]
    [InlineData(GalleryDisplayMode.Rows, false, GalleryDisplayMode.Grid)]
    [InlineData(GalleryDisplayMode.Rows, true, GalleryDisplayMode.Grid)]
    [InlineData((GalleryDisplayMode)7, true, GalleryDisplayMode.Grid)]
    public void Toggle_CurrentAndLastUsed_GivesTheOtherSide(GalleryDisplayMode current, bool rowsWasLastUsed,
        GalleryDisplayMode expected)
    {
        Assert.Equal(expected, GalleryDisplayModes.Toggle(current, rowsWasLastUsed));
    }

    [Theory]
    [InlineData(GalleryDisplayMode.Grid, false)]
    [InlineData(GalleryDisplayMode.Masonry, true)]
    [InlineData(GalleryDisplayMode.Rows, true)]
    [InlineData((GalleryDisplayMode)7, false)]
    public void ShowsTiles_Mode_IsTrueForMasonryAndRows(GalleryDisplayMode mode, bool expected)
    {
        Assert.Equal(expected, GalleryDisplayModes.ShowsTiles(mode));
    }

    [Theory]
    [InlineData(0, GalleryDisplayMode.Grid)]
    [InlineData(1, GalleryDisplayMode.Masonry)]
    [InlineData(2, GalleryDisplayMode.Rows)]
    [InlineData(3, GalleryDisplayMode.Grid)]
    [InlineData(-1, GalleryDisplayMode.Grid)]
    public void Validate_StoredNumber_IsGridUnlessOneOfTheThree(int stored, GalleryDisplayMode expected)
    {
        Assert.Equal(expected, GalleryDisplayModes.Validate((GalleryDisplayMode)stored));
    }

    [Fact]
    public void GetDefaults_DisplayMode_IsGridWithMasonryLastUsed()
    {
        var gallery = GetDefaults().Gallery!;

        Assert.Equal(GalleryDisplayMode.Grid, gallery.ExpandedGalleryDisplayMode);
        Assert.False(gallery.RowsWasLastUsedDisplayMode);
    }

    [Fact]
    public void EnsureSettings_DisplayModeMissingFromSavedSettings_IsGridWithMasonryLastUsed()
    {
        const string json = """{ "Gallery": { "IsGalleryDocked": true } }""";

        var loaded = EnsureSettings(JsonSerializer.Deserialize(json, SettingsGenerationContext.Default.AppSettings)!);

        Assert.Equal(GalleryDisplayMode.Grid, loaded.Gallery!.ExpandedGalleryDisplayMode);
        Assert.False(loaded.Gallery.RowsWasLastUsedDisplayMode);
    }

    [Theory]
    [InlineData(0, GalleryDisplayMode.Grid)]
    [InlineData(1, GalleryDisplayMode.Masonry)]
    [InlineData(2, GalleryDisplayMode.Rows)]
    [InlineData(3, GalleryDisplayMode.Grid)]
    [InlineData(99, GalleryDisplayMode.Grid)]
    [InlineData(-1, GalleryDisplayMode.Grid)]
    public void EnsureSettings_StoredDisplayModeNumber_LoadsAsThatModeOrGrid(int stored, GalleryDisplayMode expected)
    {
        var json = $$"""{ "Gallery": { "ExpandedGalleryDisplayMode": {{stored}}, "RowsWasLastUsedDisplayMode": true } }""";

        var loaded = EnsureSettings(JsonSerializer.Deserialize(json, SettingsGenerationContext.Default.AppSettings)!);

        Assert.Equal(expected, loaded.Gallery!.ExpandedGalleryDisplayMode);
        // The last-used memory is kept even when the stored mode was not valid
        Assert.True(loaded.Gallery.RowsWasLastUsedDisplayMode);
    }

    [Fact]
    public void DisplayMode_Set_IsStoredAsItsNumberAndStaysAfterSavingAndLoading()
    {
        var settings = GetDefaults();
        settings.Gallery!.ExpandedGalleryDisplayMode = GalleryDisplayMode.Rows;
        settings.Gallery.RowsWasLastUsedDisplayMode = true;

        var json = JsonSerializer.Serialize(settings, SettingsGenerationContext.Default.AppSettings);
        var loaded = JsonSerializer.Deserialize(json, SettingsGenerationContext.Default.AppSettings);

        Assert.Contains("\"ExpandedGalleryDisplayMode\": 2", json);
        Assert.Equal(GalleryDisplayMode.Rows, loaded!.Gallery!.ExpandedGalleryDisplayMode);
        Assert.True(loaded.Gallery.RowsWasLastUsedDisplayMode);
    }
}
