using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace PicView.Avalonia.Models;

internal static class SkiaExtensions
{
    private record SKBitmapDrawOperation : ICustomDrawOperation
    {
        public Rect Bounds { get; set; }

        public SKBitmap? Bitmap { get; init; }

        public void Dispose()
        {
            Bitmap?.Dispose();
        }

        public bool Equals(ICustomDrawOperation? other) => false;

        public bool HitTest(Point p) => Bounds.Contains(p);

        public void Render(ImmediateDrawingContext context)
        {
            if (Bitmap is null || context.PlatformImpl.GetFeature<ISkiaSharpApiLeaseFeature>() is not { } leaseFeature)
            {
                return;
            }

            var lease = leaseFeature.Lease();
            using (lease)
            {
                lease.SkCanvas.DrawBitmap(Bitmap, SKRect.Create((float)Bounds.X, (float)Bounds.Y, (float)Bounds.Width, (float)Bounds.Height));
            }
        }
    }

    private class AvaloniaImage : IImage, IDisposable
    {
        private readonly SKBitmap? _source;
        private SKBitmapDrawOperation? _drawImageOperation;

        public AvaloniaImage(SKBitmap? source)
        {
            _source = source;
            if (source?.Info.Size is { } size)
            {
                Size = new Size(size.Width, size.Height);
            }
        }

        public Size Size { get; }

        public void Dispose()
        {
            _source?.Dispose();
            _drawImageOperation?.Dispose();
        }

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
            _drawImageOperation ??= new SKBitmapDrawOperation()
            {
                Bitmap = _source,
            }; ;
            _drawImageOperation.Bounds = sourceRect;
            context.Custom(_drawImageOperation);
        }
    }

    public static SKBitmap? ToSKBitmap(this Stream? stream)
    {
        return stream == null ? null : SKBitmap.Decode(stream);
    }

    public static IImage? ToAvaloniaImage(this SKBitmap? bitmap)
    {
        return bitmap is not null ? new AvaloniaImage(bitmap) : default(IImage?);
    }
}