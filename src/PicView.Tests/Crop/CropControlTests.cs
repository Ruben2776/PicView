using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PicView.Avalonia.ImageTransformations;
using PicView.Avalonia.Input;
using PicView.Avalonia.Views.UC;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using PicView.Tests.WindowBehavior;
using static PicView.Tests.UI.TabViewsHarness;

namespace PicView.Tests.Crop;

/// <summary>
/// The crop view zooms through its own zoom control (ADR-0027): its handles, border and size label keep their size
/// on screen, the zoom Actions reach it, and holding Space hands the pointer to the pan.
/// </summary>
[Collection("Sequential")]
public class CropControlTests
{
    public CropControlTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [AvaloniaFact]
    public void ScaleChanged_ToTwo_HalvesTheHandlesAndTheBorder()
        => WithCrop((_, _, tab, crop) =>
        {
            crop.ZoomPanControl.SetScaleImmediate(2);

            AssertScaled(crop.TopLeftButton, 0.5);
            AssertScaled(crop.BottomRightButton, 0.5);
            AssertScaled(crop.RightMiddleButton, 0.5);
            AssertScaled(crop.SizeBorder, 0.5);
            Assert.Equal(new Thickness(1), crop.MainRectangle.BorderThickness);
        });

    [AvaloniaFact]
    public void ScaleChanged_BelowOne_GrowsTheHandles()
    {
        Settings.Zoom.AvoidZoomingOut = false;
        WithCrop((_, _, _, crop) =>
        {
            crop.ZoomPanControl.SetScaleImmediate(0.5);

            AssertScaled(crop.TopLeftButton, 2);
            Assert.Equal(new Thickness(4), crop.MainRectangle.BorderThickness);
        });
    }

    [AvaloniaFact]
    public void ScaleChanged_ReportsTheZoomFactorToTheTab()
        => WithCrop((_, _, tab, crop) =>
        {
            crop.ZoomPanControl.SetScaleImmediate(2);

            Assert.Equal(2, tab.Zoom.Factor);
        });

    [AvaloniaFact]
    public void ZoomIn_ThroughTheZoomTarget_ZoomsTheCrop()
        => WithCrop((_, _, _, crop) =>
        {
            ((IZoomScrollTarget)crop).ZoomIn();

            Assert.Equal(1.2, crop.ZoomPanControl.Scale);
        });

    [AvaloniaFact]
    public void ResetZoom_ActionWhileCropping_ResetsTheCrop()
        => WithCrop((_, vm, _, crop) =>
        {
            crop.ZoomPanControl.SetScaleImmediate(2);

            RotationManager.ResetZoom(vm);

            Assert.Equal(1, crop.ZoomPanControl.Scale);
        });

    [AvaloniaFact]
    public void CanScrollVertically_IsFalse()
        => WithCrop((_, _, _, crop) =>
        {
            IZoomScrollTarget target = crop;

            Assert.False(target.CanScrollVertically);
            Assert.False(target.IsImageTallerThanViewer);
        });

    [AvaloniaFact]
    public void SpaceHeld_TurnsOffTheSelectionsHitTesting()
        => WithCrop((window, _, _, crop) =>
        {
            // The test window has no main view, so a host presents the crop, as the main view does
            window.Content = new ContentControl { Content = crop };
            Settle(window);
            Assert.True(crop.IsLoaded);
            Assert.True(crop.RootCanvas.IsHitTestVisible);

            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Assert.False(crop.RootCanvas.IsHitTestVisible);

            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Assert.True(crop.RootCanvas.IsHitTestVisible);
        });

    [AvaloniaFact]
    public void UpdateSelectionLayout_AtAFractionOfAViewUnit_DimsAllButTheSelectionInOneShape()
        => WithCrop((_, vm, tab, crop) =>
        {
            // A selection on image pixels lies at fractions of a view unit. Four dimming rectangles side by side
            // left seams there, and layout rounding moved the border off the pixels it marks
            vm.ImageWidth.Value = 1000;
            vm.ImageHeight.Value = 750;
            tab.Zoom.SetFitScale(0.25);
            tab.Crop = new CropViewModel(tab.Zoom, 4000, 3000);
            tab.Crop.SetSelection(100.25, 50.5, 200.25, 100.75);

            crop.UpdateSelectionLayout();

            var dimming = Assert.IsType<GeometryGroup>(crop.Dimming.Data);
            Assert.Equal(FillRule.EvenOdd, dimming.FillRule);
            Assert.Equal(2, dimming.Children.Count);
            Assert.Equal(new Rect(0, 0, 1000, 750), Assert.IsType<RectangleGeometry>(dimming.Children[0]).Rect);
            Assert.Equal(new Rect(100.25, 50.5, 200.25, 100.75),
                Assert.IsType<RectangleGeometry>(dimming.Children[1]).Rect);
            Assert.Equal(100.25, Canvas.GetLeft(crop.MainRectangle));
            Assert.Equal(50.5, Canvas.GetTop(crop.MainRectangle));
            Assert.False(crop.RootCanvas.UseLayoutRounding);
            Assert.False(crop.MainRectangle.UseLayoutRounding);
        });

    [AvaloniaFact]
    public void ViewChanged_AwayFromTheSelection_MovesTheSelectionIntoView()
        => WithLaidOutCrop((tab, crop) =>
        {
            tab.Crop!.SetSelection(10, 10, 40, 30);
            crop.UpdateSelectionLayout();

            // Four times zoom about a point near the bottom right shows 100 by 75 of the image, from 300, 212.5
            crop.ZoomPanControl.ShowChildAt(4, new Point(350, 250));

            Assert.Equal(new Rect(300, 212.5, 100, 75), crop.VisibleSurfaceRect());
            Assert.Equal(300, tab.Crop.SelectionX.Value);
            Assert.Equal(213, tab.Crop.SelectionY.Value);
            Assert.Equal(40, tab.Crop.SelectionWidth.Value);
            Assert.Equal(30, tab.Crop.SelectionHeight.Value);
            Assert.Equal(300, Canvas.GetLeft(crop.MainRectangle));
        });

    [AvaloniaFact]
    public void ViewChanged_WithTheSelectionAtTheTopOfAZoomedView_KeepsTheSizeLabelInView()
        => WithLaidOutCrop((tab, crop) =>
        {
            // The view shows 100 by 75 of the image from 300, 212.5, and the selection touches its top
            tab.Crop!.SetSelection(320, 213, 40, 30);

            crop.ZoomPanControl.ShowChildAt(4, new Point(350, 250));

            // The label is drawn at a quarter of its box, from the box's bottom left
            var label = crop.SizeBorder.Bounds.Size;
            var shownTop = Canvas.GetTop(crop.SizeBorder) + label.Height - label.Height / 4;
            var shownRight = Canvas.GetLeft(crop.SizeBorder) + label.Width / 4;
            Assert.True(label.Height > 0);
            Assert.Equal(212.5, shownTop, 6);
            Assert.True(shownRight <= 400);
            Assert.True(Canvas.GetLeft(crop.SizeBorder) >= 300);
        });

    [AvaloniaFact]
    public void ViewChanged_WithAnEdgeOutOfView_HidesThatEdgesHandles()
        => WithLaidOutCrop((tab, crop) =>
        {
            // Wider than the view shows: its left edge is out of view while its top, bottom and right edges are in
            tab.Crop!.SetSelection(10, 230, 380, 30);

            crop.ZoomPanControl.ShowChildAt(4, new Point(350, 250));

            Assert.False(crop.LeftMiddleButton.IsVisible);
            Assert.True(crop.RightMiddleButton.IsVisible);
            Assert.True(crop.TopRightButton.IsVisible);
            Assert.False(crop.TopLeftButton.IsVisible);
            Assert.True(crop.TopMiddleButton.IsVisible);
            Assert.True(crop.BottomMiddleButton.IsVisible);

            crop.ZoomPanControl.ShowChildAt(1, new Point(200, 150));

            Assert.True(crop.LeftMiddleButton.IsVisible);
            Assert.True(crop.TopLeftButton.IsVisible);
        });

    [AvaloniaFact]
    public void ContextMenuOpened_WithSpaceHeld_ForgetsSpace()
        => WithLaidOutCrop((tab, crop) =>
        {
            // The menu takes the keyboard, so the window may never see Space released
            var window = (Window)TopLevel.GetTopLevel(crop)!;
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Assert.True(crop.ZoomPanControl.IsSpaceHeld);

            // The headless window cannot show a popup, so the menu's own event stands in for opening it
            crop.ContextMenu!.RaiseEvent(new RoutedEventArgs(ContextMenu.OpenedEvent));

            Assert.False(crop.ZoomPanControl.IsSpaceHeld);
            Assert.True(crop.RootCanvas.IsHitTestVisible);
        });

    [AvaloniaFact]
    public void TallImageInAShorterView_ZoomedIn_ReachesItsTopAndBottom()
        => WithLaidOutCrop((_, crop) =>
        {
            // With scrolling on, an image is fitted taller than the view
            crop.ZoomPanControl.ShowChildAt(2, new Point(200, 0));
            Assert.Equal(0, crop.VisibleSurfaceRect().Y);

            crop.ZoomPanControl.ShowChildAt(2, new Point(200, 600));
            Assert.Equal(600, crop.VisibleSurfaceRect().Bottom);
        }, imageHeight: 600);

    [AvaloniaFact]
    public void ViewChanged_WithTheSelectionInView_LeavesTheSelection()
        => WithLaidOutCrop((tab, crop) =>
        {
            tab.Crop!.SetSelection(320, 230, 40, 30);

            crop.ZoomPanControl.ShowChildAt(4, new Point(350, 250));

            Assert.Equal(320, tab.Crop.SelectionX.Value);
            Assert.Equal(230, tab.Crop.SelectionY.Value);
        });

    [AvaloniaFact]
    public void ViewChanged_WithASelectionLargerThanTheView_LeavesTheSelection()
        => WithLaidOutCrop((tab, crop) =>
        {
            tab.Crop!.SetSelection(10, 10, 300, 200);

            crop.ZoomPanControl.ShowChildAt(4, new Point(350, 250));

            Assert.Equal(10, tab.Crop.SelectionX.Value);
            Assert.Equal(10, tab.Crop.SelectionY.Value);
        });

    [AvaloniaFact]
    public void TurnRight_SizesTheSurfaceToTheTurnedImage()
        => WithLaidOutCrop((window, _, tab, crop) =>
        {
            tab.Orientation.TurnRight();
            Settle(window);

            Assert.Equal(new Size(300, 400), crop.CropSurface.Bounds.Size);
        });

    [AvaloniaFact]
    public void HalfTurn_MovesTheDrawnSelection()
        => WithLaidOutCrop((window, _, tab, crop) =>
        {
            tab.Crop!.SetSelection(10, 20, 100, 50);
            tab.Orientation.TurnTo(2);
            Settle(window);

            Assert.Equal(290, Canvas.GetLeft(crop.MainRectangle));
            Assert.Equal(230, Canvas.GetTop(crop.MainRectangle));
        });

    [AvaloniaFact]
    public void TurnRight_WhileZoomedIn_PullsTheSelectionIntoView()
        => WithLaidOutCrop((window, _, tab, crop) =>
        {
            tab.Crop!.SetSelection(10, 10, 40, 30);
            crop.ZoomPanControl.ShowChildAt(4, new Point(350, 250));
            tab.Orientation.TurnRight();
            Settle(window);

            var selection = new Rect(tab.Crop.SelectionX.Value, tab.Crop.SelectionY.Value,
                tab.Crop.SelectionWidth.Value, tab.Crop.SelectionHeight.Value);
            var bounds = crop.SelectionBounds();
            Assert.True(bounds.Contains(selection.TopLeft) && bounds.Contains(selection.BottomRight),
                $"{selection} {bounds}");
        });

    [AvaloniaFact]
    public void RotationManagerRotateRight_WithACropShown_TurnsTheTab()
        => WithLaidOutCrop((window, vm, tab, _) =>
        {
            RotationManager.RotateRight(vm, window);

            Assert.Equal(1, tab.Orientation.Current.CurrentValue.QuarterTurns);
        });

    [AvaloniaFact]
    public void RotationManagerFlip_WithACropShown_FlipsTheTab()
        => WithLaidOutCrop((_, vm, tab, _) =>
        {
            RotationManager.Flip(vm);

            Assert.True(tab.Orientation.Current.CurrentValue.IsFlipped);
        });

    private static void WithLaidOutCrop(Action<TabViewModel, CropControl> test, double imageHeight = 300)
        => WithLaidOutCrop((_, _, tab, crop) => test(tab, crop), imageHeight);

    // A crop of a 400 by 300 image shown at its pixel size in a view of the same size, hosted and laid out
    private static void WithLaidOutCrop(
        Action<TestMainWindow, MainWindowViewModel, TabViewModel, CropControl> test, double imageHeight = 300)
        => WithWindow((window, vm, tab) =>
        {
            vm.ImageWidth.Value = 400;
            vm.ImageHeight.Value = imageHeight;
            vm.ScrollViewerWidth.Value = 400;
            vm.ScrollViewerHeight.Value = 300;
            tab.Display(BitmapModel("a.jpg"));
            tab.Zoom.SetFitScale(1);
            tab.Crop = new CropViewModel(tab.Zoom, 400, (uint)imageHeight, tab.Orientation);
            var crop = Assert.IsType<CropControl>(ViewOf(tab));
            window.Content = new ContentControl { Content = crop };
            Settle(window);
            Assert.True(crop.IsLoaded);
            test(window, vm, tab, crop);
        });

    private static void AssertScaled(Control element, double expected)
    {
        var transform = Assert.IsType<ScaleTransform>(element.RenderTransform);
        Assert.Equal(expected, transform.ScaleX);
        Assert.Equal(expected, transform.ScaleY);
    }

    // A tab that shows a bitmap and has a crop open, so its view is the crop control
    private static void WithCrop(Action<TestMainWindow, MainWindowViewModel, TabViewModel, CropControl> test)
        => WithWindow((window, vm, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"));
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);
            test(window, vm, tab, Assert.IsType<CropControl>(ViewOf(tab)));
        });
}
