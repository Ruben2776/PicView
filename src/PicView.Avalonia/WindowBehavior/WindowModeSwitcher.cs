using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Views.UC;
using PicView.Core.IPlatform;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.WindowBehavior;

/// <summary>
/// Changes one window's Window Mode. Every request, operating-system report, slideshow and start goes through it;
/// its platform's <see cref="IWindowModeAdapter"/> only enters native states. It writes nothing to the settings record;
/// the window stores its mode there when it closes.
/// </summary>
public sealed class WindowModeSwitcher(MainWindow window, IWindowModeAdapter adapter) : IWindowModeSwitcher
{
    /// <inheritdoc cref="IWindowModeAdapter.SkipsInitialNativeState"/>
    public bool SkipsInitialNativeState => adapter.SkipsInitialNativeState;

    public ValueTask ToggleFullscreen() => ChangeAsync(static state => state.ToggleFullscreen());

    public ValueTask ToggleMaximized() => ChangeAsync(static state => state.ToggleMaximized());

    public ValueTask LeaveTopMode() => ChangeAsync(static state => state.LeaveTopMode());

    /// <summary>
    /// Shows a minimized window again, in the mode it had before it was minimized. Any other window is left as it is.
    /// </summary>
    public ValueTask LeaveMinimized() =>
        ChangeAsync(static state => state.Mode is WindowMode.Minimized ? state.LeaveTopMode() : state);

    public ValueTask SetAutoFit(bool autoFit) => ChangeAsync(state => state.SetAutoFit(autoFit));

    public ValueTask ResizedByHand() => ChangeAsync(static state => state.ResizedByHand());

    public ValueTask EnterSlideshow() => ChangeAsync(static state => state.EnterSlideshow());

    public ValueTask LeaveSlideshow() => ChangeAsync(static state => state.LeaveSlideshow());

    /// <summary>
    /// Brings the Window Mode in line with a native state the operating system reports.
    /// </summary>
    public ValueTask OnNativeStateAsync(WindowState state) =>
        ChangeAsync(current => current.OnNativeState(ToWindowMode(state)));

    /// <summary>
    /// Starts the window in the mode its Window Settings start it in. Call on the UI thread.
    /// </summary>
    /// <param name="isAppStart">Whether this is the first window at app start, the only one that may start fullscreen.</param>
    public void Start(bool isAppStart)
    {
        if (window.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var starting = Starting(vm, isAppStart);
        Apply(vm, vm.WindowModeState.CurrentValue, starting);
        if (starting.Mode is WindowMode.Fullscreen)
        {
            PlaceFullscreenOnceShown(vm);
        }
    }

    // Workaround, outside ADR-0026's rule that the fitter alone sizes the window: this moves it and fits nothing. The
    // first window enters fullscreen here while it is still hidden, since QuickLoad shows it once it has sized it, and
    // Windows leaves the taskbar over a window that went fullscreen hidden until the shown window is placed again. So
    // the window is placed at the origin of its screen once it is open and the dispatcher is idle, as App Start did
    // before the fitter followed its inputs (then through a Refit and a top-centering posted at ApplicationIdle)
    private void PlaceFullscreenOnceShown(MainWindowViewModel vm)
    {
        void PlaceWhenIdle() => Dispatcher.UIThread.Post(() =>
        {
            if (vm.WindowModeState.CurrentValue.Mode is WindowMode.Fullscreen)
            {
                window.WindowFitter.MoveToScreenOrigin();
            }
        }, DispatcherPriority.ApplicationIdle);

        if (window.IsVisible)
        {
            PlaceWhenIdle();
            return;
        }

        void OnOpened(object? sender, EventArgs e)
        {
            window.Opened -= OnOpened;
            PlaceWhenIdle();
        }

        window.Opened += OnOpened;
    }

    /// <summary>
    /// The state <see cref="Start"/> gives the window of <paramref name="vm"/>, from the mode its Window Settings start
    /// it in.
    /// </summary>
    /// <inheritdoc cref="Start" path="/param"/>
    public static WindowModeState Starting(MainWindowViewModel vm, bool isAppStart) =>
        WindowModeState.Start(vm.WindowSettings.StartingMode, isAppStart);

    /// <summary>
    /// Puts the window in the native state of <paramref name="mode"/>, unless it is already in it.
    /// </summary>
    public static void SetNativeState(Window window, WindowMode mode)
    {
        var state = ToWindowState(mode);
        if (window.WindowState != state)
        {
            window.WindowState = state;
        }
    }

    private async ValueTask ChangeAsync(Func<WindowModeState, WindowModeState> rule)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Change(rule);
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() => Change(rule));
        }
    }

    /// <summary>
    /// Applies the rule to the window. Its remembered mode stays with it until it closes and stores it.
    /// </summary>
    private void Change(Func<WindowModeState, WindowModeState> rule)
    {
        if (window.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var previous = vm.WindowModeState.CurrentValue;
        var next = rule(previous);
        if (previous.Mode is WindowMode.Normal && next.Mode is not WindowMode.Normal)
        {
            // While the window is still Normal, so its Window Settings keep its last Normal size and position. Not at
            // Start, where a window that starts maximized or fullscreen was never given its Normal size
            WindowFunctions.RememberBounds(window);
        }

        Apply(vm, previous, next);
    }

    private void Apply(MainWindowViewModel vm, WindowModeState previous, WindowModeState next)
    {
        vm.WindowModeState.Value = next;

        if (next.Mode == previous.Mode)
        {
            // AutoFit only decides how a Normal window takes its size
            if (next.Mode is WindowMode.Normal && next.AutoFit != previous.AutoFit)
            {
                ApplyAutoFit(vm, next.AutoFit);
            }
            return;
        }

        // Nothing is resized while the window is minimized
        if (next.Mode is WindowMode.Minimized)
        {
            return;
        }

        window.IsChangingWindowState = true;
        if (previous.Mode is WindowMode.Minimized)
        {
            // The window is shown again as it was, so only its native state and size need to catch up
            adapter.EnterNativeState(window, next);
        }
        else
        {
            if (next.Mode is WindowMode.Normal)
            {
                window.WindowFitter.FollowContent();
            }
            else
            {
                window.WindowFitter.KeepSize();
            }

            adapter.EnterNativeState(window, next);

            if (next.Mode is not WindowMode.Maximized &&
                vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.Value is NavigableView { Viewer: { } viewer })
            {
                viewer.ResetZoomSlim();
            }
        }

        // The window's fitter fits it to the new Window Mode
        Dispatcher.UIThread.Post(() =>
        {
            window.IsChangingWindowState = false;
            if (vm.WindowModeState.CurrentValue.Mode is WindowMode.Normal)
            {
                window.WindowFitter.FollowContent();
            }
        }, DispatcherPriority.SystemIdle);
    }

    private void ApplyAutoFit(MainWindowViewModel vm, bool autoFit)
    {
        if (autoFit)
        {
            window.WindowFitter.FollowContent();
        }
        else
        {
            window.WindowFitter.KeepSize();
        }
    }

    private static WindowState ToWindowState(WindowMode mode) => mode switch
    {
        WindowMode.Minimized => WindowState.Minimized,
        WindowMode.Maximized => WindowState.Maximized,
        WindowMode.Fullscreen => WindowState.FullScreen,
        _ => WindowState.Normal
    };

    private static WindowMode ToWindowMode(WindowState state) => state switch
    {
        WindowState.Minimized => WindowMode.Minimized,
        WindowState.Maximized => WindowMode.Maximized,
        WindowState.FullScreen => WindowMode.Fullscreen,
        _ => WindowMode.Normal
    };
}
