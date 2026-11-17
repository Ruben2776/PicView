using System.Text.Json;
using PicView.Core.Config;

namespace PicView.Tests.Config;

public class ComparisonSettingsTests
{
    public ComparisonSettingsTests()
    {
        SetDefaults();
    }

    [Fact]
    public void Defaults_HaveAComparisonSection()
    {
        var c = Settings.Comparison!;
        Assert.Equal(ComparisonMode.Difference, c.Mode);
        Assert.Equal(0.5, c.OverlayOpacity);
        Assert.Equal(0.5, c.SplitRatio);
        Assert.False(c.IsSplitHorizontal);
        Assert.True(c.IsBlinkFast);
        Assert.Equal(1.0, c.BlinkInterval);
        Assert.True(c.IsStretchFill);
        Assert.Equal(ComparisonBarPosition.Bottom, c.BarPosition);
    }

    [Fact]
    public void BarPosition_RoundTripsThroughJson()
    {
        Settings.Comparison!.BarPosition = ComparisonBarPosition.Left;
        var json = JsonSerializer.Serialize(Settings, SettingsGenerationContext.Default.AppSettings);
        var loaded = JsonSerializer.Deserialize(json, SettingsGenerationContext.Default.AppSettings)!;
        Assert.Equal(ComparisonBarPosition.Left, SettingsManager.EnsureSettings(loaded).Comparison!.BarPosition);
    }

    [Fact]
    public void EnsureSettings_UnknownBarPosition_ResetsToBottom()
    {
        var loaded = JsonSerializer.Deserialize("{\"Version\":1,\"Comparison\":{\"BarPosition\":42}}",
            SettingsGenerationContext.Default.AppSettings)!;
        Assert.Equal(ComparisonBarPosition.Bottom, SettingsManager.EnsureSettings(loaded).Comparison!.BarPosition);
    }

    [Fact]
    public void ComparisonMode_Synced_IsFour() => Assert.Equal(4, (int)ComparisonMode.Synced);

    [Fact]
    public void EnsureSettings_WithoutComparisonSection_AddsOne()
    {
        var loaded = JsonSerializer.Deserialize("{\"Version\":1}", SettingsGenerationContext.Default.AppSettings)!;
        Assert.Null(loaded.Comparison);

        var ensured = SettingsManager.EnsureSettings(loaded);

        Assert.NotNull(ensured.Comparison);
    }

    [Fact]
    public void Settings_HaveNoComparisonInImageScaling()
    {
        var json = JsonSerializer.Serialize(SettingsManager.GetDefaults(), SettingsGenerationContext.Default.AppSettings);

        Assert.DoesNotContain("\"Show" + "Comparison\"", json);
        Assert.DoesNotContain("\"IsSideBySide" + "Synced\"", json);
        Assert.Contains("\"Comparison\"", json);
    }
}
