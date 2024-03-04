using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.ImageTransformations;
using PicView.Core.Navigation;
using System.Runtime.InteropServices;
using Point = Avalonia.Point;

namespace PicView.Avalonia.Views;

public partial class ImageViewer : UserControl
{
    private static ScaleTransform? _scaleTransform;
    private static TranslateTransform? _translateTransform;

    private static Point _origin;
    private static Point _start;

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
                //ImageZoomBorder.Zoom(e);
                //return;
                SetPointerPosition();
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
                SetPointerPosition();
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

        void SetPointerPosition()
        {
            e.Pointer.Capture(this);
            _start = e.GetPosition(this);
            var x = _translateTransform?.X ?? 0;
            var y = _translateTransform?.Y ?? 0;
            _origin = new Point(x, y);
        }

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
    }

    public void ZoomIn(PointerWheelEventArgs e)
    {
        ZoomTo(e, true);
    }

    public void ZoomOut(PointerWheelEventArgs e)
    {
        ZoomTo(e, false);
    }

    public void ZoomTo(double zoomValue)
    {
        var absoluteX = _start.X * _scaleTransform.ScaleX + _translateTransform.X;
        var absoluteY = _start.Y * _scaleTransform.ScaleY + _translateTransform.Y;

        var newTranslateValueX = Math.Abs(zoomValue - 1) > .1 ? absoluteX - _start.X * zoomValue : 0;
        var newTranslateValueY = Math.Abs(zoomValue - 1) > .1 ? absoluteY - _start.Y * zoomValue : 0;
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _scaleTransform.ScaleX = zoomValue;
            _scaleTransform.ScaleY = zoomValue;
            _translateTransform.X = newTranslateValueX;
            _translateTransform.Y = newTranslateValueY;
        }, DispatcherPriority.Normal);

        //var duration = TimeSpan.FromSeconds(.25);

        //var anim = new Animation
        //{
        //    Duration = duration,
        //    Children =
        //    {
        //        new KeyFrame
        //        {
        //            Cue = Cue.Parse(zoomValue.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
        //            Setters =
        //            {
        //                new Setter { Property = ScaleTransform.ScaleXProperty, Value = zoomValue },
        //                new Setter { Property = ScaleTransform.ScaleYProperty, Value = zoomValue },
        //                new Setter { Property = TranslateTransform.XProperty, Value = newTranslateValueX },
        //                new Setter { Property = TranslateTransform.YProperty, Value = newTranslateValueY },
        //            },
        //        }
        //    }
        //};
        //anim.RunAsync(ImageZoomBorder);
        //while (anim.IsAnimating(ScaleTransform.ScaleXProperty))
        //{
        //}
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
        ZoomTo(currentZoom);
    }

    public void ResetZoom(bool animate)
    {
        if (animate)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                Set();
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(Set, DispatcherPriority.Normal);
            }
        }
        else
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                Set();
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(Set, DispatcherPriority.Normal);
            }
        }

        return;

        void Set()
        {
            _scaleTransform.ScaleX = 1;
            _scaleTransform.ScaleY = 1;
            _translateTransform.X = 0;
            _translateTransform.Y = 0;
        }
    }

    #endregion Zoom

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ResetZoom(false);
        }
    }
}