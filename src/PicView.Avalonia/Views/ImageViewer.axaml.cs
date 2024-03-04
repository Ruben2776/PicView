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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Point = Avalonia.Point;

namespace PicView.Avalonia.Views;

public partial class ImageViewer : UserControl
{
    private static ScaleTransform? _scaleTransform;
    private static TranslateTransform? _translateTransform;

    private static Point _start;
    private static Point _origin;

    private bool _captured;

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
            if (!SettingsHelper.Settings.Zoom.ScrollEnabled)
            {
                await LoadNextPic();
            }
            else
            {
                if (reverse)
                {
                    ImageScrollViewer.LineUp();
                }
                else
                {
                    ImageScrollViewer.LineDown();
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

    internal void PanImage(object sender, PointerEventArgs e)
    {
        //if (_scaleTransform.ScaleX is 1)
        //{
        //    return;
        //}

        //// Drag image by modifying X,Y coordinates
        //var dragMousePosition = _start - e.GetPosition(this);

        //var newXproperty = _origin.X - dragMousePosition.X;
        //var newYproperty = _origin.Y - dragMousePosition.Y;

        //// Keep panning it in bounds

        //var actualScrollWidth = Bounds.Width;
        //var actualBorderWidth = ImageZoomBorder.Width;
        //var actualScrollHeight = Bounds.Height;
        //var actualBorderHeight = ImageZoomBorder.Height;

        //var isXOutOfBorder = actualScrollWidth < actualBorderWidth * _scaleTransform.ScaleX;
        //var isYOutOfBorder = actualScrollHeight < actualBorderHeight * _scaleTransform.ScaleY;
        //var maxX = actualScrollWidth - actualBorderWidth * _scaleTransform.ScaleX;
        //var maxY = actualScrollHeight - actualBorderHeight * _scaleTransform.ScaleY;

        //if (isXOutOfBorder && newXproperty < maxX || isXOutOfBorder == false && newXproperty > maxX)
        //{
        //    newXproperty = maxX;
        //}

        //if (isXOutOfBorder && newYproperty < maxY || isXOutOfBorder == false && newYproperty > maxY)
        //{
        //    newYproperty = maxY;
        //}

        //if (isXOutOfBorder && newXproperty > 0 || isXOutOfBorder == false && newXproperty < 0)
        //{
        //    newXproperty = 0;
        //}

        //if (isYOutOfBorder && newYproperty > 0 || isYOutOfBorder == false && newYproperty < 0)
        //{
        //    newYproperty = 0;
        //}

        //// TODO Don't pan image out of screen border
        //_translateTransform.X = newXproperty;
        //_translateTransform.Y = newYproperty;

        //e.Handled = true;
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
        ZoomTo(e, currentZoom, true);
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

        var newTranslateValueX = Math.Abs(zoomValue - 1) > .1 ? absoluteX - point.X * zoomValue : 0;
        var newTranslateValueY = Math.Abs(zoomValue - 1) > .1 ? absoluteY - point.Y * zoomValue : 0;
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _scaleTransform.ScaleX = zoomValue;
            _scaleTransform.ScaleY = zoomValue;
            _translateTransform.X = newTranslateValueX;
            _translateTransform.Y = newTranslateValueY;
        }, DispatcherPriority.Normal);
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
    }

    public void Pan(PointerEventArgs e, bool enableAnimations)
    {
        if (!_captured || _scaleTransform == null)
        {
            return;
        }

        if (_translateTransform.X is 0)
        {
            return;
        }

        var v = _start - e.GetPosition(ImageZoomBorder);

        if (enableAnimations)
        {
            _translateTransform.Transitions ??=
            [
                new DoubleTransition { Property = TranslateTransform.XProperty, Duration = TimeSpan.FromSeconds(.20) },
                new DoubleTransition { Property = TranslateTransform.YProperty, Duration = TimeSpan.FromSeconds(.20) }
            ];
        }
        else
        {
            _translateTransform.Transitions = null;
        }
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _translateTransform.X = _origin.X - v.X;
            _translateTransform.Y = _origin.Y - v.Y;
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
        _start = e.GetPosition(ImageZoomBorder);
        _origin = new Point(_translateTransform.X, _translateTransform.Y);
        _captured = true;
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