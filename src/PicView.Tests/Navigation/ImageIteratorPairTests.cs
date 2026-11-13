using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// What a tab shows when it goes to an index with Side-by-Side on: the pair that holds the index (ADR-0024).
/// </summary>
[Collection("Sequential")]
public class ImageIteratorPairTests
{
    private static readonly string TestDirectory = Path.Combine(Path.GetTempPath(), "PicViewIteratorPairTests");

    private readonly TabViewModel _tab;
    private readonly ImageIterator _iterator;

    public ImageIteratorPairTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();

        var cache = new SharedImageCache(f => ValueTask.FromResult(new ImageModel { FileInfo = f }));
        _tab = new TabViewModel(null!, TestTabServices.For(cache, null));
        _tab.Initialize();
        _iterator = (ImageIterator)_tab.ImageIterator!;
        // Published, as the settings window sets it
        _tab.Services!.GlobalSettings.IsSideBySide.Value = true;
    }

    [Fact]
    public async Task IterateToIndex_LastImage_ShowsThePairBeforeIt()
    {
        _iterator.Initialize(Files("a.jpg", "b.jpg", "c.jpg"), 0);

        await _iterator.IterateToIndexAsync(2, new CancellationTokenSource());

        Assert.Equal(("b.jpg", "c.jpg"), Shown());
    }

    [Fact]
    public async Task IterateToIndex_LastImageLooping_PairsWithTheFirst()
    {
        _tab.Services!.GlobalSettings.IsLooping.Value = true;
        _iterator.Initialize(Files("a.jpg", "b.jpg", "c.jpg"), 0);

        await _iterator.IterateToIndexAsync(2, new CancellationTokenSource());

        Assert.Equal(("c.jpg", "a.jpg"), Shown());
    }

    [Fact]
    public async Task SkipToIndex_OneImage_ShowsItAlone()
    {
        _iterator.Initialize(Files("a.jpg"), 0);

        await _iterator.SkipToIndexAsync(0, new CancellationTokenSource());

        Assert.Equal(("a.jpg", null), Shown());
    }

    [Fact]
    public async Task TurningSideBySideOn_AtTheLastImage_ShowsThePairBeforeIt()
    {
        var settings = _tab.Services!.GlobalSettings;
        settings.IsSideBySide.Value = false;
        _iterator.Initialize(Files("a.jpg", "b.jpg", "c.jpg"), 0);
        await _iterator.IterateToIndexAsync(2, new CancellationTokenSource());
        Assert.Equal(("c.jpg", null), Shown());

        // The tab reloads what it shows once the setting publishes, which may wait for the cache
        settings.IsSideBySide.Value = true;
        await WaitUntil(() => Shown() == ("b.jpg", "c.jpg"));

        Assert.Equal(("b.jpg", "c.jpg"), Shown());
        Assert.Equal(1, _iterator.CurrentIndex);
    }

    [Fact]
    public async Task Reload_OneImage_ShowsItAlone()
    {
        _iterator.Initialize(Files("a.jpg"), 0);

        await _iterator.ReloadAsync();

        Assert.Equal(("a.jpg", null), Shown());
    }

    [Fact]
    public async Task DeletingTheShownFile_LeavesOneImage_ShowsItAlone()
    {
        var files = Files("a.jpg", "b.jpg");
        _iterator.Initialize(files, 0);
        await _iterator.IterateToIndexAsync(0, new CancellationTokenSource());
        Assert.Equal(("a.jpg", "b.jpg"), Shown());

        await DeleteAsync(files[0]);

        Assert.Equal(("b.jpg", null), Shown());
    }

    [Fact]
    public async Task DeletingThePartner_ShowsTheNextPartner()
    {
        var files = Files("a.jpg", "b.jpg", "c.jpg");
        _iterator.Initialize(files, 0);
        await _iterator.IterateToIndexAsync(0, new CancellationTokenSource());

        await DeleteAsync(files[1]);

        Assert.Equal(("a.jpg", "c.jpg"), Shown());
    }

    [Fact]
    public async Task IterateToIndex_LastImage_PublishesBothIndicesOfThePair()
    {
        _iterator.Initialize(Files("a.jpg", "b.jpg", "c.jpg"), 0);

        await _iterator.IterateToIndexAsync(2, new CancellationTokenSource());

        Assert.Equal((1, 2), (_tab.NavigationIndex.Value, _tab.PartnerIndex.Value));
    }

    [Fact]
    public async Task IterateToIndex_SideBySideOff_PublishesNoPartner()
    {
        _iterator.Initialize(Files("a.jpg", "b.jpg", "c.jpg"), 0);
        await _iterator.IterateToIndexAsync(0, new CancellationTokenSource());
        Assert.Equal(1, _tab.PartnerIndex.Value);

        _tab.Services!.GlobalSettings.IsSideBySide.Value = false;
        await _iterator.IterateToIndexAsync(2, new CancellationTokenSource());

        Assert.Equal((2, -1), (_tab.NavigationIndex.Value, _tab.PartnerIndex.Value));
    }

    private ValueTask DeleteAsync(FileInfo file) =>
        _tab.OnFolderChangedAsync(new FolderChange(FolderChangeKind.Deleted, file, null,
            new FileDetailsSource(file, waitUntilReady: false), string.CompareOrdinal), TestContext.Current.CancellationToken);

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }

    private (string? Primary, string? Partner) Shown() =>
        (_tab.Model.FileInfo?.Name, _tab.SecondaryModel?.FileInfo?.Name);

    private static List<FileInfo> Files(params string[] names) =>
        names.Select(name => new FileInfo(Path.Combine(TestDirectory, name))).ToList();

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
