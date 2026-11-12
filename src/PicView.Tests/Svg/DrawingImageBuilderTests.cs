using Avalonia;
using Avalonia.Media;
using PicView.Avalonia.Svg;
using PicView.Core.Svg;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Svg;

[Collection("Sequential")]
public class DrawingImageBuilderTests
{
    private static DrawingImage Build(string svgText)
    {
        return DrawingImageBuilder.Build(Drawing(svgText));
    }

    private static DrawingGroup RootGroup(DrawingImage image)
    {
        return Assert.IsType<DrawingGroup>(image.Drawing);
    }

    [AvaloniaFact]
    public void Build_EmptySvg_HasViewportSizeAndViewbox()
    {
        var image = Build($"<svg xmlns='{Namespace}' width='40' height='20'/>");
        Assert.Equal(new Size(40, 20), image.Size);
        Assert.Equal(new Rect(0, 0, 40, 20), image.Viewbox);
    }

    [AvaloniaFact]
    public void Build_ShapeOffCanvas_LeavesSizeAtViewport()
    {
        var image = Build(Document("<rect x='-50' y='10' width='20' height='20'/>", "width='40' height='20'"));
        Assert.Equal(new Size(40, 20), image.Size);
    }

    [AvaloniaFact]
    public void Build_Group_HasOpacityMaskAndTransform()
    {
        var model = Drawing(Document("<g opacity='.5' transform='matrix(1 2 3 4 5 6)'><rect width='1' height='1'/></g>"));
        var modelGroup = Assert.IsType<SvgGroup>(Assert.Single(model.Root.Children));

        var group = Assert.IsType<DrawingGroup>(Assert.Single(RootGroup(DrawingImageBuilder.Build(model)).Children));

        // Group opacity is an opacity mask, so the group is composited as one layer.
        Assert.Equal(1, group.Opacity);
        var mask = Assert.IsType<SolidColorBrush>(group.OpacityMask);
        Assert.Equal(Colors.White, mask.Color);
        Assert.Equal(0.5, mask.Opacity);
        var m = modelGroup.Transform!.Value;
        Assert.Equal(new Matrix(m.M11, m.M12, m.M21, m.M22, m.OffsetX, m.OffsetY), group.Transform!.Value);
        Assert.Equal(new Matrix(1, 2, 3, 4, 5, 6), group.Transform.Value);
    }

    [AvaloniaFact]
    public void Build_Stroke_MapsPen()
    {
        var image = Build(Document(
            "<path d='M0,0 L10,0' fill='none' stroke='#000' stroke-width='2' stroke-linecap='round' " +
            "stroke-dasharray='4 2'/>"));
        var shape = Assert.IsType<GeometryDrawing>(Assert.Single(RootGroup(image).Children));
        var pen = Assert.IsType<Pen>(shape.Pen);

        Assert.Equal(2, pen.Thickness);
        Assert.Equal(PenLineCap.Round, pen.LineCap);
        Assert.Equal(4, pen.MiterLimit);
        Assert.Equal([2d, 1d], pen.DashStyle!.Dashes!);
        Assert.Null(shape.Brush);
    }
}
