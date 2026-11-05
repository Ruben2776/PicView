using PicView.Core.Gallery;

namespace PicView.Tests.Gallery;

public class GalleryTransitionTests
{
    [Theory]
    [InlineData(GalleryMode.Closed, GalleryMode.Docked, GalleryDefaults.FastAnimationSpeed, false, false, false)]
    [InlineData(GalleryMode.Docked, GalleryMode.Closed, GalleryDefaults.MediumAnimationSpeed, false, false, false)]
    [InlineData(GalleryMode.Docked, GalleryMode.Expanded, GalleryDefaults.MediumAnimationSpeed, false, false, true)]
    [InlineData(GalleryMode.Expanded, GalleryMode.Docked, GalleryDefaults.SlowAnimationSpeed, false, true, true)]
    [InlineData(GalleryMode.Closed, GalleryMode.Expanded, GalleryDefaults.MediumAnimationSpeed, true, false, true)]
    [InlineData(GalleryMode.Expanded, GalleryMode.Closed, GalleryDefaults.FastAnimationSpeed, true, false, true)]
    public void For_EachModeChange_IsTheSpecsRow(
        GalleryMode from, GalleryMode to, double speed, bool tweensBothAxes, bool appliesLookAtEnd,
        bool locksWrap)
    {
        var transition = GalleryTransition.For(from, to);

        Assert.Equal(new GalleryTransition(speed, tweensBothAxes, appliesLookAtEnd, locksWrap), transition);
    }

    [Theory]
    [InlineData(GalleryMode.Closed)]
    [InlineData(GalleryMode.Docked)]
    [InlineData(GalleryMode.Expanded)]
    public void For_SameMode_IsNull(GalleryMode mode)
    {
        Assert.Null(GalleryTransition.For(mode, mode));
    }
}
