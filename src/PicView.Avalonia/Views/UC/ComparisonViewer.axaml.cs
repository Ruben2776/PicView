using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.CustomControls.Zoom;
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
/// modes, or A and B side by side, zoomed and panned together, in the synced mode. Its zoom is the tab's Zoom Factor.
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
        var c = Comparison;
        c.Mode.Subscribe(_ => ApplyComparisonVisuals(), LogError(nameof(ApplyComparisonVisuals)))
            .AddTo(ref _disposables);
        c.OverlayOpacity.Subscribe(_ => ApplyComparisonVisuals(), LogError(nameof(ApplyComparisonVisuals)))
            .AddTo(ref _disposables);
        c.IsStretchFill.Subscribe(_ => ApplyComparisonVisuals(), LogError(nameof(ApplyComparisonVisuals)))
            .AddTo(ref _disposables);
        c.SplitRatio.Subscribe(_ => UpdateSplitClipAndOverlay(), LogError(nameof(UpdateSplitClipAndOverlay)))
            .AddTo(ref _disposables);
        c.IsSplitHorizontal.Subscribe(_ => UpdateSplitClipAndOverlay(), LogError(nameof(UpdateSplitClipAndOverlay)))
            .AddTo(ref _disposables);
        c.IsBlinkFast.Subscribe(_ => UpdateBlinkTimer(), LogError(nameof(UpdateBlinkTimer)))
            .AddTo(ref _disposables);
        c.BlinkInterval.Subscribe(_ => UpdateBlinkTimer(), LogError(nameof(UpdateBlinkTimer)))
            .AddTo(ref _disposables);
        c.IsSwapped.AsObservable().Subscribe(_ => UpdateComparisonBadges(), LogError(nameof(UpdateComparisonBadges)))
            .AddTo(ref _disposables);
        c.FileInfoA.AsObservable().Subscribe(_ => UpdateComparisonBadges(), LogError(nameof(UpdateComparisonBadges)))
            .AddTo(ref _disposables);
        c.FileInfoB.AsObservable().Subscribe(_ => UpdateComparisonBadges(), LogError(nameof(UpdateComparisonBadges)))
            .AddTo(ref _disposables);

        c.Alignment.AsObservable().Subscribe(ShowAlignment, LogError(nameof(ShowAlignment))).AddTo(ref _disposables);
        c.HasAlignment.AsObservable().Subscribe(has =>
        {
            if (!has)
            {
                ImageB.RenderTransform = null;
            }
        }, LogError(nameof(ComparisonViewModel.HasAlignment))).AddTo(ref _disposables);
        c.IsAligning.AsObservable().Subscribe(aligning =>
        {
            ZoomPanControl.IsAligning = aligning;
            StackHost.Cursor = aligning ? CrossCursor : null;
            AlignMarkersCanvas.Children.Clear();
        }, LogError(nameof(ComparisonViewModel.IsAligning))).AddTo(ref _disposables);

        // Entering or leaving the synced layout in a shown viewer: the factor follows the layout now shown
        c.IsSynced.AsObservable().Skip(1).Subscribe(synced =>
        {
            if (synced)
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
        }, LogError(nameof(ComparisonViewModel.IsSynced))).AddTo(ref _disposables);

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
        StackHost.PointerPressed += OnStackHostPointerPressed;
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

    /// <inheritdoc cref="ZoomPanControl.ResetZoom(bool)"/>
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

    /// <summary>
    /// A press on the split handle or its strip drags the split, and a press while aligning is a point, so neither
    /// navigates.
    /// </summary>
    public bool TakesPress(PointerPressedEventArgs e)
    {
        if (Comparison.IsAligning.Value)
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
            SyncedLeftZoom.SetScaleImmediate(factor);
            SyncedRightZoom.SetScaleImmediate(factor);
            return;
        }

        ZoomPanControl.SetScaleImmediate(factor);
    }

    #endregion

    #region IZoomScrollTarget

    // Nothing scrolls: the comparison is zoomed and panned only
    bool IZoomScrollTarget.CanScrollVertically => false;

    bool IZoomScrollTarget.IsImageTallerThanViewer => false;

    void IZoomScrollTarget.ScrollLine(bool down)
    {
    }

    void IZoomScrollTarget.ScrollToHome()
    {
    }

    void IZoomScrollTarget.ScrollToEnd()
    {
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

    #endregion

    #region Zoom

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
        switch (c.Mode.Value)
        {
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

    private void OnMainZoomViewChanged() =>
        UpdateSplitClipAndOverlay();

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

    private void ShowAlignment(SimilarityMatrix? alignment)
    {
        if (alignment is not { } m)
        {
            return;
        }

        ImageB.RenderTransform = new MatrixTransform(new Matrix(m.M11, m.M12, m.M21, m.M22, m.OffsetX, m.OffsetY));
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

    private void OnHoverBarSizeChanged(object? sender, SizeChangedEventArgs e) =>
        UpdateZoomPreviewMargin();

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
        StackHost.PointerPressed -= OnStackHostPointerPressed;
        SplitOverlay.PointerPressed -= OnSplitPointerPressed;
        SplitOverlay.PointerMoved -= OnSplitPointerMoved;
        SplitOverlay.PointerReleased -= OnSplitPointerReleased;
        SplitOverlay.PointerCaptureLost -= OnSplitPointerCaptureLost;
        SplitHandle.KeyDown -= OnSplitHandleKeyDown;
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
