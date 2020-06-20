using PicView.UI.Animations;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows.Media.Imaging;

namespace PicView.ImageHandling
{
    internal static class DisplayUnsupportedImage
    {
        internal static WriteableBitmap DrawUnsupportedImageText()
        {
            var info = new SKImageInfo(1024, 1024);
            using var surface = SKSurface.Create(info);
            SKCanvas canvas = surface.Canvas;

            using var sKBitmap = new SKBitmap(info);

            canvas.Clear(SKColors.White);

            var color = AnimationHelper.GetPrefferedColorDown();

            // draw centered text, stroked
            using var paint = new SKPaint
            {
                TextSize = 64.0f,
                IsAntialias = true,
                Color = new SKColor(color.R, color.G, color.B),
                TextAlign = SKTextAlign.Center,
            };

            float x = (info.Width - sKBitmap.Width) / 2;
            float y = (info.Height - sKBitmap.Height) / 2;

            canvas.DrawText("Unable to render image", 500, 100, paint);

            canvas.Flush();

            canvas.DrawBitmap(sKBitmap, x, y);

            canvas.Dispose();

            var writeableBitmap = sKBitmap.ToWriteableBitmap();
            writeableBitmap.Freeze();
            return writeableBitmap;

        }
    }
}
