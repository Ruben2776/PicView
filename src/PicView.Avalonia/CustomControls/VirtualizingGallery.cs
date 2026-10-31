using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using PicView.Core.Gallery;

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

    private readonly GalleryLayout _layout = new();

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
        if (index >= 0 && index < _layout.Bounds.Count)
        {
            return _layout.Bounds[index];
        }
        return null;
    }

    public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment)
    {
        if (_layout.Bounds.Count is 0)
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
            foreach (var bounds in _layout.Bounds)
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
            foreach (var bounds in _layout.Bounds.Where(bounds => Math.Abs(bounds.X - lastX) > 1.0))
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OrientationProperty)
        {
            // The old viewport belongs to the other scroll axis, wait for a fresh one
            _viewport = default;
            RecycleAllItems();
        }
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
        RecycleAllItems();
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

    private void RecycleAllItems()
    {
        for (var i = _realizedItems.Count - 1; i >= 0; i--)
        {
            var realized = _realizedItems[i];
            ItemContainerGenerator?.ClearItemContainer(realized.Element);
            RemoveInternalChild(realized.Element);
        }
        _realizedItems.Clear();
        _layout.Clear();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Items is null || Items.Count is 0)
        {
            RecycleAllItems();
            return new Size();
        }

        // 1. Calculate all layout boundaries instantly in memory
        _layout.Calculate(Items,
            new GalleryLayoutOptions(Orientation, IsExpanded, ItemHeight, ItemWidth, ItemSpacing, LineSpacing,
                WrapHeightOverride),
            availableSize);

        _viewer ??= this.FindAncestorOfType<NavigateAbleItemsViewer>();
        
        // 2. Determine what is visible (inflate by 2x ItemHeight to buffer scrolling)
        var visibleRect = _viewport;
        if (visibleRect.Width <= 0 || visibleRect.Height <= 0)
        {
            // No usable viewport (not laid out yet, or collapsed to zero size).
            // Never fall back to an unbounded rect, inside a ScrollViewer that would realize every item.
            visibleRect = new Rect(0, 0,
                double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
                double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);
        }
        visibleRect = visibleRect.Inflate(new Thickness(ItemHeight * 2));

        // 3. Find which indices fall inside the visible rect
        var (startIndex, endIndex) = _layout.IndicesIn(visibleRect);

        if (startIndex is -1)
        {
            return _layout.Extent;
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
            var item = itemsList?[i];
            var container = ContainerFromIndex(i);

            // If the realized container's DataContext does not match the current item, recycle it
            if (container is not null && !ReferenceEquals(container.DataContext, item))
            {
                ItemContainerGenerator?.ClearItemContainer(container);
                RemoveInternalChild(container);
                _realizedItems.RemoveAll(r => r.Element == container);
                container = null;
            }

            if (container is null)
            {
                // Generate and add container using Avalonia's generator
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
            container?.Measure(_layout.Bounds[i].Size);
        }

        return _layout.Extent;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Items is null)
        {
            return new Size();
        }

        foreach (var realized in _realizedItems.Where(realized => realized.Index >= 0 && realized.Index < _layout.Bounds.Count))
        {
            realized.Element.Arrange(_layout.Bounds[realized.Index]);
        }

        return finalSize;
    }
    
    protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(items, e);

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Reset:
                RecycleAllItems();
                break;

            case NotifyCollectionChangedAction.Add:
                if (e.NewStartingIndex >= 0)
                {
                    var count = e.NewItems?.Count ?? 1;
                    for (var i = 0; i < _realizedItems.Count; i++)
                    {
                        var realized = _realizedItems[i];
                        if (realized.Index >= e.NewStartingIndex)
                        {
                            _realizedItems[i] = realized with { Index = realized.Index + count };
                        }
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldStartingIndex >= 0)
                {
                    var count = e.OldItems?.Count ?? 1;
                    var oldEndIndex = e.OldStartingIndex + count;
                    for (var i = _realizedItems.Count - 1; i >= 0; i--)
                    {
                        var realized = _realizedItems[i];
                        if (realized.Index >= e.OldStartingIndex && realized.Index < oldEndIndex)
                        {
                            ItemContainerGenerator?.ClearItemContainer(realized.Element);
                            RemoveInternalChild(realized.Element);
                            _realizedItems.RemoveAt(i);
                        }
                        else if (realized.Index >= oldEndIndex)
                        {
                            _realizedItems[i] = realized with { Index = realized.Index - count };
                        }
                    }
                }
                else
                {
                    RecycleAllItems();
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.NewStartingIndex >= 0)
                {
                    var count = e.NewItems?.Count ?? 1;
                    var replaceEndIndex = e.NewStartingIndex + count;
                    for (var i = _realizedItems.Count - 1; i >= 0; i--)
                    {
                        var realized = _realizedItems[i];
                        if (realized.Index < e.NewStartingIndex || realized.Index >= replaceEndIndex)
                        {
                            continue;
                        }

                        ItemContainerGenerator?.ClearItemContainer(realized.Element);
                        RemoveInternalChild(realized.Element);
                        _realizedItems.RemoveAt(i);
                    }
                }
                else
                {
                    RecycleAllItems();
                }
                break;

            case NotifyCollectionChangedAction.Move:
                RecycleAllItems();
                break;
        }

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
