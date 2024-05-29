using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia;

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
public class AutoScrollViewer : ScrollViewer
{
    protected override Type StyleKeyOverride => typeof(AutoScrollViewer);

    private readonly Subject<bool> _autoScrollingSubject = new();
    private readonly CompositeDisposable _disposables = new();

    private bool _isAutoScrolling;

    /// <summary>
    /// Gets or sets a value indicating whether auto-scrolling is active.
    /// </summary>
    public bool IsAutoScrolling
    {
        get => _isAutoScrolling;
        set
        {
            if (_isAutoScrolling == value)
            {
                return;
            }

            _isAutoScrolling = value;
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
            PointerMovedEvent,
            PointerMovedHandler,
            routes: RoutingStrategies.Tunnel,
            handledEventsToo: true);
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
            .ObserveOn(RxApp.MainThreadScheduler)
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
            .DisposeWith(_disposables);

        LostFocus += (_, _) => IsAutoScrolling = false;
        ScrollChanged += (_, _) => _autoScrollingSubject.OnNext(IsAutoScrolling);
    }

    /// <summary>
    /// Handles the pointer pressed event to start auto-scrolling if the middle mouse button is pressed.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void PreviewPointerPressedEvent(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            IsAutoScrolling = false;
            return;
        }

        e.Handled = true;
        StartAutoScroll(e);
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
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => PerformAutoScroll())
            .DisposeWith(_disposables);
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

        var speedFactor = 0.1;
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

