using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.ImageDecoding;
using PicView.Core.ImageTransformations;
using Point = Avalonia.Point;

namespace PicView.Avalonia.Views;

public partial class ImageViewer : UserControl
{
    private static ScaleTransform? _scaleTransform;
    private static TranslateTransform? _translateTransform;

    private static Point _start;
    private static Point _origin;
    private static Point _current;

    private bool _captured;
    private bool _isZoomed;

    public ImageViewer()
    {
        InitializeComponent();
        TriggerScalingModeUpdate(false);
        AddHandler(PointerWheelChangedEvent, PreviewOnPointerWheelChanged, RoutingStrategies.Tunnel);
        AddHandler(Gestures.PointerTouchPadGestureMagnifyEvent, TouchMagnifyEvent, RoutingStrategies.Bubble);

        Loaded += delegate
        {
            InitializeZoom();
            LostFocus += (_, _) =>
            {
                _captured = false;
            };
        };
    }
    
    public void TriggerScalingModeUpdate(bool invalidate)
    {
        var scalingMode = SettingsHelper.Settings.ImageScaling.IsScalingSetToNearestNeighbor 
            ? BitmapInterpolationMode.LowQuality 
            : BitmapInterpolationMode.HighQuality;

        RenderOptions.SetBitmapInterpolationMode(MainImage,scalingMode);
        if (invalidate)
        {
            MainImage.InvalidateVisual();
        }
    }

    private void TouchMagnifyEvent(object? sender, PointerDeltaEventArgs e)
    {
        ZoomTo(e.GetPosition(this), e.Delta.X > 0);
    }

    public async Task PreviewOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        e.Handled = true;
        await Main_OnPointerWheelChanged(e);
    }
    
    private async Task Main_OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel)
            return;

        if (SettingsHelper.Settings.Zoom.IsUsingTouchPad)
        {
            // Use touch gestures for zooming
            return;
        }
        var ctrl = e.KeyModifiers == KeyModifiers.Control;
        var shift = e.KeyModifiers == KeyModifiers.Shift;
        var reverse = e.Delta.Y < 0;
        
        if (SettingsHelper.Settings.Zoom.ScrollEnabled)
        {
            if (!shift)
            {
                if (ctrl && !SettingsHelper.Settings.Zoom.CtrlZoom)
                {
                    await LoadNextPic();
                    return;
                }
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
                return;
            }
            
        }

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
            if (!NavigationHelper.CanNavigate(mainViewModel))
            {
                return;
            }

            bool next;
            if (reverse)
            {
                next = SettingsHelper.Settings.Zoom.HorizontalReverseScroll;
            }
            else
            {
                next = !SettingsHelper.Settings.Zoom.HorizontalReverseScroll;
            }

            await NavigationHelper.Navigate(next, mainViewModel).ConfigureAwait(false);
        }
    }

    #region Zoom

    private void InitializeZoom()
    {
        MainBorder.RenderTransform = new TransformGroup
        {
            Children =
            [
                new ScaleTransform(),
                new TranslateTransform()
            ]
        };
        _scaleTransform = (ScaleTransform)((TransformGroup)MainBorder.RenderTransform)
            .Children.First(tr => tr is ScaleTransform);

        _translateTransform = (TranslateTransform)((TransformGroup)MainBorder.RenderTransform)
            .Children.First(tr => tr is TranslateTransform);
        MainBorder.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
        MainImage.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
    }

    public void ZoomIn(PointerWheelEventArgs e)
    {
        ZoomTo(e.GetPosition(ImageScrollViewer), true);
    }

    public void ZoomOut(PointerWheelEventArgs e)
    {
        ZoomTo(e.GetPosition(ImageScrollViewer), false);
    }

    public void ZoomIn()
    {
        ZoomTo(_current, true);
    }

    public void ZoomOut()
    {
        ZoomTo(_current, false);
    }

    public void ZoomTo(Point point, bool isZoomIn)
    {
        if (_scaleTransform == null || _translateTransform == null)
        {
            return;
        }
        var currentZoom = _scaleTransform.ScaleX;
        var zoomSpeed = SettingsHelper.Settings.Zoom.ZoomSpeed;
        
        switch (currentZoom)
        {
            // Increase speed based on the current zoom level
            case > 15 when isZoomIn:
                return;

            case > 4:
                zoomSpeed += 1;
                break;

            case > 3.2:
                zoomSpeed += 0.8;
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
        currentZoom = Math.Max(0.09, currentZoom); // Fix for zooming out too much
        TriggerScalingModeUpdate(false);
        if (SettingsHelper.Settings.Zoom.AvoidZoomingOut && currentZoom < 1.0)
        {
            ResetZoom(true);
        }
        else
        {
            ZoomTo(point, currentZoom, true);
        }
    }

    public void ZoomTo(Point point, double zoomValue, bool enableAnimations)
    {
        if (_scaleTransform == null || _translateTransform == null)
        {
            return;
        }
        if (DataContext is not MainViewModel vm)
            return;

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

        var absoluteX = point.X * _scaleTransform.ScaleX + _translateTransform.X;
        var absoluteY = point.Y * _scaleTransform.ScaleY + _translateTransform.Y;

        var newTranslateValueX = Math.Abs(zoomValue - 1) > .2 ? absoluteX - point.X * zoomValue : 0;
        var newTranslateValueY = Math.Abs(zoomValue - 1) > .2 ? absoluteY - point.Y * zoomValue : 0;
        
        _scaleTransform.ScaleX = zoomValue;
        _scaleTransform.ScaleY = zoomValue;
        _translateTransform.X = newTranslateValueX;
        _translateTransform.Y = newTranslateValueY;
        vm.ZoomValue = zoomValue;
        _isZoomed = zoomValue != 0;
        if (_isZoomed)
        {
            SetTitleHelper.SetTitle(vm);
            TooltipHelper.ShowTooltipMessage($"{Math.Floor(zoomValue * 100)}%", center: true);
        }
    }


    public void ResetZoom(bool enableAnimations)
    {
        if (_scaleTransform == null || _translateTransform == null)
        {
            return;
        }
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
        }, DispatcherPriority.Send);

        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        vm.ZoomValue = 1;
        vm.RotationAngle = 0;
        SetTitleHelper.SetTitle(vm);
        _isZoomed = false;
    }

    public void Reset()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            DoReset();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(DoReset);
        }
        return;
        
        void DoReset()
        {
            if (_isZoomed)
            {
                ResetZoom(false);
            }
            ImageLayoutTransformControl.LayoutTransform = null;
            MainImage.RenderTransform = null;
        }
    }
    
    private void Capture(PointerEventArgs e)
    {
        if (_captured)
        {
            return;
        }
        if (_scaleTransform == null || _translateTransform == null)
        {
            return;
        }

        var mainView = UIHelper.GetMainView;

        var point = e.GetCurrentPoint(mainView);
        var x = point.Position.X;
        var y = point.Position.Y;
        _start = new Point(x, y);
        _origin = new Point(_translateTransform.X, _translateTransform.Y);
        _captured = true;
    }
    
    public void Pan(PointerEventArgs e)
    {
        if (!_captured || _scaleTransform == null || !_isZoomed)
        {
            return;
        }

        var dragMousePosition = _start - e.GetPosition(this);
        
        var newXproperty = _origin.X - dragMousePosition.X;
        var newYproperty = _origin.Y - dragMousePosition.Y;

        if (SettingsHelper.Settings.WindowProperties.Fullscreen)
        {
            // TODO: figure out how to pan in fullscreen while keeping it in bounds
            _translateTransform.Transitions = null;
            _translateTransform.X = newXproperty;
            _translateTransform.Y = newYproperty;
            e.Handled = true;
            return;
        }
        
        var actualScrollWidth = ImageScrollViewer.Bounds.Width;
        var actualBorderWidth = MainBorder.Bounds.Width;
        var actualScrollHeight = ImageScrollViewer.Bounds.Height;
        var actualBorderHeight = MainBorder.Bounds.Height;

        var isXOutOfBorder = actualScrollWidth < actualBorderWidth * _scaleTransform.ScaleX;
        var isYOutOfBorder = actualScrollHeight < actualBorderHeight * _scaleTransform.ScaleY;
        var maxX = actualScrollWidth - actualBorderWidth * _scaleTransform.ScaleX;
        var maxY = actualScrollHeight - actualBorderHeight * _scaleTransform.ScaleY;
        
        if (isXOutOfBorder && newXproperty < maxX || isXOutOfBorder == false && newXproperty > maxX)
        {
            newXproperty = maxX;
        }

        if (isXOutOfBorder && newYproperty < maxY || isXOutOfBorder == false && newYproperty > maxY)
        {
            newYproperty = maxY;
        }

        if (isXOutOfBorder && newXproperty > 0 || isXOutOfBorder == false && newXproperty < 0)
        {
            newXproperty = 0;
        }

        if (isYOutOfBorder && newYproperty > 0 || isYOutOfBorder == false && newYproperty < 0)
        {
            newYproperty = 0;
        }

        _translateTransform.Transitions = null;
        _translateTransform.X = newXproperty;
        _translateTransform.Y = newYproperty;
        e.Handled = true;
    }

    #endregion Zoom

    #region Rotation and Flip

    public void Rotate(bool clockWise)
    {
        if (DataContext is not MainViewModel vm)
            return;
        if (MainImage.Source is null)
        {
            return;
        }
        if (RotationHelper.IsValidRotation(vm.RotationAngle))
        {
            var nextAngle = RotationHelper.Rotate(vm.RotationAngle, clockWise);
            vm.RotationAngle = nextAngle switch
            {
                360 => 0,
                -90 => 270,
                _ => nextAngle
            };
        }
        else
        {
            vm.RotationAngle = RotationHelper.NextRotationAngle(vm.RotationAngle, true);
        }

        var rotateTransform = new RotateTransform(vm.RotationAngle);

        if (Dispatcher.UIThread.CheckAccess())
        {
            ImageLayoutTransformControl.LayoutTransform = rotateTransform;
        }
        else
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                ImageLayoutTransformControl.LayoutTransform = rotateTransform;
            });
        }

        WindowResizing.SetSize(vm);
        MainImage.InvalidateVisual();
    }
    
    public void Rotate(double angle)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var rotateTransform = new RotateTransform(angle);
            ImageLayoutTransformControl.LayoutTransform = rotateTransform;
            
            WindowResizing.SetSize(DataContext as MainViewModel);
            MainImage.InvalidateVisual();
        });
    }

    public void Flip(bool animate)
    {
        if (DataContext is not MainViewModel vm)
            return;
        if (MainImage.Source is null)
        {
            return;
        }
        int prevScaleX;
        vm.ScaleX = vm.ScaleX == -1 ? 1 : -1;
        if (vm.ScaleX == 1)
        {
            prevScaleX = 1;
            vm.ScaleX = -1;
            vm.GetIsFlippedTranslation = vm.UnFlip;
        }
        else
        {
            prevScaleX = -1;
            vm.ScaleX = 1;
            vm.GetIsFlippedTranslation = vm.Flip;
        }
        
        if (animate)
        {
            var flipTransform = new ScaleTransform(prevScaleX, 1)
            {
                Transitions =
                [
                    new DoubleTransition { Property = ScaleTransform.ScaleXProperty, Duration = TimeSpan.FromSeconds(.2) },
                ]
            };
            ImageLayoutTransformControl.RenderTransform = flipTransform;
            flipTransform.ScaleX = vm.ScaleX;
        }
        else
        {
            var flipTransform = new ScaleTransform(vm.ScaleX, 1);
            ImageLayoutTransformControl.RenderTransform = flipTransform;
        }
    }
    
    public void SetTransform(int scaleX, int rotationAngle)
    {
        if (DataContext is not MainViewModel vm)
            return;

        vm.ScaleX = scaleX;
        vm.RotationAngle = rotationAngle;
        var flipTransform = new ScaleTransform(vm.ScaleX, 1);
        ImageLayoutTransformControl.RenderTransform = flipTransform;
        
        var rotateTransform = new RotateTransform(rotationAngle);
        ImageLayoutTransformControl.LayoutTransform = rotateTransform;
        
        if (_isZoomed)
        {
            ResetZoom(false);
        }
    }

    public void SetTransform(EXIFHelper.EXIFOrientation? orientation)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Set, DispatcherPriority.Send);
        }
        return;

        void Set()
        {
            if (SettingsHelper.Settings.Zoom.ScrollEnabled)
            {
                ImageScrollViewer.ScrollToHome();
            }

            switch (orientation)
            {
                case null:
                default:
                case EXIFHelper.EXIFOrientation.None:
                case EXIFHelper.EXIFOrientation.Horizontal:
                    Reset();
                    return;
                case EXIFHelper.EXIFOrientation.MirrorHorizontal:
                    SetTransform(-1, 0);
                    break;
                case EXIFHelper.EXIFOrientation.Rotate180:
                    SetTransform(1, 180);
                    break;
                case EXIFHelper.EXIFOrientation.MirrorVertical:
                    SetTransform(-1, 180);
                    break;
                case EXIFHelper.EXIFOrientation.MirrorHorizontalRotate270Cw:
                    SetTransform(-1, 90); // should be 270, but it's not working
                    break;
                case EXIFHelper.EXIFOrientation.Rotate90Cw:
                    SetTransform(1, 90);
                    break;
                case EXIFHelper.EXIFOrientation.MirrorHorizontalRotate90Cw:
                    SetTransform(-1, 270); // should be 90, but it's not working
                    break;
                case EXIFHelper.EXIFOrientation.Rotated270Cw:
                    SetTransform(1, 270);
                    break;
            }
        }
    }

    #endregion Rotation and Flip

    #region Events

    private void ImageScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        e.Handled = true;
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ResetZoom(true);
        }
    }

    private void MainImage_OnPointerPressed(object? sender, PointerPressedEventArgs e)
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

    private void MainImage_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        _current = e.GetPosition(this);
        Pan(e);
    }

    private void Pressed(PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }
        Capture(e);
    }

    private void MainImage_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _captured = false;
    }

    #endregion Events

    

    
}