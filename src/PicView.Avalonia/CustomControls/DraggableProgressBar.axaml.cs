using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using R3;

namespace PicView.Avalonia.CustomControls;

public class DraggableProgressBar : TemplatedControl
{
    // Define the Maximum property
    public static readonly StyledProperty<int> MaximumProperty =
        AvaloniaProperty.Register<DraggableProgressBar, int>(nameof(Maximum), 100);

    // Define the CurrentIndex property with two-way binding
    public static readonly StyledProperty<int> CurrentIndexProperty =
        AvaloniaProperty.Register<DraggableProgressBar, int>(nameof(CurrentIndex),
            defaultBindingMode: BindingMode.TwoWay);

    // Define a property for the thumb's fill color
    public static readonly StyledProperty<IBrush?> ThumbFillProperty =
        AvaloniaProperty.Register<DraggableProgressBar, IBrush?>(nameof(ThumbFill));

    // Define the DragSensitivity property
    public static readonly StyledProperty<double> DragSensitivityProperty =
        AvaloniaProperty.Register<DraggableProgressBar, double>(nameof(DragSensitivity), 1.0);

    private int _dragStartIndex;
    private Point _dragStartPoint;

    private Ellipse? _thumb;
    private Border? _track;

    private readonly CompositeDisposable _disposables = new();

    private bool _shouldUpdate;

    static DraggableProgressBar()
    {
        // This allows the control to react to property changes
        AffectsRender<DraggableProgressBar>(CurrentIndexProperty, MaximumProperty);
        AffectsMeasure<DraggableProgressBar>(CurrentIndexProperty, MaximumProperty);
    }

    public DraggableProgressBar()
    {
        Loaded += OnLoaded;
        LostFocus += OnLostFocus;
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        IsDragging = false;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        ToolTip.SetPlacement(this, PlacementMode.Top);
        ToolTip.SetVerticalOffset(this, -3);
        
        // Observe the CurrentIndexProperty for changes,
        // wait for a 25ms pause in changes (debounce), and then emit the last value.
        CurrentIndexProperty.Changed.ToObservable()
            .Debounce(TimeSpan.FromMilliseconds(25))
            .Skip(1) // Skip first loading, when it is just setup
            .SubscribeAwait(async (x, cancel) =>
            {
                // Check if the new value exists and is different from the old one.
                if (x.NewValue.HasValue && x.OldValue.HasValue && x.NewValue.Value != x.OldValue.Value)
                {
                    if (IsDragging)
                    {
                        var isReverse = x.NewValue.Value < x.OldValue.Value;
                        // Use lightweight image changing (without changing size) while dragging:
                        await NavigationManager.ImageIterator.IterateToIndexSlim(x.NewValue.Value, isReverse, cancel);
                        _shouldUpdate = true;
                    }
                    else
                    {
                        await NavigationManager.ImageIterator.IterateToIndex(x.NewValue.Value, cancel);
                        _shouldUpdate = false;
                    }
                }
            })
            .AddTo(_disposables);
        UpdateThumbPosition();
    }

    public bool IsDragging { get; private set; }

    public int Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public int CurrentIndex
    {
        get => GetValue(CurrentIndexProperty);
        set => SetValue(CurrentIndexProperty, value);
    }

    public IBrush? ThumbFill
    {
        get => GetValue(ThumbFillProperty);
        set => SetValue(ThumbFillProperty, value);
    }

    public double DragSensitivity
    {
        get => GetValue(DragSensitivityProperty);
        set => SetValue(DragSensitivityProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _track = e.NameScope.Find<Border>("PART_Track");
        _thumb = e.NameScope.Find<Ellipse>("PART_Thumb");

        if (!Settings.Theme.Dark)
        {
            _track.Background = UIHelper.GetBrush("SecondaryBackgroundColor");
            _thumb.Fill = UIHelper.GetBrush("TertiaryBackgroundColor");
        }
    }

    // Recalculate thumb position when CurrentIndex or Maximum changes
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if ((change.Property != CurrentIndexProperty && change.Property != MaximumProperty) || IsDragging)
        {
            return;
        }

        if (_track is not null && _thumb is not null)
        {
            UpdateThumbPosition();
        }
    }

    public void UpdateThumbPosition()
    {
        if (_thumb == null)
        {
            return;
        }

        var position = IndexToPosition(CurrentIndex);

        if (_thumb.RenderTransform is TranslateTransform transform)
        {
            transform.X = position;
        }
        else
        {
            _thumb.RenderTransform = new TranslateTransform(position, 0);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_track == null || _thumb == null)
        {
            return;
        }

        var properties = e.GetCurrentPoint(_track).Properties;
        if (!properties.IsLeftButtonPressed)
        {
            return;
        }

        var clickPosition = e.GetPosition(_track);

        // Get the thumb's current visual bounds relative to the track
        var thumbBounds = _thumb.Bounds;
        if (_thumb.RenderTransform is TranslateTransform transform)
        {
            thumbBounds = thumbBounds.WithX(transform.X);
        }

        // Expand bounds horizontally by 2x
        var expandedBounds = new Rect(
            thumbBounds.X - thumbBounds.Width / 2, 
            thumbBounds.Y,
            thumbBounds.Width * 2,                  
            thumbBounds.Height                    
        );

        // Check if the click was inside the expanded thumb area
        if (!expandedBounds.Contains(clickPosition))
        {
            // Click was on the track (outside expanded thumb), so jump to position
            IsDragging = false; 
            Task.Run(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateIndexFromPosition(clickPosition.X);
                }, DispatcherPriority.Send);
                await WindowResizing.SetSizeAsync(DataContext as MainViewModel);
            });

        }

        // Click was on (or near) the thumb, so start dragging
        IsDragging = true;
        _dragStartPoint = e.GetPosition(this);
        _dragStartIndex = CurrentIndex;
        e.Pointer.Capture(_thumb);
    }


    /// <summary>
    /// Show Position on hover, or handle dragging
    /// </summary>
    /// <param name="e"></param>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_track == null || _thumb == null)
        {
            return;
        }

        var trackWidth = GetTrackWidth();

        if (!IsDragging)
        {
            // Show index position on hover
            
            var pos = e.GetPosition(_track);
            if (GetThumbBounds().Contains(pos))
            {
                ToolTip.SetIsOpen(this, false);
                return;
            }

            var pointerOverIndex = Math.Max(PositionToIndex(pos.X) + 1, 1);
            ToolTip.SetTip(this, $"{pointerOverIndex}/{Maximum}");
            ToolTip.SetIsOpen(this, true);
            return;
        }

        // Dragging
        var currentPosition = e.GetPosition(this);
        var deltaX = currentPosition.X - _dragStartPoint.X;

        var pixelsPerIndex = trackWidth / Math.Max(1, Maximum - 1);
        var sensitiveDragPerIndex = pixelsPerIndex * DragSensitivity;
        if (Math.Abs(sensitiveDragPerIndex) < 0.001)
        {
            return;
        }

        var indexChange = deltaX / sensitiveDragPerIndex;
        var newIndex = _dragStartIndex + indexChange;

        UpdateIndexFromPosition(IndexToPosition((int)Math.Round(newIndex)));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_shouldUpdate)
        {
            var vm = DataContext as MainViewModel;
            // Update from lightweight image loading to properly instantiate everything and update size
            _ = NavigationManager.ImageIterator.SlimUpdate(CurrentIndex, vm.PicViewer.ImageSource.CurrentValue);

            IsDragging = false;
            e.Pointer.Capture(null);
            return;
        }
        if (!IsDragging)
        {
            return;
        }

        IsDragging = false;
        e.Pointer.Capture(null);
    }

    private void UpdateIndexFromPosition(double x)
    {
        var newIndex = PositionToIndex(x);
        if (CurrentIndex == newIndex)
        {
            return;
        }

        CurrentIndex = newIndex;
        UpdateThumbPosition();
    }

    // Ensure the thumb is in the correct position when the control is resized
    protected override Size ArrangeOverride(Size finalSize)
    {
        var arrangedSize = base.ArrangeOverride(finalSize);
        UpdateThumbPosition();
        return arrangedSize;
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        _disposables.Dispose();

        Loaded -= OnLoaded;
        LostFocus -= OnLostFocus;
    }

    #region Helpers

    private double GetTrackWidth() =>
        _track is { } t && _thumb is { } th ? Math.Max(0, t.Bounds.Width - th.Bounds.Width) : 0;

    private Rect GetThumbBounds()
    {
        if (_thumb is null)
        {
            return default;
        }

        var bounds = _thumb.Bounds;
        if (_thumb.RenderTransform is TranslateTransform transform)
        {
            bounds = bounds.WithX(transform.X);
        }

        return bounds;
    }

    private int PositionToIndex(double x)
    {
        var trackWidth = GetTrackWidth();
        if (trackWidth <= 0 || Maximum <= 1)
        {
            return 0;
        }

        var clampedX = Math.Clamp(x - _thumb!.Width / 2, 0, trackWidth);
        var percentage = clampedX / trackWidth;
        return (int)Math.Round(percentage * (Maximum - 1));
    }

    private double IndexToPosition(int index)
    {
        var trackWidth = GetTrackWidth();
        if (trackWidth <= 0 || Maximum <= 1)
        {
            return 0;
        }

        var clampedIndex = Math.Clamp(index, 0, Maximum - 1);
        return (double)clampedIndex / (Maximum - 1) * trackWidth;
    }

    #endregion
}