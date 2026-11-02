using PicView.Core.Gallery;

namespace PicView.Tests.Gallery;

public class GalleryAppearanceTests
{
    private static readonly GalleryAppearanceInputs Inputs = new(
        DockedItemSize: 80,
        ExpandedItemSize: 150,
        DockedStretch: GalleryStretchMode.Uniform,
        ExpandedStretch: GalleryStretchMode.UniformToFill,
        ItemSpacing: 4,
        ShowBottomNavBar: false,
        ShowHoverNavigationBar: true);

    private static readonly GalleryDockPosition[] Edges =
    [
        GalleryDockPosition.Top, GalleryDockPosition.Bottom, GalleryDockPosition.Left, GalleryDockPosition.Right
    ];

    [Theory]
    [InlineData(GalleryDockPosition.Top, GalleryOrientation.Horizontal, true, 0, 0, 0, 1)]
    [InlineData(GalleryDockPosition.Bottom, GalleryOrientation.Horizontal, true, 0, 1, 0, 0)]
    [InlineData(GalleryDockPosition.Left, GalleryOrientation.Vertical, false, 0, 0, 1, 0)]
    [InlineData(GalleryDockPosition.Right, GalleryOrientation.Vertical, false, 1, 0, 0, 0)]
    public void For_Docked_HasTheDockEdgesLook(GalleryDockPosition dock, GalleryOrientation orientation,
        bool scrollsHorizontally, double left, double top, double right, double bottom)
    {
        var a = GalleryAppearance.For(GalleryMode.Docked, dock, Inputs);

        Assert.True(a.IsShown);
        Assert.Equal(orientation, a.Orientation);
        Assert.Equal(scrollsHorizontally, a.ScrollsHorizontally);
        Assert.Equal(new GalleryInsets(left, top, right, bottom), a.Border);
        Assert.False(a.Wraps);
        Assert.Equal(80, a.ItemHeight);
        Assert.Equal(0, a.ItemSpacing);
        Assert.Equal(GalleryInsets.Zero, a.Margin);
    }

    [Theory]
    [InlineData(80)]
    [InlineData(120)]
    public void For_Docked_ExtentIsTheDefaultsFormula(double itemSize)
    {
        var inputs = Inputs with { DockedItemSize = itemSize };
        foreach (var dock in Edges)
        {
            var a = GalleryAppearance.For(GalleryMode.Docked, dock, inputs);
            var expected = dock is GalleryDockPosition.Top or GalleryDockPosition.Bottom
                ? GalleryDefaults.GetDockedGalleryHeight(itemSize)
                : GalleryDefaults.GetDockedGalleryWidth(itemSize);
            Assert.Equal(expected, a.Extent);
        }

        Assert.Equal(itemSize + 1 + 30,
            GalleryAppearance.For(GalleryMode.Docked, GalleryDockPosition.Top, inputs).Extent);
        Assert.Equal(itemSize + 2 + 22,
            GalleryAppearance.For(GalleryMode.Docked, GalleryDockPosition.Left, inputs).Extent);
    }

    [Fact]
    public void For_Expanded_FillsTheParent()
    {
        foreach (var dock in Edges)
        {
            var a = GalleryAppearance.For(GalleryMode.Expanded, dock, Inputs);

            Assert.True(a.IsShown);
            Assert.Equal(GalleryOrientation.Vertical, a.Orientation);
            Assert.True(a.ScrollsHorizontally);
            Assert.True(a.Wraps);
            Assert.Equal(150, a.ItemHeight);
            Assert.Equal(4, a.ItemSpacing);
            Assert.Equal(new GalleryInsets(15, 40, 15, 5), a.Margin);
            Assert.Equal(GalleryInsets.Zero, a.Border);
            Assert.True(double.IsNaN(a.Extent));
            Assert.Equal(GalleryStretchMode.UniformToFill, a.Stretch);
        }
    }

    [Fact]
    public void For_Closed_IsHidden()
    {
        foreach (var dock in Edges)
        {
            var a = GalleryAppearance.For(GalleryMode.Closed, dock, Inputs);

            Assert.False(a.IsShown);
            Assert.Equal(0, a.Extent);
            Assert.False(a.ShowsHoverBar);
        }
    }

    [Theory]
    [InlineData(GalleryStretchMode.Uniform, GalleryMode.Docked, false)]
    [InlineData(GalleryStretchMode.UniformToFill, GalleryMode.Docked, false)]
    [InlineData(GalleryStretchMode.Square, GalleryMode.Docked, true)]
    [InlineData(GalleryStretchMode.FillSquare, GalleryMode.Docked, true)]
    [InlineData(GalleryStretchMode.Uniform, GalleryMode.Expanded, false)]
    [InlineData(GalleryStretchMode.UniformToFill, GalleryMode.Expanded, false)]
    [InlineData(GalleryStretchMode.Square, GalleryMode.Expanded, true)]
    [InlineData(GalleryStretchMode.FillSquare, GalleryMode.Expanded, true)]
    public void For_SquareStretches_MakeItemsSquare(GalleryStretchMode stretch, GalleryMode mode, bool square)
    {
        var inputs = Inputs with { DockedStretch = stretch, ExpandedStretch = stretch };

        var a = GalleryAppearance.For(mode, GalleryDockPosition.Bottom, inputs);

        if (square)
        {
            Assert.Equal(a.ItemHeight, a.ItemWidth);
        }
        else
        {
            Assert.True(double.IsNaN(a.ItemWidth));
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void For_Docked_ShowsHoverBarFromTheUISettings(bool showBottomNavBar, bool showHoverNavigationBar)
    {
        var inputs = Inputs with { ShowBottomNavBar = showBottomNavBar, ShowHoverNavigationBar = showHoverNavigationBar };

        var docked = GalleryAppearance.For(GalleryMode.Docked, GalleryDockPosition.Bottom, inputs);
        var expanded = GalleryAppearance.For(GalleryMode.Expanded, GalleryDockPosition.Bottom, inputs);

        Assert.Equal(!showBottomNavBar && showHoverNavigationBar, docked.ShowsHoverBar);
        Assert.False(expanded.ShowsHoverBar);
    }
}
