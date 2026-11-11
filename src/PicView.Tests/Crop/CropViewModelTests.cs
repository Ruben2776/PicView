using PicView.Core.ImageTransformations;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Crop;

/// <summary>
/// The crop's selection is held in whole image pixels, and the view-unit values the crop surface draws are derived
/// from them through the tab's Fit Scale (ADR-0027).
/// </summary>
public class CropViewModelTests
{
    [Fact]
    public void AspectRatio_IsTheTabsFitScale()
    {
        using var zoom = new TabZoom();
        using var crop = new CropViewModel(zoom, 100, 100);

        zoom.SetFitScale(0.5);
        Assert.Equal(0.5, crop.AspectRatio);

        zoom.SetFitScale(2);
        Assert.Equal(2, crop.AspectRatio);
    }

    [Fact]
    public void SetSelection_AQuarterViewUnitAtFitScaleAQuarter_MovesOnePixel()
    {
        using var zoom = new TabZoom();
        using var crop = Crop(zoom);

        crop.SetSelection(100, 50, 200, 100);
        Assert.Equal((400, 200, 800u, 400u),
            (crop.PixelX.Value, crop.PixelY.Value, crop.PixelSelectionWidth.Value, crop.PixelSelectionHeight.Value));

        crop.SetSelection(100.25, 50, 200, 100);
        Assert.Equal(401, crop.PixelX.Value);
        Assert.Equal(100.25, crop.SelectionX.Value);
    }

    [Fact]
    public void SetSelection_PastTheRightEdge_ShrinksToTheImage()
    {
        using var zoom = new TabZoom();
        using var crop = Crop(zoom);

        crop.SetSelection(990, 0, 50, 10);

        Assert.Equal(3960, crop.PixelX.Value);
        Assert.Equal(40u, crop.PixelSelectionWidth.Value);
        Assert.Equal(10, crop.SelectionWidth.Value);
    }

    [Fact]
    public void SetSelection_NegativeOrigin_ClampsToZero()
    {
        using var zoom = new TabZoom();
        using var crop = Crop(zoom);

        crop.SetSelection(-5, -5, 20, 20);

        Assert.Equal((0, 0), (crop.PixelX.Value, crop.PixelY.Value));
    }

    [Fact]
    public void SetSelection_ZeroSize_IsOnePixel()
    {
        using var zoom = new TabZoom();
        using var crop = Crop(zoom);

        crop.SetSelection(0, 0, 0, 0);

        Assert.Equal((1u, 1u), (crop.PixelSelectionWidth.Value, crop.PixelSelectionHeight.Value));
    }

    [Fact]
    public void SetSelection_OnAOnePixelImageEnlarged_StaysInsideIt()
    {
        using var zoom = new TabZoom();
        using var crop = Crop(zoom, fitScale: 8, w: 1, h: 1);

        crop.SetSelection(3, 3, 100, 100);

        Assert.Equal((0, 0, 1u, 1u),
            (crop.PixelX.Value, crop.PixelY.Value, crop.PixelSelectionWidth.Value, crop.PixelSelectionHeight.Value));
    }

    [Fact]
    public void Refit_AfterAFitScaleChange_KeepsThePixelsAndChangesTheView()
    {
        using var zoom = new TabZoom();
        using var crop = Crop(zoom);
        crop.SetSelection(100.25, 50, 200, 100);

        zoom.SetFitScale(0.5);
        crop.Refit();

        Assert.Equal(401, crop.PixelX.Value);
        Assert.Equal(200.5, crop.SelectionX.Value);
        Assert.Equal(400, crop.SelectionWidth.Value);
    }

    [Fact]
    public void TurnRight_FourTimes_KeepsTheSourceSelection()
    {
        using var zoom = new TabZoom();
        using var orientation = new TabOrientation(null);
        using var crop = Turnable(zoom, orientation);

        for (var i = 0; i < 4; i++)
        {
            orientation.TurnRight();
            Assert.Equal(new PixelArea(10, 20, 100, 50), crop.SourceSelection);
        }
    }

    [Fact]
    public void TurnRight_SwapsTheShownSize()
    {
        using var zoom = new TabZoom();
        using var orientation = new TabOrientation(null);
        using var crop = Turnable(zoom, orientation);

        orientation.TurnRight();

        Assert.Equal((300u, 400u), (crop.ShownPixelWidth, crop.ShownPixelHeight));
        Assert.Equal((300d, 400d), (crop.ShownWidth.Value, crop.ShownHeight.Value));
    }

    [Fact]
    public void TurnRight_ShowsTheSelectionTurned()
    {
        using var zoom = new TabZoom();
        using var orientation = new TabOrientation(null);
        using var crop = Turnable(zoom, orientation);

        orientation.TurnRight();

        Assert.Equal((230, 10, 50u, 100u),
            (crop.PixelX.Value, crop.PixelY.Value, crop.PixelSelectionWidth.Value, crop.PixelSelectionHeight.Value));
    }

    [Fact]
    public void TurnRightFlipTurnLeft_KeepsTheSourceSelection()
    {
        using var zoom = new TabZoom();
        using var orientation = new TabOrientation(null);
        using var crop = Turnable(zoom, orientation);

        orientation.TurnRight();
        orientation.Flip();
        orientation.TurnLeft();

        Assert.Equal(new PixelArea(10, 20, 100, 50), crop.SourceSelection);
    }

    [Fact]
    public void Flip_Twice_KeepsTheSourceSelection()
    {
        using var zoom = new TabZoom();
        using var orientation = new TabOrientation(null);
        using var crop = Turnable(zoom, orientation);

        orientation.Flip();
        orientation.Flip();

        Assert.Equal(new PixelArea(10, 20, 100, 50), crop.SourceSelection);
        Assert.Equal((10, 20), (crop.PixelX.Value, crop.PixelY.Value));
    }

    [Fact]
    public void Flip_MirrorsTheShownSelectionAndRaisesTurned()
    {
        using var zoom = new TabZoom();
        using var orientation = new TabOrientation(null);
        using var crop = Turnable(zoom, orientation);
        var raised = 0;
        using var subscription = crop.Turned.Subscribe(_ => raised++);

        orientation.Flip();

        Assert.Equal(1, raised);
        Assert.Equal((290, 20, 100u, 50u),
            (crop.PixelX.Value, crop.PixelY.Value, crop.PixelSelectionWidth.Value, crop.PixelSelectionHeight.Value));
        Assert.Equal(290, crop.SelectionX.Value);
        Assert.Equal(new PixelArea(10, 20, 100, 50), crop.SourceSelection);
    }

    [Fact]
    public void TurnRight_RaisesTurnedAfterTheSelectionMoved()
    {
        using var zoom = new TabZoom();
        using var orientation = new TabOrientation(null);
        using var crop = Turnable(zoom, orientation);
        var pixelXAtTurned = -1;
        var raised = 0;
        using var subscription = crop.Turned.Subscribe(_ =>
        {
            raised++;
            pixelXAtTurned = crop.PixelX.Value;
        });

        orientation.TurnRight();

        Assert.Equal(1, raised);
        Assert.Equal(230, pixelXAtTurned);
    }

    [Fact]
    public void TurnRight_WithTheSelectionAtTheBottomRightCorner_KeepsItInsideTheImage()
    {
        using var zoom = new TabZoom();
        using var orientation = new TabOrientation(null);
        using var crop = Turnable(zoom, orientation);
        crop.SetSelection(300, 250, 100, 50);

        orientation.TurnRight();

        Assert.Equal((0, 300, 50u, 100u),
            (crop.PixelX.Value, crop.PixelY.Value, crop.PixelSelectionWidth.Value, crop.PixelSelectionHeight.Value));
        Assert.Equal(new PixelArea(300, 250, 100, 50), crop.SourceSelection);
    }

    [Fact]
    public void SetSelection_AfterATurn_ClampsToTheShownSize()
    {
        using var zoom = new TabZoom();
        using var orientation = new TabOrientation(null);
        using var crop = Turnable(zoom, orientation);
        orientation.TurnRight();

        crop.SetSelection(290, 0, 50, 10);

        Assert.Equal(10u, crop.PixelSelectionWidth.Value);
    }

    [Fact]
    public void Dispose_StopsFollowingTheOrientation()
    {
        using var zoom = new TabZoom();
        using var orientation = new TabOrientation(null);
        var crop = Turnable(zoom, orientation);
        var raised = 0;
        using var subscription = crop.Turned.Subscribe(_ => raised++);

        crop.Dispose();
        orientation.TurnRight();

        Assert.Equal(0, raised);
    }

    private static CropViewModel Turnable(TabZoom zoom, TabOrientation orientation)
    {
        zoom.SetFitScale(1);
        var crop = new CropViewModel(zoom, 400, 300, orientation);
        crop.SetSelection(10, 20, 100, 50);
        return crop;
    }

    private static CropViewModel Crop(TabZoom zoom, double fitScale = 0.25, uint w = 4000, uint h = 3000)
    {
        zoom.SetFitScale(fitScale);
        return new CropViewModel(zoom, w, h);
    }
}
