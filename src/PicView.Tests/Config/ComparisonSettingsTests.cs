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
}
