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

internal enum CanScrollDirection
{
    None,
    Vertical,
    Horizontal
}

[TemplatePart("PART_AutoScrollSign", typeof(AutoScrollSign))]
public class AutoScrollViewer : ScrollViewer
{
    protected override Type StyleKeyOverride => typeof(AutoScrollViewer);

    private readonly Subject<bool> _autoScrollingSubject = new();
    private readonly CompositeDisposable _disposables = new();

    private bool _isAutoScrolling;
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
    
    private static Point AutoScrollOrigin { get; set; }
    private static Point AutoScrollPos { get; set; }

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

    private void StartAutoScroll(PointerPressedEventArgs e)
    {
        if (IsAutoScrolling)
        {
            IsAutoScrolling = false;
        }
        else
        {
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
    }

    private void PointerMovedHandler(object? sender, PointerEventArgs e)
    {
        if (IsAutoScrolling)
        {
            AutoScrollPos = e.GetPosition(this);
        }
    }

    private void PerformAutoScroll()
    {
        var deltaX = AutoScrollPos.X - AutoScrollOrigin.X;
        var deltaY = AutoScrollPos.Y - AutoScrollOrigin.Y;
        const double deadZone = 20d;

        if (Math.Abs(deltaX) < deadZone && Math.Abs(deltaY) < deadZone)
        {
            return;
        }

        var speedFactor = 0.1; // Adjust speed factor as necessary
        var offsetX = Math.Sign(deltaX) * Math.Max(0, Math.Abs(deltaX) - deadZone) * speedFactor;
        var offsetY = Math.Sign(deltaY) * Math.Max(0, Math.Abs(deltaY) - deadZone) * speedFactor;

        Offset = new Vector(Offset.X + offsetX, Offset.Y + offsetY);
    }

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

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _disposables.Dispose();
    }
}
