using PicView.Core.Gallery;

namespace PicView.Tests.Gallery;

public class GalleryTransitionTests
{
    [Theory]
    [InlineData(GalleryMode.Closed, GalleryMode.Docked, GalleryDefaults.FastAnimationSpeed, false, false, false, true, false)]
    [InlineData(GalleryMode.Docked, GalleryMode.Closed, GalleryDefaults.MediumAnimationSpeed, false, false, false, false, false)]
    [InlineData(GalleryMode.Docked, GalleryMode.Expanded, GalleryDefaults.MediumAnimationSpeed, false, false, true, false, false)]
    [InlineData(GalleryMode.Expanded, GalleryMode.Docked, GalleryDefaults.SlowAnimationSpeed, false, true, true, true, false)]
    [InlineData(GalleryMode.Closed, GalleryMode.Expanded, GalleryDefaults.MediumAnimationSpeed, true, false, true, false, false)]
    [InlineData(GalleryMode.Expanded, GalleryMode.Closed, GalleryDefaults.FastAnimationSpeed, true, false, true, true, false)]
    public void For_EachModeChange_IsTheSpecsRow(
        GalleryMode from, GalleryMode to, double speed, bool tweensBothAxes, bool appliesLookAtEnd,
        bool locksWrap, bool refitsAtEnd, bool refitsEveryFrame)
    {
        var transition = GalleryTransition.For(from, to, GalleryDockPosition.Bottom, true);

        Assert.Equal(
            new GalleryTransition(speed, tweensBothAxes, appliesLookAtEnd, locksWrap, refitsAtEnd, refitsEveryFrame),
            transition);
    }

    [Theory]
    [InlineData(GalleryDockPosition.Top, true, false)]
    [InlineData(GalleryDockPosition.Top, false, true)]
    [InlineData(GalleryDockPosition.Bottom, true, false)]
    [InlineData(GalleryDockPosition.Bottom, false, true)]
    [InlineData(GalleryDockPosition.Left, true, false)]
    [InlineData(GalleryDockPosition.Left, false, false)]
    [InlineData(GalleryDockPosition.Right, true, false)]
    [InlineData(GalleryDockPosition.Right, false, false)]
    public void For_ExpandedToDocked_RefitsEveryFrameOnlyAtTopOrBottomWithoutAutoFit(
        GalleryDockPosition dock, bool autoFit, bool expected)
    {
        var transition = GalleryTransition.For(GalleryMode.Expanded, GalleryMode.Docked, dock, autoFit);

        Assert.Equal(expected, transition!.Value.RefitsEveryFrame);
    }

    [Theory]
    [InlineData(GalleryMode.Closed)]
    [InlineData(GalleryMode.Docked)]
    [InlineData(GalleryMode.Expanded)]
    public void For_SameMode_IsNull(GalleryMode mode)
    {
        Assert.Null(GalleryTransition.For(mode, mode, GalleryDockPosition.Bottom, true));
    }
}
