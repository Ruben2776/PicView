using System.Runtime.InteropServices;

namespace PicView.Core.Svg;

/// <summary>
///     A 2D affine matrix in Avalonia's row-vector layout, so SVG <c>matrix(a b c d e f)</c> is
///     <c>new SvgMatrix(a, b, c, d, e, f)</c>.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct SvgMatrix(double M11, double M12, double M21, double M22, double OffsetX, double OffsetY)
{
    public static SvgMatrix Identity { get; } = new(1, 0, 0, 1, 0, 0);

    public bool IsIdentity => M11 == 1 && M12 == 0 && M21 == 0 && M22 == 1 && OffsetX == 0 && OffsetY == 0;

    /// <summary>Returns the matrix that applies this matrix to a point first, then <paramref name="next" />.</summary>
    public SvgMatrix Then(SvgMatrix next)
    {
        return new SvgMatrix(
            M11 * next.M11 + M12 * next.M21,
            M11 * next.M12 + M12 * next.M22,
            M21 * next.M11 + M22 * next.M21,
            M21 * next.M12 + M22 * next.M22,
            OffsetX * next.M11 + OffsetY * next.M21 + next.OffsetX,
            OffsetX * next.M12 + OffsetY * next.M22 + next.OffsetY);
    }

    public (double X, double Y) Apply(double x, double y)
    {
        return (x * M11 + y * M21 + OffsetX, x * M12 + y * M22 + OffsetY);
    }
}
