using System.Globalization;
using System.Xml.Linq;
using PicView.Core.Svg;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Svg;

public class SvgToXamlConverterTests
{
    private const string BlackToWhite =
        "<stop offset='0' stop-color='#000'/><stop offset='1' stop-color='#fff' stop-opacity='.5'/>";

    internal const string FillAndStrokeSvg =
        "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24'>" +
        "<path fill='#1E88E5' d='M4,4 H20 V20 H4 Z'/>" +
        "<path fill='none' fill-rule='evenodd' stroke='#000' stroke-width='2' stroke-linecap='round' d='M2,12 L22,12'/></svg>";

    internal const string GroupSvg =
        "<g transform='translate(5 6)' opacity='.5'><rect width='10' height='10'/></g>";

    internal const string SolidAlphaSvg = "<rect width='10' height='10' fill='#FF0000' fill-opacity='.5'/>";

    internal const string LinearSvg =
        "<defs><linearGradient id='g'>" + BlackToWhite + "</linearGradient></defs>" +
        "<rect width='10' height='10' fill='url(#g)'/>";

    internal const string RadialSvg =
        "<defs><radialGradient id='g' gradientUnits='userSpaceOnUse' cx='50' cy='50' r='40' fx='45' fy='55' " +
        "spreadMethod='reflect' gradientTransform='translate(2 3)'>" + BlackToWhite + "</radialGradient></defs>" +
        "<rect width='10' height='10' fill='url(#g)'/>";

    internal const string DashSvg =
        "<path d='M0,0 L10,0' fill='none' stroke='#000' stroke-width='2' stroke-dasharray='4 2'/>";

    internal const string MiterSvg = "<path d='M0,0 L10,0 L10,10' fill='none' stroke='#000'/>";

    internal const string OneClipSvg =
        "<defs><clipPath id='c'><rect width='5' height='5'/></clipPath></defs>" +
        "<g clip-path='url(#c)'><rect width='10' height='10'/></g>";

    internal const string TwoClipSvg =
        "<defs><clipPath id='c'><rect width='5' height='5'/>" +
        "<rect width='2' height='2' transform='translate(1 1)' clip-rule='evenodd'/></clipPath></defs>" +
        "<g clip-path='url(#c)'><rect width='10' height='10'/></g>";

    private static string Convert(string body)
    {
        var text = SvgToXamlConverter.Convert(Document(body), out var reason);
        Assert.Null(reason);
        Assert.NotNull(text);
        return text;
    }

    [Fact]
    public void Convert_FillAndStroke_WritesDrawingImage()
    {
        const string expected =
            "<DrawingImage Viewbox=\"0 0 24 24\">\n" +
            "  <DrawingImage.Drawing>\n" +
            "    <DrawingGroup>\n" +
            "      <GeometryDrawing Brush=\"#FF1E88E5\" Geometry=\"F1 M4,4 H20 V20 H4 Z\" />\n" +
            "      <GeometryDrawing Geometry=\"M2,12 L22,12\">\n" +
            "        <GeometryDrawing.Pen>\n" +
            "          <Pen Brush=\"#FF000000\" Thickness=\"2\" LineCap=\"Round\" MiterLimit=\"4\" />\n" +
            "        </GeometryDrawing.Pen>\n" +
            "      </GeometryDrawing>\n" +
            "    </DrawingGroup>\n" +
            "  </DrawingImage.Drawing>\n" +
            "</DrawingImage>";
        Assert.Equal(expected, SvgToXamlConverter.Convert(FillAndStrokeSvg, out _));
    }

    [Fact]
    public void Convert_GroupWithTransformAndOpacity_WritesPropertyElements()
    {
        const string expected =
            "<DrawingImage Viewbox=\"0 0 100 100\">\n" +
            "  <DrawingImage.Drawing>\n" +
            "    <DrawingGroup>\n" +
            "      <DrawingGroup>\n" +
            "        <DrawingGroup.Transform>\n" +
            "          <MatrixTransform Matrix=\"1,0,0,1,5,6\" />\n" +
            "        </DrawingGroup.Transform>\n" +
            "        <DrawingGroup.OpacityMask>\n" +
            "          <SolidColorBrush Color=\"#FFFFFFFF\" Opacity=\"0.5\" />\n" +
            "        </DrawingGroup.OpacityMask>\n" +
            "        <GeometryDrawing Brush=\"#FF000000\" Geometry=\"F1 M0,0 H10 V10 H0 Z\" />\n" +
            "      </DrawingGroup>\n" +
            "    </DrawingGroup>\n" +
            "  </DrawingImage.Drawing>\n" +
            "</DrawingImage>";
        Assert.Equal(expected, Convert(GroupSvg));
    }

    [Fact]
    public void Convert_SolidFillWithAlpha_WritesAlphaInColor()
    {
        const string expected =
            "<DrawingImage Viewbox=\"0 0 100 100\">\n" +
            "  <DrawingImage.Drawing>\n" +
            "    <DrawingGroup>\n" +
            "      <GeometryDrawing Brush=\"#80FF0000\" Geometry=\"F1 M0,0 H10 V10 H0 Z\" />\n" +
            "    </DrawingGroup>\n" +
            "  </DrawingImage.Drawing>\n" +
            "</DrawingImage>";
        Assert.Equal(expected, Convert(SolidAlphaSvg));
    }

    [Fact]
    public void Convert_LinearGradientInBoundingBox_WritesPercentPoints()
    {
        const string expected =
            "<DrawingImage Viewbox=\"0 0 100 100\">\n" +
            "  <DrawingImage.Drawing>\n" +
            "    <DrawingGroup>\n" +
            "      <GeometryDrawing Geometry=\"F1 M0,0 H10 V10 H0 Z\">\n" +
            "        <GeometryDrawing.Brush>\n" +
            "          <LinearGradientBrush StartPoint=\"0%,0%\" EndPoint=\"100%,0%\">\n" +
            "            <GradientStop Offset=\"0\" Color=\"#FF000000\" />\n" +
            "            <GradientStop Offset=\"1\" Color=\"#80FFFFFF\" />\n" +
            "          </LinearGradientBrush>\n" +
            "        </GeometryDrawing.Brush>\n" +
            "      </GeometryDrawing>\n" +
            "    </DrawingGroup>\n" +
            "  </DrawingImage.Drawing>\n" +
            "</DrawingImage>";
        Assert.Equal(expected, Convert(LinearSvg));
    }

    [Fact]
    public void Convert_RadialGradientInUserSpace_WritesTransformAndReflect()
    {
        const string expected =
            "<DrawingImage Viewbox=\"0 0 100 100\">\n" +
            "  <DrawingImage.Drawing>\n" +
            "    <DrawingGroup>\n" +
            "      <GeometryDrawing Geometry=\"F1 M0,0 H10 V10 H0 Z\">\n" +
            "        <GeometryDrawing.Brush>\n" +
            "          <RadialGradientBrush Center=\"50,50\" GradientOrigin=\"45,55\" RadiusX=\"40\" RadiusY=\"40\" SpreadMethod=\"Reflect\" TransformOrigin=\"0,0\">\n" +
            "            <RadialGradientBrush.Transform>\n" +
            "              <MatrixTransform Matrix=\"1,0,0,1,2,3\" />\n" +
            "            </RadialGradientBrush.Transform>\n" +
            "            <GradientStop Offset=\"0\" Color=\"#FF000000\" />\n" +
            "            <GradientStop Offset=\"1\" Color=\"#80FFFFFF\" />\n" +
            "          </RadialGradientBrush>\n" +
            "        </GeometryDrawing.Brush>\n" +
            "      </GeometryDrawing>\n" +
            "    </DrawingGroup>\n" +
            "  </DrawingImage.Drawing>\n" +
            "</DrawingImage>";
        Assert.Equal(expected, Convert(RadialSvg));
    }

    [Fact]
    public void Convert_Dashes_AreDividedByStrokeWidth()
    {
        const string expected =
            "<DrawingImage Viewbox=\"0 0 100 100\">\n" +
            "  <DrawingImage.Drawing>\n" +
            "    <DrawingGroup>\n" +
            "      <GeometryDrawing Geometry=\"F1 M0,0 L10,0\">\n" +
            "        <GeometryDrawing.Pen>\n" +
            "          <Pen Brush=\"#FF000000\" Thickness=\"2\" MiterLimit=\"4\">\n" +
            "            <Pen.DashStyle>\n" +
            "              <DashStyle Dashes=\"2,1\" />\n" +
            "            </Pen.DashStyle>\n" +
            "          </Pen>\n" +
            "        </GeometryDrawing.Pen>\n" +
            "      </GeometryDrawing>\n" +
            "    </DrawingGroup>\n" +
            "  </DrawingImage.Drawing>\n" +
            "</DrawingImage>";
        Assert.Equal(expected, Convert(DashSvg));
    }

    [Fact]
    public void Convert_MiterJoinAtSvgDefault_WritesMiterLimitFour()
    {
        const string expected =
            "<DrawingImage Viewbox=\"0 0 100 100\">\n" +
            "  <DrawingImage.Drawing>\n" +
            "    <DrawingGroup>\n" +
            "      <GeometryDrawing Geometry=\"F1 M0,0 L10,0 L10,10\">\n" +
            "        <GeometryDrawing.Pen>\n" +
            "          <Pen Brush=\"#FF000000\" MiterLimit=\"4\" />\n" +
            "        </GeometryDrawing.Pen>\n" +
            "      </GeometryDrawing>\n" +
            "    </DrawingGroup>\n" +
            "  </DrawingImage.Drawing>\n" +
            "</DrawingImage>";
        Assert.Equal(expected, Convert(MiterSvg));
    }

    [Fact]
    public void Convert_ClipWithOneShape_WritesStreamGeometry()
    {
        const string expected =
            "<DrawingImage Viewbox=\"0 0 100 100\">\n" +
            "  <DrawingImage.Drawing>\n" +
            "    <DrawingGroup>\n" +
            "      <DrawingGroup>\n" +
            "        <DrawingGroup.ClipGeometry>\n" +
            "          <StreamGeometry>F1 M0,0 H5 V5 H0 Z</StreamGeometry>\n" +
            "        </DrawingGroup.ClipGeometry>\n" +
            "        <GeometryDrawing Brush=\"#FF000000\" Geometry=\"F1 M0,0 H10 V10 H0 Z\" />\n" +
            "      </DrawingGroup>\n" +
            "    </DrawingGroup>\n" +
            "  </DrawingImage.Drawing>\n" +
            "</DrawingImage>";
        Assert.Equal(expected, Convert(OneClipSvg));
    }

    [Fact]
    public void Convert_ClipWithTwoShapesOneTransformed_WritesUnion()
    {
        const string expected =
            "<DrawingImage Viewbox=\"0 0 100 100\">\n" +
            "  <DrawingImage.Drawing>\n" +
            "    <DrawingGroup>\n" +
            "      <DrawingGroup>\n" +
            "        <DrawingGroup.ClipGeometry>\n" +
            "          <CombinedGeometry GeometryCombineMode=\"Union\">\n" +
            "            <CombinedGeometry.Geometry1>\n" +
            "              <StreamGeometry>F1 M0,0 H5 V5 H0 Z</StreamGeometry>\n" +
            "            </CombinedGeometry.Geometry1>\n" +
            "            <CombinedGeometry.Geometry2>\n" +
            "              <GeometryGroup>\n" +
            "                <GeometryGroup.Transform>\n" +
            "                  <MatrixTransform Matrix=\"1,0,0,1,1,1\" />\n" +
            "                </GeometryGroup.Transform>\n" +
            "                <StreamGeometry>M0,0 H2 V2 H0 Z</StreamGeometry>\n" +
            "              </GeometryGroup>\n" +
            "            </CombinedGeometry.Geometry2>\n" +
            "          </CombinedGeometry>\n" +
            "        </DrawingGroup.ClipGeometry>\n" +
            "        <GeometryDrawing Brush=\"#FF000000\" Geometry=\"F1 M0,0 H10 V10 H0 Z\" />\n" +
            "      </DrawingGroup>\n" +
            "    </DrawingGroup>\n" +
            "  </DrawingImage.Drawing>\n" +
            "</DrawingImage>";
        Assert.Equal(expected, Convert(TwoClipSvg));
    }

    [Fact]
    public void Convert_UnderDanishCulture_UsesDots()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("da-DK");
            var text = SvgToXamlConverter.Convert(
                "<svg xmlns='http://www.w3.org/2000/svg' width='24.5' height='10'><g opacity='.25'>" +
                "<rect width='10.5' height='10' fill='#00f'/></g></svg>", out _);
            Assert.NotNull(text);
            Assert.Contains("Viewbox=\"0 0 24.5 10\"", text, StringComparison.Ordinal);
            Assert.Contains("Opacity=\"0.25\"", text, StringComparison.Ordinal);
            Assert.Contains("H10.5", text, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    [Fact]
    public void Convert_Unsupported_ReturnsNullAndReason()
    {
        var text = SvgToXamlConverter.Convert(Document("<text/>"), out var reason);
        Assert.Null(text);
        Assert.Equal("element:text", reason);
    }

    [Fact]
    public void Convert_Supported_HasNoReason()
    {
        SvgToXamlConverter.Convert(FillAndStrokeSvg, out var reason);
        Assert.Null(reason);
    }

    [Theory]
    [InlineData(GroupSvg)]
    [InlineData(SolidAlphaSvg)]
    [InlineData(LinearSvg)]
    [InlineData(RadialSvg)]
    [InlineData(DashSvg)]
    [InlineData(MiterSvg)]
    [InlineData(OneClipSvg)]
    [InlineData(TwoClipSvg)]
    public void Write_Output_IsWellFormedXml(string body)
    {
        var document = XDocument.Parse(Convert(body));
        Assert.Equal("DrawingImage", document.Root!.Name.LocalName);
    }

    [Fact]
    public void Write_FillAndStrokeOutput_IsWellFormedXml()
    {
        var text = SvgToXamlConverter.Convert(FillAndStrokeSvg, out _);
        Assert.Equal("DrawingImage", XDocument.Parse(text!).Root!.Name.LocalName);
    }
}
