namespace PicView.Tests.Navigation;

/// <summary>
/// A clock that moves only when a test advances it. Its timers fire inline from <see cref="Advance"/>, once per
/// period that has elapsed.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly List<ManualTimer> _timers = [];
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    // The same clock as timestamps, for code that measures time with them
    public override long GetTimestamp() => _now.UtcTicks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period);
        _timers.Add(timer);
        return timer;
    }

    public void Advance(TimeSpan by)
    {
        var end = _now + by;
        while (true)
        {
            var next = _timers
                .Where(static t => t.Due is not null)
                .OrderBy(static t => t.Due)
                .FirstOrDefault(t => t.Due <= end);
            if (next is null)
            {
                break;
            }

            _now = next.Due!.Value;
            next.Fire();
        }

        _now = end;
    }

    private sealed class ManualTimer(ManualTimeProvider clock, TimerCallback callback, object? state) : ITimer
    {
        private TimeSpan _period;

        public DateTimeOffset? Due { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            // As the real timer does, so a test fails where production would throw, instead of looping
            ThrowIfNegative(dueTime);
            ThrowIfNegative(period);
            _period = period;
            Due = dueTime == Timeout.InfiniteTimeSpan ? null : clock._now + dueTime;
            return true;
        }

        private static void ThrowIfNegative(TimeSpan time)
        {
            if (time < TimeSpan.Zero && time != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(time), time, "A timer cannot wait a negative time.");
            }
        }

        public void Fire()
        {
            Due = _period == Timeout.InfiniteTimeSpan || _period == TimeSpan.Zero ? null : Due + _period;
            callback(state);
        }

        public void Dispose()
        {
            Due = null;
            clock._timers.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
