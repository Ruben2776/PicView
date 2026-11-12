using Avalonia;
using Avalonia.Media;
using PicView.Core.Svg;

namespace PicView.Avalonia.Svg;

/// <summary>
/// Builds an Avalonia <see cref="DrawingImage" /> from an <see cref="SvgDrawing" />, with the same mapping
/// <see cref="AxamlDrawingWriter" /> writes as text. Construction only; call it on the UI thread.
/// </summary>
public static class DrawingImageBuilder
{
    public static DrawingImage Build(SvgDrawing drawing)
    {
        return new DrawingImage
        {
            // The viewport, so content outside it neither changes the image's size nor shows.
            Viewbox = new Rect(0, 0, drawing.Width, drawing.Height),
            Drawing = Group(drawing.Root)
        };
    }

    private static DrawingGroup Group(SvgGroup group)
    {
        var result = new DrawingGroup();
        if (group.Transform is { } transform)
        {
            result.Transform = new MatrixTransform(ToMatrix(transform));
        }

        if (group.Opacity != 1)
        {
            // An opacity mask composites the group as one layer, as SVG group opacity does; DrawingGroup.Opacity
            // fades each shape on its own, so overlaps would show through.
            result.OpacityMask = new SolidColorBrush(Colors.White, group.Opacity);
        }

        if (group.Clip is { } clip)
        {
            result.ClipGeometry = Clip(clip.Shapes, clip.Shapes.Count);
        }

        foreach (var child in group.Children)
        {
            switch (child)
            {
                case SvgGroup childGroup:
                    result.Children.Add(Group(childGroup));
                    break;
                case SvgShape shape:
                    result.Children.Add(Shape(shape));
                    break;
            }
        }

        return result;
    }

    /// <summary>The union of the first <paramref name="count" /> shapes, nesting left to right.</summary>
    private static Geometry Clip(IReadOnlyList<SvgClipShape> shapes, int count)
    {
        if (count <= 1)
        {
            return ClipShape(shapes[0]);
        }

        return new CombinedGeometry(GeometryCombineMode.Union, Clip(shapes, count - 1), ClipShape(shapes[count - 1]));
    }

    private static Geometry ClipShape(SvgClipShape shape)
    {
        var geometry = StreamGeometry.Parse(shape.PathData);
        if (shape.Transform is { } transform)
        {
            geometry.Transform = new MatrixTransform(ToMatrix(transform));
        }

        return geometry;
    }

    private static GeometryDrawing Shape(SvgShape shape)
    {
        return new GeometryDrawing
        {
            Geometry = StreamGeometry.Parse(shape.PathData),
            Brush = shape.Fill is { } fill ? Brush(fill) : null,
            Pen = shape.Stroke is { } stroke ? Pen(stroke) : null
        };
    }

    private static Pen Pen(SvgStroke stroke)
    {
        var pen = new Pen
        {
            Brush = Brush(stroke.Paint),
            Thickness = stroke.Width,
            LineCap = stroke.LineCap switch
            {
                SvgLineCap.Round => PenLineCap.Round,
                SvgLineCap.Square => PenLineCap.Square,
                _ => PenLineCap.Flat
            },
            LineJoin = stroke.LineJoin switch
            {
                SvgLineJoin.Round => PenLineJoin.Round,
                SvgLineJoin.Bevel => PenLineJoin.Bevel,
                _ => PenLineJoin.Miter
            },
            MiterLimit = stroke.MiterLimit
        };

        if (stroke.DashArray is { Count: > 0 } dashArray)
        {
            // Avalonia measures dashes in multiples of the pen thickness; SVG measures them in user units.
            var unit = stroke.Width > 0 ? stroke.Width : 1;
            var dashes = new double[dashArray.Count];
            for (var i = 0; i < dashes.Length; i++)
            {
                dashes[i] = dashArray[i] / unit;
            }

            pen.DashStyle = new DashStyle(dashes, stroke.DashOffset / unit);
        }

        return pen;
    }

    private static IBrush? Brush(SvgPaint paint)
    {
        switch (paint)
        {
            case SvgSolidPaint solid:
                return new SolidColorBrush(ToColor(solid.Color));
            case SvgLinearGradientPaint linear:
            {
                var unit = Unit(linear);
                var brush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(linear.X1, linear.Y1, unit),
                    EndPoint = new RelativePoint(linear.X2, linear.Y2, unit)
                };
                return Gradient(brush, linear);
            }
            case SvgRadialGradientPaint radial:
            {
                var unit = Unit(radial);
                var radius = new RelativeScalar(radial.R, unit);
                var brush = new RadialGradientBrush
                {
                    Center = new RelativePoint(radial.Cx, radial.Cy, unit),
                    GradientOrigin = new RelativePoint(radial.Fx, radial.Fy, unit),
                    RadiusX = radius,
                    RadiusY = radius
                };
                return Gradient(brush, radial);
            }
            default:
                return null;
        }
    }

    private static GradientBrush Gradient(GradientBrush brush, SvgGradientPaint gradient)
    {
        brush.SpreadMethod = gradient.Spread switch
        {
            SvgSpreadMethod.Reflect => GradientSpreadMethod.Reflect,
            SvgSpreadMethod.Repeat => GradientSpreadMethod.Repeat,
            _ => GradientSpreadMethod.Pad
        };
        brush.Opacity = gradient.Opacity;
        if (gradient.Transform is { } transform)
        {
            // SVG transforms a gradient about the user-space origin; Avalonia's default origin is the shape's corner.
            brush.Transform = new MatrixTransform(ToMatrix(transform));
            brush.TransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
        }

        foreach (var stop in gradient.Stops)
        {
            brush.GradientStops.Add(new GradientStop(ToColor(stop.Color), stop.Offset));
        }

        return brush;
    }

    private static RelativeUnit Unit(SvgGradientPaint gradient)
    {
        return gradient.Units == SvgGradientUnits.ObjectBoundingBox ? RelativeUnit.Relative : RelativeUnit.Absolute;
    }

    private static Matrix ToMatrix(SvgMatrix m)
    {
        return new Matrix(m.M11, m.M12, m.M21, m.M22, m.OffsetX, m.OffsetY);
    }

    private static Color ToColor(SvgColor c)
    {
        return Color.FromArgb(c.A, c.R, c.G, c.B);
    }
}
