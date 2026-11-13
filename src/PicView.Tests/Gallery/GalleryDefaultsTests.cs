using PicView.Core.Gallery;
using PicView.Core.Sizing;

namespace PicView.Tests.Gallery;

public class GalleryDefaultsTests
{
    [Fact]
    public void GetDockedGalleryHeight_TopOrBottomDock_AddsHorizontalScrollbarAndTopBottomBorder()
    {
        // A gallery docked top or bottom scrolls horizontally, so its height holds the horizontal scrollbar.
        // Matches 5.1.2: 76 + 2 + 22 = 100, leaving the items presenter exactly the item height.
        Assert.Equal(100, GalleryDefaults.GetDockedGalleryHeight(GalleryDefaults.DefaultDockedGalleryHeight));
        Assert.Equal(
            50 + GalleryDefaults.BorderTopAndBottomThickness + SizeDefaults.HorizontalScrollbarSize,
            GalleryDefaults.GetDockedGalleryHeight(50));
    }

    [Fact]
    public void GetDockedGalleryWidth_LeftOrRightDock_AddsVerticalScrollbarAndSideBorder()
    {
        // A gallery docked left or right scrolls vertically, so its width holds the vertical scrollbar.
        Assert.Equal(107, GalleryDefaults.GetDockedGalleryWidth(GalleryDefaults.DefaultDockedGalleryHeight));
        Assert.Equal(
            50 + GalleryDefaults.BorderSideThickness + SizeDefaults.VerticalScrollbarSize,
            GalleryDefaults.GetDockedGalleryWidth(50));
    }
}
