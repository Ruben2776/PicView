using System.Text.Json;
using PicView.Core.Config;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;

namespace PicView.Tests.Gallery;

/// <summary>
/// The item spacing is never below 5 and the line spacing never below 8, since below that the gallery items look
/// as if they overlap. A lower value is not accepted from the settings file or from the settings view.
/// </summary>
[Collection("Sequential")]
public class GallerySpacingLimitsTests
{
    public GallerySpacingLimitsTests()
    {
        SetDefaults();
    }

    [Fact]
    public void Limits_AreFiveForItemsAndEightForLines()
    {
        Assert.Equal(5, GalleryDefaults.MinItemSpacing);
        Assert.Equal(8, GalleryDefaults.MinLineSpacing);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-3, 5)]
    [InlineData(4.9, 5)]
    [InlineData(5, 5)]
    [InlineData(25, 25)]
    [InlineData(70, 70)]
    [InlineData(500, 70)]
    [InlineData(double.NaN, 5)]
    public void AcceptedItemSpacing_AnyValue_IsTheNearestOneInRange(double value, double expected)
    {
        Assert.Equal(expected, GalleryDefaults.AcceptedItemSpacing(value));
    }

    [Theory]
    [InlineData(0, 8)]
    [InlineData(5, 8)]
    [InlineData(7.9, 8)]
    [InlineData(8, 8)]
    [InlineData(20, 20)]
    [InlineData(70, 70)]
    [InlineData(500, 70)]
    [InlineData(double.NaN, 8)]
    public void AcceptedLineSpacing_AnyValue_IsTheNearestOneInRange(double value, double expected)
    {
        Assert.Equal(expected, GalleryDefaults.AcceptedLineSpacing(value));
    }

    [Theory]
    [InlineData(0, 0, 5, 8)]
    [InlineData(2, 5, 5, 8)]
    [InlineData(5, 8, 5, 8)]
    [InlineData(30, 40, 30, 40)]
    public void EnsureSettings_StoredSpacings_LoadNoLowerThanTheLimits(double storedItem, double storedLine,
        double expectedItem, double expectedLine)
    {
        var json = $$"""{ "Gallery": { "ItemSpacing": {{storedItem}}, "LineSpacing": {{storedLine}} } }""";

        var loaded = EnsureSettings(JsonSerializer.Deserialize(json, SettingsGenerationContext.Default.AppSettings)!);

        Assert.Equal(expectedItem, loaded.Gallery!.ItemSpacing);
        Assert.Equal(expectedLine, loaded.Gallery.LineSpacing);
    }

    [Fact]
    public void Defaults_Spacings_AreWithinTheLimits()
    {
        var gallery = GetDefaults().Gallery!;

        Assert.Equal(gallery.ItemSpacing, GalleryDefaults.AcceptedItemSpacing(gallery.ItemSpacing));
        Assert.Equal(gallery.LineSpacing, GalleryDefaults.AcceptedLineSpacing(gallery.LineSpacing));
    }

    [Fact]
    public void SpacingsSetBelowTheLimits_AreNotAccepted()
    {
        var shared = new GallerySharedSettingsViewModel(() => ValueTask.FromResult(true));
        shared.Start();

        shared.GalleryItemSpacing.Value = 0;
        shared.GalleryLineSpacing.Value = 3;

        Assert.Equal(5, Settings.Gallery.ItemSpacing);
        Assert.Equal(8, Settings.Gallery.LineSpacing);
        Assert.Equal(5, shared.GalleryItemSpacing.Value);
        Assert.Equal(8, shared.GalleryLineSpacing.Value);
    }

    [Fact]
    public void SpacingsSetWithinTheLimits_AreStoredAsSet()
    {
        var shared = new GallerySharedSettingsViewModel(() => ValueTask.FromResult(true));
        shared.Start();

        shared.GalleryItemSpacing.Value = 12;
        shared.GalleryLineSpacing.Value = 9;

        Assert.Equal(12, Settings.Gallery.ItemSpacing);
        Assert.Equal(9, Settings.Gallery.LineSpacing);
    }
}
