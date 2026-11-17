namespace PicView.Core.Animation;

/// <summary>
///     The frame durations of an animation, as cumulative end times. Maps a playback position to a frame.
/// </summary>
public sealed class FrameTimeline
{
    public static readonly TimeSpan DefaultFrameDuration = TimeSpan.FromMilliseconds(100);

    private readonly long[] _endTicks;

    public FrameTimeline(IReadOnlyList<TimeSpan> durations)
    {
        _endTicks = new long[durations.Count];
        long sum = 0;
        for (var i = 0; i < durations.Count; i++)
        {
            var duration = durations[i] > TimeSpan.Zero ? durations[i] : DefaultFrameDuration;
            sum += duration.Ticks;
            _endTicks[i] = sum;
        }

        Total = TimeSpan.FromTicks(sum);
    }

    public int Count => _endTicks.Length;

    /// <summary>Zero only when <see cref="Count" /> is 0.</summary>
    public TimeSpan Total { get; }

    /// <summary>Returns the frame shown at <paramref name="position" />, wrapping by <see cref="Total" />.</summary>
    public int FrameAt(TimeSpan position)
    {
        if (_endTicks.Length == 0)
        {
            return 0;
        }

        var total = Total.Ticks;
        var ticks = position.Ticks % total;
        if (ticks < 0)
        {
            ticks += total;
        }

        // First frame whose end is after ticks
        var low = 0;
        var high = _endTicks.Length - 1;
        while (low < high)
        {
            var mid = low + (high - low) / 2;
            if (_endTicks[mid] > ticks)
            {
                high = mid;
            }
            else
            {
                low = mid + 1;
            }
        }

        return low;
    }

    public TimeSpan StartOf(int frame)
    {
        if (frame <= 0 || _endTicks.Length == 0)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromTicks(_endTicks[Math.Min(frame, _endTicks.Length) - 1]);
    }
}
