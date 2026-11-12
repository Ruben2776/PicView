using PicView.Core.Svg;

namespace PicView.Tests.Svg;

public class SvgMatrixTests
{
    [Fact]
    public void Then_TranslateAfterScale_MatchesSvgListOrder()
    {
        // transform="translate(10,0) scale(2)" applied to (1,0) is (12,0)
        var m = new SvgMatrix(2, 0, 0, 2, 0, 0).Then(new SvgMatrix(1, 0, 0, 1, 10, 0));
        Assert.Equal((12d, 0d), m.Apply(1, 0));
    }

    [Fact]
    public void Identity_IsIdentity()
    {
        Assert.True(SvgMatrix.Identity.IsIdentity);
        Assert.False(new SvgMatrix(1, 0, 0, 1, 1, 0).IsIdentity);
    }

    [Fact]
    public void Apply_UsesRowVectorLayout()
    {
        // x' = x*M11 + y*M21 + OffsetX ; y' = x*M12 + y*M22 + OffsetY
        var m = new SvgMatrix(1, 2, 3, 4, 5, 6);
        Assert.Equal((1 * 1 + 1 * 3 + 5d, 1 * 2 + 1 * 4 + 6d), m.Apply(1, 1));
    }
}
