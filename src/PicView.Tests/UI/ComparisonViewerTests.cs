using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.CustomControls.Zoom;
using PicView.Avalonia.Input;
using PicView.Avalonia.Views.Menu;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;
using PicView.Core.ImageDecoding;
using PicView.Core.Keybindings;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using PicView.Tests.Input;
using PicView.Tests.WindowBehavior;
using static PicView.Tests.UI.TabViewsHarness;
using MouseButton = Avalonia.Input.MouseButton;

namespace PicView.Tests.UI;

/// <summary>
/// The comparison viewer shows a tab's comparison on its own, stacked A under B or side by side and synced, with the
/// mode's visuals, the alignment and the zoom written to the tab.
/// </summary>
[Collection("Sequential")]
public class ComparisonViewerTests
{
    public ComparisonViewerTests()
    {
        SetDefaults();
    }

    [AvaloniaFact]
    public void DifferenceMode_ShowsBWithDifferenceBlendAndFill() => WithWindow((window, _, tab) =>
    {
        var viewer = Host(window, tab);

        var b = Part<PicBox>(viewer, "ImageB");
        Assert.True(b.IsVisible);
        Assert.Equal(BitmapBlendingMode.Difference, b.BlendMode);
        Assert.Equal(Stretch.Fill, b.Stretch);
    });

    [AvaloniaFact]
    public void AnimatedB_IsNotAnimated() => WithWindow((window, _, tab) =>
    {
        // An animation draws its frames without the blend, so it would cover A instead of diffing it
        var partner = BitmapModel("b.gif");
        partner.ImageType = ImageType.AnimatedGif;

        var viewer = Host(window, tab, b: partner);

        Assert.NotEqual(ImageType.AnimatedGif, Part<PicBox>(viewer, "ImageB").ImageType);
    });

    [AvaloniaFact]
    public void OverlayMode_AppliesNormalBlendAndOpacity() => WithWindow((window, _, tab) =>
    {
        var viewer = Host(window, tab, configure: comparison =>
        {
            comparison.SetMode(ComparisonMode.Overlay);
            comparison.OverlayOpacity.Value = 0.35;
        });

        var b = Part<PicBox>(viewer, "ImageB");
        Assert.Equal(BitmapBlendingMode.Unspecified, b.BlendMode);
        Assert.Equal(0.35, b.Opacity, 3);
        Assert.Null(b.Clip);
    });

    [AvaloniaFact]
    public void SplitMode_AppliesClipForVerticalAndHorizontal() => WithWindow((window, vm, tab) =>
    {
        vm.ImageWidth.Value = 400;
        vm.ImageHeight.Value = 300;
        var viewer = Host(window, tab, configure: comparison =>
        {
            comparison.SetMode(ComparisonMode.Split);
            comparison.SplitRatio.Value = 0.4;
        });

        var b = Part<PicBox>(viewer, "ImageB");
        var host = Part<Grid>(viewer, "StackHost");
        Assert.True(host.Bounds.Width > 0 && host.Bounds.Height > 0);
        var clip = Assert.IsType<RectangleGeometry>(b.Clip);
        Assert.Equal(host.Bounds.Width * 0.4, clip.Rect.Width, 1);
        Assert.Equal(host.Bounds.Height, clip.Rect.Height, 1);

        viewer.Comparison.IsSplitHorizontal.Value = true;
        Settle(window);

        clip = Assert.IsType<RectangleGeometry>(b.Clip);
        Assert.Equal(host.Bounds.Width, clip.Rect.Width, 1);
        Assert.Equal(host.Bounds.Height * 0.4, clip.Rect.Height, 1);
    });

    [AvaloniaFact]
    public void SplitMode_ZoomedIn_DraggingHandleChangesRatioWithoutPanning() => WithWindow((window, vm, tab) =>
    {
        vm.ImageWidth.Value = 400;
        vm.ImageHeight.Value = 300;
        var viewer = Host(window, tab, configure: comparison =>
        {
            comparison.SetMode(ComparisonMode.Split);
            comparison.SplitRatio.Value = 0.5;
        });
        var zoom = Part<ZoomPanControl>(viewer, "ZoomPanControl");
        var handle = Part<Border>(viewer, "SplitHandle");

        zoom.SetScaleImmediate(3.0);
        // The zoom preview sizes itself from the main view, which this host replaces, so unsized it would cover all
        Part<ZoomPreviewer>(viewer, "ZoomPreview").IsVisible = false;
        Settle(window);

        var initialTx = zoom.TranslateX;
        var start = CenterOf(handle, window);
        var end = new Point(start.X + 40, start.Y);
        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(end);
        window.MouseUp(end, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.True(viewer.Comparison.SplitRatio.Value > 0.5);
        Assert.Equal(initialTx, zoom.TranslateX, 3);
    });

    [AvaloniaFact]
    public void SyncedMode_ZoomIn_SyncsLeftAndRightScale() => WithWindow((window, _, tab) =>
    {
        var viewer = Host(window, tab, configure: static comparison => comparison.SetMode(ComparisonMode.Synced));
        var left = Part<ZoomPanControl>(viewer, "SyncedLeftZoom");
        var right = Part<ZoomPanControl>(viewer, "SyncedRightZoom");

        ((IZoomScrollTarget)viewer).ZoomIn();
        Settle(window);

        Assert.True(left.Scale > 1.0, $"Expected left.Scale > 1.0, got {left.Scale}");
        Assert.Equal(left.Scale, right.Scale, 3);
    });

    [AvaloniaFact]
    public void SyncedMode_DifferentSizedImages_SyncsNormalizedCenter() => WithWindow((window, _, tab) =>
    {
        var viewer = Host(window, tab, Sized("a.jpg", 400, 200), Sized("b.jpg", 200, 400),
            static comparison => comparison.SetMode(ComparisonMode.Synced));
        var leftZoom = Part<ZoomPanControl>(viewer, "SyncedLeftZoom");
        var rightZoom = Part<ZoomPanControl>(viewer, "SyncedRightZoom");

        // A point inside A that the bounds do not clamp
        leftZoom.ShowChildAt(3.0, new Point(300, 250));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(3.0, rightZoom.Scale, 3);
        Assert.NotEqual(leftZoom.TranslateX, rightZoom.TranslateX);
    });

    [AvaloniaFact]
    public void SyncedMode_WideImage_IsCenteredAtScale1_AndPansVerticallyWhenZoomed() => WithWindow((window, _, tab) =>
    {
        var viewer = Host(window, tab, Sized("wide.gif", 400, 100), Sized("tall.jpg", 200, 400),
            static comparison => comparison.SetMode(ComparisonMode.Synced));
        var leftZoom = Part<ZoomPanControl>(viewer, "SyncedLeftZoom");
        var rightZoom = Part<ZoomPanControl>(viewer, "SyncedRightZoom");
        var leftContainer = Part<Grid>(viewer, "SyncedLeftContainer");
        var leftImage = Part<PicBox>(viewer, "SyncedLeftImage");

        // The container fills the zoom slot and centers the wide image instead of pushing it down
        Assert.Equal(leftZoom.Bounds.Height, leftContainer.Bounds.Height, 1);
        var centeredY = (leftContainer.Bounds.Height - leftImage.Bounds.Height) * 0.5;
        Assert.InRange(leftImage.Bounds.Y, centeredY - 1, centeredY + 1);
        Assert.Equal(0, leftZoom.TranslateX, 1);
        Assert.Equal(0, leftZoom.TranslateY, 1);
        Assert.Equal(0, rightZoom.TranslateX, 1);
        Assert.Equal(0, rightZoom.TranslateY, 1);

        // A slight zoom pans both axes on both sides
        leftZoom.ShowChildAt(1.2, new Point(120, 350));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1.2, leftZoom.Scale, 3);
        Assert.Equal(1.2, rightZoom.Scale, 3);
        Assert.NotEqual(0, leftZoom.TranslateY, 1);

        leftZoom.SetConstrainedTranslation(-20, -60);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(-20, leftZoom.TranslateX, 1);
        Assert.Equal(-60, leftZoom.TranslateY, 1);
        Assert.NotEqual(0, rightZoom.TranslateY, 1);

        viewer.ResetZoom(false);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1.0, leftZoom.Scale, 3);
        Assert.Equal(1.0, rightZoom.Scale, 3);
        Assert.Equal(0, leftZoom.TranslateX, 1);
        Assert.Equal(0, leftZoom.TranslateY, 1);
        Assert.Equal(0, rightZoom.TranslateX, 1);
        Assert.Equal(0, rightZoom.TranslateY, 1);
    });

    [AvaloniaFact]
    public void ZoomIn_WritesTheTabsZoomFactor() => WithWindow((window, _, tab) =>
    {
        var viewer = Host(window, tab);

        ((IZoomScrollTarget)viewer).ZoomIn();
        Settle(window);

        Assert.True(tab.Zoom.Factor > 1, $"Expected the factor above 1, got {tab.Zoom.Factor}");
    });

    [AvaloniaFact]
    public void SyncedMode_ZoomIn_WritesTheFactorFromTheLeft() => WithWindow((window, _, tab) =>
    {
        var viewer = Host(window, tab, configure: static comparison => comparison.SetMode(ComparisonMode.Synced));

        ((IZoomScrollTarget)viewer).ZoomIn();
        Settle(window);

        var left = Part<ZoomPanControl>(viewer, "SyncedLeftZoom");
        Assert.True(left.Scale > 1);
        Assert.Equal(left.Scale, tab.Zoom.Factor, 3);
    });

    [AvaloniaFact]
    public void ShowFactor_BeforeLoaded_StartsAtThatFactor() => WithWindow((window, _, tab) =>
    {
        var viewer = Host(window, tab, beforeHost: static viewer => viewer.ShowFactor(2));

        Assert.Equal(2, Part<ZoomPanControl>(viewer, "ZoomPanControl").Scale, 3);
    });

    [AvaloniaFact]
    public void Aligning_FourClicks_TransformsBAndStops() => WithWindow((window, vm, tab) =>
    {
        vm.ImageWidth.Value = 400;
        vm.ImageHeight.Value = 300;
        var viewer = Host(window, tab);
        var stack = Part<Grid>(viewer, "StackHost");
        var markers = Part<Canvas>(viewer, "AlignMarkersCanvas");

        viewer.Comparison.ToggleAligning();
        Settle(window);
        // A1, B1, A2, B2: B is A moved right by 20, so the alignment is a translation
        Press(window, stack, new Point(100, 100));
        Press(window, stack, new Point(120, 100));
        Press(window, stack, new Point(300, 200));
        Assert.Equal(3, markers.Children.Count);
        Press(window, stack, new Point(320, 200));

        Assert.IsType<MatrixTransform>(Part<PicBox>(viewer, "ImageB").RenderTransform);
        Assert.False(viewer.Comparison.IsAligning.Value);
        Assert.Empty(markers.Children);
    });

    [AvaloniaFact]
    public void Aligning_On_SetsTheZoomControlAligning() => WithWindow((window, _, tab) =>
    {
        var viewer = Host(window, tab);
        var zoom = Part<ZoomPanControl>(viewer, "ZoomPanControl");

        viewer.Comparison.ToggleAligning();
        Settle(window);
        Assert.True(zoom.IsAligning);

        viewer.Comparison.ToggleAligning();
        Settle(window);
        Assert.False(zoom.IsAligning);
    });

    [AvaloniaFact]
    public void BlinkMode_Detached_StopsBlinking() => WithWindow((window, _, tab) =>
    {
        var viewer = Host(window, tab);

        viewer.Comparison.SetMode(ComparisonMode.Blink);
        Settle(window);
        Assert.True(viewer.IsBlinking);

        window.Content = null;
        Settle(window);
        Assert.False(viewer.IsBlinking);
    });

    [AvaloniaFact]
    public void Swap_ExchangesTheShownImages() => WithWindow((window, _, tab) =>
    {
        var b = BitmapModel("b.jpg");
        var viewer = Host(window, tab, b: b);

        viewer.Comparison.Swap();
        Settle(window);

        Assert.Same(b.Image, Part<PicBox>(viewer, "ImageA").Source);
    });

    [AvaloniaFact]
    public void SetMode_SyncedThenOverlay_FactorFollowsTheShownLayout() => WithWindow((window, _, tab) =>
    {
        var viewer = Host(window, tab);
        var zoom = Part<ZoomPanControl>(viewer, "ZoomPanControl");
        ((IZoomScrollTarget)viewer).ZoomIn();
        ((IZoomScrollTarget)viewer).ZoomIn();
        Settle(window);

        viewer.Comparison.SetMode(ComparisonMode.Synced);
        Settle(window);
        Assert.Equal(1, tab.Zoom.Factor, 3);
        Assert.Equal(1, Part<ZoomPanControl>(viewer, "SyncedLeftZoom").Scale, 3);

        viewer.Comparison.SetMode(ComparisonMode.Overlay);
        Settle(window);
        Assert.True(zoom.Scale > 1);
        Assert.Equal(zoom.Scale, tab.Zoom.Factor, 3);
    });

    [AvaloniaFact]
    public void Orientation_Turned_RotatesAndFlipsTheStackedLayout() => WithWindow((window, _, tab) =>
    {
        var viewer = Host(window, tab);

        tab.Orientation.TurnTo(1);
        tab.Orientation.Flip();
        Settle(window);

        var transform = Part<LayoutTransformControl>(viewer, "StackTransform");
        Assert.Equal(90, Assert.IsType<RotateTransform>(transform.LayoutTransform).Angle);
        Assert.Equal(-1, Assert.IsType<ScaleTransform>(transform.RenderTransform).ScaleX);
    });

    [AvaloniaFact]
    public void TriggerScalingModeUpdate_NearestNeighbor_SetsAllFourPicBoxes() => WithWindow((window, _, tab) =>
    {
        var viewer = Host(window, tab);

        Settings.ImageScaling.IsScalingSetToNearestNeighbor = true;
        viewer.TriggerScalingModeUpdate(true);

        foreach (var name in (string[])["ImageA", "ImageB", "SyncedLeftImage", "SyncedRightImage"])
        {
            Assert.Equal(BitmapInterpolationMode.None,
                RenderOptions.GetBitmapInterpolationMode(Part<PicBox>(viewer, name)));
        }
    });

    [AvaloniaFact]
    public void ScrollEnabled_TallPair_ScrollsThePairTogether() => WithWindow((window, vm, tab) =>
    {
        // Fitted with scrolling on: the pair is as wide as the view and taller than it
        ShowTallPairFit(vm);
        var viewer = Host(window, tab);
        var target = (IZoomScrollTarget)viewer;

        Assert.True(target.CanScrollVertically);
        Assert.True(target.IsImageTallerThanViewer);

        var scroll = Part<AutoScrollViewer>(viewer, "StackScrollViewer");
        target.ScrollLine(true);
        Settle(window);
        Assert.True(scroll.Offset.Y > 0, $"Expected the offset below 0, got {scroll.Offset.Y}");

        target.ScrollToEnd();
        Settle(window);
        Assert.Equal(scroll.Extent.Height - scroll.Viewport.Height, scroll.Offset.Y, 1);

        target.ScrollToHome();
        Settle(window);
        Assert.Equal(0, scroll.Offset.Y, 1);
    });

    [AvaloniaFact]
    public void SplitMode_Scrolled_KeepsTheDividerOnTheSplit() => WithWindow((window, vm, tab) =>
    {
        ShowTallPairFit(vm);
        var viewer = Host(window, tab, configure: static comparison =>
        {
            comparison.SetMode(ComparisonMode.Split);
            comparison.IsSplitHorizontal.Value = true;
            comparison.SplitRatio.Value = 0.5;
        });
        var scroll = Part<AutoScrollViewer>(viewer, "StackScrollViewer");
        var host = Part<Grid>(viewer, "StackHost");
        var main = Part<Panel>(viewer, "MainPanel");
        var line = Part<Border>(viewer, "SplitDividerLine");
        var strip = Part<Border>(viewer, "SplitDividerHitStrip");
        var handle = Part<Border>(viewer, "SplitHandle");

        scroll.Offset = new Vector(0, 400);
        Settle(window);
        Assert.True(scroll.Offset.Y > 0, $"Expected the offset below 0, got {scroll.Offset.Y}");

        var splitY = host.Bounds.Height * 0.5;
        var expectedY = host.TranslatePoint(new Point(host.Bounds.Width * 0.5, splitY), main)!.Value.Y;
        Assert.InRange(expectedY, 15, main.Bounds.Height - 15);
        Assert.Equal(expectedY - 1, line.Margin.Top, 1);
        Assert.Equal(expectedY - 8, strip.Margin.Top, 1);
        Assert.Equal(expectedY - 14, handle.Margin.Top, 1);
    });

    [AvaloniaFact]
    public void ScrollEnabled_Synced_DoesNotScroll()=> WithWindow((window, vm, tab) =>
    {
        ShowTallPairFit(vm);
        var viewer = Host(window, tab, configure: static comparison => comparison.SetMode(ComparisonMode.Synced));
        var target = (IZoomScrollTarget)viewer;

        Assert.False(target.CanScrollVertically);
        Assert.False(target.IsImageTallerThanViewer);
    });

    [AvaloniaFact]
    public void ScrollDisabled_TallPair_DoesNotScroll() => WithWindow((window, vm, tab) =>
    {
        ShowTallPairFit(vm);
        vm.GlobalSettings.IsScrollingEnabled.Value = false;
        var viewer = Host(window, tab);

        Assert.False(((IZoomScrollTarget)viewer).CanScrollVertically);
    });

    // The fit the window gives a tall image with scrolling on: the view's size, and the image taller than it
    private static void ShowTallPairFit(MainWindowViewModel vm)
    {
        vm.GlobalSettings.IsScrollingEnabled.Value = true;
        vm.ScrollViewerWidth.Value = 400;
        vm.ScrollViewerHeight.Value = 300;
        vm.ImageWidth.Value = 400;
        vm.ImageHeight.Value = 1200;
    }

    [AvaloniaFact]
    public void TakesPress_OverTheSplitHandle_IsTrue() => WithWindow((window, vm, tab) =>
    {
        vm.ImageWidth.Value = 400;
        vm.ImageHeight.Value = 300;
        var viewer = Host(window, tab, configure: static comparison => comparison.SetMode(ComparisonMode.Split));

        Assert.True(TakesPressAt(window, viewer, CenterOf(Part<Border>(viewer, "SplitHandle"), window)));
    });

    [AvaloniaFact]
    public void TakesPress_OverTheSplitStrip_IsTrue() => WithWindow((window, vm, tab) =>
    {
        vm.ImageWidth.Value = 400;
        vm.ImageHeight.Value = 300;
        var viewer = Host(window, tab, configure: static comparison => comparison.SetMode(ComparisonMode.Split));
        var strip = Part<Border>(viewer, "SplitDividerHitStrip");

        // Below the handle, on the strip
        var point = strip.TranslatePoint(new Point(strip.Bounds.Width / 2, strip.Bounds.Height - 10), window);

        Assert.True(TakesPressAt(window, viewer, point!.Value));
    });

    [AvaloniaFact]
    public void TakesPress_WhileAligning_IsTrue() => WithWindow((window, vm, tab) =>
    {
        vm.ImageWidth.Value = 400;
        vm.ImageHeight.Value = 300;
        var viewer = Host(window, tab);
        viewer.Comparison.ToggleAligning();
        Settle(window);

        Assert.True(TakesPressAt(window, viewer, CenterOf(Part<Grid>(viewer, "StackHost"), window)));
    });

    [AvaloniaFact]
    public void TakesPress_WhileAligningInSynced_IsFalse() => WithWindow((window, vm, tab) =>
    {
        // The stacked pair the points are picked on is hidden, so a press is the window's
        vm.ImageWidth.Value = 400;
        vm.ImageHeight.Value = 300;
        var viewer = Host(window, tab, configure: static comparison => comparison.SetMode(ComparisonMode.Synced));
        viewer.Comparison.ToggleAligning();
        Settle(window);
        Assert.True(viewer.Comparison.IsAligning.Value);

        Assert.False(TakesPressAt(window, viewer, CenterOf(Part<Grid>(viewer, "SyncedGrid"), window)));
    });

    [AvaloniaFact]
    public void TakesPress_OverTheImage_IsFalse() => WithWindow((window, vm, tab) =>
    {
        vm.ImageWidth.Value = 400;
        vm.ImageHeight.Value = 300;
        var viewer = Host(window, tab);

        Assert.False(TakesPressAt(window, viewer, CenterOf(Part<Grid>(viewer, "StackHost"), window)));
    });

    [AvaloniaFact]
    public void DoubleClick_OnTheImage_RunsTheDoubleClickAction() => WithWindow((window, vm, tab) =>
    {
        // The window's click handling runs as in the app, so the tests below that expect nothing can fail
        Settings.UIProperties.DoubleClickBehavior = 2;
        var (viewer, actions) = HostInNavigableView(window, vm, tab);

        DoubleClickAt(window, CenterOf(Part<Grid>(viewer, "StackHost"), window), new Vector(2, 0));

        Assert.Contains(Actions.ToggleFullscreen, actions.Calls);
    });

    [AvaloniaFact]
    public void DoubleClick_WhileAligning_DoesNotToggleFullscreen() => WithWindow((window, vm, tab) =>
    {
        Settings.UIProperties.DoubleClickBehavior = 2;
        var (viewer, actions) = HostInNavigableView(window, vm, tab);
        viewer.Comparison.ToggleAligning();
        Settle(window);

        // A1 and B1 a few pixels apart, quickly: a double-click to the window
        DoubleClickAt(window, CenterOf(Part<Grid>(viewer, "StackHost"), window), new Vector(2, 0));

        Assert.Empty(actions.Calls);
        Assert.Equal(3, viewer.Comparison.AlignmentStep.Value);
    });

    [AvaloniaFact]
    public void DoubleClick_WhileAligningZoomedIn_KeepsTheZoom() => WithWindow((window, vm, tab) =>
    {
        Settings.UIProperties.DoubleClickBehavior = 1;
        var (viewer, actions) = HostInNavigableView(window, vm, tab);
        var zoom = Part<ZoomPanControl>(viewer, "ZoomPanControl");
        zoom.SetScaleImmediate(2.0);
        // The zoom preview sizes itself from the main view, which this host lacks, so unsized it would cover all
        Part<ZoomPreviewer>(viewer, "ZoomPreview").IsVisible = false;
        viewer.Comparison.ToggleAligning();
        Settle(window);

        DoubleClickAt(window, CenterOf(Part<Grid>(viewer, "StackHost"), window), new Vector(2, 0));

        // Neither the window's Reset Zoom nor the zoom control's own double-click reset
        Assert.Empty(actions.Calls);
        Assert.Equal(2.0, zoom.Scale, 3);
        Assert.Equal(3, viewer.Comparison.AlignmentStep.Value);
    });

    [AvaloniaFact]
    public void DoubleClick_OnTheSplitHandle_OnlyResetsTheRatio() => WithWindow((window, vm, tab) =>
    {
        Settings.UIProperties.DoubleClickBehavior = 2;
        var (viewer, actions) = HostInNavigableView(window, vm, tab, static comparison =>
        {
            comparison.SetMode(ComparisonMode.Split);
            comparison.SplitRatio.Value = 0.3;
        });

        DoubleClickAt(window, CenterOf(Part<Border>(viewer, "SplitHandle"), window), default);

        Assert.Empty(actions.Calls);
        Assert.Equal(0.5, viewer.Comparison.SplitRatio.Value, 3);
    });

    [AvaloniaFact]
    public async Task Display_OnAnotherThreadAfterAligning_ShowsTheNewPairUnaligned() => await WithWindowAsync(
        async (window, vm, tab) =>
        {
            // A tab shows its next pair on a loading thread, which the comparison's changes then come from
            var viewer = ShowComparing(window, vm, tab);
            var stack = Part<Grid>(viewer, "StackHost");
            var b = Part<PicBox>(viewer, "ImageB");
            viewer.Comparison.ToggleAligning();
            Settle(window);
            Press(window, stack, new Point(100, 100));
            Press(window, stack, new Point(120, 100));
            Press(window, stack, new Point(300, 200));
            Press(window, stack, new Point(320, 200));
            Assert.IsType<MatrixTransform>(b.RenderTransform);
            var (c, d) = (BitmapModel("c.jpg"), BitmapModel("d.jpg"));

            await Task.Run(() => tab.Display(c, d)).ConfigureAwait(true);
            Settle(window);

            Assert.Null(b.RenderTransform);
            Assert.Same(c.Image, Part<PicBox>(viewer, "ImageA").Source);
            Assert.Same(d.Image, b.Source);
            Assert.Equal("A · c.jpg", Part<TextBlock>(viewer, "SyncedLeftBadgeText").Text);
            Assert.Equal("B · d.jpg", Part<TextBlock>(viewer, "SyncedRightBadgeText").Text);
        });

    [AvaloniaFact]
    public async Task Display_OnAnotherThreadWhileAligning_StopsAligningInTheView() => await WithWindowAsync(
        async (window, vm, tab) =>
        {
            var viewer = ShowComparing(window, vm, tab);
            var stack = Part<Grid>(viewer, "StackHost");
            var markers = Part<Canvas>(viewer, "AlignMarkersCanvas");
            var hint = Assert.IsType<TextBlock>(Part<CompareBar>(viewer, "CompareBar").FindControl<TextBlock>("AlignHintText"));
            viewer.Comparison.ToggleAligning();
            Settle(window);
            Press(window, stack, new Point(100, 100));
            Assert.Single(markers.Children);
            Assert.NotNull(stack.Cursor);
            Assert.False(string.IsNullOrEmpty(hint.Text));
            var (c, d) = (BitmapModel("c.jpg"), BitmapModel("d.jpg"));

            await Task.Run(() => tab.Display(c, d)).ConfigureAwait(true);
            Settle(window);

            Assert.Empty(markers.Children);
            Assert.Null(stack.Cursor);
            Assert.False(Part<ZoomPanControl>(viewer, "ZoomPanControl").IsAligning);
            Assert.True(string.IsNullOrEmpty(hint.Text));
        });

    // Turns the tab's comparing on over a pair, so the tab passes its next pair to the comparison, and shows its view
    private static ComparisonViewer ShowComparing(TestMainWindow window, MainWindowViewModel vm, TabViewModel tab)
    {
        window.Width = 800;
        window.Height = 600;
        vm.ImageWidth.Value = 400;
        vm.ImageHeight.Value = 300;
        tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
        tab.SetComparing(true);
        window.Content = Assert.IsType<NavigableView>(ViewOf(tab));
        Settle(window);
        return Assert.IsType<ComparisonViewer>(ViewerOf(tab));
    }

    // Shows the viewer in the tab's navigable view, in place of the image viewer, with the window's click handling
    // and its Actions recorded
    private static (ComparisonViewer Viewer, RecordingActionRunner Actions) HostInNavigableView(
        TestMainWindow window, MainWindowViewModel vm, TabViewModel tab,
        Action<ComparisonViewModel>? configure = null)
    {
        window.Width = 800;
        window.Height = 600;
        vm.ImageWidth.Value = 400;
        vm.ImageHeight.Value = 300;
        tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
        var comparison = new ComparisonViewModel(tab.Model, tab.SecondaryModel, static () => { });
        configure?.Invoke(comparison);
        var viewer = new ComparisonViewer(tab, comparison);
        var nav = Assert.IsType<NavigableView>(ViewOf(tab));
        nav.Show(viewer);
        window.Content = nav;
        // The bars the click handling asks about, which the app sets up
        window.SharedTitleBar = new CustomTitleBar();
        window.SharedBottomBar = new BottomBar();
        var actions = new RecordingActionRunner();
        vm.ActionRunner = actions;
        window.AddHandler(InputElement.PointerPressedEvent,
            (_, e) => _ = ImageViewerClickBehavior.PointerPressedAsync(e, window), RoutingStrategies.Tunnel);
        Settle(window);
        return (viewer, actions);
    }

    private static void DoubleClickAt(TestMainWindow window, Point first, Vector offset)
    {
        window.MouseDown(first, MouseButton.Left);
        window.MouseUp(first, MouseButton.Left);
        var second = first + offset;
        window.MouseDown(second, MouseButton.Left);
        window.MouseUp(second, MouseButton.Left);
        Settle(window);
    }

    // Hosts the viewer in the window after the tab shows the pair, A and B unless given
    private static ComparisonViewer Host(TestMainWindow window, TabViewModel tab, ImageModel? a = null,
        ImageModel? b = null, Action<ComparisonViewModel>? configure = null,
        Action<ComparisonViewer>? beforeHost = null)
    {
        window.Width = 800;
        window.Height = 600;
        tab.Display(a ?? BitmapModel("a.jpg"), b ?? BitmapModel("b.jpg"));
        var comparison = new ComparisonViewModel(tab.Model, tab.SecondaryModel, static () => { });
        configure?.Invoke(comparison);
        var viewer = new ComparisonViewer(tab, comparison);
        beforeHost?.Invoke(viewer);
        window.Content = viewer;
        Settle(window);
        return viewer;
    }

    private static T Part<T>(ComparisonViewer viewer, string name) where T : Control =>
        Assert.IsType<T>(viewer.FindControl<T>(name));

    private static ImageModel Sized(string name, int width, int height) => new()
    {
        FileInfo = new FileInfo(name),
        Image = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96)),
        PixelWidth = (uint)width,
        PixelHeight = (uint)height,
    };

    private static Point CenterOf(Control control, Window window) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    private static void Press(TestMainWindow window, Control target, Point point)
    {
        var at = target.TranslatePoint(point, window)!.Value;
        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        Settle(window);
    }

    // What the viewer answers to a press the window sees first, as the click navigation does
    private static bool TakesPressAt(TestMainWindow window, ComparisonViewer viewer, Point point)
    {
        bool? taken = null;
        EventHandler<PointerPressedEventArgs> handler = (_, e) => taken = ((INavigableContent)viewer).TakesPress(e);
        window.AddHandler(InputElement.PointerPressedEvent, handler, RoutingStrategies.Tunnel);
        try
        {
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Settle(window);
        }
        finally
        {
            window.RemoveHandler(InputElement.PointerPressedEvent, handler);
        }

        Assert.NotNull(taken);
        return taken.Value;
    }
}
