using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.ImageTransformations;
using PicView.Core.Navigation;
using ReactiveUI;
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
        // TODO: Make a zoom and pan custom control that will work for MVVM
        
        InitializeComponent();
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
        this.WhenAnyValue(x => x.MainImage.Source).Select(x => x is not null).Subscribe(x =>
        {
            if (SettingsHelper.Settings.Zoom.ScrollEnabled)
            {
                ImageScrollViewer.ScrollToHome();
            }

            Reset();
        });
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

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Use touch gestures for zooming on macOS
            return;
        }
        var ctrl = MainKeyboardShortcuts.CtrlDown;
        var shift = MainKeyboardShortcuts.ShiftDown;
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
            var navigateTo = SettingsHelper.Settings.Zoom.HorizontalReverseScroll ? NavigateTo.Previous : NavigateTo.Next;
            if (reverse)
            {
                navigateTo = SettingsHelper.Settings.Zoom.HorizontalReverseScroll ? NavigateTo.Next : NavigateTo.Previous;
            }

            await mainViewModel.ImageIterator.NextIteration(navigateTo).ConfigureAwait(false);
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
        var point = new Point(Bounds.Width / 2, Bounds.Height / 2);
        ZoomTo(point, true);
    }

    public void ZoomOut()
    {
        var point = new Point(Bounds.Width / 2, Bounds.Height / 2);
        ZoomTo(point, false);
    }

    public void ZoomTo(Point point, bool isZoomIn)
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
        currentZoom = Math.Max(0.09, currentZoom); // Fix for zooming out too much
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
        SetTitleHelper.SetTitle(vm);
        _isZoomed = false;
    }

    public void Reset()
    {
        if (DataContext is not MainViewModel vm)
            return;
        
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
            if (vm.ScaleX == -1)
            {
                var flipTransform = new ScaleTransform(vm.ScaleX, 1);
                MainImage.RenderTransform = flipTransform;
            }
            else
            {
                MainImage.RenderTransform = null;
            }
        }
    }
    
    private void Capture(PointerEventArgs e)
    {
        if (_captured)
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

        Dispatcher.UIThread.Invoke(() =>
        {
            _translateTransform.Transitions = null;
            _translateTransform.X = newXproperty;
            _translateTransform.Y = newYproperty;
        });
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

        if (MainKeyboardShortcuts.IsKeyHeldDown)
        {
            vm.RotationAngle = clockWise ? vm.RotationAngle + 1 : vm.RotationAngle -1;
            if (vm.RotationAngle < 0)
            {
                vm.RotationAngle = 359;
            }

            if (vm.RotationAngle > 359)
            {
                vm.RotationAngle = 0;
            }
        }
        else
        {
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

        WindowHelper.SetSize(vm);
    }
    
    public void Rotate(double angle)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var rotateTransform = new RotateTransform(angle);
            ImageLayoutTransformControl.LayoutTransform = rotateTransform;
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
            vm.GetFlipped = vm.UnFlip;
        }
        else
        {
            prevScaleX = -1;
            vm.ScaleX = 1;
            vm.GetFlipped = vm.Flip;
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
    
    public void SetScaleX()
    {
        if (DataContext is not MainViewModel vm)
            return;
        if (MainImage.Source is null)
        {
            return;
        }
        var flipTransform = new ScaleTransform(vm.ScaleX, 1);
        MainImage.RenderTransform = flipTransform;
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