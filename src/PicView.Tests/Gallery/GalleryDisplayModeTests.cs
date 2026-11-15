using System.Text.Json;
using PicView.Core.Config;
using PicView.Core.Gallery;

namespace PicView.Tests.Gallery;

/// <summary>
/// The Gallery Display Mode: its stored numbers, the switch between Columns and the last used tile mode, and how
/// it loads from the settings file.
/// </summary>
public class GalleryDisplayModeTests
{
    [Fact]
    public void GalleryDisplayMode_Values_KeepTheirStoredNumbers()
    {
        Assert.Equal(0, (int)GalleryDisplayMode.Columns);
        Assert.Equal(1, (int)GalleryDisplayMode.Masonry);
        Assert.Equal(2, (int)GalleryDisplayMode.Rows);
    }

    [Theory]
    [InlineData(GalleryDisplayMode.Columns, false, GalleryDisplayMode.Masonry)]
    [InlineData(GalleryDisplayMode.Columns, true, GalleryDisplayMode.Rows)]
    [InlineData(GalleryDisplayMode.Masonry, false, GalleryDisplayMode.Columns)]
    [InlineData(GalleryDisplayMode.Masonry, true, GalleryDisplayMode.Columns)]
    [InlineData(GalleryDisplayMode.Rows, false, GalleryDisplayMode.Columns)]
    [InlineData(GalleryDisplayMode.Rows, true, GalleryDisplayMode.Columns)]
    [InlineData((GalleryDisplayMode)7, true, GalleryDisplayMode.Columns)]
    public void Toggle_CurrentAndLastUsed_GivesTheOtherSide(GalleryDisplayMode current, bool rowsWasLastUsed,
        GalleryDisplayMode expected)
    {
        Assert.Equal(expected, GalleryDisplayModes.Toggle(current, rowsWasLastUsed));
    }

    [Theory]
    [InlineData(GalleryDisplayMode.Columns, false)]
    [InlineData(GalleryDisplayMode.Masonry, true)]
    [InlineData(GalleryDisplayMode.Rows, true)]
    [InlineData((GalleryDisplayMode)7, false)]
    public void ShowsTiles_Mode_IsTrueForMasonryAndRows(GalleryDisplayMode mode, bool expected)
    {
        Assert.Equal(expected, GalleryDisplayModes.ShowsTiles(mode));
    }

    [Theory]
    [InlineData(0, GalleryDisplayMode.Columns)]
    [InlineData(1, GalleryDisplayMode.Masonry)]
    [InlineData(2, GalleryDisplayMode.Rows)]
    [InlineData(3, GalleryDisplayMode.Columns)]
    [InlineData(-1, GalleryDisplayMode.Columns)]
    public void Validate_StoredNumber_IsColumnsUnlessOneOfTheThree(int stored, GalleryDisplayMode expected)
    {
        Assert.Equal(expected, GalleryDisplayModes.Validate((GalleryDisplayMode)stored));
    }

    [Fact]
    public void GetDefaults_DisplayMode_IsColumnsWithMasonryLastUsed()
    {
        var gallery = GetDefaults().Gallery!;

        Assert.Equal(GalleryDisplayMode.Columns, gallery.ExpandedGalleryDisplayMode);
        Assert.False(gallery.RowsWasLastUsedDisplayMode);
    }

    [Fact]
    public void EnsureSettings_DisplayModeMissingFromSavedSettings_IsColumnsWithMasonryLastUsed()
    {
        const string json = """{ "Gallery": { "IsGalleryDocked": true } }""";

        var loaded = EnsureSettings(JsonSerializer.Deserialize(json, SettingsGenerationContext.Default.AppSettings)!);

        Assert.Equal(GalleryDisplayMode.Columns, loaded.Gallery!.ExpandedGalleryDisplayMode);
        Assert.False(loaded.Gallery.RowsWasLastUsedDisplayMode);
    }

    [Theory]
    [InlineData(0, GalleryDisplayMode.Columns)]
    [InlineData(1, GalleryDisplayMode.Masonry)]
    [InlineData(2, GalleryDisplayMode.Rows)]
    [InlineData(3, GalleryDisplayMode.Columns)]
    [InlineData(99, GalleryDisplayMode.Columns)]
    [InlineData(-1, GalleryDisplayMode.Columns)]
    public void EnsureSettings_StoredDisplayModeNumber_LoadsAsThatModeOrColumns(int stored, GalleryDisplayMode expected)
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
