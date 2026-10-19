using PicView.Core.Sizing;

namespace PicView.Tests.Sizing;

public class WindowModeStateTests
{
    private static WindowModeState Start(bool fullscreen = false, bool maximized = false, bool autoFit = false,
        bool isAppStart = true) =>
        WindowModeState.Start(new RememberedWindowMode(fullscreen, maximized, autoFit), isAppStart);

    [Theory]
    [InlineData(false, false, WindowMode.Normal)]
    [InlineData(false, true, WindowMode.Maximized)]
    [InlineData(true, false, WindowMode.Fullscreen)]
    public void Start_AtAppStart_StartsInTheRememberedMode(bool fullscreen, bool maximized, WindowMode expected)
    {
        Assert.Equal(expected, Start(fullscreen, maximized).Mode);
    }

    [Fact]
    public void Start_WindowOpenedWhileRunning_NeverStartsFullscreen()
    {
        var state = Start(fullscreen: true, isAppStart: false);

        Assert.Equal(WindowMode.Normal, state.Mode);
    }

    [Fact]
    public void Start_RememberedAutoFit_KeepsItBeneathMaximized()
    {
        var state = Start(maximized: true, autoFit: true);

        Assert.Equal(WindowMode.Maximized, state.Mode);
        Assert.True(state.AutoFit);
        Assert.Equal(FitMode.FixedWindow, state.FitMode);
        Assert.Equal(FitMode.AutoFit, state.ToggleMaximized().FitMode);
    }

    [Fact]
    public void Start_InFullscreen_LeavingGivesBackNormal()
    {
        var state = Start(fullscreen: true).ToggleFullscreen();

        Assert.Equal(WindowMode.Normal, state.Mode);
    }

    [Fact]
    public void ToggleFullscreen_FromMaximized_LeavingGivesBackMaximized()
    {
        var state = Start(maximized: true).ToggleFullscreen();
        Assert.Equal(WindowMode.Fullscreen, state.Mode);

        Assert.Equal(WindowMode.Maximized, state.ToggleFullscreen().Mode);
    }

    [Fact]
    public void ToggleMaximized_TogglesBetweenNormalAndMaximized()
    {
        var maximized = Start().ToggleMaximized();

        Assert.Equal(WindowMode.Maximized, maximized.Mode);
        Assert.Equal(WindowMode.Normal, maximized.ToggleMaximized().Mode);
    }

    [Fact]
    public void ToggleMaximized_FromFullscreen_Maximizes()
    {
        var state = Start(fullscreen: true).ToggleMaximized();

        Assert.Equal(WindowMode.Maximized, state.Mode);
    }

    [Fact]
    public void LeaveTopMode_LeavesOneModeAtATime()
    {
        var fullscreen = Start(maximized: true).ToggleFullscreen();

        var maximized = fullscreen.LeaveTopMode();
        var normal = maximized.LeaveTopMode();

        Assert.Equal(WindowMode.Maximized, maximized.Mode);
        Assert.Equal(WindowMode.Normal, normal.Mode);
        Assert.Equal(normal, normal.LeaveTopMode());
    }

    [Fact]
    public void Remembered_NeverHoldsFullscreenAndMaximizedTogether()
    {
        var state = Start(maximized: true).ToggleFullscreen();

        Assert.Equal(new RememberedWindowMode(Fullscreen: true, Maximized: false, AutoFit: false), state.Remembered);
    }

    [Fact]
    public void OnNativeState_Minimized_IsNotRemembered()
    {
        var maximized = Start(maximized: true);

        var minimized = maximized.OnNativeState(WindowMode.Minimized);

        Assert.Equal(WindowMode.Minimized, minimized.Mode);
        Assert.Equal(maximized.Remembered, minimized.Remembered);
    }

    [Theory]
    [InlineData(WindowMode.Normal)]
    [InlineData(WindowMode.Maximized)]
    [InlineData(WindowMode.Fullscreen)]
    public void OnNativeState_LeavingMinimized_GivesBackTheModeBefore(WindowMode restoredTo)
    {
        var fullscreen = Start(maximized: true).ToggleFullscreen();

        var restored = fullscreen.OnNativeState(WindowMode.Minimized).OnNativeState(restoredTo);

        Assert.Equal(fullscreen, restored);
        Assert.Equal(WindowMode.Maximized, restored.ToggleFullscreen().Mode);
    }

    [Fact]
    public void OnNativeState_MaximizedWhileFullscreen_IsIgnored()
    {
        var fullscreen = Start(fullscreen: true);

        Assert.Equal(fullscreen, fullscreen.OnNativeState(WindowMode.Maximized));
    }

    [Fact]
    public void OnNativeState_NormalWhileFullscreen_LeavesBothFullscreenAndMaximized()
    {
        var state = Start(maximized: true).ToggleFullscreen().OnNativeState(WindowMode.Normal);

        Assert.Equal(WindowMode.Normal, state.Mode);
    }

    [Fact]
    public void OnNativeState_Fullscreen_IsRemembered()
    {
        var state = Start(maximized: true).OnNativeState(WindowMode.Fullscreen);

        Assert.Equal(WindowMode.Fullscreen, state.Mode);
        Assert.True(state.Remembered.Fullscreen);
        Assert.Equal(WindowMode.Maximized, state.LeaveTopMode().Mode);
    }

    [Fact]
    public void OnNativeState_SameMode_ChangesNothing()
    {
        var state = Start(maximized: true);

        Assert.Equal(state, state.OnNativeState(WindowMode.Maximized));
    }

    [Fact]
    public void Slideshow_FromMaximized_IsNotRememberedAndGivesBackMaximized()
    {
        var maximized = Start(maximized: true);

        var slideshow = maximized.EnterSlideshow();

        Assert.Equal(WindowMode.Fullscreen, slideshow.Mode);
        Assert.Equal(maximized.Remembered, slideshow.Remembered);
        Assert.Equal(maximized, slideshow.LeaveSlideshow());
    }

    [Fact]
    public void Slideshow_InFullscreen_StaysFullscreenAfterwards()
    {
        var fullscreen = Start(fullscreen: true);

        var afterSlideshow = fullscreen.EnterSlideshow().LeaveSlideshow();

        Assert.Equal(fullscreen, afterSlideshow);
    }

    [Fact]
    public void Slideshow_UserLeftFullscreenDuringIt_StoppingChangesNothing()
    {
        var left = Start(maximized: true).EnterSlideshow().ToggleFullscreen();

        Assert.Equal(WindowMode.Maximized, left.Mode);
        Assert.Equal(left, left.LeaveSlideshow());
    }

    [Fact]
    public void Slideshow_StoppedWhileMinimized_GivesBackTheModeBeforeIt()
    {
        var state = Start(maximized: true).EnterSlideshow()
            .OnNativeState(WindowMode.Minimized)
            .LeaveSlideshow()
            .OnNativeState(WindowMode.Normal);

        Assert.Equal(WindowMode.Maximized, state.Mode);
    }

    [Fact]
    public void ResizedByHand_NormalWindow_TurnsAutoFitOff()
    {
        var state = Start(autoFit: true).ResizedByHand();

        Assert.False(state.AutoFit);
        Assert.False(state.Remembered.AutoFit);
    }

    [Fact]
    public void ResizedByHand_MaximizedWindow_KeepsAutoFit()
    {
        var maximized = Start(maximized: true, autoFit: true);

        Assert.Equal(maximized, maximized.ResizedByHand());
    }

    [Fact]
    public void SetAutoFit_WhileMaximized_AppliesOnceNormalAgain()
    {
        var state = Start(maximized: true).SetAutoFit(true);

        Assert.Equal(WindowMode.Maximized, state.Mode);
        Assert.True(state.Remembered.AutoFit);
        Assert.Equal(FitMode.AutoFit, state.LeaveTopMode().FitMode);
    }
}
