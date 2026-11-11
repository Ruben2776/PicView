using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PicView.Avalonia.Crop;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC;

/// <summary>
/// The view a tab shows while a crop is open. It zooms through its own <see cref="CustomControls.ZoomPanControl"/>
/// (ADR-0027): the wheel and the zoom Actions zoom it, Space and a drag pan it, and a plain drag moves or resizes
/// the selection. The handles, the selection's border and the size label keep their size on screen at any zoom.
/// </summary>
public partial class CropControl : UserControl, IZoomScrollTarget
{
    private const double SelectionBorderThickness = 2;

    private readonly CropDragHandler? _dragHandler;
    private readonly CropLayoutManager? _layoutManager;
    private readonly CropResizeHandler? _resizeHandler;

    // What is drawn over the image at a fixed size on screen, each with the transform that undoes the zoom
    private readonly ScaleTransform[] _constantSize;
    private IDisposable? _imageWidthSubscription;
    private IDisposable? _imageHeightSubscription;
    private bool _isLoadedOnce;

    public CropControl()
    {
        InitializeComponent();
        if (OperatingSystem.IsMacOS())
        {
            MainRectangle.Cursor = Cursor.Parse("DragMove");
        }

        _dragHandler = new CropDragHandler(this);
        _resizeHandler = new CropResizeHandler(this);
        _layoutManager = new CropLayoutManager(this);

        _constantSize =
        [
            KeepSize(TopLeftButton), KeepSize(TopRightButton), KeepSize(TopMiddleButton),
            KeepSize(BottomLeftButton), KeepSize(BottomRightButton), KeepSize(BottomMiddleButton),
            KeepSize(LeftMiddleButton), KeepSize(RightMiddleButton), KeepSize(SizeBorder)
        ];
        // The label sits above the selection's corner, so it shrinks towards it
        SizeBorder.RenderTransformOrigin = new RelativePoint(0, 1, RelativeUnit.Relative);

        ZoomPanControl.ScaleChanged += OnScaleChanged;
        ZoomPanControl.SpaceHeldChanged += OnSpaceHeldChanged;
        ZoomPanControl.Initialize();

        AddHandler(PointerWheelChangedEvent, PreviewOnPointerWheelChanged, RoutingStrategies.Tunnel);
        AddHandler(PointerTouchPadGestureMagnifyEvent, TouchMagnifyEvent, RoutingStrategies.Bubble);
        AddHandler(PinchEvent, TouchMagnifyEvent, RoutingStrategies.Bubble);

        Loaded += OnControlLoaded;
        Unloaded += OnControlUnloaded;
    }

    private static ScaleTransform KeepSize(Control element)
    {
        var transform = new ScaleTransform(1, 1);
        element.RenderTransform = transform;
        return transform;
    }

    private void OnControlLoaded(object? sender, RoutedEventArgs e)
    {
        if (!_isLoadedOnce)
        {
            _isLoadedOnce = true;
            InitLoaded();
        }

        if (DataContext is not TabViewModel tab)
        {
            return;
        }

        // The Fit Scale changes with the fitted size, as when the window is resized: the selection stays on its pixels
        var window = tab.ParentWindowContext;
        _imageWidthSubscription = window.ImageWidth.Subscribe(_ => Refit(),
            DebugHelper.LogError(nameof(CropControl), nameof(Refit)));
        _imageHeightSubscription = window.ImageHeight.Subscribe(_ => Refit(),
            DebugHelper.LogError(nameof(CropControl), nameof(Refit)));
    }

    private void OnControlUnloaded(object? sender, RoutedEventArgs e)
    {
        _imageWidthSubscription?.Dispose();
        _imageWidthSubscription = null;
        _imageHeightSubscription?.Dispose();
        _imageHeightSubscription = null;
    }

    private void Refit()
    {
        if (DataContext is not TabViewModel { Crop: { } crop })
        {
            return;
        }

        crop.Refit();
        _layoutManager.UpdateLayout();
    }

    private void InitLoaded()
    {
        if (Settings.Theme.GlassTheme)
        {
            var glassBrush = new SolidColorBrush(Color.FromArgb(120, 225, 225, 225));
            var glassBrushAlt = new SolidColorBrush(Color.FromArgb(150, 225, 225, 225));
            SizeBorder.Background = glassBrush;
            TopLeftButton.Background = glassBrushAlt;
            BottomRightButton.Background = glassBrushAlt;
            BottomLeftButton.Background = glassBrushAlt;
            TopRightButton.Background = glassBrushAlt;
            TopMiddleButton.Background = glassBrushAlt;
            BottomMiddleButton.Background = glassBrushAlt;
            LeftMiddleButton.Background = glassBrushAlt;
            RightMiddleButton.Background = glassBrushAlt;
        }
        else if (!Settings.Theme.Dark)
        {
            if (Application.Current.TryGetResource("SecondaryTextColor",
                    Application.Current.RequestedThemeVariant, out var textColor) && textColor is Color color)
            {
                SizeBorder.Background = new SolidColorBrush(color);
            }
        }

        InitializeResizeHandlers();
        if (DataContext is TabViewModel tab)
        {
            _layoutManager.InitializeLayout(tab.ParentWindowContext);
        }


        MainRectangle.PointerPressed += _dragHandler.OnDragStart;
        MainRectangle.PointerReleased += _dragHandler.OnDragEnd;
        MainRectangle.PointerMoved += _dragHandler.OnDragMove;
        MainRectangle.PointerMoved += (_, _) => _layoutManager.UpdateLayout();

        TopLeftButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
        TopRightButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
        BottomLeftButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
        BottomRightButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
        LeftMiddleButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
        RightMiddleButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
        TopMiddleButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
        BottomMiddleButton.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);

        TopLeftButton.PointerReleased += _resizeHandler.OnResizeEnd;
        TopRightButton.PointerReleased += _resizeHandler.OnResizeEnd;
        BottomLeftButton.PointerReleased += _resizeHandler.OnResizeEnd;
        BottomRightButton.PointerReleased += _resizeHandler.OnResizeEnd;
        LeftMiddleButton.PointerReleased += _resizeHandler.OnResizeEnd;
        RightMiddleButton.PointerReleased += _resizeHandler.OnResizeEnd;
        TopMiddleButton.PointerReleased += _resizeHandler.OnResizeEnd;
        BottomMiddleButton.PointerReleased += _resizeHandler.OnResizeEnd;

        LostFocus += OnControlLostFocus;
    }

    private void OnControlLostFocus(object? sender, RoutedEventArgs e)
    {
        _dragHandler.Reset();
        _resizeHandler.Reset();
    }

    private void InitializeResizeHandlers()
    {
        var resizeControls = new Dictionary<Border, CropResizeMode>
        {
            { TopLeftButton, CropResizeMode.TopLeft },
            { TopRightButton, CropResizeMode.TopRight },
            { BottomLeftButton, CropResizeMode.BottomLeft },
            { BottomRightButton, CropResizeMode.BottomRight },
            { LeftMiddleButton, CropResizeMode.Left },
            { RightMiddleButton, CropResizeMode.Right },
            { TopMiddleButton, CropResizeMode.Top },
            { BottomMiddleButton, CropResizeMode.Bottom }
        };

        foreach (var control in resizeControls)
        {
            control.Key.PointerPressed += (_, e) => _resizeHandler.OnResizeStart(e);
            control.Key.PointerMoved += (s, e) => _resizeHandler.OnResizeMove(s, e, control.Value);
            control.Key.PointerMoved += (s, e) => _layoutManager.UpdateLayout();
            control.Key.PointerReleased += _resizeHandler.OnResizeEnd;
        }
    }

    #region Zoom

    /// <summary>
    /// Keeps the handles, the selection's border and the size label at their size on screen, and reports the scale
    /// to the tab as its Zoom Factor, as the viewer does (ADR-0016).
    /// </summary>
    private void OnScaleChanged(double scale)
    {
        if (scale <= 0 || double.IsNaN(scale))
        {
            return;
        }

        var inverse = 1 / scale;
        foreach (var transform in _constantSize)
        {
            transform.ScaleX = inverse;
            transform.ScaleY = inverse;
        }

        MainRectangle.BorderThickness = new Thickness(SelectionBorderThickness * inverse);

        if (DataContext is TabViewModel tab)
        {
            tab.Zoom.SetFactor(scale);
        }
    }

    /// <summary>
    /// While Space is held the pointer pans, so the selection lets it through and ends any move or resize.
    /// </summary>
    private void OnSpaceHeldChanged(bool held)
    {
        RootCanvas.IsHitTestVisible = !held;
        if (held)
        {
            _dragHandler.Reset();
            _resizeHandler.Reset();
        }
    }

    /// <inheritdoc cref="CustomControls.ZoomPanControl.ResetZoom(bool)"/>
    public void ResetZoom() => ZoomPanControl.ResetZoom(false);

    private void TouchMagnifyEvent(object? sender, PointerDeltaEventArgs e) =>
        ZoomPanControl.ZoomWithPointerWheelCore(e.Delta.Y > 0, e.GetPosition(ZoomPanControl));

    private async ValueTask PreviewOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // The wheel only works on the crop the active tab shows
        if (TopLevel.GetTopLevel(this) is not MainWindow
            {
                DataContext: MainWindowViewModel { WindowTabs.ActiveTab.CurrentValue.CurrentView.Value: CropControl }
            } mainWindow)
        {
            return;
        }

        e.Handled = await mainWindow.KeybindRouter.WheelAsync(Keybind.FromWheel(e.Delta.Y, e.KeyModifiers),
            e.Pointer.Type is PointerType.Touch, e.GetPosition(ZoomPanControl), this).ConfigureAwait(true);
    }

    // The crop surface never scrolls: the wheel and the keys zoom it, and Space with a drag pans it
    bool IZoomScrollTarget.CanScrollVertically => false;

    bool IZoomScrollTarget.IsImageTallerThanViewer => false;

    void IZoomScrollTarget.ScrollLine(bool down)
    {
    }

    void IZoomScrollTarget.ScrollToHome()
    {
    }

    void IZoomScrollTarget.ScrollToEnd()
    {
    }

    void IZoomScrollTarget.ZoomIn() => ZoomPanControl.ZoomIn();

    void IZoomScrollTarget.ZoomOut() => ZoomPanControl.ZoomOut();

    void IZoomScrollTarget.ZoomAt(bool zoomIn, Point pointer) =>
        ZoomPanControl.ZoomWithPointerWheelCore(zoomIn, pointer);

    #endregion
}
