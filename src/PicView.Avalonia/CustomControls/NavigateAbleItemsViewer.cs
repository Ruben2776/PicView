using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.Views.Gallery;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.CustomControls;

[StructLayout(LayoutKind.Auto)]
public readonly record struct ItemPosition(int Index, Point Position, Size Size);

[TemplatePart("PART_ScrollViewer", typeof(AutoScrollViewer))]
public class NavigateAbleItemsViewer : ItemsControl
{
    #region Fields and  Avalonia Properties
    
    private const double ScrollLineSize = 50;

    private AutoScrollViewer? _scrollViewer;

    private IDisposable? _viewportSubscription;

    protected override Type StyleKeyOverride => typeof(NavigateAbleItemsViewer);

    public static readonly StyledProperty<int> SelectedItemIndexProperty =
        AvaloniaProperty.Register<NavigateAbleItemsViewer, int>(nameof(SelectedItemIndex), defaultValue: -1);

    public int SelectedItemIndex
    {
        get => GetValue(SelectedItemIndexProperty);
        set => SetValue(SelectedItemIndexProperty, value);
    }

    public static readonly StyledProperty<int> CurrentItemIndexProperty =
        AvaloniaProperty.Register<NavigateAbleItemsViewer, int>(nameof(CurrentItemIndex), defaultValue: -1);

    public int CurrentItemIndex
    {
        get => GetValue(CurrentItemIndexProperty);
        set => SetValue(CurrentItemIndexProperty, value);
    }

    /// <summary>
    /// The index of the item shown beside the current one, or -1 for none. A Side-by-Side Pair marks both items as
    /// current, and they are not always next to each other, as the last image pairs with the first when looping.
    /// </summary>
    public static readonly StyledProperty<int> PartnerItemIndexProperty =
        AvaloniaProperty.Register<NavigateAbleItemsViewer, int>(nameof(PartnerItemIndex), defaultValue: -1);

    public int PartnerItemIndex
    {
        get => GetValue(PartnerItemIndexProperty);
        set => SetValue(PartnerItemIndexProperty, value);
    }

    // Whether the item at the index is shown: the current item, or its partner
    private bool IsCurrentItem(int index) => index >= 0 && (index == CurrentItemIndex || index == PartnerItemIndex);

    #endregion

    #region Constructor and Control Overrides

    public NavigateAbleItemsViewer()
    {
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Direct | RoutingStrategies.Tunnel);
        TileRestTimer = new DispatcherTileRestTimer(OnTileRestElapsed);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _scrollViewer = e.NameScope.Find<AutoScrollViewer>("PART_ScrollViewer");
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _viewportSubscription?.Dispose();
        _viewportSubscription = null;
        TileRestTimer.Stop();
        CancelTileSharpening();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        RestartTileRest();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CurrentItemIndexProperty)
        {
            UpdateCurrentVisualState();
            ScrollToCenterOfCurrentItem();
        }
        else if (change.Property == PartnerItemIndexProperty)
        {
            UpdateCurrentVisualState();
        }
        else if (change.Property == SelectedItemIndexProperty)
        {
            UpdateSelectionVisualState();
        }
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is not ContentPresenter presenter)
        {
            return;
        }

        if (presenter.Child is NavigateAbleItem existingItem)
        {
            InitializeNavItem(existingItem, index);
        }
        else
        {
            EventHandler<AvaloniaPropertyChangedEventArgs>? handler = null;
            handler = (_, e) =>
            {
                if (e.Property != ContentPresenter.ChildProperty || e.NewValue is not NavigateAbleItem newItem)
                {
                    return;
                }

                presenter.PropertyChanged -= handler;
                var currentIndex = IndexFromContainer(presenter);
                InitializeNavItem(newItem, currentIndex >= 0 ? currentIndex : index);
            };
            presenter.PropertyChanged += handler;
        }
    }

    private void InitializeNavItem(NavigateAbleItem navItem, int index)
    {
        navItem.SetCurrent(IsCurrentItem(index));
        navItem.SetSelected(SelectedItemIndex >= 0 && index == SelectedItemIndex);

        if (navItem is not GalleryItem { DataContext: GalleryItemViewModel vm } galleryItem)
        {
            return;
        }

        // Read from the panel: a recycled item is not in it yet, and the tile style may not be applied yet
        var isTile = ItemsPanelRoot is VirtualizingGallery gallery && ShowsTiles(gallery);
        _ = galleryItem.LoadImage(vm, isTile).ConfigureAwait(false);
        if (isTile)
        {
            RequestTileSharpen();
        }
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        // Before the base clears the container, which takes the item's data context, the view model it unloads
        if (container is ContentPresenter { Child: GalleryItem galleryItem })
        {
            galleryItem.UnloadImage();
        }

        base.ClearContainerForItemOverride(container);
        if (container is not ContentPresenter { Child: NavigateAbleItem navItem })
        {
            return;
        }
        navItem.SetCurrent(false);
        navItem.SetSelected(false);
    }
    #endregion

    #region Scrolling & Viewport Logic

    public void SetVerticalScrolling()
    {
        _scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
    }
    
    public void SetHorizontalScrolling()
    {
        _scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
        _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    /// <summary>
    ///     A center request: the panel centers the current item in its next layout, and in every layout after that
    ///     until someone else scrolls it. A request made before the panel exists needs nothing more, since a new panel
    ///     centers the current item.
    /// </summary>
    public void ScrollToCenterOfCurrentItem()
    {
        if (Dispatcher.CheckAccess())
        {
            CenterCurrentItem();
        }
        else
        {
            Dispatcher.UIThread.Post(CenterCurrentItem, DispatcherPriority.Render);
        }
    }

    private void CenterCurrentItem() => (ItemsPanelRoot as VirtualizingGallery)?.CenterCurrentItem();

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        switch (Settings.Gallery.GalleryMouseWheelBehavior)
        {
            case GalleryMouseWheel.Navigate:
                 NavigateTheControl(e);
                break;
            case GalleryMouseWheel.Scroll:
                ScrollTheControl(e);
                break;
        }
    }

    private void NavigateTheControl(PointerWheelEventArgs e)
    {
        if (DataContext is not TabViewModel tab)
        {
            return;
        }

        if (tab.Gallery.IsGalleryExpanded.CurrentValue)
        {
            ScrollTheControl(e);
            return;
        }

        // Mark as handled, so the inner ScrollViewer doesn't scroll on its own
        // and fight the centering of the current item
        e.Handled = true;

        if (GetScrollDelta(e) < 0)
        {
            _ = tab.Next();
        }
        else
        {
            _ = tab.Prev();
        }
    }

    private void ScrollTheControl(PointerWheelEventArgs e)
    {
        // Mark as handled, so the inner ScrollViewer doesn't apply its own scrolling on top of ours.
        // Otherwise, a small delta on the opposite axis (common on trackpads) makes it scroll backwards
        e.Handled = true;

        var delta = GetScrollDelta(e);
        if (delta is 0)
        {
            return;
        }

        var offset = _scrollViewer.Offset;
        var extent = _scrollViewer.Extent;
        var viewport = _scrollViewer.Viewport;

        if (_scrollViewer.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled &&
            extent.Width > viewport.Width)
        {
            var maxX = extent.Width - viewport.Width;
            offset = offset.WithX(Math.Clamp(offset.X - delta * ScrollLineSize, 0, maxX));
        }
        else if (_scrollViewer.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled &&
                 extent.Height > viewport.Height)
        {
            var maxY = extent.Height - viewport.Height;
            offset = offset.WithY(Math.Clamp(offset.Y - delta * ScrollLineSize, 0, maxY));
        }
        else
        {
            return;
        }

        _scrollViewer.SetCurrentValue(ScrollViewer.OffsetProperty, offset);
    }

    /// <summary>
    /// Reduces the wheel delta to a single value along the dominant axis,
    /// so noise on the opposite axis can't reverse the scroll direction.
    /// </summary>
    private static double GetScrollDelta(PointerWheelEventArgs e)
        => Math.Abs(e.Delta.X) > Math.Abs(e.Delta.Y) ? e.Delta.X : e.Delta.Y;

    #endregion

    #region Selection & Visual State Management

    public void UpdateCurrentVisualState()
    {
        if (ItemsPanelRoot is VirtualizingGallery gallery)
        {
            foreach (var realized in gallery.RealizedItems)
            {
                var navItem = GetNavigateAbleItem(realized.Element);
                navItem?.SetCurrent(IsCurrentItem(realized.Index));
            }
        }
        else
        {
            for (var i = 0; i < ItemCount; i++)
            {
                var navItem = GetNavigateAbleItem(ContainerFromIndex(i));
                navItem?.SetCurrent(IsCurrentItem(i));
            }
        }
    }

    public void UpdateSelectionVisualState()
    {
        var selectedIndex = SelectedItemIndex;
        if (ItemsPanelRoot is VirtualizingGallery gallery)
        {
            foreach (var realized in gallery.RealizedItems)
            {
                var navItem = GetNavigateAbleItem(realized.Element);
                navItem?.SetSelected(selectedIndex >= 0 && realized.Index == selectedIndex);
            }
        }
        else
        {
            for (var i = 0; i < ItemCount; i++)
            {
                var navItem = GetNavigateAbleItem(ContainerFromIndex(i));
                navItem?.SetSelected(selectedIndex >= 0 && i == selectedIndex);
            }
        }
    }

    private static NavigateAbleItem? GetNavigateAbleItem(Control container)
    {
        return container switch
        {
            NavigateAbleItem navItem => navItem,
            ContentPresenter presenter => presenter.Child as NavigateAbleItem,
            _ => null
        };
    }

    public void ScrollItemIntoView(int index)
    {
        if (index < 0 || index >= ItemCount)
        {
            return;
        }

        if (ContainerFromIndex(index) is { } container)
        {
            container.BringIntoView();
            return;
        }

        if (ItemsPanelRoot is not VirtualizingGallery gallery)
        {
            return;
        }

        var bounds = gallery.GetItemBounds(index);
        if (!bounds.HasValue)
        {
            return;
        }

        var rect = bounds.Value;
        if (IsInVerticalDisplayMode(gallery))
        {
            // The scroll viewer scrolls over the panel's margin too
            var top = rect.Top + gallery.Margin.Top;
            var bottom = rect.Bottom + gallery.Margin.Top;
            var current = _scrollViewer.Offset;
            var height = _scrollViewer.Viewport.Height;
            if (top < current.Y)
            {
                _scrollViewer.SetCurrentValue(ScrollViewer.OffsetProperty, current.WithY(top));
            }
            else if (bottom > current.Y + height)
            {
                _scrollViewer.SetCurrentValue(ScrollViewer.OffsetProperty, current.WithY(bottom - height));
            }

            return;
        }

        var offset = _scrollViewer.Offset;
        var viewport = _scrollViewer.Viewport;
        var newX = offset.X;
        var newY = offset.Y;

        if (_scrollViewer.Extent.Width > viewport.Width)
        {
            if (rect.Left < offset.X)
            {
                newX = rect.Left;
            }
            else if (rect.Right > offset.X + viewport.Width)
            {
                newX = rect.Right - viewport.Width;
            }
        }

        if (_scrollViewer.Extent.Height > viewport.Height)
        {
            if (rect.Top < offset.Y)
            {
                newY = rect.Top;
            }
            else if (rect.Bottom > offset.Y + viewport.Height)
            {
                newY = rect.Bottom - viewport.Height;
            }
        }

        _scrollViewer.Offset = new Vector(newX, newY);
    }

    private void UpdateSelectionIndex(int index)
    {
        SelectedItemIndex = index;
    }

    // The Expanded Gallery's Masonry and Rows; the side-docked strip also scrolls vertically, but keeps the column rules
    private static bool IsInVerticalDisplayMode(VirtualizingGallery gallery) => ShowsTiles(gallery);

    /// <summary>Whether the panel shows tiles: it is expanded, in Masonry or Rows.</summary>
    internal static bool ShowsTiles(VirtualizingGallery gallery) =>
        gallery.IsExpanded && GalleryDisplayModes.ShowsTiles(gallery.DisplayMode);

    #endregion

    #region Tile Sharpening

    /// <summary>
    /// Starts the rest period again, as when the user scrolled or resized the view, or changed the item size or the
    /// display mode. Sharpening requests still waiting for their turn are cancelled, so only what is on screen once the
    /// view rests is decoded.
    /// </summary>
    public void RestartTileRest()
    {
        CancelTileSharpening();
        TileRestTimer.Stop();
        if (ItemsPanelRoot is VirtualizingGallery gallery && ShowsTiles(gallery))
        {
            TileRestTimer.Start();
        }
    }

    /// <summary>
    /// Starts the rest period unless it runs, as when a tile was loaded or the panel laid out again: neither is the
    /// user moving the view, so neither postpones sharpening.
    /// </summary>
    public void RequestTileSharpen()
    {
        if (!TileRestTimer.IsEnabled && ItemsPanelRoot is VirtualizingGallery gallery && ShowsTiles(gallery))
        {
            TileRestTimer.Start();
        }
    }

    /// <summary>
    /// The view rested: every realized tile that shows something shorter than it needs, and was not asked for that
    /// height already, asks for its Tile Thumbnail, nearest the middle of the view first.
    /// </summary>
    internal void OnTileRestElapsed()
    {
        TileRestTimer.Stop();
        if (ItemsPanelRoot is not VirtualizingGallery gallery || !ShowsTiles(gallery))
        {
            return;
        }

        var viewMiddle = ViewMiddleIn(gallery);
        var realizedItems = gallery.RealizedItems;
        for (var i = 0; i < realizedItems.Count; i++)
        {
            var realized = realizedItems[i];
            if (GetNavigateAbleItem(realized.Element) is not GalleryItem { DataContext: GalleryItemViewModel vm } item)
            {
                continue;
            }

            var needed = gallery.TileThumbnailHeightFor(realized.Index);
            if (needed is 0 || needed <= vm.TileHeightShown || needed <= item.TileHeightAsked ||
                gallery.GetItemBounds(realized.Index) is not { } bounds)
            {
                continue;
            }

            var distance = Point.Distance(bounds.Center, viewMiddle);
            _tilesToSharpen.Add(new TileToSharpen(distance, item, vm, needed));
        }

        if (_tilesToSharpen.Count is 0)
        {
            return;
        }

        _tilesToSharpen.Sort(TileToSharpen.NearestFirst);
        _tileSharpenCts ??= new CancellationTokenSource();
        var token = _tileSharpenCts.Token;
        foreach (var tile in _tilesToSharpen)
        {
            _ = tile.Item.SharpenAsync(tile.Vm, tile.Height, token);
        }

        _tilesToSharpen.Clear();
    }

    /// <summary>The rest timer. A test replaces it with one on its own clock.</summary>
    internal ITileRestTimer TileRestTimer { get; set; }

    private readonly List<TileToSharpen> _tilesToSharpen = [];

    /// <summary>Cancels the sharpening requests of the rest period; null until the view first rests.</summary>
    private CancellationTokenSource? _tileSharpenCts;

    private void CancelTileSharpening()
    {
        if (_tileSharpenCts is null)
        {
            return;
        }

        _tileSharpenCts.Cancel();
        _tileSharpenCts.Dispose();
        _tileSharpenCts = null;
    }

    // The middle of the view in the panel's coordinates. The scroll viewer scrolls over the panel's margin too
    private Point ViewMiddleIn(VirtualizingGallery gallery)
    {
        if (_scrollViewer is null)
        {
            return default;
        }

        var offset = _scrollViewer.Offset;
        var viewport = _scrollViewer.Viewport;
        var margin = gallery.Margin;
        return new Point(offset.X - margin.Left + viewport.Width / 2, offset.Y - margin.Top + viewport.Height / 2);
    }

    private readonly record struct TileToSharpen(double Distance, GalleryItem Item, GalleryItemViewModel Vm,
        uint Height)
    {
        public static readonly Comparison<TileToSharpen> NearestFirst =
            static (a, b) => a.Distance.CompareTo(b.Distance);
    }

    #endregion

    #region Spatial Navigation Logic

    /// <summary>
    /// Moves the selection by an arrow key, or to the first or last item. In the Expanded Gallery's Masonry and Rows the
    /// arrows follow <see cref="GalleryNavigator"/>, with no wrap; elsewhere the column rules below apply.
    /// </summary>
    public void Navigate(NavigationDirection direction)
    {
        if (ItemCount is 0)
        {
            return;
        }

        switch (direction)
        {
            case NavigationDirection.First:
                UpdateSelectionIndex(0);
                ScrollItemIntoView(0);
                return;
            case NavigationDirection.Last:
                UpdateSelectionIndex(ItemCount - 1);
                ScrollItemIntoView(ItemCount - 1);
                return;
        }

        var startIndex = SelectedItemIndex == -1 ? CurrentItemIndex : SelectedItemIndex;
        if (startIndex < 0) startIndex = 0;
        if (startIndex >= ItemCount) startIndex = ItemCount - 1;

        // Before GetItemPositions, which builds a list: this path allocates nothing per key press
        if (ItemsPanelRoot is VirtualizingGallery gallery && IsInVerticalDisplayMode(gallery))
        {
            var target = GalleryNavigator.Neighbor(gallery.ItemBounds, startIndex, direction, gallery.DisplayMode);
            if (target >= 0 && target < ItemCount)
            {
                UpdateSelectionIndex(target);
                ScrollItemIntoView(target);
            }

            return;
        }

        var items = GetItemPositions();
        if (items.Count is 0)
        {
            return;
        }

        if (items.TrueForAll(x => x.Index != startIndex))
        {
            startIndex = items[^1].Index;
        }

        var currentItemPos = items.FirstOrDefault(x => x.Index == startIndex);

        var targetItem = direction switch
        {
            // If there's no item directly above, wrap to the bottom of the previous column
            NavigationDirection.Up => GetClosestItemAbove(currentItemPos, items) ?? GetBottomItemInPreviousColumn(currentItemPos, items),
        
            // If there's no item directly below, wrap to the top of the next column
            NavigationDirection.Down => GetClosestItemBelow(currentItemPos, items) ?? GetTopItemInNextColumn(currentItemPos, items),
        
            NavigationDirection.Left => GetClosestItemLeft(currentItemPos, items),
            NavigationDirection.Right => GetClosestItemRight(currentItemPos, items),
            _ => null
        };

        // FIX: Check against the total ItemCount, and prevent redundant navigation
        if (targetItem is not { } validItem || validItem.Index >= ItemCount || validItem.Index == startIndex)
        {
            return;
        }
        
        switch (direction)
        {                
            case NavigationDirection.Left:
            case NavigationDirection.Right:
                if (validItem.Index is 0) return; // Don't loop or jump
                UpdateSelectionIndex(validItem.Index);
                break;
                    
            case NavigationDirection.Down:
                if (validItem.Index is 0)
                {
                    // If at bottom of column, go to top of next column
                    var nextColumnItem = GetTopItemInNextColumn(currentItemPos, items);
                    UpdateSelectionIndex(nextColumnItem?.Index ?? startIndex);
                }
                else
                {
                    UpdateSelectionIndex(validItem.Index);
                }
                break;
                    
            case NavigationDirection.Up:
                if (validItem.Index is 0)
                {
                    // If at top of column, go to bottom of previous column
                    var prevColumnItem = GetBottomItemInPreviousColumn(currentItemPos, items);
                    if (currentItemPos.Index is 1)
                    {
                        UpdateSelectionIndex(0);
                    }
                    else
                    {
                        UpdateSelectionIndex(prevColumnItem?.Index ?? startIndex);
                    }
                }
                else
                {
                    UpdateSelectionIndex(validItem.Index);
                }
                break;
            default: return;
        }
        
        ScrollItemIntoView(SelectedItemIndex);
    }

    private List<ItemPosition> GetItemPositions()
    {
        var list = new List<ItemPosition>();

        if (ItemsPanelRoot is not VirtualizingGallery gallery)
        {
            return list;
        }

        for (var i = 0; i < ItemCount; i++)
        {
            var bounds = gallery.GetItemBounds(i);
            if (bounds.HasValue)
            {
                list.Add(new ItemPosition(i, bounds.Value.Position, bounds.Value.Size));
            }
        }

        return list;
    }

    private static ItemPosition? GetClosestItemAbove(ItemPosition currentItem, IEnumerable<ItemPosition> items)
    {
        var candidates = items.Where(item => item.Position.Y + item.Size.Height <= currentItem.Position.Y);
        return candidates.OrderByDescending(item => item.Position.Y).ThenBy(item => Math.Abs(item.Position.X - currentItem.Position.X)).FirstOrDefault();
    }

    private static ItemPosition? GetClosestItemBelow(ItemPosition currentItem, IEnumerable<ItemPosition> items)
    {
        var candidates = items.Where(item => item.Position.Y >= currentItem.Position.Y + currentItem.Size.Height);
        return candidates.OrderBy(item => item.Position.Y).ThenBy(item => Math.Abs(item.Position.X - currentItem.Position.X)).FirstOrDefault();
    }

    private static ItemPosition? GetClosestItemLeft(ItemPosition currentItem, IEnumerable<ItemPosition> items)
    {
        var candidates = items.Where(item => item.Position.X + item.Size.Width <= currentItem.Position.X);
        return candidates.OrderByDescending(item => item.Position.X).ThenBy(item => Math.Abs(item.Position.Y - currentItem.Position.Y)).FirstOrDefault();
    }

    private static ItemPosition? GetClosestItemRight(ItemPosition currentItem, IEnumerable<ItemPosition> items)
    {
        var candidates = items.Where(item => item.Position.X >= currentItem.Position.X + currentItem.Size.Width);
        return candidates.OrderBy(item => item.Position.X).ThenBy(item => Math.Abs(item.Position.Y - currentItem.Position.Y)).FirstOrDefault();
    }

    private static ItemPosition? GetTopItemInNextColumn(ItemPosition currentItem, IEnumerable<ItemPosition> items)
    {
        var nextColumnItems = items
            .Where(item => item.Position.X >= currentItem.Position.X + currentItem.Size.Width)
            .OrderBy(item => item.Position.X)
            .ToList();

        if (nextColumnItems.Count is 0)
        {
            return null;
        }

        var nextColumnX = nextColumnItems[0].Position.X;

        return nextColumnItems
            .Where(item => Math.Abs(item.Position.X - nextColumnX) < 1.0)
            .OrderBy(item => item.Position.Y)
            .FirstOrDefault();
    }

    private static ItemPosition? GetBottomItemInPreviousColumn(ItemPosition currentItem, IEnumerable<ItemPosition> items)
    {
        var prevColumnItems = items
            .Where(item => item.Position.X + item.Size.Width <= currentItem.Position.X)
            .OrderByDescending(item => item.Position.X)
            .ToList();

        if (prevColumnItems.Count == 0) return null;

        var prevColumnX = prevColumnItems[0].Position.X;

        return prevColumnItems
            .Where(item => Math.Abs(item.Position.X - prevColumnX) < 1.0)
            .OrderByDescending(item => item.Position.Y)
            .FirstOrDefault();
    }

    #endregion
    
}