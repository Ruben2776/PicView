using System.Collections.Concurrent;
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

    // How long Next gets to end before the file it waits for comes, which is the bug the extraction tests catch
    private static readonly TimeSpan EarlyEndWait = TimeSpan.FromMilliseconds(500);

    // Bounds each wait, so a failing test fails instead of hanging the run
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

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
    public async Task IterateToIndices_PageInsertedBeforeThePairWhileItLoads_ShowsThePair()
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        var held = new TaskCompletionSource<ImageModel>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cache = new SharedImageCache(file => file.Name == "e.jpg"
            ? new ValueTask<ImageModel>(held.Task)
            : ValueTask.FromResult(new ImageModel { FileInfo = file, Image = new ProbeImage() }));
        var iterator = new ImageIterator(cache, new ThumbnailCache(), new MockThumbnailLoader(), _tab);
        var c = File("c.jpg");
        var e = File("e.jpg");
        iterator.Initialize([c, File("d.jpg"), e, File("f.jpg")], 0);
        _tab.Display(new ImageModel { FileInfo = c, Image = new ProbeImage() });

        // An archive's extraction inserts a page before the pair while the pair still loads
        var next = iterator.IterateToIndicesAsync(2, 3, new CancellationTokenSource()).AsTask();
        iterator.Insert(File("b.jpg"), string.CompareOrdinal);
        held.SetResult(new ImageModel { FileInfo = e, Image = new ProbeImage() });
        await next.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal("e.jpg", _tab.Model.FileInfo?.Name);
        Assert.Equal("f.jpg", _tab.SecondaryModel?.FileInfo?.Name);
        Assert.Equal((3, 4), (iterator.CurrentIndex, iterator.SecondaryCurrentIndex));
    }

    [Fact]
    public async Task IterateToIndex_AnotherStepLandsOnItsOldIndexWhileItLoads_ShowsTheOtherStep()
    {
        var held = new TaskCompletionSource<ImageModel>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cache = new SharedImageCache(file => file.Name == "c.jpg"
            ? new ValueTask<ImageModel>(held.Task)
            : ValueTask.FromResult(new ImageModel { FileInfo = file, Image = new ProbeImage() }));
        var iterator = new ImageIterator(cache, new ThumbnailCache(), new MockThumbnailLoader(), _tab);
        var b = File("b.jpg");
        var c = File("c.jpg");
        iterator.Initialize([b, c, File("d.jpg")], 0);
        _tab.Display(new ImageModel { FileInfo = b, Image = new ProbeImage() });

        // A slow Next to c, then an archive's extraction inserts a page before it, so a Previous lands on c's old index
        var next = iterator.IterateToIndexAsync(1, new CancellationTokenSource()).AsTask();
        iterator.Insert(File("a.jpg"), string.CompareOrdinal);
        await iterator.NavigateAsync(NavigateTo.Previous, SkipAmount.One, new CancellationTokenSource())
            .AsTask().WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        held.SetResult(new ImageModel { FileInfo = c, Image = new ProbeImage() });
        await next.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal("b.jpg", _tab.Model.FileInfo?.Name);
        Assert.Equal(1, iterator.CurrentIndex);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Next_AtTheLastFileWhileExtracting_WaitsForTheFileAfterTheShownOne(bool keyHeldDown)
    {
        var c = File("c.jpg");
        var shown = new ImageModel { FileInfo = c, Image = new ProbeImage() };
        _iterator.Initialize([c], 0);
        _tab.Display(shown);
        _tab.IsArchiveExtracting.Value = true;

        var next = Next(keyHeldDown);
        // An archive reopened at a page extracts pages before it too
        _iterator.Insert(File("b.jpg"), string.CompareOrdinal);
        await Task.WhenAny(next, Task.Delay(EarlyEndWait, TestContext.Current.CancellationToken));
        var endedEarly = next.IsCompleted;
        var stillShown = ReferenceEquals(shown, _tab.Model);
        _iterator.Insert(File("d.jpg"), string.CompareOrdinal);
        await next.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.False(endedEarly, "A page before the shown one ended the wait");
        Assert.True(stillShown, "The shown file was shown again");
        Assert.Equal("d.jpg", _tab.Model.FileInfo?.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Next_SideBySideAtTheLastPairWhileExtracting_WaitsForThePairAfterTheShownOne(bool keyHeldDown)
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        // Looping would otherwise go round to the first pair
        Settings.UIProperties.Looping = true;
        var c = File("c.jpg");
        var d = File("d.jpg");
        var shown = new ImageModel { FileInfo = c, Image = new ProbeImage() };
        _iterator.Initialize([c, d], 0);
        _tab.Display(shown, new ImageModel { FileInfo = d, Image = new ProbeImage() });
        _tab.IsArchiveExtracting.Value = true;

        var next = Next(keyHeldDown);
        _iterator.Insert(File("b.jpg"), string.CompareOrdinal);
        // Half of the next pair
        _iterator.Insert(File("e.jpg"), string.CompareOrdinal);
        await Task.WhenAny(next, Task.Delay(EarlyEndWait, TestContext.Current.CancellationToken));
        var endedEarly = next.IsCompleted;
        var stillShown = ReferenceEquals(shown, _tab.Model);
        _iterator.Insert(File("f.jpg"), string.CompareOrdinal);
        await next.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.False(endedEarly, "The pair after the shown one was not there yet");
        Assert.True(stillShown, "Another pair was shown first");
        Assert.Equal("e.jpg", _tab.Model.FileInfo?.Name);
        Assert.Equal("f.jpg", _tab.SecondaryModel?.FileInfo?.Name);
    }

    [Fact]
    public async Task Next_WaitingWhileExtracting_GivesUpWhenAnotherFileIsShown()
    {
        var c = File("c.jpg");
        _iterator.Initialize([c], 0);
        _tab.Display(new ImageModel { FileInfo = c, Image = new ProbeImage() });
        _tab.IsArchiveExtracting.Value = true;
        var next = Next(keyHeldDown: false);
        _iterator.Insert(File("b.jpg"), string.CompareOrdinal);

        // As a gallery click would, while Next still waits
        await _iterator.IterateToIndexAsync(0, new CancellationTokenSource());
        _iterator.Insert(File("d.jpg"), string.CompareOrdinal);
        await next.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal("b.jpg", _tab.Model.FileInfo?.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Next_SideBySideWithOnePageLeftWhenTheExtractionEnds_ShowsTheLastPair(bool keyHeldDown)
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        var a = File("a.jpg");
        var b = File("b.jpg");
        _iterator.Initialize([a, b, File("c.jpg")], 0);
        _tab.Display(new ImageModel { FileInfo = a, Image = new ProbeImage() },
            new ImageModel { FileInfo = b, Image = new ProbeImage() });
        _tab.IsArchiveExtracting.Value = true;

        var next = Next(keyHeldDown);
        // The pair after the shown one never comes, as c is the last page
        EndExtraction();
        await next.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal("b.jpg", _tab.Model.FileInfo?.Name);
        Assert.Equal("c.jpg", _tab.SecondaryModel?.FileInfo?.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Next_LoopingAtTheLastPageWhenTheExtractionEnds_GoesRoundToTheFirst(bool keyHeldDown)
    {
        // As after an archive reopened at a page, whose pages before it come last
        Settings.UIProperties.Looping = true;
        var c = File("c.jpg");
        _iterator.Initialize([File("a.jpg"), File("b.jpg"), c], 2);
        _tab.Display(new ImageModel { FileInfo = c, Image = new ProbeImage() });
        _tab.IsArchiveExtracting.Value = true;

        var next = Next(keyHeldDown);
        EndExtraction();
        await next.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal("a.jpg", _tab.Model.FileInfo?.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Next_WaitingWhileExtracting_WhenCancelled_ShowsTheTitleAgain(bool keyHeldDown)
    {
        var c = File("c.jpg");
        _iterator.Initialize([c], 0);
        _tab.IsSelected = true;
        _tab.Display(new ImageModel { FileInfo = c, Image = new ProbeImage() });
        _tab.UpdateTabTitle();
        var title = _tab.Title!.Value;
        _tab.IsArchiveExtracting.Value = true;
        var cts = new CancellationTokenSource();

        var next = keyHeldDown
            ? _iterator.RepeatNavigateAsync(NavigateTo.Next, TimeSpan.Zero, cts).AsTask()
            : _iterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, cts).AsTask();
        var titleWhileWaiting = _tab.Title.Value;
        await cts.CancelAsync();
        await next.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.NotEqual(title, titleWhileWaiting);
        Assert.Equal(title, _tab.Title.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Next_WaitingWhenTheExtractionIsStoppedNotFinished_GivesUpAndShowsTheTitleAgain(bool keyHeldDown)
    {
        // Looping, which goes round to the first page when the extraction counts as finished
        Settings.UIProperties.Looping = true;
        var c = File("c.jpg");
        _iterator.Initialize([File("a.jpg"), File("b.jpg"), c], 2);
        _tab.IsSelected = true;
        _tab.Display(new ImageModel { FileInfo = c, Image = new ProbeImage() });
        _tab.UpdateTabTitle();
        var title = _tab.Title!.Value;
        var extraction = _tab.ArchiveExtractionService!.ResetExtractionCts();
        _tab.IsArchiveExtracting.Value = true;

        var next = Next(keyHeldDown);
        // Another archive opens, which stops this one's extraction before its own page shows
        _tab.ArchiveExtractionService.StopExtraction();
        NavigationService.EndExtraction(_tab, extraction);
        _iterator.NotifyFileAdded();
        await next.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal("c.jpg", _tab.Model.FileInfo?.Name);
        Assert.Equal(title, _tab.Title.Value);
    }

    [Fact]
    public async Task Next_TwoPressesWaitingWhenTheNextFileComes_NeitherGoesRoundToTheFirst()
    {
        // With looping, a step that counts from where the other one went goes round to the first file. Both wake on the
        // same insert at once, so the rounds give them the chance to
        Settings.UIProperties.Looping = true;
        var shown = new ConcurrentQueue<string?>();
        using var subscription = _tab.FileInfo.Subscribe(file => shown.Enqueue(file?.Name));
        _tab.IsArchiveExtracting.Value = true;

        for (var round = 0; round < 100; round++)
        {
            var b = File("b.jpg");
            _iterator.Initialize([File("a.jpg"), b], 1);
            _tab.Display(new ImageModel { FileInfo = b, Image = new ProbeImage() });
            var first = Next(keyHeldDown: false);
            var second = Next(keyHeldDown: false);

            _iterator.Insert(File("c.jpg"), string.CompareOrdinal);
            await Task.WhenAll(first, second).WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        }

        Assert.DoesNotContain("a.jpg", shown);
    }

    // As the end of an archive's background extraction does
    private void EndExtraction()
    {
        _tab.IsArchiveExtracting.Value = false;
        _iterator.NotifyFileAdded();
    }

    private Task Next(bool keyHeldDown) => keyHeldDown
        ? _iterator.RepeatNavigateAsync(NavigateTo.Next, TimeSpan.Zero, new CancellationTokenSource()).AsTask()
        : _iterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, new CancellationTokenSource()).AsTask();

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
