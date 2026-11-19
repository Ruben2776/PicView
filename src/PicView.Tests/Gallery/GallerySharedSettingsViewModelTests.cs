using PicView.Core.ViewModels;

namespace PicView.Tests.Gallery;

/// <summary>
/// The three gallery settings that stay app-wide: the item and line spacing and the motion photo badges. The look,
/// which belongs to each Main Window, is pinned by WindowGallerySettingsTests.
/// </summary>
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

    [Fact]
    public void SpacingsAndBadge_Changed_WriteTheRecordAndSaveThroughTheInjectedSave()
    {
        var shared = Create();

        shared.GalleryItemSpacing.Value = 12;
        shared.GalleryLineSpacing.Value = 9;
        shared.ShowMotionPhotoBadges.Value = !Settings.Gallery.ShowMotionPhotoBadges;

        Assert.Equal(12, Settings.Gallery.ItemSpacing);
        Assert.Equal(9, Settings.Gallery.LineSpacing);
        Assert.Equal(shared.ShowMotionPhotoBadges.Value, Settings.Gallery.ShowMotionPhotoBadges);
        Assert.Equal(3, _saves);
    }

    [Fact]
    public void AnySetting_ChangedBeforeStart_WritesAndSavesNothing()
    {
        var shared = Unstarted();
        object Stored() => (Settings.Gallery.ItemSpacing, Settings.Gallery.LineSpacing,
            Settings.Gallery.ShowMotionPhotoBadges);
        var before = Stored();

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
        shared.GalleryItemSpacing.Value = 12;

        Assert.Equal(1, _saves);
    }

    [Fact]
    public async Task ItemSpacing_ChangedManyTimesWhileOneSaveRuns_SavesOnceMoreWithTheLastValue()
    {
        var saves = 0;
        var gate = new TaskCompletionSource<bool>();
        var shared = StartedGallerySettings.New(async () => { saves++; return await gate.Task; });

        for (var spacing = 10; spacing <= 50; spacing += 10)
        {
            shared.GalleryItemSpacing.Value = spacing;
        }
        gate.SetResult(true);
        await shared.Saving;

        Assert.Equal(50, Settings.Gallery.ItemSpacing);
        Assert.Equal(2, saves);
    }

    [Fact]
    public async Task ItemSpacing_SaveThrows_KeepsWritingTheRecord()
    {
        var shared = StartedGallerySettings.New(() => throw new IOException("disk full"));

        shared.GalleryItemSpacing.Value = 12;
        await shared.Saving;
        shared.GalleryItemSpacing.Value = 14;
        await shared.Saving;

        Assert.Equal(14, Settings.Gallery.ItemSpacing);
    }
}
