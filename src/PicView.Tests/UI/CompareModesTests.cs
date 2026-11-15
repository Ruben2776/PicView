using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;
using static PicView.Tests.UI.TabViewsHarness;

namespace PicView.Tests.UI;

[Collection("Sequential")]
public class CompareModesTests
{
    public CompareModesTests()
    {
        SetDefaults();
    }

    [AvaloniaFact]
    public void CompareBar_ShowingPair_IsVisibleAndHiddenOtherwise() => WithWindow((window, vm, tab) =>
    {
        tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
        Settle(window);

        var bar = CompareBarOf(tab);
        Assert.False(bar.IsVisible);

        vm.GlobalSettings.IsComparing.Value = true;
        Settle(window);
        Assert.True(bar.IsVisible);

        vm.GlobalSettings.IsSideBySide.Value = true;
        Settle(window);
        Assert.True(bar.IsVisible);

        vm.GlobalSettings.IsSideBySide.Value = false;
        Settle(window);
        Assert.False(bar.IsVisible);
    });

    [AvaloniaFact]
    public void CompareImage_OverlayMode_AppliesNormalBlendAndOpacity() => WithWindow((window, vm, tab) =>
    {
        tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
        vm.GlobalSettings.ComparisonMode.Value = ComparisonMode.Overlay;
        vm.GlobalSettings.ComparisonOverlayOpacity.Value = 0.35;
        vm.GlobalSettings.IsComparing.Value = true;
        Settle(window);

        var compare = CompareImageOf(tab);
        Assert.Equal(BitmapBlendingMode.Unspecified, compare.BlendMode);
        Assert.Equal(0.35, compare.Opacity, 3);
        Assert.Null(compare.Clip);
    });

    [AvaloniaFact]
    public void CompareImage_SplitMode_AppliesClipForVerticalAndHorizontal() => WithWindow((window, vm, tab) =>
    {
        tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
        vm.GlobalSettings.ComparisonMode.Value = ComparisonMode.Split;
        vm.GlobalSettings.ComparisonSplitRatio.Value = 0.4;
        vm.GlobalSettings.IsComparing.Value = true;
        Settle(window);

        var compare = CompareImageOf(tab);
        var host = CompareHostOf(tab);
        if (host.Bounds.Width > 0 && host.Bounds.Height > 0)
        {
            var clip = Assert.IsType<RectangleGeometry>(compare.Clip);
            Assert.Equal(host.Bounds.Width * 0.4, clip.Rect.Width, 1);
            Assert.Equal(host.Bounds.Height, clip.Rect.Height, 1);

            vm.GlobalSettings.IsComparisonSplitHorizontal.Value = true;
            Settle(window);

            clip = Assert.IsType<RectangleGeometry>(compare.Clip);
            Assert.Equal(host.Bounds.Width, clip.Rect.Width, 1);
            Assert.Equal(host.Bounds.Height * 0.4, clip.Rect.Height, 1);
        }
    });

    [AvaloniaFact]
    public void SyncedSideBySide_ZoomIn_SyncsLeftAndRightScale() => WithWindow((window, vm, tab) =>
    {
        tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
        vm.GlobalSettings.IsSideBySide.Value = true;
        vm.GlobalSettings.IsSideBySideSynced.Value = true;
        Settle(window);

        var viewer = Assert.IsType<ImageViewer>(ViewOf(tab));
        var leftZoom = Assert.IsType<PicView.Avalonia.CustomControls.Zoom.ZoomPanControl>(
            viewer.FindControl<PicView.Avalonia.CustomControls.Zoom.ZoomPanControl>("SyncedLeftZoom"));
        var rightZoom = Assert.IsType<PicView.Avalonia.CustomControls.Zoom.ZoomPanControl>(
            viewer.FindControl<PicView.Avalonia.CustomControls.Zoom.ZoomPanControl>("SyncedRightZoom"));

        viewer.ZoomIn();
        Settle(window);

        Assert.True(leftZoom.Scale > 1.0, $"Expected leftZoom.Scale > 1.0, got {leftZoom.Scale}");
        Assert.Equal(leftZoom.Scale, rightZoom.Scale, 3);
    });

    private static PicBox CompareImageOf(PicView.Core.ViewModels.TabViewModel tab)
    {
        var viewer = Assert.IsType<ImageViewer>(ViewOf(tab));
        return Assert.IsType<PicBox>(viewer.FindControl<PicBox>("CompareImage"));
    }

    private static Grid CompareHostOf(PicView.Core.ViewModels.TabViewModel tab)
    {
        var viewer = Assert.IsType<ImageViewer>(ViewOf(tab));
        return Assert.IsType<Grid>(viewer.FindControl<Grid>("CompareHostGrid"));
    }

    private static CompareBar CompareBarOf(PicView.Core.ViewModels.TabViewModel tab)
    {
        var viewer = Assert.IsType<ImageViewer>(ViewOf(tab));
        return Assert.IsType<CompareBar>(viewer.FindControl<CompareBar>("CompareBar"));
    }
}
