using System.Collections;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// A horizontal custom TabControl that adds drag-to-reorder, drag-to-detach, and auto-scroll while dragging behavior.
/// </summary>
[PseudoClasses(PseudoDragging, PseudoDetaching)]
public class DraggableTabControl : TabControl
{
    // --- Constants ---
    private const double DragThreshold = 4.0;
    private const double DetachThreshold = 50.0;

    // Auto-Scroll Settings
    private const double ScrollTriggerZone = 30.0; // Distance from edge to trigger scroll
    private const double ScrollSpeed = 15.0; // Pixels per tick
    private readonly DispatcherTimer? _autoScrollTimer;

    private const string PseudoDragging = ":dragging";
    private const string PseudoDetaching = ":detaching";

    // Ghost Window Settings
    private const double GhostOpacity = 0.5;
    private const double GhostOffsetX = 30.0;
    private const double GhostOffsetY = 15.0;

    // --- State Fields ---
    private readonly Dictionary<TabItem, double> _originalXPositions = new();
    private int _currentTargetIndex = -1;
    private double _draggedTabStartX;
    private double _draggedTabWidth;

    private bool _isSwitchingTabs; // Prevent accidental tab switching

    private Window? _ghostWindow;
    private bool _isDetaching;

    private bool _isDragging;
    private bool _isForeignDragging;
    private Point _lastPointerPosition; // Track pointer for timer updates

    // Represents the mouse offset relative to the Dragged Tab's Left Edge (Content Space)
    private double _pointerOffsetWithinTab;

    private TabItem? _pressedTab;
    private double _scrollDirection; // -1 for Left, 1 for Right, 0 for None

    // Auto-Scroll State
    private AutoScrollViewer? _scrollViewer;

    private int _sourceIndex = -1;
    private Point _startClickPoint;

    public DraggableTabControl()
    {
        // Initialize Auto-Scroll Timer
        _autoScrollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _autoScrollTimer.Tick += AutoScrollTimerOnTick;
    }

    protected override Type StyleKeyOverride => typeof(DraggableTabControl);

    // --- Events ---
    public event EventHandler<TabDetachEventArgs>? TabDetached;
    public event EventHandler<TabCreatedEventArgs>? TabCreated;

    #region Auto-Scroll Logic

    private void AutoScrollTimerOnTick(object? sender, EventArgs e)
    {
        if (_scrollViewer == null || _scrollDirection == 0 || !_isDragging)
        {
            return;
        }

        var currentOffset = _scrollViewer.Offset.X;
        var maxOffset = _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width;
        var targetOffset = currentOffset + _scrollDirection * ScrollSpeed;

        // Clamp Target
        targetOffset = Math.Clamp(targetOffset, 0, maxOffset);


        if (!(Math.Abs(targetOffset - currentOffset) > 0.1))
        {
            return;
        }

        // Apply Scroll
        _scrollViewer.Offset = new Vector(targetOffset, _scrollViewer.Offset.Y);

        // Re-process the drag visuals with the new Scroll Offset
        // using the last known mouse position (which hasn't moved relative to the Window)
        ProcessDragMovement(_lastPointerPosition);
    }

    #endregion

    #region Lifecycle Overrides

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        // Attempt to find the internal ScrollViewer defined in the template
        _scrollViewer = this.FindDescendantOfType<AutoScrollViewer>();
    }

    protected override void ContainerForItemPreparedOverride(Control container, object? item, int index)
    {
        base.ContainerForItemPreparedOverride(container, item, index);

        if (container is not TabItem tabItem)
        {
            return;
        }

        if (item is TabViewModel { IsClosing: false })
        {
            TabCreated?.Invoke(this, new TabCreatedEventArgs(item, index));
        }
        
        tabItem.AddHandler(PointerPressedEvent, OnItemPointerPressed,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        tabItem.AddHandler(PointerMovedEvent, OnItemPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        tabItem.AddHandler(PointerReleasedEvent, OnItemPointerReleased,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        tabItem.PointerCaptureLost += OnPointerCaptureLost;

        tabItem.AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        
        SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _isSwitchingTabs = IsPointerOver;
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        if (container is TabItem tabItem)
        {
            tabItem.RemoveHandler(PointerPressedEvent, OnItemPointerPressed);
            tabItem.RemoveHandler(PointerMovedEvent, OnItemPointerMoved);
            tabItem.RemoveHandler(PointerReleasedEvent, OnItemPointerReleased);
            tabItem.PointerCaptureLost -= OnPointerCaptureLost;

            if (tabItem.DataContext is TabViewModel { IsClosing: true } && _pressedTab == tabItem)
            {
                EndDrag();
            }

            tabItem.RenderTransform = null;
        }

        base.ClearContainerForItemOverride(container);
    }

    #endregion

    #region Pointer Events

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.Delta.Y > 0)
        {
            _scrollViewer.LineLeft();
        }
        else
        {
            _scrollViewer.LineRight();
        }
    }

    private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        if (!props.IsLeftButtonPressed || sender is not TabItem tabItem)
        {
            return;
        }

        _pressedTab = tabItem;
        _startClickPoint = e.GetPosition(this); // Viewport relative

        _isDragging = false;
        _isDetaching = false;


        ItemFromContainer(tabItem);
        _sourceIndex = IndexFromContainer(tabItem);
        _currentTargetIndex = _sourceIndex;

        CacheTabPositions();

        _draggedTabStartX = tabItem.Bounds.X; // Content relative
        _draggedTabWidth = tabItem.Bounds.Width;

        // Calculate offset in Content Space
        var currentScrollX = _scrollViewer?.Offset.X ?? 0;
        // The mouse X in content space is (ViewportX + ScrollX)
        // The Tab X is (ContentX)
        _pointerOffsetWithinTab = _startClickPoint.X + currentScrollX - _draggedTabStartX;
    }

    private void OnItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pressedTab == null)
        {
            return;
        }
        if (_isSwitchingTabs)
        {
            return;
        }

        _lastPointerPosition = e.GetPosition(this);
        var deltaX = _lastPointerPosition.X - _startClickPoint.X;
        var deltaY = _lastPointerPosition.Y - _startClickPoint.Y;
        var absDeltaY = Math.Abs(deltaY);

        // 1. Check start dragging
        if (!_isDragging)
        {
            if (!HandleDragStart(e, deltaX, absDeltaY))
            {
                return;
            }
        }

        // 2. State Transition (Attached <-> Detached)
        HandleStateTransition(absDeltaY);

        // 3. Movement Logic
        if (_isDetaching)
        {
            StopAutoScroll(); // Don't scroll parent when detached
            HandleExternalDrag(e);
        }
        else
        {
            // Handle Auto-Scroll Triggers
            UpdateScrollState(_lastPointerPosition.X);

            // Update Visuals
            ProcessDragMovement(_lastPointerPosition);
        }

        e.Handled = true;
    }

    private void UpdateScrollState(double mouseViewportX)
    {
        if (_scrollViewer == null)
        {
            return;
        }

        // Determine direction
        if (mouseViewportX < ScrollTriggerZone)
        {
            _scrollDirection = -1; // Scroll Left
        }
        else if (mouseViewportX > Bounds.Width - ScrollTriggerZone)
        {
            _scrollDirection = 1; // Scroll Right
        }
        else
        {
            _scrollDirection = 0;
        }

        // Start/Stop Timer
        if (_scrollDirection != 0 && !_autoScrollTimer!.IsEnabled)
        {
            _autoScrollTimer.Start();
        }
        else if (_scrollDirection == 0 && _autoScrollTimer!.IsEnabled)
        {
            _autoScrollTimer.Stop();
        }
    }

    private void StopAutoScroll()
    {
        _scrollDirection = 0;
        _autoScrollTimer?.Stop();
    }

    private void ProcessDragMovement(Point mouseViewportPos)
    {
        if (_scrollViewer == null)
        {
            return;
        }

        // 1. Calculate Clamped Viewport Position (The "Stickiness" Logic)
        var clampedViewportX = mouseViewportPos.X;
        var currentScrollX = _scrollViewer.Offset.X;
        var maxScrollX = _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width;

        var canScrollLeft = currentScrollX > 0.1;
        var canScrollRight = currentScrollX < maxScrollX - 0.1;

        // If we can't scroll left, clamp drag to 0
        if (!canScrollLeft && clampedViewportX < 0)
        {
            clampedViewportX = 0;
        }
        // If we can't scroll right, clamp drag to Bounds
        else if (!canScrollRight && clampedViewportX > Bounds.Width)
        {
            clampedViewportX = Bounds.Width;
        }

        // 2. Convert to Content Space
        // Visual Pos + Scroll Offset = Absolute Content Position
        var contentMouseX = clampedViewportX + currentScrollX;

        // 3. Apply the original click offset to find the top-left of the dragging tab
        var dragLeftPosContent = contentMouseX - _pointerOffsetWithinTab;

        UpdateTabReorderingVisuals(dragLeftPosContent);
    }

    private void OnItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isSwitchingTabs = false;
        
        CloseGhostWindow();
        StopAutoScroll();

        if (_pressedTab == null)
        {
            return;
        }

        if (_isDetaching)
        {
            if (TabDragContext.CurrentTarget != null && TabDragContext.CurrentTarget != this)
            {
                TabDragContext.CurrentTarget.ExternalDrop();
                var item = ItemFromContainer(_pressedTab);
                if (item != null)
                {
                    var screenPos = _pressedTab.PointToScreen(e.GetPosition(_pressedTab));
                    TabDetached?.Invoke(this, new TabDetachEventArgs(item, screenPos));
                }
            }
            else
            {
                PerformDetach(e);
            }
        }
        else if (_isDragging && _currentTargetIndex >= 0)
        {
            if (TryMoveItem(_sourceIndex, _currentTargetIndex))
            {
                SelectedIndex = _currentTargetIndex;
            }
        }

        EndDrag();
        e.Handled = true;
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        StopAutoScroll();
        EndDrag();
    }

    #endregion

    #region Drag Logic Helpers

    private void CacheTabPositions()
    {
        _originalXPositions.Clear();
        foreach (var item in GetRealizedContainers().OfType<TabItem>())
        {
            _originalXPositions[item] = item.Bounds.X;
        }
    }

    private bool HandleDragStart(PointerEventArgs e, double deltaX, double absDeltaY)
    {
        if (Math.Abs(deltaX) <= DragThreshold && absDeltaY <= DragThreshold)
        {
            return false;
        }

        _isDragging = true;
        if (_pressedTab == null)
        {
            return false;
        }

        e.Pointer.Capture(_pressedTab);
        PseudoClasses.Set(PseudoDragging, true);
        _pressedTab.ZIndex = 1000;

        TabDragContext.StartDrag(DataContext as TabViewModel, this);
        if (_pressedTab.DataContext is TabViewModel vm)
        {
            TabDragContext.DraggingItem = vm;
        }

        return true;
    }

    private void HandleStateTransition(double absDeltaY)
    {
        if (_pressedTab == null)
        {
            return;
        }

        switch (_isDetaching)
        {
            case false when absDeltaY > DetachThreshold:
                _isDetaching = true;
                PseudoClasses.Set(PseudoDetaching, true);
                PseudoClasses.Set(PseudoDragging, false);
                _pressedTab.Opacity = 0;
                CreateGhostWindow(_pressedTab);
                break;
            case true when absDeltaY <= DetachThreshold:
                // Check if the pointer is within the horizontal bounds of the control
                var isWithinBounds = _lastPointerPosition.X >= 0 && _lastPointerPosition.X <= Bounds.Width;

                if (isWithinBounds && (TabDragContext.CurrentTarget == null || TabDragContext.CurrentTarget == this))
                {
                    _isDetaching = false;
                    PseudoClasses.Set(PseudoDetaching, false);
                    PseudoClasses.Set(PseudoDragging, true);
                    _pressedTab.Opacity = 1.0;
                    CloseGhostWindow();

                    if (TabDragContext.CurrentTarget != null && TabDragContext.CurrentTarget != this)
                    {
                        TabDragContext.CurrentTarget.ExternalDragLeave(TabDragContext.DraggingItem!);
                        TabDragContext.CurrentTarget = null;
                    }

                    // Force a visual update immediately upon re-attaching
                    ProcessDragMovement(_lastPointerPosition);
                }

                break;
        }
    }

    // Expects dragLeftPos in CONTENT SPACE (Absolute position inside the scroll viewer)
    private void UpdateTabReorderingVisuals(double dragLeftPos)
    {
        var dragCenter = dragLeftPos + _draggedTabWidth / 2;
        var realizedItems = GetRealizedContainers().OfType<TabItem>().ToArray();

        if (realizedItems.Length == 0)
        {
            return;
        }

        // 1. Calculate Target Index
        var newTargetIndex = _sourceIndex;
        for (var i = 0; i < realizedItems.Length; i++)
        {
            var tab = realizedItems[i];
            var tabStartX = _originalXPositions.TryGetValue(tab, out var val) ? val : tab.Bounds.X;
            var tabCenter = tabStartX + tab.Bounds.Width / 2;

            if (dragCenter > tabCenter)
            {
                newTargetIndex = i;
            }
        }

        _currentTargetIndex = newTargetIndex;

        // 2. Apply Transforms
        for (var i = 0; i < realizedItems.Length; i++)
        {
            var tab = realizedItems[i];
            var startX = _originalXPositions.GetValueOrDefault(tab, tab.Bounds.X);
            double offsetX = 0;

            if (tab == _pressedTab)
            {
                // Move based on mouse drag
                offsetX = dragLeftPos - startX;
            }
            else
            {
                // Shift other items
                if (_currentTargetIndex > _sourceIndex && i > _sourceIndex && i <= _currentTargetIndex)
                {
                    offsetX = -_draggedTabWidth;
                }
                else if (_currentTargetIndex < _sourceIndex && i >= _currentTargetIndex && i < _sourceIndex)
                {
                    offsetX = _draggedTabWidth;
                }
            }

            tab.RenderTransform = new TranslateTransform(offsetX, 0);
        }
    }

    private void PerformDetach(PointerReleasedEventArgs e)
    {
        if (_pressedTab == null)
        {
            return;
        }

        var item = ItemFromContainer(_pressedTab);
        if (item == null)
        {
            return;
        }

        var screenPos = _pressedTab.PointToScreen(e.GetPosition(_pressedTab));
        var list = ItemsSource as IList ?? Items;
        if (list != null && _sourceIndex >= 0 && _sourceIndex < list.Count)
        {
            list.RemoveAt(_sourceIndex);
        }

        TabDetached?.Invoke(this, new TabDetachEventArgs(item, screenPos));
    }

    private void EndDrag()
    {
        TabDragContext.EndDrag();
        StopAutoScroll();

        if (_pressedTab != null)
        {
            PseudoClasses.Remove(PseudoDragging);
            PseudoClasses.Remove(PseudoDetaching);
            _pressedTab.Opacity = 1.0;
            _pressedTab.ZIndex = 0;
        }

        foreach (var c in GetRealizedContainers())
        {
            c.RenderTransform = null;
        }

        CloseGhostWindow();

        _pressedTab = null;
        _isDragging = false;
        _isDetaching = false;
        _isForeignDragging = false;
        _sourceIndex = -1;
        _currentTargetIndex = -1;
        _originalXPositions.Clear();
    }

    private bool TryMoveItem(int oldIndex, int newIndex)
    {
        if (oldIndex == newIndex)
        {
            return true;
        }

        var list = ItemsSource as IList ?? Items;
        if (list == null || oldIndex < 0 || oldIndex >= list.Count)
        {
            return false;
        }

        newIndex = Math.Clamp(newIndex, 0, list.Count - 1);

        try
        {
            var item = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region External Drag Logic

    // Note: External drag logic generally doesn't require auto-scrolling of the SOURCE window
    // but relies on the target window's scroll logic. 
    // This implementation keeps external logic mostly as is, assuming 'UpdateTabReorderingVisuals'
    // handles the transforms correctly.

    private void HandleExternalDrag(PointerEventArgs e)
    {
        if (_pressedTab == null)
        {
            return;
        }

        var screenPosPixel = _pressedTab.PointToScreen(e.GetPosition(_pressedTab));

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                var winPoint = window.PointToClient(screenPosPixel);
                var winRect = new Rect(0, 0, window.ClientSize.Width, window.ClientSize.Height);

                if (!winRect.Contains(winPoint))
                {
                    continue;
                }

                var tabControl = window.FindDescendantOfType<DraggableTabControl>();
                if (tabControl == null)
                {
                    continue;
                }

                var tabPoint = tabControl.PointToClient(screenPosPixel);
                var tabStripBounds = new Rect(0, 0, tabControl.Bounds.Width, DetachThreshold);

                if (tabStripBounds.Contains(tabPoint))
                {
                    if (tabControl != TabDragContext.CurrentTarget)
                    {
                        if (TabDragContext.CurrentTarget != null && TabDragContext.CurrentTarget != this)
                        {
                            TabDragContext.CurrentTarget.ExternalDragLeave(TabDragContext.DraggingItem!);
                        }

                        TabDragContext.CurrentTarget = tabControl;
                        if (tabControl != this)
                        {
                            tabControl.ExternalDragEnter(TabDragContext.DraggingItem!);
                        }
                    }

                    if (tabControl != this)
                    {
                        tabControl.ExternalDragOver(screenPosPixel);
                        _ghostWindow?.Hide();
                    }
                    else
                    {
                        UpdateGhostWindowPosition(e);
                        _ghostWindow?.Show();
                    }

                    return;
                }
            }
        }

        if (TabDragContext.CurrentTarget != null && TabDragContext.CurrentTarget != this)
        {
            TabDragContext.CurrentTarget.ExternalDragLeave(TabDragContext.DraggingItem!);
        }

        TabDragContext.CurrentTarget = null;
        UpdateGhostWindowPosition(e);
        _ghostWindow?.Show();
    }

    internal void ExternalDragEnter(object item)
    {
        var list = ItemsSource as IList ?? Items;
        if (list == null || list.Contains(item))
        {
            return;
        }

        _isForeignDragging = true;
        list.Add(item);
        UpdateLayout();

        if (ContainerFromItem(item) is not TabItem container)
        {
            return;
        }

        _pressedTab = container;
        _sourceIndex = IndexFromContainer(container);
        _draggedTabWidth = container.Bounds.Width;
        _draggedTabStartX = container.Bounds.X;

        // Center the dragged item under the mouse
        _pointerOffsetWithinTab = _draggedTabWidth / 2;
        CacheTabPositions();
    }

    internal void ExternalDragOver(PixelPoint screenPos)
    {
        if (!_isForeignDragging || _pressedTab == null)
        {
            return;
        }

        var localPos = this.PointToClient(screenPos);

        // Convert local pos (Viewport) to Content space
        var scrollX = _scrollViewer?.Offset.X ?? 0;
        var contentX = localPos.X + scrollX;
        var dragLeftPos = contentX - _pointerOffsetWithinTab;

        UpdateTabReorderingVisuals(dragLeftPos);
    }

    internal void ExternalDragLeave(object item)
    {
        if (!_isForeignDragging)
        {
            return;
        }

        var list = ItemsSource as IList ?? Items;
        if (list != null && list.Contains(item))
        {
            list.Remove(item);
        }

        _isForeignDragging = false;
        _pressedTab = null;
        _sourceIndex = -1;
        _currentTargetIndex = -1;

        foreach (var c in GetRealizedContainers())
        {
            c.RenderTransform = null;
        }
    }

    internal void ExternalDrop()
    {
        if (!_isForeignDragging)
        {
            return;
        }

        _isForeignDragging = false;
        foreach (var c in GetRealizedContainers())
        {
            c.RenderTransform = null;
        }

        _pressedTab = null;
    }

    #endregion

    #region Ghost Window Logic

    private void CreateGhostWindow(TabItem tabItem)
    {
        if (_ghostWindow != null)
        {
            return;
        }

        var contentBitmap = CaptureContentBitmap(tabItem);
        if (contentBitmap == null)
        {
            return;
        }

        var window = TopLevel.GetTopLevel(this) as Window;
        var windowWidth = window?.Width ?? double.NaN;
        var windowHeight = window?.Height ?? double.NaN;
        var cornerRadius = new CornerRadius(16);
        var isMacOs = OperatingSystem.IsMacOS();

        _ghostWindow = new Window
        {
            WindowDecorations = isMacOs ? WindowDecorations.BorderOnly : WindowDecorations.None,
            CornerRadius = cornerRadius,
            ShowInTaskbar = false,
            Topmost = true,
            Background = Brushes.Transparent,
            IsHitTestVisible = false,
            Width = windowWidth,
            Height = windowHeight,
            Content = new Border
            {
                CornerRadius = cornerRadius,
                Opacity = GhostOpacity,
                Child = new Image
                {
                    Source = contentBitmap,
                    Stretch = Stretch.Uniform
                }
            }
        };

        _ghostWindow.Show();
    }

    private Bitmap? CaptureContentBitmap(TabItem tabItem)
    {
        var vm = tabItem.DataContext as TabViewModel;
        return CaptureVisual(vm?.CurrentView.Value as Control);
    }

    private void UpdateGhostWindowPosition(PointerEventArgs e)
    {
        if (_ghostWindow == null || _pressedTab == null)
        {
            return;
        }

        var screenPos = _pressedTab.PointToScreen(e.GetPosition(_pressedTab));
        _ghostWindow.Position = new PixelPoint(
            screenPos.X - (int)GhostOffsetX,
            screenPos.Y - (int)GhostOffsetY
        );
    }

    private void CloseGhostWindow()
    {
        _ghostWindow?.Close();
        _ghostWindow = null;
    }

    private static Bitmap? CaptureVisual(Visual? visual)
    {
        if (visual == null)
        {
            return null;
        }

        var bounds = visual.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return null;
        }

        try
        {
            var pixelSize = new PixelSize((int)bounds.Width, (int)bounds.Height);
            var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
            bitmap.Render(visual);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    #endregion
}

public class TabDetachEventArgs(object detachedItem, PixelPoint screenPosition) : EventArgs
{
    public object DetachedItem { get; } = detachedItem;
    public PixelPoint ScreenPosition { get; } = screenPosition;
}

public class TabCreatedEventArgs(object createdItem, int index) : EventArgs
{
    public object CreatedItem { get; } = createdItem;
    public int Index { get; } = index;
}

// Helper for Shared Drag State
internal static class TabDragContext
{
    public static object? DraggingItem { get; set; }
    public static DraggableTabControl? SourceControl { get; set; }
    public static DraggableTabControl? CurrentTarget { get; set; }

    public static void StartDrag(object? item, DraggableTabControl source)
    {
        DraggingItem = item;
        SourceControl = source;
        CurrentTarget = null;
    }

    public static void EndDrag()
    {
        DraggingItem = null;
        SourceControl = null;
        CurrentTarget = null;
    }
}