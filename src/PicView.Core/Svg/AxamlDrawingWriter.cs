using System.Text;

namespace PicView.Core.Svg;

/// <summary>Writes an <see cref="SvgDrawing" /> as the text of an Avalonia <c>DrawingImage</c> in AXAML.</summary>
public static class AxamlDrawingWriter
{
    private const string FillRuleNonZeroPrefix = "F1 ";

    public static string Write(SvgDrawing drawing)
    {
        var sb = new StringBuilder();
        Line(sb, 0, "<DrawingImage Viewbox=\"0 0 " + SvgNumber.Format(drawing.Width) + " " +
                    SvgNumber.Format(drawing.Height) + "\">");
        Line(sb, 1, "<DrawingImage.Drawing>");
        WriteGroup(sb, 2, drawing.Root);
        Line(sb, 1, "</DrawingImage.Drawing>");
        Line(sb, 0, "</DrawingImage>");
        return sb.ToString();
    }

    private static void WriteGroup(StringBuilder sb, int depth, SvgGroup group)
    {
        var tag = new StringBuilder("<DrawingGroup");
        if (group.Opacity != 1)
        {
            Attribute(tag, "Opacity", SvgNumber.Format(group.Opacity));
        }

        if (group.Transform is null && group.Clip is null && group.Children.Count == 0)
        {
            Line(sb, depth, tag.Append(" />").ToString());
            return;
        }

        Line(sb, depth, tag.Append('>').ToString());
        if (group.Transform is { } transform)
        {
            Line(sb, depth + 1, "<DrawingGroup.Transform>");
            Line(sb, depth + 2, MatrixTag(transform));
            Line(sb, depth + 1, "</DrawingGroup.Transform>");
        }

        if (group.Clip is { } clip)
        {
            Line(sb, depth + 1, "<DrawingGroup.ClipGeometry>");
            WriteClip(sb, depth + 2, clip.Shapes, clip.Shapes.Count);
            Line(sb, depth + 1, "</DrawingGroup.ClipGeometry>");
        }

        foreach (var child in group.Children)
        {
            switch (child)
            {
                case SvgGroup childGroup:
                    WriteGroup(sb, depth + 1, childGroup);
                    break;
                case SvgShape shape:
                    WriteShape(sb, depth + 1, shape);
                    break;
            }
        }

        Line(sb, depth, "</DrawingGroup>");
    }

    /// <summary>Writes the union of the first <paramref name="count" /> shapes, nesting left to right.</summary>
    private static void WriteClip(StringBuilder sb, int depth, IReadOnlyList<SvgClipShape> shapes, int count)
    {
        if (count <= 1)
        {
            WriteClipShape(sb, depth, shapes[0]);
            return;
        }

        Line(sb, depth, "<CombinedGeometry GeometryCombineMode=\"Union\">");
        Line(sb, depth + 1, "<CombinedGeometry.Geometry1>");
        WriteClip(sb, depth + 2, shapes, count - 1);
        Line(sb, depth + 1, "</CombinedGeometry.Geometry1>");
        Line(sb, depth + 1, "<CombinedGeometry.Geometry2>");
        WriteClipShape(sb, depth + 2, shapes[count - 1]);
        Line(sb, depth + 1, "</CombinedGeometry.Geometry2>");
        Line(sb, depth, "</CombinedGeometry>");
    }

    private static void WriteClipShape(StringBuilder sb, int depth, SvgClipShape shape)
    {
        var geometry = "<StreamGeometry>" + Escape(shape.PathData) + "</StreamGeometry>";
        if (shape.Transform is not { } transform)
        {
            Line(sb, depth, geometry);
            return;
        }

        var tag = new StringBuilder("<GeometryGroup");
        if (shape.PathData.StartsWith(FillRuleNonZeroPrefix, StringComparison.Ordinal))
        {
            Attribute(tag, "FillRule", "NonZero");
        }

        Line(sb, depth, tag.Append('>').ToString());
        Line(sb, depth + 1, "<GeometryGroup.Transform>");
        Line(sb, depth + 2, MatrixTag(transform));
        Line(sb, depth + 1, "</GeometryGroup.Transform>");
        Line(sb, depth + 1, geometry);
        Line(sb, depth, "</GeometryGroup>");
    }

    private static void WriteShape(StringBuilder sb, int depth, SvgShape shape)
    {
        var tag = new StringBuilder("<GeometryDrawing");
        var gradientFill = shape.Fill as SvgGradientPaint;
        if (shape.Fill is SvgSolidPaint solid)
        {
            Attribute(tag, "Brush", ColorText(solid.Color));
        }

        Attribute(tag, "Geometry", shape.PathData);

        if (gradientFill is null && shape.Stroke is null)
        {
            Line(sb, depth, tag.Append(" />").ToString());
            return;
        }

        Line(sb, depth, tag.Append('>').ToString());
        if (gradientFill is not null)
        {
            Line(sb, depth + 1, "<GeometryDrawing.Brush>");
            WriteGradient(sb, depth + 2, gradientFill);
            Line(sb, depth + 1, "</GeometryDrawing.Brush>");
        }

        if (shape.Stroke is { } stroke)
        {
            Line(sb, depth + 1, "<GeometryDrawing.Pen>");
            WritePen(sb, depth + 2, stroke);
            Line(sb, depth + 1, "</GeometryDrawing.Pen>");
        }

        Line(sb, depth, "</GeometryDrawing>");
    }

    private static void WritePen(StringBuilder sb, int depth, SvgStroke stroke)
    {
        var tag = new StringBuilder("<Pen");
        var gradientBrush = stroke.Paint as SvgGradientPaint;
        if (stroke.Paint is SvgSolidPaint solid)
        {
            Attribute(tag, "Brush", ColorText(solid.Color));
        }

        if (stroke.Width != 1)
        {
            Attribute(tag, "Thickness", SvgNumber.Format(stroke.Width));
        }

        switch (stroke.LineCap)
        {
            case SvgLineCap.Round:
                Attribute(tag, "LineCap", "Round");
                break;
            case SvgLineCap.Square:
                Attribute(tag, "LineCap", "Square");
                break;
        }

        switch (stroke.LineJoin)
        {
            case SvgLineJoin.Round:
                Attribute(tag, "LineJoin", "Round");
                break;
            case SvgLineJoin.Bevel:
                Attribute(tag, "LineJoin", "Bevel");
                break;
            default:
                if (stroke.MiterLimit != 10)
                {
                    Attribute(tag, "MiterLimit", SvgNumber.Format(stroke.MiterLimit));
                }

                break;
        }

        var hasDashes = stroke.DashArray is { Count: > 0 };
        if (gradientBrush is null && !hasDashes)
        {
            Line(sb, depth, tag.Append(" />").ToString());
            return;
        }

        Line(sb, depth, tag.Append('>').ToString());
        if (gradientBrush is not null)
        {
            Line(sb, depth + 1, "<Pen.Brush>");
            WriteGradient(sb, depth + 2, gradientBrush);
            Line(sb, depth + 1, "</Pen.Brush>");
        }

        if (hasDashes)
        {
            // Avalonia measures dashes in multiples of the pen thickness; SVG measures them in user units.
            var unit = stroke.Width > 0 ? stroke.Width : 1;
            var dashes = new StringBuilder();
            foreach (var dash in stroke.DashArray!)
            {
                if (dashes.Length > 0)
                {
                    dashes.Append(',');
                }

                dashes.Append(SvgNumber.Format(dash / unit));
            }

            var dashTag = new StringBuilder("<DashStyle");
            Attribute(dashTag, "Dashes", dashes.ToString());
            if (stroke.DashOffset != 0)
            {
                Attribute(dashTag, "Offset", SvgNumber.Format(stroke.DashOffset / unit));
            }

            Line(sb, depth + 1, "<Pen.DashStyle>");
            Line(sb, depth + 2, dashTag.Append(" />").ToString());
            Line(sb, depth + 1, "</Pen.DashStyle>");
        }

        Line(sb, depth, "</Pen>");
    }

    private static void WriteGradient(StringBuilder sb, int depth, SvgGradientPaint gradient)
    {
        var boundingBox = gradient.Units == SvgGradientUnits.ObjectBoundingBox;
        string name;
        var tag = new StringBuilder();
        switch (gradient)
        {
            case SvgLinearGradientPaint linear:
                name = "LinearGradientBrush";
                tag.Append('<').Append(name);
                Attribute(tag, "StartPoint", Point(linear.X1, linear.Y1, boundingBox));
                Attribute(tag, "EndPoint", Point(linear.X2, linear.Y2, boundingBox));
                break;
            case SvgRadialGradientPaint radial:
                name = "RadialGradientBrush";
                var radius = Coordinate(radial.R, boundingBox);
                tag.Append('<').Append(name);
                Attribute(tag, "Center", Point(radial.Cx, radial.Cy, boundingBox));
                Attribute(tag, "GradientOrigin", Point(radial.Fx, radial.Fy, boundingBox));
                Attribute(tag, "RadiusX", radius);
                Attribute(tag, "RadiusY", radius);
                break;
            default:
                return;
        }

        if (gradient.Spread != SvgSpreadMethod.Pad)
        {
            Attribute(tag, "SpreadMethod", gradient.Spread.ToString());
        }

        if (gradient.Opacity != 1)
        {
            Attribute(tag, "Opacity", SvgNumber.Format(gradient.Opacity));
        }

        if (gradient.Transform is null && gradient.Stops.Count == 0)
        {
            Line(sb, depth, tag.Append(" />").ToString());
            return;
        }

        Line(sb, depth, tag.Append('>').ToString());
        if (gradient.Transform is { } transform)
        {
            Line(sb, depth + 1, "<" + name + ".Transform>");
            Line(sb, depth + 2, MatrixTag(transform));
            Line(sb, depth + 1, "</" + name + ".Transform>");
        }

        foreach (var stop in gradient.Stops)
        {
            Line(sb, depth + 1, "<GradientStop Offset=\"" + SvgNumber.Format(stop.Offset) + "\" Color=\"" +
                                ColorText(stop.Color) + "\" />");
        }

        Line(sb, depth, "</" + name + ">");
    }

    private static string Coordinate(double value, bool boundingBox)
    {
        return boundingBox ? SvgNumber.Format(value * 100) + "%" : SvgNumber.Format(value);
    }

    private static string Point(double x, double y, bool boundingBox)
    {
        return Coordinate(x, boundingBox) + "," + Coordinate(y, boundingBox);
    }

    private static string MatrixTag(SvgMatrix m)
    {
        return "<MatrixTransform Matrix=\"" + SvgNumber.Format(m.M11) + "," + SvgNumber.Format(m.M12) + "," +
               SvgNumber.Format(m.M21) + "," + SvgNumber.Format(m.M22) + "," + SvgNumber.Format(m.OffsetX) + "," +
               SvgNumber.Format(m.OffsetY) + "\" />";
    }

    private static string ColorText(SvgColor c)
    {
        return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
    }

    private static void Attribute(StringBuilder tag, string name, string value)
    {
        tag.Append(' ').Append(name).Append("=\"").Append(Escape(value)).Append('"');
    }

    private static string Escape(string value)
    {
        if (value.AsSpan().IndexOfAny("&<>\"") < 0)
        {
            return value;
        }

        return value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }

    private static void Line(StringBuilder sb, int depth, string text)
    {
        if (sb.Length > 0)
        {
            sb.Append('\n');
        }

        sb.Append(' ', depth * 2).Append(text);
    }
}
