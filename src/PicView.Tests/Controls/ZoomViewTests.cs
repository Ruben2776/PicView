using Avalonia;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.CustomControls.Zoom;

namespace PicView.Tests.Controls;

/// <summary>
/// The zoom handed between a tab's viewer and its crop (ADR-0027) is a scale and the view centre as a fraction of
/// the image, so it holds though the two views lay the image out at different sizes.
/// </summary>
public class ZoomViewTests
{
    [Fact]
    public void From_CentreOfAnOffsetImage_IsTheFraction()
        => Assert.Equal(new ZoomView(2, 0.25, 0.5),
            ZoomView.From(2, new Point(200, 250), new Rect(100, 50, 400, 400)));

    [Fact]
    public void ToChildCentre_InASmallerImageRect_KeepsTheFraction()
        => Assert.Equal(new Point(50, 100), new ZoomView(2, 0.25, 0.5).ToChildCenter(new Rect(0, 0, 200, 200)));

    [Fact]
    public void ToChildCentre_InAnOffsetImageRect_AddsTheOffset()
        => Assert.Equal(new Point(110, 120), new ZoomView(2, 0.25, 0.5).ToChildCenter(new Rect(60, 20, 200, 200)));

    [Fact]
    public void From_AnEmptyImageRect_IsTheCentre()
        => Assert.Equal(new ZoomView(3, 0.5, 0.5), ZoomView.From(3, new Point(10, 10), default));

    [Fact]
    public void Fit_IsScaleOneAtTheCentre()
        => Assert.Equal(new ZoomView(1, 0.5, 0.5), ZoomView.Fit);
}
