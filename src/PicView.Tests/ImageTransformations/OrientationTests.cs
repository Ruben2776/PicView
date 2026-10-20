using PicView.Core.ImageTransformations;

namespace PicView.Tests.ImageTransformations;

public class OrientationTests
{
    [Fact]
    public void Default_IsUprightAndNotFlipped()
    {
        var orientation = default(Orientation);

        Assert.Equal(Orientation.Upright, orientation);
        Assert.Equal(0, orientation.QuarterTurns);
        Assert.False(orientation.IsFlipped);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 0)]
    public void TurnedRight_AddsAQuarterTurnAndWrapsPastThree(int quarterTurns, int expected)
    {
        Assert.Equal(expected, new Orientation(quarterTurns, false).TurnedRight().QuarterTurns);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 0)]
    [InlineData(3, 2)]
    public void TurnedLeft_TakesAQuarterTurnAwayAndWrapsBelowZero(int quarterTurns, int expected)
    {
        Assert.Equal(expected, new Orientation(quarterTurns, false).TurnedLeft().QuarterTurns);
    }

    [Theory]
    [InlineData(4, 0)]
    [InlineData(5, 1)]
    [InlineData(-1, 3)]
    [InlineData(-6, 2)]
    public void Constructor_OutOfRangeQuarterTurns_WrapsIntoZeroToThree(int quarterTurns, int expected)
    {
        Assert.Equal(expected, new Orientation(quarterTurns, false).QuarterTurns);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 90)]
    [InlineData(2, 180)]
    [InlineData(3, 270)]
    public void Degrees_IsNinetyPerQuarterTurn(int quarterTurns, int expected)
    {
        Assert.Equal(expected, new Orientation(quarterTurns, false).Degrees);
    }

    [Fact]
    public void FlippedOver_TogglesTheFlipAndKeepsTheTurns()
    {
        var turned = new Orientation(2, false);

        var flipped = turned.FlippedOver();

        Assert.Equal(new Orientation(2, true), flipped);
        Assert.Equal(turned, flipped.FlippedOver());
    }

    [Fact]
    public void Turning_KeepsTheFlip()
    {
        var flipped = new Orientation(0, true);

        Assert.True(flipped.TurnedRight().IsFlipped);
        Assert.True(flipped.TurnedLeft().IsFlipped);
    }
}
