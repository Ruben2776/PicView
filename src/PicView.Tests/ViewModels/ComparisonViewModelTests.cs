using PicView.Core.Config;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Preloading;
using PicView.Core.ViewModels;

namespace PicView.Tests.ViewModels;

/// <summary>
/// A tab's comparison: which two images show as A and B, the per-tab swap, pin, overrides and alignment, and the
/// comparison preferences, which write through to <see cref="AppSettings.Comparison"/>.
/// </summary>
[Collection("Sequential")]
public class ComparisonViewModelTests
{
    private int _saves;

    public ComparisonViewModelTests()
    {
        SetDefaults();
    }

    private sealed class DisposeProbe : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private static ImageModel Model(string name) => new()
    {
        FileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "PicViewComparisonViewModelTests", name)),
        Image = new DisposeProbe(),
        PixelWidth = 1,
        PixelHeight = 1
    };

    private ComparisonViewModel New(ImageModel primary, ImageModel? secondary) => new(primary, secondary, () => _saves++);

    private static bool IsDisposed(ImageModel model) => ((DisposeProbe)model.Image!).IsDisposed;

    [Fact]
    public void New_ShowsPrimaryAsAAndSecondaryAsB()
    {
        var a = Model("a.jpg");
        var b = Model("b.jpg");
        using var vm = New(a, b);

        Assert.Same(a.Image, vm.ImageA.Value);
        Assert.Same(b.FileInfo, vm.FileInfoB.Value);
    }

    [Fact]
    public void Swap_ExchangesAAndB()
    {
        var a = Model("a.jpg");
        var b = Model("b.jpg");
        using var vm = New(a, b);

        vm.Swap();

        Assert.Same(b.Image, vm.ImageA.Value);
        Assert.Same(a.Image, vm.ImageB.Value);
        Assert.True(vm.IsSwapped.Value);
    }

    [Fact]
    public void Swap_NothingShown_DoesNothing()
    {
        using var vm = New(new ImageModel(), null);

        vm.Swap();

        Assert.False(vm.IsSwapped.Value);
    }

    [Fact]
    public void SetOverride_B_ShowsItAsBWithoutTouchingThePair()
    {
        var a = Model("a.jpg");
        var b = Model("b.jpg");
        var c = Model("c.jpg");
        using var vm = New(a, b);

        vm.SetOverride(ComparisonSlot.B, c);

        Assert.Same(c.Image, vm.ImageB.Value);
        Assert.Same(a.Image, vm.ImageA.Value);
        vm.Swap();
        Assert.Same(c.Image, vm.ImageA.Value);
        Assert.False(IsDisposed(b));
    }

    [Fact]
    public void SetOverride_Replaced_DisposesThePreviousPick()
    {
        var c = Model("c.jpg");
        using var vm = New(Model("a.jpg"), Model("b.jpg"));

        vm.SetOverride(ComparisonSlot.B, c);
        vm.SetOverride(ComparisonSlot.B, Model("d.jpg"));

        Assert.True(IsDisposed(c));
    }

    [Fact]
    public void OnPairChanged_ResetsSwapAndAlignmentAndClearsOverrides()
    {
        var c = Model("c.jpg");
        var d = Model("d.jpg");
        using var vm = New(Model("a.jpg"), Model("b.jpg"));
        vm.SetOverride(ComparisonSlot.A, c);
        vm.Swap();
        vm.ToggleAligning();

        vm.OnPairChanged(d, Model("e.jpg"));

        Assert.True(IsDisposed(c));
        Assert.Same(d.Image, vm.ImageA.Value);
        Assert.False(vm.IsSwapped.Value);
        Assert.False(vm.IsAligning.Value);
        Assert.Equal(0, vm.AlignmentStep.Value);
    }

    [Fact]
    public void OnPairChanged_Pinned_KeepsOverrides()
    {
        var c = Model("c.jpg");
        using var vm = New(Model("a.jpg"), Model("b.jpg"));
        vm.SetOverride(ComparisonSlot.A, c);
        vm.TogglePin();

        vm.OnPairChanged(Model("d.jpg"), Model("e.jpg"));

        Assert.Same(c.Image, vm.ImageA.Value);
        Assert.False(IsDisposed(c));
    }

    [Fact]
    public void TogglePin_OnWithAEmpty_PinsThePrimaryAsA()
    {
        var a = Model("a.jpg");
        var e = Model("e.jpg");
        using var vm = New(a, Model("b.jpg"));

        vm.TogglePin();
        vm.OnPairChanged(Model("d.jpg"), e);

        Assert.True(vm.IsPinned.Value);
        Assert.Same(a.Image, vm.ImageA.Value);
        Assert.Same(e.Image, vm.ImageB.Value);
    }

    [Fact]
    public void TogglePin_OffAfterPinningTheModel_DoesNotDisposeIt()
    {
        var a = Model("a.jpg");
        var d = Model("d.jpg");
        using var vm = New(a, Model("b.jpg"));
        vm.TogglePin();
        vm.OnPairChanged(d, Model("e.jpg"));

        vm.TogglePin();

        Assert.False(IsDisposed(a));
        Assert.False(vm.IsPinned.Value);
        Assert.Same(d.Image, vm.ImageA.Value);
    }

    [Fact]
    public void Dispose_WithPinnedModel_DoesNotDisposeIt()
    {
        var a = Model("a.jpg");
        var b = Model("b.jpg");
        var vm = New(a, b);
        vm.TogglePin();

        vm.Dispose();

        Assert.False(IsDisposed(a));
        Assert.False(IsDisposed(b));
        Assert.True(vm.IsDisposed);
    }

    [Fact]
    public async Task BrowsingPastThePrimary_NotPinned_TheCacheDisposesIt()
    {
        // What the pin guards against: the cache disposes the primary once the tab has browsed past it
        var (cache, primary, files) = await CachedPrimary();
        using var vm = New(primary, null, cache);

        await BrowsePastThePreloadWindow(cache, files);

        Assert.True(IsDisposed(primary));
    }

    [Fact]
    public async Task TogglePin_OnACachedPrimary_KeepsItWhileTheTabBrowsesPastIt()
    {
        var (cache, primary, files) = await CachedPrimary();
        using var vm = New(primary, null, cache);
        vm.TogglePin();

        await BrowsePastThePreloadWindow(cache, files);

        Assert.False(IsDisposed(primary));
        Assert.Same(primary.Image, vm.ImageA.Value);
    }

    [Fact]
    public async Task TogglePin_Off_ReleasesTheCachedPrimary()
    {
        var (cache, primary, files) = await CachedPrimary();
        using var vm = New(primary, null, cache);
        vm.TogglePin();
        await BrowsePastThePreloadWindow(cache, files);

        vm.TogglePin();
        cache.ForceDisposalQueue();

        Assert.True(IsDisposed(primary));
    }

    [Fact]
    public async Task SetOverride_AOverAPinnedPrimary_ReleasesTheCachedPrimary()
    {
        var (cache, primary, files) = await CachedPrimary();
        using var vm = New(primary, null, cache);
        vm.TogglePin();
        await BrowsePastThePreloadWindow(cache, files);

        vm.SetOverride(ComparisonSlot.A, Model("c.jpg"));
        cache.ForceDisposalQueue();

        Assert.True(IsDisposed(primary));
    }

    [Fact]
    public async Task ClearOverrides_WithAPinnedPrimary_ReleasesTheCachedPrimary()
    {
        var (cache, primary, files) = await CachedPrimary();
        using var vm = New(primary, null, cache);
        vm.TogglePin();
        await BrowsePastThePreloadWindow(cache, files);

        vm.ClearOverrides();
        cache.ForceDisposalQueue();

        Assert.True(IsDisposed(primary));
    }

    [Fact]
    public async Task Dispose_WithAPinnedCachedPrimary_ReleasesIt()
    {
        var (cache, primary, files) = await CachedPrimary();
        var vm = New(primary, null, cache);
        vm.TogglePin();
        await BrowsePastThePreloadWindow(cache, files);

        vm.Dispose();
        cache.ForceDisposalQueue();

        Assert.True(IsDisposed(primary));
    }

    private const uint TabId = 1;

    private ComparisonViewModel New(ImageModel primary, ImageModel? secondary, SharedImageCache cache) =>
        new(primary, secondary, () => _saves++, cache.Hold);

    // A cache over a tab's files, with the first one loaded for the tab and shown as its primary
    private static async Task<(SharedImageCache Cache, ImageModel Primary, FileInfo[] Files)> CachedPrimary()
    {
        var files = Enumerable.Range(0, PreLoaderConfig.MaxCount * 3).Select(static i => Model($"{i}.jpg").FileInfo!)
            .ToArray();
        var cache = new SharedImageCache(static f => ValueTask.FromResult(Model(f.Name)));
        cache.SetFiles(TabId, files);
        var primary = await cache.GetAsync(TabId, files[0], TestContext.Current.CancellationToken);
        return (cache, Assert.IsType<ImageModel>(primary), files);
    }

    // Shows each file after the first in turn, as browsing does, so the first leaves the tab's preload window
    private static async Task BrowsePastThePreloadWindow(SharedImageCache cache, FileInfo[] files)
    {
        for (var i = 1; i < files.Length; i++)
        {
            await cache.GetAsync(TabId, files[i], TestContext.Current.CancellationToken);
        }

        cache.ForceDisposalQueue();
    }

    [Fact]
    public void Dispose_DisposesPickedOverrides()
    {
        var c = Model("c.jpg");
        var d = Model("d.jpg");
        var vm = New(Model("a.jpg"), Model("b.jpg"));
        vm.SetOverride(ComparisonSlot.A, c);
        vm.SetOverride(ComparisonSlot.B, d);

        vm.Dispose();

        Assert.True(IsDisposed(c));
        Assert.True(IsDisposed(d));
    }

    [Fact]
    public void AddAlignmentPoint_FourPoints_SetsAlignmentAndStops()
    {
        using var vm = New(Model("a.jpg"), Model("b.jpg"));
        vm.ToggleAligning();

        vm.AddAlignmentPoint(0, 0);
        vm.AddAlignmentPoint(1, 1);
        vm.AddAlignmentPoint(10, 0);
        vm.AddAlignmentPoint(11, 1);

        Assert.True(vm.HasAlignment.Value);
        Assert.NotNull(vm.Alignment.Value);
        Assert.Equal(0, vm.AlignmentStep.Value);
        Assert.False(vm.IsAligning.Value);
    }

    [Fact]
    public void AddAlignmentPoint_DegeneratePoints_StopsWithoutAlignment()
    {
        using var vm = New(Model("a.jpg"), Model("b.jpg"));
        vm.ToggleAligning();

        for (var i = 0; i < 4; i++)
        {
            vm.AddAlignmentPoint(5, 5);
        }

        Assert.False(vm.HasAlignment.Value);
        Assert.Null(vm.Alignment.Value);
        Assert.False(vm.IsAligning.Value);
    }

    [Fact]
    public void AddAlignmentPoint_NotAligning_DoesNothing()
    {
        using var vm = New(Model("a.jpg"), Model("b.jpg"));

        vm.AddAlignmentPoint(1, 1);

        Assert.Equal(0, vm.AlignmentStep.Value);
    }

    [Theory]
    [InlineData(ComparisonMode.Overlay, ComparisonMode.Split)]
    [InlineData(ComparisonMode.Difference, ComparisonMode.Synced)]
    [InlineData(ComparisonMode.Synced, ComparisonMode.Overlay)]
    public void Next_CyclesAllFive(ComparisonMode from, ComparisonMode to)
        => Assert.Equal(to, ComparisonViewModel.Next(from));

    [Fact]
    public void SetMode_SyncedWhileAligning_StopsAligning()
    {
        // The synced layout hides the stacked pair the points are picked on, and the button that stops picking them
        using var vm = New(Model("a.jpg"), Model("b.jpg"));
        vm.ToggleAligning();
        vm.AddAlignmentPoint(1, 1);

        vm.SetMode(ComparisonMode.Synced);

        Assert.Equal(0, vm.AlignmentStep.Value);
        Assert.False(vm.IsAligning.Value);
    }

    [Fact]
    public void NextMode_IntoSyncedWhileAligning_StopsAligning()
    {
        using var vm = New(Model("a.jpg"), Model("b.jpg"));
        vm.SetMode(ComparisonMode.Difference);
        vm.ToggleAligning();

        vm.NextMode();

        Assert.True(vm.IsSynced.Value);
        Assert.False(vm.IsAligning.Value);
    }

    [Fact]
    public void SetMode_SyncedAfterAligning_KeepsTheAlignment()
    {
        using var vm = New(Model("a.jpg"), Model("b.jpg"));
        vm.ToggleAligning();
        vm.AddAlignmentPoint(0, 0);
        vm.AddAlignmentPoint(1, 1);
        vm.AddAlignmentPoint(10, 0);
        vm.AddAlignmentPoint(11, 1);
        var alignment = vm.Alignment.Value;

        vm.SetMode(ComparisonMode.Synced);

        Assert.True(vm.HasAlignment.Value);
        Assert.Equal(alignment, vm.Alignment.Value);
    }

    [Fact]
    public void SetMode_Synced_OnlyIsSyncedIsTrue()
    {
        using var vm = New(Model("a.jpg"), Model("b.jpg"));

        vm.SetMode(ComparisonMode.Synced);

        Assert.True(vm.IsSynced.Value);
        Assert.False(vm.IsOverlay.Value);
        Assert.False(vm.IsSplit.Value);
        Assert.False(vm.IsBlink.Value);
        Assert.False(vm.IsDifference.Value);
    }

    [Fact]
    public void Mode_Changed_WritesTheSettingAndSaves()
    {
        using var vm = New(Model("a.jpg"), Model("b.jpg"));

        vm.SetMode(ComparisonMode.Split);

        Assert.Equal(ComparisonMode.Split, Settings.Comparison!.Mode);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public void Preferences_Changed_WriteTheirSettings()
    {
        using var vm = New(Model("a.jpg"), Model("b.jpg"));

        vm.OverlayOpacity.Value = 0.3;
        vm.SplitRatio.Value = 0.7;
        vm.IsSplitHorizontal.Value = true;
        vm.IsBlinkFast.Value = false;
        vm.BlinkInterval.Value = 2;
        vm.IsStretchFill.Value = false;
        vm.BarPosition.Value = ComparisonBarPosition.Right;

        var saved = Settings.Comparison!;
        Assert.Equal(0.3, saved.OverlayOpacity);
        Assert.Equal(0.7, saved.SplitRatio);
        Assert.True(saved.IsSplitHorizontal);
        Assert.False(saved.IsBlinkFast);
        Assert.Equal(2, saved.BlinkInterval);
        Assert.False(saved.IsStretchFill);
        Assert.Equal(ComparisonBarPosition.Right, saved.BarPosition);
        Assert.Equal(7, _saves);
    }

    [Fact]
    public void New_StartsFromTheSavedBarPosition()
    {
        Settings.Comparison!.BarPosition = ComparisonBarPosition.Top;
        using var vm = New(Model("a.jpg"), Model("b.jpg"));
        Assert.Equal(ComparisonBarPosition.Top, vm.BarPosition.Value);
        Assert.Equal(0, _saves);
    }

    [Fact]
    public void New_StartsFromTheSavedPreferences()
    {
        Settings.Comparison!.Mode = ComparisonMode.Blink;

        using var vm = New(Model("a.jpg"), Model("b.jpg"));

        Assert.True(vm.IsBlink.Value);
        Assert.Equal(0, _saves);
    }
}
