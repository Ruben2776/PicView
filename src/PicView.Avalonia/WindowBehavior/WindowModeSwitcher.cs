using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Core.IPlatform;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.WindowBehavior;

/// <summary>
/// Changes one window's Window Mode. Every request, operating-system report, slideshow and start goes through it;
/// its platform's <see cref="IWindowModeAdapter"/> only enters native states.
/// </summary>
/// <param name="saveSettings">Saves the settings once the remembered mode changed; the settings file by default.</param>
public sealed class WindowModeSwitcher(
    MainWindow window,
    IWindowModeAdapter adapter,
    Func<ValueTask<bool>>? saveSettings = null) : IWindowModeSwitcher
{
    private readonly Func<ValueTask<bool>> _saveSettings = saveSettings ?? SaveSettingsAsync;

    /// <inheritdoc cref="IWindowModeAdapter.SkipsInitialNativeState"/>
    public bool SkipsInitialNativeState => adapter.SkipsInitialNativeState;

    public ValueTask ToggleFullscreen() => ChangeAsync(static state => state.ToggleFullscreen());

    public ValueTask ToggleMaximized() => ChangeAsync(static state => state.ToggleMaximized());

    public ValueTask LeaveTopMode() => ChangeAsync(static state => state.LeaveTopMode());

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
    /// Starts the window in the mode the settings remember, without saving. Call on the UI thread.
    /// </summary>
    /// <param name="isAppStart">Whether this is the first window at app start, the only one that may start fullscreen.</param>
    public void Start(bool isAppStart)
    {
        if (window.DataContext is MainWindowViewModel vm)
        {
            Apply(vm, vm.WindowModeState.CurrentValue, Starting(isAppStart));
        }
    }

    /// <summary>
    /// The state <see cref="Start"/> gives the window, from the mode the settings remember.
    /// </summary>
    /// <inheritdoc cref="Start" path="/param"/>
    public static WindowModeState Starting(bool isAppStart) =>
        WindowModeState.Start(new RememberedWindowMode(Settings.WindowProperties.Fullscreen,
            Settings.WindowProperties.Maximized, Settings.WindowProperties.AutoFit), isAppStart);

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
        var rememberedChanged = Dispatcher.UIThread.CheckAccess()
            ? Change(rule)
            : await Dispatcher.UIThread.InvokeAsync(() => Change(rule));
        if (rememberedChanged)
        {
            await _saveSettings().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Applies the rule to the window, and returns whether the mode the settings remember changed.
    /// </summary>
    private bool Change(Func<WindowModeState, WindowModeState> rule)
    {
        if (window.DataContext is not MainWindowViewModel vm)
        {
            return false;
        }

        var previous = vm.WindowModeState.CurrentValue;
        var next = rule(previous);
        Apply(vm, previous, next);
        if (next.Remembered == previous.Remembered)
        {
            return false;
        }

        // The last change the user made in any window is the one remembered
        Settings.WindowProperties.Fullscreen = next.Remembered.Fullscreen;
        Settings.WindowProperties.Maximized = next.Remembered.Maximized;
        Settings.WindowProperties.AutoFit = next.Remembered.AutoFit;
        return true;
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

            if (previous.Mode is WindowMode.Fullscreen)
            {
                ToggleUIVisibility.RestoreInterface(vm);
            }

            adapter.EnterNativeState(window, next);

            if (next.Mode is WindowMode.Fullscreen)
            {
                ToggleUIVisibility.FullscreenHideInterface(vm);
            }

            if (next.Mode is not WindowMode.Maximized &&
                vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is ImageViewer imageViewer)
            {
                imageViewer.ResetZoomSlim();
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
