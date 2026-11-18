using PicView.Core.Gallery;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Gallery;

[Collection("Sequential")]
public class GallerySharedSettingsViewModelTests
{
    private int _saves;

    public GallerySharedSettingsViewModelTests()
    {
        SetDefaults();
    }

    private GallerySharedSettingsViewModel Create() => StartedGallerySettings.New(CountingSave);

    private GallerySharedSettingsViewModel Unstarted() => new(CountingSave);

    private ValueTask<bool> CountingSave()
    {
        _saves++;
        return ValueTask.FromResult(true);
    }

    [Theory]
    [InlineData(GalleryDisplayMode.Columns, true, false, false, false)]
    [InlineData(GalleryDisplayMode.Masonry, false, true, false, true)]
    [InlineData(GalleryDisplayMode.Rows, false, false, true, true)]
    public void Constructed_StoredMode_PublishesItAndItsFlagsWithoutSaving(GalleryDisplayMode stored, bool columns,
        bool masonry, bool rows, bool tiles)
    {
        Settings.Gallery.ExpandedGalleryDisplayMode = stored;

        var shared = Unstarted();

        Assert.Equal(stored, shared.ExpandedGalleryDisplayMode.Value);
        Assert.Equal(columns, shared.IsExpandedDisplayModeColumns.Value);
        Assert.Equal(masonry, shared.IsExpandedDisplayModeMasonry.Value);
        Assert.Equal(rows, shared.IsExpandedDisplayModeRows.Value);
        Assert.Equal(tiles, shared.IsExpandedDisplayModeTiles.Value);
        Assert.Equal(0, _saves);
    }

    [Theory]
    [InlineData(GalleryDisplayMode.Masonry, false)]
    [InlineData(GalleryDisplayMode.Rows, true)]
    public void SetExpandedDisplayModeCommand_TileMode_WritesTheRecordAndSavesOnce(GalleryDisplayMode mode,
        bool rowsWasLastUsed)
    {
        Settings.Gallery.RowsWasLastUsedDisplayMode = !rowsWasLastUsed;
        var shared = Create();

        shared.SetExpandedDisplayModeCommand.Execute(mode);

        Assert.Equal(mode, shared.ExpandedGalleryDisplayMode.Value);
        Assert.Equal(mode, Settings.Gallery.ExpandedGalleryDisplayMode);
        Assert.Equal(rowsWasLastUsed, Settings.Gallery.RowsWasLastUsedDisplayMode);
        Assert.True(shared.IsExpandedDisplayModeTiles.Value);
        Assert.False(shared.IsExpandedDisplayModeColumns.Value);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public void SetExpandedDisplayModeCommand_SameUnknownOrColumns_SavesOnlyAChangeAndKeepsTheLastUsedTileMode()
    {
        var shared = Create();

        shared.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Columns);
        Assert.Equal(0, _saves);

        shared.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows);
        shared.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows);
        Assert.Equal(1, _saves);

        shared.SetExpandedDisplayModeCommand.Execute((GalleryDisplayMode)7);
        Assert.Equal(GalleryDisplayMode.Columns, shared.ExpandedGalleryDisplayMode.Value);
        Assert.Equal(GalleryDisplayMode.Columns, Settings.Gallery.ExpandedGalleryDisplayMode);
        Assert.True(Settings.Gallery.RowsWasLastUsedDisplayMode);
        Assert.Equal(2, _saves);
    }

    [Fact]
    public void ToggleExpandedDisplayModeCommand_FromColumns_GoesToTheLastUsedTileMode()
    {
        var shared = Create();

        shared.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);
        Assert.Equal(GalleryDisplayMode.Masonry, shared.ExpandedGalleryDisplayMode.Value);
        shared.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);
        Assert.Equal(GalleryDisplayMode.Columns, shared.ExpandedGalleryDisplayMode.Value);

        shared.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows);
        shared.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);
        Assert.Equal(GalleryDisplayMode.Columns, shared.ExpandedGalleryDisplayMode.Value);
        shared.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);
        Assert.Equal(GalleryDisplayMode.Rows, Settings.Gallery.ExpandedGalleryDisplayMode);
        Assert.Equal(5, _saves);
    }

    [Fact]
    public void ToggleExpandedDisplayModeCommand_RowsLastUsedInTheStoredSettings_GoesToRows()
    {
        Settings.Gallery.RowsWasLastUsedDisplayMode = true;
        var shared = Create();

        shared.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);

        Assert.Equal(GalleryDisplayMode.Rows, shared.ExpandedGalleryDisplayMode.Value);
    }

    [Fact]
    public void WriteThrough_LaterSubscriber_ReadsTheNewRecord()
    {
        var shared = Create();
        GalleryDisplayMode? stored = null;
        var rowsWasLastUsed = false;
        using var subscription = shared.ExpandedGalleryDisplayMode.Skip(1).Subscribe(_ =>
        {
            stored = Settings.Gallery.ExpandedGalleryDisplayMode;
            rowsWasLastUsed = Settings.Gallery.RowsWasLastUsedDisplayMode;
        });

        shared.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows);

        Assert.Equal(GalleryDisplayMode.Rows, stored);
        Assert.True(rowsWasLastUsed);
    }

    [Fact]
    public async Task WriteThrough_SaveThrows_KeepsWritingTheRecord()
    {
        var shared = new GallerySharedSettingsViewModel(() => throw new IOException("disk full"));
        shared.Start();

        shared.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Masonry);
        await shared.Saving;
        shared.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows);
        await shared.Saving;

        Assert.Equal(GalleryDisplayMode.Rows, shared.ExpandedGalleryDisplayMode.Value);
        Assert.Equal(GalleryDisplayMode.Rows, Settings.Gallery.ExpandedGalleryDisplayMode);
    }

    public static TheoryData<bool, GalleryStretchMode, bool, bool, bool, bool> Stretches => new()
    {
        // docked, mode, uniform, uniformToFill, square, squareFill
        { true, GalleryStretchMode.Uniform, true, false, false, false },
        { true, GalleryStretchMode.Square, false, false, true, false },
        { true, GalleryStretchMode.FillSquare, false, false, false, true },
        { false, GalleryStretchMode.UniformToFill, false, true, false, false },
        { false, GalleryStretchMode.Square, false, false, true, false },
        { false, GalleryStretchMode.FillSquare, false, false, false, true },
    };

    [Theory, MemberData(nameof(Stretches))]
    public void SetStretchModeCommand_AnotherMode_WritesTheRecordSavesOnceAndSetsOneFlag(bool docked,
        GalleryStretchMode mode, bool uniform, bool uniformToFill, bool square, bool squareFill)
    {
        var shared = Create();

        (docked ? shared.SetDockedStretchModeCommand : shared.SetExpandedStretchModeCommand).Execute(mode);

        Assert.Equal(mode, docked ? Settings.Gallery.DockedGalleryStretchMode : Settings.Gallery.ExpandedGalleryStretchMode);
        Assert.Equal(mode, (docked ? shared.DockedGalleryStretchMode : shared.ExpandedGalleryStretchMode).Value);
        Assert.Equal(uniform, (docked ? shared.IsDockedStretchUniform : shared.IsExpandedStretchUniform).Value);
        Assert.Equal(uniformToFill, (docked ? shared.IsDockedStretchUniformToFill : shared.IsExpandedStretchUniformToFill).Value);
        Assert.Equal(square, (docked ? shared.IsDockedStretchSquare : shared.IsExpandedStretchSquare).Value);
        Assert.Equal(squareFill, (docked ? shared.IsDockedStretchSquareFill : shared.IsExpandedStretchSquareFill).Value);
        Assert.Equal(1, _saves);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetStretchModeCommand_SameOrUnknownMode_WritesAndSavesNothing(bool docked)
    {
        var shared = Create();
        var stored = docked ? Settings.Gallery.DockedGalleryStretchMode : Settings.Gallery.ExpandedGalleryStretchMode;
        var command = docked ? shared.SetDockedStretchModeCommand : shared.SetExpandedStretchModeCommand;

        command.Execute(stored);
        command.Execute((GalleryStretchMode)2);

        Assert.Equal(stored, (docked ? shared.DockedGalleryStretchMode : shared.ExpandedGalleryStretchMode).Value);
        Assert.Equal(stored, docked ? Settings.Gallery.DockedGalleryStretchMode : Settings.Gallery.ExpandedGalleryStretchMode);
        Assert.Equal(stored == GalleryStretchMode.Uniform,
            (docked ? shared.IsDockedStretchUniform : shared.IsExpandedStretchUniform).Value);
        Assert.Equal(stored == GalleryStretchMode.UniformToFill,
            (docked ? shared.IsDockedStretchUniformToFill : shared.IsExpandedStretchUniformToFill).Value);
        Assert.Equal(stored == GalleryStretchMode.Square,
            (docked ? shared.IsDockedStretchSquare : shared.IsExpandedStretchSquare).Value);
        Assert.Equal(stored == GalleryStretchMode.FillSquare,
            (docked ? shared.IsDockedStretchSquareFill : shared.IsExpandedStretchSquareFill).Value);
        Assert.Equal(0, _saves);
    }

    [Fact]
    public void SizesSpacingsAndBadge_Changed_WriteTheRecordAndSaveThroughTheInjectedSave()
    {
        var shared = Create();

        shared.DockedGalleryItemSize.Value = 77;
        shared.ExpandedGalleryItemSize.Value = 222;
        shared.GalleryItemSpacing.Value = 12;
        shared.GalleryLineSpacing.Value = 9;
        shared.ShowMotionPhotoBadges.Value = !Settings.Gallery.ShowMotionPhotoBadges;

        Assert.Equal(77, Settings.Gallery.DockedGalleryItemSize);
        Assert.Equal(222, Settings.Gallery.ExpandedGalleryItemSize);
        Assert.Equal(12, Settings.Gallery.ItemSpacing);
        Assert.Equal(9, Settings.Gallery.LineSpacing);
        Assert.Equal(shared.ShowMotionPhotoBadges.Value, Settings.Gallery.ShowMotionPhotoBadges);
        Assert.Equal(5, _saves);
    }

    [Fact]
    public void AnySetting_ChangedBeforeStart_WritesAndSavesNothing()
    {
        var shared = Unstarted();
        object Stored() => (Settings.Gallery.DockedGalleryStretchMode, Settings.Gallery.ExpandedGalleryStretchMode,
            Settings.Gallery.DockedGalleryItemSize, Settings.Gallery.ExpandedGalleryItemSize,
            Settings.Gallery.ItemSpacing, Settings.Gallery.LineSpacing, Settings.Gallery.ShowMotionPhotoBadges,
            Settings.Gallery.ExpandedGalleryDisplayMode);
        var before = Stored();

        shared.SetDockedStretchModeCommand.Execute(GalleryStretchMode.Square);
        shared.SetExpandedStretchModeCommand.Execute(GalleryStretchMode.Square);
        shared.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows);
        shared.DockedGalleryItemSize.Value = 77;
        shared.ExpandedGalleryItemSize.Value = 222;
        shared.GalleryItemSpacing.Value = 12;
        shared.GalleryLineSpacing.Value = 9;
        shared.ShowMotionPhotoBadges.Value = !Settings.Gallery.ShowMotionPhotoBadges;

        Assert.Equal(before, Stored());
        Assert.Equal(0, _saves);
    }

    [Fact]
    public void Start_CalledTwiceAndFromManyThreads_SavesOncePerChange()
    {
        var shared = Unstarted();

        Parallel.For(0, 16, _ => shared.Start());
        shared.Start();
        shared.DockedGalleryItemSize.Value = 77;

        Assert.Equal(1, _saves);
    }

    [Fact]
    public async Task ItemSize_ChangedManyTimesWhileOneSaveRuns_SavesOnceMoreWithTheLastValue()
    {
        var saves = 0;
        var gate = new TaskCompletionSource<bool>();
        var shared = StartedGallerySettings.New(async () => { saves++; return await gate.Task; });

        for (var size = 60; size <= 100; size += 10)
        {
            shared.DockedGalleryItemSize.Value = size;
        }
        gate.SetResult(true);
        await shared.Saving;

        Assert.Equal(100, Settings.Gallery.DockedGalleryItemSize);
        Assert.Equal(2, saves);
    }

    [Fact]
    public async Task ItemSize_SaveThrows_KeepsWritingTheRecord()
    {
        var shared = StartedGallerySettings.New(() => throw new IOException("disk full"));

        shared.DockedGalleryItemSize.Value = 77;
        await shared.Saving;
        shared.DockedGalleryItemSize.Value = 88;
        await shared.Saving;

        Assert.Equal(88, Settings.Gallery.DockedGalleryItemSize);
    }
}
