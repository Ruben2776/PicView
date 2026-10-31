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
///     <para>
///         The panel follows the current item, centering it in every layout, until someone else scrolls the view, as
///         the user does. Then it holds the item in the middle of the view where it is while sizes change around it,
///         until a center request.
///     </para>
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

    /// <summary>
    ///     How far apart two positions along the scroll axis can be and still count as the same. Layout rounding moves
    ///     an offset by a fraction of a pixel.
    /// </summary>
    private const double SamePositionTolerance = 1;

    private readonly GalleryLayout _layout = new();

    private readonly List<RealizedGalleryItem> _realizedItems = [];
    public IReadOnlyList<RealizedGalleryItem> RealizedItems => _realizedItems;
    private Rect _viewport;
    private NavigateAbleItemsViewer? _viewer;
    private ScrollViewer? _scrollViewer;

    /// <summary>
    ///     True while every layout centers the current item. False while every layout holds <see cref="_heldItem" />
    ///     instead, because someone else scrolled the view.
    /// </summary>
    private bool _isFollowing = true;

    /// <summary>
    ///     The item nearest the middle of the view, by reference, since edits shift indices.
    /// </summary>
    private object? _heldItem;

    /// <summary>
    ///     How far the middle of <see cref="_heldItem" /> is past the middle of the view along the scroll axis.
    ///     Negative when before it.
    /// </summary>
    private double _heldDistance;

    /// <summary>
    ///     The offset along the scroll axis that the last layout chose, for the scroll viewer to take once the layout
    ///     pass is done. Null when the view is already there.
    /// </summary>
    private double? _pendingOffset;

    /// <summary>
    ///     The offset along the scroll axis that the scroll viewer took, after coercion, when the panel last set it.
    ///     NaN when there is none, or once the view arrived there.
    /// </summary>
    private double _offsetSetByPanel = double.NaN;

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
            if (field == value)
            {
                return;
            }

            field = value;
            // A held distance belongs to the other layout, so center the current item in the new one
            _isFollowing = true;
            InvalidateMeasure();
        }
    }

    /// <summary>
    ///     Whether the panel scrolls along Y rather than X. It reads the panel's own properties, so it is right between
    ///     a change to them and the next measure, when the layout's axis is stale or cleared.
    /// </summary>
    private bool ScrollsVertically => GalleryLayout.ScrollsVerticallyFor(Orientation, IsExpanded);

    /// <summary>
    ///     A center request: from the next layout on, every layout centers the current item again, until someone else
    ///     scrolls the view.
    /// </summary>
    public void CenterCurrentItem()
    {
        _isFollowing = true;
        InvalidateMeasure();
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
        var isVerticalDocked = ScrollsVertically;
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
            // The old viewport belongs to the other scroll axis, wait for a fresh one. A held distance belongs to that
            // axis too, so center the current item when the fresh one comes
            _viewport = default;
            _isFollowing = true;
            RecycleAllItems();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _viewer = this.FindAncestorOfType<NavigateAbleItemsViewer>();
        _scrollViewer = this.FindAncestorOfType<ScrollViewer>();
        // Shown again, as after a tab switch: center the current item rather than hold a view from before
        _isFollowing = true;
        EffectiveViewportChanged += OnEffectiveViewportChanged;
        LayoutUpdated += OnLayoutUpdated;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        EffectiveViewportChanged -= OnEffectiveViewportChanged;
        LayoutUpdated -= OnLayoutUpdated;
        RecycleAllItems();
        // The viewport that comes after attaching again arrives for the first time, so it isn't a scroll
        _viewport = default;
        _viewer = null;
        _scrollViewer = null;
    }

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        var newViewport = e.EffectiveViewport;
        if (_viewport == newViewport)
        {
            // Avoid re-measuring on every viewport notification, it makes scrolling stutter
            return;
        }

        // A viewport arriving for the first time, as when the panel is shown, isn't a scroll
        if (HasSize(_viewport))
        {
            HoldIfScrolled(newViewport);
        }

        _viewport = newViewport;
        InvalidateMeasure();
    }

    /// <summary>
    ///     Stops following when someone other than the panel scrolled the view, as the user does with the wheel, the
    ///     scroll bar, middle-click auto-scroll or keyboard selection, and holds the item in the middle of the new view.
    /// </summary>
    private void HoldIfScrolled(Rect newViewport)
    {
        if (_scrollViewer is null)
        {
            return;
        }

        var scrollsVertically = ScrollsVertically;
        var oldPosition = scrollsVertically ? _viewport.Y : _viewport.X;
        var newPosition = scrollsVertically ? newViewport.Y : newViewport.X;
        if (Math.Abs(newPosition - oldPosition) <= SamePositionTolerance)
        {
            // Not moved along the scroll axis, as when the view is only resized
            return;
        }

        // Compare what the scroll viewer took, not the viewport: the panel's own coordinates start after its margin,
        // which the Expanded Gallery has
        var offset = scrollsVertically ? _scrollViewer.Offset.Y : _scrollViewer.Offset.X;
        if (Math.Abs(offset - _offsetSetByPanel) <= SamePositionTolerance)
        {
            // The view arrived where the panel put it
            _offsetSetByPanel = double.NaN;
            return;
        }

        var extent = scrollsVertically ? _scrollViewer.Extent.Height : _scrollViewer.Extent.Width;
        var viewport = scrollsVertically ? _scrollViewer.Viewport.Height : _scrollViewer.Viewport.Width;
        if (newPosition < oldPosition && offset >= Math.Max(0, extent - viewport) - SamePositionTolerance)
        {
            // The strip got shorter than the view reached, as when its items are cleared, so the scroll viewer pulled
            // the view back to its new end. No one scrolled
            return;
        }

        // Hold the item now in the middle of the view, as the last layout placed it, which is what the user sees. Sizes
        // read since that layout then move the items around it, not it
        _isFollowing = false;
        HoldItemNearest(newPosition + (scrollsVertically ? newViewport.Height : newViewport.Width) / 2);
    }

    /// <summary>
    ///     Gives the scroll viewer the offset the last layout chose, now that the layout pass is done and the scroll
    ///     viewer's extent has caught up with it. Avalonia lays the new offset out before any other queued work runs.
    /// </summary>
    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_pendingOffset is not { } pendingOffset)
        {
            return;
        }

        _pendingOffset = null;
        if (_scrollViewer is null)
        {
            return;
        }

        var scrollsVertically = ScrollsVertically;
        var offset = _scrollViewer.Offset;
        _scrollViewer.SetCurrentValue(ScrollViewer.OffsetProperty,
            scrollsVertically ? offset.WithY(pendingOffset) : offset.WithX(pendingOffset));

        // Only a view that moves brings a viewport to tell apart. One the scroll viewer kept in place, as it does when
        // the Expanded Gallery's margin keeps the target a little off the viewport, would otherwise stay remembered
        // and pass a later scroll that lands there for the panel's own
        var before = scrollsVertically ? offset.Y : offset.X;
        var taken = scrollsVertically ? _scrollViewer.Offset.Y : _scrollViewer.Offset.X;
        _offsetSetByPanel = Math.Abs(taken - before) > SamePositionTolerance ? taken : double.NaN;
    }

    private static bool HasSize(Rect viewport) => viewport.Width > 0 && viewport.Height > 0;

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
        // Each layout decides afresh whether the view moves
        _pendingOffset = null;

        var items = Items;
        if (items is null || items.Count is 0)
        {
            RecycleAllItems();
            return new Size();
        }

        // 1. Calculate all layout boundaries instantly in memory
        _layout.Calculate(items,
            new GalleryLayoutOptions(Orientation, IsExpanded, ItemHeight, ItemWidth, ItemSpacing, LineSpacing,
                WrapHeightOverride),
            availableSize);

        _viewer ??= this.FindAncestorOfType<NavigateAbleItemsViewer>();
        
        // 2. Without a usable viewport (not laid out yet, or collapsed to zero size) there is no view to fill. Wait for
        // one, rather than fill the start of the strip, which would load the thumbnails there first
        if (!HasSize(_viewport))
        {
            return _layout.Extent;
        }

        // 3. Decide where the view goes, and which item it is about
        var scrollsVertically = ScrollsVertically;
        var position = scrollsVertically ? _viewport.Y : _viewport.X;
        var length = scrollsVertically ? _viewport.Height : _viewport.Width;
        var (target, anchor) = ChooseView(position, length);

        // The scroll viewer takes the new offset once the layout pass is done, when its extent has caught up
        if (Math.Abs(target - position) > SamePositionTolerance)
        {
            _pendingOffset = target;
        }

        // 4. Find which indices fall inside that view (inflate by 2x ItemHeight to buffer scrolling)
        var view = scrollsVertically ? _viewport.WithY(target) : _viewport.WithX(target);
        var (startIndex, endIndex) = _layout.IndicesIn(view.Inflate(new Thickness(ItemHeight * 2)));

        if (startIndex is -1)
        {
            return _layout.Extent;
        }

        // 5. Recycle items that are no longer visible
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

        // 6. Realize and measure the visible items outward from the anchor: anchor, +1, -1, +2, -2 and so on. A
        // container requests its thumbnail when it is created, so the item the view is about is decoded first
        var first = Math.Clamp(anchor, startIndex, endIndex);
        RealizeAndMeasure(items, first);
        for (var step = 1; first + step <= endIndex || first - step >= startIndex; step++)
        {
            if (first + step <= endIndex)
            {
                RealizeAndMeasure(items, first + step);
            }

            if (first - step >= startIndex)
            {
                RealizeAndMeasure(items, first - step);
            }
        }

        // 7. Hold the item nearest the middle of that view, for when someone else scrolls it
        HoldItemNearest(target + length / 2);

        return _layout.Extent;
    }

    /// <summary>
    ///     Where the view goes along the scroll axis, and the item it is about. While following, it centers the
    ///     current item. While holding, it puts the held item back at its distance from the middle. Otherwise, as when
    ///     the current index is out of range or the held item was removed, it stays where it is.
    /// </summary>
    private (double Target, int Anchor) ChooseView(double position, double length)
    {
        var current = _viewer?.CurrentItemIndex ?? -1;
        if (_isFollowing && current >= 0 && current < _layout.Bounds.Count)
        {
            return (_layout.CenterOffset(current, length), current);
        }

        var held = !_isFollowing && _heldItem is not null ? IndexOf(_heldItem) : -1;
        if (held is not -1)
        {
            return (_layout.HoldOffset(held, _heldDistance, length), held);
        }

        return (position, _layout.IndexNearest(position + length / 2));
    }

    /// <summary>
    ///     Holds the item whose middle is nearest the middle of a view, at its distance from it.
    /// </summary>
    /// <param name="viewMiddle">The middle of the view along the scroll axis, in the panel's coordinates.</param>
    private void HoldItemNearest(double viewMiddle)
    {
        var items = Items;
        var index = _layout.IndexNearest(viewMiddle);
        if (items is null || index < 0 || index >= items.Count)
        {
            _heldItem = null;
            return;
        }

        _heldItem = items[index];
        _heldDistance = _layout.Middle(index) - viewMiddle;
    }

    /// <summary>
    ///     The index of the item, compared by reference, or -1 when it is no longer in the items.
    /// </summary>
    private int IndexOf(object item)
    {
        var items = Items;
        if (items is null)
        {
            return -1;
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (ReferenceEquals(items[i], item))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    ///     Gives the item at the index a container, unless its container is realized already, and measures the
    ///     container at the item's bounds.
    /// </summary>
    private void RealizeAndMeasure(IReadOnlyList<object?> items, int index)
    {
        var item = items[index];
        var container = ContainerFromIndex(index);

        // If the realized container's DataContext does not match the current item, recycle it
        if (container is not null && !ReferenceEquals(container.DataContext, item))
        {
            ItemContainerGenerator?.ClearItemContainer(container);
            RemoveInternalChild(container);
            for (var i = _realizedItems.Count - 1; i >= 0; i--)
            {
                if (_realizedItems[i].Element == container)
                {
                    _realizedItems.RemoveAt(i);
                }
            }

            container = null;
        }

        // Generate and add container using Avalonia's generator
        if (container is null && ItemContainerGenerator.NeedsContainer(item, index, out var recycleKey))
        {
            container = ItemContainerGenerator.CreateContainer(item, index, recycleKey);
            ItemContainerGenerator.PrepareItemContainer(container, item, index);
            AddInternalChild(container);
            ItemContainerGenerator.ItemContainerPrepared(container, item, index);

            // Keep the realized items list sorted by index
            var insertIndex = _realizedItems.Count;
            while (insertIndex > 0 && _realizedItems[insertIndex - 1].Index > index)
            {
                insertIndex--;
            }

            _realizedItems.Insert(insertIndex, new RealizedGalleryItem(index, container));
        }

        // Measure the container using the exact bounds we already calculated
        container?.Measure(_layout.Bounds[index].Size);
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
                // Cleared, as for another folder: center the current item of what comes next
                RecycleAllItems();
                _isFollowing = true;
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
