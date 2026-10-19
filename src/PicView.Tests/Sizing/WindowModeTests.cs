using PicView.Core.Sizing;

namespace PicView.Tests.Sizing;

public class WindowModeTests
{
    [Theory]
    [InlineData(false, false, WindowMode.Normal)]
    [InlineData(false, true, WindowMode.Maximized)]
    [InlineData(true, true, WindowMode.Fullscreen)]
    [InlineData(true, false, WindowMode.Fullscreen)]
    public void From_Settings_FullscreenWinsOverMaximized(bool fullscreen, bool maximized, WindowMode expected)
    {
        Assert.Equal(expected, WindowModes.From(fullscreen, maximized));
    }

    [Theory]
    [InlineData(WindowMode.Normal, false, FitMode.FixedWindow)]
    [InlineData(WindowMode.Normal, true, FitMode.AutoFit)]
    [InlineData(WindowMode.Maximized, true, FitMode.FixedWindow)]
    [InlineData(WindowMode.Fullscreen, true, FitMode.Fullscreen)]
    public void ToFitMode_MaximizedWindow_FitsImageToItself(WindowMode mode, bool autoFit, FitMode expected)
    {
        Assert.Equal(expected, mode.ToFitMode(autoFit));
    }
}
