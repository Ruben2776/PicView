using PicView.Core.Sizing;
using PicView.Core.ViewModels;

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

    private static CropViewModel Crop(TabZoom zoom, double fitScale = 0.25, uint w = 4000, uint h = 3000)
    {
        zoom.SetFitScale(fitScale);
        return new CropViewModel(zoom, w, h);
    }
}
