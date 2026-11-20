using PicView.Core.IPlatform;
using PicView.Core.Navigation;
using PicView.Core.Sizing;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// The rules of one Main Window's slideshow, on a clock the test moves.
/// </summary>
[Collection("Sequential")]
public class SlideshowRulesTests
{
    private readonly ManualTimeProvider _clock = new();
    private readonly ReactiveProperty<WindowModeState> _state = new(WindowModeState.Opening(autoFit: false));
    private readonly FakeSwitcher _switcher;
    private bool _canNavigateForwards = true;
    private int _next;

    public SlideshowRulesTests()
    {
        SetDefaults();
        _switcher = new FakeSwitcher(_state);
    }

    private Slideshow Create(ScreensaverHold? hold = null) =>
        new(() => _canNavigateForwards, () =>
            {
                _next++;
                return ValueTask.CompletedTask;
            }, () => _switcher, _state, hold ?? new ScreensaverHold(null), _clock);

    [Fact]
    public void Toggle_NoNextFile_DoesNotStart()
    {
        _canNavigateForwards = false;
        var hold = new ScreensaverHold(null);
        var slideshow = Create(hold);

        slideshow.Toggle(2000, isKeyHeldDown: false);

        Assert.False(slideshow.IsRunning);
        Assert.Equal(0, _switcher.Entered);
        Assert.False(hold.IsHeld);
    }

    [Fact]
    public void Toggle_Starts_EntersSlideshowAndHoldsScreensaver()
    {
        var hold = new ScreensaverHold(null);
        var slideshow = Create(hold);

        slideshow.Toggle(2000, isKeyHeldDown: false);

        Assert.True(slideshow.IsRunning);
        Assert.Equal(1, _switcher.Entered);
        Assert.Equal(WindowMode.Fullscreen, _state.Value.Mode);
        Assert.True(hold.IsHeld);
    }

    [Fact]
    public void Tick_EachInterval_GoesNextOnce()
    {
        var slideshow = Create();
        slideshow.Toggle(2000, isKeyHeldDown: false);

        _clock.Advance(TimeSpan.FromSeconds(2));
        _clock.Advance(TimeSpan.FromSeconds(2));
        _clock.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(3, _next);
    }

    [Fact]
    public void Toggle_ZeroMilliseconds_UsesSetting()
    {
        Settings.UIProperties.SlideShowTimer = 5;
        var slideshow = Create();
        slideshow.Toggle(0, isKeyHeldDown: false);

        _clock.Advance(TimeSpan.FromSeconds(4.9));
        Assert.Equal(0, _next);
        _clock.Advance(TimeSpan.FromSeconds(0.1));
        Assert.Equal(1, _next);
    }

    [Fact]
    public void Toggle_DefaultSettings_TicksEveryFiveSeconds()
    {
        SetDefaults();
        var slideshow = Create();
        slideshow.Toggle(0, isKeyHeldDown: false);

        _clock.Advance(TimeSpan.FromSeconds(4.9));
        Assert.Equal(0, _next);
        _clock.Advance(TimeSpan.FromSeconds(0.1));
        Assert.Equal(1, _next);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Toggle_SettingZeroOrLess_TicksEverySecond(double seconds)
    {
        Settings.UIProperties.SlideShowTimer = seconds;
        var slideshow = Create();
        slideshow.Toggle(-1, isKeyHeldDown: false);

        _clock.Advance(TimeSpan.FromSeconds(0.9));
        Assert.Equal(0, _next);
        _clock.Advance(TimeSpan.FromSeconds(0.1));
        Assert.Equal(1, _next);
        _clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(2, _next);
    }

    [Fact]
    public void Toggle_WhileRunning_Stops()
    {
        var slideshow = Create();
        slideshow.Toggle(2000, isKeyHeldDown: false);

        slideshow.Toggle(2000, isKeyHeldDown: false);

        Assert.False(slideshow.IsRunning);
        Assert.Equal(1, _switcher.Left);
    }

    [Fact]
    public void Toggle_WhileRunningKeyHeld_KeepsRunning()
    {
        var slideshow = Create();
        slideshow.Toggle(2000, isKeyHeldDown: false);

        slideshow.Toggle(2000, isKeyHeldDown: true);

        Assert.True(slideshow.IsRunning);
        Assert.Equal(0, _switcher.Left);
    }

    [Fact]
    public void Toggle_WhileRunningAtLastFile_Stops()
    {
        var slideshow = Create();
        slideshow.Toggle(2000, isKeyHeldDown: false);
        _canNavigateForwards = false;

        slideshow.Toggle(2000, isKeyHeldDown: false);

        Assert.False(slideshow.IsRunning);
    }

    [Fact]
    public void Stop_CallsLeaveSlideshowOnce_AndReleasesHold()
    {
        var hold = new ScreensaverHold(null);
        var slideshow = Create(hold);
        slideshow.Toggle(2000, isKeyHeldDown: false);

        slideshow.Stop();
        slideshow.Stop();

        Assert.False(slideshow.IsRunning);
        Assert.Equal(1, _switcher.Left);
        Assert.False(hold.IsHeld);
        Assert.NotEqual(WindowMode.Fullscreen, _state.Value.Mode);
    }

    [Fact]
    public void LeavingFullscreen_Stops()
    {
        var slideshow = Create();
        slideshow.Toggle(2000, isKeyHeldDown: false);

        _state.Value = _state.Value.ToggleFullscreen();

        Assert.False(slideshow.IsRunning);
        Assert.Equal(1, _switcher.Left);
    }

    [Fact]
    public void Minimized_Stops()
    {
        var slideshow = Create();
        slideshow.Toggle(2000, isKeyHeldDown: false);

        _state.Value = _state.Value.OnNativeState(WindowMode.Minimized);

        Assert.False(slideshow.IsRunning);
        Assert.Equal(1, _switcher.Left);
        _clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(0, _next);
    }

    [Fact]
    public void Dispose_StopsTickingWithoutLeaving()
    {
        var hold = new ScreensaverHold(null);
        var slideshow = Create(hold);
        slideshow.Toggle(2000, isKeyHeldDown: false);

        slideshow.Dispose();
        slideshow.Dispose();
        _clock.Advance(TimeSpan.FromSeconds(10));

        Assert.False(slideshow.IsRunning);
        Assert.Equal(0, _next);
        Assert.Equal(0, _switcher.Left);
        Assert.False(hold.IsHeld);
    }

    [Fact]
    public void Stop_DuringEnterSlideshow_LateRunDoesNothing()
    {
        var slideshow = Create();
        _switcher.OnEnter = slideshow.Stop;

        slideshow.Toggle(2000, isKeyHeldDown: false);
        var leftByStop = _switcher.Left;
        Assert.NotEqual(WindowMode.Fullscreen, _state.Value.Mode);
        _state.Value = _state.Value.ToggleFullscreen();
        _clock.Advance(TimeSpan.FromSeconds(10));

        Assert.False(slideshow.IsRunning);
        Assert.Equal(0, _next);
        Assert.Equal(leftByStop, _switcher.Left);
    }

    [Fact]
    public void TwoSlideshows_HoldScreensaverUntilLastStops()
    {
        var platform = new CountingPlatform();
        var hold = new ScreensaverHold(platform);
        var first = Create(hold);
        // Another window, with its own Window Mode
        var otherState = new ReactiveProperty<WindowModeState>(WindowModeState.Opening(autoFit: false));
        var otherSwitcher = new FakeSwitcher(otherState);
        var second = new Slideshow(static () => true, static () => ValueTask.CompletedTask, () => otherSwitcher,
            otherState, hold, _clock);
        first.Toggle(2000, isKeyHeldDown: false);
        second.Toggle(2000, isKeyHeldDown: false);

        first.Stop();
        Assert.Equal(0, platform.Enabled);
        second.Stop();

        Assert.Equal(1, platform.Disabled);
        Assert.Equal(1, platform.Enabled);
    }

    private sealed class FakeSwitcher(ReactiveProperty<WindowModeState> state) : IWindowModeSwitcher
    {
        public int Entered { get; private set; }
        public int Left { get; private set; }

        /// <summary>Runs inside <see cref="EnterSlideshow"/>, before the window goes fullscreen.</summary>
        public Action? OnEnter { get; set; }

        public void EnterSlideshow()
        {
            Entered++;
            OnEnter?.Invoke();
            state.Value = state.Value.EnterSlideshow();
        }

        public void LeaveSlideshow()
        {
            Left++;
            state.Value = state.Value.LeaveSlideshow();
        }

        public void ToggleFullscreen() { }
        public void ToggleMaximized() { }
        public void LeaveTopMode() { }
        public void SetAutoFit(bool autoFit) { }
        public void ResizedByHand() { }
    }
}
