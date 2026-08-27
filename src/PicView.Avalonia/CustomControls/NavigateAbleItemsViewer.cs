using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.CustomControls;

public readonly record struct ItemPosition(int Index, Point Position, Size Size);

[TemplatePart("PART_ScrollViewer", typeof(AutoScrollViewer))]
public class NavigateAbleItemsViewer : ItemsControl
{
    #region Fields and  Avalonia Properties
    
    private AutoScrollViewer? _scrollViewer;

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

    public static readonly StyledProperty<bool> CenterCurrentItemProperty =
        AvaloniaProperty.Register<NavigateAbleItemsViewer, bool>(nameof(CenterCurrentItem));

    public bool CenterCurrentItem
    {
        get => GetValue(CenterCurrentItemProperty);
        set => SetValue(CenterCurrentItemProperty, value);
    }

    #endregion

    #region Constructor and Control Overrides

    public NavigateAbleItemsViewer()
    {
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Direct | RoutingStrategies.Tunnel);
        LayoutUpdated += (_, _) => ScheduleVisibilityUpdate();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _scrollViewer = e.NameScope.Find<AutoScrollViewer>("PART_ScrollViewer");
    }


    private bool _isVisibilityUpdatePending;
    
    private void ScheduleVisibilityUpdate()
    {
        if (_isVisibilityUpdatePending)
        {
            return;
        }
        _isVisibilityUpdatePending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _isVisibilityUpdatePending = false;
            UpdateViewportVisibility();
        }, DispatcherPriority.Background);
    }
    
    private void UpdateViewportVisibility()
    {
        var animControl = this.FindLogicalAncestorOfType<GalleryAnimationControl>();
        var isAnimating = animControl?.IsInAnimation ?? false;

        var viewportRect = new Rect(new Point(0, 0), _scrollViewer.Viewport);
        // Extend viewport slightly to preload items right before the user scrolls into view
        var extendedViewportRect = viewportRect.Inflate(new Thickness(0, 200, 0, 200)); 

        for (var i = 0; i < ItemCount; i++)
        {
            var container = ContainerFromIndex(i);
            if (container is not { IsVisible: true })
            {
                continue;
            }
            
            if (container is not ContentPresenter { Child: NavigateAbleItem item })
            {
                continue;
            }

            var position = container.TranslatePoint(new Point(0, 0), _scrollViewer);
            if (!position.HasValue)
            {
                continue;
            }

            var itemRect = new Rect(position.Value, container.Bounds.Size);
            var isVisible = extendedViewportRect.Intersects(itemRect);
            if (!isAnimating && isVisible)
            {
                item.SetViewportVisibility(isVisible);
            }
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CurrentItemIndexProperty)
        {
            if (change is { OldValue: int oldIndex, NewValue: int newIndex })
            {
                ApplyCurrentItemVisualState(newIndex, oldIndex);
                SelectedItemIndex = newIndex;
            }
        }
        else if (change.Property == SelectedItemIndexProperty)
        {
            if (change is { OldValue: int oldIndex, NewValue: int newIndex })
            {
                UpdatePreviousAndNextSelection(newIndex, oldIndex);
            }
        }
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is not ContentPresenter presenter)
        {
            return;
        }

        presenter.ApplyTemplate();
        if (presenter.Child is not NavigateAbleItem navItem)
        {
            return;
        }

        if (index == CurrentItemIndex)
        {
            navItem.SetCurrent(true);
        }

        if (index == SelectedItemIndex) navItem.SetSelected(true);

        // If VirtualizingGallery prepared it, it is in the viewport! Load the image.
        navItem.SetViewportVisibility(true);
    }

    
    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        if (container is ContentPresenter { Child: NavigateAbleItem navItem })
        {
            // The panel is recycling this item. Unload the image.
            navItem.SetViewportVisibility(false);
        }
    }
    #endregion

    #region Scrolling & Viewport Logic

    public void SetVerticalScrolling()
    {
        if (_scrollViewer == null)
        {
            return;
        }

        _scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
    }
    
    public void SetHorizontalScrolling()
    {
        if (_scrollViewer == null)
        {
            return;
        }

        _scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
        _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    public void ScrollToCenterOfCurrentItem()
    {
        // Need to use Post to have calculations take place after render
        Dispatcher.UIThread.Post(() =>
        {
            if (_scrollViewer is null)
            {
                return;
            }

            // Ask the VirtualizingGallery for the exact bounds, realized or not!
            if (ItemsPanelRoot is not VirtualizingGallery gallery)
            {
                return;
            }

            var itemRect = gallery.GetItemBounds(CurrentItemIndex);
            if (itemRect is null)
            {
                return;
            }

            var pos = itemRect.Value;
            var offset = _scrollViewer.Offset;
            var newX = offset.X;
            var newY = offset.Y;

            // Center Horizontally if scrolling is possible
            if (_scrollViewer.Extent.Width > _scrollViewer.Viewport.Width)
            {
                var itemCenter = pos.X + pos.Width / 2;
                var viewportCenter = _scrollViewer.Viewport.Width / 2;
                var maxScrollX = _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width;
                
                newX = Math.Clamp(itemCenter - viewportCenter, 0, maxScrollX);
            }

            // Center Vertically if scrolling is possible
            if (_scrollViewer.Extent.Height > _scrollViewer.Viewport.Height)
            {
                var itemCenter = pos.Y + pos.Height / 2;
                var viewportCenter = _scrollViewer.Viewport.Height / 2;
                var maxScrollY = _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height;
                
                newY = Math.Clamp(itemCenter - viewportCenter, 0, maxScrollY);
            }

            _scrollViewer.Offset = new Vector(newX, newY);
        });
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
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

        if (e.Delta.Y < 0 || e.Delta.X < 0)
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
        if (e.Delta.Y < 0 || e.Delta.X < 0)
        {
            _scrollViewer.LineRight();
        }
        else
        {
            _scrollViewer.LineLeft();
        }
    }

    #endregion

    #region Selection & Visual State Management

    public void UpdatePreviousAndNextSelection(int index, int prevIndex)
    {
        if (_scrollViewer == null || index < 0 || index >= ItemCount 
            || ContainerFromIndex(index) is not ContentPresenter presenter)
        {
            return;
        }

        if (presenter.Child is not NavigateAbleItem item)
        {
            return;
        }

        item.SetSelected(true);

        // Handle deselecting the previous item
        if (prevIndex == index || prevIndex < 0 || prevIndex >= ItemCount 
            || ContainerFromIndex(prevIndex) is not ContentPresenter prevPresenter)
        {
            return;
        }
        
        if (prevPresenter.Child is NavigateAbleItem prevItem)
        {
            prevItem.SetSelected(false);
        }
    }

    private void ApplyCurrentItemVisualState(int index, int prevIndex)
    {
        if (_scrollViewer == null || index < 0 || index >= ItemCount
            || ContainerFromIndex(index) is not ContentPresenter presenter)
        {
            return;
        }

        if (presenter.Child is NavigateAbleItem item)
        {
            item.SetCurrent(true);
        }

        if (prevIndex == index || prevIndex < 0 || prevIndex >= ItemCount
            || ContainerFromIndex(prevIndex) is not ContentPresenter prevPresenter)
        {
            return;
        }

        if (prevPresenter.Child is NavigateAbleItem prevItem)
        {
            prevItem.SetCurrent(false);
        }
    }

    private void UpdateSelectionIndex(int index)
    {
        SelectedItemIndex = index;
    }
    
    #endregion

    #region Spatial Navigation Logic

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
                ScrollToCenterOfCurrentItem();
                return;
            case NavigationDirection.Last:
                UpdateSelectionIndex(ItemCount - 1);
                ScrollToCenterOfCurrentItem();
                return;
        }

        var startIndex = SelectedItemIndex == -1 ? CurrentItemIndex : SelectedItemIndex;
        if (startIndex < 0) startIndex = 0;
        if (startIndex >= ItemCount) startIndex = ItemCount - 1;

        var items = GetItemPositions();
        if (items.Count is 0)
        {
            return;
        }

        if (items.All(x => x.Index != startIndex))
        {
            startIndex = items.Last().Index;
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
        
        ContainerFromIndex(SelectedItemIndex)?.BringIntoView();
    }

    private List<ItemPosition> GetItemPositions()
    {
        var list = new List<ItemPosition>();

        if (ItemsPanelRoot is VirtualizingGallery gallery)
        {
            for (var i = 0; i < ItemCount; i++)
            {
                var bounds = gallery.GetItemBounds(i);
                if (bounds.HasValue)
                {
                    list.Add(new ItemPosition(i, bounds.Value.Position, bounds.Value.Size));
                }
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

        if (nextColumnItems.Count is 0) return null;

        var nextColumnX = nextColumnItems.First().Position.X;

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

        var prevColumnX = prevColumnItems.First().Position.X;

        return prevColumnItems
            .Where(item => Math.Abs(item.Position.X - prevColumnX) < 1.0)
            .OrderByDescending(item => item.Position.Y)
            .FirstOrDefault();
    }

    #endregion
    
}