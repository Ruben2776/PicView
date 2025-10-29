using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Utilities;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Extensions;
using PicView.Avalonia.History;
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
    public async Task RotateAsync(bool clockWise)
    {
        if (getDataContext() is not MainViewModel vm || mainImage.Source is null)
            return;

        if (vm.PicViewer.ImageSource.Value is not Bitmap bmp)
            return;

        var angle = clockWise ? 90 : -90;
        var desc = clockWise ? "Rotated right 90°" : "Rotated left 90°";

        vm.MainWindow.IsLoadingIndicatorShown.Value = true;

        try
        {
            var rotated = await Task.Run(() => RotateBitmap((Bitmap)vm.PicViewer.ImageSource.Value, 90));

            // Create a new history snapshot using the Bitmap
            await vm.HistoryManager.AddSnapshot(EditKind.Rotate, desc, rotated).ConfigureAwait(false);

            // Apply to the PicViewer on the UI thread
            await Dispatcher.UIThread.InvokeAsync(() => vm.ImageViewer.ApplyBitmapAndRefresh(rotated, vm));
        }
        finally
        {
            vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        }
    }

    public async Task RotateAsync(double angle)
    {
        if (getDataContext() is not MainViewModel vm || mainImage.Source is null)
            return;

        if (vm.PicViewer.ImageSource.Value is not Bitmap bmp)
            return;

        var desc = $"Rotated {angle}°";

        // Show loading indicator
        vm.MainWindow.IsLoadingIndicatorShown.Value = true;

        try
        {
            var rotated = await Task.Run(() => RotateBitmap((Bitmap)vm.PicViewer.ImageSource.Value, angle));

            // Create a new history snapshot using the Bitmap
            await vm.HistoryManager.AddSnapshot(EditKind.Rotate, desc, rotated).ConfigureAwait(false);

            // Apply updates on UI thread
            await Dispatcher.UIThread.InvokeAsync(() => vm.ImageViewer.ApplyBitmapAndRefresh(rotated, vm));
        }
        finally
        {
            vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        }
    }

    public static Bitmap RotateBitmap(Bitmap source, double angle)
    {
        var isRightAngle = (Math.Abs(angle) % 180) == 90;

        var size = isRightAngle
            ? new PixelSize(source.PixelSize.Height, source.PixelSize.Width)
            : new PixelSize(source.PixelSize.Width, source.PixelSize.Height);

        var target = new RenderTargetBitmap(size);

        using (var ctx = target.CreateDrawingContext())
        {
            // Translate to center → rotate → translate back
            var transform = Matrix.CreateTranslation(-source.PixelSize.Width / 2, -source.PixelSize.Height / 2)
                            * Matrix.CreateRotation(MathUtilities.Deg2Rad(angle))
                            * Matrix.CreateTranslation(size.Width / 2, size.Height / 2);

            using (ctx.PushTransform(transform))
            {
                ctx.DrawImage(
                    source,
                    new Rect(0, 0, source.PixelSize.Width, source.PixelSize.Height),
                    new Rect(0, 0, source.PixelSize.Width, source.PixelSize.Height));
            }
        }

        return target;
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

    public async Task FlipAsync(bool horizontal)
    {
        if (getDataContext() is not MainViewModel vm || mainImage.Source is null)
            return;

        if (vm.PicViewer.ImageSource.Value is not Bitmap bmp)
            return;

        var desc = horizontal ? "Flipped horizontally" : "Flipped vertically";
        vm.MainWindow.IsLoadingIndicatorShown.Value = true;

        try
        {
            var flipped = await Task.Run(() => FlipBitmap(bmp, horizontal));

            // Create a new history snapshot using the Bitmap
            await vm.HistoryManager.AddSnapshot(EditKind.Flip, desc, flipped).ConfigureAwait(false);

            // Update the viewer
            await Dispatcher.UIThread.InvokeAsync(() => vm.ImageViewer.ApplyBitmapAndRefresh(flipped, vm));
        }
        finally
        {
            vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        }
    }
    
    public static Bitmap FlipBitmap(Bitmap source, bool horizontal)
{
    var size   = source.PixelSize;
    var target = new RenderTargetBitmap(size);

    using (var ctx = target.CreateDrawingContext())
    {
        var m = Matrix.Identity;
        if (horizontal)
        {
            m *= Matrix.CreateScale(-1, 1);
            m *= Matrix.CreateTranslation(size.Width, 0);
        }
        else
        {
            m *= Matrix.CreateScale(1, -1);
            m *= Matrix.CreateTranslation(0, size.Height);
        }

        using (ctx.PushTransform(m))
        {
            ctx.DrawImage(source,
                new Rect(0, 0, size.Width, size.Height),
                new Rect(0, 0, size.Width, size.Height));
        }
    }

    return target;
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