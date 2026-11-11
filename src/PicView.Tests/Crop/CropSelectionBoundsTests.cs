using Avalonia;
using PicView.Avalonia.Crop;

namespace PicView.Tests.Crop;

/// <summary>
/// Moving or resizing the selection keeps it in the part of the image the view shows, which is the whole image
/// until the crop is zoomed in (ADR-0027).
/// </summary>
public class CropSelectionBoundsTests
{
    private static readonly Rect Image = new(0, 0, 1000, 750);
    private static readonly Rect Visible = new(100, 50, 300, 200);

    [Fact]
    public void ClampMove_NotZoomed_StopsAtTheImagesEdge()
        => Assert.Equal(new Point(800, 275),
            CropDragHandler.ClampMove(new Rect(400, 275, 200, 200), new Point(1400, 275), Image, Image));

    [Fact]
    public void ClampMove_ZoomedIn_StopsAtTheFarEdgesOfTheVisiblePart()
        => Assert.Equal(new Point(300, 200),
            CropDragHandler.ClampMove(new Rect(150, 100, 100, 50), new Point(650, 600), Visible, Image));

    [Fact]
    public void ClampMove_ZoomedIn_StopsAtTheNearEdgesOfTheVisiblePart()
        => Assert.Equal(new Point(100, 50),
            CropDragHandler.ClampMove(new Rect(150, 100, 100, 50), new Point(-350, -400), Visible, Image));

    [Fact]
    public void ClampMove_SelectionWiderThanTheVisiblePart_MovesWithinTheImageOnThatAxis()
    {
        // A selection made before zooming in cannot fit the visible part, so that axis keeps the image's bounds
        var selection = new Rect(0, 60, 600, 100);

        Assert.Equal(new Point(50, 60), CropDragHandler.ClampMove(selection, new Point(50, 60), Visible, Image));
        Assert.Equal(new Point(0, 50), CropDragHandler.ClampMove(selection, new Point(-50, 0), Visible, Image));
        Assert.Equal(new Point(400, 150), CropDragHandler.ClampMove(selection, new Point(900, 900), Visible, Image));
    }

    [Fact]
    public void Constrain_PastTheFarEdge_ShrinksToTheBounds()
        => Assert.Equal(new Rect(150, 100, 250, 50), CropResizer.Constrain(new Rect(150, 100, 400, 50), Visible));

    [Fact]
    public void Constrain_PastTheNearEdge_KeepsTheOppositeEdge()
        => Assert.Equal(new Rect(100, 50, 50, 100), CropResizer.Constrain(new Rect(50, 20, 100, 130), Visible));

    [Fact]
    public void ResizeBounds_SelectionInsideTheVisiblePart_IsTheVisiblePart()
        => Assert.Equal(Visible, CropResizer.ResizeBounds(new Rect(150, 100, 100, 50), Visible, Image));

    [Fact]
    public void ResizeBounds_SelectionReachingOutsideTheVisiblePart_KeepsItsOwnEdges()
        // An edge that is already off screen stays where it is while another edge is dragged
        => Assert.Equal(new Rect(0, 50, 400, 200), CropResizer.ResizeBounds(new Rect(0, 60, 250, 100), Visible, Image));
}
