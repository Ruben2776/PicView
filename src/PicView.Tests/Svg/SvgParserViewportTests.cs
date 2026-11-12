using PicView.Core.Svg;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Svg;

public class SvgParserViewportTests
{
    private static SvgDrawing Viewport(string rootAttributes)
    {
        return Drawing(Document("", rootAttributes));
    }

    [Fact]
    public void Parse_SizeAndViewBox_ScalesRoot()
    {
        var drawing = Viewport("width='48' height='48' viewBox='0 0 24 24'");
        Assert.Equal(48, drawing.Width);
        Assert.Equal(48, drawing.Height);
        Assert.Equal(new SvgMatrix(2, 0, 0, 2, 0, 0), drawing.Root.Transform);
    }

    [Fact]
    public void Parse_ViewBoxOnly_TakesViewBoxSize()
    {
        var drawing = Viewport("viewBox='0 0 24 12'");
        Assert.Equal(24, drawing.Width);
        Assert.Equal(12, drawing.Height);
        Assert.Null(drawing.Root.Transform);
    }

    [Fact]
    public void Parse_WidthAndViewBox_DerivesHeightFromAspectRatio()
    {
        var drawing = Viewport("width='48' viewBox='0 0 24 12'");
        Assert.Equal(48, drawing.Width);
        Assert.Equal(24, drawing.Height);
    }

    [Fact]
    public void Parse_HeightAndViewBox_DerivesWidthFromAspectRatio()
    {
        var drawing = Viewport("height='24' viewBox='0 0 24 12'");
        Assert.Equal(48, drawing.Width);
        Assert.Equal(24, drawing.Height);
    }

    [Fact]
    public void Parse_PercentWidthAndViewBox_DerivesWidthFromHeight()
    {
        var drawing = Viewport("width='100%' height='24' viewBox='0 0 24 12'");
        Assert.Equal(48, drawing.Width);
        Assert.Equal(24, drawing.Height);
    }

    [Fact]
    public void Parse_BothPercentAndViewBox_TakesViewBoxSize()
    {
        var drawing = Viewport("width='100%' height='100%' viewBox='0 0 24 12'");
        Assert.Equal(24, drawing.Width);
        Assert.Equal(12, drawing.Height);
    }

    [Fact]
    public void Parse_NoViewBox_HasNoRootTransform()
    {
        var drawing = Viewport("width='10' height='20'");
        Assert.Equal(10, drawing.Width);
        Assert.Equal(20, drawing.Height);
        Assert.Null(drawing.Root.Transform);
        Assert.Equal(1, drawing.Root.Opacity);
        Assert.Null(drawing.Root.Clip);
    }

    [Fact]
    public void Parse_ViewBoxOrigin_TranslatesRoot()
    {
        var drawing = Viewport("width='10' height='10' viewBox='10 20 10 10'");
        Assert.Equal(new SvgMatrix(1, 0, 0, 1, -10, -20), drawing.Root.Transform);
    }

    [Fact]
    public void Parse_DefaultPreserveAspectRatio_CentersAndMeets()
    {
        var drawing = Viewport("width='100' height='50' viewBox='0 0 10 10'");
        Assert.Equal(new SvgMatrix(5, 0, 0, 5, 25, 0), drawing.Root.Transform);
    }

    [Fact]
    public void Parse_PreserveAspectRatioNone_ScalesEachAxis()
    {
        var drawing = Viewport("width='100' height='50' viewBox='0 0 10 10' preserveAspectRatio='none'");
        Assert.Equal(new SvgMatrix(10, 0, 0, 5, 0, 0), drawing.Root.Transform);
    }

    [Fact]
    public void Parse_PreserveAspectRatioSlice_ScalesToCover()
    {
        var drawing = Viewport("width='100' height='50' viewBox='0 0 10 10' preserveAspectRatio='xMinYMin slice'");
        Assert.Equal(new SvgMatrix(10, 0, 0, 10, 0, 0), drawing.Root.Transform);
    }

    [Theory]
    [InlineData("xMaxYMax meet", 50, 0)]
    [InlineData("xMidYMax slice", 0, -50)]
    [InlineData("xMinYMid slice", 0, -25)]
    public void Parse_PreserveAspectRatioAlign_OffsetsContent(string value, double x, double y)
    {
        var drawing = Viewport($"width='100' height='50' viewBox='0 0 10 10' preserveAspectRatio='{value}'");
        Assert.NotNull(drawing.Root.Transform);
        Assert.Equal(x, drawing.Root.Transform.Value.OffsetX);
        Assert.Equal(y, drawing.Root.Transform.Value.OffsetY);
    }

    [Theory]
    [InlineData("width='10'")]
    [InlineData("width='100%' height='10'")]
    [InlineData("width='10' height='10' viewBox='0 0 0 10'")]
    [InlineData("width='-1' height='10'")]
    public void Parse_UnusableSize_IsSize(string rootAttributes)
    {
        Assert.Equal("size", Reason(Document("", rootAttributes)));
    }

    [Theory]
    [InlineData("width='10' height='10' viewBox='0 0 10'", "value:viewBox")]
    [InlineData("width='10' height='10' viewBox='0 0 10 10' preserveAspectRatio='middle'", "value:preserveAspectRatio")]
    [InlineData("width='10' height='10' viewBox='0 0 10 10' preserveAspectRatio='xminymin'", "value:preserveAspectRatio")]
    [InlineData("width='10' height='10' viewBox='0 0 10 10' preserveAspectRatio='None'", "value:preserveAspectRatio")]
    [InlineData("width='10' height='10' viewBox='0 0 10 10' preserveAspectRatio='xMidYMid Slice'", "value:preserveAspectRatio")]
    public void Parse_InvalidViewportAttribute_GivesReason(string rootAttributes, string reason)
    {
        Assert.Equal(reason, Reason(Document("", rootAttributes)));
    }
}
