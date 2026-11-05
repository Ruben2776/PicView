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

    [Theory]
    [InlineData(GalleryDockPosition.Bottom)]
    [InlineData(GalleryDockPosition.Top)]
    [InlineData(GalleryDockPosition.Left)]
    [InlineData(GalleryDockPosition.Right)]
    public void ReservedBy_ExpandedThatReturnsToDocked_IsTheDockedThicknessItReturnsTo(GalleryDockPosition dock)
    {
        var inputs = new GalleryAppearanceInputs(100, 220, GalleryStretchMode.Uniform, GalleryStretchMode.Uniform, 0);
        var docked = GalleryAppearance.For(GalleryMode.Docked, dock, inputs);
        var expanded = GalleryAppearance.For(GalleryMode.Expanded, dock, inputs);

        Assert.Equal(GalleryDocking.ReservedBy(docked), GalleryDocking.ReservedBy(expanded, returnsToDocked: true));
        Assert.NotEqual((0d, 0d), GalleryDocking.ReservedBy(expanded, returnsToDocked: true));
    }

    [Fact]
    public void ReservedBy_ExpandedThatReturnsToClosed_IsNothing()
    {
        var inputs = new GalleryAppearanceInputs(100, 220, GalleryStretchMode.Uniform, GalleryStretchMode.Uniform, 0);
        var expanded = GalleryAppearance.For(GalleryMode.Expanded, GalleryDockPosition.Bottom, inputs);

        Assert.Equal((0d, 0d), GalleryDocking.ReservedBy(expanded, returnsToDocked: false));
    }

    [Fact]
    public void ReservedBy_ClosedThatWouldReturnToDocked_IsNothing()
    {
        var inputs = new GalleryAppearanceInputs(100, 220, GalleryStretchMode.Uniform, GalleryStretchMode.Uniform, 0);
        var closed = GalleryAppearance.For(GalleryMode.Closed, GalleryDockPosition.Bottom, inputs);

        Assert.Equal((0d, 0d), GalleryDocking.ReservedBy(closed, returnsToDocked: true));
    }
}
