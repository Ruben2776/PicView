using PicView.Core.Sizing;

namespace PicView.Tests.Sizing;

public class InterfaceVisibilityTests
{
    [Theory]
    // showInterface, showBottomBar, showHoverBar, mode, interfaceShown, bottomBarShown, hoverBar
    [InlineData(true,  true,  true,  WindowMode.Normal,     true,  true,  false)]
    [InlineData(true,  true,  false, WindowMode.Normal,     true,  true,  false)]
    [InlineData(true,  false, true,  WindowMode.Normal,     true,  false, true)]
    [InlineData(true,  false, false, WindowMode.Normal,     true,  false, false)]
    [InlineData(false, true,  true,  WindowMode.Normal,     false, false, true)]
    [InlineData(false, true,  false, WindowMode.Normal,     false, false, false)]
    [InlineData(false, false, true,  WindowMode.Normal,     false, false, true)]
    [InlineData(false, false, false, WindowMode.Normal,     false, false, false)]
    [InlineData(true,  true,  true,  WindowMode.Maximized,  true,  true,  false)]
    [InlineData(true,  true,  false, WindowMode.Maximized,  true,  true,  false)]
    [InlineData(true,  false, true,  WindowMode.Maximized,  true,  false, true)]
    [InlineData(true,  false, false, WindowMode.Maximized,  true,  false, false)]
    [InlineData(false, true,  true,  WindowMode.Maximized,  false, false, true)]
    [InlineData(false, true,  false, WindowMode.Maximized,  false, false, false)]
    [InlineData(false, false, true,  WindowMode.Maximized,  false, false, true)]
    [InlineData(false, false, false, WindowMode.Maximized,  false, false, false)]
    [InlineData(true,  true,  true,  WindowMode.Fullscreen, false, false, true)]
    [InlineData(true,  true,  false, WindowMode.Fullscreen, false, false, false)]
    [InlineData(true,  false, true,  WindowMode.Fullscreen, false, false, true)]
    [InlineData(true,  false, false, WindowMode.Fullscreen, false, false, false)]
    [InlineData(false, true,  true,  WindowMode.Fullscreen, false, false, true)]
    [InlineData(false, true,  false, WindowMode.Fullscreen, false, false, false)]
    [InlineData(false, false, true,  WindowMode.Fullscreen, false, false, true)]
    [InlineData(false, false, false, WindowMode.Fullscreen, false, false, false)]
    public void For_DerivesTheBarsAndTheHoverBar(bool showInterface, bool showBottomBar, bool showHoverBar,
        WindowMode mode, bool interfaceShown, bool bottomBarShown, bool hoverBar)
    {
        var v = InterfaceVisibility.For(showInterface, showBottomBar, showHoverBar, mode);

        Assert.Equal(interfaceShown, v.IsInterfaceShown);
        Assert.Equal(bottomBarShown, v.IsBottomBarShown);
        Assert.Equal(interfaceShown ? SizeDefaults.MainTitlebarHeight : 0, v.TitlebarHeight);
        Assert.Equal(bottomBarShown ? SizeDefaults.BottombarHeight : 0, v.BottombarHeight);
        Assert.Equal(hoverBar, v.ShowsHoverBar);
    }

    [Fact]
    public void For_SameInputs_AreEqual()
    {
        Assert.Equal(InterfaceVisibility.For(true, false, true, WindowMode.Normal),
            InterfaceVisibility.For(true, false, true, WindowMode.Normal));
    }

    [Fact]
    public void Shown_Minimized_IsTheModeItComesBackTo()
    {
        var state = WindowModeState.Start(new RememberedWindowMode(true, false, false), isAppStart: true)
            .OnNativeState(WindowMode.Minimized);

        Assert.Equal(WindowMode.Minimized, state.Mode);
        Assert.Equal(WindowMode.Fullscreen, state.Shown);
    }
}
