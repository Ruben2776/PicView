using System.Xml.Linq;
using PicView.Core.Svg;

namespace PicView.Tests.Svg;

public class AxamlDrawingWriterTests
{
    private static readonly SvgColor Red = new(0xFF, 0xFF, 0, 0);
    private static readonly SvgColor Blue = new(0x80, 0, 0, 0xFF);
    private static readonly SvgGradientStop[] Stops = [new(0, Red), new(1, Blue)];

    private static SvgDrawing Drawing(params SvgNode[] children)
    {
        return new SvgDrawing(10, 20, new SvgGroup(null, 1, null, children));
    }

    private static SvgStroke Stroke(
        SvgPaint? paint = null,
        double width = 1,
        SvgLineCap cap = SvgLineCap.Butt,
        SvgLineJoin join = SvgLineJoin.Miter,
        double miterLimit = 10,
        double[]? dashes = null,
        double dashOffset = 0)
    {
        return new SvgStroke(paint ?? new SvgSolidPaint(Red), width, cap, join, miterLimit, dashes, dashOffset);
    }

    private static string Written(SvgStroke stroke)
    {
        return AxamlDrawingWriter.Write(Drawing(new SvgShape("M0,0 L1,1", null, stroke)));
    }

    private static string Lines(params string[] lines)
    {
        return string.Join('\n', lines);
    }

    [Fact]
    public void Write_EmptyRootGroup_IsSelfClosing()
    {
        var expected = Lines(
            "<DrawingImage Viewbox=\"0 0 10 20\">",
            "  <DrawingImage.Drawing>",
            "    <DrawingGroup />",
            "  </DrawingImage.Drawing>",
            "</DrawingImage>");
        Assert.Equal(expected, AxamlDrawingWriter.Write(Drawing()));
    }

    [Fact]
    public void Write_ShapeWithoutFillOrStroke_IsStillWritten()
    {
        var text = AxamlDrawingWriter.Write(Drawing(new SvgShape("M0,0 L1,1", null, null)));
        Assert.Contains("    <DrawingGroup>\n      <GeometryDrawing Geometry=\"M0,0 L1,1\" />\n    </DrawingGroup>", text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Write_PenAtDefaults_LeavesOutEveryDefault()
    {
        Assert.Contains("<Pen Brush=\"#FFFF0000\" />", Written(Stroke()), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(SvgLineJoin.Round, 4, "<Pen Brush=\"#FFFF0000\" LineJoin=\"Round\" />")]
    [InlineData(SvgLineJoin.Bevel, 4, "<Pen Brush=\"#FFFF0000\" LineJoin=\"Bevel\" />")]
    [InlineData(SvgLineJoin.Miter, 4, "<Pen Brush=\"#FFFF0000\" MiterLimit=\"4\" />")]
    [InlineData(SvgLineJoin.Miter, 2.5, "<Pen Brush=\"#FFFF0000\" MiterLimit=\"2.5\" />")]
    [InlineData(SvgLineJoin.Miter, 10, "<Pen Brush=\"#FFFF0000\" />")]
    public void Write_LineJoin_WritesMiterLimitOnlyForMiterAwayFromTen(SvgLineJoin join, double limit, string pen)
    {
        Assert.Contains(pen, Written(Stroke(join: join, miterLimit: limit)), StringComparison.Ordinal);
    }

    [Fact]
    public void Write_SquareCapAndWidth_WritesThicknessThenCap()
    {
        Assert.Contains("<Pen Brush=\"#FFFF0000\" Thickness=\"3.5\" LineCap=\"Square\" />",
            Written(Stroke(width: 3.5, cap: SvgLineCap.Square)), StringComparison.Ordinal);
    }

    [Fact]
    public void Write_DashesWithOffset_AreDividedByWidth()
    {
        var text = Written(Stroke(width: 4, dashes: [8, 2, 4, 2], dashOffset: 6));
        Assert.Contains("<DashStyle Dashes=\"2,0.5,1,0.5\" Offset=\"1.5\" />", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_GradientStrokeWithDashes_WritesBrushBeforeDashStyle()
    {
        var gradient = new SvgLinearGradientPaint(0, 0, 1, 1, Stops, SvgGradientUnits.UserSpaceOnUse, null,
            SvgSpreadMethod.Pad, 1);
        var expected = Lines(
            "<DrawingImage Viewbox=\"0 0 10 20\">",
            "  <DrawingImage.Drawing>",
            "    <DrawingGroup>",
            "      <GeometryDrawing Geometry=\"M0,0 L1,1\">",
            "        <GeometryDrawing.Pen>",
            "          <Pen Thickness=\"2\">",
            "            <Pen.Brush>",
            "              <LinearGradientBrush StartPoint=\"0,0\" EndPoint=\"1,1\">",
            "                <GradientStop Offset=\"0\" Color=\"#FFFF0000\" />",
            "                <GradientStop Offset=\"1\" Color=\"#800000FF\" />",
            "              </LinearGradientBrush>",
            "            </Pen.Brush>",
            "            <Pen.DashStyle>",
            "              <DashStyle Dashes=\"1,1\" />",
            "            </Pen.DashStyle>",
            "          </Pen>",
            "        </GeometryDrawing.Pen>",
            "      </GeometryDrawing>",
            "    </DrawingGroup>",
            "  </DrawingImage.Drawing>",
            "</DrawingImage>");
        Assert.Equal(expected, Written(Stroke(gradient, 2, dashes: [2, 2])));
    }

    [Fact]
    public void Write_FillAndGradientStroke_WritesBrushElementBeforePenElement()
    {
        var gradient = new SvgLinearGradientPaint(1, 2, 3, 4, Stops, SvgGradientUnits.UserSpaceOnUse, null,
            SvgSpreadMethod.Repeat, 0.5);
        var shape = new SvgShape("F1 M0,0 L1,1", gradient, Stroke(gradient));
        var text = AxamlDrawingWriter.Write(Drawing(shape));
        Assert.True(text.IndexOf("<GeometryDrawing.Brush>", StringComparison.Ordinal) <
                    text.IndexOf("<GeometryDrawing.Pen>", StringComparison.Ordinal));
        Assert.Contains("<LinearGradientBrush StartPoint=\"1,2\" EndPoint=\"3,4\" SpreadMethod=\"Repeat\" Opacity=\"0.5\">",
            text, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_RadialGradientInBoundingBox_WritesPercentRadii()
    {
        var gradient = new SvgRadialGradientPaint(0.5, 0.5, 0.5, 0.25, 0.75, Stops, SvgGradientUnits.ObjectBoundingBox,
            null, SvgSpreadMethod.Pad, 1);
        var text = AxamlDrawingWriter.Write(Drawing(new SvgShape("M0,0 L1,1", gradient, null)));
        Assert.Contains(
            "<RadialGradientBrush Center=\"50%,50%\" GradientOrigin=\"25%,75%\" RadiusX=\"50%\" RadiusY=\"50%\">", text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Write_ClipShapeWithTransform_WrapsInGeometryGroupWithFillRuleOnlyForNonZero()
    {
        var clip = new SvgClip([
            new SvgClipShape("F1 M0,0 H1 V1 Z", new SvgMatrix(2, 0, 0, 2, 0, 0)),
            new SvgClipShape("M0,0 H1 V1 Z", new SvgMatrix(1, 0, 0, 1, 3, 4))
        ]);
        var text = AxamlDrawingWriter.Write(Drawing(new SvgGroup(null, 1, clip, [new SvgShape("M0,0", null, null)])));
        Assert.Contains("<GeometryGroup FillRule=\"NonZero\">", text, StringComparison.Ordinal);
        Assert.Contains("<GeometryGroup>", text, StringComparison.Ordinal);
        Assert.Contains("<MatrixTransform Matrix=\"2,0,0,2,0,0\" />", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_ThreeClipShapes_AreABalancedTreeWithTheLargerHalfFirst()
    {
        var clip = new SvgClip([new SvgClipShape("A", null), new SvgClipShape("B", null), new SvgClipShape("C", null)]);
        var text = AxamlDrawingWriter.Write(Drawing(new SvgGroup(null, 1, clip, [])));
        var expected = Lines(
            "<DrawingImage Viewbox=\"0 0 10 20\">",
            "  <DrawingImage.Drawing>",
            "    <DrawingGroup>",
            "      <DrawingGroup>",
            "        <DrawingGroup.ClipGeometry>",
            "          <CombinedGeometry GeometryCombineMode=\"Union\">",
            "            <CombinedGeometry.Geometry1>",
            "              <CombinedGeometry GeometryCombineMode=\"Union\">",
            "                <CombinedGeometry.Geometry1>",
            "                  <StreamGeometry>A</StreamGeometry>",
            "                </CombinedGeometry.Geometry1>",
            "                <CombinedGeometry.Geometry2>",
            "                  <StreamGeometry>B</StreamGeometry>",
            "                </CombinedGeometry.Geometry2>",
            "              </CombinedGeometry>",
            "            </CombinedGeometry.Geometry1>",
            "            <CombinedGeometry.Geometry2>",
            "              <StreamGeometry>C</StreamGeometry>",
            "            </CombinedGeometry.Geometry2>",
            "          </CombinedGeometry>",
            "        </DrawingGroup.ClipGeometry>",
            "      </DrawingGroup>",
            "    </DrawingGroup>",
            "  </DrawingImage.Drawing>",
            "</DrawingImage>");
        Assert.Equal(expected, text);
    }

    [Fact]
    public void Write_FourClipShapes_AreTwoUnionsOfTwo()
    {
        var clip = new SvgClip([
            new SvgClipShape("A", null), new SvgClipShape("B", null), new SvgClipShape("C", null),
            new SvgClipShape("D", null)
        ]);
        var text = AxamlDrawingWriter.Write(Drawing(new SvgGroup(null, 1, clip, [])));
        var expected = Lines(
            "<DrawingImage Viewbox=\"0 0 10 20\">",
            "  <DrawingImage.Drawing>",
            "    <DrawingGroup>",
            "      <DrawingGroup>",
            "        <DrawingGroup.ClipGeometry>",
            "          <CombinedGeometry GeometryCombineMode=\"Union\">",
            "            <CombinedGeometry.Geometry1>",
            "              <CombinedGeometry GeometryCombineMode=\"Union\">",
            "                <CombinedGeometry.Geometry1>",
            "                  <StreamGeometry>A</StreamGeometry>",
            "                </CombinedGeometry.Geometry1>",
            "                <CombinedGeometry.Geometry2>",
            "                  <StreamGeometry>B</StreamGeometry>",
            "                </CombinedGeometry.Geometry2>",
            "              </CombinedGeometry>",
            "            </CombinedGeometry.Geometry1>",
            "            <CombinedGeometry.Geometry2>",
            "              <CombinedGeometry GeometryCombineMode=\"Union\">",
            "                <CombinedGeometry.Geometry1>",
            "                  <StreamGeometry>C</StreamGeometry>",
            "                </CombinedGeometry.Geometry1>",
            "                <CombinedGeometry.Geometry2>",
            "                  <StreamGeometry>D</StreamGeometry>",
            "                </CombinedGeometry.Geometry2>",
            "              </CombinedGeometry>",
            "            </CombinedGeometry.Geometry2>",
            "          </CombinedGeometry>",
            "        </DrawingGroup.ClipGeometry>",
            "      </DrawingGroup>",
            "    </DrawingGroup>",
            "  </DrawingImage.Drawing>",
            "</DrawingImage>");
        Assert.Equal(expected, text);
    }

    [Fact]
    public void Write_ThousandClipShapes_NestsOnlyTenUnionsDeep()
    {
        var shapes = new SvgClipShape[1000];
        for (var i = 0; i < shapes.Length; i++)
        {
            shapes[i] = new SvgClipShape("S" + i, null);
        }

        var text = AxamlDrawingWriter.Write(Drawing(new SvgGroup(null, 1, new SvgClip(shapes), [])));
        var document = XDocument.Parse(text);
        var geometries = document.Descendants("StreamGeometry").ToList();
        Assert.Equal(shapes.Select(s => s.PathData), geometries.Select(g => g.Value), StringComparer.Ordinal);
        Assert.Equal(10, geometries.Max(g => g.Ancestors("CombinedGeometry").Count()));
    }

    [Fact]
    public void Write_ReservedCharactersInPathData_AreEscaped()
    {
        var shape = new SvgShape("M0,0 & <\">", new SvgSolidPaint(Red), null);
        var clip = new SvgClip([new SvgClipShape("a&b<c", null)]);
        var text = AxamlDrawingWriter.Write(Drawing(new SvgGroup(null, 1, clip, [shape])));
        Assert.Contains("Geometry=\"M0,0 &amp; &lt;&quot;&gt;\"", text, StringComparison.Ordinal);
        Assert.Contains("<StreamGeometry>a&amp;b&lt;c</StreamGeometry>", text, StringComparison.Ordinal);
        var document = XDocument.Parse(text);
        Assert.Equal("M0,0 & <\">",
            document.Descendants("GeometryDrawing").Single().Attribute("Geometry")!.Value);
    }

    [Fact]
    public void Write_Output_HasNoTrailingNewlineOrCarriageReturn()
    {
        var text = Written(Stroke());
        Assert.False(text.EndsWith('\n'));
        Assert.DoesNotContain('\r', text);
    }
}
