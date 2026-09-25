using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.CustomControls;

/// <summary>
///     A virtualizing wrap panel for image galleries supporting variable item widths with fixed item height,
///     horizontal or vertical orientation, and container recycling.
/// </summary>
public class VirtualizingGallery : VirtualizingPanel
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

    public static readonly StyledProperty<double> WrapHeightOverrideProperty =
        AvaloniaProperty.Register<VirtualizingGallery, double>(nameof(WrapHeightOverride), double.NaN);

    static VirtualizingGallery()
    {
        AffectsMeasure<VirtualizingGallery>(
            OrientationProperty,
            ItemHeightProperty,
            ItemWidthProperty,
            ItemSpacingProperty,
            LineSpacingProperty,
            WrapHeightOverrideProperty);
    }
    
    public readonly record struct RealizedGalleryItem(int Index, Control Element);

    private readonly List<Rect> _itemBounds = [];

    private readonly List<RealizedGalleryItem> _realizedItems = [];
    public IReadOnlyList<RealizedGalleryItem> RealizedItems => _realizedItems;
    private Rect _viewport;
    private NavigateAbleItemsViewer? _viewer;

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
    /// The fixed item height of all items.
    /// <remarks>Must be greater than zero. The widths are calculated dynamically.</remarks>
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
    
    public double WrapHeightOverride
    {
        get => GetValue(WrapHeightOverrideProperty);
        set => SetValue(WrapHeightOverrideProperty, value);
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

        // Only provide snap points when the requested orientation matches our layout's scroll axis
        var isVerticalDocked = !IsExpanded && Orientation == Orientation.Vertical;
        if (isVerticalDocked && orientation is not Orientation.Vertical || !isVerticalDocked && orientation is not Orientation.Horizontal)
        {
            return snapPoints;
        }

        if (!IsExpanded)
        {
            // Docked mode: Snap to individual items
            foreach (var bounds in _itemBounds)
            {
                var point = isVerticalDocked
                    ? snapPointsAlignment switch
                    {
                        SnapPointsAlignment.Near => bounds.Top,
                        SnapPointsAlignment.Center => bounds.Center.Y,
                        SnapPointsAlignment.Far => bounds.Bottom,
                        _ => bounds.Top
                    }
                    : snapPointsAlignment switch
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

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _viewer = this.FindAncestorOfType<NavigateAbleItemsViewer>();
        EffectiveViewportChanged += OnEffectiveViewportChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        EffectiveViewportChanged -= OnEffectiveViewportChanged;
        _viewer = null;
    }

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        var newViewport = e.EffectiveViewport;
        if (_viewport == newViewport)
        {
            // Avoid re-measuring on every viewport notification, it makes scrolling stutter
            return;
        }

        _viewport = newViewport;
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
        var currentColumnStartIndex = 0; // Track where the current column starts

        // Use the override if provided, otherwise use the actual available height
        var availableHeight = double.IsNaN(WrapHeightOverride) || WrapHeightOverride <= 0
            ? availableSize.Height
            : WrapHeightOverride;

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
                // Vertical Wrapping: Use the locked availableHeight
                if (currentY + ItemHeight > availableHeight && currentY > 0)
                {
                    for (var j = currentColumnStartIndex; j < _itemBounds.Count; j++)
                    {
                        var rect = _itemBounds[j];
                        _itemBounds[j] = new Rect(rect.X, rect.Y, currentColumnMaxWidth, rect.Height);
                    }

                    currentY = 0;
                    currentX += currentColumnMaxWidth + LineSpacing;
                    currentColumnMaxWidth = 0; // Reset for the new column
                    currentColumnStartIndex = _itemBounds.Count;
                }

                _itemBounds.Add(new Rect(currentX, currentY, itemWidth, ItemHeight));
                currentY += ItemHeight + ItemSpacing;
                currentColumnMaxWidth = Math.Max(currentColumnMaxWidth, itemWidth);
                maxExtentY = Math.Max(maxExtentY, currentY - ItemSpacing);
            }
            else if (Orientation == Orientation.Vertical)
            {
                // Vertical Docked Mode (Single Column) - items fill available width
                _itemBounds.Add(new Rect(currentX, currentY, availableSize.Width, ItemHeight));
                currentY += ItemHeight;
            }
            else
            {
                // Horizontal Docked Mode (Single Row) - NO SPACING
                _itemBounds.Add(new Rect(currentX, currentY, itemWidth, ItemHeight));
                currentX += itemWidth;
            }
        }

        if (IsExpanded && currentColumnStartIndex < _itemBounds.Count)
        {
            // Apply max width to the very last column
            for (var j = currentColumnStartIndex; j < _itemBounds.Count; j++)
            {
                var rect = _itemBounds[j];
                _itemBounds[j] = new Rect(rect.X, rect.Y, currentColumnMaxWidth, rect.Height);
            }
        }

        if (!IsExpanded)
        {
            // Return exact extent without deducting spacing
            return Orientation == Orientation.Vertical
                ? new Size(availableSize.Width, currentY)
                : new Size(currentX, ItemHeight);
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

        _viewer ??= this.FindAncestorOfType<NavigateAbleItemsViewer>();
        
        if (_viewer is { CurrentItemIndex: >= 0 } && _viewer.CurrentItemIndex < _itemBounds.Count &&
            (_viewport == new Rect() || _viewer.PendingScrollToCurrentItem))
        {
            var isVerticalDocked = !IsExpanded && Orientation == Orientation.Vertical;
            var targetBounds = _itemBounds[_viewer.CurrentItemIndex];

            var viewportWidth = _viewport.Width > 0
                ? _viewport.Width
                : double.IsInfinity(availableSize.Width)
                    ? _viewer.Bounds.Width > 0 ? _viewer.Bounds.Width : TopLevel.GetTopLevel(this)?.Bounds.Width ?? 0
                    : availableSize.Width;

            var viewportHeight = _viewport.Height > 0
                ? _viewport.Height
                : double.IsInfinity(availableSize.Height)
                    ? _viewer.Bounds.Height > 0 ? _viewer.Bounds.Height : TopLevel.GetTopLevel(this)?.Bounds.Height ?? 0
                    : availableSize.Height;

            double targetX = 0;
            double targetY = 0;

            if (isVerticalDocked)
            {
                if (extentSize.Height > viewportHeight && viewportHeight > 0)
                {
                    var maxScrollY = extentSize.Height - viewportHeight;
                    targetY = Math.Clamp(targetBounds.Center.Y - viewportHeight / 2, 0, maxScrollY);
                }
            }
            else
            {
                if (extentSize.Width > viewportWidth && viewportWidth > 0)
                {
                    var maxScrollX = extentSize.Width - viewportWidth;
                    targetX = Math.Clamp(targetBounds.Center.X - viewportWidth / 2, 0, maxScrollX);
                }
            }

            _viewport = new Rect(targetX, targetY, viewportWidth, viewportHeight);
            _viewer.ScrollViewer.Offset = new Vector(targetX, targetY);
            _viewer.PendingScrollToCurrentItem = false;
        }
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
            else if (startIndex is not -1)
            {
                // Early exit: We've completely passed the visible viewport
                var pastViewport = Orientation == Orientation.Vertical && !IsExpanded
                    ? _itemBounds[i].Y > visibleRect.Bottom
                    : _itemBounds[i].X > visibleRect.Right;
                if (pastViewport)
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
                var item = itemsList[i];
                if (ItemContainerGenerator.NeedsContainer(item, i, out var recycleKey))
                {
                    container = ItemContainerGenerator.CreateContainer(item, i, recycleKey);
                    ItemContainerGenerator.PrepareItemContainer(container, item, i);
                    AddInternalChild(container);
                    ItemContainerGenerator.ItemContainerPrepared(container, item, i);

                    // Keep the realized items list sorted by index
                    var insertIndex = _realizedItems.FindIndex(r => r.Index > i);
                    if (insertIndex == -1)
                    {
                        _realizedItems.Add(new RealizedGalleryItem(i, container));
                    }
                    else
                    {
                        _realizedItems.Insert(insertIndex, new RealizedGalleryItem(i, container));
                    }
                }
            }

            // Measure the container using the exact bounds we already calculated
            container.Measure(_itemBounds[i].Size);
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

    protected override IEnumerable<Control> GetRealizedContainers() =>
        _realizedItems.Select(realizedItem => realizedItem.Element);

    protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap) 
        => null; // Let the NavigateAbleItemsViewer handle spatial navigation
}

