using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using PicView.Avalonia.UI;
using R3;
using CompositeDisposable = R3.CompositeDisposable;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// Specifies the direction in which the AutoScrollViewer can scroll.
/// </summary>
internal enum CanScrollDirection
{
    /// <summary>
    /// Indicates no scrolling is possible.
    /// </summary>
    None,

    /// <summary>
    /// Indicates vertical scrolling is possible.
    /// </summary>
    Vertical,

    /// <summary>
    /// Indicates horizontal scrolling is possible.
    /// </summary>
    Horizontal
}

/// <summary>
/// A custom ScrollViewer that supports auto-scrolling when the middle mouse button is pressed.
/// </summary>
[TemplatePart("PART_AutoScrollSign", typeof(AutoScrollSign))]
[PseudoClasses(ScrollingPseudoClass)]
public class AutoScrollViewer : ScrollViewer
{
    private const string ScrollingPseudoClass = ":scrolling";
    protected override Type StyleKeyOverride => typeof(AutoScrollViewer);

    private readonly Subject<bool> _autoScrollingSubject = new();
    private DisposableBag _disposables;

    /// <summary>
    /// Defines the <see cref="CanScrollUp"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanScrollUpProperty =
        AvaloniaProperty.Register<AutoScrollViewer, bool>(nameof(CanScrollUp));

    /// <summary>
    /// Gets or sets a value indicating whether the viewer can scroll up.
    /// </summary>
    public bool CanScrollUp
    {
        get => GetValue(CanScrollUpProperty);
        private set => SetValue(CanScrollUpProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="CanScrollDown"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanScrollDownProperty =
        AvaloniaProperty.Register<AutoScrollViewer, bool>(nameof(CanScrollDown));

    /// <summary>
    /// Gets or sets a value indicating whether the viewer can scroll down.
    /// </summary>
    public bool CanScrollDown
    {
        get => GetValue(CanScrollDownProperty);
        private set => SetValue(CanScrollDownProperty, value);
    }
    
    /// <summary>
    /// Defines the <see cref="CanScrollLeft"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanScrollLeftProperty =
        AvaloniaProperty.Register<AutoScrollViewer, bool>(nameof(CanScrollLeft));

    /// <summary>
    /// Gets or sets a value indicating whether the viewer can scroll left.
    /// </summary>
    public bool CanScrollLeft
    {
        get => GetValue(CanScrollLeftProperty);
        private set => SetValue(CanScrollLeftProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="CanScrollRight"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanScrollRightProperty =
        AvaloniaProperty.Register<AutoScrollViewer, bool>(nameof(CanScrollRight));

    /// <summary>
    /// Gets or sets a value indicating whether the viewer can scroll right.
    /// </summary>
    public bool CanScrollRight
    {
        get => GetValue(CanScrollRightProperty);
        private set => SetValue(CanScrollRightProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether auto-scrolling is active.
    /// </summary>
    public bool IsAutoScrolling
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            _autoScrollingSubject.OnNext(value);
        }
    }

    /// <summary>
    /// Gets or sets the starting point of auto-scroll.
    /// </summary>
    private static Point AutoScrollOrigin { get; set; }

    /// <summary>
    /// Gets or sets the current point of auto-scroll.
    /// </summary>
    private static Point AutoScrollPos { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoScrollViewer"/> class.
    /// </summary>
    public AutoScrollViewer()
    {
        AddHandler(
            PointerPressedEvent,
            PreviewPointerPressedEvent,
            routes: RoutingStrategies.Tunnel,
            handledEventsToo: true);
        
        AddHandler(
            PointerReleasedEvent,
            PreviewPointerReleasedEvent,
            routes: RoutingStrategies.Tunnel,
            handledEventsToo: true);
        
        AddHandler(
            PointerCaptureLostEvent,
            PreviewPointerLostEvent,
            routes: RoutingStrategies.Tunnel,
            handledEventsToo: true);

        AddHandler(
            PointerMovedEvent,
            PointerMovedHandler,
            routes: RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }
    
    /// <summary>
    /// Scrolls the content to the horizontal end (right) while preserving the vertical offset.
    /// </summary>
    public void ScrollToRightEnd()
    {
        // using PositiveInfinity ensures we scroll to the absolute end 
        // regardless of the current Extent/Viewport calculations.
        SetCurrentValue(OffsetProperty, new Vector(double.PositiveInfinity, Offset.Y));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OffsetProperty ||
            change.Property == ExtentProperty ||
            change.Property == ViewportProperty)
        {
            UpdateScrollButtonsState();
        }
    }

    private void UpdateScrollButtonsState()
    {
        // Vertical Logic
        CanScrollUp = Offset.Y > 0;
        CanScrollDown = Offset.Y < Extent.Height - Viewport.Height;

        // Horizontal Logic
        // Can scroll left if we are not at the absolute start (0)
        CanScrollLeft = Offset.X > 0; 
        
        // Can scroll right if the current position is less than the total width minus the visible width
        CanScrollRight = Offset.X < Extent.Width - Viewport.Width;
    }

    /// <summary>
    /// Applies the control template and initializes the AutoScrollSign icon.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        var autoScrollSign = e.NameScope.Find<AutoScrollSign>("PART_AutoScrollSign");

        _autoScrollingSubject
            .Subscribe(isAutoScrolling =>
            {
                var canScroll = CanScroll();
                switch (canScroll)
                {
                    default:
                        autoScrollSign.IsVisible = false;
                        autoScrollSign.RenderTransform = null;
                        break;
                    case CanScrollDirection.Vertical:
                        autoScrollSign.IsVisible = isAutoScrolling;
                        autoScrollSign.RenderTransform = new RotateTransform(0);
                        Canvas.SetTop(autoScrollSign, AutoScrollOrigin.Y);
                        Canvas.SetLeft(autoScrollSign, AutoScrollOrigin.X);
                        break;
                    case CanScrollDirection.Horizontal:
                        autoScrollSign.IsVisible = isAutoScrolling;
                        autoScrollSign.RenderTransform = new RotateTransform(90);
                        Canvas.SetTop(autoScrollSign, AutoScrollOrigin.Y);
                        Canvas.SetLeft(autoScrollSign, AutoScrollOrigin.X);
                        break;
                }
            })
            .AddTo(ref _disposables);

        // Handle all types of focus loss events to end auto-scrolling
        LostFocus += (_, _) => IsAutoScrolling = false;
        // End auto-scrolling when parent window loses focus
        var parentWindow = this.GetVisualAncestors().OfType<Window>().FirstOrDefault();
        if (parentWindow != null)
        {
            parentWindow.Deactivated += (_, _) => IsAutoScrolling = false;
        }

        ScrollChanged += (_, _) => _autoScrollingSubject.OnNext(IsAutoScrolling);
    }

    /// <summary>
    /// Handles the pointer pressed event to start auto-scrolling if the middle mouse button is pressed.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void PreviewPointerPressedEvent(object? sender, PointerPressedEventArgs e)
    {
        PseudoClasses.Set(ScrollingPseudoClass, true);
        
        if (!e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            IsAutoScrolling = false;
            return;
        }

        e.Handled = true;
        StartAutoScroll(e);
    }
    
    private void PreviewPointerReleasedEvent(object? sender, PointerReleasedEventArgs e)
    {
        PseudoClasses.Set(ScrollingPseudoClass, false);
    }
    
    private void PreviewPointerLostEvent(object? sender, PointerCaptureLostEventArgs e)
    {
        PseudoClasses.Set(ScrollingPseudoClass, false);
    }

    /// <summary>
    /// Starts auto-scrolling based on the pointer pressed event.
    /// </summary>
    /// <param name="e">The pointer pressed event data.</param>
    private void StartAutoScroll(PointerPressedEventArgs e)
    {
        if (IsAutoScrolling)
        {
            IsAutoScrolling = false;
            return;
        }

        var canScroll = CanScroll();
        if (canScroll == CanScrollDirection.None)
        {
            return;
        }

        AutoScrollOrigin = e.GetPosition(this);
        AutoScrollPos = AutoScrollOrigin;
        IsAutoScrolling = true;

        Observable.Interval(TimeSpan.FromMilliseconds(16))
            .TakeUntil(_autoScrollingSubject.Where(isScrolling => !isScrolling))
            .Subscribe(_ => PerformAutoScroll())
            .AddTo(ref _disposables);
    }

    /// <summary>
    /// Handles the pointer moved event to update the current auto-scroll position.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void PointerMovedHandler(object? sender, PointerEventArgs e)
    {
        if (IsAutoScrolling)
        {
            AutoScrollPos = e.GetPosition(this);
        }
    }

    /// <summary>
    /// Performs auto-scrolling based on the current pointer position and the origin.
    /// </summary>
    private void PerformAutoScroll()
    {
        var deltaX = AutoScrollPos.X - AutoScrollOrigin.X;
        var deltaY = AutoScrollPos.Y - AutoScrollOrigin.Y;
        const int deadZone = 20;

        if (Math.Abs(deltaX) < deadZone && Math.Abs(deltaY) < deadZone)
        {
            return;
        }

        const double speedFactor = 0.1;
        var offsetX = Math.Sign(deltaX) * Math.Max(0, Math.Abs(deltaX) - deadZone) * speedFactor;
        var offsetY = Math.Sign(deltaY) * Math.Max(0, Math.Abs(deltaY) - deadZone) * speedFactor;

        Offset = new Vector(Offset.X + offsetX, Offset.Y + offsetY);
    }

    /// <summary>
    /// Determines whether the viewer can scroll and in which direction.
    /// </summary>
    /// <returns>The scroll direction.</returns>
    private CanScrollDirection CanScroll()
    {
        if (Extent.Height > Viewport.Height && VerticalScrollBarVisibility != ScrollBarVisibility.Disabled &&
            VerticalScrollBarVisibility != ScrollBarVisibility.Hidden)
        {
            return CanScrollDirection.Vertical;
        }
        if (Extent.Width > Viewport.Width && HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled &&
            HorizontalScrollBarVisibility != ScrollBarVisibility.Hidden)
        {
            return CanScrollDirection.Horizontal;
        }
        return CanScrollDirection.None;
    }

    /// <summary>
    /// Disposes of the disposables when the control is detached from the visual tree.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _disposables.Dispose();
    }
}