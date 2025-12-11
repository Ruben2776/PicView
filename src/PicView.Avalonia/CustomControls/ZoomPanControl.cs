using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;

namespace PicView.Avalonia.CustomControls;

public class ZoomPanControl : Decorator
{
    #region Properties and Fields

    // Styled Properties
    public static readonly StyledProperty<double> DeadzoneToleranceProperty =
        AvaloniaProperty.Register<ZoomPanControl, double>(nameof(DeadzoneTolerance), 0.05);

    /// <summary>
    /// The tolerance range around 1.0 where zoom will snap to reset (1.0).
    /// For example, 0.05 means zoom values between 0.95 and 1.05 will snap to 1.0.
    /// </summary>
    public double DeadzoneTolerance
    {
        get => GetValue(DeadzoneToleranceProperty);
        set => SetValue(DeadzoneToleranceProperty, Math.Max(0, value));
    }

    // Transform Properties

    public double Scale { get; private set; } = 1.0;

    public double TranslateX { get; private set; }

    public double TranslateY { get; private set; }

    /// <summary>
    /// Represents the current zoom level as a percentage.
    /// A value of 100 corresponds to the default zoom level, while values higher or lower indicate zoomed-in or zoomed-out states, respectively.
    /// </summary>
    public double ZoomLevel { get; private set; } = 100;

    // Transform Objects (persistent for animations)
    private ScaleTransform? _scaleTransform;
    private TranslateTransform? _translateTransform;
    private TransformGroup? _transformGroup;

    public static readonly TimeSpan ZoomAnimationDuration = TimeSpan.FromSeconds(0.25);

    // Panning State
    private bool _isPanning;
    private Point _panStartPointer;
    private Point _panStartTranslate;

    // UI Components
    private ZoomPreviewer? _zoomPreviewer;

    #endregion

    #region Initialization and Lifecycle

    public void Initialize()
    {
        // Pointer handling for panning
        AddHandler(PointerPressedEvent, HandleResetZoomOrStartPanning, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, HandlePanning, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, StopPanning, RoutingStrategies.Tunnel);

        _zoomPreviewer = new ZoomPreviewer
        {
            DataContext = DataContext,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 25, 25),
            ZIndex = 90,
            IsVisible = false
        };
        _zoomPreviewer.SetZoomPanControl(this);

        // TODO: should figure out how to make a more self-contained approach, for ZoomPreviewer, for possible future tab-based layout
        UIHelper.GetMainView.MainGrid.Children.Add(_zoomPreviewer);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Show preview window when attached
        if (_zoomPreviewer is not { IsVisible: false })
        {
            return;
        }

        _zoomPreviewer.SetVisible();
        UpdatePreviewWindow();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_isPanning)
        {
            // After layout, ensure transforms are constrained
            ConstrainTranslationToBounds();
            UpdateChildTransform();
        }

        return base.ArrangeOverride(finalSize);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        RemoveHandler(PointerPressedEvent, HandleResetZoomOrStartPanning);
        RemoveHandler(PointerMovedEvent, HandlePanning);
        RemoveHandler(PointerReleasedEvent, StopPanning);

        UIHelper.GetMainView.MainGrid.Children.Remove(_zoomPreviewer);
        _zoomPreviewer = null;
        base.OnDetachedFromVisualTree(e);
    }

    #endregion

    #region Public Zoom API

    /// <summary>
    /// Handles zooming functionality using the mouse pointer wheel. Zooms in or out based on the scroll direction.
    /// </summary>
    /// <param name="e">The event arguments containing details about the pointer wheel input.</param>
    public void ZoomWithPointerWheel(PointerWheelEventArgs e) =>
        ZoomWithPointerWheelCore(e.Delta.Y > 0, e.GetPosition(this));

    /// <inheritdoc cref="ZoomWithPointerWheel(PointerWheelEventArgs)"/>
    public void ZoomWithPointerWheel(PointerDeltaEventArgs e) =>
        ZoomWithPointerWheelCore(e.Delta.Y > 0, e.GetPosition(this));

    /// <summary>
    /// Zooms in the content by increasing the scale based on the specified multiplier.
    /// Updates the zoom level and optionally animates the zoom effect while focusing on a specific point.
    /// </summary>
    /// <param name="multiplier">The factor by which the scale is increased. Defaults to 1.2.</param>
    /// <param name="zoomAtCursorPoint">The point to zoom around. Defaults to the center if null.</param>
    public void ZoomIn(double multiplier = 1.2, Point? zoomAtCursorPoint = null)
    {
        var center = zoomAtCursorPoint ?? CenterPoint();
        var targetScale = Scale * multiplier;

        ZoomBy(targetScale, Settings.Zoom.IsZoomAnimated, center);
    }

    /// <summary>
    /// Zooms out the view by a specified multiplier, applying deadzone logic
    /// and animation if enabled.
    /// </summary>
    /// <param name="multiplier">The factor by which to decrease the zoom level. For example, a multiplier of 1/1.2 reduces the scale.</param>
    public void ZoomOut(double multiplier = 1.0 / 1.2)
    {
        var center = CenterPoint();
        var targetScale = Scale * multiplier;

        ZoomBy(targetScale, Settings.Zoom.IsZoomAnimated, center);
    }

    /// <summary>
    /// Resets the zoom level to its default state. Optionally allows enabling animations during the reset process.
    /// </summary>
    /// <param name="animated">Determines whether the reset should be animated.</param>
    public void ResetZoom(bool animated)
    {
        if (Child == null)
        {
            return;
        }

        _zoomPreviewer?.IsVisible = false;

        ApplyZoomAndTitle(1.0, CenterPoint(), animated);
        SetZoomValue(100);
    }

    /// <summary>
    /// Used to quickly reset the zoom, I.E, when changing picture.
    /// </summary>
    public void ResetZoomSlim()
    {
        SetTransitions(false);
        Scale = 1.0;
        TranslateX = 0;
        TranslateY = 0;
        SetScaleImmediate(1.0, CenterPoint());

        SetZoomValue(100);
    }

    /// <summary>
    /// Sets the scale of the control immediately, optionally focusing the scaling around a specific point.
    /// Updates the internal zoom level and applies the necessary transformations to the child element.
    /// </summary>
    /// <param name="newScale">The new scale value to apply.</param>
    /// <param name="around">The point around which the scaling should occur. If null, the scaling is applied around the center of the control.</param>
    public void SetScaleImmediate(double newScale, Point? around = null)
    {
        if (double.IsNaN(newScale) || double.IsInfinity(newScale))
        {
            return;
        }

        var center = around ?? CenterPoint();
        ApplyScaleAroundPoint(newScale, center);
        ConstrainTranslationToBounds();
        UpdateChildTransform();

        SetZoomValue(newScale * 100);
    }

    /// <summary>
    /// Sets translation values and ensures they are constrained to bounds.
    /// This method should be used by external controls (like <see cref="Views.UC.ZoomPreviewer"/>) to ensure consistent behavior.
    /// </summary>
    public void SetConstrainedTranslation(double translateX, double translateY)
    {
        TranslateX = translateX;
        TranslateY = translateY;
        ConstrainTranslationToBounds();
        UpdateChildTransform();
    }

    #endregion

    #region Panning Event Handlers

    private void HandleResetZoomOrStartPanning(object? sender, PointerPressedEventArgs e)
    {
        if (Child == null)
        {
            return;
        }

        // Panning shouldn't happen when moving the window by holding shift
        if (e.KeyModifiers == KeyModifiers.Shift)
        {
            return;
        }

        if (e.ClickCount is 2 && Settings.UIProperties.DoubleClickBehavior is 1)
        {
            ResetZoom(Settings.Zoom.IsZoomAnimated);
            return;
        }

        var p = e.GetPosition(this);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || !(Math.Abs(Scale) > 1.0001))
        {
            return;
        }

        // Animated panning feels off, should disable it
        SetTransitions(false);

        _isPanning = true;
        _panStartPointer = p;
        _panStartTranslate = new Point(TranslateX, TranslateY);
        e.Pointer.Capture(this);
    }

    private void HandlePanning(object? sender, PointerEventArgs e)
    {
        if (!_isPanning || Child == null)
        {
            return;
        }

        // Panning shouldn't happen when moving the window by holding shift
        if (e.KeyModifiers == KeyModifiers.Shift)
        {
            return;
        }

        var p = e.GetPosition(this);
        var delta = p - _panStartPointer;

        // delta is in control coordinates; we need to convert that into translate change respecting rotation/scale
        // Given we compose transforms as: Result = Translate + Rotate( Scale * childPoint )
        // The translate we manipulate is in control coordinates directly, so we can add the delta to it,
        // but rotation means dragging direction should rotate together (so we rotate delta by -Rotation to convert?)
        // Simpler and correct: update TranslateX/Y by delta (works because translate is last transform).
        var newTx = _panStartTranslate.X + delta.X;
        var newTy = _panStartTranslate.Y + delta.Y;

        TranslateX = newTx;
        TranslateY = newTy;

        ConstrainTranslationToBounds();
        UpdateChildTransform();
    }

    private void StopPanning(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isPanning)
        {
            return;
        }

        _isPanning = false;
        e.Pointer.Capture(null);
    }

    #endregion

    #region Internal Zoom Logic

    public void ZoomWithPointerWheelCore(bool isZoomIn, Point pos)
    {
        var step = isZoomIn ? Settings.Zoom.ZoomSpeed : -Math.Abs(Settings.Zoom.ZoomSpeed);
        ZoomBy(Math.Max(0.09, Scale + step), Settings.Zoom.IsZoomAnimated, pos);
    }

    /// <summary>
    /// Adjusts the zoom level to the specified target scale. Optionally, applies animation and centers the zoom at the provided point.
    /// </summary>
    /// <param name="targetScale">The desired target scale for zooming.</param>
    /// <param name="animated">Indicates whether the zoom transition should be animated.</param>
    /// <param name="zoomAtPoint">The point at which the zoom should be centered (optional).</param>
    private void ZoomBy(double targetScale, bool animated = true, Point? zoomAtPoint = null)
    {
        var center = zoomAtPoint ?? CenterPoint();

        if (Settings.Zoom.AvoidZoomingOut && targetScale < 1)
        {
            ResetZoom(animated);
            return;
        }

        // Apply deadzone logic
        const double resetZoom = 1.0;
        var lowerBound = resetZoom - DeadzoneTolerance;
        var upperBound = resetZoom + DeadzoneTolerance;

        // Check if target scale is within deadzone
        if (!(targetScale >= lowerBound) || !(targetScale <= upperBound))
        {
            ApplyZoomAndTitle(targetScale, center, animated);
        }
        else
        {
            ResetZoom(animated);
        }
    }

    private void ApplyZoomAndTitle(double targetScale, Point center, bool animated)
    {
        SetTransitionsAndScale(targetScale, center, animated);
        TitleManager.SetTitle(DataContext as MainViewModel);
        if (Settings.Zoom.IsShowingZoomPercentagePopup)
        {
            _ = TooltipHelper.ShowTooltipMessageContinuallyAsync($"{Math.Floor(ZoomLevel)}%", true,
                TimeSpan.FromSeconds(1));
        }
    }

    private void SetTransitionsAndScale(double targetScale, Point center, bool animated)
    {
        SetTransitions(animated);
        SetScaleImmediate(targetScale, center);
    }

    private void SetZoomValue(double zoomValue)
    {
        ZoomLevel = zoomValue;
        if (DataContext is MainViewModel vm)
        {
            vm.PicViewer.ZoomValue.Value = zoomValue;
        }
    }

    /// <summary>
    /// Applies the scale change so that the child point under `controlPoint` remains fixed in control coordinates.
    /// Transform order used: Result = Translate + Scale * childPoint.
    /// </summary>
    private void ApplyScaleAroundPoint(double newScale, Point controlPoint)
    {
        if (Child == null)
        {
            return;
        }

        var s = Scale;
        if (Math.Abs(s - newScale) < 1e-9)
        {
            return;
        }

        var px = (controlPoint.X - TranslateX) / s;
        var py = (controlPoint.Y - TranslateY) / s;

        var newTx = controlPoint.X - newScale * px;
        var newTy = controlPoint.Y - newScale * py;

        Scale = newScale;
        TranslateX = newTx;
        TranslateY = newTy;
    }

    /// <summary>
    /// Applies the RenderTransform on the child according to current properties.
    /// Transform order: Scale (including flipping) -> Rotate -> Translate.
    /// </summary>
    private void UpdateChildTransform()
    {
        if (Child == null)
        {
            return;
        }

        // Initialize transforms only once
        if (_transformGroup == null)
        {
            _scaleTransform = new ScaleTransform();
            _translateTransform = new TranslateTransform();

            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(_scaleTransform);
            _transformGroup.Children.Add(_translateTransform);

            Child.RenderTransform = _transformGroup;
            Child.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
        }

        // Update the properties of the existing transform objects
        // This is what makes animations work!
        _scaleTransform.ScaleX = Scale;
        _scaleTransform.ScaleY = Scale;
        _translateTransform.X = TranslateX;
        _translateTransform.Y = TranslateY;

        // Update preview window after transform change
        UpdatePreviewWindow();
    }

    private void SetTransitions(bool isAnimated)
    {
        if (_scaleTransform == null || _translateTransform == null)
        {
            // Transforms not yet initialized
            return;
        }

        if (!isAnimated)
        {
            _scaleTransform.Transitions = null;
            _translateTransform.Transitions = null;
        }
        else
        {
            // Apply transitions to the persistent transform objects
            _scaleTransform.Transitions ??=
            [
                new DoubleTransition
                {
                    Property = ScaleTransform.ScaleXProperty,
                    Duration = ZoomAnimationDuration
                },

                new DoubleTransition
                {
                    Property = ScaleTransform.ScaleYProperty,
                    Duration = ZoomAnimationDuration
                }
            ];

            _translateTransform.Transitions ??=
            [
                new DoubleTransition
                {
                    Property = TranslateTransform.XProperty,
                    Duration = ZoomAnimationDuration
                },

                new DoubleTransition
                {
                    Property = TranslateTransform.YProperty,
                    Duration = ZoomAnimationDuration
                }
            ];
        }
    }

    /// <summary>
    /// Ensures the transformed child covers the control area (i.e. prevents panning away until whitespace appears).
    /// </summary>
    private void ConstrainTranslationToBounds()
    {
        if (Child == null)
        {
            return;
        }


        // We need the child's size in local coordinates
        var childSize = Child.Bounds.Size;
        if (childSize.Width <= 0 || childSize.Height <= 0 || double.IsNaN(childSize.Width) ||
            double.IsNaN(childSize.Height))
        {
            // Fallback to desired size
            childSize = Child.DesiredSize;
        }

        if (childSize.Width <= 0 || childSize.Height <= 0)
        {
            return;
        }

        // Without rotation, the scaled content bounds are straightforward
        var scaledWidth = childSize.Width * Scale;
        var scaledHeight = childSize.Height * Scale;

        var controlWidth = Bounds.Width;
        var controlHeight = Bounds.Height;

        var desiredTx = TranslateX;
        var desiredTy = TranslateY;

        // Horizontal
        if (scaledWidth <= controlWidth)
        {
            // Center horizontally if content is smaller than control
            desiredTx = (controlWidth - scaledWidth) / 2.0;
        }
        else
        {
            // Constrain to prevent showing whitespace
            // Left edge: TranslateX should be <= 0
            if (desiredTx > 0)
            {
                desiredTx = 0;
            }

            // Right edge: TranslateX + scaledWidth should be >= controlWidth
            if (desiredTx + scaledWidth < controlWidth)
            {
                desiredTx = controlWidth - scaledWidth;
            }
        }

        // Vertical
        if (scaledHeight <= controlHeight)
        {
            // Center vertically if content is smaller than control
            desiredTy = (controlHeight - scaledHeight) / 2.0;
        }
        else
        {
            // Constrain to prevent showing whitespace
            // Top edge: TranslateY should be <= 0
            if (desiredTy > 0)
            {
                desiredTy = 0;
            }

            // Bottom edge: TranslateY + scaledHeight should be >= controlHeight
            if (desiredTy + scaledHeight < controlHeight)
            {
                desiredTy = controlHeight - scaledHeight;
            }
        }

        TranslateX = desiredTx;
        TranslateY = desiredTy;
    }

    #endregion

    #region Utility Methods

    private void UpdatePreviewWindow()
    {
        if (_zoomPreviewer == null)
        {
            return;
        }

        // Update visibility based on zoom state
        _zoomPreviewer.UpdateVisibility();

        // Update viewport rectangle
        _zoomPreviewer.UpdateViewportRect();
    }

    private Point CenterPoint() => new(Bounds.Width / 2.0, Bounds.Height / 2.0);

    #endregion
}