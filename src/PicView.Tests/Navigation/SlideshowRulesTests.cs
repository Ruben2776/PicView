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
    public async Task Toggle_NoNextFile_DoesNotStart()
    {
        _canNavigateForwards = false;
        var hold = new ScreensaverHold(null);
        var slideshow = Create(hold);

        await slideshow.Toggle(2000, isKeyHeldDown: false);

        Assert.False(slideshow.IsRunning);
        Assert.Equal(0, _switcher.Entered);
        Assert.False(hold.IsHeld);
    }

    [Fact]
    public async Task Toggle_Starts_EntersSlideshowAndHoldsScreensaver()
    {
        var hold = new ScreensaverHold(null);
        var slideshow = Create(hold);

        await slideshow.Toggle(2000, isKeyHeldDown: false);

        Assert.True(slideshow.IsRunning);
        Assert.Equal(1, _switcher.Entered);
        Assert.Equal(WindowMode.Fullscreen, _state.Value.Mode);
        Assert.True(hold.IsHeld);
    }

    [Fact]
    public async Task Tick_EachInterval_GoesNextOnce()
    {
        var slideshow = Create();
        await slideshow.Toggle(2000, isKeyHeldDown: false);

        _clock.Advance(TimeSpan.FromSeconds(2));
        _clock.Advance(TimeSpan.FromSeconds(2));
        _clock.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(3, _next);
    }

    [Fact]
    public async Task Toggle_ZeroMilliseconds_UsesSetting()
    {
        Settings.UIProperties.SlideShowTimer = 5;
        var slideshow = Create();
        await slideshow.Toggle(0, isKeyHeldDown: false);

        _clock.Advance(TimeSpan.FromSeconds(4.9));
        Assert.Equal(0, _next);
        _clock.Advance(TimeSpan.FromSeconds(0.1));
        Assert.Equal(1, _next);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Toggle_SettingZeroOrLess_TicksEverySecond(double seconds)
    {
        Settings.UIProperties.SlideShowTimer = seconds;
        var slideshow = Create();
        await slideshow.Toggle(-1, isKeyHeldDown: false);

        _clock.Advance(TimeSpan.FromSeconds(0.9));
        Assert.Equal(0, _next);
        _clock.Advance(TimeSpan.FromSeconds(0.1));
        Assert.Equal(1, _next);
        _clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(2, _next);
    }

    [Fact]
    public async Task Toggle_WhileRunning_Stops()
    {
        var slideshow = Create();
        await slideshow.Toggle(2000, isKeyHeldDown: false);

        await slideshow.Toggle(2000, isKeyHeldDown: false);

        Assert.False(slideshow.IsRunning);
        Assert.Equal(1, _switcher.Left);
    }

    [Fact]
    public async Task Toggle_WhileRunningKeyHeld_KeepsRunning()
    {
        var slideshow = Create();
        await slideshow.Toggle(2000, isKeyHeldDown: false);

        await slideshow.Toggle(2000, isKeyHeldDown: true);

        Assert.True(slideshow.IsRunning);
        Assert.Equal(0, _switcher.Left);
    }

    [Fact]
    public async Task Toggle_WhileRunningAtLastFile_Stops()
    {
        var slideshow = Create();
        await slideshow.Toggle(2000, isKeyHeldDown: false);
        _canNavigateForwards = false;

        await slideshow.Toggle(2000, isKeyHeldDown: false);

        Assert.False(slideshow.IsRunning);
    }

    [Fact]
    public async Task Stop_CallsLeaveSlideshowOnce_AndReleasesHold()
    {
        var hold = new ScreensaverHold(null);
        var slideshow = Create(hold);
        await slideshow.Toggle(2000, isKeyHeldDown: false);

        slideshow.Stop();
        slideshow.Stop();

        Assert.False(slideshow.IsRunning);
        Assert.Equal(1, _switcher.Left);
        Assert.False(hold.IsHeld);
        Assert.NotEqual(WindowMode.Fullscreen, _state.Value.Mode);
    }

    [Fact]
    public async Task LeavingFullscreen_Stops()
    {
        var slideshow = Create();
        await slideshow.Toggle(2000, isKeyHeldDown: false);

        _state.Value = _state.Value.ToggleFullscreen();

        Assert.False(slideshow.IsRunning);
        Assert.Equal(1, _switcher.Left);
    }

    [Fact]
    public async Task Minimized_Stops()
    {
        var slideshow = Create();
        await slideshow.Toggle(2000, isKeyHeldDown: false);

        _state.Value = _state.Value.OnNativeState(WindowMode.Minimized);

        Assert.False(slideshow.IsRunning);
        Assert.Equal(1, _switcher.Left);
        _clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(0, _next);
    }

    [Fact]
    public async Task Dispose_StopsTickingWithoutLeaving()
    {
        var hold = new ScreensaverHold(null);
        var slideshow = Create(hold);
        await slideshow.Toggle(2000, isKeyHeldDown: false);

        slideshow.Dispose();
        slideshow.Dispose();
        _clock.Advance(TimeSpan.FromSeconds(10));

        Assert.False(slideshow.IsRunning);
        Assert.Equal(0, _next);
        Assert.Equal(0, _switcher.Left);
        Assert.False(hold.IsHeld);
    }

    [Fact]
    public async Task Stop_DuringEnterSlideshow_LateRunDoesNothing()
    {
        var slideshow = Create();
        var entering = new TaskCompletionSource();
        _switcher.HoldEnter = entering.Task;

        var toggle = slideshow.Toggle(2000, isKeyHeldDown: false);
        slideshow.Stop();
        entering.SetResult();
        await toggle;
        var leftByStop = _switcher.Left;
        Assert.NotEqual(WindowMode.Fullscreen, _state.Value.Mode);
        _state.Value = _state.Value.ToggleFullscreen();
        _clock.Advance(TimeSpan.FromSeconds(10));

        Assert.False(slideshow.IsRunning);
        Assert.Equal(0, _next);
        Assert.Equal(leftByStop, _switcher.Left);
    }

    [Fact]
    public async Task TwoSlideshows_HoldScreensaverUntilLastStops()
    {
        var platform = new CountingPlatform();
        var hold = new ScreensaverHold(platform);
        var first = Create(hold);
        // Another window, with its own Window Mode
        var otherState = new ReactiveProperty<WindowModeState>(WindowModeState.Opening(autoFit: false));
        var otherSwitcher = new FakeSwitcher(otherState);
        var second = new Slideshow(static () => true, static () => ValueTask.CompletedTask, () => otherSwitcher,
            otherState, hold, _clock);
        await first.Toggle(2000, isKeyHeldDown: false);
        await second.Toggle(2000, isKeyHeldDown: false);

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
        public Task? HoldEnter { get; set; }

        public async ValueTask EnterSlideshow()
        {
            Entered++;
            if (HoldEnter is not null)
            {
                await HoldEnter.ConfigureAwait(false);
            }

            state.Value = state.Value.EnterSlideshow();
        }

        public ValueTask LeaveSlideshow()
        {
            Left++;
            state.Value = state.Value.LeaveSlideshow();
            return ValueTask.CompletedTask;
        }

        public ValueTask ToggleFullscreen() => ValueTask.CompletedTask;
        public ValueTask ToggleMaximized() => ValueTask.CompletedTask;
        public ValueTask LeaveTopMode() => ValueTask.CompletedTask;
        public ValueTask SetAutoFit(bool autoFit) => ValueTask.CompletedTask;
        public ValueTask ResizedByHand() => ValueTask.CompletedTask;
    }
}
