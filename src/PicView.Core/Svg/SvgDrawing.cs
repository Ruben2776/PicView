using System.Runtime.InteropServices;

namespace PicView.Core.Svg;

/// <summary>A parsed SVG: its size in user units and the root group of drawing nodes.</summary>
public sealed record SvgDrawing(double Width, double Height, SvgGroup Root);

public abstract record SvgNode;

public sealed record SvgGroup(SvgMatrix? Transform, double Opacity, SvgClip? Clip, IReadOnlyList<SvgNode> Children)
    : SvgNode;

public sealed record SvgShape(string PathData, SvgPaint? Fill, SvgStroke? Stroke) : SvgNode;

public abstract record SvgPaint;

/// <summary>A solid paint; its opacity is folded into the color's alpha.</summary>
public sealed record SvgSolidPaint(SvgColor Color) : SvgPaint;

[StructLayout(LayoutKind.Auto)]
public readonly record struct SvgGradientStop(double Offset, SvgColor Color);

public enum SvgGradientUnits
{
    ObjectBoundingBox,
    UserSpaceOnUse
}

public enum SvgSpreadMethod
{
    Pad,
    Reflect,
    Repeat
}

public abstract record SvgGradientPaint(
    IReadOnlyList<SvgGradientStop> Stops,
    SvgGradientUnits Units,
    SvgMatrix? Transform,
    SvgSpreadMethod Spread,
    double Opacity) : SvgPaint;

public sealed record SvgLinearGradientPaint(
    double X1,
    double Y1,
    double X2,
    double Y2,
    IReadOnlyList<SvgGradientStop> Stops,
    SvgGradientUnits Units,
    SvgMatrix? Transform,
    SvgSpreadMethod Spread,
    double Opacity) : SvgGradientPaint(Stops, Units, Transform, Spread, Opacity);

public sealed record SvgRadialGradientPaint(
    double Cx,
    double Cy,
    double R,
    double Fx,
    double Fy,
    IReadOnlyList<SvgGradientStop> Stops,
    SvgGradientUnits Units,
    SvgMatrix? Transform,
    SvgSpreadMethod Spread,
    double Opacity) : SvgGradientPaint(Stops, Units, Transform, Spread, Opacity);

public enum SvgLineCap
{
    Butt,
    Round,
    Square
}

public enum SvgLineJoin
{
    Miter,
    Round,
    Bevel
}

public sealed record SvgStroke(
    SvgPaint Paint,
    double Width,
    SvgLineCap LineCap,
    SvgLineJoin LineJoin,
    double MiterLimit,
    IReadOnlyList<double>? DashArray,
    double DashOffset);

public sealed record SvgClipShape(string PathData, SvgMatrix? Transform);

public sealed record SvgClip(IReadOnlyList<SvgClipShape> Shapes);
