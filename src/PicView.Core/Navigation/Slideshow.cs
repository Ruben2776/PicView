using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.Sizing;
using R3;

namespace PicView.Core.Navigation;

/// <summary>
/// One Main Window's slideshow: it goes to the next file on each tick, shows its window fullscreen while it runs, and
/// keeps the screensaver off. It puts only its own window in the Slideshow Input Mode, and ends when the window
/// closes. Call on the UI thread; ticks are posted back to the thread it was started on.
/// </summary>
/// <param name="canNavigateForwards">Whether the window's active tab has a next file to go to.</param>
/// <param name="next">Goes to the next file in the window's active tab.</param>
/// <param name="switcher">The window's Window Mode switcher, which is set after the window is created.</param>
/// <param name="windowModeState">The window's Window Mode, to stop when it leaves the slideshow's fullscreen.</param>
/// <param name="screensaver">The hold every Main Window shares.</param>
/// <param name="time">The clock the slideshow ticks on.</param>
public sealed class Slideshow(
    Func<bool> canNavigateForwards,
    Func<ValueTask> next,
    Func<IWindowModeSwitcher?> switcher,
    Observable<WindowModeState> windowModeState,
    ScreensaverHold screensaver,
    TimeProvider time) : IDisposable
{
    private const double MinimumMilliseconds = 1000;

    // Each start and each end moves it on, so a late tick or subscription of an ended run does nothing
    private int _run;
    private ITimer? _timer;
    private IDisposable? _leavingFullscreen;
    private SynchronizationContext? _context;

    public bool IsRunning { get; private set; }

    /// <summary>
    /// Starts the slideshow, or stops it when it runs, unless the key is only being held down.
    /// </summary>
    /// <param name="milliseconds">The time per image; zero or less uses the slideshow timer setting.</param>
    /// <param name="isKeyHeldDown">Whether the Keybind that asked is only being held down.</param>
    public Task Toggle(int milliseconds, bool isKeyHeldDown)
    {
        if (IsRunning)
        {
            if (!isKeyHeldDown)
            {
                Stop();
            }

            return Task.CompletedTask;
        }

        return canNavigateForwards() ? StartAsync(milliseconds) : Task.CompletedTask;
    }

    /// <summary>
    /// Stops the slideshow and gives back the mode its window had. Does nothing when it is not running.
    /// </summary>
    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        End();
        _ = LeaveAsync();
    }

    /// <summary>
    /// Stops the slideshow without changing the Window Mode, as its window closes.
    /// </summary>
    public void Dispose()
    {
        if (IsRunning)
        {
            End();
        }
    }

    private async Task StartAsync(int milliseconds)
    {
        _context = SynchronizationContext.Current;
        var run = ++_run;
        var period = TimeSpan.FromMilliseconds(Math.Max(MinimumMilliseconds, milliseconds > 0
            ? milliseconds
            : TimeSpan.FromSeconds(Settings.UIProperties.SlideShowTimer).TotalMilliseconds));
        _timer = time.CreateTimer(_ => OnUiThread(() => Tick(run)), null, period, period);
        IsRunning = true;
        screensaver.Take();

        if (switcher() is { } modeSwitcher)
        {
            await modeSwitcher.EnterSlideshow().ConfigureAwait(false);
        }

        if (run != _run)
        {
            // Stopped while it went fullscreen: leaving came first, so give back the mode now, unless a new run took over
            if (!IsRunning)
            {
                await LeaveAsync().ConfigureAwait(false);
            }

            return;
        }

        // Leaving the slideshow's fullscreen any other way, as by toggling fullscreen or minimizing, ends it too.
        // Posted, so the stop does not change the Window Mode while it is still being published
        _leavingFullscreen = windowModeState
            .Where(static state => state.Mode is not WindowMode.Fullscreen)
            .Take(1)
            .Subscribe(_ => OnUiThread(() =>
            {
                if (run == _run)
                {
                    Stop();
                }
            }));
    }

    private void Tick(int run)
    {
        if (run != _run || !IsRunning)
        {
            return;
        }

        _ = NextAsync();
    }

    private async Task NextAsync()
    {
        try
        {
            await next().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(Slideshow), nameof(NextAsync), e);
        }
    }

    private void End()
    {
        _run++;
        IsRunning = false;
        _timer?.Dispose();
        _timer = null;
        _leavingFullscreen?.Dispose();
        _leavingFullscreen = null;
        screensaver.Release();
    }

    private async Task LeaveAsync()
    {
        if (switcher() is { } modeSwitcher)
        {
            await modeSwitcher.LeaveSlideshow().ConfigureAwait(false);
        }
    }

    private void OnUiThread(Action action)
    {
        if (_context is null)
        {
            action();
        }
        else
        {
            _context.Post(static state => ((Action)state!)(), action);
        }
    }
}
