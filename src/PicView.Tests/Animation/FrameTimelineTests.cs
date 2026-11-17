using PicView.Core.Animation;

namespace PicView.Tests.Animation;

public class FrameTimelineTests
{
    private static TimeSpan T(int ms) => TimeSpan.FromMilliseconds(ms);

    private static FrameTimeline Create() => new([T(100), T(200), T(300)]);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(99, 0)]
    [InlineData(100, 1)]
    [InlineData(299, 1)]
    [InlineData(300, 2)]
    [InlineData(599, 2)]
    [InlineData(600, 0)]
    [InlineData(750, 1)]
    [InlineData(-1, 2)]
    public void FrameAt_Position_ReturnsFrameWrappingByTotal(int ms, int expected)
    {
        Assert.Equal(expected, Create().FrameAt(T(ms)));
    }

    [Fact]
    public void StartOf_Frame_ReturnsSumOfEarlierDurations()
    {
        var timeline = Create();
        Assert.Equal(T(0), timeline.StartOf(0));
        Assert.Equal(T(100), timeline.StartOf(1));
        Assert.Equal(T(300), timeline.StartOf(2));
    }

    [Fact]
    public void Total_ZeroAndNegativeDurations_CountAs100Ms()
    {
        var timeline = new FrameTimeline([T(0), T(-5), T(40)]);
        Assert.Equal(T(240), timeline.Total);
    }

    [Fact]
    public void FrameAt_NoFrames_ReturnsZero()
    {
        var timeline = new FrameTimeline([]);
        Assert.Equal(0, timeline.Count);
        Assert.Equal(TimeSpan.Zero, timeline.Total);
        Assert.Equal(0, timeline.FrameAt(T(500)));
    }

    [Fact]
    public void FrameAt_HoursOfPosition_StaysInsideTimeline()
    {
        var frame = Create().FrameAt(TimeSpan.FromHours(30));
        Assert.InRange(frame, 0, 2);
    }
}
