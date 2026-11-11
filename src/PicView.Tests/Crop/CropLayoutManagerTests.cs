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

    private static readonly Rect Image = new(0, 0, 1000, 750);

    // What a view zoomed four times shows; a handle is then 5 wide on the canvas and the label a quarter of its size
    private static readonly Rect Zoomed = new(300, 212.5, 100, 75);
    private static readonly Size Label = new(120, 30);

    [Fact]
    public void PlaceHandle_AwayFromTheEdges_IsCentredOnItsPoint()
        => Assert.Equal(new Point(90, 90), CropLayoutManager.PlaceHandle(new Point(100, 100), Image, 1));

    [Fact]
    public void PlaceHandle_OnTheImagesCorner_StaysInsideIt()
        => Assert.Equal(new Point(0, 0), CropLayoutManager.PlaceHandle(new Point(0, 0), Image, 1));

    [Fact]
    public void PlaceHandle_OnTheCornerOfAZoomedView_StaysInsideTheViewAtItsSizeOnScreen()
        // Drawn 5 wide about the centre of its 20 wide box, so its box starts 7.5 before the handle does
        => Assert.Equal(new Point(292.5, 205), CropLayoutManager.PlaceHandle(new Point(300, 212.5), Zoomed, 4));

    [Fact]
    public void PlaceHandle_OnTheFarCornerOfAZoomedView_StaysInsideTheView()
        => Assert.Equal(new Point(387.5, 275), CropLayoutManager.PlaceHandle(new Point(400, 287.5), Zoomed, 4));

    [Theory]
    [InlineData(350, 250, true)]
    // On the view's own edge: the handle is shown, moved inwards
    [InlineData(400, 250, true)]
    [InlineData(300, 212.5, true)]
    // The selection's edge is out of view: a handle drawn at the view's edge would not be on the edge it moves
    [InlineData(500, 250, false)]
    [InlineData(350, 100, false)]
    public void ShowsHandle_OnlyWhenItsPointIsInView(double x, double y, bool expected)
        => Assert.Equal(expected, CropLayoutManager.ShowsHandle(new Point(x, y), Zoomed));

    [Fact]
    public void SnapInward_ToWholeImagePixels_NeverReachesOutsideTheView()
    {
        // The selection sits on whole image pixels, so what it may reach is the whole pixels inside the view
        Assert.Equal(new Rect(300.5, 212.5, 99.75, 75),
            CropLayoutManager.SnapInward(new Rect(300.3, 212.5, 100, 75), 0.25));
        Assert.Equal(new Rect(301, 213, 99, 74), CropLayoutManager.SnapInward(new Rect(300.3, 212.5, 100, 75), 1));
        Assert.Equal(Image, CropLayoutManager.SnapInward(Image, 0.25));
    }

    [Fact]
    public void SnapInward_WithNoWholePixelInView_IsTheViewItself()
        => Assert.Equal(new Rect(0.2, 0.2, 0.5, 0.5), CropLayoutManager.SnapInward(new Rect(0.2, 0.2, 0.5, 0.5), 1));

    [Fact]
    public void PlaceLabel_AwayFromTheEdges_SitsAboveAndBesideTheHandle()
        => Assert.Equal(new Point(112, 70), CropLayoutManager.PlaceLabel(new Point(100, 100), Label, Image, 1));

    [Fact]
    public void PlaceLabel_AtTheTopOfTheImage_StaysInsideIt()
        => Assert.Equal(new Point(112, 0), CropLayoutManager.PlaceLabel(new Point(100, 10), Label, Image, 1));

    [Fact]
    public void PlaceLabel_AtTheTopOfAZoomedView_StaysInsideTheView()
        // Drawn 30 by 7.5 from the bottom left of its box, so a label whose top is at 212.5 has its box at 190
        => Assert.Equal(new Point(305.5, 190),
            CropLayoutManager.PlaceLabel(new Point(302.5, 215), Label, Zoomed, 4));

    [Fact]
    public void PlaceLabel_AtTheRightOfAZoomedView_StaysInsideTheView()
        => Assert.Equal(new Point(370, 230),
            CropLayoutManager.PlaceLabel(new Point(397.5, 260), Label, Zoomed, 4));

    [Fact]
    public void DefaultSelection_OnASmallVisiblePart_IsHalfOfItInItsCentre()
        => Assert.Equal(new Rect(175, 100, 150, 100), CropLayoutManager.DefaultSelection(new Rect(100, 50, 300, 200)));
}
