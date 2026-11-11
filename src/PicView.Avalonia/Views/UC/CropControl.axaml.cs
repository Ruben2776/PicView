using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using PicView.Avalonia.Crop;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.CustomControls.Zoom;
using PicView.Avalonia.Input;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC;

/// <summary>
/// The view a tab shows while a crop is open. It zooms through its own <see cref="CustomControls.Zoom.ZoomPanControl"/>
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
    private DisposableBag _disposables;
    private bool _isLoadedOnce;

    /// <param name="start">The view the crop opens at, taken from the tab's viewer.</param>
    public CropControl(ZoomView start)
    {
        PendingZoomView = start;
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
        ZoomPanControl.ViewChanged += KeepSelectionInView;

        // The menu takes the keyboard, so the window may never see Space released while it is open
        CropContextMenu.Opened += (_, _) => ZoomPanControl.OnWindowDeactivated();
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
        // The view comes first, since the default selection is placed in the part of the image it shows
        if (PendingZoomView is { } pending)
        {
            PendingZoomView = null;
            ShowZoomView(pending);
        }

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
        window.ImageWidth.Subscribe(_ => Refit(),
            DebugHelper.LogError(nameof(CropControl), nameof(Refit)));
        window.ImageHeight.Subscribe(_ => Refit(),
            DebugHelper.LogError(nameof(CropControl), nameof(Refit)));
        window.ScrollViewerWidth.Subscribe(_ => SizeViewPanel(window),
            DebugHelper.LogError(nameof(CropControl), nameof(SizeViewPanel)));
       window.ScrollViewerHeight.Subscribe(_ => SizeViewPanel(window),
            DebugHelper.LogError(nameof(CropControl), nameof(SizeViewPanel)))
           .AddTo(ref _disposables);
    }

    private static double AtLeast(double view, double image) => double.IsNaN(image) ? view : Math.Max(view, image);

    /// <summary>
    /// Makes the zoomed panel at least as large as the view. A view area that is not sized, as while the window
    /// fits the image, leaves the panel to fill the control. An image fitted larger than the view, as a tall image
    /// with scrolling on, makes the panel as large as the image, starting at the view's top left, so the whole of
    /// it can be zoomed and panned to.
    /// </summary>
    private void SizeViewPanel(MainWindowViewModel window)
    {
        var width = window.ScrollViewerWidth.CurrentValue;
        var height = window.ScrollViewerHeight.CurrentValue;
        var isWidthKnown = !double.IsNaN(width);
        var isHeightKnown = !double.IsNaN(height);
        // A minimum holds when the panel is arranged, where a size it merely asks for is cut to the room there is
        ViewPanel.MinWidth = isWidthKnown ? AtLeast(width, window.ImageWidth.CurrentValue) : 0;
        ViewPanel.MinHeight = isHeightKnown ? AtLeast(height, window.ImageHeight.CurrentValue) : 0;
        ViewPanel.HorizontalAlignment = isWidthKnown ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
        ViewPanel.VerticalAlignment = isHeightKnown ? VerticalAlignment.Top : VerticalAlignment.Stretch;
    }

    private void OnControlUnloaded(object? sender, RoutedEventArgs e)
    {
        _disposables.Dispose();
    }

    /// <summary>Places the selection's rectangle, its handles and the dimming from the crop's selection.</summary>
    internal void UpdateSelectionLayout() => _layoutManager.UpdateLayout();

    private void Refit()
    {
        if (DataContext is not TabViewModel { Crop: { } crop } tab)
        {
            return;
        }

        SizeViewPanel(tab.ParentWindowContext);
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
            control.Key.PointerMoved += (_, _) => _layoutManager.UpdateLayout();
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
        if (scale is <= 0 or double.NaN)
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
    /// Moves the selection into the part of the image the view shows after a zoom or a pan, as a move by hand
    /// keeps it there. A selection too long for the view on an axis is left where it is on that axis.
    /// </summary>
    private void KeepSelectionInView()
    {
        // Before the first layout there is no selection to keep
        if (!_isLoadedOnce || DataContext is not TabViewModel { Crop: { } crop } tab)
        {
            return;
        }

        var window = tab.ParentWindowContext;
        var image = new Rect(0, 0, window.ImageWidth.CurrentValue, window.ImageHeight.CurrentValue);
        if (double.IsNaN(image.Width) || double.IsNaN(image.Height))
        {
            return;
        }

        var selection = new Rect(crop.SelectionX.Value, crop.SelectionY.Value, crop.SelectionWidth.Value,
            crop.SelectionHeight.Value);
        var moved = CropDragHandler.ClampMove(selection, selection.Position, SelectionBounds(), image);

        // Both lie on whole image pixels, so less than half a pixel apart is the same place
        var pixel = crop.AspectRatio / 2;
        if (Math.Abs(moved.X - selection.X) < pixel && Math.Abs(moved.Y - selection.Y) < pixel)
        {
            // The handles and the size label are still placed inside what the view now shows, at its scale
            _layoutManager.UpdateButtonPositions();
            return;
        }

        crop.SetSelection(moved.X, moved.Y, selection.Width, selection.Height);
        _layoutManager.UpdateLayout();
    }

    /// <summary>
    /// While Space is held the pointer pans, so the selection lets it through and ends any move or resize.
    /// </summary>
    private void OnSpaceHeldChanged(bool held)
    {
        RootCanvas.IsHitTestVisible = !held;
        if (!held)
        {
            return;
        }

        _dragHandler.Reset();
        _resizeHandler.Reset();
    }

    /// <inheritdoc cref="CustomControls.Zoom.ZoomPanControl.ResetZoom(bool)"/>
    public void ResetZoom() => ZoomPanControl.ResetZoom(false);

    /// <summary>A view taken before this crop is laid out, which it shows when it is loaded.</summary>
    internal ZoomView? PendingZoomView { get; private set; }

    /// <summary>
    /// How far this crop is zoomed and where it looks, to hand back to the tab's viewer (ADR-0027).
    /// </summary>
    public ZoomView GetZoomView() =>
        PendingZoomView ?? ZoomView.From(ZoomPanControl.Scale, ZoomPanControl.ViewCenter, SurfaceRect());

    /// <summary>
    /// Shows <paramref name="view"/>, at once when this crop is shown, else when it is loaded.
    /// </summary>
    public void SetZoomView(ZoomView view)
    {
        if (IsLoaded)
        {
            ShowZoomView(view);
            return;
        }

        PendingZoomView = view;
    }

    private void ShowZoomView(ZoomView view) =>
        ZoomPanControl.ShowChildAt(view.Scale, view.ToChildCenter(SurfaceRect()));

    // Where the crop surface, the fitted image, lies in the zoom control's child, or nothing before the first layout
    private Rect SurfaceRect() =>
        ZoomPanControl.Child is { } child && CropSurface.TranslatePoint(default, child) is { } origin
            ? new Rect(origin, CropSurface.Bounds.Size)
            : default;

    /// <summary>
    /// What the selection may reach when moved or resized: the whole image pixels in the part of the surface the
    /// view shows, so no edge of it comes to lie outside the view.
    /// </summary>
    internal Rect SelectionBounds()
    {
        var visible = VisibleSurfaceRect();
        return DataContext is TabViewModel { Crop: { } crop }
            ? CropLayoutManager.SnapInward(visible, crop.AspectRatio)
            : visible;
    }

    /// <summary>
    /// The part of the crop surface the view shows, in the surface's own units: all of it when not zoomed in.
    /// </summary>
    internal Rect VisibleSurfaceRect()
    {
        var surface = SurfaceRect();
        var whole = new Rect(surface.Size);
        if (surface.Width <= 0 || surface.Height <= 0)
        {
            return whole;
        }

        var visible = ZoomPanControl.VisibleChildRect.Translate(new Vector(-surface.X, -surface.Y)).Intersect(whole);
        return visible is { Width: > 0, Height: > 0 } ? visible : whole;
    }

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

    void IZoomScrollTarget.ScrollLine(bool down) { }

    void IZoomScrollTarget.ScrollToHome() { }

    void IZoomScrollTarget.ScrollToEnd() { }

    void IZoomScrollTarget.ZoomIn() => ZoomPanControl.ZoomIn();

    void IZoomScrollTarget.ZoomOut() => ZoomPanControl.ZoomOut();

    void IZoomScrollTarget.ZoomAt(bool zoomIn, Point pointer) =>
        ZoomPanControl.ZoomWithPointerWheelCore(zoomIn, pointer);

    #endregion
}
