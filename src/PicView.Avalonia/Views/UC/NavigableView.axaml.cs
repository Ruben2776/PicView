using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC;

/// <summary>
/// What a tab shows while it navigates its images: the gallery, the click arrows, the hover bar and the gallery
/// shortcut around a slot that holds the viewer (ADR-0022). The tab keeps one, made once, and shows it again after a
/// crop or the menu, so the gallery is never rebuilt.
/// </summary>
public partial class NavigableView : UserControl, IZoomScrollTarget, IDisposable
{
    private readonly TabViewModel _tab;
    private bool _isLoadedOnce;
    private DisposableBag _disposables;

    /// <param name="tab">The tab this view shows, which is not always the active one: a view is also created for a
    /// tab that opens in the background.</param>
    public NavigableView(TabViewModel tab)
    {
        _tab = tab;
        DataContext = tab;
        InitializeComponent();
        GalleryView.Initialize(tab);
        Loaded += OnLoaded;
    }

    /// <summary>
    /// The viewer in the slot, or nothing before one is shown.
    /// </summary>
    public INavigableContent? Viewer { get; private set; }

    /// <summary>
    /// Puts <paramref name="viewer"/> in the slot, in place of the one there.
    /// </summary>
    public void Show(INavigableContent viewer)
    {
        if (ReferenceEquals(Viewer, viewer))
        {
            return;
        }

        if (Viewer?.ZoomPreview is { } previous)
        {
            OverlayHost.Children.Remove(previous);
        }

        Viewer = viewer;
        Slot.Content = viewer;

        // Moved, not copied: the chrome sits above the whole slot, so the preview is lifted above it
        if (viewer.ZoomPreview is { } preview)
        {
            (preview.Parent as Panel)?.Children.Remove(preview);
            OverlayHost.Children.Add(preview);
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // The view is kept and loaded again each time it is shown after a crop or the menu, and its subscriptions
        // last until it is disposed, so they are made on the first load only
        if (_isLoadedOnce)
        {
            return;
        }

        _isLoadedOnce = true;

        // Routed here, not in the viewer, so the wheel, a touchpad magnify and a pinch over the arrows, the hover bar
        // and the gallery shortcut reach the viewer too. The gallery is outside the content panel and keeps its wheel
        ContentPanel.AddHandler(PointerWheelChangedEvent, OnContentWheelChanged, RoutingStrategies.Tunnel);
        ContentPanel.AddHandler(PointerTouchPadGestureMagnifyEvent, OnContentMagnify, RoutingStrategies.Bubble);
        ContentPanel.AddHandler(PinchEvent, OnContentMagnify, RoutingStrategies.Bubble);

        // Correspond to change when index clicked on track
        Observable.FromEvent<EventHandler<int>, int>(
                handler => (_, index) => handler(index),
                handler => HoverBar.ProgressBar.ClickedOnTrack += handler,
                handler => HoverBar.ProgressBar.ClickedOnTrack -= handler)
            .SubscribeAwait(async (x, _) =>
            {
                await _tab.ImageIterator.SkipToIndexAsync(x, _tab.GetTabCancellation()).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(NavigableView), nameof(OnLoaded)), AwaitOperation.Drop)
            .AddTo(ref _disposables);
        // Correspond to change when index dragged on track
        // wait for a 25ms pause in changes (debounce), and then emit the last value.
        Observable.FromEvent<EventHandler<int>, int>(
                handler => (_, index) => handler(index),
                handler => HoverBar.ProgressBar.DraggedOnTrack += handler,
                handler => HoverBar.ProgressBar.DraggedOnTrack -= handler)
            .Debounce(TimeSpan.FromMilliseconds(25)) // Debounce handles rapid events during drag
            .SubscribeAwait(async (x, _) =>
            {
                await _tab.ImageIterator.SkipToIndexAsync(x, _tab.GetTabCancellation()).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(NavigableView), nameof(OnLoaded)), AwaitOperation.Drop)
            .AddTo(ref _disposables);

        _disposables.Add(new HoverFadeButtonHandler(GalleryShortcut, GalleryShortcut.InnerButton));
    }

    private async ValueTask OnContentWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // The wheel only works on the view the active tab shows
        if (Viewer is not { } viewer ||
            TopLevel.GetTopLevel(this) is not MainWindow
            {
                DataContext: MainWindowViewModel { WindowTabs.ActiveTab.CurrentValue.CurrentView.Value: var view }
            } mainWindow ||
            !ReferenceEquals(view, this))
        {
            return;
        }

        e.Handled = await mainWindow.KeybindRouter.WheelAsync(Keybind.FromWheel(e.Delta.Y, e.KeyModifiers),
            e.Pointer.Type is PointerType.Touch, viewer.ZoomPointer(e), viewer).ConfigureAwait(true);
    }

    private void OnContentMagnify(object? sender, PointerDeltaEventArgs e) => Viewer?.Magnify(e);

    #region IZoomScrollTarget

    bool IZoomScrollTarget.CanScrollVertically => Viewer?.CanScrollVertically ?? false;

    bool IZoomScrollTarget.IsImageTallerThanViewer => Viewer?.IsImageTallerThanViewer ?? false;

    void IZoomScrollTarget.ScrollLine(bool down) => Viewer?.ScrollLine(down);

    void IZoomScrollTarget.ScrollToHome() => Viewer?.ScrollToHome();

    void IZoomScrollTarget.ScrollToEnd() => Viewer?.ScrollToEnd();

    void IZoomScrollTarget.ZoomIn() => Viewer?.ZoomIn();

    void IZoomScrollTarget.ZoomOut() => Viewer?.ZoomOut();

    void IZoomScrollTarget.ZoomAt(bool zoomIn, Point pointer) => Viewer?.ZoomAt(zoomIn, pointer);

    #endregion

    /// <summary>
    /// Stops following the tab's gallery and hover bar. The viewer in the slot is its owner's to dispose.
    /// </summary>
    public void Dispose()
    {
        ContentPanel.RemoveHandler(PointerWheelChangedEvent, OnContentWheelChanged);
        ContentPanel.RemoveHandler(PointerTouchPadGestureMagnifyEvent, OnContentMagnify);
        ContentPanel.RemoveHandler(PinchEvent, OnContentMagnify);
        GalleryView.Dispose();
        _disposables.Dispose();
        HoverBar.Dispose();
        GC.SuppressFinalize(this);
    }
}
