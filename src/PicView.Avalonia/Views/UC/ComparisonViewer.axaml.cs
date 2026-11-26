using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.CustomControls.Zoom;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.ImageTransformations;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Core.Comparison;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.ViewModels;
using R3;
using Path = Avalonia.Controls.Shapes.Path;

namespace PicView.Avalonia.Views.UC;

/// <summary>
/// Shows a tab's <see cref="ComparisonViewModel"/>: B stacked over A in the overlay, split, blink and difference
/// modes, A and B side by side, zoomed and panned together, in the synced mode, or A alone in Pixel Info, which
/// reads the pixel under the pointer. Its zoom is the tab's Zoom Factor.
/// </summary>
public partial class ComparisonViewer : UserControl, INavigableContent, IDisposable
{
    private static readonly Cursor CrossCursor = new(StandardCursorType.Cross);
    private static readonly Cursor SizeWestEastCursor = new(StandardCursorType.SizeWestEast);
    private static readonly Cursor SizeNorthSouthCursor = new(StandardCursorType.SizeNorthSouth);
    private static readonly Geometry SplitVerticalIcon =
        Geometry.Parse("M5,8 L2,11 L5,14 M11,8 L14,11 L11,14 M2,11 L14,11");
    private static readonly Geometry SplitHorizontalIcon =
        Geometry.Parse("M8,5 L11,2 L14,5 M8,11 L11,14 L14,11 M11,2 L11,14");
    private static readonly IBrush PrimaryMarkerFill = new SolidColorBrush(Color.Parse("#FF38BDF8"), 0.88);
    private static readonly IBrush SecondaryMarkerFill = new SolidColorBrush(Color.Parse("#FFEC4899"), 0.88);

    private readonly TabViewModel _tab;
    // The settings of the window this viewer is shown in: a viewer stays in one window, as its views are made again
    // when its tab moves to another
    private readonly WindowSettings? _windowSettings;
    private DisposableBag _disposables;
    private bool _isLoadedOnce;
    private bool _isAttached;
    private bool _isDisposed;
    private double? _pendingFactor;
    private DispatcherTimer? _blinkTimer;
    private bool _blinkShowsB = true;
    private bool _isDraggingSplit;
    private bool _isSyncingZoom;
    // The hover bar of the NavigableView this viewer is shown in, followed while attached
    private HoverBar? _hoverBar;

    /// <param name="tab">The tab this viewer shows, its DataContext, for its Orientation and the fitted size.</param>
    /// <param name="comparison">The comparison shown, which the bindings and this code read.</param>
    public ComparisonViewer(TabViewModel tab, ComparisonViewModel comparison)
    {
        _tab = tab;
        _windowSettings = tab.ParentWindowContext?.WindowSettings;
        Comparison = comparison;
        DataContext = tab;
        InitializeComponent();
        ImageB.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
        TriggerScalingModeUpdate(true);
        InitializeComparison();
        Loaded += OnLoaded;
    }

    public ComparisonViewModel Comparison { get; }

    /// <summary>Whether B is blinking, for tests.</summary>
    internal bool IsBlinking => _blinkTimer is { IsEnabled: true };

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (!_isLoadedOnce)
        {
            _isLoadedOnce = true;
            ZoomPanControl.Initialize(ZoomPreview);
            SyncedLeftZoom.Initialize();
            SyncedRightZoom.Initialize();
        }

        if (_pendingFactor is { } factor)
        {
            _pendingFactor = null;
            ApplyFactor(factor);
        }
    }

    private void InitializeComparison()
    {
        // The comparison changes on the thread that shows the tab's next pair, so each of these runs on the UI thread
        var c = Comparison;
        c.Mode.SubscribeOnUIThread(ApplyComparisonVisuals, LogError(nameof(ApplyComparisonVisuals)))
            .AddTo(ref _disposables);
        c.OverlayOpacity.SubscribeOnUIThread(ApplyComparisonVisuals, LogError(nameof(ApplyComparisonVisuals)))
            .AddTo(ref _disposables);
        c.IsStretchFill.SubscribeOnUIThread(ApplyComparisonVisuals, LogError(nameof(ApplyComparisonVisuals)))
            .AddTo(ref _disposables);
        c.SplitRatio.SubscribeOnUIThread(UpdateSplitClipAndOverlay, LogError(nameof(UpdateSplitClipAndOverlay)))
            .AddTo(ref _disposables);
        c.IsSplitHorizontal.SubscribeOnUIThread(UpdateSplitClipAndOverlay, LogError(nameof(UpdateSplitClipAndOverlay)))
            .AddTo(ref _disposables);
        c.IsBlinkFast.SubscribeOnUIThread(UpdateBlinkTimer, LogError(nameof(UpdateBlinkTimer)))
            .AddTo(ref _disposables);
        c.BlinkInterval.SubscribeOnUIThread(UpdateBlinkTimer, LogError(nameof(UpdateBlinkTimer)))
            .AddTo(ref _disposables);
        c.IsSwapped.AsObservable().SubscribeOnUIThread(UpdateComparisonBadges, LogError(nameof(UpdateComparisonBadges)))
            .AddTo(ref _disposables);
        c.FileInfoA.AsObservable().SubscribeOnUIThread(UpdateComparisonBadges, LogError(nameof(UpdateComparisonBadges)))
            .AddTo(ref _disposables);
        c.FileInfoB.AsObservable().SubscribeOnUIThread(UpdateComparisonBadges, LogError(nameof(UpdateComparisonBadges)))
            .AddTo(ref _disposables);

        // The scroll bar follows its window's scrolling, as the image viewer's does
        _windowSettings?.IsScrollingEnabled.SubscribeOnUIThread(ShowScrolling,
            LogError(nameof(ShowScrolling))).AddTo(ref _disposables);

        // Set with the alignment, and cleared with it
        c.Alignment.AsObservable().SubscribeOnUIThread(ShowAlignment, LogError(nameof(ShowAlignment)))
            .AddTo(ref _disposables);
        c.IsAligning.AsObservable().SubscribeOnUIThread(ShowAligning, LogError(nameof(ShowAligning)))
            .AddTo(ref _disposables);

        // Pixel Info reads the pixel again in the image A now shows, so a swap reads the same place in the other
        c.IsPixelInfo.AsObservable().SubscribeOnUIThread(ReadPixel, LogError(nameof(ReadPixel)))
            .AddTo(ref _disposables);
        c.ImageA.AsObservable().SubscribeOnUIThread(ReadPixel, LogError(nameof(ReadPixel)))
            .AddTo(ref _disposables);
        // A Quarter Turn or a flip moves the image under a still pointer
        _tab.Orientation.Degrees.AsObservable().Skip(1)
            .SubscribeOnUIThread(ReadPixelAfterLayout, LogError(nameof(ReadPixelAfterLayout)))
            .AddTo(ref _disposables);
        _tab.Orientation.ScaleX.AsObservable().Skip(1)
            .SubscribeOnUIThread(ReadPixelAfterLayout, LogError(nameof(ReadPixelAfterLayout)))
            .AddTo(ref _disposables);

        // Entering or leaving the synced layout in a shown viewer: the factor follows the layout now shown
        c.IsSynced.AsObservable().Skip(1).SubscribeOnUIThread(ShowSyncedFactor, LogError(nameof(ShowSyncedFactor)))
            .AddTo(ref _disposables);

        ZoomPanControl.ScaleChanged += OnScaleChanged;
        ZoomPanControl.ViewChanged += OnMainZoomViewChanged;
        SyncedLeftZoom.ScaleChanged += OnSyncedScaleChanged;
        SyncedLeftZoom.ViewChanged += OnSyncedLeftViewChanged;
        SyncedRightZoom.ViewChanged += OnSyncedRightViewChanged;
        SyncedLeftZoom.PointerMoved += OnSyncedLeftPointerMoved;
        SyncedLeftZoom.PointerExited += OnSyncedPointerExited;
        SyncedRightZoom.PointerMoved += OnSyncedRightPointerMoved;
        SyncedRightZoom.PointerExited += OnSyncedPointerExited;

        StackHost.SizeChanged += OnStackHostSizeChanged;
        MainPanel.SizeChanged += OnStackHostSizeChanged;
        StackScrollViewer.ScrollChanged += OnStackScrollChanged;
        StackHost.PointerPressed += OnStackHostPointerPressed;
        ZoomPanControl.PointerMoved += OnZoomPointerMoved;
        ZoomPanControl.PointerExited += OnZoomPointerExited;
        SplitOverlay.PointerPressed += OnSplitPointerPressed;
        SplitOverlay.PointerMoved += OnSplitPointerMoved;
        SplitOverlay.PointerReleased += OnSplitPointerReleased;
        SplitOverlay.PointerCaptureLost += OnSplitPointerCaptureLost;
        SplitHandle.KeyDown += OnSplitHandleKeyDown;
    }

    private static Action<Result> LogError(string method) => DebugHelper.LogError(nameof(ComparisonViewer), method);

    #region INavigableContent

    /// <inheritdoc/>
    public bool HasImage => Comparison.ImageA.Value is not null;

    /// <inheritdoc/>
    public bool IsZoomed => (Comparison.IsSynced.Value ? SyncedLeftZoom.Scale : ZoomPanControl.Scale) is not 1.0;

    /// <summary>
    /// Does nothing: the comparison's flip follows the tab's Orientation without an animation, as in the crop.
    /// </summary>
    public void AnimateNextFlip()
    {
    }

    /// <inheritdoc cref="CustomControls.Zoom.ZoomPanControl.ResetZoom(bool)"/>
    public void ResetZoom(bool enableAnimations = true)
    {
        if (Comparison.IsSynced.Value)
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
        if (Comparison.IsSynced.Value)
        {
            SyncedLeftZoom.ResetZoomSlim();
            SyncedRightZoom.ResetZoomSlim();
            _tab.Zoom.SetFactor(1.0);
            return;
        }

        ZoomPanControl.ResetZoomSlim();
    }

    public void TriggerScalingModeUpdate(bool invalidate)
    {
        ImageControlHelper.TriggerScalingModeUpdate(ImageA, invalidate);
        ImageControlHelper.TriggerScalingModeUpdate(ImageB, invalidate);
        ImageControlHelper.TriggerScalingModeUpdate(SyncedLeftImage, invalidate);
        ImageControlHelper.TriggerScalingModeUpdate(SyncedRightImage, invalidate);
    }

    // Measured on the zoom control the wheel zooms: the one under the pointer while synced
    Point INavigableContent.ZoomPointer(PointerEventArgs e) => e.GetPosition(ZoomTarget());

    void INavigableContent.Magnify(PointerDeltaEventArgs e) =>
        ZoomTarget().ZoomWithPointerWheelCore(e.Delta.Y > 0, e.GetPosition(ZoomTarget()));

    ZoomPreviewer? INavigableContent.ZoomPreview => ZoomPreview;

    Control? INavigableContent.DockedBar => CompareBar;

    /// <summary>
    /// A press on the split handle or its strip drags the split, and a press while aligning is a point, so neither
    /// navigates. The synced layout hides the stacked pair, so a press there is never a point.
    /// </summary>
    public bool TakesPress(PointerPressedEventArgs e)
    {
        if (Comparison.IsAligning.Value && !Comparison.IsSynced.Value)
        {
            return true;
        }

        return SplitOverlay.IsVisible && e.Source is Visual source &&
               (IsWithin(source, SplitHandle) || IsWithin(source, SplitDividerHitStrip));
    }

    private static bool IsWithin(Visual source, Visual target) =>
        ReferenceEquals(source, target) || target.IsVisualAncestorOf(source);

    /// <summary>
    /// Shows the comparison at <paramref name="factor"/>, the tab's Zoom Factor: at once when loaded, else when it
    /// is loaded.
    /// </summary>
    public void ShowFactor(double factor)
    {
        if (IsLoaded)
        {
            ApplyFactor(factor);
            return;
        }

        _pendingFactor = factor;
    }

    private void ApplyFactor(double factor)
    {
        if (Comparison.IsSynced.Value)
        {
            ShowScale(SyncedLeftZoom, factor);
            ShowScale(SyncedRightZoom, factor);
            return;
        }

        ShowScale(ZoomPanControl, factor);
    }

    // A control already at the factor is left alone, as setting the scale again would re-centre a panned view
    private static void ShowScale(ZoomPanControl zoom, double factor)
    {
        if (zoom.Scale.Equals(factor))
        {
            return;
        }

        zoom.SetScaleImmediate(factor);
    }

    #endregion

    #region IZoomScrollTarget

    // The stacked pair scrolls as the image viewer's image does; the synced layout is zoomed and panned only
    bool IZoomScrollTarget.CanScrollVertically =>
        !Comparison.IsSynced.Value &&
        StackScrollViewer.VerticalScrollBarVisibility is ScrollBarVisibility.Visible or ScrollBarVisibility.Auto;

    bool IZoomScrollTarget.IsImageTallerThanViewer =>
        !Comparison.IsSynced.Value && StackScrollViewer.Extent.Height > StackScrollViewer.Viewport.Height;

    void IZoomScrollTarget.ScrollLine(bool down)
    {
        if (Comparison.IsSynced.Value)
        {
            return;
        }

        if (down)
        {
            StackScrollViewer.LineDown();
        }
        else
        {
            StackScrollViewer.LineUp();
        }
    }

    void IZoomScrollTarget.ScrollToHome()
    {
        if (!Comparison.IsSynced.Value)
        {
            StackScrollViewer.ScrollToHome();
        }
    }

    void IZoomScrollTarget.ScrollToEnd()
    {
        if (!Comparison.IsSynced.Value)
        {
            StackScrollViewer.ScrollToEnd();
        }
    }

    void IZoomScrollTarget.ZoomIn() => (Comparison.IsSynced.Value ? SyncedLeftZoom : ZoomPanControl).ZoomIn();

    void IZoomScrollTarget.ZoomOut() => (Comparison.IsSynced.Value ? SyncedLeftZoom : ZoomPanControl).ZoomOut();

    void IZoomScrollTarget.ZoomAt(bool zoomIn, Point pointer) =>
        ZoomTarget().ZoomWithPointerWheelCore(zoomIn, pointer);

    // The control the wheel zooms: the main one, or the synced one under the pointer
    private ZoomPanControl ZoomTarget() =>
        Comparison.IsSynced.Value
            ? SyncedRightZoom.IsPointerOver ? SyncedRightZoom : SyncedLeftZoom
            : ZoomPanControl;

    // Runs only from the subscription to the window's scrolling, so only with a window
    private void ShowScrolling() =>
        StackScrollViewer.VerticalScrollBarVisibility = _windowSettings!.IsScrollingEnabled.Value
            ? ScrollBarVisibility.Visible
            : ScrollBarVisibility.Disabled;

    #endregion

    #region Zoom

    private void ShowSyncedFactor()
    {
        if (Comparison.IsSynced.Value)
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
    }

    /// <summary>
    /// Reports the main control's scale to the tab as the Zoom Factor (ADR-0016), shows the zoom percentage popup
    /// when that setting asks, and keeps the zoom preview above the hover bar.
    /// </summary>
    private void OnScaleChanged(double scale)
    {
        if (Comparison.IsSynced.Value)
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

        UpdateZoomPreviewMargin();
    }

    // In the synced layout only the left control writes the factor; the right one syncs the left, which raises it
    private void OnSyncedScaleChanged(double scale)
    {
        if (Comparison.IsSynced.Value)
        {
            _tab.Zoom.SetFactor(scale);
        }
    }

    private void OnSyncedLeftViewChanged() =>
        SyncSyncedZoom(SyncedLeftZoom, SyncedLeftImage, SyncedRightZoom, SyncedRightImage);

    private void OnSyncedRightViewChanged() =>
        SyncSyncedZoom(SyncedRightZoom, SyncedRightImage, SyncedLeftZoom, SyncedLeftImage);

    // Shows the same relative point of the other image at the center of the other control
    private void SyncSyncedZoom(ZoomPanControl sourceZoom, PicBox sourceImage, ZoomPanControl targetZoom,
        PicBox targetImage)
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

    #endregion

    #region Mode visuals

    private void ApplyComparisonVisuals()
    {
        var c = Comparison;
        ImageB.Stretch = c.IsStretchFill.Value ? Stretch.Fill : Stretch.Uniform;
        // Pixel Info shows A alone
        ImageB.IsVisible = c.Mode.Value is not ComparisonMode.PixelInfo;
        switch (c.Mode.Value)
        {
            case ComparisonMode.PixelInfo:
                StopBlinkTimer();
                ImageB.Clip = null;
                break;

            case ComparisonMode.Overlay:
                StopBlinkTimer();
                ImageB.BlendMode = BitmapBlendingMode.Unspecified;
                ImageB.Opacity = Math.Clamp(c.OverlayOpacity.Value, 0.0, 1.0);
                ImageB.Clip = null;
                break;

            case ComparisonMode.Split:
                StopBlinkTimer();
                ImageB.BlendMode = BitmapBlendingMode.Unspecified;
                ImageB.Opacity = 1.0;
                UpdateSplitClipAndOverlay();
                break;

            case ComparisonMode.Blink:
                ImageB.BlendMode = BitmapBlendingMode.Unspecified;
                ImageB.Clip = null;
                UpdateBlinkTimer();
                break;

            case ComparisonMode.Difference:
            case ComparisonMode.Synced:
            default:
                StopBlinkTimer();
                ImageB.BlendMode = BitmapBlendingMode.Difference;
                ImageB.Opacity = 1.0;
                ImageB.Clip = null;
                break;
        }
    }

    // Also on the main panel's: it is arranged after the stack host, so only then does the stack host map to it
    private void OnStackHostSizeChanged(object? sender, SizeChangedEventArgs e) =>
        UpdateSplitClipAndOverlay();

    // Scrolling moves the stack host, so the divider has to follow it, and another pixel is under the pointer
    private void OnStackScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        UpdateSplitClipAndOverlay();
        ReadPixelUnderPointer();
    }

    private void OnMainZoomViewChanged()
    {
        UpdateSplitClipAndOverlay();
        ReadPixelUnderPointer();
    }

    private void UpdateSplitClipAndOverlay()
    {
        if (!Comparison.IsSplit.Value)
        {
            ImageB.Clip = null;
            return;
        }

        var bounds = StackHost.Bounds;
        var w = bounds.Width;
        var h = bounds.Height;
        if (w <= 0 || h <= 0)
        {
            return;
        }

        var ratio = Math.Clamp(Comparison.SplitRatio.Value, 0.0, 1.0);
        var mainH = MainPanel.Bounds.Height > 0 ? MainPanel.Bounds.Height : h;
        var mainW = MainPanel.Bounds.Width > 0 ? MainPanel.Bounds.Width : w;

        if (Comparison.IsSplitHorizontal.Value)
        {
            var splitY = h * ratio;
            ImageB.Clip = new RectangleGeometry(new Rect(0, 0, w, splitY));

            var screenPos = StackHost.TranslatePoint(new Point(w * 0.5, splitY), MainPanel);
            var lineY = screenPos?.Y ?? splitY;
            var handleX = screenPos?.X ?? w * 0.5;

            SplitDividerLine.HorizontalAlignment = HorizontalAlignment.Stretch;
            SplitDividerLine.VerticalAlignment = VerticalAlignment.Top;
            SplitDividerLine.Width = double.NaN;
            SplitDividerLine.Height = 2;
            SplitDividerLine.Margin = new Thickness(0, Math.Clamp(lineY - 1, 0, Math.Max(0, mainH - 2)), 0, 0);

            SplitDividerHitStrip.HorizontalAlignment = HorizontalAlignment.Stretch;
            SplitDividerHitStrip.VerticalAlignment = VerticalAlignment.Top;
            SplitDividerHitStrip.Width = double.NaN;
            SplitDividerHitStrip.Height = 16;
            SplitDividerHitStrip.Margin = new Thickness(0, Math.Clamp(lineY - 8, 0, Math.Max(0, mainH - 16)), 0, 0);
            SplitDividerHitStrip.Cursor = SizeNorthSouthCursor;

            SplitHandle.Margin = new Thickness(
                Math.Clamp(handleX - 14, 0, Math.Max(0, mainW - 28)),
                Math.Clamp(lineY - 14, 0, Math.Max(0, mainH - 28)),
                0, 0);
            SplitHandle.Cursor = SizeNorthSouthCursor;
            SplitHandleIcon.Data = SplitHorizontalIcon;
            // The up-down arrows sit off center to the left without this, as the left-right ones do vertically
            SplitHandleIcon.Margin = new Thickness(5, 0, 0, 0);
        }
        else
        {
            var splitX = w * ratio;
            ImageB.Clip = new RectangleGeometry(new Rect(0, 0, splitX, h));

            var screenPos = StackHost.TranslatePoint(new Point(splitX, h * 0.5), MainPanel);
            var lineX = screenPos?.X ?? splitX;
            var handleY = screenPos?.Y ?? h * 0.5;

            SplitDividerLine.HorizontalAlignment = HorizontalAlignment.Left;
            SplitDividerLine.VerticalAlignment = VerticalAlignment.Stretch;
            SplitDividerLine.Width = 2;
            SplitDividerLine.Height = double.NaN;
            SplitDividerLine.Margin = new Thickness(Math.Clamp(lineX - 1, 0, Math.Max(0, mainW - 2)), 0, 0, 0);

            SplitDividerHitStrip.HorizontalAlignment = HorizontalAlignment.Left;
            SplitDividerHitStrip.VerticalAlignment = VerticalAlignment.Stretch;
            SplitDividerHitStrip.Width = 16;
            SplitDividerHitStrip.Height = double.NaN;
            SplitDividerHitStrip.Margin = new Thickness(Math.Clamp(lineX - 8, 0, Math.Max(0, mainW - 16)), 0, 0, 0);
            SplitDividerHitStrip.Cursor = SizeWestEastCursor;

            SplitHandle.Margin = new Thickness(
                Math.Clamp(lineX - 14, 0, Math.Max(0, mainW - 28)),
                Math.Clamp(handleY - 14, 0, Math.Max(0, mainH - 28)),
                0, 0);
            SplitHandle.Cursor = SizeWestEastCursor;
            SplitHandleIcon.Data = SplitVerticalIcon;
            SplitHandleIcon.Margin = new Thickness(0, 5, 0, 0);
        }
    }

    private void OnSplitPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!Comparison.IsSplit.Value || !e.GetCurrentPoint(SplitOverlay).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.ClickCount == 2 && SplitHandle.Bounds.Inflate(6).Contains(e.GetPosition(SplitOverlay)))
        {
            Comparison.SplitRatio.Value = 0.5;
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
        var hostBounds = StackHost.Bounds;
        if (hostBounds.Width <= 0 || hostBounds.Height <= 0)
        {
            return;
        }

        var mapped = MainPanel.TranslatePoint(mainPanelPos, StackHost) ?? mainPanelPos;
        var ratio = Comparison.IsSplitHorizontal.Value
            ? mapped.Y / hostBounds.Height
            : mapped.X / hostBounds.Width;
        Comparison.SplitRatio.Value = Math.Clamp(ratio, 0.0, 1.0);
    }

    private void OnSplitHandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (!Comparison.IsSplit.Value)
        {
            return;
        }

        var ratio = Comparison.SplitRatio;
        switch (e.Key)
        {
            case Key.Left:
            case Key.Up:
                ratio.Value = Math.Clamp(ratio.Value - 0.01, 0.0, 1.0);
                e.Handled = true;
                break;
            case Key.Right:
            case Key.Down:
                ratio.Value = Math.Clamp(ratio.Value + 0.01, 0.0, 1.0);
                e.Handled = true;
                break;
            case Key.Home:
                ratio.Value = 0.5;
                e.Handled = true;
                break;
        }
    }

    // Blinks only while attached, so a viewer that is not shown leaves the timer stopped
    private void UpdateBlinkTimer()
    {
        if (!_isAttached || _isDisposed || !Comparison.IsBlink.Value)
        {
            StopBlinkTimer();
            return;
        }

        var seconds = Comparison.IsBlinkFast.Value
            ? 0.3
            : Math.Clamp(Comparison.BlinkInterval.Value, 0.1, 5.0);

        if (_blinkTimer is null)
        {
            _blinkTimer = new DispatcherTimer();
            _blinkTimer.Tick += OnBlinkTick;
        }

        _blinkTimer.Interval = TimeSpan.FromSeconds(seconds);
        if (!_blinkTimer.IsEnabled)
        {
            _blinkShowsB = true;
            ImageB.Opacity = 1.0;
            _blinkTimer.Start();
        }
    }

    private void OnBlinkTick(object? sender, EventArgs e)
    {
        _blinkShowsB = !_blinkShowsB;
        ImageB.Opacity = _blinkShowsB ? 1.0 : 0.0;
    }

    private void StopBlinkTimer()
    {
        if (_blinkTimer is { IsEnabled: true })
        {
            _blinkTimer.Stop();
        }

        _blinkShowsB = true;
    }

    #endregion

    #region Alignment

    private void ShowAlignment() =>
        ImageB.RenderTransform = Comparison.Alignment.Value is { } m
            ? new MatrixTransform(new Matrix(m.M11, m.M12, m.M21, m.M22, m.OffsetX, m.OffsetY))
            : null;

    // Picking points shows a cross over the pair and the points picked so far; stopping clears them
    private void ShowAligning()
    {
        var aligning = Comparison.IsAligning.Value;
        ZoomPanControl.IsAligning = aligning;
        StackHost.Cursor = aligning ? CrossCursor : null;
        AlignMarkersCanvas.Children.Clear();
    }

    private void OnStackHostPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!Comparison.IsAligning.Value || !e.GetCurrentPoint(StackHost).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var point = e.GetPosition(StackHost);
        // The fourth point ends the picking, which clears the markers
        switch (Comparison.AlignmentStep.Value)
        {
            case 1:
                AddAlignMarker(point, "A1", isSecondary: false);
                break;
            case 2:
                AddAlignMarker(point, "B1", isSecondary: true);
                break;
            case 3:
                AddAlignMarker(point, "A2", isSecondary: false);
                break;
        }

        Comparison.AddAlignmentPoint(point.X, point.Y);
        e.Handled = true;
    }

    private void AddAlignMarker(Point point, string label, bool isSecondary)
    {
        var marker = new Border
        {
            Width = 20,
            Height = 20,
            CornerRadius = new CornerRadius(10),
            Background = isSecondary ? SecondaryMarkerFill : PrimaryMarkerFill,
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1.5),
            Child = new TextBlock
            {
                Text = label,
                FontSize = 9,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Canvas.SetLeft(marker, point.X - 10);
        Canvas.SetTop(marker, point.Y - 10);
        AlignMarkersCanvas.Children.Add(marker);
    }

    #endregion

    #region Pixel Info

    // The pixel last read, in A's own pixels: 0, 0 until the pointer has been over the image
    private PixelPoint _pixel;

    // Where the pointer is in this viewer while it is over the stacked view, so the pixel under it is read again
    // when the image moves under a still pointer: a zoom, a scroll, a Quarter Turn or a flip
    private Point? _pointer;

    // On the zoom control, which captures the pointer while it pans, so the pixel is read during a pan too
    private void OnZoomPointerMoved(object? sender, PointerEventArgs e)
    {
        _pointer = e.GetPosition(this);
        ReadPixelUnderPointer();
    }

    private void OnZoomPointerExited(object? sender, PointerEventArgs e) => _pointer = null;

    // A Quarter Turn or a flip is shown by the next layout, so the pixel is read once that is done
    private void ReadPixelAfterLayout() =>
        Dispatcher.UIThread.Post(ReadPixelUnderPointer, DispatcherPriority.Loaded);

    private void ReadPixelUnderPointer()
    {
        if (_isDisposed || !Comparison.IsPixelInfo.Value || _pointer is not { } pointer ||
            PixelSizeOfA() is not { } size)
        {
            return;
        }

        // Measured on the image itself, below the rotation and the flip, so the pixel is the image's own however
        // it is turned. Off the image the last reading stays
        if (this.TranslatePoint(pointer, ImageA) is not { } point ||
            !PixelReading.TryLocate(point.X, point.Y, ImageA.Bounds.Width, ImageA.Bounds.Height, size.Width,
                size.Height, out var x, out var y) || _pixel == new PixelPoint(x, y))
        {
            return;
        }

        _pixel = new PixelPoint(x, y);
        ReadPixel();
    }

    // Writes the reading at the pixel last read. A drawing, such as an SVG, has a place and no color
    private void ReadPixel()
    {
        if (!Comparison.IsPixelInfo.Value)
        {
            return;
        }

        var bitmap = Comparison.ImageA.Value as Bitmap;
        if (bitmap is not null)
        {
            // Another image may be smaller than the one the pixel was read in
            var size = bitmap.PixelSize;
            _pixel = new PixelPoint(Math.Clamp(_pixel.X, 0, Math.Max(0, size.Width - 1)),
                Math.Clamp(_pixel.Y, 0, Math.Max(0, size.Height - 1)));
        }

        Comparison.ShowPixel(new PixelReading(_pixel.X, _pixel.Y,
            bitmap is null ? null : BitmapPixelReader.ReadArgb(bitmap, _pixel.X, _pixel.Y)));
    }

    // A's size in its own pixels: a bitmap's, or the size a drawing is laid out at
    private PixelSize? PixelSizeOfA() => Comparison.ImageA.Value switch
    {
        Bitmap bitmap => bitmap.PixelSize,
        null => null,
        _ => ImageA.Source is IImage drawing
            ? new PixelSize((int)Math.Round(drawing.Size.Width), (int)Math.Round(drawing.Size.Height))
            : null
    };

    #endregion

    #region Synced badges and crosshairs

    private void UpdateComparisonBadges()
    {
        var swapped = Comparison.IsSwapped.Value;
        var leftLabel = swapped ? "B" : "A";
        var rightLabel = swapped ? "A" : "B";
        var leftName = Comparison.FileInfoA.Value?.Name;
        var rightName = Comparison.FileInfoB.Value?.Name;
        SyncedLeftBadgeText.Text = string.IsNullOrEmpty(leftName) ? leftLabel : $"{leftLabel} · {leftName}";
        SyncedRightBadgeText.Text = string.IsNullOrEmpty(rightName) ? rightLabel : $"{rightLabel} · {rightName}";
    }

    private void OnSyncedLeftPointerMoved(object? sender, PointerEventArgs e) =>
        UpdateSyncedCrosshair(e, SyncedLeftZoom, SyncedLeftImage, SyncedRightZoom, SyncedRightImage,
            SyncedRightCrosshair, SyncedLeftCrosshair);

    private void OnSyncedRightPointerMoved(object? sender, PointerEventArgs e) =>
        UpdateSyncedCrosshair(e, SyncedRightZoom, SyncedRightImage, SyncedLeftZoom, SyncedLeftImage,
            SyncedLeftCrosshair, SyncedRightCrosshair);

    private void OnSyncedPointerExited(object? sender, PointerEventArgs e) =>
        HideSyncedCrosshairs();

    private void HideSyncedCrosshairs()
    {
        SyncedLeftCrosshair.IsVisible = false;
        SyncedRightCrosshair.IsVisible = false;
    }

    // Marks on the other image the point under the pointer
    private void UpdateSyncedCrosshair(PointerEventArgs e, ZoomPanControl sourceZoom, PicBox sourceImage,
        ZoomPanControl targetZoom, PicBox targetImage, Path targetCrosshair, Path sourceCrosshair)
    {
        if (!Comparison.IsSynced.Value)
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

    #region Hover bar

    // The hover bar is the NavigableView's, which this viewer finds once it is shown in one
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttached = true;
        UpdateBlinkTimer();
        UnfollowHoverBar();
        _hoverBar = this.FindAncestorOfType<NavigableView>()?.HoverBar;
        if (_hoverBar is null)
        {
            return;
        }

        _hoverBar.SizeChanged += OnHoverBarSizeChanged;
        UpdateZoomPreviewMargin();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _isAttached = false;
        StopBlinkTimer();
        UnfollowHoverBar();
    }

    private void OnHoverBarSizeChanged(object? sender, SizeChangedEventArgs e) => UpdateZoomPreviewMargin();

    private void UnfollowHoverBar()
    {
        if (_hoverBar is null)
        {
            return;
        }

        _hoverBar.SizeChanged -= OnHoverBarSizeChanged;
        _hoverBar = null;
    }

    // Keeps the zoom preview above the hover bar while it shows
    private void UpdateZoomPreviewMargin()
    {
        if (_hoverBar is not { } hoverBar)
        {
            return;
        }

        ZoomPreview.Margin = hoverBar.Opacity > 0
            ? new Thickness(0, 0, 25, hoverBar.Bounds.Height / 2 + 25)
            : new Thickness(0, 0, 25, 25);
    }

    #endregion

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        Loaded -= OnLoaded;
        ZoomPanControl.ScaleChanged -= OnScaleChanged;
        ZoomPanControl.ViewChanged -= OnMainZoomViewChanged;
        SyncedLeftZoom.ScaleChanged -= OnSyncedScaleChanged;
        SyncedLeftZoom.ViewChanged -= OnSyncedLeftViewChanged;
        SyncedRightZoom.ViewChanged -= OnSyncedRightViewChanged;
        SyncedLeftZoom.PointerMoved -= OnSyncedLeftPointerMoved;
        SyncedLeftZoom.PointerExited -= OnSyncedPointerExited;
        SyncedRightZoom.PointerMoved -= OnSyncedRightPointerMoved;
        SyncedRightZoom.PointerExited -= OnSyncedPointerExited;
        StackHost.SizeChanged -= OnStackHostSizeChanged;
        MainPanel.SizeChanged -= OnStackHostSizeChanged;
        StackScrollViewer.ScrollChanged -= OnStackScrollChanged;
        StackHost.PointerPressed -= OnStackHostPointerPressed;
        ZoomPanControl.PointerMoved -= OnZoomPointerMoved;
        ZoomPanControl.PointerExited -= OnZoomPointerExited;
        SplitOverlay.PointerPressed -= OnSplitPointerPressed;
        SplitOverlay.PointerMoved -= OnSplitPointerMoved;
        SplitOverlay.PointerReleased -= OnSplitPointerReleased;
        SplitOverlay.PointerCaptureLost -= OnSplitPointerCaptureLost;
        SplitHandle.KeyDown -= OnSplitHandleKeyDown;
        CompareBar.Dispose();
        StopBlinkTimer();
        if (_blinkTimer is not null)
        {
            _blinkTimer.Tick -= OnBlinkTick;
            _blinkTimer = null;
        }

        UnfollowHoverBar();
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
