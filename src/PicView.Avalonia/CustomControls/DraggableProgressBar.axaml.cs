using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using R3;

namespace PicView.Avalonia.CustomControls;

public class DraggableProgressBar : TemplatedControl
{
    // Define the Maximum property
    public static readonly StyledProperty<int> MaximumProperty =
        AvaloniaProperty.Register<DraggableProgressBar, int>(nameof(Maximum), 100);

    // Define the CurrentIndex property with two-way binding.
    // This property is 1-based (i.e., from 1 to Maximum).
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

    // Flag to signal that a full, non-slim update is needed after dragging ends
    private bool _needsFullUpdateOnDragEnd;

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
        // Stop dragging if the control loses focus
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
                if (IsDragging)
                {
                    var isReverse = x.NewValue.Value < x.OldValue.Value;
                    // Use lightweight image changing (without changing size) while dragging:
                    await NavigationManager.ImageIterator.IterateToIndexSlim(x.NewValue.Value, isReverse, cancel);
                    _needsFullUpdateOnDragEnd = true;
                }
                else
                {
                    await NavigationManager.ImageIterator.IterateToIndex(x.NewValue.Value, cancel);
                    _needsFullUpdateOnDragEnd = false;
                }
            }, AwaitOperation.Switch)
            .AddTo(_disposables);

        this.GetObservable(PointerReleasedEvent)
            .ToObservable()
            .SubscribeAwait(async (x, _) => { await HandlePointerReleased(x); })
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

        if (Settings.Theme.Dark)
        {
            return;
        }

        _track.Background = UIHelper.GetBrush("SecondaryBackgroundColor");
        _thumb.Fill = UIHelper.GetBrush("TertiaryBackgroundColor");
    }

    // Recalculate thumb position when CurrentIndex or Maximum changes
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // If we are dragging, OnPointerMoved is responsible for updating the thumb.
        // This prevents OnPropertyChanged from conflicting with the drag logic.
        if (IsDragging)
        {
            return;
        }

        if (change.Property != CurrentIndexProperty && change.Property != MaximumProperty)
        {
            return;
        }

        if (_track is not null && _thumb is not null)
        {
            UpdateThumbPosition();
        }
    }

    private void UpdateThumbPosition()
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

        // Expand bounds horizontally by 2x for easier grabbing
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
            CurrentIndex = Math.Max(PositionToIndex(clickPosition.X) - 1, 0);
            return; 
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

        if (!IsDragging)
        {
            // Show index position on hover
            var pos = e.GetPosition(_track);
            if (GetThumbBounds().Contains(pos))
            {
                ToolTip.SetIsOpen(this, false);
                return;
            }

            var pointerOverIndex = PositionToIndex(pos.X);
            ToolTip.SetTip(this, $"{pointerOverIndex}/{Maximum}");
            ToolTip.SetIsOpen(this, true);
            return;
        }

        // --- Dragging ---
        var trackWidth = GetTrackWidth();
        if (trackWidth <= 0)
        {
            return;
        }

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

        var clampedIndex = (int)Math.Clamp(Math.Round(newIndex), 1, Maximum);

        if (CurrentIndex == clampedIndex)
        {
            return;
        }

        // Set the property. This will trigger the Debounced subscription.
        CurrentIndex = clampedIndex;

        // Manually update the thumb's visual position for smooth dragging.
        // OnPropertyChanged is skipped because IsDragging is true.
        UpdateThumbPosition();
    }

    private async ValueTask HandlePointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        // Check if we were the one who captured the pointer
        if (!ReferenceEquals(e.Pointer.Captured, _thumb) && !IsDragging)
        {
            return; // Not dragging, or capture was lost/handled elsewhere
        }

        if (_needsFullUpdateOnDragEnd)
        {
            var vm = DataContext as MainViewModel;
            // Update from lightweight image loading to properly instantiate everything and update size
            await NavigationManager.ImageIterator.SlimUpdate(CurrentIndex, vm?.PicViewer.ImageSource.CurrentValue);
            _needsFullUpdateOnDragEnd = false; // Reset flag
        }

        IsDragging = false;
        e.Pointer.Capture(null);
    }

    // Ensure the thumb is in the correct position when the control is resized
    protected override Size ArrangeOverride(Size finalSize)
    {
        var arrangedSize = base.ArrangeOverride(finalSize);
        // Update position on resize, as track width may have changed
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

    /// <summary>
    /// Converts a pixel position on the track to a 1-based index.
    /// </summary>
    private int PositionToIndex(double x)
    {
        var trackWidth = GetTrackWidth();
        if (_thumb is null || trackWidth <= 0 || Maximum <= 1)
        {
            return 1; // Return 1-based minimum
        }

        var clampedX = Math.Clamp(x - _thumb.Width / 2, 0, trackWidth);
        var percentage = clampedX / trackWidth;
        var zeroBasedIndex = (int)Math.Round(percentage * (Maximum - 1));

        return zeroBasedIndex + 1;
    }

    /// <summary>
    /// Converts a 1-based index to a pixel position on the track.
    /// </summary>
    private double IndexToPosition(int index)
    {
        var trackWidth = GetTrackWidth();
        if (trackWidth <= 0 || Maximum <= 1)
        {
            return 0;
        }

        // Convert 1-based index to 0-based for calculation
        var zeroBasedIndex = Math.Clamp(index - 1, 0, Maximum - 1);

        // Avoid division by zero if Maximum is 1
        var denominator = (double)Math.Max(1, Maximum - 1);

        return zeroBasedIndex / denominator * trackWidth;
    }

    #endregion
}
