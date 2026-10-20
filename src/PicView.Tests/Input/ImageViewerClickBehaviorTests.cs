using PicView.Avalonia.Input;

namespace PicView.Tests.Input;

public class ImageViewerClickBehaviorTests
{
    [Theory]
    [InlineData(0, true, DoubleClickAction.None)]
    [InlineData(1, true, DoubleClickAction.ResetZoom)]
    [InlineData(2, true, DoubleClickAction.ToggleFullscreen)]
    [InlineData(0, false, DoubleClickAction.None)]
    [InlineData(1, false, DoubleClickAction.None)]
    [InlineData(2, false, DoubleClickAction.ToggleFullscreen)]
    internal void DoubleClickActionFor_SettingAndView_FollowsTheSetting(int doubleClickBehavior, bool canResetZoom,
        DoubleClickAction expected)
    {
        Assert.Equal(expected, ImageViewerClickBehavior.DoubleClickActionFor(doubleClickBehavior, canResetZoom));
    }
}
