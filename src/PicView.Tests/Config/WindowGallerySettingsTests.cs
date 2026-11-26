using PicView.Core.Config;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;
using R3;
using GalleryRecord = PicView.Core.Config.Gallery;

namespace PicView.Tests.Config;

[Collection("Sequential")]
public class WindowGallerySettingsTests
{
    public WindowGallerySettingsTests() => SetDefaults();

    private static GalleryRecord RecordWithTheEightSet() => new()
    {
        DockPosition = GalleryDockPosition.Right,
        DockedGalleryStretchMode = GalleryStretchMode.Square,
        ExpandedGalleryStretchMode = GalleryStretchMode.FillSquare,
        ExpandedGalleryDisplayMode = GalleryDisplayMode.Rows,
        RowsWasLastUsedDisplayMode = true,
        DockedGalleryItemSize = 111,
        ExpandedGalleryItemSize = 222,
        IsGalleryDocked = true
    };

    private static void AssertTheEight(WindowGallerySettings settings)
    {
        Assert.Equal(GalleryDockPosition.Right, settings.DockPosition.Value);
        Assert.Equal(GalleryStretchMode.Square, settings.DockedGalleryStretchMode.Value);
        Assert.Equal(GalleryStretchMode.FillSquare, settings.ExpandedGalleryStretchMode.Value);
        Assert.Equal(GalleryDisplayMode.Rows, settings.ExpandedGalleryDisplayMode.Value);
        Assert.True(settings.RowsWasLastUsedDisplayMode);
        Assert.Equal(111, settings.DockedGalleryItemSize.Value);
        Assert.Equal(222, settings.ExpandedGalleryItemSize.Value);
        Assert.True(settings.StartsDocked.Value);
    }

    [Fact]
    public void FromRecord_ReadsTheEightValues()
    {
        using var settings = WindowGallerySettings.FromRecord(RecordWithTheEightSet());
        AssertTheEight(settings);
    }

    [Fact]
    public void FromRecord_NoGalleryGroup_GivesTheDefaults()
    {
        var defaults = new GalleryRecord();
        using var settings = WindowGallerySettings.FromRecord(null);
        Assert.Equal(defaults.DockPosition, settings.DockPosition.Value);
        Assert.Equal(defaults.DockedGalleryStretchMode, settings.DockedGalleryStretchMode.Value);
        Assert.Equal(defaults.ExpandedGalleryStretchMode, settings.ExpandedGalleryStretchMode.Value);
        Assert.Equal(defaults.ExpandedGalleryDisplayMode, settings.ExpandedGalleryDisplayMode.Value);
        Assert.Equal(defaults.RowsWasLastUsedDisplayMode, settings.RowsWasLastUsedDisplayMode);
        Assert.Equal(defaults.DockedGalleryItemSize, settings.DockedGalleryItemSize.Value);
        Assert.Equal(defaults.ExpandedGalleryItemSize, settings.ExpandedGalleryItemSize.Value);
        Assert.Equal(defaults.IsGalleryDocked, settings.StartsDocked.Value);

        using var fromEmpty = WindowSettings.FromRecord(new AppSettings { Gallery = null });
        Assert.Equal(defaults.DockPosition, fromEmpty.Gallery.DockPosition.Value);
    }

    [Fact]
    public void StoreTo_WritesTheEightValues_AndLeavesSpacingBadgesWheelAndFileLimitAlone()
    {
        var record = new GalleryRecord
        {
            ItemSpacing = 33,
            LineSpacing = 44,
            ShowMotionPhotoBadges = true,
            GalleryMouseWheelBehavior = GalleryMouseWheel.Navigate,
            HideGalleryAtFileCount = 500
        };
        var wheel = record.GalleryMouseWheelBehavior;
        using var settings = WindowGallerySettings.FromRecord(RecordWithTheEightSet());

        settings.StoreTo(record);

        Assert.Equal(GalleryDockPosition.Right, record.DockPosition);
        Assert.Equal(GalleryStretchMode.Square, record.DockedGalleryStretchMode);
        Assert.Equal(GalleryStretchMode.FillSquare, record.ExpandedGalleryStretchMode);
        Assert.Equal(GalleryDisplayMode.Rows, record.ExpandedGalleryDisplayMode);
        Assert.True(record.RowsWasLastUsedDisplayMode);
        Assert.Equal(111, record.DockedGalleryItemSize);
        Assert.Equal(222, record.ExpandedGalleryItemSize);
        Assert.True(record.IsGalleryDocked);
        Assert.Equal(33, record.ItemSpacing);
        Assert.Equal(44, record.LineSpacing);
        Assert.True(record.ShowMotionPhotoBadges);
        Assert.Equal(wheel, record.GalleryMouseWheelBehavior);
        Assert.Equal(500, record.HideGalleryAtFileCount);
    }

    [Fact]
    public void Copy_TakesTheValues_AndIsIndependentAfterwards()
    {
        using var original = WindowGallerySettings.FromRecord(RecordWithTheEightSet());
        using var copy = original.Copy();
        AssertTheEight(copy);

        copy.Start();
        copy.DockPosition.Value = GalleryDockPosition.Top;
        copy.DockedGalleryStretchMode.Value = GalleryStretchMode.Uniform;
        copy.ExpandedGalleryStretchMode.Value = GalleryStretchMode.Uniform;
        copy.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Masonry);
        copy.DockedGalleryItemSize.Value = 60;
        copy.ExpandedGalleryItemSize.Value = 60;
        copy.StartsDocked.Value = false;

        Assert.False(copy.RowsWasLastUsedDisplayMode);
        AssertTheEight(original);
    }

    [Fact]
    public void Change_WritesNothingToTheRecord()
    {
        var record = RecordWithTheEightSet();
        using var settings = WindowGallerySettings.FromRecord(record);
        settings.Start();

        settings.DockPosition.Value = GalleryDockPosition.Top;
        settings.StartsDocked.Value = false;
        settings.DockedGalleryItemSize.Value = 60;
        settings.ExpandedGalleryItemSize.Value = 60;
        settings.SetDockedStretchModeCommand.Execute(GalleryStretchMode.Uniform);
        settings.SetExpandedStretchModeCommand.Execute(GalleryStretchMode.Uniform);
        settings.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Masonry);
        settings.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);

        var expected = RecordWithTheEightSet();
        Assert.Equal(expected.DockPosition, record.DockPosition);
        Assert.Equal(expected.DockedGalleryStretchMode, record.DockedGalleryStretchMode);
        Assert.Equal(expected.ExpandedGalleryStretchMode, record.ExpandedGalleryStretchMode);
        Assert.Equal(expected.ExpandedGalleryDisplayMode, record.ExpandedGalleryDisplayMode);
        Assert.Equal(expected.RowsWasLastUsedDisplayMode, record.RowsWasLastUsedDisplayMode);
        Assert.Equal(expected.DockedGalleryItemSize, record.DockedGalleryItemSize);
        Assert.Equal(expected.ExpandedGalleryItemSize, record.ExpandedGalleryItemSize);
        Assert.Equal(expected.IsGalleryDocked, record.IsGalleryDocked);
    }

    [Theory]
    [InlineData(true, 1, GalleryDefaults.MinDockedGalleryItemHeight)]
    [InlineData(true, 100000, GalleryDefaults.MaxDockedGalleryItemHeight)]
    [InlineData(false, 1, GalleryDefaults.MinExpandedGalleryItemHeight)]
    [InlineData(false, 100000, GalleryDefaults.MaxExpandedGalleryItemHeight)]
    public void ItemSize_OutsideItsLimits_IsHeldToThem(bool docked, double set, double expected)
    {
        using var settings = WindowGallerySettings.FromRecord(null);
        settings.Start();
        var size = docked ? settings.DockedGalleryItemSize : settings.ExpandedGalleryItemSize;

        size.Value = set;

        Assert.Equal(expected, size.Value);
    }

    [Fact]
    public void DisplayModeCommands_BehaveAsTheSharedOnesDid()
    {
        using var settings = WindowGallerySettings.FromRecord(null);
        settings.Start();
        Assert.Equal(GalleryDisplayMode.Grid, settings.ExpandedGalleryDisplayMode.Value);
        Assert.False(settings.RowsWasLastUsedDisplayMode);

        settings.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);
        Assert.Equal(GalleryDisplayMode.Masonry, settings.ExpandedGalleryDisplayMode.Value);
        Assert.True(settings.IsExpandedDisplayModeMasonry.Value);
        Assert.True(settings.IsExpandedDisplayModeTiles.Value);

        settings.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows);
        Assert.Equal(GalleryDisplayMode.Rows, settings.ExpandedGalleryDisplayMode.Value);
        Assert.True(settings.RowsWasLastUsedDisplayMode);

        settings.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);
        Assert.Equal(GalleryDisplayMode.Grid, settings.ExpandedGalleryDisplayMode.Value);
        Assert.True(settings.RowsWasLastUsedDisplayMode);
        Assert.True(settings.IsExpandedDisplayModeGrid.Value);

        settings.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);
        Assert.Equal(GalleryDisplayMode.Rows, settings.ExpandedGalleryDisplayMode.Value);

        settings.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Masonry);
        Assert.False(settings.RowsWasLastUsedDisplayMode);
        settings.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Grid);
        Assert.False(settings.RowsWasLastUsedDisplayMode);
        settings.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);
        Assert.Equal(GalleryDisplayMode.Masonry, settings.ExpandedGalleryDisplayMode.Value);

        // An unknown mode sets Grid
        settings.SetExpandedDisplayModeCommand.Execute((GalleryDisplayMode)99);
        Assert.Equal(GalleryDisplayMode.Grid, settings.ExpandedGalleryDisplayMode.Value);
    }

    [Fact]
    public void StretchCommands_SetTheirMode()
    {
        using var settings = WindowGallerySettings.FromRecord(null);
        settings.Start();

        settings.SetDockedStretchModeCommand.Execute(GalleryStretchMode.FillSquare);
        settings.SetExpandedStretchModeCommand.Execute(GalleryStretchMode.Square);
        Assert.Equal(GalleryStretchMode.FillSquare, settings.DockedGalleryStretchMode.Value);
        Assert.Equal(GalleryStretchMode.Square, settings.ExpandedGalleryStretchMode.Value);
        Assert.True(settings.IsDockedStretchSquareFill.Value);
        Assert.True(settings.IsExpandedStretchSquare.Value);

        // An unknown mode is ignored
        settings.SetDockedStretchModeCommand.Execute((GalleryStretchMode)99);
        settings.SetExpandedStretchModeCommand.Execute((GalleryStretchMode)99);
        Assert.Equal(GalleryStretchMode.FillSquare, settings.DockedGalleryStretchMode.Value);
        Assert.Equal(GalleryStretchMode.Square, settings.ExpandedGalleryStretchMode.Value);
    }

    [Fact]
    public void Commands_BeforeStart_DoNothing_AndStartIsIdempotent()
    {
        using var settings = WindowGallerySettings.FromRecord(null);

        settings.SetDockedStretchModeCommand.Execute(GalleryStretchMode.Square);
        settings.SetExpandedStretchModeCommand.Execute(GalleryStretchMode.Square);
        settings.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows);
        settings.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);
        Assert.Equal(GalleryStretchMode.UniformToFill, settings.DockedGalleryStretchMode.Value);
        Assert.Equal(GalleryStretchMode.Uniform, settings.ExpandedGalleryStretchMode.Value);
        Assert.Equal(GalleryDisplayMode.Grid, settings.ExpandedGalleryDisplayMode.Value);

        settings.Start();
        settings.Start();

        // A second Start must not subscribe the toggle twice: two toggles would end back on Grid
        settings.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);
        Assert.Equal(GalleryDisplayMode.Masonry, settings.ExpandedGalleryDisplayMode.Value);
    }

    [Fact]
    public void Dispose_CommandsAndValuesAreDisposed()
    {
        var settings = WindowGallerySettings.FromRecord(null);
        settings.Start();
        Assert.False(settings.IsDisposed);

        settings.Dispose();
        settings.Dispose();

        Assert.True(settings.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => settings.DockPosition.Value = GalleryDockPosition.Top);
        Assert.Throws<ObjectDisposedException>(() => settings.StartsDocked.Value = true);
        Assert.Throws<ObjectDisposedException>(() => settings.DockedGalleryStretchMode.Value = GalleryStretchMode.Square);
        Assert.Throws<ObjectDisposedException>(() => settings.ExpandedGalleryStretchMode.Value = GalleryStretchMode.Square);
        Assert.Throws<ObjectDisposedException>(() => settings.ExpandedGalleryDisplayMode.Value = GalleryDisplayMode.Rows);
        Assert.Throws<ObjectDisposedException>(() => settings.DockedGalleryItemSize.Value = 60);
        Assert.Throws<ObjectDisposedException>(() => settings.ExpandedGalleryItemSize.Value = 60);
        Assert.Throws<ObjectDisposedException>(() => settings.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows));
        Assert.Throws<ObjectDisposedException>(() => settings.ToggleExpandedDisplayModeCommand.Execute(Unit.Default));
        Assert.Throws<ObjectDisposedException>(() => settings.SetDockedStretchModeCommand.Execute(GalleryStretchMode.Square));
        Assert.Throws<ObjectDisposedException>(() => settings.SetExpandedStretchModeCommand.Execute(GalleryStretchMode.Square));
        // A start after the dispose subscribes nothing and does not throw
        settings.Start();
    }
}
