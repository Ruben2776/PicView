using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.ImageTransformations;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC;

public partial class ImageViewer : UserControl, IDisposable, IZoomScrollTarget
{
    private bool _isInitialized;
    private bool _isLoadedOnce;
    private DisposableBag _disposables;
    private ScaleTransform? _flipTransform;
    private bool _animatesNextFlip;
    private readonly TabViewModel _tab;

    /// <param name="tab">The tab this viewer shows, which is not always the active one: a viewer is also created for a
    /// tab that opens in the background.</param>
    public ImageViewer(TabViewModel tab)
    {
        _tab = tab;
        DataContext = tab;
        InitializeComponent();
        ZoomPanControl.ScaleChanged += OnScaleChanged;
        GalleryView.Initialize(tab);
        ImageControlHelper.TriggerScalingModeUpdate(MainImage, true);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // A view handed over while this viewer was not shown, as when its tab's crop closes
        if (PendingZoomView is { } pending)
        {
            PendingZoomView = null;
            ShowZoomView(pending);
        }

        // The viewer is kept and loaded again each time it is shown after a crop or the menu, and its handlers
        // and subscriptions last until it is disposed, so they are made on the first load only. Made on every
        // load, one wheel step zoomed once more for each time the viewer had been shown again
        if (_isLoadedOnce)
        {
            return;
        }

        _isLoadedOnce = true;
        AddHandler(PointerWheelChangedEvent, PreviewOnPointerWheelChanged, RoutingStrategies.Tunnel);
        AddHandler(PointerTouchPadGestureMagnifyEvent, TouchMagnifyEvent, RoutingStrategies.Bubble);
        AddHandler(PinchEvent, TouchMagnifyEvent, RoutingStrategies.Bubble);

        Dispatcher.UIThread.Post(InitializeImageTransformer, DispatcherPriority.Background);

        Dispatcher.UIThread.Post(InitializeMotionPhoto, DispatcherPriority.ApplicationIdle);
    }

    private void InitializeMotionPhoto()
    {
        // Float the badges above the transformed image container, so they stay upright
        // when the image is rotated, flipped or zoomed, then keep each badge anchored
        // to the on-screen top-right corner of its image
        MotionPhotoView.FloatBadge(MotionPhotoBadgeHost);
        SecondaryMotionPhotoView.FloatBadge(MotionPhotoBadgeHost);

        MainPanel.LayoutUpdated += OnMotionPhotoBadgeAnchorChanged;
        MotionPhotoView.LayoutUpdated += OnMotionPhotoBadgeAnchorChanged;
        SecondaryMotionPhotoView.LayoutUpdated += OnMotionPhotoBadgeAnchorChanged;
        ImageScrollViewer.ScrollChanged += OnMotionPhotoBadgeScrollChanged;
        UpdateMotionPhotoBadgePositions();

        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        // Zooming and panning move the images through render transforms without
        // triggering layout, so observe the transform values while they change. The
        // child's render transform is read because animated zooms interpolate it
        // while the control's own properties already hold the target values.
        // The frame provider ticks with the render loop, so this only runs while
        // frames are actually being rendered (i.e. while something visually moves)
        Observable.EveryValueChanged(ZoomPanControl, zoom =>
            {
                if (zoom.Child?.RenderTransform is TransformGroup group &&
                    group.Children.Count == 2 &&
                    group.Children[0] is ScaleTransform scale &&
                    group.Children[1] is TranslateTransform translate)
                {
                    return (X: scale.ScaleX, Y: translate.X, Z: translate.Y);
                }

                return (X: zoom.Scale, Y: zoom.TranslateX, Z: zoom.TranslateY);
            }, mainWindow.FrameProvider)
            .Subscribe(_ => UpdateMotionPhotoBadgePositions(),
                DebugHelper.LogError(nameof(ImageViewer), nameof(OnLoaded)))
            .AddTo(ref _disposables);

        // The flip animates the RenderTransform of MainTransform without triggering layout
        Observable.EveryValueChanged(MainTransform,
                transform => (transform.RenderTransform as ScaleTransform)?.ScaleX ?? 1d,
                mainWindow.FrameProvider)
            .Subscribe(_ => UpdateMotionPhotoBadgePositions(),
                DebugHelper.LogError(nameof(ImageViewer), nameof(OnLoaded)))
            .AddTo(ref _disposables);

        // Zoom/pan is locked for the duration of motion photo playback
        MotionPhotoView.PlaybackStarted += OnMotionPhotoPlaybackStarted;
        MotionPhotoView.PlaybackStopped += OnMotionPhotoPlaybackStopped;
        MotionPhotoView.FirstFrameShown += OnMotionPhotoFirstFrameShown;
        SecondaryMotionPhotoView.PlaybackStarted += OnMotionPhotoPlaybackStarted;
        SecondaryMotionPhotoView.PlaybackStopped += OnMotionPhotoPlaybackStopped;
        SecondaryMotionPhotoView.FirstFrameShown += OnMotionPhotoFirstFrameShown;
    }

    private void OnMotionPhotoPlaybackStarted(object? sender, EventArgs e)
    {
        ZoomPanControl.IsEnabled = false;

        // Only one clip plays at a time
        if (ReferenceEquals(sender, MotionPhotoView))
        {
            SecondaryMotionPhotoView.Stop();
        }
        else
        {
            MotionPhotoView.Stop();
        }
    }

    private void OnMotionPhotoPlaybackStopped(object? sender, EventArgs e)
    {
        // Opacity instead of IsVisible: hiding the image would collapse the grid cell
        // that sizes the video overlay.
        if (ReferenceEquals(sender, MotionPhotoView))
        {
            MainImage.Opacity = 1;
        }
        else
        {
            SecondaryImage.Opacity = 1;
        }

        ZoomPanControl.IsEnabled = !MotionPhotoView.IsPlaying && !SecondaryMotionPhotoView.IsPlaying;
    }

    private void OnMotionPhotoFirstFrameShown(object? sender, EventArgs e)
    {
        // Hide the still image while its video covers it, so the letterboxed video
        // never leaves strips of the still visible along its sides. Opacity is used
        // instead of IsVisible so the image keeps sizing the grid cell that hosts
        // the video overlay.
        if (ReferenceEquals(sender, MotionPhotoView))
        {
            MainImage.Opacity = 0;
        }
        else
        {
            SecondaryImage.Opacity = 0;
        }
    }

    /// <summary>
    /// Notifies the motion photo overlays that a new image is displayed,
    /// stopping any running playback and preparing the badge when applicable.
    /// May be called from any thread; UI work is marshalled to the UI thread.
    /// </summary>
    public void UpdateMotionPhoto(TabViewModel tabViewModel)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            UpdateMotionPhotoOverlays(tabViewModel);
        }
        else
        {
            Dispatcher.UIThread.Post(() => UpdateMotionPhotoOverlays(tabViewModel));
        }
    }

    private void UpdateMotionPhotoOverlays(TabViewModel tabViewModel)
    {
        // No SingleImageType check is needed: single (non-file) images such as clipboard or
        // base64 images never go through file-based detection, so their MotionPhoto metadata
        // is always null and the overlays hide themselves. Images downloaded from a URL live
        // in a real temp file and intentionally behave like any local file.
        MotionPhotoView.OnImageChanged(tabViewModel.Model);
        SecondaryMotionPhotoView.OnImageChanged(tabViewModel.SecondaryModel);
        UpdateMotionPhotoBadgePositions();
    }

    private void OnMotionPhotoBadgeAnchorChanged(object? sender, EventArgs e) =>
        UpdateMotionPhotoBadgePositions();

    private void OnMotionPhotoBadgeScrollChanged(object? sender, ScrollChangedEventArgs e) =>
        UpdateMotionPhotoBadgePositions();

    /// <summary>
    /// Anchors each floating motion photo badge to the on-screen top-right corner
    /// of its image.
    /// </summary>
    private void UpdateMotionPhotoBadgePositions()
    {
        var hostSize = MotionPhotoBadgeHost.Bounds.Size;
        MotionPhotoView.UpdateBadgePosition(GetVisualTopRightCorner(MotionPhotoView), hostSize);
        SecondaryMotionPhotoView.UpdateBadgePosition(GetVisualTopRightCorner(SecondaryMotionPhotoView), hostSize);
    }

    /// <summary>
    /// Maps the corners of the source into MainPanel coordinates and returns its
    /// on-screen top-right corner. Rotations are multiples of 90° and flipping keeps
    /// the rectangle axis-aligned, so that is simply (max X, min Y). Returns null
    /// when the source cannot currently be mapped.
    /// </summary>
    private Point? GetVisualTopRightCorner(Control source)
    {
        if (!source.IsVisible || source.Bounds is not { Width: > 0, Height: > 0 })
        {
            return null;
        }

        var bounds = source.Bounds;
        if (source.TranslatePoint(bounds.TopLeft, MainPanel) is not { } topLeft ||
            source.TranslatePoint(bounds.TopRight, MainPanel) is not { } topRight ||
            source.TranslatePoint(bounds.BottomLeft, MainPanel) is not { } bottomLeft ||
            source.TranslatePoint(bounds.BottomRight, MainPanel) is not { } bottomRight)
        {
            return null;
        }

        var x = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
        var y = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
        return new Point(x, y);
    }

    /// <summary>Whether the current image is a playable motion photo.</summary>
    public bool IsMotionPhotoActive => MotionPhotoView.IsMotionPhotoActive || SecondaryMotionPhotoView.IsMotionPhotoActive;

    /// <summary>Stops motion photo playback. Returns true when playback was active.</summary>
    public bool StopMotionPhotoIfPlaying() => MotionPhotoView.StopIfPlaying() | SecondaryMotionPhotoView.StopIfPlaying();

    /// <summary>Starts, pauses or resumes motion photo playback.</summary>
    public void ToggleMotionPhotoPlayPause()
    {
        if (MotionPhotoView.IsPlaying || !SecondaryMotionPhotoView.IsMotionPhotoActive)
        {
            MotionPhotoView.TogglePlayPause();
        }
        else
        {
            SecondaryMotionPhotoView.TogglePlayPause();
        }
    }

    public void TriggerScalingModeUpdate(bool invalidate) =>
        ImageControlHelper.TriggerScalingModeUpdate(MainImage, invalidate);

    private void TouchMagnifyEvent(object? sender, PointerDeltaEventArgs e) =>
        ZoomPanControl.ZoomWithPointerWheelCore(e.Delta.Y > 0, e.GetPosition(this));

    private async ValueTask PreviewOnPointerWheelChanged(object? sender, PointerWheelEventArgs e) =>
        await PreviewOnPointerWheelChangedAsync(e).ConfigureAwait(false);

    private async ValueTask PreviewOnPointerWheelChangedAsync(PointerWheelEventArgs e)
    {
        if (GalleryView.IsPointerOver)
        {
            return;
        }

        // The wheel only works on the image viewer the active tab shows
        if (TopLevel.GetTopLevel(this) is not MainWindow
            {
                DataContext: MainWindowViewModel { WindowTabs.ActiveTab.CurrentValue.CurrentView.Value: ImageViewer }
            } mainWindow)
        {
            return;
        }

        e.Handled = await mainWindow.KeybindRouter.WheelAsync(Keybind.FromWheel(e.Delta.Y, e.KeyModifiers),
            e.Pointer.Type is PointerType.Touch, e.GetPosition(ZoomPanControl), this).ConfigureAwait(true);
    }

    /// <summary>
    /// Whether the shown image is taller than the scroll viewer, so there is something to scroll.
    /// </summary>
    public bool IsImageTallerThanViewer => ImageScrollViewer.Extent.Height > ImageScrollViewer.Viewport.Height;

    bool IZoomScrollTarget.CanScrollVertically =>
        ImageScrollViewer.VerticalScrollBarVisibility is ScrollBarVisibility.Visible or ScrollBarVisibility.Auto;

    void IZoomScrollTarget.ScrollLine(bool down)
    {
        if (down)
        {
            ImageScrollViewer.LineDown();
        }
        else
        {
            ImageScrollViewer.LineUp();
        }
    }

    void IZoomScrollTarget.ScrollToHome() => ImageScrollViewer.ScrollToHome();

    void IZoomScrollTarget.ScrollToEnd() => ImageScrollViewer.ScrollToEnd();

    void IZoomScrollTarget.ZoomAt(bool zoomIn, Point pointer) =>
        ZoomPanControl.ZoomWithPointerWheelCore(zoomIn, pointer);

    private void InitializeImageTransformer()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        ZoomPanControl.Initialize(ZoomPreview);
        
        if (DataContext is not TabViewModel tab)
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is MainWindow { ViewModel: { } vm })
        {
            vm.GlobalSettings.IsScrollingEnabled.Subscribe(isScrolling =>
            {
                ImageScrollViewer.VerticalScrollBarVisibility = isScrolling ?
                    ScrollBarVisibility.Visible : ScrollBarVisibility.Disabled;
            }, DebugHelper.LogError(nameof(ImageViewer), nameof(InitializeImageTransformer))).AddTo(ref _disposables);
        }
        
        // Correspond to change when index clicked on track
        Observable.FromEvent<EventHandler<int>, int>(
                handler => (sender, index) => handler(index),
                handler => HoverBar.ProgressBar.ClickedOnTrack += handler,
                handler => HoverBar.ProgressBar.ClickedOnTrack -= handler)
            .SubscribeAwait(async (x, _) =>
            {
                await tab.ImageIterator.SkipToIndexAsync(x, tab.GetTabCancellation()).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(ImageViewer), nameof(InitializeImageTransformer)), AwaitOperation.Drop)
            .AddTo(ref _disposables);
        // Correspond to change when index dragged on track
        // wait for a 25ms pause in changes (debounce), and then emit the last value.
        Observable.FromEvent<EventHandler<int>, int>(
                handler => (sender, index) => handler(index),
                handler => HoverBar.ProgressBar.DraggedOnTrack += handler,
                handler => HoverBar.ProgressBar.DraggedOnTrack -= handler)
            .Debounce(TimeSpan.FromMilliseconds(25)) // Debounce handles rapid events during drag
            .SubscribeAwait(async (x, _) =>
            {
                await tab.ImageIterator.SkipToIndexAsync(x, tab.GetTabCancellation()).ConfigureAwait(false);
            },DebugHelper.LogError(nameof(ImageViewer), nameof(InitializeImageTransformer)), AwaitOperation.Drop)
            .AddTo(ref _disposables);
        
        tab.Orientation.Current
            .Select(static orientation => orientation.IsFlipped)
            .DistinctUntilChanged()
            .Subscribe(_ => ShowFlip(),
            DebugHelper.LogError(nameof(ImageViewer), nameof(ShowFlip)))
            .AddTo(ref _disposables);
        _disposables.Add(new HoverFadeButtonHandler(GalleryShortcut, GalleryShortcut.InnerButton));
    }

    #region Zoom

    /// <summary>
    /// Reports the control's scale to the tab as the Zoom Factor (ADR-0016), shows the zoom percentage popup when that
    /// setting asks, and keeps the zoom preview above the hover bar.
    /// </summary>
    private void OnScaleChanged(double scale)
    {
        _tab.Zoom.SetFactor(scale);
        if (Settings.Zoom.IsShowingZoomPercentagePopup)
        {
            var message = StringExtensions.CombineWithPercentage(_tab.Zoom.Level.CurrentValue);
            _ = TooltipHelper.ShowTooltipMessageAsync(message, true,
                TopLevel.GetTopLevel(this) as MainWindow, TimeSpan.FromSeconds(1));
        }

        ZoomPreview.Margin = HoverBar.Opacity > 0
            ? new Thickness(0, 0, 25, HoverBar.Bounds.Height / 2 + 25)
            : new Thickness(0, 0, 25, 25);
    }

    /// <summary>A view taken while this viewer is not shown, which it shows when it is loaded again.</summary>
    internal ZoomView? PendingZoomView { get; private set; }

    /// <summary>
    /// How far this viewer is zoomed and where it looks, to hand to the tab's crop (ADR-0027).
    /// </summary>
    public ZoomView GetZoomView() =>
        PendingZoomView ?? ZoomView.From(ZoomPanControl.Scale, ZoomPanControl.ViewCentre, ImageRect());

    /// <summary>
    /// Shows <paramref name="view"/>, at once when this viewer is shown, else when it is loaded again.
    /// </summary>
    public void SetZoomView(ZoomView view)
    {
        if (IsLoaded)
        {
            ShowZoomView(view);
            return;
        }

        PendingZoomView = view;
    }

    private void ShowZoomView(ZoomView view) =>
        ZoomPanControl.SetView(view.Scale, view.ToChildCentre(ImageRect()));

    // Where the fitted image lies in the zoom control's child, or nothing before the first layout
    private Rect ImageRect() =>
        ZoomPanControl.Child is { } child && MainBorder.TranslatePoint(default, child) is { } origin
            ? new Rect(origin, MainBorder.Bounds.Size)
            : default;

    /// <inheritdoc cref="ZoomPanControl.ZoomIn"/>
    public void ZoomIn() =>
        ZoomPanControl.ZoomIn();

    /// <inheritdoc cref="ZoomPanControl.ZoomOut"/>
    public void ZoomOut() =>
        ZoomPanControl.ZoomOut();

    /// <inheritdoc cref="ZoomPanControl.ResetZoom(bool)"/>
    public void ResetZoom(bool enableAnimations = true) =>
        ZoomPanControl.ResetZoom(enableAnimations);
    
    /// <inheritdoc cref="ZoomPanControl.ResetZoomSlim"/>
    public void ResetZoomSlim() =>
        ZoomPanControl.ResetZoomSlim();
    
    #endregion

    #region Image Transformation

    /// <summary>
    /// Whether an image is shown, so that turning or flipping it means something.
    /// </summary>
    public bool HasImage => MainImage.Source is not null;

    /// <summary>
    /// Animates the next change of the tab's flip, as the Flip Action does; resetting it on a new image does not.
    /// </summary>
    public void AnimateNextFlip() => _animatesNextFlip = true;

    /// <summary>
    /// Mirrors the image as its tab's Orientation asks. The rotation is bound in the view instead.
    /// </summary>
    private void ShowFlip()
    {
        // Posted, never invoked: a tab resets its Orientation on the thread that shows its next image, and only the
        // picture waits for this, not that thread
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(ShowFlip, DispatcherPriority.Send);
            return;
        }

        // Read when it runs, not when it was posted: a flip on the UI thread and a reset on the loading thread may
        // land in either order, and the one written last is the one to show
        var scaleX = _tab.Orientation.Current.CurrentValue.IsFlipped ? -1d : 1d;

        if (_flipTransform is null)
        {
            if (scaleX is 1)
            {
                return;
            }

            _flipTransform = new ScaleTransform();
            MainTransform.RenderTransform = _flipTransform;
        }

        _flipTransform.Transitions = _animatesNextFlip
            ? [new DoubleTransition { Property = ScaleTransform.ScaleXProperty, Duration = TimeSpan.FromSeconds(.2) }]
            : null;
        _animatesNextFlip = false;
        _flipTransform.ScaleX = scaleX;
    }
        
    #endregion

    public void Dispose()
    {
        ZoomPanControl.ScaleChanged -= OnScaleChanged;
        RemoveHandler(PointerWheelChangedEvent, PreviewOnPointerWheelChanged);
        RemoveHandler(PointerTouchPadGestureMagnifyEvent, TouchMagnifyEvent);
        RemoveHandler(PinchEvent, TouchMagnifyEvent);
        MainPanel.LayoutUpdated -= OnMotionPhotoBadgeAnchorChanged;
        MotionPhotoView.LayoutUpdated -= OnMotionPhotoBadgeAnchorChanged;
        SecondaryMotionPhotoView.LayoutUpdated -= OnMotionPhotoBadgeAnchorChanged;
        ImageScrollViewer.ScrollChanged -= OnMotionPhotoBadgeScrollChanged;
        MotionPhotoView.PlaybackStarted -= OnMotionPhotoPlaybackStarted;
        MotionPhotoView.PlaybackStopped -= OnMotionPhotoPlaybackStopped;
        MotionPhotoView.FirstFrameShown -= OnMotionPhotoFirstFrameShown;
        MotionPhotoView.Dispose();
        SecondaryMotionPhotoView.PlaybackStarted -= OnMotionPhotoPlaybackStarted;
        SecondaryMotionPhotoView.PlaybackStopped -= OnMotionPhotoPlaybackStopped;
        SecondaryMotionPhotoView.FirstFrameShown -= OnMotionPhotoFirstFrameShown;
        SecondaryMotionPhotoView.Dispose();
        GalleryView.Dispose();
        _disposables.Dispose();
        HoverBar.Dispose();
        
        GC.SuppressFinalize(this);
    }
}
