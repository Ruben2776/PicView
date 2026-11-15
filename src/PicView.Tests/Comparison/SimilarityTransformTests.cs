using PicView.Core.Comparison;

namespace PicView.Tests.Comparison;

public class SimilarityTransformTests
{
    [Fact]
    public void TryCompute_PureTranslation_MapsBothPointsExactly()
    {
        var ok = SimilarityTransform.TryCompute(
            (110, 220), (210, 220),
            (100, 200), (200, 200),
            out var m);

        Assert.True(ok);
        Assert.Equal(1.0, m.M11, 6);
        Assert.Equal(0.0, m.M12, 6);
        Assert.Equal(10.0, m.OffsetX, 6);
        Assert.Equal(20.0, m.OffsetY, 6);

        var p1 = m.Transform(100, 200);
        var p2 = m.Transform(200, 200);
        Assert.Equal(110, p1.X, 6);
        Assert.Equal(220, p1.Y, 6);
        Assert.Equal(210, p2.X, 6);
        Assert.Equal(220, p2.Y, 6);
    }

    [Fact]
    public void TryCompute_ScaleAnd90DegreeRotation_MapsBothPointsExactly()
    {
        // B: (0,0) -> (10,0); A: (50,50) -> (50,70) (2x scale, +90 deg rotation)
        var ok = SimilarityTransform.TryCompute(
            (50, 50), (50, 70),
            (0, 0), (10, 0),
            out var m);

        Assert.True(ok);
        var p1 = m.Transform(0, 0);
        var p2 = m.Transform(10, 0);
        Assert.Equal(50, p1.X, 6);
        Assert.Equal(50, p1.Y, 6);
        Assert.Equal(50, p2.X, 6);
        Assert.Equal(70, p2.Y, 6);
    }

    [Fact]
    public void TryCompute_DegeneratePoints_ReturnsFalseAndIdentity()
    {
        var ok = SimilarityTransform.TryCompute(
            (10, 10), (10, 10),
            (20, 20), (40, 20),
            out var m);

        Assert.False(ok);
        Assert.Equal(SimilarityMatrix.Identity, m);
    }
}
