using System.Runtime.InteropServices;

namespace PicView.Core.Comparison;

/// <summary>
/// 2D similarity transform (uniform scale, rotation, translation) mapping image B coordinates onto image A.
/// </summary>
/// <param name="M11">Scale * cos(theta).</param>
/// <param name="M12">Scale * sin(theta).</param>
/// <param name="M21">-Scale * sin(theta).</param>
/// <param name="M22">Scale * cos(theta).</param>
/// <param name="OffsetX">Horizontal translation.</param>
/// <param name="OffsetY">Vertical translation.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct SimilarityMatrix(
    double M11,
    double M12,
    double M21,
    double M22,
    double OffsetX,
    double OffsetY)
{
    public static SimilarityMatrix Identity => new(1, 0, 0, 1, 0, 0);

    public (double X, double Y) Transform(double x, double y) =>
        (M11 * x + M21 * y + OffsetX, M12 * x + M22 * y + OffsetY);
}

public static class SimilarityTransform
{
    /// <summary>
    /// Computes the similarity transform mapping <paramref name="b1"/>-><paramref name="a1"/>
    /// and <paramref name="b2"/>-><paramref name="a2"/>.
    /// Returns false if either anchor pair is degenerate (identical points).
    /// </summary>
    public static bool TryCompute(
        (double X, double Y) a1,
        (double X, double Y) a2,
        (double X, double Y) b1,
        (double X, double Y) b2,
        out SimilarityMatrix matrix)
    {
        var dax = a2.X - a1.X;
        var day = a2.Y - a1.Y;
        var dbx = b2.X - b1.X;
        var dby = b2.Y - b1.Y;

        var lenA2 = dax * dax + day * day;
        var lenB2 = dbx * dbx + dby * dby;
        if (lenA2 < 1e-6 || lenB2 < 1e-6)
        {
            matrix = SimilarityMatrix.Identity;
            return false;
        }

        var a = (dax * dbx + day * dby) / lenB2;
        var b = (day * dbx - dax * dby) / lenB2;
        var tx = a1.X - (a * b1.X - b * b1.Y);
        var ty = a1.Y - (b * b1.X + a * b1.Y);

        matrix = new SimilarityMatrix(a, b, -b, a, tx, ty);
        return true;
    }
}
