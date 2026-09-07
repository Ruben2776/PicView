using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using PicView.Core.Extensions;

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

    // Define the DragSensitivity property
    public static readonly StyledProperty<double> DragSensitivityProperty =
        AvaloniaProperty.Register<DraggableProgressBar, double>(nameof(DragSensitivity), 1.0);
    
    public event EventHandler<int>? ClickedOnTrack;
    public event EventHandler<int>? DraggedOnTrack;

    private int _dragStartIndex;
    private Point _dragStartPoint;

    private Ellipse? _thumb;
    private Border? _track;

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
        UpdateThumbPosition();
        PointerReleased += HandlePointerReleased;
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
    
    private void UpdateToolTip(Point position)
    {
        if (GetThumbBounds().Contains(position))
        {
            ToolTip.SetIsOpen(this, false);
            return;
        }
        
        var pointerOverIndex = PositionToIndex(position.X);
        var progress = StringExtensions.CombineProgress(pointerOverIndex, Maximum);
        ToolTip.SetTip(this, progress);
        ToolTip.SetIsOpen(this, true);
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

            // Fire the event reporting the new index
            ClickedOnTrack?.Invoke(this, CurrentIndex);
            
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
            UpdateToolTip(e.GetPosition(_track));
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
        
        // Fire the event reporting the new index
        DraggedOnTrack?.Invoke(this, CurrentIndex);

        // Manually update the thumb's visual position for smooth dragging.
        // OnPropertyChanged is skipped because IsDragging is true.
        UpdateThumbPosition();
    }

    private void HandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        // Check if we were the one who captured the pointer
        if (!ReferenceEquals(e.Pointer.Captured, _thumb) && !IsDragging)
        {
            return; // Not dragging, or capture was lost/handled elsewhere
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

        Loaded -= OnLoaded;
        LostFocus -= OnLostFocus;
        PointerReleased -= HandlePointerReleased;
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
