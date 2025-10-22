using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
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

        var magick = vm.PicViewer.MagickFrame.Value;
        if (magick is null)
            return;

        var angle = clockWise ? 90 : -90;
        var desc = clockWise ? "Rotated right 90°" : "Rotated left 90°";

        // Show loading indicator while rotating
        vm.MainWindow.IsLoadingIndicatorShown.Value = true;

        // Do the heavy Magick rotation in a background thread
        var rotated = await Task.Run(() =>
        {
            var clone = magick.Clone();
            clone.Rotate(angle);
            clone.Orientation = OrientationType.TopLeft;
            return clone;
        });

        try
        {
            // Create a new history step (and generate thumbnail) in background
            await Task.Run(() =>
            {
                vm.History?.AddStep(EditKind.Rotate, desc, (MagickImage)rotated);
            });

            // Convert to Avalonia Bitmap off-thread
            var bitmap = await Task.Run(() => rotated.ToAvaloniaBitmap());

            // Apply the rotated image on the UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Replace viewer bitmap safely
                if (vm.PicViewer.ImageSource.Value is Bitmap oldBmp)
                    oldBmp.Dispose();

                vm.PicViewer.MagickFrame.Value = (MagickImage)rotated.Clone();
                vm.PicViewer.ImageSource.Value = bitmap;


                vm.PicViewer.HasChanges.Value = true;

                // Reset transform & zoom (rotation now baked into pixels)
                imageLayoutTransformControl.LayoutTransform = new RotateTransform(0);
                resetZoom?.Invoke();

                WindowResizing.SetSize(vm);
                mainImage.InvalidateVisual();
            });
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

        var magick = vm.PicViewer.MagickFrame.Value;
        if (magick is null)
            return;

        var desc = $"Rotated {angle}°";

        // Show loading indicator
        vm.MainWindow.IsLoadingIndicatorShown.Value = true;

        try
        {
            // Perform heavy rotation off the UI thread
            var rotated = await Task.Run(() =>
            {
                var clone = magick.Clone();
                clone.Rotate(angle);
                clone.Orientation = OrientationType.TopLeft;
                return clone;
            });

            // Commit to history (off-thread)
            await Task.Run(() =>
            {
                vm.History?.AddStep(EditKind.Rotate, desc, (MagickImage)rotated);
            });

            // Convert to Avalonia bitmap (off-thread)
            var bitmap = await Task.Run(() => rotated.ToAvaloniaBitmap());

            // Apply updates on UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Dispose previous image if it's a Bitmap
                if (vm.PicViewer.ImageSource.Value is Bitmap oldBmp)
                    oldBmp.Dispose();

                vm.PicViewer.MagickFrame.Value = (MagickImage)rotated.Clone();
                vm.PicViewer.ImageSource.Value = bitmap;
                vm.PicViewer.HasChanges.Value = true;

                // Reset any layout transform — rotation now baked into pixels
                imageLayoutTransformControl.LayoutTransform = new RotateTransform(0);
                resetZoom?.Invoke();

                WindowResizing.SetSize(vm);
                mainImage.InvalidateVisual();
            });
        }
        finally
        {
            vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        }
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
    public async Task FlipAsync(bool horizontal)
{
    if (getDataContext() is not MainViewModel vm || mainImage.Source is null)
        return;

    var magick = vm.PicViewer.MagickFrame.Value;
    if (magick is null)
        return;

    var desc = horizontal ? "Flipped horizontally" : "Flipped vertically";
    vm.MainWindow.IsLoadingIndicatorShown.Value = true;

    try
    {
        // Flip off the UI thread using Magick.NET
        var flipped = await Task.Run(() =>
        {
            var clone = magick.Clone();

            if (horizontal)
                clone.Flop(); // Mirror horizontally
            else
                clone.Flip(); // Mirror vertically

            clone.Orientation = OrientationType.TopLeft;
            return clone;
        });

        // Add as a history step
        await Task.Run(() =>
        {
            vm.History?.AddStep(EditKind.Flip, desc, (MagickImage)flipped);
        });

        // Convert to Avalonia Bitmap off-thread
        var bitmap = await Task.Run(() => flipped.ToAvaloniaBitmap());

        // Update the viewer
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Dispose previous image if necessary
            if (vm.PicViewer.ImageSource.Value is Bitmap oldBmp)
                oldBmp.Dispose();

            vm.PicViewer.MagickFrame.Value = (MagickImage)flipped.Clone();
            vm.PicViewer.ImageSource.Value = bitmap;
            vm.PicViewer.HasChanges.Value = true;

            // Reset transforms — no need for ScaleX hacks now
            imageLayoutTransformControl.RenderTransform = null;
            imageLayoutTransformControl.LayoutTransform = new RotateTransform(0);

            resetZoom?.Invoke();
            WindowResizing.SetSize(vm);
            mainImage.InvalidateVisual();
        });
    }
    finally
    {
        vm.MainWindow.IsLoadingIndicatorShown.Value = false;
    }
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