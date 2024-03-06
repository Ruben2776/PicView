using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.Navigation;
using System.Runtime.InteropServices;
using Avalonia.Controls.Primitives;
using Point = Avalonia.Point;

namespace PicView.Avalonia.Views;

public partial class ImageViewer : UserControl
{
    private static ScaleTransform? _scaleTransform;
    private static TranslateTransform? _translateTransform;

    private static Point _start;
    private static Point _origin;

    private bool _captured;
    private bool _isZoomed;

    public ImageViewer()
    {
        InitializeComponent();
        AddHandler(PointerWheelChangedEvent, PreviewOnPointerWheelChanged, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, PreviewKeyDown, RoutingStrategies.Tunnel);
        // TODO add visual feedback for drag and drop
        //AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);

        Loaded += delegate
        {
            InitializeZoom();
            if (DataContext is not MainViewModel vm)
                return;
            vm.ImageChanged += (s, e) =>
            {
                if (SettingsHelper.Settings.Zoom.ScrollEnabled)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ImageScrollViewer.ScrollToHome();
                    }, DispatcherPriority.Normal);
                }
                ResetZoom(false);
            };
            LostFocus += (s, e) =>
            {
                _captured = false;
            };
        };
    }

    private void PreviewKeyDown(object? sender, KeyEventArgs e)
    {
    }

    private async Task PreviewOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        e.Handled = true;
        await Main_OnPointerWheelChanged(e);
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        var data = e.Data.GetFiles();
        if (data == null)
        {
            // TODO Handle URL and folder drops
            return;
        }

        var storageItems = data as IStorageItem[] ?? data.ToArray();
        var firstFile = storageItems.FirstOrDefault();
        var path = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? firstFile.Path.AbsolutePath : firstFile.Path.LocalPath;
        _ = vm.LoadPicFromString(path).ConfigureAwait(false);
        foreach (var file in storageItems.Skip(1))
        {
            // TODO Open each file in a new window if the setting to open in the same window is false
        }
    }

    private async Task Main_OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel)
            return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // TODO figure out how to do image navigation with gestures
            return;
        }
        var ctrl = e.KeyModifiers == KeyModifiers.Control;
        var reverse = e.Delta.Y < 0;

        if (SettingsHelper.Settings.Zoom.CtrlZoom)
        {
            if (ctrl)
            {
                if (reverse)
                {
                    ZoomOut(e);
                }
                else
                {
                    ZoomIn(e);
                }
            }
            else
            {
                await ScrollOrNavigate();
            }
        }
        else
        {
            if (ctrl)
            {
                await ScrollOrNavigate();
            }
            else
            {
                if (reverse)
                {
                    ZoomOut(e);
                }
                else
                {
                    ZoomIn(e);
                }
            }
        }
        return;

        async Task ScrollOrNavigate()
        {
            if (!SettingsHelper.Settings.Zoom.ScrollEnabled || e.KeyModifiers == KeyModifiers.Shift)
            {
                await LoadNextPic();
            }
            else
            {
                if (ImageScrollViewer.VerticalScrollBarVisibility is ScrollBarVisibility.Visible or ScrollBarVisibility.Auto)
                {
                    if (reverse)
                    {
                        ImageScrollViewer.LineDown();
                    }
                    else
                    {
                        ImageScrollViewer.LineUp();
                    }
                }
                else
                {
                    await LoadNextPic();
                }
            }
        }

        async Task LoadNextPic()
        {
            var navigateTo = SettingsHelper.Settings.Zoom.HorizontalReverseScroll ? NavigateTo.Next : NavigateTo.Previous;
            if (reverse)
            {
                navigateTo = SettingsHelper.Settings.Zoom.HorizontalReverseScroll ? NavigateTo.Previous : NavigateTo.Next;
            }
            await mainViewModel.LoadNextPic(navigateTo);
        }
    }

    private void ImageScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        e.Handled = true;
    }

    #region Zoom

    private void InitializeZoom()
    {
        ImageZoomBorder.RenderTransform = new TransformGroup
        {
            Children =
            [
                new ScaleTransform(),
                new TranslateTransform()
            ]
        };
        _scaleTransform = (ScaleTransform)((TransformGroup)ImageZoomBorder.RenderTransform)
            .Children.First(tr => tr is ScaleTransform);

        _translateTransform = (TranslateTransform)((TransformGroup)ImageZoomBorder.RenderTransform)
            .Children.First(tr => tr is TranslateTransform);
        ImageZoomBorder.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
    }

    private void Capture(PointerEventArgs e)
    {
        _start = e.GetPosition(ImageZoomBorder);
        _origin = new Point(_translateTransform.X, _translateTransform.Y);
        _captured = true;
    }

    public void ZoomIn(PointerWheelEventArgs e)
    {
        ZoomTo(e, true);
    }

    public void ZoomOut(PointerWheelEventArgs e)
    {
        ZoomTo(e, false);
    }

    public void ZoomTo(PointerWheelEventArgs e, bool isZoomIn)
    {
        var currentZoom = _scaleTransform.ScaleX;
        var zoomSpeed = SettingsHelper.Settings.Zoom.ZoomSpeed;

        switch (currentZoom)
        {
            // Increase speed based on the current zoom level
            case > 14 when isZoomIn:
                return;

            case > 4:
                zoomSpeed += 1.5;
                break;

            case > 3.2:
                zoomSpeed += 1;
                break;

            case > 1.6:
                zoomSpeed += 0.5;
                break;
        }

        if (!isZoomIn)
        {
            zoomSpeed = -zoomSpeed;
        }

        currentZoom += zoomSpeed;
        currentZoom = Math.Max(0.09, currentZoom);
        if (SettingsHelper.Settings.Zoom.AvoidZoomingOut && currentZoom < 1.0)
        {
            ResetZoom(true);
        }
        else
        {
            ZoomTo(e, currentZoom, true);
        }
    }

    public void ZoomTo(PointerWheelEventArgs e, double zoomValue, bool enableAnimations)
    {
        if (_scaleTransform == null || _translateTransform == null)
        {
            return;
        }

        if (enableAnimations)
        {
            _scaleTransform.Transitions ??=
            [
                new DoubleTransition { Property = ScaleTransform.ScaleXProperty, Duration = TimeSpan.FromSeconds(.25) },
                new DoubleTransition { Property = ScaleTransform.ScaleYProperty, Duration = TimeSpan.FromSeconds(.25) }
            ];
            _translateTransform.Transitions ??=
            [
                new DoubleTransition { Property = TranslateTransform.XProperty, Duration = TimeSpan.FromSeconds(.25) },
                new DoubleTransition { Property = TranslateTransform.YProperty, Duration = TimeSpan.FromSeconds(.25) }
            ];
        }
        else
        {
            _scaleTransform.Transitions = null;
            _translateTransform.Transitions = null;
        }

        var point = e.GetPosition(this);

        var absoluteX = point.X * _scaleTransform.ScaleX + _translateTransform.X;
        var absoluteY = point.Y * _scaleTransform.ScaleY + _translateTransform.Y;

        var newTranslateValueX = Math.Abs(zoomValue - 1) > .2 ? absoluteX - point.X * zoomValue : 0;
        var newTranslateValueY = Math.Abs(zoomValue - 1) > .2 ? absoluteY - point.Y * zoomValue : 0;
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _scaleTransform.ScaleX = zoomValue;
            _scaleTransform.ScaleY = zoomValue;
            _translateTransform.X = newTranslateValueX;
            _translateTransform.Y = newTranslateValueY;
        }, DispatcherPriority.Normal);
        _isZoomed = zoomValue != 0;
    }

    public void ResetZoom(bool enableAnimations)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (enableAnimations)
            {
                _scaleTransform.Transitions ??=
                [
                    new DoubleTransition { Property = ScaleTransform.ScaleXProperty, Duration = TimeSpan.FromSeconds(.25) },
                    new DoubleTransition { Property = ScaleTransform.ScaleYProperty, Duration = TimeSpan.FromSeconds(.25) }
                ];
                _translateTransform.Transitions ??=
                [
                    new DoubleTransition { Property = TranslateTransform.XProperty, Duration = TimeSpan.FromSeconds(.25) },
                    new DoubleTransition { Property = TranslateTransform.YProperty, Duration = TimeSpan.FromSeconds(.25) }
                ];
            }
            else
            {
                _scaleTransform.Transitions = null;
                _translateTransform.Transitions = null;
            }

            _scaleTransform.ScaleX = 1;
            _scaleTransform.ScaleY = 1;
            _translateTransform.X = 0;
            _translateTransform.Y = 0;
        }, DispatcherPriority.Normal);
        _isZoomed = false;
    }

    public void Pan(PointerEventArgs e, bool enableAnimations)
    {
        if (!_captured || _scaleTransform == null || !_isZoomed)
        {
            return;
        }

        if (enableAnimations || _translateTransform.X < -0)
        {
            _translateTransform.Transitions ??=
            [
                new DoubleTransition { Property = TranslateTransform.XProperty, Duration = TimeSpan.FromSeconds(.15) },
                new DoubleTransition { Property = TranslateTransform.YProperty, Duration = TimeSpan.FromSeconds(.15) }
            ];
        }
        else
        {
            _translateTransform.Transitions = null;
        }

        var position = _start - e.GetPosition(ImageZoomBorder);
        var speed = _translateTransform.X < -200 ? 7 : 3;
        speed = _translateTransform.X < -350 ? 11 : speed;
        speed = _translateTransform.X < -500 ? 15 : speed;
        speed = _translateTransform.X < -750 ? 20 : speed;
        speed = _translateTransform.X < -1000 ? 25 : speed;
        speed = _translateTransform.X < -1200 ? -35 : speed;
        var x = _origin.X - (position.X + speed);
        var y = _origin.Y - (position.Y + speed);

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _translateTransform.X = x;
            _translateTransform.Y = y;
        }, DispatcherPriority.Normal);
    }

    #endregion Zoom

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ResetZoom(true);
        }
    }

    private void ImageZoomBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }
        if (e.ClickCount == 2)
        {
            ResetZoom(true);
        }
        else
        {
            Pressed(e);
        }
    }

    private void ImageZoomBorder_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        Pan(e, true);
    }

    private void Pressed(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }
        Capture(e);
    }

    private void ImageZoomBorder_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_captured)
        {
            return;
        }

        _captured = false;
    }
}