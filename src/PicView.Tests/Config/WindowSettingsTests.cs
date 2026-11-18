using PicView.Core.Config;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Tests.Config;

[Collection("Sequential")]
public class WindowSettingsTests
{
    public WindowSettingsTests() => SetDefaults();

    [Fact]
    public void FromRecord_ReadsEveryWindowSetting()
    {
        var record = GetDefaults();
        record.WindowProperties = new WindowProperties
        {
            AutoFit = true, Maximized = true, Fullscreen = false, TopMost = true, KeepCentered = true,
            Left = 10, Top = 20, Width = 300, Height = 400
        };
        using var settings = WindowSettings.FromRecord(record);
        Assert.Equal(new RememberedWindowMode(false, true, true), settings.StartingMode);
        Assert.True(settings.IsTopMost.Value);
        Assert.True(settings.IsStayingCentered.Value);
        Assert.Equal(new NormalBounds(10, 20, 300, 400), settings.Bounds);
    }

    [Fact]
    public void FromRecord_NoWindowProperties_GivesTheDefaults()
    {
        using var fromNull = WindowSettings.FromRecord(null);
        using var fromEmpty = WindowSettings.FromRecord(new AppSettings());
        var defaults = new WindowProperties();
        foreach (var settings in new[] { fromNull, fromEmpty })
        {
            Assert.Equal(new RememberedWindowMode(defaults.Fullscreen, defaults.Maximized, defaults.AutoFit), settings.StartingMode);
            Assert.Equal(new NormalBounds(defaults.Left, defaults.Top, defaults.Width, defaults.Height), settings.Bounds);
        }
    }

    [Fact]
    public void Store_WritesEveryWindowSettingAndNothingElse()
    {
        var record = GetDefaults();
        record.WindowProperties!.Margin = 33;
        record.WindowProperties.EscLeavesFullscreen = false;
        using var settings = WindowSettings.FromRecord(GetDefaults());
        settings.IsTopMost.Value = true;
        settings.IsStayingCentered.Value = true;
        settings.Bounds = new NormalBounds(1, 2, 3, 4);
        settings.Store(record, new RememberedWindowMode(Fullscreen: true, Maximized: false, AutoFit: true));
        var stored = record.WindowProperties;
        Assert.True(stored.Fullscreen);
        Assert.False(stored.Maximized);
        Assert.True(stored.AutoFit);
        Assert.True(stored.TopMost);
        Assert.True(stored.KeepCentered);
        Assert.Equal((1d, 2d, 3d, 4d), (stored.Left, stored.Top, stored.Width, stored.Height));
        Assert.Equal(33, stored.Margin);
        Assert.False(stored.EscLeavesFullscreen);
    }

    [Fact]
    public void Store_RecordWithoutWindowProperties_CreatesTheGroup()
    {
        var record = new AppSettings();
        using var settings = WindowSettings.FromRecord(null);
        settings.Store(record, default);
        Assert.NotNull(record.WindowProperties);
    }

    [Fact]
    public void CopyFor_TakesTheOpenersValuesAndItsLiveMode_AndIsIndependentAfterwards()
    {
        using var opener = WindowSettings.FromRecord(GetDefaults());
        opener.IsTopMost.Value = true;
        opener.Bounds = new NormalBounds(5, 6, 7, 8);
        using var copy = opener.CopyFor(new RememberedWindowMode(false, true, true));
        Assert.Equal(new RememberedWindowMode(false, true, true), copy.StartingMode);
        Assert.True(copy.IsTopMost.Value);
        Assert.Equal(opener.Bounds, copy.Bounds);
        copy.IsTopMost.Value = false;
        Assert.True(opener.IsTopMost.Value);
    }

    [Fact]
    public void Change_WritesNothingToTheRecord()
    {
        using var settings = WindowSettings.FromRecord(Settings);
        settings.IsTopMost.Value = !settings.IsTopMost.Value;
        settings.IsStayingCentered.Value = !settings.IsStayingCentered.Value;
        Assert.Equal(new WindowProperties().TopMost, Settings.WindowProperties!.TopMost);
        Assert.Equal(new WindowProperties().KeepCentered, Settings.WindowProperties.KeepCentered);
    }
}
