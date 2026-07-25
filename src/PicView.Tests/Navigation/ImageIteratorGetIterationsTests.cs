using PicView.Core.Navigation;
using PicView.Core.Models;
using PicView.Core.Config;

namespace PicView.Tests.Navigation;

public class ImageIteratorGetIterationsTests
{
    [Fact]
    public void GetIterations_Next_NoLooping_ClampsAtEnd()
    {
        // Arrange
        var count = 3;
        Settings.UIProperties.Looping = false;

        // Act
        var result = IterationHelper.GetIterations(0, count, NavigateTo.Next, SkipAmount.One);
        var resultLast = IterationHelper.GetIterations(1, count, NavigateTo.Next, SkipAmount.One);

        // Assert
        Assert.Equal((1, 2, false), result);
        Assert.Equal((1, 2, false), resultLast);
    }

    [Fact]
    public void GetIterations_Next_Looping_LoopsAround()
    {
        // Arrange
        var count = 3;
        Settings.UIProperties.Looping = true;

        // Act
        var result = IterationHelper.GetIterations(2, count, NavigateTo.Next, SkipAmount.One);

        // Assert
        Assert.Equal((0, 1, false), result);
    }

    [Fact]
    public void GetIterations_Previous_NoLooping_ClampsAtStart()
    {
        // Arrange
        var count = 3;
        Settings.UIProperties.Looping = false;

        // Act
        var result = IterationHelper.GetIterations(2, count, NavigateTo.Previous, SkipAmount.One);
        var resultFirst = IterationHelper.GetIterations(0, count, NavigateTo.Previous, SkipAmount.One);

        // Assert
        Assert.Equal((1, 2, true), result);
        Assert.Equal((0, 1, true), resultFirst);
    }

    [Fact]
    public void GetIterations_First_And_Last()
    {
        // Arrange
        var count = 4;

        // Act
        var first = IterationHelper.GetIterations(2, count, NavigateTo.First, SkipAmount.One);
        var last = IterationHelper.GetIterations(2, count, NavigateTo.Last, SkipAmount.One);

        // Assert
        Assert.Equal((0, 1, true), first);
        Assert.Equal((2, 3, false), last);
    }
}
