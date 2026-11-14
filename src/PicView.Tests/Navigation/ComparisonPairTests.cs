using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// Comparison mode shows a pair the same way side-by-side does: the image at the index and the next one (issue #367).
/// </summary>
[Collection("Sequential")]
public class ComparisonPairTests
{
    private static readonly string TestDirectory = Path.Combine(Path.GetTempPath(), "PicViewComparisonPairTests");

    private readonly TabViewModel _tab;
    private readonly ImageIterator _iterator;

    public ComparisonPairTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();

        var cache = new SharedImageCache(f => ValueTask.FromResult(new ImageModel { FileInfo = f }));
        _tab = new TabViewModel(null!, TestTabServices.For(cache, null));
        _tab.Initialize();
        _iterator = (ImageIterator)_tab.ImageIterator!;
        _tab.Services!.GlobalSettings.IsComparing.Value = true;
    }

    [Fact]
    public async Task IterateToIndex_Comparing_ShowsCurrentAndNext()
    {
        _iterator.Initialize(Files("a.jpg", "b.jpg", "c.jpg"), 0);

        await _iterator.IterateToIndexAsync(0, new CancellationTokenSource());

        Assert.Equal(("a.jpg", "b.jpg"), Shown());
    }

    [Fact]
    public async Task IterateToIndex_ComparingLastImage_ShowsThePairBeforeIt()
    {
        _iterator.Initialize(Files("a.jpg", "b.jpg", "c.jpg"), 0);

        await _iterator.IterateToIndexAsync(2, new CancellationTokenSource());

        Assert.Equal(("b.jpg", "c.jpg"), Shown());
    }

    [Fact]
    public async Task Comparing_OneImage_ShowsItAlone()
    {
        _iterator.Initialize(Files("a.jpg"), 0);

        await _iterator.SkipToIndexAsync(0, new CancellationTokenSource());

        Assert.Equal(("a.jpg", null), Shown());
    }

    [Fact]
    public async Task TurningComparisonOn_ShowsThePair()
    {
        var settings = _tab.Services!.GlobalSettings;
        settings.IsComparing.Value = false;
        _iterator.Initialize(Files("a.jpg", "b.jpg", "c.jpg"), 0);
        await _iterator.IterateToIndexAsync(0, new CancellationTokenSource());
        Assert.Equal(("a.jpg", null), Shown());

        // The tab reloads what it shows once the setting publishes, which may wait for the cache
        settings.IsComparing.Value = true;
        await WaitUntil(() => Shown() == ("a.jpg", "b.jpg"));

        Assert.Equal(("a.jpg", "b.jpg"), Shown());
    }

    [Fact]
    public async Task SwitchingSideBySideToComparison_KeepsThePair()
    {
        var settings = _tab.Services!.GlobalSettings;
        settings.IsSideBySide.Value = true;
        _iterator.Initialize(Files("a.jpg", "b.jpg", "c.jpg"), 0);
        await _iterator.IterateToIndexAsync(0, new CancellationTokenSource());

        settings.IsComparing.Value = true;

        Assert.Equal(("a.jpg", "b.jpg"), Shown());
        Assert.Equal(0, _iterator.CurrentIndex);
    }

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
