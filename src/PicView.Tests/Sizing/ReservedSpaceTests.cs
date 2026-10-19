using PicView.Core.Sizing;

namespace PicView.Tests.Sizing;

public class ReservedSpaceTests
{
    private const double Titlebar = SizeDefaults.MainTitlebarHeight;

    [Fact]
    public void For_OneTabWithTitlebar_ReservesTheTitlebar()
    {
        var space = ReservedSpace.For(1, Titlebar, isTopToolbarShown: true, showBottomBar: false, 0, 0);

        Assert.Equal(Titlebar + 2, space.Top);
        Assert.Equal(0, space.Bottom);
    }

    [Fact]
    public void For_SeveralTabs_ReservesTheTabStripAndTitlebarEvenWithTheTitlebarHidden()
    {
        var space = ReservedSpace.For(2, Titlebar, isTopToolbarShown: false, showBottomBar: false, 0, 0);

        Assert.Equal(SizeDefaults.TabHeight + Titlebar + 2, space.Top);
    }

    [Fact]
    public void For_OneTabWithTitlebarHidden_ReservesNothingAbove()
    {
        var space = ReservedSpace.For(1, Titlebar, isTopToolbarShown: false, showBottomBar: false, 0, 0);

        Assert.Equal(0, space.Top);
    }

    [Fact]
    public void For_BottomBarAndDockedGallery_ReservesThem()
    {
        var space = ReservedSpace.For(1, Titlebar, isTopToolbarShown: true, showBottomBar: true, 120, 90);

        Assert.Equal(SizeDefaults.BottombarHeight, space.Bottom);
        Assert.Equal(120, space.GalleryWidth);
        Assert.Equal(90, space.GalleryHeight);
    }
}
