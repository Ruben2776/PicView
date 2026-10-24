using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.Input;
using PicView.Core.IPlatform;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using MouseButton = PicView.Avalonia.Input.MouseButton;
using PicView.Core.Keybindings;

namespace PicView.Tests.Input;

[Collection("Sequential")]
public class KeybindRouterTests
{
    private static readonly InputModeState Normal = new();
    private static readonly InputModeState Dialog = new(IsDialogOpen: true);

    private readonly MainWindowViewModel _vm;
    private readonly List<string> _calls;
    private readonly List<string> _windowModeCalls;
    private InputModeState _state = Normal;
    private Keymap _keymap = Keymap.Empty.Assign(new Keybind(Key.Right), "Next");
    private Window? _secondaryWindow;

    public KeybindRouterTests()
    {
        SetDefaults();
        _vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var mapper = DispatchProxy.Create<IFunctionsMapper, RecordingMapper>();
        _calls = ((RecordingMapper)(object)mapper).Calls;
        _vm.Mapper = mapper;
        var switcher = DispatchProxy.Create<IWindowModeSwitcher, RecordingMapper>();
        _windowModeCalls = ((RecordingMapper)(object)switcher).Calls;
        _vm.WindowModeSwitcher = switcher;
    }

    [Fact]
    public async Task KeyDown_BoundKey_RunsItsAction()
    {
        await Router().KeyDownAsync(new Keybind(Key.Right), null);

        Assert.Equal(["Next"], _calls);
    }

    [Fact]
    public async Task KeyDown_RepeatedKey_IsHeldDownUntilReleased()
    {
        var router = Router();

        await router.KeyDownAsync(new Keybind(Key.Right), null);
        Assert.False(router.IsKeyHeldDown);
        await router.KeyDownAsync(new Keybind(Key.Right), null);
        Assert.True(router.IsKeyHeldDown);

        await router.KeyUpAsync(Key.Right);
        Assert.False(router.IsKeyHeldDown);
    }

    [Fact]
    public async Task KeyUp_StopsHeldNavigation()
    {
        var router = Router();
        await router.KeyDownAsync(new Keybind(Key.Right), null);

        await router.KeyUpAsync(Key.Right);

        Assert.Equal(["Next", nameof(IFunctionsMapper.StopRepeatedNavigation)], _calls);
    }

    [Fact]
    public async Task KeyUp_WithoutViewModel_DoesNothing()
    {
        var router = new KeybindRouter(() => null, () => _state, () => _keymap, _ => NoContextualActions.Instance,
            () => _secondaryWindow);

        await router.KeyUpAsync(Key.Right);

        Assert.Empty(_calls);
    }

    [Fact]
    public async Task Escape_EmptyOrdinaryKeymap_StillClosesTheWindow()
    {
        _keymap = Keymap.Empty;

        await Router().KeyDownAsync(InputDispatch.CancelKeybind, null);

        Assert.Equal([Actions.Close], _calls);
    }

    [Fact]
    public async Task Escape_HeldDown_DoesNotCloseTheWindow()
    {
        var router = Router();
        await router.KeyDownAsync(new Keybind(Key.Right), null);

        await router.KeyDownAsync(InputDispatch.CancelKeybind, null);

        Assert.DoesNotContain(Actions.Close, _calls);
    }

    [Fact]
    public async Task Escape_FullscreenAndEscLeavesFullscreen_LeavesFullscreenInsteadOfClosing()
    {
        Settings.WindowProperties.EscLeavesFullscreen = true;
        _vm.WindowModeState.Value = WindowModeState.Opening(autoFit: false).ToggleFullscreen();

        await Router().KeyDownAsync(InputDispatch.CancelKeybind, null);

        Assert.Equal([nameof(IWindowModeSwitcher.LeaveTopMode)], _windowModeCalls);
        Assert.Empty(_calls);
    }

    [Fact]
    public async Task Escape_FullscreenWithoutEscLeavesFullscreen_ClosesTheWindow()
    {
        Settings.WindowProperties.EscLeavesFullscreen = false;
        _vm.WindowModeState.Value = WindowModeState.Opening(autoFit: false).ToggleFullscreen();

        await Router().KeyDownAsync(InputDispatch.CancelKeybind, null);

        Assert.Equal([Actions.Close], _calls);
        Assert.Empty(_windowModeCalls);
    }

    [Fact]
    public async Task Escape_MaximizedWithEscLeavesFullscreen_ClosesTheWindow()
    {
        Settings.WindowProperties.EscLeavesFullscreen = true;
        _vm.WindowModeState.Value = WindowModeState.Opening(autoFit: false).ToggleMaximized();

        await Router().KeyDownAsync(InputDispatch.CancelKeybind, null);

        Assert.Equal([Actions.Close], _calls);
        Assert.Empty(_windowModeCalls);
    }

    [Fact]
    public async Task Escape_FullscreenHeldDown_NeitherLeavesFullscreenNorCloses()
    {
        Settings.WindowProperties.EscLeavesFullscreen = true;
        _vm.WindowModeState.Value = WindowModeState.Opening(autoFit: false).ToggleFullscreen();
        var router = Router();
        await router.KeyDownAsync(new Keybind(Key.Right), null);

        await router.KeyDownAsync(InputDispatch.CancelKeybind, null);

        Assert.Empty(_windowModeCalls);
        Assert.DoesNotContain(Actions.Close, _calls);
    }

    [Fact]
    public async Task Escape_SlideshowClaimsItInFullscreen_DoesNotLeaveFullscreen()
    {
        Settings.WindowProperties.EscLeavesFullscreen = true;
        _state = new InputModeState(IsSlideshowRunning: true);
        _vm.WindowModeState.Value = WindowModeState.Opening(autoFit: false).ToggleFullscreen();
        var router = new KeybindRouter(() => _vm, () => _state, () => _keymap, _ => ClaimingContextualActions.Instance,
            () => _secondaryWindow);

        await router.KeyDownAsync(InputDispatch.CancelKeybind, null);

        Assert.Empty(_windowModeCalls);
        Assert.Empty(_calls);
    }

    [AvaloniaFact]
    public async Task Escape_SecondaryWindowOpen_ClosesItAndIgnoresTheHeldPress()
    {
        var closed = false;
        _secondaryWindow = new Window();
        _secondaryWindow.Closed += (_, _) => closed = true;
        _secondaryWindow.Show();
        var router = Router();

        await router.KeyDownAsync(InputDispatch.CancelKeybind, null);
        _secondaryWindow = null;
        await router.KeyDownAsync(InputDispatch.CancelKeybind, null);

        Assert.True(closed);
        Assert.DoesNotContain(Actions.Close, _calls);

        await router.KeyUpAsync(Key.Escape);
        await router.KeyDownAsync(InputDispatch.CancelKeybind, null);

        Assert.Contains(Actions.Close, _calls);
    }

    [Fact]
    public async Task IgnoreUntilReleased_IgnoresTheKeyUntilItIsReleased()
    {
        var router = Router();
        router.IgnoreUntilReleased(Key.Right);

        await router.KeyDownAsync(new Keybind(Key.Right), null);
        Assert.Empty(_calls);

        await router.KeyUpAsync(Key.Right);
        await router.KeyDownAsync(new Keybind(Key.Right), null);

        Assert.Contains("Next", _calls);
    }

    [Fact]
    public async Task Deactivated_ForgetsHeldAndIgnoredKeys()
    {
        var router = Router();
        await router.KeyDownAsync(new Keybind(Key.Right), null);
        await router.KeyDownAsync(new Keybind(Key.Right), null);
        router.IgnoreUntilReleased(Key.Escape);

        router.Deactivated();

        Assert.False(router.IsKeyHeldDown);
        await router.KeyDownAsync(InputDispatch.CancelKeybind, null);
        Assert.Contains(Actions.Close, _calls);
    }

    [AvaloniaFact]
    public async Task KeyDown_DialogWithTextBoxFocused_LeavesTheTextBoxItsKeys()
    {
        _state = Dialog;
        _keymap = Keymap.Empty.Assign(new Keybind(Key.A), "Next");
        var handler = new RecordingContextualActions();
        var router = new KeybindRouter(() => _vm, () => _state, () => _keymap, _ => handler, () => null);

        await router.KeyDownAsync(new Keybind(Key.A), new TextBox());

        Assert.Empty(handler.Asked);
        Assert.Empty(_calls);
    }

    [Fact]
    public async Task MouseButton_InDialog_DoesNothing()
    {
        _state = Dialog;
        _keymap = Keymap.Empty.Assign(new Keybind(MouseButton.XButton1), "Next");

        await Router().MouseButtonAsync(new Keybind(MouseButton.XButton1));

        Assert.Empty(_calls);
    }

    [Fact]
    public async Task MouseButton_Bound_RunsItsAction()
    {
        _keymap = Keymap.Empty.Assign(new Keybind(MouseButton.XButton1), "Next");

        await Router().MouseButtonAsync(new Keybind(MouseButton.XButton1));

        Assert.Equal(["Next"], _calls);
    }

    [Fact]
    public async Task Wheel_ImageTallerThanTheViewer_ScrollsInsteadOfRunningAnAction()
    {
        Settings.Zoom.ScrollEnabled = true;
        _keymap = Keymap.Empty.Assign(new Keybind(MouseButton.WheelDown), "Next");
        var viewer = new FakeViewer { CanScrollVertically = true };

        var used = await Router().WheelAsync(new Keybind(MouseButton.WheelDown), false, default, viewer);

        Assert.True(used);
        Assert.Equal(["ScrollLine(down)"], viewer.Calls);
        Assert.Empty(_calls);
    }

    [Fact]
    public async Task Wheel_Zoom_ZoomsTowardsThePointerEvenOnATouchpad()
    {
        _keymap = Keymap.Empty.Assign(new Keybind(MouseButton.WheelUp), "ZoomIn");
        var viewer = new FakeViewer();

        await Router().WheelAsync(new Keybind(MouseButton.WheelUp), isTouch: true, new Point(10, 20), viewer);

        Assert.Equal(["ZoomAt(in, 10, 20)"], viewer.Calls);
    }

    [Fact]
    public async Task Wheel_OtherActionOnATouchpad_IsSkipped()
    {
        _keymap = Keymap.Empty.Assign(new Keybind(MouseButton.WheelDown), "Next");

        await Router().WheelAsync(new Keybind(MouseButton.WheelDown), isTouch: true, default, new FakeViewer());

        Assert.Empty(_calls);
    }

    [Fact]
    public async Task Wheel_InDialog_IsNotUsed()
    {
        _state = Dialog;

        var used = await Router().WheelAsync(new Keybind(MouseButton.WheelDown), false, default, new FakeViewer());

        Assert.False(used);
    }

    private KeybindRouter Router() =>
        new(() => _vm, () => _state, () => _keymap, _ => NoContextualActions.Instance, () => _secondaryWindow);

    private sealed class NoContextualActions : IContextualActionHandler
    {
        public static NoContextualActions Instance { get; } = new();

        public ValueTask<bool> TryRunAsync(InputMode mode, string contextualAction) => ValueTask.FromResult(false);
    }

    private sealed class ClaimingContextualActions : IContextualActionHandler
    {
        public static ClaimingContextualActions Instance { get; } = new();

        public ValueTask<bool> TryRunAsync(InputMode mode, string contextualAction) => ValueTask.FromResult(true);
    }

    private sealed class RecordingContextualActions : IContextualActionHandler
    {
        public List<string> Asked { get; } = [];

        public ValueTask<bool> TryRunAsync(InputMode mode, string contextualAction)
        {
            Asked.Add(contextualAction);
            return ValueTask.FromResult(false);
        }
    }

    private sealed class FakeViewer : IZoomScrollTarget
    {
        public List<string> Calls { get; } = [];

        public bool CanScrollVertically { get; init; }

        public void ScrollLine(bool down) => Calls.Add(down ? "ScrollLine(down)" : "ScrollLine(up)");

        public void ZoomAt(bool zoomIn, Point pointer) =>
            Calls.Add($"ZoomAt({(zoomIn ? "in" : "out")}, {pointer.X}, {pointer.Y})");
    }

    /// <summary>
    /// Records every mapper or Window Mode switcher call by name, and every Action run through Run by the
    /// Action's name.
    /// </summary>
    public class RecordingMapper : DispatchProxy
    {
        public List<string> Calls { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            // An Action is recorded by its name, any other call by the method's name
            Calls.Add(targetMethod!.Name is nameof(IFunctionsMapper.Run) ? (string)args![0]! : targetMethod.Name);
            if (targetMethod.ReturnType == typeof(ValueTask))
            {
                return ValueTask.CompletedTask;
            }

            return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
        }
    }
}
