using PicView.Core.Config;
using PicView.Core.Models;
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

        var saved = Settings.Comparison!;
        Assert.Equal(0.3, saved.OverlayOpacity);
        Assert.Equal(0.7, saved.SplitRatio);
        Assert.True(saved.IsSplitHorizontal);
        Assert.False(saved.IsBlinkFast);
        Assert.Equal(2, saved.BlinkInterval);
        Assert.False(saved.IsStretchFill);
        Assert.Equal(6, _saves);
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
