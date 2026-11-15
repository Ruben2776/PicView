using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.CustomControls.Zoom;
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
        vm.ScrollViewerWidth.Value = 800;
        vm.ScrollViewerHeight.Value = 600;
        vm.ImageWidth.Value = 400;
        vm.ImageHeight.Value = 300;
        vm.GlobalSettings.ComparisonMode.Value = ComparisonMode.Split;
        vm.GlobalSettings.ComparisonSplitRatio.Value = 0.4;
        vm.GlobalSettings.IsComparing.Value = true;

        var hostWindow = new Window { Width = 800, Height = 600, Content = ViewOf(tab) };
        hostWindow.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            hostWindow.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var compare = CompareImageOf(tab);
            var host = CompareHostOf(tab);
            Assert.True(host.Bounds.Width > 0 && host.Bounds.Height > 0);
            var clip = Assert.IsType<RectangleGeometry>(compare.Clip);
            Assert.Equal(host.Bounds.Width * 0.4, clip.Rect.Width, 1);
            Assert.Equal(host.Bounds.Height, clip.Rect.Height, 1);

            vm.GlobalSettings.IsComparisonSplitHorizontal.Value = true;
            Dispatcher.UIThread.RunJobs();
            hostWindow.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            clip = Assert.IsType<RectangleGeometry>(compare.Clip);
            Assert.Equal(host.Bounds.Width, clip.Rect.Width, 1);
            Assert.Equal(host.Bounds.Height * 0.4, clip.Rect.Height, 1);
        }
        finally
        {
            hostWindow.Content = null;
            hostWindow.Close();
        }
    });

    [AvaloniaFact]
    public void SplitMode_ZoomedIn_DraggingHandleChangesRatioWithoutPanning() => WithWindow((window, vm, tab) =>
    {
        tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
        vm.ScrollViewerWidth.Value = 800;
        vm.ScrollViewerHeight.Value = 600;
        vm.ImageWidth.Value = 400;
        vm.ImageHeight.Value = 300;
        vm.GlobalSettings.ComparisonMode.Value = ComparisonMode.Split;
        vm.GlobalSettings.ComparisonSplitRatio.Value = 0.5;
        vm.GlobalSettings.IsComparing.Value = true;

        var hostWindow = new Window { Width = 800, Height = 600, Content = ViewOf(tab) };
        hostWindow.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            hostWindow.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var viewer = Assert.IsType<ImageViewer>(ViewOf(tab));
            var zoom = Assert.IsType<ZoomPanControl>(viewer.FindControl<ZoomPanControl>("ZoomPanControl"));
            var handle = Assert.IsType<Border>(viewer.FindControl<Border>("SplitHandle"));

            zoom.SetScaleImmediate(3.0);
            Dispatcher.UIThread.RunJobs();
            hostWindow.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var initialTx = zoom.TranslateX;
            var startPt = handle.TranslatePoint(new Point(handle.Bounds.Width * 0.5, handle.Bounds.Height * 0.5), hostWindow);
            Assert.NotNull(startPt);

            var endPt = new Point(startPt.Value.X + 40, startPt.Value.Y);
            hostWindow.MouseDown(startPt.Value, MouseButton.Left);
            hostWindow.MouseMove(endPt);
            hostWindow.MouseUp(endPt, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            Assert.True(vm.GlobalSettings.ComparisonSplitRatio.Value > 0.5);
            Assert.Equal(initialTx, zoom.TranslateX, 3);
        }
        finally
        {
            hostWindow.Content = null;
            hostWindow.Close();
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

    [AvaloniaFact]
    public void SyncedSideBySide_DifferentSizedImages_SyncsNormalizedCenter() => WithWindow((window, vm, tab) =>
    {
        var modelA = new PicView.Core.Models.ImageModel
        {
            FileInfo = new FileInfo("a.jpg"),
            Image = new WriteableBitmap(new PixelSize(400, 200), new Vector(96, 96)),
            PixelWidth = 400,
            PixelHeight = 200,
        };
        var modelB = new PicView.Core.Models.ImageModel
        {
            FileInfo = new FileInfo("b.jpg"),
            Image = new WriteableBitmap(new PixelSize(200, 400), new Vector(96, 96)),
            PixelWidth = 200,
            PixelHeight = 400,
        };

        tab.Display(modelA, modelB);
        vm.GlobalSettings.IsSideBySide.Value = true;
        vm.GlobalSettings.IsSideBySideSynced.Value = true;

        var hostWindow = new Window { Width = 800, Height = 600, Content = ViewOf(tab) };
        hostWindow.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            hostWindow.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var viewer = Assert.IsType<ImageViewer>(ViewOf(tab));
            var leftZoom = Assert.IsType<ZoomPanControl>(viewer.FindControl<ZoomPanControl>("SyncedLeftZoom"));
            var rightZoom = Assert.IsType<ZoomPanControl>(viewer.FindControl<ZoomPanControl>("SyncedRightZoom"));

            // Pan leftZoom to a non-clamped point inside image A
            leftZoom.ShowChildAt(3.0, new Point(300, 250));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(3.0, rightZoom.Scale, 3);
            Assert.NotEqual(leftZoom.TranslateX, rightZoom.TranslateX);
        }
        finally
        {
            hostWindow.Content = null;
            hostWindow.Close();
        }
    });

    [AvaloniaFact]
    public void SwapAndPick_DoNotMutateTabPrimaryFileInfo() => WithWindow((window, vm, tab) =>
    {
        var modelA = BitmapModel("a.jpg");
        var modelB = BitmapModel("b.jpg");
        var modelC = BitmapModel("c.jpg");
        tab.Display(modelA, modelB);
        vm.GlobalSettings.IsComparing.Value = true;
        Settle(window);

        tab.SwapComparisonPair();
        Settle(window);
        Assert.Equal("a.jpg", tab.FileInfo.Value?.Name);
        Assert.Same(modelB.Image, tab.DisplayPrimaryImage.Value);
        Assert.Same(modelA.Image, tab.DisplaySecondaryImage.Value);

        tab.SetComparisonPrimary(modelC);
        Settle(window);
        Assert.Equal("a.jpg", tab.FileInfo.Value?.Name);
        Assert.Same(modelB.Image, tab.DisplayPrimaryImage.Value);
        Assert.Same(modelC.Image, tab.DisplaySecondaryImage.Value);

        tab.SwapComparisonPair();
        Settle(window);
        Assert.Same(modelC.Image, tab.DisplayPrimaryImage.Value);
        Assert.Same(modelB.Image, tab.DisplaySecondaryImage.Value);
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
