using Avalonia.Threading;
using PicView.Core.Gallery;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// The timer that tells <see cref="NavigateAbleItemsViewer"/> its view has rested. In the app it is a
/// <see cref="DispatcherTileRestTimer"/>; a test gives its own, since the headless dispatcher runs no timers.
/// </summary>
internal interface ITileRestTimer
{
    bool IsEnabled { get; }

    /// <summary>Starts the timer; does nothing while it runs.</summary>
    void Start();

    void Stop();
}

/// <summary>
/// A <see cref="DispatcherTimer"/> of <see cref="GalleryDefaults.TileSharpenDelayMilliseconds"/> that calls back on
/// each tick.
/// </summary>
internal sealed class DispatcherTileRestTimer : ITileRestTimer
{
    private readonly DispatcherTimer _timer;

    public DispatcherTileRestTimer(Action elapsed)
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(GalleryDefaults.TileSharpenDelayMilliseconds)
        };
        _timer.Tick += (_, _) => elapsed();
    }

    public TimeSpan Interval => _timer.Interval;

    public bool IsEnabled => _timer.IsEnabled;

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();
}
