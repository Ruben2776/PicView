using PicView.Core.Config;
using PicView.Core.Sizing;

namespace PicView.Tests.Sizing;

public class ComparisonBarDockingTests
{
    [Theory]
    [InlineData(ComparisonBarPosition.Bottom, 0, 40)]
    [InlineData(ComparisonBarPosition.Top, 0, 40)]
    [InlineData(ComparisonBarPosition.Left, 40, 0)]
    [InlineData(ComparisonBarPosition.Right, 40, 0)]
    public void ReservedBy_EachPosition_TakesTheExtentOnItsAxis(ComparisonBarPosition position, double width,
        double height)
        => Assert.Equal((width, height), ComparisonBarDocking.ReservedBy(position, 40));

    [Fact]
    public void ReservedBy_NotMeasured_ReservesNothing() =>
        Assert.Equal((0d, 0d), ComparisonBarDocking.ReservedBy(ComparisonBarPosition.Left, 0));
}
