using PicView.Core.Sizing;

namespace PicView.Tests.Sizing;

public class WindowModeTests
{
    [Theory]
    [InlineData(false, false, false, WindowMode.Manual)]
    [InlineData(false, false, true, WindowMode.AutoFit)]
    [InlineData(false, true, true, WindowMode.Maximized)]
    [InlineData(true, true, true, WindowMode.Fullscreen)]
    [InlineData(true, false, false, WindowMode.Fullscreen)]
    public void From_Settings_FullscreenWinsOverMaximizedWinsOverAutoFit(bool fullscreen, bool maximized,
        bool autoFit, WindowMode expected)
    {
        Assert.Equal(expected, WindowModes.From(fullscreen, maximized, autoFit));
    }

    [Theory]
    [InlineData(WindowMode.Manual, FitMode.FixedWindow)]
    [InlineData(WindowMode.AutoFit, FitMode.AutoFit)]
    [InlineData(WindowMode.Maximized, FitMode.FixedWindow)]
    [InlineData(WindowMode.Fullscreen, FitMode.Fullscreen)]
    public void ToFitMode_MaximizedWindow_FitsImageToItself(WindowMode mode, FitMode expected)
    {
        Assert.Equal(expected, mode.ToFitMode());
    }
}
