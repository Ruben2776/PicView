using Avalonia;
using PicView.Avalonia.Crop;

namespace PicView.Tests.Crop;

/// <summary>
/// A crop opens with its selection in the middle of the part of the image the view shows, so a crop started on a
/// zoomed image has its selection on screen (ADR-0027).
/// </summary>
public class CropLayoutManagerTests
{
    [Fact]
    public void DefaultSelection_OnAWholeLargeImage_IsTwoHundredSquareInItsCentre()
        => Assert.Equal(new Rect(400, 275, 200, 200), CropLayoutManager.DefaultSelection(new Rect(0, 0, 1000, 750)));

    [Fact]
    public void DefaultSelection_OnASmallVisiblePart_IsHalfOfItInItsCentre()
        => Assert.Equal(new Rect(175, 100, 150, 100), CropLayoutManager.DefaultSelection(new Rect(100, 50, 300, 200)));
}
