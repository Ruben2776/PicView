using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Exif;
using PicView.Core.ImageTransformations;

namespace PicView.Avalonia.ImageTransformations.Rotation;

public class RotationTransformer(
    LayoutTransformControl imageLayoutTransformControl,
    PicBox mainImage,
    Func<object?> getDataContext,
    Action resetZoom)
{
    public void Rotate(bool clockWise)
    {
        if (getDataContext() is not MainViewModel vm || mainImage.Source is null)
        {
            return;
        }

        if (RotationHelper.IsValidRotation(vm.PicViewer.RotationAngle.CurrentValue))
        {
            var nextAngle = RotationHelper.Rotate(vm.PicViewer.RotationAngle.CurrentValue, clockWise);
            vm.PicViewer.RotationAngle.Value = nextAngle switch
            {
                360 => 0,
                -90 => 270,
                _ => nextAngle
            };
        }
        else
        {
            vm.PicViewer.RotationAngle.Value =
                RotationHelper.NextRotationAngle(vm.PicViewer.RotationAngle.CurrentValue, true);
        }

        SetImageLayoutTransform(new RotateTransform(vm.PicViewer.RotationAngle.CurrentValue));
        WindowResizing.SetSize(vm);
        mainImage.InvalidateVisual();
    }

    public void Rotate(double angle)
    {
        SetImageLayoutTransform(new RotateTransform(angle));
        WindowResizing.SetSize(getDataContext() as MainViewModel);
        mainImage.InvalidateVisual();
    }

    private void SetImageLayoutTransform(RotateTransform rotateTransform)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            imageLayoutTransformControl.LayoutTransform = rotateTransform;
        }
        else
        {
            Dispatcher.UIThread.Invoke(() =>
                imageLayoutTransformControl.LayoutTransform = rotateTransform);
        }
    }

    private ScaleTransform? _scaleTransform;
    public void Flip(bool animate)
    {
        if (getDataContext() is not MainViewModel vm || mainImage.Source is null)
        {
            return;
        }
        
        _scaleTransform ??= new ScaleTransform();

        var prevScaleX = vm.PicViewer.ScaleX.CurrentValue;
        var newScaleX = prevScaleX == -1 ? 1 : -1;

        if (animate)
        {
            _scaleTransform.Transitions ??=
            [
                new DoubleTransition
                {
                    Property = ScaleTransform.ScaleXProperty,
                    Duration = TimeSpan.FromSeconds(.2)
                }
            ];
        }
        else
        {
            _scaleTransform.Transitions = null;
        }
        imageLayoutTransformControl.RenderTransform = _scaleTransform;
        _scaleTransform.ScaleX = newScaleX;
    }

    public void SetTransform(int scaleX, int rotationAngle)
    {
        if (getDataContext() is not MainViewModel vm)
        {
            return;
        }

        vm.PicViewer.ScaleX.Value = scaleX;
        vm.PicViewer.RotationAngle.Value = rotationAngle;
        imageLayoutTransformControl.RenderTransform = new ScaleTransform(vm.PicViewer.ScaleX.CurrentValue, 1);
        imageLayoutTransformControl.LayoutTransform = new RotateTransform(rotationAngle);

        resetZoom?.Invoke();
    }

    public void SetTransform(ExifOrientation? orientation, MagickFormat? format, bool reset = true)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ApplyOrientationTransform(orientation, format, reset);
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(() =>
                ApplyOrientationTransform(orientation, format, reset), DispatcherPriority.Send);
        }
    }

    private void ApplyOrientationTransform(ExifOrientation? orientation, MagickFormat? format, bool reset)
    {
        if (Settings.Zoom.ScrollEnabled && imageLayoutTransformControl.Parent is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToHome();
        }

        if (format is MagickFormat.Heic or MagickFormat.Heif)
        {
            if (reset)
            {
                SetTransform(1, 0);
            }

            return;
        }

        switch (orientation)
        {
            case null:
            case ExifOrientation.None:
            case ExifOrientation.Horizontal:
                if (reset)
                {
                    SetTransform(1, 0);
                }

                break;
            case ExifOrientation.MirrorHorizontal:
                SetTransform(-1, 0);
                break;
            case ExifOrientation.Rotate180:
                SetTransform(1, 180);
                break;
            case ExifOrientation.MirrorVertical:
                SetTransform(-1, 180);
                break;
            case ExifOrientation.MirrorHorizontalRotate270Cw:
                SetTransform(-1, 90);
                break;
            case ExifOrientation.Rotate90Cw:
                SetTransform(1, 90);
                break;
            case ExifOrientation.MirrorHorizontalRotate90Cw:
                SetTransform(-1, 270);
                break;
            case ExifOrientation.Rotated270Cw:
                SetTransform(1, 270);
                break;
        }
    }
}