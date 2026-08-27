using Avalonia;
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
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC;

public partial class ImageViewer : UserControl, IDisposable
{
    private RotationTransformer? _imageTransformer;
    private DisposableBag _disposables;
    
    public ImageViewer()
    {
        InitializeComponent();
        ImageControlHelper.TriggerScalingModeUpdate(MainImage, true);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        InitializeImageTransformer();
        
        AddHandler(PointerWheelChangedEvent, PreviewOnPointerWheelChanged, RoutingStrategies.Tunnel);
        AddHandler(PointerTouchPadGestureMagnifyEvent, TouchMagnifyEvent, RoutingStrategies.Bubble);
        AddHandler(PinchEvent, TouchMagnifyEvent, RoutingStrategies.Bubble);
        _disposables.Add(new HoverFadeButtonHandler(GalleryShortcut, GalleryShortcut.InnerButton));

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

        // Zooming and panning move the images through render transforms without
        // triggering layout, so observe the transform values while they change. The
        // child's render transform is read because animated zooms interpolate it
        // while the control's own properties already hold the target values.
        // The frame provider ticks with the render loop, so this only runs while
        // frames are actually being rendered (i.e. while something visually moves)
        var frameProvider = (TopLevel.GetTopLevel(this) as MainWindow)?.FrameProvider;
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
            }, frameProvider)
            .Subscribe(_ => UpdateMotionPhotoBadgePositions(),
                DebugHelper.LogError(nameof(ImageViewer), nameof(OnLoaded)))
            .AddTo(ref _disposables);

        // The flip animates the RenderTransform of MainTransform without triggering layout
        Observable.EveryValueChanged(MainTransform,
                transform => (transform.RenderTransform as ScaleTransform)?.ScaleX ?? 1d,
                frameProvider)
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
        var isSingleImage = tabViewModel.SingleImageType is not SingleImageType.None;
        MotionPhotoView.OnImageChanged(isSingleImage ? null : tabViewModel.Model);
        SecondaryMotionPhotoView.OnImageChanged(isSingleImage ? null : tabViewModel.SecondaryModel);
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

    public async ValueTask PreviewOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (GalleryView.IsPointerOver)
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        
        await MouseShortcuts.HandlePointerWheelChanged(
            e,
            mainWindow.DataContext as MainWindowViewModel,          
            mainWindow,
            ImageScrollViewer,
            async args => await Dispatcher.UIThread.InvokeAsync(() => ZoomIn(args)),
            async args => await Dispatcher.UIThread.InvokeAsync(() => ZoomOut(args)));
    }
        

    private void InitializeImageTransformer()
    {
        if (_imageTransformer is not null)
        {
            return;
        }

        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        // The image is not flipped by default, update translation to reflect that
        core.Translation.IsFlipped.Value = TranslationManager.Translation.Flip;

        _imageTransformer = new RotationTransformer(
            MainTransform,
            MainImage,
            core.MainWindows.ActiveWindow.CurrentValue,
            TopLevel.GetTopLevel(this) as MainWindow);
        ZoomPanControl.Initialize(ZoomPreview);

        Observable.EveryValueChanged(ZoomPanControl, zoom => zoom.Scale)
            .Skip(1)
            .Subscribe(zoomLevel =>
            {
                if (DataContext is not TabViewModel tab)
                {
                    return;
                }
                var adjustedZoomLevel = Convert.ToInt32(tab.InitialZoom.CurrentValue * (zoomLevel * 100));
                tab.ZoomLevel.Value = adjustedZoomLevel;;
                tab.UpdateTabTitle();
                if (Settings.Zoom.IsShowingZoomPercentagePopup)
                {
                    var message = StringExtensions.CombineWithPercentage(adjustedZoomLevel);
                    _ = TooltipHelper.ShowTooltipMessageContinuallyAsync(message, true,
                        TopLevel.GetTopLevel(this) as MainWindow, TimeSpan.FromSeconds(1));
                }

                ZoomPreview.Margin = HoverBar.Opacity > 0 ? new Thickness(0,0,25,HoverBar.Bounds.Height / 2 + 25) : new Thickness(0, 0, 25, 25);
            }, DebugHelper.LogError(nameof(ImageViewer), nameof(InitializeImageTransformer))).AddTo(ref _disposables);
        
        core.MainWindows.ActiveWindow.CurrentValue.IsScrollingEnabled.Subscribe(isScrolling =>
        {
            ImageScrollViewer.VerticalScrollBarVisibility = isScrolling ?
                ScrollBarVisibility.Visible : ScrollBarVisibility.Disabled;
        }, DebugHelper.LogError(nameof(ImageViewer), nameof(InitializeImageTransformer))).AddTo(ref _disposables);
        
        // Correspond to change when index clicked on track
        Observable.FromEvent<EventHandler<int>, int>(
                handler => (sender, index) => handler(index),
                handler => HoverBar.ProgressBar.ClickedOnTrack += handler,
                handler => HoverBar.ProgressBar.ClickedOnTrack -= handler)
            .SubscribeAwait(async (x, _) =>
            {
                if (DataContext is not TabViewModel tab)
                {
                    return;
                }
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
                if (DataContext is not TabViewModel tab)
                {
                    return;
                }
                await tab.ImageIterator.SkipToIndexAsync(x, tab.GetTabCancellation()).ConfigureAwait(false);
            },DebugHelper.LogError(nameof(ImageViewer), nameof(InitializeImageTransformer)), AwaitOperation.Drop)
            .AddTo(ref _disposables);
    }

    #region Zoom
    /// <inheritdoc cref="Zoom.ZoomIn(ViewModels.MainViewModel)"/>
    private void ZoomIn(PointerWheelEventArgs e) =>
        ZoomPanControl.ZoomWithPointerWheel(e);

    /// <inheritdoc cref="Zoom.ZoomOut(ViewModels.MainViewModel)"/>
    private void ZoomOut(PointerWheelEventArgs e) =>
        ZoomPanControl.ZoomWithPointerWheel(e);

    /// <inheritdoc cref="Zoom.ZoomIn(ViewModels.MainViewModel)"/>
    public void ZoomIn() =>
        ZoomPanControl.ZoomIn();

    /// <inheritdoc cref="Zoom.ZoomOut(ViewModels.MainViewModel)"/>
    public void ZoomOut() =>
        ZoomPanControl.ZoomOut();

    /// <inheritdoc cref="Zoom.ResetZoom(bool, ViewModels.MainViewModel)"/>
    public void ResetZoom(bool enableAnimations = true) =>
        ZoomPanControl.ResetZoom(enableAnimations);
    
    public void ResetZoomSlim() =>
        ZoomPanControl.ResetZoomSlim();
    
    #endregion

    #region Image Transformation
    public void Rotate(bool clockWise) => _imageTransformer?.Rotate(clockWise);
    public void Rotate(int angle) => _imageTransformer?.Rotate(angle);
    public void Flip(bool animate) => _imageTransformer?.Flip(animate);
        
    #endregion

    public void Dispose()
    {
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
        _disposables.Dispose();
        HoverBar.Dispose();
    }
}
