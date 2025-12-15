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
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Exif;
using PicView.Core.Localization;

namespace PicView.Avalonia.ImageTransformations.Rotation;

public class RotationTransformer(
    LayoutTransformControl imageLayoutTransformControl,
    PicBox mainImage,
    Func<object?> getDataContext,
    Action resetZoom)
{
    public async Task RotateAsync(double angle)
    {
        if (getDataContext() is not MainViewModel vm || mainImage.Source is null)
            return;

        if (vm.PicViewer.ImageSource.Value is not Bitmap bmp)
            return;

        
        bool clockWise = angle > 0;
        var desc = $"{TranslationManager.Translation.Rotated} {(clockWise ? TranslationManager.Translation.Right : TranslationManager.Translation.Left)} {angle.ToString().Replace("-", "")}°";

        await using (DebouncedLoadingScope.Start(vm.MainWindow.IsLoadingIndicatorShown, 150))
        {
            var rotated = await Task.Run(() => RotateBitmap((Bitmap)vm.PicViewer.ImageSource.Value, angle));

            // Add to History
            await vm.HistoryManager.AddSnapshot(EditKind.Rotate, desc, rotated).ConfigureAwait(false);

            // Apply to the PicViewer
            await Dispatcher.UIThread.InvokeAsync(() => vm.ImageViewer.ApplyBitmapAndRefresh(rotated, vm));
        }
    }

    private static Bitmap RotateBitmap(Bitmap source, double angle)
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

        var desc = $"{TranslationManager.Translation.Flipped} {(horizontal ? TranslationManager.Translation.Horizontal : TranslationManager.Translation.Vertical)}";

        await using (DebouncedLoadingScope.Start(vm.MainWindow.IsLoadingIndicatorShown, 150))
        {
            var flipped = await Task.Run(() => FlipBitmap(bmp, horizontal));

            // Add to History
            await vm.HistoryManager.AddSnapshot(horizontal ? EditKind.FlipH : EditKind.FlipV, desc, flipped).ConfigureAwait(false);

            // Apply to the PicViewer
            await Dispatcher.UIThread.InvokeAsync(() => vm.ImageViewer.ApplyBitmapAndRefresh(flipped, vm));
        }
    }
    
    private static Bitmap FlipBitmap(Bitmap source, bool horizontal)
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
}