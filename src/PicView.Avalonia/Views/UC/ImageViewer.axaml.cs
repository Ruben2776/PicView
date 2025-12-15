using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.History;
using PicView.Avalonia.ImageTransformations;
using PicView.Avalonia.ImageTransformations.Rotation;
using PicView.Avalonia.Input;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Exif;

namespace PicView.Avalonia.Views.UC;


public partial class ImageViewer : UserControl
{
    private RotationTransformer? _imageTransformer;
    
    public ImageViewer()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        InitializeImageTransformer();
        ZoomPanControl.Initialize();
        ImageControlHelper.TriggerScalingModeUpdate(MainImage, true);
        
        // Start in dispatcher with low priority,
        // because it is more important to schedule it after more important things.
        Dispatcher.UIThread.Invoke(() =>
        {
            AddHandler(PointerWheelChangedEvent, PreviewOnPointerWheelChanged, RoutingStrategies.Tunnel);
            AddHandler(Gestures.PointerTouchPadGestureMagnifyEvent, TouchMagnifyEvent, RoutingStrategies.Bubble);
            AddHandler(Gestures.PinchEvent, TouchMagnifyEvent, RoutingStrategies.Bubble);
            InitializeMouseInputHelper();
        }, DispatcherPriority.Background);
    }

    public void TriggerScalingModeUpdate(bool invalidate) =>
        ImageControlHelper.TriggerScalingModeUpdate(MainImage, invalidate);

    private void TouchMagnifyEvent(object? sender, PointerDeltaEventArgs e) =>
        ZoomPanControl.ZoomWithPointerWheelCore(e.Delta.Y > 0, e.GetPosition(this));

    public static async Task PreviewOnPointerWheelChanged(object? sender, PointerWheelEventArgs e) =>
        await MouseShortcuts.HandlePointerWheelChanged(e);

    private void InitializeImageTransformer()
    {
        if (_imageTransformer is not null)
        {
            return;
        }
        _imageTransformer = new RotationTransformer(
            ImageLayoutTransformControl,
            MainImage,
            () => DataContext,
            () =>
            {
                ZoomPanControl.ResetZoomSlim();
            });
    }

    private void InitializeMouseInputHelper() =>
        MouseShortcuts.InitializeMouseShortcuts(
            ImageScrollViewer,
            async e => { await Dispatcher.UIThread.InvokeAsync(() => { ZoomIn(e); }); },
            async e => { await Dispatcher.UIThread.InvokeAsync(() => { ZoomOut(e); }); });

    public async Task ApplyBitmapAndRefresh(Bitmap bmp, MainViewModel vm)
    {
        if (bmp is null || vm is null)
            return;

        if (vm.PicViewer.ImageSource.Value is Bitmap oldBmp)
            oldBmp.Dispose();

        vm.PicViewer.ImageSource.Value = bmp;

        var ps = bmp.PixelSize;
        vm.PicViewer.PixelWidth.Value  = ps.Width;
        vm.PicViewer.PixelHeight.Value = ps.Height;

        ImageLayoutTransformControl.LayoutTransform = null;
        MainImage.RenderTransform = null;

        MainImage.Width = double.NaN;
        MainImage.Height = double.NaN;
        MainImage.InvalidateMeasure();
        MainImage.InvalidateArrange();
        MainImage.InvalidateVisual();

        WindowResizing.SetSize(ps.Width, ps.Height, 0, 0, vm);
        ZoomPanControl.NotifyContentResized();
    }

    public async Task ApplySnapshotBitmap(Bitmap bmp, MainViewModel vm)
    {
        if (bmp is null || vm is null)
            return;

        vm.PicViewer.ImageSource.Value = bmp;

        var ps = bmp.PixelSize;
        vm.PicViewer.PixelWidth.Value  = ps.Width;
        vm.PicViewer.PixelHeight.Value = ps.Height;

        WindowResizing.SetSize(ps.Width, ps.Height, 0, 0, vm);
        ZoomPanControl.NotifyContentResized();
    }

    #region Zoom
    /// <inheritdoc cref="Zoom.ZoomIn(MainViewModel)"/>
    private void ZoomIn(PointerWheelEventArgs e) =>
        ZoomPanControl.ZoomWithPointerWheel(e);

    /// <inheritdoc cref="Zoom.ZoomOut(MainViewModel)"/>
    private void ZoomOut(PointerWheelEventArgs e) =>
        ZoomPanControl.ZoomWithPointerWheel(e);

    /// <inheritdoc cref="Zoom.ZoomIn(MainViewModel)"/>
    public void ZoomIn() =>
        ZoomPanControl.ZoomIn();

    /// <inheritdoc cref="Zoom.ZoomOut(MainViewModel)"/>
    public void ZoomOut() =>
        ZoomPanControl.ZoomOut();

    /// <inheritdoc cref="Zoom.ResetZoom(bool, MainViewModel)"/>
    public void ResetZoom(bool enableAnimations = true) =>
        ZoomPanControl.ResetZoom(enableAnimations);
    
    #endregion

    #region Image Transformation
    public async Task RotateAsync(double angle) => await _imageTransformer?.RotateAsync(angle);
    public async Task FlipAsync(bool horizontal) => await _imageTransformer?.FlipAsync(horizontal);
    #endregion
}