using PicView.Core.Navigation;
using PicView.Core.Models;
using PicView.Core.Config;

namespace PicView.Tests.Navigation;

public class SideBySidePairTests
{
    [Theory]
    // index, count, navigation, looping, expected index, expected reversed
    [InlineData(0, 3, NavigateTo.Next, false, 1, false)] // clamped, so the step ends on the last full pair
    [InlineData(1, 3, NavigateTo.Next, false, 1, false)]
    [InlineData(0, 5, NavigateTo.Next, false, 2, false)]
    [InlineData(2, 3, NavigateTo.Next, true, 1, false)] // looping wraps around
    [InlineData(0, 3, NavigateTo.Previous, false, 0, true)]
    [InlineData(0, 3, NavigateTo.Previous, true, 1, true)]
    [InlineData(2, 4, NavigateTo.First, false, 0, true)]
    [InlineData(0, 4, NavigateTo.Last, false, 2, false)]
    [InlineData(0, 4, NavigateTo.Last, true, 2, false)]
    [InlineData(0, 1, NavigateTo.Next, false, 0, false)]
    [InlineData(0, 0, NavigateTo.Next, false, -1, false)]
    public void Navigate_StepsByPairs(int index, int count, NavigateTo navigation, bool looping, int expected,
        bool reversed)
    {
        Assert.Equal((expected, reversed), SideBySidePair.Navigate(index, count, navigation, SkipAmount.One, looping));
    }

    [Theory]
    // index, count, sideBySide, looping, expected primary, expected partner (-1 = none)
    [InlineData(0, 3, true, false, 0, 1)]
    [InlineData(1, 3, true, false, 1, 2)]
    [InlineData(2, 3, true, false, 1, 2)] // the last image shows in the pair before it
    [InlineData(2, 3, true, true, 2, 0)] // looping pairs the last image with the first
    [InlineData(0, 1, true, false, 0, -1)] // a list of one image shows alone
    [InlineData(0, 1, true, true, 0, -1)]
    [InlineData(1, 2, true, false, 0, 1)]
    [InlineData(2, 3, false, false, 2, -1)] // side-by-side off
    [InlineData(0, 3, false, true, 0, -1)]
    public void PairAt_ShowsThePairThatHoldsTheIndex(int index, int count, bool sideBySide, bool looping,
        int primary, int partner)
    {
        var pair = SideBySidePair.PairAt(index, count, sideBySide, looping);

        Assert.Equal(primary, pair.Primary);
        Assert.Equal(partner < 0 ? null : (int?)partner, pair.Partner);
    }

    [Theory]
    // index, count, sideBySide, looping, expected
    [InlineData(0, 3, true, false, true)]
    [InlineData(1, 3, true, false, false)] // from index 1 the pair is already (1, 2), the last pair of three
    [InlineData(1, 3, false, false, true)]
    [InlineData(2, 3, false, false, false)]
    [InlineData(2, 3, true, true, true)] // looping always moves on
    public void CanNavigateForwards_NeedsAPairAhead(int index, int count, bool sideBySide, bool looping, bool expected)
    {
        Assert.Equal(expected,
            SideBySidePair.CanNavigateForwards(index, count, isExtracting: false, sideBySide, looping));
    }

    [Fact]
    public void CanNavigateForwards_WhileExtracting_IsAllowed()
    {
        Assert.True(SideBySidePair.CanNavigateForwards(2, 3, isExtracting: true, sideBySide: true, looping: false));
    }

    [Fact]
    public void IndexOf_ExactSpelling_FindsTheFile()
    {
        // Arrange
        var files = new List<FileInfo>
        {
            new("/pics/a.jpg"),
            new("/pics/b.jpg")
        };

        // Act & Assert
        Assert.Equal(1, SideBySidePair.IndexOf(files, new FileInfo("/pics/b.jpg")));
    }

    [Fact]
    public void IndexOf_DifferentCase_FindsTheFile()
    {
        // Arrange
        var files = new List<FileInfo>
        {
            new("/pics/a.jpg"),
            new("/pics/B.jpg")
        };

        // Act & Assert
        Assert.Equal(1, SideBySidePair.IndexOf(files, new FileInfo("/pics/b.JPG")));
    }

    [Fact]
    public void IndexOf_NotInList_ReturnsNull()
    {
        // Arrange
        var files = new List<FileInfo> { new("/pics/a.jpg") };

        // Act & Assert
        Assert.Null(SideBySidePair.IndexOf(files, new FileInfo("/other/a.jpg")));
    }
}
