using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
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
