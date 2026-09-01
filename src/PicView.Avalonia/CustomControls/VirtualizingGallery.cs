using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.CustomControls;

/// <summary>
///     A virtualizing wrap panel for image galleries supporting variable item widths with fixed item height,
///     horizontal or vertical orientation, and container recycling.
/// </summary>
public class VirtualizingGallery : VirtualizingPanel, IScrollSnapPointsInfo
{
    /// <inheritdoc cref="WrapPanel" />
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<WrapPanel, Orientation>(nameof(Orientation));

    /// <inheritdoc cref="WrapPanel" />
    public static readonly StyledProperty<double> ItemHeightProperty =
        AvaloniaProperty.Register<WrapPanel, double>(nameof(ItemHeight),
            GalleryDefaults.DefaultDockedGalleryHeight);
    
    public static readonly StyledProperty<double> ItemWidthProperty =
        AvaloniaProperty.Register<VirtualizingGallery, double>(nameof(ItemWidth), double.NaN);

    /// <inheritdoc cref="WrapPanel" />
    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<WrapPanel, double>(nameof(ItemSpacing));

    /// <inheritdoc cref="WrapPanel" />
    public static readonly StyledProperty<double> LineSpacingProperty =
        AvaloniaProperty.Register<WrapPanel, double>(nameof(LineSpacing));

    private readonly List<Rect> _itemBounds = [];

    private readonly List<RealizedItem> _realizedItems = [];
    private Rect _viewport;

    /// <inheritdoc cref="WrapPanel" />
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }
    
    /// <summary>
    /// The width is either double.NaN or the same as ItemWidth when in a square ratio.
    /// </summary>
    public double ItemWidth
    {
        get => GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    /// <summary>
    ///     The fixed item height of all items.
    ///     <remarks>Must be greater than zero. The widths are calculated dynamically.</remarks>
    /// </summary>
    public double ItemHeight
    {
        get => GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <inheritdoc cref="WrapPanel" />
    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    /// <inheritdoc cref="WrapPanel" />
    public double LineSpacing
    {
        get => GetValue(LineSpacingProperty);
        set => SetValue(LineSpacingProperty, value);
    }

    public bool IsExpanded
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            InvalidateMeasure();
        }
    }

    public Rect? GetItemBounds(int index)
    {
        if (index >= 0 && index < _itemBounds.Count)
        {
            return _itemBounds[index];
        }
        return null;
    }

    public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment)
    {
        if (_itemBounds.Count is 0)
        {
            return [];
        }

        var snapPoints = new List<double>();

        if (orientation is not Orientation.Horizontal)
        {
            return snapPoints;
        }

        if (!IsExpanded)
        {
            // Docked mode: Snap to individual items
            foreach (var bounds in _itemBounds)
            {
                var point = snapPointsAlignment switch
                {
                    SnapPointsAlignment.Near => bounds.Left,
                    SnapPointsAlignment.Center => bounds.Center.X,
                    SnapPointsAlignment.Far => bounds.Right,
                    _ => bounds.Left
                };
                snapPoints.Add(point);
            }
        }
        else
        {
            // Expanded mode: Snap to the start of each column
            var lastX = -1.0;
            foreach (var bounds in _itemBounds.Where(bounds => Math.Abs(bounds.X - lastX) > 1.0))
            {
                lastX = bounds.X;
                var point = snapPointsAlignment switch
                {
                    SnapPointsAlignment.Near => bounds.Left,
                    SnapPointsAlignment.Center => bounds.Center.X,
                    SnapPointsAlignment.Far => bounds.Right,
                    _ => bounds.Left
                };
                snapPoints.Add(point);
            }
        }

        return snapPoints;
    }

    public double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset)
    {
        offset = 0d;
        return 0d; // We return 0 here because our layout requires irregular snap points
    }

    public bool AreHorizontalSnapPointsRegular { get; set; } = false;
    public bool AreVerticalSnapPointsRegular { get; set; } = false;
    public event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged;
    public event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        EffectiveViewportChanged += OnEffectiveViewportChanged;
    }


    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        EffectiveViewportChanged -= OnEffectiveViewportChanged;
    }

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        _viewport = e.EffectiveViewport;
        InvalidateMeasure();
    }

    private Size CalculateBounds(Size availableSize)
    {
        _itemBounds.Clear();

        if (Items is not IEnumerable<object> items)
        {
            return new Size();
        }

        double currentX = 0;
        double currentY = 0;
        double currentColumnMaxWidth = 0;
        double maxExtentY = 0;

        foreach (var item in items)
        {
            var itemWidth = ItemHeight; // Fallback for square

            // 1. Check if the user forced a specific width (e.g., Square Stretch Mode)
            if (!double.IsNaN(ItemWidth))
            {
                itemWidth = ItemWidth;
            }
            // 2. Otherwise, dynamically calculate based on aspect ratio
            else if (item is GalleryItemViewModel { PixelHeight: > 0 } vm)
            {
                var aspectRatio = (double)vm.PixelWidth / vm.PixelHeight;
                itemWidth = ItemHeight * aspectRatio;
            }

            if (IsExpanded)
            {
                // Vertical Wrapping: Wrap to next column if exceeding available height
                if (currentY + ItemHeight > availableSize.Height && currentY > 0)
                {
                    currentY = 0;
                    currentX += currentColumnMaxWidth + LineSpacing;
                    currentColumnMaxWidth = 0; // Reset for the new column
                }

                _itemBounds.Add(new Rect(currentX, currentY, itemWidth, ItemHeight));

                currentY += ItemHeight + ItemSpacing;
                currentColumnMaxWidth = Math.Max(currentColumnMaxWidth, itemWidth);
                maxExtentY = Math.Max(maxExtentY, currentY - ItemSpacing);
            }
            else
            {
                // Horizontal Docked Mode (Single Row)
                _itemBounds.Add(new Rect(currentX, currentY, itemWidth, ItemHeight));
                currentX += itemWidth + ItemSpacing;
            }
        }

        if (!IsExpanded)
        {
            return new Size(currentX > 0 ? currentX - ItemSpacing : 0, ItemHeight);
        }

        var totalWidth = currentX + currentColumnMaxWidth;
        // Guard against Infinity if height is temporarily unconstrained during layout passes
        var totalHeight = double.IsInfinity(availableSize.Height) ? maxExtentY : availableSize.Height;
        return new Size(totalWidth, totalHeight);

    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Items is null || Items.Count is 0)
        {
            return new Size();
        }

        // 1. Calculate all layout boundaries instantly in memory
        var extentSize = CalculateBounds(availableSize);

        // 2. Determine what is visible (inflate by 2x ItemHeight to buffer scrolling)
        var visibleRect = _viewport == new Rect() ? new Rect(new Point(), availableSize) : _viewport;
        visibleRect = visibleRect.Inflate(new Thickness(ItemHeight * 2));

        // 3. Find which indices fall inside the visible rect
        var startIndex = -1;
        var endIndex = -1;

        for (var i = 0; i < _itemBounds.Count; i++)
        {
            if (visibleRect.Intersects(_itemBounds[i]))
            {
                if (startIndex is -1) startIndex = i;
                {
                    endIndex = i;
                }
            }
            else if (startIndex is not -1 && _itemBounds[i].X > visibleRect.Right)
            {
                // Early exit: We've completely passed the visible horizontal viewport
                break;
            }
        }

        if (startIndex is -1)
        {
            return extentSize;
        }

        // 4. Recycle items that are no longer visible
        for (var i = _realizedItems.Count - 1; i >= 0; i--)
        {
            var realized = _realizedItems[i];
            if (realized.Index >= startIndex && realized.Index <= endIndex)
            {
                continue;
            }

            ItemContainerGenerator?.ClearItemContainer(realized.Element);
            RemoveInternalChild(realized.Element);
            _realizedItems.RemoveAt(i);
        }

        // 5. Realize and measure items that ARE visible
        var itemsList = Items as IList;
        for (var i = startIndex; i <= endIndex; i++)
        {
            var container = ContainerFromIndex(i);
            if (container is null)
            {
                // Generate and add container using Avalonia's generator
                var item = itemsList![i];
                if (ItemContainerGenerator!.NeedsContainer(item, i, out var recycleKey))
                {
                    container = ItemContainerGenerator.CreateContainer(item, i, recycleKey);
                    ItemContainerGenerator.PrepareItemContainer(container, item, i);
                    AddInternalChild(container);
                    ItemContainerGenerator.ItemContainerPrepared(container, item, i);

                    // Keep the realized items list sorted by index
                    var insertIndex = _realizedItems.FindIndex(r => r.Index > i);
                    if (insertIndex == -1)
                    {
                        _realizedItems.Add(new RealizedItem(i, container));
                    }
                    else
                    {
                        _realizedItems.Insert(insertIndex, new RealizedItem(i, container));
                    }
                }
            }

            // Measure the container using the exact bounds we already calculated
            container?.Measure(_itemBounds[i].Size);
        }

        return extentSize;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Items is null)
        {
            return new Size();
        }

        foreach (var realized in _realizedItems.Where(realized => realized.Index >= 0 && realized.Index < _itemBounds.Count))
        {
            realized.Element.Arrange(_itemBounds[realized.Index]);
        }

        return finalSize;
    }
    
    protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(items, e);
        InvalidateMeasure();
    }


    protected override Control? ScrollIntoView(int index)
    {
        // Return the container if it happens to be realized, otherwise null. 
        // We handle the actual scrolling externally now.
        return ContainerFromIndex(index);
    }

    protected override Control? ContainerFromIndex(int index)
    {
        foreach (var realizedItem in _realizedItems)
        {
            if (realizedItem.Index == index)
            {
                return realizedItem.Element;
            }
        }

        return null;
    }

    protected override int IndexFromContainer(Control container)
    {
        foreach (var realizedItem in _realizedItems)
        {
            if (realizedItem.Element == container)
            {
                return realizedItem.Index;
            }
        }

        return -1;
    }

    protected override IEnumerable<Control>? GetRealizedContainers()
    {
        return _realizedItems.Select(realizedItem => realizedItem.Element);
    }

    protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        // Let the NavigateAbleItemsViewer handle spatial navigation
        return null;
    }


    private record struct RealizedItem(int Index, Control Element);
}