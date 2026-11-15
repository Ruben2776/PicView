using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.CustomControls.Zoom;
using PicView.Avalonia.ImageTransformations;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Core.Config;
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
    private ScaleTransform? _syncedLeftFlipTransform;
    private ScaleTransform? _syncedRightFlipTransform;
    private bool _animatesNextFlip;
    private readonly TabViewModel _tab;
    private DispatcherTimer? _blinkTimer;
    private bool _blinkShowCompare = true;
    private bool _isDraggingSplit;
    private bool _isSyncingZoom;
    private Point _alignA1;
    private Point _alignB1;
    private Point _alignA2;
    private Point _alignB2;
    private static readonly Cursor CrossCursor = new(StandardCursorType.Cross);
    private static readonly Geometry SplitVerticalIcon =
        Geometry.Parse("M5,8 L2,11 L5,14 M11,8 L14,11 L11,14 M2,11 L14,11");
    private static readonly Geometry SplitHorizontalIcon =
        Geometry.Parse("M8,5 L11,2 L14,5 M8,11 L11,14 L14,11 M11,2 L11,14");

    /// <param name="tab">The tab this viewer shows, which is not always the active one: a viewer is also created for a
    /// tab that opens in the background.</param>
    public ImageViewer(TabViewModel tab)
    {
        _tab = tab;
        DataContext = tab;
        InitializeComponent();
        InitializeComparison();
        ZoomPanControl.ScaleChanged += OnScaleChanged;
        GalleryView.Initialize(tab);
        ImageControlHelper.TriggerScalingModeUpdate(MainImage, true);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
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
        // While comparing, no motion photo plays: a clip would cover the blended pair
        var comparing = Settings.ImageScaling.ShowComparison;
        MotionPhotoView.OnImageChanged(comparing ? null : tabViewModel.Model);
        SecondaryMotionPhotoView.OnImageChanged(comparing ? null : tabViewModel.SecondaryModel);
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

        var pointerControl = _tab.ParentWindowContext.GlobalSettings.IsSideBySideSyncedActive.Value
            ? (SyncedRightZoom.IsPointerOver ? SyncedRightZoom : SyncedLeftZoom)
            : ZoomPanControl;
        e.Handled = await mainWindow.KeybindRouter.WheelAsync(Keybind.FromWheel(e.Delta.Y, e.KeyModifiers),
            e.Pointer.Type is PointerType.Touch, e.GetPosition(pointerControl), this).ConfigureAwait(true);
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

    void IZoomScrollTarget.ZoomAt(bool zoomIn, Point pointer)
    {
        if (_tab.ParentWindowContext.GlobalSettings.IsSideBySideSyncedActive.Value)
        {
            var target = SyncedRightZoom.IsPointerOver ? SyncedRightZoom : SyncedLeftZoom;
            target.ZoomWithPointerWheelCore(zoomIn, pointer);
            return;
        }

        ZoomPanControl.ZoomWithPointerWheelCore(zoomIn, pointer);
    }

    private void InitializeImageTransformer()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        ZoomPanControl.Initialize(ZoomPreview);
        SyncedLeftZoom.Initialize();
        SyncedRightZoom.Initialize();
        
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
        if (_tab.ParentWindowContext.GlobalSettings.IsSideBySideSyncedActive.Value)
        {
            return;
        }

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

    /// <summary>
    /// How far this viewer is zoomed and where it looks, to hand to the tab's crop (ADR-0027).
    /// </summary>
    public ZoomView GetZoomView() =>
        ZoomView.From(ZoomPanControl.Scale, ZoomPanControl.ViewCenter, ImageRect());

    // Where the fitted image lies in the zoom control's child, or nothing before the first layout
    private Rect ImageRect() =>
        ZoomPanControl.Child is { } child && MainBorder.TranslatePoint(default, child) is { } origin
            ? new Rect(origin, MainBorder.Bounds.Size)
            : default;

    /// <inheritdoc cref="CustomControls.Zoom.ZoomPanControl.ZoomIn"/>
    public void ZoomIn()
    {
        if (_tab.ParentWindowContext.GlobalSettings.IsSideBySideSyncedActive.Value)
        {
            SyncedLeftZoom.ZoomIn();
            return;
        }

        ZoomPanControl.ZoomIn();
    }

    /// <inheritdoc cref="ZoomPanControl.ZoomOut"/>
    public void ZoomOut()
    {
        if (_tab.ParentWindowContext.GlobalSettings.IsSideBySideSyncedActive.Value)
        {
            SyncedLeftZoom.ZoomOut();
            return;
        }

        ZoomPanControl.ZoomOut();
    }

    /// <inheritdoc cref="ZoomPanControl.ResetZoom(bool)"/>
    public void ResetZoom(bool enableAnimations = true)
    {
        if (_tab.ParentWindowContext.GlobalSettings.IsSideBySideSyncedActive.Value)
        {
            SyncedLeftZoom.ResetZoom(enableAnimations);
            SyncedRightZoom.ResetZoom(enableAnimations);
            _tab.Zoom.SetFactor(1.0);
            return;
        }

        ZoomPanControl.ResetZoom(enableAnimations);
        _tab.Zoom.SetFactor(1.0);
    }

    /// <inheritdoc cref="ZoomPanControl.ResetZoomSlim"/>
    public void ResetZoomSlim()
    {
        if (_tab.ParentWindowContext.GlobalSettings.IsSideBySideSyncedActive.Value)
        {
            SyncedLeftZoom.ResetZoomSlim();
            SyncedRightZoom.ResetZoomSlim();
            _tab.Zoom.SetFactor(1.0);
            return;
        }

        ZoomPanControl.ResetZoomSlim();
    }
    
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
        var animate = _animatesNextFlip;
        _animatesNextFlip = false;

        ApplyFlipTransform(MainTransform, ref _flipTransform, scaleX, animate);
        ApplyFlipTransform(SyncedLeftTransform, ref _syncedLeftFlipTransform, scaleX, animate);
        ApplyFlipTransform(SyncedRightTransform, ref _syncedRightFlipTransform, scaleX, animate);
    }

    private static void ApplyFlipTransform(LayoutTransformControl target, ref ScaleTransform? transform, double scaleX, bool animate)
    {
        if (transform is null)
        {
            if (scaleX is 1)
            {
                return;
            }

            transform = new ScaleTransform();
            target.RenderTransform = transform;
        }

        transform.Transitions = animate
            ? [new DoubleTransition { Property = ScaleTransform.ScaleXProperty, Duration = TimeSpan.FromSeconds(.2) }]
            : null;
        transform.ScaleX = scaleX;
    }
        
    #endregion

    #region Comparison Modes

    private void InitializeComparison()
    {
        var gs = _tab.ParentWindowContext.GlobalSettings;

        gs.IsComparing.Subscribe(_ => ApplyComparisonVisuals()).AddTo(ref _disposables);
        gs.ComparisonMode.Subscribe(_ => ApplyComparisonVisuals()).AddTo(ref _disposables);
        gs.ComparisonOverlayOpacity.Subscribe(_ => ApplyComparisonVisuals()).AddTo(ref _disposables);
        gs.ComparisonSplitRatio.Subscribe(_ => UpdateSplitClipAndOverlay()).AddTo(ref _disposables);
        gs.IsComparisonSplitHorizontal.Subscribe(_ => UpdateSplitClipAndOverlay()).AddTo(ref _disposables);
        gs.IsComparisonBlinkFast.Subscribe(_ => UpdateBlinkTimer()).AddTo(ref _disposables);
        gs.ComparisonBlinkInterval.Subscribe(_ => UpdateBlinkTimer()).AddTo(ref _disposables);
        _tab.ParentWindowContext.WindowTabs.ActiveTab.Subscribe(_ => UpdateBlinkTimer()).AddTo(ref _disposables);
        gs.IsComparisonStretchFill.Subscribe(_ => ApplyComparisonVisuals()).AddTo(ref _disposables);
        gs.ShowHoverBar.Subscribe(_ => UpdateCompareBarMargin()).AddTo(ref _disposables);
        _tab.IsComparisonSwapped.Subscribe(_ => UpdateComparisonBadges()).AddTo(ref _disposables);
        _tab.DisplayPrimaryFileInfo.Subscribe(_ => UpdateComparisonBadges()).AddTo(ref _disposables);
        _tab.DisplaySecondaryFileInfo.Subscribe(_ => UpdateComparisonBadges()).AddTo(ref _disposables);
        _tab.HasAlignment.Subscribe(has =>
        {
            if (!has)
            {
                CompareImage.RenderTransform = null;
            }
        }).AddTo(ref _disposables);
        _tab.IsAligning.Subscribe(aligning =>
        {
            CompareHostGrid.Cursor = aligning ? CrossCursor : null;
            ZoomPanControl.SuspendPanning = aligning;
            AlignMarkersCanvas.Children.Clear();
            if (aligning && _tab.HasAlignment.Value)
            {
                CompareImage.RenderTransform = null;
                _tab.HasAlignment.Value = false;
            }
        }).AddTo(ref _disposables);
        gs.IsSideBySideSyncedActive.AsObservable().Subscribe(active =>
        {
            if (active)
            {
                SyncedLeftZoom.ResetZoom(false);
                SyncedRightZoom.ResetZoom(false);
                _tab.Zoom.SetFactor(1.0);
            }
            else
            {
                HideSyncedCrosshairs();
                _tab.Zoom.SetFactor(ZoomPanControl.Scale);
            }
        }).AddTo(ref _disposables);

        SyncedLeftZoom.ViewChanged += OnSyncedLeftViewChanged;
        SyncedRightZoom.ViewChanged += OnSyncedRightViewChanged;
        SyncedLeftZoom.ScaleChanged += OnSyncedScaleChanged;
        SyncedLeftZoom.PointerMoved += OnSyncedLeftPointerMoved;
        SyncedLeftZoom.PointerExited += OnSyncedPointerExited;
        SyncedRightZoom.PointerMoved += OnSyncedRightPointerMoved;
        SyncedRightZoom.PointerExited += OnSyncedPointerExited;

        CompareHostGrid.SizeChanged += OnCompareHostSizeChanged;
        CompareHostGrid.PointerPressed += OnCompareHostPointerPressed;
        ZoomPanControl.ViewChanged += OnMainZoomViewChanged;
        SplitOverlay.PointerPressed += OnSplitPointerPressed;
        SplitOverlay.PointerMoved += OnSplitPointerMoved;
        SplitOverlay.PointerReleased += OnSplitPointerReleased;
        SplitOverlay.PointerCaptureLost += OnSplitPointerCaptureLost;
        SplitHandle.KeyDown += OnSplitHandleKeyDown;
        HoverBar.SizeChanged += OnHoverBarSizeChanged;
        MainPanel.PointerMoved += OnMainPanelPointerMoved;

        UpdateCompareBarMargin();
        UpdateComparisonBadges();
        ApplyComparisonVisuals();
    }

    private void OnCompareHostSizeChanged(object? sender, SizeChangedEventArgs e) =>
        UpdateSplitClipAndOverlay();

    private void OnMainZoomViewChanged() =>
        UpdateSplitClipAndOverlay();

    private void ApplyComparisonVisuals()
    {
        var gs = _tab.ParentWindowContext.GlobalSettings;
        CompareImage.Stretch = gs.IsComparisonStretchFill.CurrentValue ? Stretch.Fill : Stretch.Uniform;
        if (!gs.IsComparing.CurrentValue)
        {
            StopBlinkTimer();
            CompareImage.BlendMode = BitmapBlendingMode.Difference;
            CompareImage.Opacity = 1.0;
            CompareImage.Clip = null;
            return;
        }

        switch (gs.ComparisonMode.CurrentValue)
        {
            case ComparisonMode.Overlay:
                StopBlinkTimer();
                CompareImage.BlendMode = BitmapBlendingMode.Unspecified;
                CompareImage.Opacity = Math.Clamp(gs.ComparisonOverlayOpacity.CurrentValue, 0.0, 1.0);
                CompareImage.Clip = null;
                break;

            case ComparisonMode.Split:
                StopBlinkTimer();
                CompareImage.BlendMode = BitmapBlendingMode.Unspecified;
                CompareImage.Opacity = 1.0;
                UpdateSplitClipAndOverlay();
                break;

            case ComparisonMode.Blink:
                CompareImage.BlendMode = BitmapBlendingMode.Unspecified;
                CompareImage.Clip = null;
                UpdateBlinkTimer();
                break;

            case ComparisonMode.Difference:
            default:
                StopBlinkTimer();
                CompareImage.BlendMode = BitmapBlendingMode.Difference;
                CompareImage.Opacity = 1.0;
                CompareImage.Clip = null;
                break;
        }
    }

    private void UpdateSplitClipAndOverlay()
    {
        var gs = _tab.ParentWindowContext.GlobalSettings;
        if (!gs.IsComparing.CurrentValue || gs.ComparisonMode.CurrentValue != ComparisonMode.Split)
        {
            CompareImage.Clip = null;
            return;
        }

        var bounds = CompareHostGrid.Bounds;
        var w = bounds.Width;
        var h = bounds.Height;
        if (w <= 0 || h <= 0)
        {
            return;
        }

        var ratio = Math.Clamp(gs.ComparisonSplitRatio.CurrentValue, 0.0, 1.0);
        var isHorizontal = gs.IsComparisonSplitHorizontal.CurrentValue;

        if (isHorizontal)
        {
            var splitY = h * ratio;
            CompareImage.Clip = new RectangleGeometry(new Rect(0, 0, w, splitY));

            var screenPos = CompareHostGrid.TranslatePoint(new Point(w * 0.5, splitY), MainPanel);
            var lineY = screenPos?.Y ?? splitY;
            var handleX = screenPos?.X ?? w * 0.5;
            var mainH = MainPanel.Bounds.Height > 0 ? MainPanel.Bounds.Height : h;
            var mainW = MainPanel.Bounds.Width > 0 ? MainPanel.Bounds.Width : w;

            SplitDividerLine.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
            SplitDividerLine.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top;
            SplitDividerLine.Width = double.NaN;
            SplitDividerLine.Height = 2;
            SplitDividerLine.Margin = new Thickness(0, Math.Clamp(lineY - 1, 0, Math.Max(0, mainH - 2)), 0, 0);

            SplitDividerHitStrip.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
            SplitDividerHitStrip.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top;
            SplitDividerHitStrip.Width = double.NaN;
            SplitDividerHitStrip.Height = 16;
            SplitDividerHitStrip.Margin = new Thickness(0, Math.Clamp(lineY - 8, 0, Math.Max(0, mainH - 16)), 0, 0);
            SplitDividerHitStrip.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);

            SplitHandle.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left;
            SplitHandle.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top;
            SplitHandle.Margin = new Thickness(
                Math.Clamp(handleX - 14, 0, Math.Max(0, mainW - 28)),
                Math.Clamp(lineY - 14, 0, Math.Max(0, mainH - 28)),
                0, 0);
            SplitHandle.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
            SplitHandleIcon.Data = SplitHorizontalIcon;
        }
        else
        {
            var splitX = w * ratio;
            CompareImage.Clip = new RectangleGeometry(new Rect(0, 0, splitX, h));

            var screenPos = CompareHostGrid.TranslatePoint(new Point(splitX, h * 0.5), MainPanel);
            var lineX = screenPos?.X ?? splitX;
            var handleY = screenPos?.Y ?? h * 0.5;
            var mainH = MainPanel.Bounds.Height > 0 ? MainPanel.Bounds.Height : h;
            var mainW = MainPanel.Bounds.Width > 0 ? MainPanel.Bounds.Width : w;

            SplitDividerLine.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left;
            SplitDividerLine.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;
            SplitDividerLine.Width = 2;
            SplitDividerLine.Height = double.NaN;
            SplitDividerLine.Margin = new Thickness(Math.Clamp(lineX - 1, 0, Math.Max(0, mainW - 2)), 0, 0, 0);

            SplitDividerHitStrip.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left;
            SplitDividerHitStrip.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;
            SplitDividerHitStrip.Width = 16;
            SplitDividerHitStrip.Height = double.NaN;
            SplitDividerHitStrip.Margin = new Thickness(Math.Clamp(lineX - 8, 0, Math.Max(0, mainW - 16)), 0, 0, 0);
            SplitDividerHitStrip.Cursor = new Cursor(StandardCursorType.SizeWestEast);

            SplitHandle.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left;
            SplitHandle.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top;
            SplitHandle.Margin = new Thickness(
                Math.Clamp(lineX - 14, 0, Math.Max(0, mainW - 28)),
                Math.Clamp(handleY - 14, 0, Math.Max(0, mainH - 28)),
                0, 0);
            SplitHandle.Cursor = new Cursor(StandardCursorType.SizeWestEast);
            SplitHandleIcon.Data = SplitVerticalIcon;
        }
    }

    private void OnSplitPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var gs = _tab.ParentWindowContext.GlobalSettings;
        if (!gs.IsSplitMode.Value || !e.GetCurrentPoint(SplitOverlay).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.ClickCount == 2 && SplitHandle.Bounds.Inflate(6).Contains(e.GetPosition(SplitOverlay)))
        {
            gs.ComparisonSplitRatio.Value = 0.5;
            e.Handled = true;
            return;
        }

        _isDraggingSplit = true;
        e.Pointer.Capture(SplitOverlay);
        UpdateSplitFromPointer(e.GetPosition(MainPanel));
        e.Handled = true;
    }

    private void OnSplitPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDraggingSplit)
        {
            return;
        }

        UpdateSplitFromPointer(e.GetPosition(MainPanel));
        e.Handled = true;
    }

    private void OnSplitPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDraggingSplit)
        {
            return;
        }

        _isDraggingSplit = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnSplitPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) =>
        _isDraggingSplit = false;

    private void UpdateSplitFromPointer(Point mainPanelPos)
    {
        var hostBounds = CompareHostGrid.Bounds;
        if (hostBounds.Width <= 0 || hostBounds.Height <= 0)
        {
            return;
        }

        var mapped = MainPanel.TranslatePoint(mainPanelPos, CompareHostGrid) ?? mainPanelPos;
        var gs = _tab.ParentWindowContext.GlobalSettings;
        var ratio = gs.IsComparisonSplitHorizontal.CurrentValue
            ? mapped.Y / hostBounds.Height
            : mapped.X / hostBounds.Width;
        gs.ComparisonSplitRatio.Value = Math.Clamp(ratio, 0.0, 1.0);
    }

    private void UpdateBlinkTimer()
    {
        var gs = _tab.ParentWindowContext.GlobalSettings;
        var isTabActive = _tab.IsSelected || ReferenceEquals(_tab.ParentWindowContext.WindowTabs.ActiveTab.CurrentValue, _tab);
        if (!isTabActive || !gs.IsComparing.CurrentValue || gs.ComparisonMode.CurrentValue != ComparisonMode.Blink)
        {
            StopBlinkTimer();
            return;
        }

        var seconds = gs.IsComparisonBlinkFast.CurrentValue
            ? 0.3
            : Math.Clamp(gs.ComparisonBlinkInterval.CurrentValue, 0.1, 5.0);

        if (_blinkTimer is null)
        {
            _blinkTimer = new DispatcherTimer();
            _blinkTimer.Tick += OnBlinkTick;
        }

        _blinkTimer.Interval = TimeSpan.FromSeconds(seconds);
        if (!_blinkTimer.IsEnabled)
        {
            _blinkShowCompare = true;
            CompareImage.Opacity = 1.0;
            _blinkTimer.Start();
        }
    }

    private void OnBlinkTick(object? sender, EventArgs e)
    {
        if (!_tab.IsSelected)
        {
            return;
        }

        _blinkShowCompare = !_blinkShowCompare;
        CompareImage.Opacity = _blinkShowCompare ? 1.0 : 0.0;
    }

    private void StopBlinkTimer()
    {
        if (_blinkTimer is { IsEnabled: true })
        {
            _blinkTimer.Stop();
        }
        _blinkShowCompare = true;
    }

    private void OnSyncedLeftViewChanged() =>
        SyncSyncedZoom(SyncedLeftZoom, SyncedLeftImage, SyncedRightZoom, SyncedRightImage);

    private void OnSyncedRightViewChanged()
    {
        SyncSyncedZoom(SyncedRightZoom, SyncedRightImage, SyncedLeftZoom, SyncedLeftImage);
        if (!_isSyncingZoom)
        {
            _tab.Zoom.SetFactor(SyncedRightZoom.Scale);
        }
    }

    private void SyncSyncedZoom(ZoomPanControl sourceZoom, PicBox sourceImage, ZoomPanControl targetZoom, PicBox targetImage)
    {
        if (_isSyncingZoom)
        {
            return;
        }

        _isSyncingZoom = true;
        try
        {
            var srcRect = GetUniformImageRect(sourceImage, sourceZoom);
            var dstRect = GetUniformImageRect(targetImage, targetZoom);
            if (srcRect.Width > 0 && srcRect.Height > 0 && dstRect.Width > 0 && dstRect.Height > 0)
            {
                var viewCenter = sourceZoom.ViewCenter;
                var u = (viewCenter.X - srcRect.X) / srcRect.Width;
                var v = (viewCenter.Y - srcRect.Y) / srcRect.Height;
                var targetCenter = new Point(dstRect.X + u * dstRect.Width, dstRect.Y + v * dstRect.Height);
                targetZoom.ShowChildAt(sourceZoom.Scale, targetCenter);
            }
            else
            {
                targetZoom.SyncTransformFrom(sourceZoom);
            }
        }
        finally
        {
            _isSyncingZoom = false;
        }
    }

    private static Rect GetUniformImageRect(PicBox picBox, ZoomPanControl zoom)
    {
        if (picBox.Bounds.Width > 0 && picBox.Bounds.Height > 0)
        {
            return picBox.Bounds;
        }

        var containerBounds = (zoom.Child as Control)?.Bounds ?? zoom.Bounds;
        if (containerBounds.Width <= 0 || containerBounds.Height <= 0)
        {
            return default;
        }

        var sourceSize = picBox.Source is IImage img ? img.Size : default;
        if (sourceSize.Width <= 0 || sourceSize.Height <= 0)
        {
            return new Rect(0, 0, containerBounds.Width, containerBounds.Height);
        }

        var ratio = Math.Min(containerBounds.Width / sourceSize.Width, containerBounds.Height / sourceSize.Height);
        var w = sourceSize.Width * ratio;
        var h = sourceSize.Height * ratio;
        return new Rect((containerBounds.Width - w) * 0.5, (containerBounds.Height - h) * 0.5, w, h);
    }

    private void OnSyncedScaleChanged(double scale)
    {
        if (_tab.ParentWindowContext.GlobalSettings.IsSideBySideSyncedActive.Value)
        {
            _tab.Zoom.SetFactor(scale);
        }
    }

    private void UpdateComparisonBadges()
    {
        var swapped = _tab.IsComparisonSwapped.CurrentValue;
        var leftLabel = swapped ? "B" : "A";
        var rightLabel = swapped ? "A" : "B";
        var leftName = _tab.DisplayPrimaryFileInfo.CurrentValue?.Name;
        var rightName = _tab.DisplaySecondaryFileInfo.CurrentValue?.Name;
        SyncedLeftBadgeText.Text = string.IsNullOrEmpty(leftName) ? leftLabel : $"{leftLabel} · {leftName}";
        SyncedRightBadgeText.Text = string.IsNullOrEmpty(rightName) ? rightLabel : $"{rightLabel} · {rightName}";
    }

    private void OnMainPanelPointerMoved(object? sender, PointerEventArgs e) =>
        CompareBar.NotifyUserActivity();

    private void OnHoverBarSizeChanged(object? sender, SizeChangedEventArgs e) =>
        UpdateCompareBarMargin();

    private void UpdateCompareBarMargin()
    {
        var hoverVisible = HoverBar.IsVisible && HoverBar.Bounds.Height > 0;
        var bottom = hoverVisible
            ? HoverBar.Bounds.Height + HoverBar.Margin.Bottom + 12d
            : 18d;
        CompareBar.Margin = new Thickness(0, 0, 0, bottom);
    }

    private void OnCompareHostPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_tab.IsAligning.Value || !e.GetCurrentPoint(CompareHostGrid).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var pt = e.GetPosition(CompareHostGrid);
        switch (_tab.AlignmentStep.Value)
        {
            case 1:
                _alignA1 = pt;
                AddAlignMarker(pt, "A1", isSecondary: false);
                _tab.AlignmentStep.Value = 2;
                break;
            case 2:
                _alignB1 = pt;
                AddAlignMarker(pt, "B1", isSecondary: true);
                _tab.AlignmentStep.Value = 3;
                break;
            case 3:
                _alignA2 = pt;
                AddAlignMarker(pt, "A2", isSecondary: false);
                _tab.AlignmentStep.Value = 4;
                break;
            case 4:
                _alignB2 = pt;
                if (PicView.Core.Comparison.SimilarityTransform.TryCompute(
                        (_alignA1.X, _alignA1.Y),
                        (_alignA2.X, _alignA2.Y),
                        (_alignB1.X, _alignB1.Y),
                        (_alignB2.X, _alignB2.Y),
                        out var m))
                {
                    CompareImage.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
                    CompareImage.RenderTransform = new MatrixTransform(
                        new Matrix(m.M11, m.M12, m.M21, m.M22, m.OffsetX, m.OffsetY));
                    _tab.HasAlignment.Value = true;
                }

                AlignMarkersCanvas.Children.Clear();
                _tab.IsAligning.Value = false;
                _tab.AlignmentStep.Value = 0;
                break;
        }

        e.Handled = true;
    }

    private void AddAlignMarker(Point pt, string label, bool isSecondary)
    {
        var fill = isSecondary ? Color.Parse("#FFEC4899") : Color.Parse("#FF38BDF8");
        var marker = new Border
        {
            Width = 20,
            Height = 20,
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(fill, 0.88),
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1.5),
            Child = new TextBlock
            {
                Text = label,
                FontSize = 9,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
            }
        };
        Canvas.SetLeft(marker, pt.X - 10);
        Canvas.SetTop(marker, pt.Y - 10);
        AlignMarkersCanvas.Children.Add(marker);
    }

    private void OnSplitHandleKeyDown(object? sender, KeyEventArgs e)
    {
        var gs = _tab.ParentWindowContext.GlobalSettings;
        if (!gs.IsSplitMode.Value)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Left:
            case Key.Up:
                gs.ComparisonSplitRatio.Value = Math.Clamp(gs.ComparisonSplitRatio.CurrentValue - 0.01, 0.0, 1.0);
                e.Handled = true;
                break;
            case Key.Right:
            case Key.Down:
                gs.ComparisonSplitRatio.Value = Math.Clamp(gs.ComparisonSplitRatio.CurrentValue + 0.01, 0.0, 1.0);
                e.Handled = true;
                break;
            case Key.Home:
                gs.ComparisonSplitRatio.Value = 0.5;
                e.Handled = true;
                break;
        }
    }

    private void OnSyncedLeftPointerMoved(object? sender, PointerEventArgs e) =>
        UpdateSyncedCrosshair(e, SyncedLeftZoom, SyncedLeftImage, SyncedRightZoom, SyncedRightImage, SyncedRightCrosshair, SyncedLeftCrosshair);

    private void OnSyncedRightPointerMoved(object? sender, PointerEventArgs e) =>
        UpdateSyncedCrosshair(e, SyncedRightZoom, SyncedRightImage, SyncedLeftZoom, SyncedLeftImage, SyncedLeftCrosshair, SyncedRightCrosshair);

    private void OnSyncedPointerExited(object? sender, PointerEventArgs e) =>
        HideSyncedCrosshairs();

    private void HideSyncedCrosshairs()
    {
        SyncedLeftCrosshair.IsVisible = false;
        SyncedRightCrosshair.IsVisible = false;
    }

    private void UpdateSyncedCrosshair(
        PointerEventArgs e,
        ZoomPanControl sourceZoom,
        PicBox sourceImage,
        ZoomPanControl targetZoom,
        PicBox targetImage,
        global::Avalonia.Controls.Shapes.Path targetCrosshair,
        global::Avalonia.Controls.Shapes.Path sourceCrosshair)
    {
        if (!_tab.ParentWindowContext.GlobalSettings.IsSideBySideSyncedActive.Value)
        {
            HideSyncedCrosshairs();
            return;
        }

        sourceCrosshair.IsVisible = false;
        var srcVisual = (sourceZoom.Child as Visual) ?? sourceImage;
        var targetVisual = (targetZoom.Child as Visual) ?? targetImage;
        var srcPt = e.GetPosition(srcVisual);
        var srcRect = GetUniformImageRect(sourceImage, sourceZoom);
        var dstRect = GetUniformImageRect(targetImage, targetZoom);
        if (srcRect.Width <= 0 || srcRect.Height <= 0 || dstRect.Width <= 0 || dstRect.Height <= 0)
        {
            targetCrosshair.IsVisible = false;
            return;
        }

        var u = (srcPt.X - srcRect.X) / srcRect.Width;
        var v = (srcPt.Y - srcRect.Y) / srcRect.Height;
        if (u is < 0 or > 1 || v is < 0 or > 1)
        {
            targetCrosshair.IsVisible = false;
            return;
        }

        var dstImagePt = new Point(dstRect.X + u * dstRect.Width, dstRect.Y + v * dstRect.Height);
        if (targetCrosshair.Parent is Visual canvas && targetVisual.TranslatePoint(dstImagePt, canvas) is { } canvasPt)
        {
            Canvas.SetLeft(targetCrosshair, canvasPt.X - 8);
            Canvas.SetTop(targetCrosshair, canvasPt.Y - 8);
            targetCrosshair.IsVisible = true;
        }
        else
        {
            targetCrosshair.IsVisible = false;
        }
    }

    #endregion

    public void Dispose()
    {
        SyncedLeftZoom.ViewChanged -= OnSyncedLeftViewChanged;
        SyncedRightZoom.ViewChanged -= OnSyncedRightViewChanged;
        SyncedLeftZoom.ScaleChanged -= OnSyncedScaleChanged;
        SyncedLeftZoom.PointerMoved -= OnSyncedLeftPointerMoved;
        SyncedLeftZoom.PointerExited -= OnSyncedPointerExited;
        SyncedRightZoom.PointerMoved -= OnSyncedRightPointerMoved;
        SyncedRightZoom.PointerExited -= OnSyncedPointerExited;
        StopBlinkTimer();
        if (_blinkTimer is not null)
        {
            _blinkTimer.Tick -= OnBlinkTick;
            _blinkTimer = null;
        }
        CompareHostGrid.SizeChanged -= OnCompareHostSizeChanged;
        CompareHostGrid.PointerPressed -= OnCompareHostPointerPressed;
        ZoomPanControl.ViewChanged -= OnMainZoomViewChanged;
        SplitOverlay.PointerPressed -= OnSplitPointerPressed;
        SplitOverlay.PointerMoved -= OnSplitPointerMoved;
        SplitOverlay.PointerReleased -= OnSplitPointerReleased;
        SplitOverlay.PointerCaptureLost -= OnSplitPointerCaptureLost;
        SplitHandle.KeyDown -= OnSplitHandleKeyDown;
        HoverBar.SizeChanged -= OnHoverBarSizeChanged;
        MainPanel.PointerMoved -= OnMainPanelPointerMoved;
        CompareBar.Dispose();
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
