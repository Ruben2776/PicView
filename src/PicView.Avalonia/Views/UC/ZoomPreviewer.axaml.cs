using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using PicView.Avalonia.Animations;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Sizing;

namespace PicView.Avalonia.Views.UC;

public partial class ZoomPreviewer : UserControl
{
    private Control? _childControl;
    private Timer? _hideTimer;

    // Dragging state
    private bool _isDragging;
    private ZoomPanControl? _zoomPanControl;

    public ZoomPreviewer()
    {
        InitializeComponent();

        CloseButton.Click += delegate { SetInvisible(); };

        // Add pointer event handlers for dragging
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);

        if (Settings.Theme.Dark && !Settings.Theme.GlassTheme)
        {
            return;
        }

        if (Settings.Theme.GlassTheme)
        {
            MainBorder.BorderThickness = new Thickness(0);
        }

        ResetZoomButton.Classes.Remove("altHover");
        CloseButton.Classes.Remove("altHover");
        ResetZoomButton.Classes.Add("hover");
        CloseButton.Classes.Add("hover");
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        // Don't call base to prevent focus
        e.Handled = true;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_zoomPanControl == null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var position = e.GetPosition(OverlayCanvas);

        // Check if the click is within the viewport border area (with some tolerance)
        if (!IsPointInViewportBorder(position))
        {
            return;
        }

        _isDragging = true;

        e.Pointer.Capture(this);
        e.Handled = true;

        // Reset hide timer while dragging
        _hideTimer?.Dispose();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || _zoomPanControl == null || _childControl == null)
        {
            return;
        }

        var currentPosition = e.GetPosition(OverlayCanvas);

        // Convert preview window position to viewport center position in main control
        var targetViewportCenter = ConvertPreviewPositionToMainControlPosition(currentPosition);

        // Calculate what the translation should be to center the viewport at this position
        var controlBounds = _zoomPanControl.Bounds;
        var scale = _zoomPanControl.Scale;

        // Calculate the translation needed to center the viewport at the target position
        var newTranslateX = controlBounds.Width / 2.0 - targetViewportCenter.X * scale;
        var newTranslateY = controlBounds.Height / 2.0 - targetViewportCenter.Y * scale;

        // Use the ZoomPanControl's constrained translation method
        _zoomPanControl.SetConstrainedTranslation(newTranslateX, newTranslateY);

        e.Handled = true;
    }

    private Point ConvertPreviewPositionToMainControlPosition(Point previewPosition)
    {
        if (_zoomPanControl == null || _childControl == null)
        {
            return new Point(0, 0);
        }

        var previewBounds = OverlayCanvas.Bounds;
        var childBounds = _childControl.Bounds;

        if (previewBounds.Width == 0 || previewBounds.Height == 0 ||
            childBounds.Width == 0 || childBounds.Height == 0)
        {
            return new Point(0, 0);
        }

        // Convert preview position to normalized coordinates (0-1)
        var normalizedX = previewPosition.X / previewBounds.Width;
        var normalizedY = previewPosition.Y / previewBounds.Height;

        // Convert to child coordinates
        var childX = normalizedX * childBounds.Width;
        var childY = normalizedY * childBounds.Height;

        return new Point(childX, childY);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;

        // Restart the hide timer
        RestartHideTimer();
    }

    private bool IsPointInViewportBorder(Point point)
    {
        var borderRect = new Rect(
            Canvas.GetLeft(ViewportBorder),
            Canvas.GetTop(ViewportBorder),
            ViewportBorder.Width,
            ViewportBorder.Height
        );

        // Add some tolerance around the border for easier clicking
        const double tolerance = 10;
        var expandedRect = new Rect(
            borderRect.X - tolerance,
            borderRect.Y - tolerance,
            borderRect.Width + tolerance * 2,
            borderRect.Height + tolerance * 2
        );

        return expandedRect.Contains(point);
    }

    public void SetZoomPanControl(ZoomPanControl zoomPanControl)
    {
        _zoomPanControl = zoomPanControl;
        _childControl = zoomPanControl.Child;

        UpdateViewportRect();
    }

    public void UpdateVisibility()
    {
        if (_zoomPanControl == null)
        {
            SetInvisible();
            return;
        }

        if (_isDragging)
        {
            // Don't change position when dragging the zoom preview window
            return;
        }

        if (DataContext is MainViewModel vm)
        {
            if (vm.HoverbarViewModel.IsHoverbarVisible.CurrentValue && UIHelper.GetHoverBar?.Opacity > 0)
            {
                // Fit zoom preview window on top of gallery and/or hoverbar
                // TODO: refactor
                if (UIHelper.GetMainView.Bounds.Width > vm.HoverbarViewModel.MaxWidth + 300)
                {
                    var newBottomMargin = Settings.Gallery.IsBottomGalleryShown
                        ? GalleryFunctions.GetGalleryHeight(vm) + UIHelper.GetHoverBar.BottomBorder.Bounds.Height + 5
                        : 25;
                    Margin = new Thickness(0, 0, 25,
                        UIHelper.GetMainView.Bounds.Height > SizeDefaults.WindowMinSize ? newBottomMargin : 0);
                }
                else
                {
                    var newBottomMargin = Settings.Gallery.IsBottomGalleryShown
                        ? GalleryFunctions.GetGalleryHeight(vm) + UIHelper.GetHoverBar.BottomBorder.Bounds.Height + 10
                        : 115;
                    Margin = new Thickness(0, 0, 70,
                        UIHelper.GetMainView.Bounds.Height > SizeDefaults.WindowMinSize ? newBottomMargin : 0);
                }
                
            }
            else if (Settings.Gallery.IsBottomGalleryShown)
            {
                Margin = new Thickness(0, 0, 25, vm.Gallery.GalleryMargin.CurrentValue.Bottom + 7);
            }
            else
            {
                Margin = new Thickness(0, 0, 25, 25);
            }

            UpdateSize(vm);
        }

        // Show when zoomed in or out (not at 1.0 scale)
        var shouldShow = _zoomPanControl.Scale > 1;

        if (shouldShow)
        {
            SetVisible();
            UpdateViewportRect();
        }
        else
        {
            SetInvisible();
        }

        // Don't start hide timer if we're currently dragging
        if (!_isDragging)
        {
            RestartHideTimer();
        }
    }

    private void UpdateSize(MainViewModel vm)
    {
        const int defaultHeight = 150;
        OverlayImage.Height = defaultHeight;
        if (vm.PicViewer.PixelWidth.CurrentValue is 0 || vm.PicViewer.PixelHeight.CurrentValue is 0)
        {
            return;
        }

        // ReSharper disable once PossibleLossOfFraction

        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            var secondaryWidth = vm.PicViewer.SecondaryImageWidth.CurrentValue * defaultHeight /
                                 vm.PicViewer.ImageHeight.CurrentValue;
            var width = vm.PicViewer.ImageWidth.CurrentValue * defaultHeight / vm.PicViewer.ImageHeight.Value;
            OverlayImage.Width = width;
            OverlayImage.SecondaryImageWidth = secondaryWidth;
        }
        else
        {
            OverlayImage.Width = vm.PicViewer.PixelWidth.CurrentValue * defaultHeight /
                                 vm.PicViewer.PixelHeight.CurrentValue;
            OverlayImage.SecondaryImageWidth = 0;
        }
    }

    private void RestartHideTimer()
    {
        _hideTimer?.Dispose();
        _hideTimer = new Timer(_ =>
        {
            Dispatcher.UIThread.Invoke(async () =>
            {
                // Only hide if we're not dragging
                if (!_isDragging && !IsPointerOver)
                {
                    var opacityAnim = AnimationsHelper.OpacityAnimation(1, 0, TimeSpan.FromSeconds(0.5));
                    await opacityAnim.RunAsync(this);
                    IsVisible = false;
                }
            });
        }, null, TimeSpan.FromSeconds(2.5), Timeout.InfiniteTimeSpan);
    }

    public void SetVisible()
    {
        Opacity = 1;
        IsVisible = true;
    }

    public void SetInvisible()
    {
        Opacity = 1;
        IsVisible = false;
    }

    internal void UpdateViewportRect()
    {
        if (_zoomPanControl == null || _childControl == null)
        {
            return;
        }

        var viewportRect = GetCurrentViewportRect();

        // Update the viewport border rectangle
        Canvas.SetLeft(ViewportBorder, viewportRect.X);
        Canvas.SetTop(ViewportBorder, viewportRect.Y);
        ViewportBorder.Width = viewportRect.Width;
        ViewportBorder.Height = viewportRect.Height;
    }

    private Rect GetCurrentViewportRect()
    {
        if (_zoomPanControl == null || _childControl == null)
        {
            return new Rect();
        }

        // Get the viewport rectangle in normalized coordinates (0-1)
        var scale = _zoomPanControl.Scale;
        var translateX = _zoomPanControl.TranslateX;
        var translateY = _zoomPanControl.TranslateY;

        var controlBounds = _zoomPanControl.Bounds;
        var childBounds = _childControl.Bounds;

        if (controlBounds.Width == 0 || controlBounds.Height == 0 ||
            childBounds.Width == 0 || childBounds.Height == 0)
        {
            return new Rect();
        }

        // Calculate what portion of the child is visible in the control
        var visibleLeft = Math.Max(0, -translateX / scale) / childBounds.Width;
        var visibleTop = Math.Max(0, -translateY / scale) / childBounds.Height;
        var visibleRight = Math.Min(1, (controlBounds.Width - translateX) / scale / childBounds.Width);
        var visibleBottom = Math.Min(1, (controlBounds.Height - translateY) / scale / childBounds.Height);

        // Ensure valid bounds - prevent negative dimensions
        visibleLeft = Math.Clamp(visibleLeft, 0, 1);
        visibleTop = Math.Clamp(visibleTop, 0, 1);
        visibleRight = Math.Clamp(visibleRight, 0, 1);
        visibleBottom = Math.Clamp(visibleBottom, 0, 1);

        // Ensure right >= left and bottom >= top
        if (visibleRight < visibleLeft)
        {
            visibleRight = visibleLeft;
        }

        if (visibleBottom < visibleTop)
        {
            visibleBottom = visibleTop;
        }

        // Convert to preview window coordinates
        var previewWidth = OverlayCanvas.Bounds.Width;
        var previewHeight = OverlayCanvas.Bounds.Height;

        var width = (visibleRight - visibleLeft) * previewWidth;
        var height = (visibleBottom - visibleTop) * previewHeight;

        // Final safety check to ensure non-negative dimensions
        width = Math.Max(0, width);
        height = Math.Max(0, height);

        return new Rect(
            visibleLeft * previewWidth,
            visibleTop * previewHeight,
            width,
            height
        );
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        _hideTimer?.Dispose();
    }
}