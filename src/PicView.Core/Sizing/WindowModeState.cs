using System.Runtime.InteropServices;

namespace PicView.Core.Sizing;

/// <summary>
/// The Window Mode the settings remember for a new window to start in. It is never Minimized, and never
/// Fullscreen and Maximized together.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct RememberedWindowMode(bool Fullscreen, bool Maximized, bool AutoFit);

/// <summary>
/// One window's Window Mode, and the modes that leaving Fullscreen or Minimized give back.
/// Each rule returns the next state; the settings are saved when <see cref="Remembered"/> changes.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct WindowModeState
{
    public WindowMode Mode { get; private init; }

    /// <summary>
    /// Whether the window follows the shown image while it is Normal.
    /// </summary>
    public bool AutoFit { get; private init; }

    /// <summary>
    /// The mode leaving Fullscreen gives back: Normal or Maximized.
    /// </summary>
    private WindowMode BeforeFullscreen { get; init; }

    /// <summary>
    /// The mode leaving Minimized gives back: any mode but Minimized.
    /// </summary>
    private WindowMode BeforeMinimized { get; init; }

    /// <summary>
    /// Whether a slideshow made the window fullscreen, which the settings do not remember.
    /// </summary>
    private bool IsSlideshowFullscreen { get; init; }

    public FitMode FitMode => Mode.ToFitMode(AutoFit);

    public RememberedWindowMode Remembered
    {
        get
        {
            var shown = Shown;
            if (shown is WindowMode.Fullscreen && IsSlideshowFullscreen)
            {
                shown = BeforeFullscreen;
            }

            return new RememberedWindowMode(shown is WindowMode.Fullscreen, shown is WindowMode.Maximized, AutoFit);
        }
    }

    /// <summary>
    /// The mode the window is shown in, or will be shown in again once it is no longer minimized.
    /// </summary>
    private WindowMode Shown => Mode is WindowMode.Minimized ? BeforeMinimized : Mode;

    /// <summary>
    /// The state a window opens in, before <see cref="Start"/> gives it the remembered mode: Normal, following the
    /// remembered AutoFit.
    /// </summary>
    public static WindowModeState Opening(bool autoFit) => new() { AutoFit = autoFit };

    /// <summary>
    /// The state a window starts in from the remembered mode. Only the first window at app start may start
    /// fullscreen; a window opened while PicView is running never does.
    /// </summary>
    public static WindowModeState Start(RememberedWindowMode remembered, bool isAppStart) => new()
    {
        Mode = WindowModes.From(remembered.Fullscreen && isAppStart, remembered.Maximized),
        AutoFit = remembered.AutoFit,
        BeforeFullscreen = remembered.Maximized ? WindowMode.Maximized : WindowMode.Normal
    };

    public WindowModeState ToggleFullscreen() =>
        Shown is WindowMode.Fullscreen ? LeaveFullscreen() : EnterFullscreen(bySlideshow: false);

    public WindowModeState ToggleMaximized() =>
        Show(Shown is WindowMode.Maximized ? WindowMode.Normal : WindowMode.Maximized);

    /// <summary>
    /// Leaves the mode on top, as the Restore button does: Fullscreen and Minimized give back the mode the window
    /// had before them, and Maximized gives back Normal.
    /// </summary>
    public WindowModeState LeaveTopMode() => Mode switch
    {
        WindowMode.Fullscreen => LeaveFullscreen(),
        WindowMode.Maximized => Show(WindowMode.Normal),
        WindowMode.Minimized => LeaveMinimized(),
        _ => this
    };

    public WindowModeState SetAutoFit(bool autoFit) => this with { AutoFit = autoFit };

    /// <summary>
    /// Resizing a Normal window by hand turns AutoFit off for it.
    /// </summary>
    public WindowModeState ResizedByHand() => Mode is WindowMode.Normal ? this with { AutoFit = false } : this;

    /// <summary>
    /// Shows the window fullscreen for a slideshow, without changing the remembered mode.
    /// </summary>
    public WindowModeState EnterSlideshow() => EnterFullscreen(bySlideshow: true);

    /// <summary>
    /// Gives back the mode the window had before the slideshow made it fullscreen.
    /// </summary>
    public WindowModeState LeaveSlideshow()
    {
        if (!IsSlideshowFullscreen)
        {
            return this;
        }

        return Mode is WindowMode.Minimized
            ? this with { BeforeMinimized = BeforeFullscreen, IsSlideshowFullscreen = false }
            : LeaveFullscreen();
    }

    /// <summary>
    /// Brings the state in line with a native state the operating system reports, such as after the user used its
    /// own window controls.
    /// </summary>
    public WindowModeState OnNativeState(WindowMode native)
    {
        if (native == Mode)
        {
            return this;
        }

        if (native is WindowMode.Minimized)
        {
            return this with { Mode = WindowMode.Minimized, BeforeMinimized = Mode };
        }

        if (Mode is WindowMode.Minimized)
        {
            // Leaving Minimized gives back the mode the window had, whatever state the window is restored to
            return LeaveMinimized();
        }

        return native switch
        {
            // Leaving native fullscreen can pass through Maximized
            WindowMode.Maximized when Mode is WindowMode.Fullscreen => this,
            WindowMode.Fullscreen => EnterFullscreen(bySlideshow: false),
            _ => Show(native)
        };
    }

    private WindowModeState EnterFullscreen(bool bySlideshow)
    {
        var shown = Shown;
        if (shown is WindowMode.Fullscreen)
        {
            return this with { Mode = WindowMode.Fullscreen, IsSlideshowFullscreen = IsSlideshowFullscreen && bySlideshow };
        }

        return this with { Mode = WindowMode.Fullscreen, BeforeFullscreen = shown, IsSlideshowFullscreen = bySlideshow };
    }

    private WindowModeState LeaveMinimized() => this with { Mode = BeforeMinimized, BeforeMinimized = default };

    private WindowModeState LeaveFullscreen() => this with { Mode = BeforeFullscreen, IsSlideshowFullscreen = false };

    private WindowModeState Show(WindowMode mode) => this with { Mode = mode, IsSlideshowFullscreen = false };
}
