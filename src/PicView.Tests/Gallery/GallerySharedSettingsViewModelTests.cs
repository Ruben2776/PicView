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

    private GallerySharedSettingsViewModel Create() => new(() =>
    {
        _saves++;
        return ValueTask.FromResult(true);
    });

    [Theory]
    [InlineData(GalleryDisplayMode.Columns, true, false, false, false)]
    [InlineData(GalleryDisplayMode.Masonry, false, true, false, true)]
    [InlineData(GalleryDisplayMode.Rows, false, false, true, true)]
    public void Constructed_StoredMode_PublishesItAndItsFlagsWithoutSaving(GalleryDisplayMode stored, bool columns,
        bool masonry, bool rows, bool tiles)
    {
        Settings.Gallery.ExpandedGalleryDisplayMode = stored;

        var shared = Create();

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

        shared.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Masonry);
        await shared.Saving;
        shared.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows);
        await shared.Saving;

        Assert.Equal(GalleryDisplayMode.Rows, shared.ExpandedGalleryDisplayMode.Value);
        Assert.Equal(GalleryDisplayMode.Rows, Settings.Gallery.ExpandedGalleryDisplayMode);
    }
}
