using PicView.Core.Config;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// Comparing is per tab: the tab owns its comparison while it compares and shows something, and shows it as the
/// Comparison Tab View.
/// </summary>
[Collection("Sequential")]
public class TabComparisonTests
{
    private readonly TabViewModel _tab;

    public TabComparisonTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        _tab = NewTab(TestTabServices.For(Cache()));
    }

    private static SharedImageCache Cache() => new(f => ValueTask.FromResult(Model(f.Name))!);

    private static TabViewModel NewTab(TabServices services) => new(new MainWindowViewModel(null!, null!, null!, null!), services);

    private static ImageModel Model(string name) => new()
    {
        FileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "PicViewTabComparisonTests", name)),
        Image = new object(),
        PixelWidth = 1,
        PixelHeight = 1
    };

    [Fact]
    public void SetComparing_OnWhileShowingAnImage_MakesAComparisonAndKindComparison()
    {
        _tab.Display(Model("a.jpg"), Model("b.jpg"));

        _tab.SetComparing(true);

        Assert.NotNull(_tab.Comparison);
        Assert.True(_tab.IsComparing.Value);
        Assert.Equal(TabViewKind.Comparison, _tab.ViewKind.Value);
    }

    [Fact]
    public void SetComparing_OnWhileShowingNothing_MakesNone()
    {
        _tab.SetComparing(true);

        Assert.Null(_tab.Comparison);
        Assert.Equal(TabViewKind.StartUpMenu, _tab.ViewKind.Value);
    }

    [Fact]
    public void Display_WhileComparingOnAFreshTab_MakesAComparison()
    {
        _tab.SetComparing(true);

        _tab.Display(Model("a.jpg"), Model("b.jpg"));

        Assert.NotNull(_tab.Comparison);
        Assert.Equal(TabViewKind.Comparison, _tab.ViewKind.Value);
    }

    [Fact]
    public void SetComparing_Off_DisposesTheComparisonAndKindImageViewer()
    {
        _tab.Display(Model("a.jpg"), Model("b.jpg"));
        _tab.SetComparing(true);
        var old = _tab.Comparison!;

        _tab.SetComparing(false);

        Assert.True(old.IsDisposed);
        Assert.Null(_tab.Comparison);
        Assert.Equal(TabViewKind.ImageViewer, _tab.ViewKind.Value);
    }

    [Fact]
    public void Dispose_WhileComparing_DisposesTheComparison()
    {
        _tab.Display(Model("a.jpg"), Model("b.jpg"));
        _tab.SetComparing(true);
        var comparison = _tab.Comparison!;

        _tab.Dispose();

        Assert.True(comparison.IsDisposed);
    }

    [Fact]
    public void Crop_WhileComparing_KindIsCrop()
    {
        _tab.Display(Model("a.jpg"), Model("b.jpg"));
        _tab.SetComparing(true);

        _tab.Crop = new CropViewModel(_tab.Zoom, 2, 2);

        Assert.Equal(TabViewKind.Crop, _tab.ViewKind.Value);
    }

    [Fact]
    public void Display_WhileComparing_PassesTheNewPair()
    {
        _tab.Display(Model("a.jpg"), Model("b.jpg"));
        _tab.SetComparing(true);
        var comparison = _tab.Comparison!;
        var c = Model("c.jpg");

        _tab.Display(c, Model("d.jpg"));

        Assert.Same(comparison, _tab.Comparison);
        Assert.Same(c.FileInfo, comparison.FileInfoA.Value);
    }

    [Fact]
    public async Task TogglePin_OnACachedPrimary_TheCacheKeepsItAfterTheTabReleasesIt()
    {
        var cache = Cache();
        var tab = NewTab(TestTabServices.For(cache));
        var file = Model("a.jpg").FileInfo!;
        cache.SetFiles(tab.Id, [file]);
        var a = Assert.IsType<ImageModel>(await cache.GetAsync(tab.Id, file, TestContext.Current.CancellationToken));
        tab.Display(a, Model("b.jpg"));
        tab.SetComparing(true);

        tab.Comparison!.TogglePin();
        // As browsing past it does
        cache.ReleaseImages(tab.Id);
        cache.ForceDisposalQueue();

        // A disposed image leaves the cache
        Assert.True(cache.TryGetReady(file, out var ready));
        Assert.Same(a, ready);
    }

    [Fact]
    public async Task CloseImageAsync_WhileComparing_ShowsTheMenuAndKeepsComparingOn()
    {
        _tab.Display(Model("a.jpg"), Model("b.jpg"));
        _tab.SetComparing(true);

        await _tab.CloseImageAsync();

        Assert.Equal(TabViewKind.StartUpMenu, _tab.ViewKind.Value);
        Assert.Null(_tab.Comparison);
        Assert.True(_tab.IsComparing.Value);
    }

    [Fact]
    public async Task Display_AfterClosingWhileComparing_MakesANewComparison()
    {
        _tab.Display(Model("a.jpg"), Model("b.jpg"));
        _tab.SetComparing(true);
        var old = _tab.Comparison!;
        await _tab.CloseImageAsync();

        _tab.Display(Model("c.jpg"), Model("d.jpg"));

        Assert.NotNull(_tab.Comparison);
        Assert.NotSame(old, _tab.Comparison);
        Assert.Equal(TabViewKind.Comparison, _tab.ViewKind.Value);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void IsShowingPair_SideBySideOrComparing_IsEither(bool sideBySide, bool comparing, bool expected)
    {
        _tab.CurrentWindowSettings.CurrentValue!.IsSideBySide.Value = sideBySide;

        _tab.SetComparing(comparing);

        Assert.Equal(expected, _tab.IsShowingPair.CurrentValue);
        Assert.Equal(expected, _tab.PairSettings().ShowsPair);
    }

    [Fact]
    public void IsShowingPair_SideBySideOnThenComparingOn_NeverEmitsFalse()
    {
        var emitted = new List<bool>();
        using var _ = _tab.IsShowingPair.Skip(1).Subscribe(emitted.Add);

        _tab.CurrentWindowSettings.CurrentValue!.IsSideBySide.Value = true;
        _tab.SetComparing(true);

        Assert.DoesNotContain(false, emitted);
        Assert.True(_tab.IsShowingPair.CurrentValue);
    }

    [Fact]
    public void SideBySide_TurnedOnWhileComparing_KeepsComparing()
    {
        _tab.Display(Model("a.jpg"), Model("b.jpg"));
        _tab.SetComparing(true);

        _tab.CurrentWindowSettings.CurrentValue!.IsSideBySide.Value = true;

        Assert.True(_tab.IsComparing.Value);
        Assert.NotNull(_tab.Comparison);
    }

    [Fact]
    public void SetComparing_InOneTab_LeavesAnotherTabsPairAndKindAlone()
    {
        var services = TestTabServices.For(Cache());
        var a = NewTab(services);
        var b = NewTab(services);
        a.Display(Model("a.jpg"), Model("b.jpg"));
        b.Display(Model("c.jpg"));

        a.SetComparing(true);

        Assert.False(b.IsShowingPair.CurrentValue);
        Assert.Equal(TabViewKind.ImageViewer, b.ViewKind.Value);
        Assert.Null(b.Comparison);
    }

    [Fact]
    public async Task Mode_ChangedThroughATabsComparison_SavesThroughTheGlobalSettings()
    {
        var saves = 0;
        var settings = new GlobalSettingsViewModel(() =>
        {
            saves++;
            return ValueTask.FromResult(true);
        });
        var tab = NewTab(TestTabServices.For(Cache(), globalSettings: settings));
        tab.Display(Model("a.jpg"), Model("b.jpg"));
        tab.SetComparing(true);
        var mode = ComparisonViewModel.Next(tab.Comparison!.Mode.Value);

        tab.Comparison.SetMode(mode);
        await settings.Saving;

        Assert.Equal(1, saves);
        Assert.Equal(mode, Settings.Comparison!.Mode);
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
