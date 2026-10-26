using ImageMagick;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// The iterator is the only writer of its file list: each change keeps the current file current and tells the cache.
/// </summary>
[Collection("Sequential")]
public class ImageIteratorFileListTests
{
    private static readonly string TestDirectory = Path.Combine(Path.GetTempPath(), "PicViewIteratorFileListTests");

    private readonly SharedImageCache _cache;
    private readonly TabViewModel _tab;
    private readonly ImageIterator _iterator;

    public ImageIteratorFileListTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        Settings.Sorting.SortPreference = 0; // Name
        Settings.Sorting.Ascending = true;

        _cache = new SharedImageCache(static f =>
            ValueTask.FromResult(new ImageModel { FileInfo = f, Image = new ProbeImage() }));
        _tab = new TabViewModel(null!);
        _iterator = new ImageIterator(_cache, new ThumbnailCache(), new MockThumbnailLoader(), _tab);
    }

    [Fact]
    public void Insert_BeforeCurrent_CurrentStaysOnSameFile()
    {
        _iterator.Initialize([File("b.jpg"), File("d.jpg")], 1);

        var index = _iterator.Insert(File("a.jpg"), string.CompareOrdinal);

        Assert.Equal(0, index);
        Assert.Equal("d.jpg", CurrentName());
    }

    [Fact]
    public void Insert_AfterCurrent_CurrentUnchanged()
    {
        _iterator.Initialize([File("b.jpg"), File("d.jpg")]);

        var index = _iterator.Insert(File("c.jpg"), string.CompareOrdinal);

        Assert.Equal(1, index);
        Assert.Equal("b.jpg", CurrentName());
        Assert.Equal(["b.jpg", "c.jpg", "d.jpg"], Names());
    }

    [Fact]
    public void Remove_BeforeCurrent_CurrentStaysOnSameFile()
    {
        _iterator.Initialize([File("a.jpg"), File("b.jpg"), File("c.jpg")], 2);

        var index = _iterator.Remove(File("a.jpg").FullName);

        Assert.Equal(0, index);
        Assert.Equal("c.jpg", CurrentName());
    }

    [Fact]
    public void Remove_CurrentLastFile_CurrentMovesToNewLast()
    {
        _iterator.Initialize([File("a.jpg"), File("b.jpg")], 1);

        _iterator.Remove(File("b.jpg").FullName);

        Assert.Equal("a.jpg", CurrentName());
    }

    [Fact]
    public void Remove_FileNotInList_ReturnsMinusOneAndChangesNothing()
    {
        _iterator.Initialize([File("a.jpg")]);

        Assert.Equal(-1, _iterator.Remove(File("z.jpg").FullName));
        Assert.Equal(["a.jpg"], Names());
    }

    [Fact]
    public void Replace_SamePath_SwapsEntryInPlace()
    {
        _iterator.Initialize([File("a.jpg"), File("b.jpg")], 1);
        var refreshed = File("a.jpg");

        var index = _iterator.Replace(refreshed);

        Assert.Equal(0, index);
        Assert.Same(refreshed, _iterator.Files[0]);
        Assert.Equal("b.jpg", CurrentName());
    }

    [Fact]
    public void Move_CurrentFileRenamed_CurrentFollowsIt()
    {
        _iterator.Initialize([File("a.jpg"), File("b.jpg"), File("c.jpg")]);

        var (removed, inserted) = _iterator.Move(File("a.jpg").FullName, File("z.jpg"), string.CompareOrdinal);

        Assert.Equal(0, removed);
        Assert.Equal(2, inserted);
        Assert.Equal("z.jpg", CurrentName());
    }

    [Fact]
    public void Move_OtherFileRenamedPastCurrent_CurrentStaysOnSameFile()
    {
        _iterator.Initialize([File("a.jpg"), File("b.jpg"), File("c.jpg")], 1);

        _iterator.Move(File("a.jpg").FullName, File("d.jpg"), string.CompareOrdinal);

        Assert.Equal("b.jpg", CurrentName());
        Assert.Equal(["b.jpg", "c.jpg", "d.jpg"], Names());
    }

    [Fact]
    public void Insert_EarlierSnapshot_IsNotChanged()
    {
        _iterator.Initialize([File("a.jpg")]);
        var snapshot = _iterator.Files;

        _iterator.Insert(File("b.jpg"), string.CompareOrdinal);

        Assert.Single(snapshot);
        Assert.Equal(2, _iterator.Files.Count);
    }

    [Fact]
    public async Task Remove_FileWhoseImageTheTabHeld_CacheReleasesIt()
    {
        var removed = File("a.jpg");
        _iterator.Initialize([removed, File("b.jpg")], 1);
        var model = await _cache.GetAsync(_tab.Id, removed, TestContext.Current.CancellationToken);

        _iterator.Remove(removed.FullName);
        _cache.ForceDisposalQueue();

        Assert.True(Assert.IsType<ProbeImage>(model!.Image).IsDisposed);
    }

    [Fact]
    public async Task IterateToIndex_AfterAPair_ClearsTheSecondary()
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        var files = new List<FileInfo> { File("a.jpg"), File("b.jpg"), File("c.jpg") };
        _iterator.Initialize(files, 0);
        _tab.Display(new ImageModel { FileInfo = files[0], Image = new ProbeImage() });
        var ct = new CancellationTokenSource();
        await _iterator.IterateToIndicesAsync(0, 1, ct);
        Assert.Equal(1, _iterator.SecondaryCurrentIndex);
        Assert.NotNull(_tab.SecondaryModel);

        await _iterator.IterateToIndexAsync(2, ct);

        Assert.Equal(-1, _iterator.SecondaryCurrentIndex);
        Assert.Null(_tab.SecondaryModel);
    }

    [Fact]
    public async Task NavigateAsync_SideBySide_UsesIterateToIndicesNotIterateToIndex()
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        var files = new List<FileInfo> { File("a.jpg"), File("b.jpg"), File("c.jpg"), File("d.jpg") };
        _iterator.Initialize(files, 0);
        _tab.Display(new ImageModel { FileInfo = files[0], Image = new ProbeImage() });

        await _iterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, new CancellationTokenSource());

        // IterateToIndexAsync always clears the secondary, so a secondary means the pair path was taken
        Assert.Equal(2, _iterator.CurrentIndex);
        Assert.Equal(3, _iterator.SecondaryCurrentIndex);
        Assert.Same(files[3], _tab.SecondaryModel?.FileInfo);
    }

    [Fact]
    public async Task RepeatNavigate_SideBySideNext_StepsByPairs()
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        var files = new List<FileInfo> { File("a.jpg"), File("b.jpg"), File("c.jpg"), File("d.jpg"), File("e.jpg") };
        _iterator.Initialize(files, 0);
        _tab.Display(new ImageModel { FileInfo = files[0], Image = new ProbeImage() });

        await _iterator.RepeatNavigateAsync(NavigateTo.Next, TimeSpan.Zero, new CancellationTokenSource());

        Assert.Equal(2, _iterator.CurrentIndex);
        Assert.Equal(3, _iterator.SecondaryCurrentIndex);
    }

    [Fact]
    public async Task RepeatNavigate_SideBySidePrevious_StepsByPairs()
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        var files = new List<FileInfo> { File("a.jpg"), File("b.jpg"), File("c.jpg"), File("d.jpg"), File("e.jpg") };
        _iterator.Initialize(files, 2);
        _tab.Display(new ImageModel { FileInfo = files[2], Image = new ProbeImage() });

        await _iterator.RepeatNavigateAsync(NavigateTo.Previous, TimeSpan.Zero, new CancellationTokenSource());

        Assert.Equal(0, _iterator.CurrentIndex);
        Assert.Equal(1, _iterator.SecondaryCurrentIndex);
    }

    [Fact]
    public async Task NavigateAsync_OneImageSideBySide_ShowsNoSecondary()
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        var files = new List<FileInfo> { File("only.jpg") };
        _iterator.Initialize(files, 0);
        _tab.Display(new ImageModel { FileInfo = files[0], Image = new ProbeImage() });

        await _iterator.NavigateAsync(NavigateTo.Next, SkipAmount.None, new CancellationTokenSource());

        Assert.Equal(-1, _iterator.SecondaryCurrentIndex);
        Assert.Null(_tab.SecondaryModel);
    }

    private static FileInfo File(string name) => new(Path.Combine(TestDirectory, name));

    private string CurrentName() => _iterator.Files[_iterator.CurrentIndex].Name;

    private string[] Names() => _iterator.Files.Select(f => f.Name).ToArray();

    private sealed class ProbeImage : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null) => ValueTask.FromResult<object?>(null);
        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
