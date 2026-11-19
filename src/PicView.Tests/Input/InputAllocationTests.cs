using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Input;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Input;
using PicView.Core.IPlatform;
using PicView.Core.ViewModels;
using R3;
using MouseButton = PicView.Avalonia.Input.MouseButton;

namespace PicView.Tests.Input;

/// <summary>
/// Key presses, mouse buttons and the wheel drive navigation, so turning one into its Action must not allocate.
/// The view model has no mapper, so each press stops where the mapper would run the Action; the mapper's own
/// lookup is measured on its own.
/// </summary>
[Collection("Sequential")]
public class InputAllocationTests
{
    private const int Presses = 100;

    private static readonly Keybind Right = new(Key.Right);
    private static readonly Keybind Enter = new(Key.Enter);
    private static readonly Keybind F1 = new(Key.F1);
    private static readonly Keybind Up = new(Key.Up);

    private static readonly Keymap Keymap = Keymap.Empty
        .Assign(Right, "Next")
        .Assign(F1, "SettingsWindow")
        .Assign(Enter, "ToggleFullscreen")
        .Assign(Enter, "Confirm")
        .Assign(Up, "RotateRight")
        .Assign(Up, ContextualActionHandler.Up)
        .Assign(new Keybind(MouseButton.XButton1), "Prev")
        .Assign(new Keybind(MouseButton.WheelDown), "Next")
        .Assign(new Keybind(MouseButton.WheelUp), "ZoomIn");

    private readonly MainWindowViewModel _vm;
    private InputModeState _state;

    public InputAllocationTests()
    {
        SetDefaults();
        _vm = new MainWindowViewModel(null!, null!, null!, null!);
    }

    [Fact]
    public void KeyDown_OrdinaryAction_AllocatesNothing()
    {
        var router = Router(Claims.Nothing);

        Assert.Equal(0, AllocatedBytes(() => PressAsync(router, Right)));
    }

    [Fact]
    public void KeyDown_OrdinaryActionWhileAnimatedImageShows_AllocatesNothing()
    {
        // A non-blocking mode also checks whether the Action needs a still image
        _state = new InputModeState(IsAnimatedImageActive: true);
        var router = Router(Claims.Nothing);

        Assert.Equal(0, AllocatedBytes(() => PressAsync(router, Right)));
    }

    [Fact]
    public void KeyDown_ContextualActionUnclaimedInGallery_AllocatesNothing()
    {
        // The gallery leaves the Keybind to its ordinary Action
        _state = new InputModeState(IsGalleryExpanded: true);
        var router = Router(Claims.Nothing);

        Assert.Equal(0, AllocatedBytes(() => PressAsync(router, Right)));
    }

    [Fact]
    public void KeyDown_EscapeThroughEveryNonBlockingMode_AllocatesNothing()
    {
        // Every mode leaves Cancel to the next one, down to what Escape closes in the Normal mode
        _state = new InputModeState(IsMotionPhotoActive: true, IsSlideshowRunning: true);
        var router = Router(Claims.Nothing);

        Assert.Equal(0, AllocatedBytes(() => PressAsync(router, InputDispatch.CancelKeybind)));
    }

    [Fact]
    public void KeyDown_ContextualActionClaimedInCrop_AllocatesNothing()
    {
        _state = new InputModeState(IsCropping: true);
        var router = Router(Claims.Everything);

        Assert.Equal(0, AllocatedBytes(() => PressAsync(router, Enter)));
    }

    [Fact]
    public void KeyDown_WindowActionInCrop_AllocatesNothing()
    {
        _state = new InputModeState(IsCropping: true);
        var router = Router(Claims.Nothing);

        Assert.Equal(0, AllocatedBytes(() => PressAsync(router, F1)));
    }

    [Fact]
    public void MouseButton_OrdinaryAction_AllocatesNothing()
    {
        var router = Router(Claims.Nothing);

        Assert.Equal(0, AllocatedBytes(() => router.MouseButtonAsync(new Keybind(MouseButton.XButton1))));
    }

    [Fact]
    public void Wheel_OrdinaryAction_AllocatesNothing()
    {
        var router = Router(Claims.Nothing);
        var viewer = new CountingViewer();

        Assert.Equal(0, AllocatedBytes(() => WheelAsync(router, new Keybind(MouseButton.WheelDown), viewer)));
    }

    [Fact]
    public void Wheel_Zoom_AllocatesNothing()
    {
        var router = Router(Claims.Nothing);
        var viewer = new CountingViewer();

        Assert.Equal(0, AllocatedBytes(() => WheelAsync(router, new Keybind(MouseButton.WheelUp), viewer)));
        Assert.Equal(Presses + 1, viewer.Zooms);
    }

    [Fact]
    public void Wheel_ScrollingOnInTheWindow_ScrollsAndAllocatesNothing()
    {
        // The window's scrolling is read, never the record's
        Settings.Zoom.ScrollEnabled = false;
        _vm.WindowSettings.IsScrollingEnabled.Value = true;
        var router = Router(Claims.Nothing);
        var viewer = new CountingViewer { CanScrollVertically = true };

        Assert.Equal(0, AllocatedBytes(() => WheelAsync(router, new Keybind(MouseButton.WheelUp), viewer)));
        Assert.Equal(Presses + 1, viewer.ScrollLines);
        Assert.Equal(0, viewer.Zooms);
    }

    [Fact]
    public void Wheel_ScrollingOffInTheWindow_ZoomsAndAllocatesNothing()
    {
        Settings.Zoom.ScrollEnabled = true;
        _vm.WindowSettings.IsScrollingEnabled.Value = false;
        var router = Router(Claims.Nothing);
        var viewer = new CountingViewer { CanScrollVertically = true };

        Assert.Equal(0, AllocatedBytes(() => WheelAsync(router, new Keybind(MouseButton.WheelUp), viewer)));
        Assert.Equal(Presses + 1, viewer.Zooms);
        Assert.Equal(0, viewer.ScrollLines);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void KeyDown_ArrowThroughTheWindowsContextualActions_AllocatesNothing(bool isScrollingEnabled)
    {
        // The real Contextual Actions read the window's scrolling and then the viewer; an image that fits leaves Up to
        // its ordinary Action either way, so the press never leaves this thread
        _vm.WindowSettings.IsScrollingEnabled.Value = isScrollingEnabled;
        var tab = new TabViewModel(_vm);
        var viewer = new CountingViewer();
        tab.SetViews(new ViewOf(viewer));
        _vm.WindowTabs.ActiveTab.Value = tab;
        var router = Router(new ContextualActionHandler(null!, _vm));

        Assert.Equal(0, AllocatedBytes(() => PressAsync(router, Up)));
        Assert.Equal(isScrollingEnabled ? Presses + 1 : 0, viewer.TallerChecks);
    }

    [Fact]
    public void GetFunctionByName_AllocatesNothing()
    {
        // The constructor only builds delegates, so its dependencies are never touched
        var mapper = new FunctionsMapper(null!, null!);
        Assert.NotNull(mapper.GetFunctionByName("Next"));

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Presses; i++)
        {
            mapper.GetFunctionByName("Next");
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private KeybindRouter Router(IContextualActionHandler contextualActions) =>
        new(() => _vm, () => _state, () => Keymap, _ => contextualActions, () => null);

    /// <summary>
    /// Presses and releases <paramref name="keybind"/>, so Escape is never held down.
    /// </summary>
    private static async ValueTask PressAsync(KeybindRouter router, Keybind keybind)
    {
        await router.KeyDownAsync(keybind, null);
        await router.KeyUpAsync(keybind.Key);
    }

    private static async ValueTask WheelAsync(KeybindRouter router, Keybind keybind, CountingViewer viewer) =>
        await router.WheelAsync(keybind, isTouch: false, new Point(10, 20), viewer);

    /// <summary>
    /// Whether PicView.Avalonia was compiled with optimizations. Without them the compiler makes every async state
    /// machine a class, which allocates even when the method finishes at once.
    /// </summary>
    private static bool IsOptimized { get; } =
        typeof(KeybindRouter).Assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled is not true;

    /// <summary>
    /// Returns the bytes allocated by <paramref name="press"/> over <see cref="Presses"/> presses, after a first
    /// press that builds what is kept.
    /// </summary>
    private static long AllocatedBytes(Func<ValueTask> press)
    {
        Assert.SkipUnless(IsOptimized, "Debug builds allocate every async state machine; run in Release");
        Run(press);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Presses; i++)
        {
            Run(press);
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static void Run(Func<ValueTask> press)
    {
        var pressing = press();
        // Every fake finishes at once, so the press never leaves this thread and its allocations are counted
        Assert.True(pressing.IsCompletedSuccessfully);
        pressing.GetAwaiter().GetResult();
    }

    private sealed class Claims(bool claims) : IContextualActionHandler
    {
        public static Claims Nothing { get; } = new(false);

        public static Claims Everything { get; } = new(true);

        public ValueTask<bool> TryRunAsync(InputMode mode, string contextualAction) => ValueTask.FromResult(claims);
    }

    private sealed class CountingViewer : IZoomScrollTarget
    {
        private int _tallerChecks;

        public int Zooms { get; private set; }

        public int ScrollLines { get; private set; }

        // How often the Contextual Actions asked whether the image is taller, which they ask only with scrolling on
        public int TallerChecks => _tallerChecks;

        public bool CanScrollVertically { get; init; }

        public bool IsImageTallerThanViewer
        {
            get
            {
                _tallerChecks++;
                return CanScrollVertically;
            }
        }

        public void ScrollLine(bool down) => ScrollLines++;

        public void ScrollToHome()
        {
        }

        public void ScrollToEnd()
        {
        }

        public void ZoomIn()
        {
        }

        public void ZoomOut()
        {
        }

        public void ZoomAt(bool zoomIn, Point pointer) => Zooms++;
    }

    private sealed class ViewOf(object view) : ITabViews
    {
        public IReadOnlyBindableReactiveProperty<object?> View { get; } = new BindableReactiveProperty<object?>(view);

        public void Dispose()
        {
        }
    }
}
