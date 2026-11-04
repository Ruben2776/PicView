using PicView.Core.Sizing;

namespace PicView.Tests.Sizing;

public class ReservedSpaceTests
{
    private const double Titlebar = SizeDefaults.MainTitlebarHeight;

    private static InterfaceVisibility Bars(bool showInterface, bool showBottomBar) =>
        InterfaceVisibility.For(showInterface, showBottomBar, true, WindowMode.Normal);

    [Fact]
    public void For_OneTabWithTitlebar_ReservesTheTitlebar()
    {
        var space = ReservedSpace.For(1, Bars(showInterface: true, showBottomBar: false), 0, 0);

        Assert.Equal(Titlebar + 2, space.Top);
        Assert.Equal(0, space.Bottom);
    }

    [Fact]
    public void For_SeveralTabsWithTheTitlebarHidden_ReservesTheTabStripWithoutTheTitlebar()
    {
        // The hidden titlebar is collapsed to no height, so the strip sits where it was
        var space = ReservedSpace.For(2, Bars(showInterface: false, showBottomBar: false), 0, 0);

        Assert.Equal(SizeDefaults.TabHeight + 2, space.Top);
    }

    [Fact]
    public void For_OneTabWithTitlebarHidden_ReservesNothingAbove()
    {
        var space = ReservedSpace.For(1, Bars(showInterface: false, showBottomBar: false), 0, 0);

        Assert.Equal(0, space.Top);
    }

    [Fact]
    public void For_BottomBarAndDockedGallery_ReservesThem()
    {
        var space = ReservedSpace.For(1, Bars(showInterface: true, showBottomBar: true), 120, 90);

        Assert.Equal(SizeDefaults.BottombarHeight, space.Bottom);
        Assert.Equal(120, space.GalleryWidth);
        Assert.Equal(90, space.GalleryHeight);
    }
}
