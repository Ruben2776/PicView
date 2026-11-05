using PicView.Core.Gallery;

namespace PicView.Tests.Gallery;

public class GalleryDockingTests
{
    [Theory]
    [InlineData(GalleryMode.Docked, GalleryDockPosition.Bottom, true, 100.0, 0.0, 100.0)]
    [InlineData(GalleryMode.Docked, GalleryDockPosition.Top, true, 100.0, 0.0, 100.0)]
    [InlineData(GalleryMode.Docked, GalleryDockPosition.Left, true, 120.0, 120.0, 0.0)]
    [InlineData(GalleryMode.Docked, GalleryDockPosition.Right, true, 120.0, 120.0, 0.0)]
    [InlineData(GalleryMode.Docked, GalleryDockPosition.Top, false, 100.0, 0.0, 0.0)]
    [InlineData(GalleryMode.Expanded, GalleryDockPosition.Bottom, true, double.NaN, 0.0, 0.0)]
    [InlineData(GalleryMode.Closed, GalleryDockPosition.Bottom, false, 0.0, 0.0, 0.0)]
    public void ReservedBy_Appearance_IsTheDockedThicknessOnItsAxis(GalleryMode mode, GalleryDockPosition dock,
        bool isShown, double extent, double expectedWidth, double expectedHeight)
    {
        var appearance = GalleryAppearance.For(GalleryMode.Closed, dock,
            new GalleryAppearanceInputs(100, 100, GalleryStretchMode.Uniform, GalleryStretchMode.Uniform, 0)) with
        {
            Mode = mode, IsShown = isShown, Extent = extent
        };

        var (width, height) = GalleryDocking.ReservedBy(appearance);

        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
    }
}
