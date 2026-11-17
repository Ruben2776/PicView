using PicView.Core.Animation;
using PicView.Core.ImageDecoding;

namespace PicView.Tests.Animation;

public class AnimationPlaybackTests
{
    private static TimeSpan T(int ms) => TimeSpan.FromMilliseconds(ms);

    private static FrameTimeline Timeline() => new([T(100), T(200), T(300)]);

    private static AnimationPlayback Create(bool attach = true)
    {
        var playback = new AnimationPlayback(new FileInfo("a.gif"), ImageType.AnimatedGif);
        if (attach)
        {
            playback.Attach(Timeline());
        }

        return playback;
    }

    [Fact]
    public void Advance_WhilePlaying_MovesByElapsed()
    {
        var playback = Create();
        Assert.Equal(1, playback.Advance(T(150)));
        Assert.Equal(2, playback.Advance(T(200)));
    }

    [Fact]
    public void Advance_WhilePaused_KeepsFrame()
    {
        var playback = Create();
        playback.Advance(T(150));
        playback.TogglePause();
        Assert.Equal(1, playback.Advance(T(500)));
    }

    [Fact]
    public void Advance_AtSpeedTwo_MovesTwiceAsFar()
    {
        var playback = Create();
        playback.SetSpeed(2);
        Assert.Equal(1, playback.Advance(T(50)));
    }

    [Fact]
    public void Advance_BeforeAttach_ReturnsZero()
    {
        Assert.Equal(0, Create(false).Advance(T(500)));
    }

    [Fact]
    public void Advance_NegativeOrHugeElapsed_StaysInsideTimeline()
    {
        var playback = Create();
        Assert.InRange(playback.Advance(T(-50)), 0, 2);
        Assert.InRange(playback.Advance(TimeSpan.FromHours(30)), 0, 2);
    }

    [Fact]
    public void Advance_AllocatesNothing()
    {
        var playback = Create();
        playback.Advance(T(16));
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            playback.Advance(T(16));
        }

        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    [Fact]
    public void Step_Forward_PausesAndMovesToStartOfNextFrame()
    {
        var playback = Create();
        playback.Advance(T(150));
        playback.Step(1);
        Assert.True(playback.IsPaused);
        Assert.Equal(2, playback.CurrentFrame);
        Assert.Equal(2, playback.Advance(T(0)));
    }

    [Fact]
    public void Step_PastEitherEnd_Wraps()
    {
        var playback = Create();
        playback.Advance(T(300));
        Assert.Equal(2, playback.CurrentFrame);
        playback.Step(1);
        Assert.Equal(0, playback.CurrentFrame);
        playback.Step(-1);
        Assert.Equal(2, playback.CurrentFrame);
    }

    [Fact]
    public void Step_BeforeAttach_PausesAndPauseHoldsAfterAttach()
    {
        var playback = Create(false);
        playback.Step(1);
        playback.Attach(Timeline());
        Assert.Equal(0, playback.Advance(T(500)));
    }

    [Fact]
    public void SetSpeed_OutOfRange_Clamps()
    {
        var playback = Create();
        playback.SetSpeed(0);
        Assert.Equal(0.25, playback.Speed);
        playback.SetSpeed(10);
        Assert.Equal(4.0, playback.Speed);
        playback.SetSpeed(double.NaN);
        Assert.Equal(4.0, playback.Speed);
    }

    [Fact]
    public void TogglePause_Twice_ResumesFromSamePosition()
    {
        var playback = Create();
        playback.Advance(T(150));
        playback.TogglePause();
        playback.TogglePause();
        Assert.False(playback.IsPaused);
        Assert.Equal(1, playback.CurrentFrame);
        Assert.Equal(1, playback.Advance(T(10)));
    }
}
