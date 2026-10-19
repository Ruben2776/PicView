using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Core.IPlatform;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Input;

/// <summary>
/// Turns one main window's key presses, mouse buttons and wheel into Actions. It tracks the key that is held down,
/// leaves a focused dialog input the keys it edits with, gives Escape its last meaning, and dispatches every Keybind
/// through the window's Input Mode and the Keymap.
/// </summary>
public sealed class KeybindRouter
{
    private readonly Func<MainWindowViewModel?> _viewModel;
    private readonly Func<InputModeState> _inputMode;
    private readonly Func<Keymap> _keymap;
    private readonly Func<MainWindowViewModel, IContextualActionHandler> _contextualActions;
    private readonly Func<Window?> _newestSecondaryWindow;

    private readonly HashSet<Key> _ignoredUntilReleased = [];
    private Keybind _lastKeyDown;
    private int _keyDownCount;

    public KeybindRouter(MainWindow window) : this(
        () => window.DataContext as MainWindowViewModel,
        () => InputModes.StateOf(window),
        () => Keymaps.Current,
        vm => new ContextualActionHandler(window, vm),
        () => window.MainWindowInitializer?.NewestOpenWindow)
    {
    }

    /// <param name="newestSecondaryWindow">The secondary window this main window opened most recently that is
    /// still open, which Escape closes first.</param>
    internal KeybindRouter(
        Func<MainWindowViewModel?> viewModel,
        Func<InputModeState> inputMode,
        Func<Keymap> keymap,
        Func<MainWindowViewModel, IContextualActionHandler> contextualActions,
        Func<Window?> newestSecondaryWindow)
    {
        _viewModel = viewModel;
        _inputMode = inputMode;
        _keymap = keymap;
        _contextualActions = contextualActions;
        _newestSecondaryWindow = newestSecondaryWindow;
    }

    /// <summary>
    /// Whether the key that is down has repeated, as when the user holds an arrow key to navigate quickly.
    /// </summary>
    public bool IsKeyHeldDown { get; private set; }

    /// <param name="focused">The window's focused element, which may keep the key for itself in a dialog.</param>
    public async ValueTask KeyDownAsync(Keybind keybind, IInputElement? focused)
    {
        if (_ignoredUntilReleased.Contains(keybind.Key) || _viewModel() is not { } vm)
        {
            return;
        }
#if DEBUG
        // F12 shows Avalonia's DevTools
        if (keybind.Key is Key.F12)
        {
            return;
        }
#endif

        _lastKeyDown = keybind;
        _keyDownCount++;
        IsKeyHeldDown = _keyDownCount > 1;
        if (IsModifierKey(keybind.Key))
        {
            return;
        }

        var state = _inputMode();
        // A text box or Keybind box in a dialog keeps the keys it edits with, such as a Confirm bound to a letter
        if (state.Resolve() is InputMode.Dialog && FocusedInput.OwnsKeybind(focused, keybind))
        {
            return;
        }

        await DispatchAsync(keybind, state, vm).ConfigureAwait(false);
    }

    public async ValueTask KeyUpAsync(Key key)
    {
        _ignoredUntilReleased.Remove(key);
        var released = _lastKeyDown;
        Reset();
        if (_viewModel() is not { } vm)
        {
            return;
        }

        // Releasing Alt on its own opens or closes the menu, as in other Windows and Linux apps
        if (!OperatingSystem.IsMacOS() && released.Key is Key.LeftAlt or Key.RightAlt)
        {
            vm.TopTitlebarViewModel.ToggleMenu();
        }

        if (vm.Mapper is not null)
        {
            await vm.Mapper.StopRepeatedNavigation().ConfigureAwait(false);
        }
    }

    public async ValueTask MouseButtonAsync(Keybind keybind)
    {
        if (_viewModel() is not { } vm)
        {
            return;
        }

        var state = _inputMode();
        if (state.Resolve() is InputMode.Dialog)
        {
            return;
        }

        await DispatchAsync(keybind, state, vm).ConfigureAwait(false);
    }

    /// <summary>
    /// Scrolls <paramref name="viewer"/> while the image is taller than it, else runs the wheel's Action. Wheel zoom
    /// zooms towards <paramref name="pointer"/>, and is the only Action a touchpad or touch wheel runs.
    /// </summary>
    /// <returns>Whether the wheel was used, so it does not also scroll anything else.</returns>
    public async ValueTask<bool> WheelAsync(Keybind keybind, bool isTouch, Point pointer, IZoomScrollTarget viewer)
    {
        if (_viewModel() is not { } vm)
        {
            return false;
        }

        var state = _inputMode();
        if (state.Resolve() is InputMode.Dialog)
        {
            return false;
        }

        if (Settings.Zoom.ScrollEnabled && viewer.CanScrollVertically)
        {
            viewer.ScrollLine(down: keybind.MouseButton is MouseButton.WheelDown);
            return true;
        }

        var isTouchPad = isTouch || Settings.Zoom.IsUsingTouchPad;
        await InputDispatch.DispatchAsync(keybind, state, _keymap(), ContextualActions(vm), async resolution =>
        {
            switch (resolution.Value)
            {
                case nameof(IFunctionsMapper.ZoomIn) or nameof(IFunctionsMapper.ZoomOut):
                    var zoomIn = resolution.Value is nameof(IFunctionsMapper.ZoomIn);
                    await Dispatcher.UIThread.InvokeAsync(() => viewer.ZoomAt(zoomIn, pointer));
                    break;
                default:
                    if (!isTouchPad)
                    {
                        await Keymaps.ExecuteAsync(resolution, vm.Mapper).ConfigureAwait(false);
                    }
                    break;
            }
        }).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Forgets the keys that are down when the window loses focus, since it will not see them released.
    /// </summary>
    public void Deactivated()
    {
        Reset();
        _ignoredUntilReleased.Clear();
    }

    /// <summary>
    /// Ignores <paramref name="key"/> until it is released, such as an Escape still held from closing another window.
    /// </summary>
    public void IgnoreUntilReleased(Key key) => _ignoredUntilReleased.Add(key);

    private ValueTask DispatchAsync(Keybind keybind, InputModeState state, MainWindowViewModel vm) =>
        InputDispatch.DispatchAsync(keybind, state, _keymap(), ContextualActions(vm),
            resolution => Keymaps.ExecuteAsync(resolution, vm.Mapper));

    private EscapeFallback ContextualActions(MainWindowViewModel vm) => new(this, vm, _contextualActions(vm));

    /// <summary>
    /// What Escape does when no Input Mode claims it: closes the main menu (open while the UI is hidden), else the
    /// newest secondary window this window opened, else this window unless Escape is held down.
    /// </summary>
    private async ValueTask CloseOnEscapeAsync(MainWindowViewModel vm)
    {
        if (vm.TopTitlebarViewModel.IsMainMenuVisible.CurrentValue)
        {
            vm.TopTitlebarViewModel.CloseMenu();
            return;
        }

        if (_newestSecondaryWindow() is { } secondaryWindow)
        {
            secondaryWindow.Close();
            IgnoreUntilReleased(Key.Escape);
            return;
        }

        if (!IsKeyHeldDown && vm.Mapper is not null)
        {
            await vm.Mapper.Close().ConfigureAwait(false);
        }
    }

    private void Reset()
    {
        IsKeyHeldDown = false;
        _lastKeyDown = default;
        _keyDownCount = 0;
    }

    private static bool IsModifierKey(Key key) => key is
        Key.LeftShift or Key.RightShift or
        Key.LeftCtrl or Key.RightCtrl or
        Key.LeftAlt or Key.RightAlt or
        Key.LWin or Key.RWin;

    /// <summary>
    /// Gives Cancel in the Normal Input Mode its meaning, closing what Escape closes, and leaves the rest to the
    /// window's Contextual Actions.
    /// </summary>
    private sealed class EscapeFallback(KeybindRouter router, MainWindowViewModel vm, IContextualActionHandler inner)
        : IContextualActionHandler
    {
        public async ValueTask<bool> TryRunAsync(InputMode mode, string contextualAction)
        {
            if (mode is not InputMode.Normal || contextualAction is not ContextualActionHandler.Cancel)
            {
                return await inner.TryRunAsync(mode, contextualAction).ConfigureAwait(false);
            }

            await router.CloseOnEscapeAsync(vm).ConfigureAwait(false);
            return true;
        }
    }
}
