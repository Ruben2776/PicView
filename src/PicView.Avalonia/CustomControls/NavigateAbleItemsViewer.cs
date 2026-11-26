using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.Views.Gallery;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.CustomControls;

[TemplatePart("PART_ScrollViewer", typeof(AutoScrollViewer))]
public class NavigateAbleItemsViewer : ItemsControl
{
    #region Fields and  Avalonia Properties
    
    private const double ScrollLineSize = 50;

    private AutoScrollViewer? _scrollViewer;

    private IDisposable? _viewportSubscription;

    // Turning the motion photo badges on, in the window the viewer is in
    private IDisposable? _badgesSubscription;

    // Whether the viewer is on the visual tree, where a rest can sharpen its tiles
    private bool _isAttached;

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

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttached = true;
        if (TopLevel.GetTopLevel(this)?.DataContext is MainWindowViewModel window)
        {
            // Posted, so it runs after the setting the detection reads has followed the switch
            _badgesSubscription = window.GallerySettings.ShowMotionPhotoBadges
                .Skip(1)
                .Where(static show => show)
                .Subscribe(this, static (_, viewer) => Dispatcher.UIThread.Post(viewer.DetectShownMotionPhotos));
        }
    }

    /// <summary>
    /// Detects the motion photo badges of the items shown, which were loaded while the badges were hidden and so never
    /// checked. Each item that was checked before, or is not shown, is skipped.
    /// </summary>
    private void DetectShownMotionPhotos()
    {
        foreach (var container in GetRealizedContainers())
        {
            if (container is ContentPresenter { Child: GalleryItem item })
            {
                item.DetectMotionPhoto();
            }
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // First, so the sharpening cancelled below, and the items unloaded after, start no rest
        _isAttached = false;
        base.OnDetachedFromVisualTree(e);
        _viewportSubscription?.Dispose();
        _viewportSubscription = null;
        _badgesSubscription?.Dispose();
        _badgesSubscription = null;
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
        // What the item showed by now, it showed at once. A thumbnail that comes later fades in
        galleryItem.IsTakingItem = false;
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
            galleryItem.ReleaseContextMenu();
        }

        if (container is ContentPresenter presenter)
        {
            // Only the content. The base clears the template too, and the presenter then discards the item, which the
            // panel's next use of the container would have to build again
            presenter.ClearValue(ContentPresenter.ContentProperty);
        }
        else
        {
            base.ClearContainerForItemOverride(container);
        }

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

    /// <summary>
    /// Scrolls the Expanded Gallery, which scrolls down in every display mode, just far enough to show the item.
    /// </summary>
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

        if (ItemsPanelRoot is not VirtualizingGallery gallery || gallery.GetItemBounds(index) is not { } rect)
        {
            return;
        }

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
    }

    private void UpdateSelectionIndex(int index)
    {
        SelectedItemIndex = index;
    }

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
    /// user moving the view, so neither postpones sharpening. A viewer off the visual tree starts nothing.
    /// </summary>
    public void RequestTileSharpen()
    {
        if (!TileRestTimer.IsEnabled && _isAttached && ItemsPanelRoot is VirtualizingGallery gallery &&
            ShowsTiles(gallery))
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
                SmallThumbnailIsTallEnough(vm, needed) || gallery.GetItemBounds(realized.Index) is not { } bounds)
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

    /// <summary>
    /// Whether the item shows a small thumbnail with at least <paramref name="needed"/> pixels in height, as when the tile
    /// is shorter than the gallery height or the image is small. A Tile Thumbnail is compared by the height it was made
    /// for instead, which <see cref="GalleryItemViewModel.TileHeightShown"/> holds.
    /// </summary>
    private static bool SmallThumbnailIsTallEnough(GalleryItemViewModel vm, uint needed) =>
        vm is { TileHeightShown: 0, Image.Value: Bitmap bitmap } && bitmap.PixelSize.Height >= needed;

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
    /// Moves the Expanded Gallery's selection by an arrow key, as <see cref="GalleryNavigator"/> says, with no wrap, or
    /// to the first or last item. Only the Expanded Gallery is navigated by keys, so a docked panel moves nothing.
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

        if (ItemsPanelRoot is not VirtualizingGallery { IsExpanded: true } gallery)
        {
            return;
        }

        var startIndex = SelectedItemIndex == -1 ? CurrentItemIndex : SelectedItemIndex;
        startIndex = Math.Clamp(startIndex, 0, ItemCount - 1);

        // Reads the panel's bounds in place: this path allocates nothing per key press
        var target = GalleryNavigator.Neighbor(gallery.ItemBounds, startIndex, direction, gallery.DisplayMode);
        if (target >= 0 && target < ItemCount)
        {
            UpdateSelectionIndex(target);
            ScrollItemIntoView(target);
        }
    }

    #endregion
    
}