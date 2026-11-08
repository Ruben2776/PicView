using ImageMagick;
using PicView.Core.ArchiveHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// A tab derives whether Next and Prev run from its own state (ADR-0017): its position in its files, its Archive
/// Openings state, its Crop, whether it has an iterator, and the two navigation settings. Nothing else writes them.
/// </summary>
[Collection("Sequential")]
public class TabCanNavigateTests
{
    private readonly TabViewModel _tab;

    public TabCanNavigateTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        _tab = new TabViewModel(null!,
            TestTabServices.For(new SharedImageCache(f => ValueTask.FromResult(Model(f.Name))!), new ThumbnailCache(),
                new MockThumbnailLoader()));
    }

    private static ImageModel Model(string name) => new()
    {
        FileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "PicViewTabCanNavigateTests", name)),
        Image = new object(),
        PixelWidth = 1,
        PixelHeight = 1
    };

    /// <summary>Gives the tab an iterator browsing <paramref name="names"/>, at the first.</summary>
    private FileInfo[] Browse(params string[] names)
    {
        var files = names.Select(n => Model(n).FileInfo!).ToArray();
        _tab.Initialize();
        _tab.ImageIterator!.Initialize(files, 0);
        return files;
    }

    private bool Forwards => _tab.CanNavigateForwards.Value;
    private bool Backwards => _tab.CanNavigateBackwards.Value;

    [Fact]
    public void FreshTab_BothFalse()
    {
        Assert.False(Forwards);
        Assert.False(Backwards);
    }

    [Fact]
    public void Browse_ThreeAtFirst_ForwardsOnly()
    {
        Browse("a.jpg", "b.jpg", "c.jpg");

        Assert.True(Forwards);
        Assert.False(Backwards);
    }

    [Fact]
    public void Initialize_ThreeAtLast_BackwardsOnly()
    {
        var files = Browse("a.jpg", "b.jpg", "c.jpg");

        _tab.ImageIterator!.Initialize(files, 2);

        Assert.False(Forwards);
        Assert.True(Backwards);
    }

    [Fact]
    public void Browse_OneFile_BothFalse()
    {
        Browse("a.jpg");

        Assert.False(Forwards);
        Assert.False(Backwards);
    }

    [Fact]
    public void Looping_OneFile_BothFalse()
    {
        _tab.Services!.GlobalSettings.IsLooping.Value = true;

        Browse("a.jpg");

        Assert.False(Forwards);
        Assert.False(Backwards);
    }

    [Fact]
    public void Looping_TurnedOnAtLast_ForwardsTrue_ThenOffFalse()
    {
        var files = Browse("a.jpg", "b.jpg", "c.jpg");
        _tab.ImageIterator!.Initialize(files, 2);

        _tab.Services!.GlobalSettings.IsLooping.Value = true;
        Assert.True(Forwards);

        _tab.Services.GlobalSettings.IsLooping.Value = false;
        Assert.False(Forwards);
    }

    [Fact]
    public void SideBySide_IndexOneOfThree_ForwardsFalse()
    {
        _tab.Services!.GlobalSettings.IsSideBySide.Value = true;
        var files = Browse("a.jpg", "b.jpg", "c.jpg");

        _tab.ImageIterator!.Initialize(files, 1);
        Assert.False(Forwards);

        _tab.ImageIterator.Initialize(files, 0);
        Assert.True(Forwards);
    }

    [Fact]
    public void SideBySide_TwoFilesAtFirst_BothFalse()
    {
        _tab.Services!.GlobalSettings.IsSideBySide.Value = true;

        Browse("a.jpg", "b.jpg");

        Assert.False(Forwards);
        Assert.False(Backwards);
    }

    [Fact]
    public void Extracting_OneFile_ForwardsOnly_UntilFinished()
    {
        Browse("a.jpg");

        _tab.Archives.SetStateForTest(ExtractionState.Extracting);
        Assert.True(Forwards);
        Assert.False(Backwards);

        _tab.Archives.SetStateForTest(ExtractionState.Finished);
        Assert.False(Forwards);
    }

    [Fact]
    public void Crop_Set_BothFalse_Cleared_FollowsListNow()
    {
        var files = Browse("a.jpg", "b.jpg", "c.jpg");
        _tab.ImageIterator!.Initialize(files, 1);

        _tab.Crop = new CropViewModel();
        Assert.False(Forwards);
        Assert.False(Backwards);

        // The list changes during the crop, so the flags it gets back are the list's now, not the saved ones
        _tab.ImageIterator.Insert(Model("0.jpg").FileInfo!, string.CompareOrdinal);
        _tab.Crop = null;
        Assert.Equal(2, _tab.NavigationIndex.CurrentValue);
        Assert.Equal(4, _tab.MaxIndex.CurrentValue);
        Assert.True(Forwards);
        Assert.True(Backwards);
    }

    [Fact]
    public void DisposeImageIterator_BothFalse_SingleImageStaysFalse()
    {
        Browse("a.jpg", "b.jpg", "c.jpg");

        _tab.DisposeImageIterator();
        Assert.False(Forwards);
        Assert.False(Backwards);

        var single = new ImageModel { Image = new object(), PixelWidth = 1, PixelHeight = 1 };
        _tab.DisplaySingleImage(single, SingleImageType.Base64, "Pasted");
        Assert.False(Forwards);
        Assert.False(Backwards);
    }

    [Fact]
    public void DisposeThenBrowse_FollowsNewList()
    {
        var files = Browse("a.jpg", "b.jpg", "c.jpg");
        _tab.ImageIterator!.Initialize(files, 2);
        _tab.DisposeImageIterator();

        Browse("x.jpg", "y.jpg");

        Assert.True(Forwards);
        Assert.False(Backwards);
    }

    [Fact]
    public void Insert_BeforeShown_FollowsNewPosition()
    {
        Browse("b.jpg", "c.jpg");

        _tab.ImageIterator!.Insert(Model("a.jpg").FileInfo!, string.CompareOrdinal);

        Assert.Equal(1, _tab.NavigationIndex.CurrentValue);
        Assert.Equal(3, _tab.MaxIndex.CurrentValue);
        Assert.True(Forwards);
        Assert.True(Backwards);
    }

    [Fact]
    public void Dispose_ThenArchiveStateChanges_DerivesNothing()
    {
        Browse("a.jpg");
        var archives = _tab.Archives;

        _tab.Dispose();

        archives.SetStateForTest(ExtractionState.Extracting);

        // The subscription went with the tab, so nothing derived: a live one would turn forwards on for the extraction
        Assert.False(Forwards);
        Assert.False(Backwards);
    }

    [Fact]
    public void DisposeThenInitializeImageIterator_NoShownFile_BothFalse()
    {
        var files = Browse("a.jpg", "b.jpg", "c.jpg");
        _tab.ImageIterator!.Initialize(files, 2);
        _tab.DisposeImageIterator();

        // No file is shown, so the new iterator lists nothing yet; the old list's position must not light the buttons
        _tab.InitializeImageIterator(files);

        Assert.False(Forwards);
        Assert.False(Backwards);
    }

    [Fact]
    public void Flags_ArePublishedReadOnly()
    {
        var forwards = typeof(TabViewModel).GetProperty(nameof(TabViewModel.CanNavigateForwards))!.PropertyType;
        var backwards = typeof(TabViewModel).GetProperty(nameof(TabViewModel.CanNavigateBackwards))!.PropertyType;

        Assert.Equal(typeof(IReadOnlyBindableReactiveProperty<bool>), forwards);
        Assert.Equal(typeof(IReadOnlyBindableReactiveProperty<bool>), backwards);
    }

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);

        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null,
            MagickImage? magick = null) => ValueTask.FromResult<object?>(null);

        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
