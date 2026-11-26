using PicView.Core.Gallery;

namespace PicView.Tests.Gallery;

public class GalleryAppearanceTests
{
    private static readonly GalleryAppearanceInputs Inputs = new(
        DockedItemSize: 80,
        ExpandedItemSize: 150,
        DockedStretch: GalleryStretchMode.Uniform,
        ExpandedStretch: GalleryStretchMode.UniformToFill,
        ItemSpacing: 4);

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

        // Top/bottom docks scroll horizontally (22px scrollbar), left/right docks vertically (30px scrollbar)
        Assert.Equal(itemSize + 2 + 22,
            GalleryAppearance.For(GalleryMode.Docked, GalleryDockPosition.Top, inputs).Extent);
        Assert.Equal(itemSize + 1 + 30,
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
            Assert.False(a.ScrollsHorizontally);
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

    private static readonly GalleryStretchMode[] Stretches =
    [
        GalleryStretchMode.Uniform, GalleryStretchMode.UniformToFill, GalleryStretchMode.Square,
        GalleryStretchMode.FillSquare
    ];

    [Theory]
    [InlineData(GalleryDisplayMode.Masonry)]
    [InlineData(GalleryDisplayMode.Rows)]
    public void For_ExpandedTiles_ScrollsVerticallyAndFillsWhateverTheStretch(GalleryDisplayMode displayMode)
    {
        var grid = GalleryAppearance.For(GalleryMode.Expanded, GalleryDockPosition.Bottom, Inputs);
        foreach (var stretch in Stretches)
        {
            var a = GalleryAppearance.For(GalleryMode.Expanded, GalleryDockPosition.Bottom,
                Inputs with { ExpandedStretch = stretch, ExpandedDisplayMode = displayMode });

            Assert.Equal(displayMode, a.DisplayMode);
            Assert.False(a.ScrollsHorizontally);
            Assert.True(double.IsNaN(a.ItemWidth));
            Assert.Equal(GalleryStretchMode.UniformToFill, a.Stretch);
            // Everything else is the Grid record: item height 150, margin (15, 40, 15, 5), wraps
            Assert.Equal(grid, a with { DisplayMode = GalleryDisplayMode.Grid });
        }
    }

    [Theory]
    [InlineData(GalleryMode.Docked)]
    [InlineData(GalleryMode.Closed)]
    public void For_DockedOrClosed_IsTheSameRecordInEveryDisplayMode(GalleryMode mode)
    {
        foreach (var dock in Edges)
        {
            var grid = GalleryAppearance.For(mode, dock, Inputs);

            Assert.Equal(GalleryDisplayMode.Grid, grid.DisplayMode);
            Assert.Equal(grid, GalleryAppearance.For(mode, dock, Inputs with { ExpandedDisplayMode = GalleryDisplayMode.Masonry }));
            Assert.Equal(grid, GalleryAppearance.For(mode, dock, Inputs with { ExpandedDisplayMode = GalleryDisplayMode.Rows }));
        }
    }

    [Fact]
    public void For_ExpandedGridOrUnknownDisplayMode_KeepsTheSavedStretchAndScrollsVertically()
    {
        var inputs = Inputs with { ExpandedStretch = GalleryStretchMode.FillSquare };

        var a = GalleryAppearance.For(GalleryMode.Expanded, GalleryDockPosition.Bottom, inputs);

        Assert.Equal(GalleryDisplayMode.Grid, a.DisplayMode);
        Assert.False(a.ScrollsHorizontally);
        Assert.Equal(GalleryOrientation.Vertical, a.Orientation);
        Assert.Equal(a.ItemHeight, a.ItemWidth);
        Assert.Equal(GalleryStretchMode.FillSquare, a.Stretch);
        Assert.Equal(a, GalleryAppearance.For(GalleryMode.Expanded, GalleryDockPosition.Bottom,
            inputs with { ExpandedDisplayMode = (GalleryDisplayMode)7 }));
    }
}
