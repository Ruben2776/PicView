using PicView.Core.ImageDecoding;

namespace PicView.Core.Animation;

/// <summary>
///     Playback state of one animated image: position, pause and speed. Animations always loop.
///     Safe to call <see cref="Advance" /> from the compositor thread while the UI thread changes state.
/// </summary>
public sealed class AnimationPlayback(FileInfo file, ImageType imageType)
{
    public const double MinSpeed = 0.25;
    public const double MaxSpeed = 4.0;

    private readonly Lock _lock = new();
    private bool _isPaused;
    private long _positionTicks;
    private double _speed = 1.0;
    private FrameTimeline? _timeline;

    public FileInfo File { get; } = file;

    public ImageType ImageType { get; } = imageType;

    public bool IsPaused
    {
        get
        {
            lock (_lock)
            {
                return _isPaused;
            }
        }
    }

    public double Speed
    {
        get
        {
            lock (_lock)
            {
                return _speed;
            }
        }
    }

    public FrameTimeline? Timeline
    {
        get
        {
            lock (_lock)
            {
                return _timeline;
            }
        }
    }

    public int CurrentFrame
    {
        get
        {
            lock (_lock)
            {
                return _timeline?.FrameAt(TimeSpan.FromTicks(_positionTicks)) ?? 0;
            }
        }
    }

    public void Attach(FrameTimeline timeline)
    {
        lock (_lock)
        {
            _timeline = timeline;
            _positionTicks = 0;
        }
    }

    /// <summary>Moves the position on by <paramref name="elapsed" /> (scaled by speed) unless paused; returns the frame to show.</summary>
    public int Advance(TimeSpan elapsed)
    {
        lock (_lock)
        {
            if (_timeline is null || _timeline.Count == 0)
            {
                return 0;
            }

            if (!_isPaused)
            {
                var total = _timeline.Total.Ticks;
                var delta = (long)((elapsed.Ticks * _speed) % total);
                var position = (_positionTicks + delta) % total;
                _positionTicks = position < 0 ? position + total : position;
            }

            return _timeline.FrameAt(TimeSpan.FromTicks(_positionTicks));
        }
    }

    public void TogglePause()
    {
        lock (_lock)
        {
            _isPaused = !_isPaused;
        }
    }

    public void SetPaused(bool paused)
    {
        lock (_lock)
        {
            _isPaused = paused;
        }
    }

    /// <summary>Pauses and moves to the start of the frame <paramref name="frames" /> away, wrapping at either end.</summary>
    public void Step(int frames)
    {
        lock (_lock)
        {
            _isPaused = true;
            if (_timeline is null || _timeline.Count == 0)
            {
                return;
            }

            var count = _timeline.Count;
            var current = _timeline.FrameAt(TimeSpan.FromTicks(_positionTicks));
            var target = (int)(((current + (long)frames) % count + count) % count);
            _positionTicks = _timeline.StartOf(target).Ticks;
        }
    }

    public void SetSpeed(double speed)
    {
        if (double.IsNaN(speed))
        {
            return;
        }

        lock (_lock)
        {
            _speed = Math.Clamp(speed, MinSpeed, MaxSpeed);
        }
    }
}
