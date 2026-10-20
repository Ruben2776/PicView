using PicView.Core.Navigation;
using PicView.Core.Models;
using PicView.Core.Config;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class SideBySidePairTests
{
    public SideBySidePairTests()
    {
        SetDefaults();
    }

    [Fact]
    public void Navigate_Next_NoLooping_ClampsAtEnd()
    {
        // Arrange
        var count = 3;
        Settings.UIProperties.Looping = false;

        // Act
        var result = SideBySidePair.Navigate(0, count, NavigateTo.Next, SkipAmount.One);
        var resultLast = SideBySidePair.Navigate(1, count, NavigateTo.Next, SkipAmount.One);

        // Assert
        Assert.Equal((1, (int?)2, false), result);
        Assert.Equal((1, (int?)2, false), resultLast);
    }

    [Fact]
    public void Navigate_Next_Looping_LoopsAround()
    {
        // Arrange
        var count = 3;
        Settings.UIProperties.Looping = true;

        // Act
        var result = SideBySidePair.Navigate(2, count, NavigateTo.Next, SkipAmount.One);

        // Assert
        Assert.Equal((1, (int?)2, false), result);
    }

    [Fact]
    public void Navigate_Previous_NoLooping_ClampsAtStart()
    {
        // Arrange
        var count = 3;
        Settings.UIProperties.Looping = false;

        // Act
        var result = SideBySidePair.Navigate(0, count, NavigateTo.Previous, SkipAmount.One);

        // Assert
        Assert.Equal((0, (int?)1, true), result);
    }

    [Fact]
    public void Navigate_First_And_Last()
    {
        // Arrange
        var count = 4;

        // Act
        var first = SideBySidePair.Navigate(2, count, NavigateTo.First, SkipAmount.One);
        var last = SideBySidePair.Navigate(2, count, NavigateTo.Last, SkipAmount.One);

        // Assert
        Assert.Equal((0, (int?)1, true), first);
        Assert.Equal((2, (int?)3, false), last);
    }

    [Fact]
    public void Navigate_OneImage_HasNoSecondary()
    {
        // Act
        var result = SideBySidePair.Navigate(0, 1, NavigateTo.Next, SkipAmount.None);

        // Assert
        Assert.Equal((0, (int?)null, false), result);
    }

    [Fact]
    public void Navigate_NoImages_ReturnsNoIndices()
    {
        // Act
        var result = SideBySidePair.Navigate(0, 0, NavigateTo.Next, SkipAmount.None);

        // Assert
        Assert.Equal((-1, (int?)null, false), result);
    }

    [Fact]
    public void PartnerOf_NextImage_IsTheImageAfter()
    {
        // Arrange
        Settings.ImageScaling.ShowImageSideBySide = true;
        Settings.UIProperties.Looping = false;

        // Act & Assert
        Assert.Equal(1, SideBySidePair.PartnerOf(0, 3));
        Assert.Equal(2, SideBySidePair.PartnerOf(1, 3));
    }

    [Fact]
    public void PartnerOf_LastImage_NoLooping_HasNoPartner()
    {
        // Arrange
        Settings.ImageScaling.ShowImageSideBySide = true;
        Settings.UIProperties.Looping = false;

        // Act & Assert
        Assert.Null(SideBySidePair.PartnerOf(2, 3));
    }

    [Fact]
    public void PartnerOf_LastImage_Looping_IsTheFirstImage()
    {
        // Arrange
        Settings.ImageScaling.ShowImageSideBySide = true;
        Settings.UIProperties.Looping = true;

        // Act & Assert
        Assert.Equal(0, SideBySidePair.PartnerOf(2, 3));
    }

    [Fact]
    public void PartnerOf_OneImage_HasNoPartner()
    {
        // Arrange
        Settings.ImageScaling.ShowImageSideBySide = true;
        Settings.UIProperties.Looping = true;

        // Act & Assert
        Assert.Null(SideBySidePair.PartnerOf(0, 1));
    }

    [Fact]
    public void PartnerOf_SideBySideOff_HasNoPartner()
    {
        // Arrange
        Settings.ImageScaling.ShowImageSideBySide = false;

        // Act & Assert
        Assert.Null(SideBySidePair.PartnerOf(0, 3));
    }

    [Fact]
    public void PartnerOf_IndexOutOfRange_HasNoPartner()
    {
        // Arrange
        Settings.ImageScaling.ShowImageSideBySide = true;

        // Act & Assert
        Assert.Null(SideBySidePair.PartnerOf(-1, 3));
        Assert.Null(SideBySidePair.PartnerOf(3, 3));
    }

    [Fact]
    public void ShownCount_SideBySideWithTwoEntries_IsTwo()
    {
        // Arrange
        Settings.ImageScaling.ShowImageSideBySide = true;

        // Act & Assert
        Assert.Equal(2, SideBySidePair.ShownCount(2));
    }

    [Fact]
    public void ShownCount_SideBySideWithOneEntry_IsOne()
    {
        // Arrange
        Settings.ImageScaling.ShowImageSideBySide = true;

        // Act & Assert
        Assert.Equal(1, SideBySidePair.ShownCount(1));
    }

    [Fact]
    public void ShownCount_SideBySideOff_IsOne()
    {
        // Arrange
        Settings.ImageScaling.ShowImageSideBySide = false;

        // Act & Assert
        Assert.Equal(1, SideBySidePair.ShownCount(3));
    }

    [Fact]
    public void CanNavigateForwards_SideBySide_NeedsAnImageAhead()
    {
        // Arrange
        Settings.ImageScaling.ShowImageSideBySide = true;
        Settings.UIProperties.Looping = false;

        // Act & Assert: from index 1 the pair is already (1, 2), the last pair in a list of three
        Assert.True(SideBySidePair.CanNavigateForwards(0, 3, isExtracting: false));
        Assert.False(SideBySidePair.CanNavigateForwards(1, 3, isExtracting: false));
    }

    [Fact]
    public void CanNavigateForwards_SideBySideOff_CanGoToLastImage()
    {
        // Arrange
        Settings.ImageScaling.ShowImageSideBySide = false;
        Settings.UIProperties.Looping = false;

        // Act & Assert
        Assert.True(SideBySidePair.CanNavigateForwards(1, 3, isExtracting: false));
        Assert.False(SideBySidePair.CanNavigateForwards(2, 3, isExtracting: false));
    }

    [Fact]
    public void CanNavigateForwards_Looping_AlwaysAllowed()
    {
        // Arrange
        Settings.ImageScaling.ShowImageSideBySide = true;
        Settings.UIProperties.Looping = true;

        // Act & Assert
        Assert.True(SideBySidePair.CanNavigateForwards(2, 3, isExtracting: false));
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
