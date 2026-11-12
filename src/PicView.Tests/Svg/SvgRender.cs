using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Avalonia.Svg;
using PicView.Core.Svg;

namespace PicView.Tests.Svg;

/// <summary>Renders SVG text through the parser and <see cref="DrawingImageBuilder" /> with real Skia.</summary>
internal static class SvgRender
{
    /// <summary>
    /// Parses and builds <paramref name="svgText" />, draws the image at its own size at scale 1 on a transparent
    /// background, and returns the straight (non-premultiplied) color at the pixel.
    /// </summary>
    public static Color Pixel(string svgText, int x, int y)
    {
        return Pixels(svgText).At(x, y);
    }

    /// <summary>Like <see cref="Pixel" />, for reading several pixels of one rendering.</summary>
    public static RenderedPixels Pixels(string svgText)
    {
        var result = SvgParser.Parse(svgText);
        Assert.Null(result.UnsupportedReason);
        Assert.NotNull(result.Drawing);
        var image = DrawingImageBuilder.Build(result.Drawing);

        var size = new PixelSize((int)Math.Ceiling(image.Size.Width), (int)Math.Ceiling(image.Size.Height));
        using var bitmap = new RenderTargetBitmap(size, new Vector(96, 96));
        using (var context = bitmap.CreateDrawingContext())
        {
            context.DrawImage(image, new Rect(image.Size));
        }

        var stride = size.Width * 4;
        var buffer = new byte[stride * size.Height];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(new PixelRect(size), handle.AddrOfPinnedObject(), buffer.Length, stride);
        }
        finally
        {
            handle.Free();
        }

        var format = bitmap.Format ?? PixelFormat.Bgra8888;
        Assert.True(format == PixelFormat.Bgra8888 || format == PixelFormat.Rgba8888, $"Unexpected format {format}");
        return new RenderedPixels(buffer, size.Width, format == PixelFormat.Rgba8888,
            bitmap.AlphaFormat != AlphaFormat.Unpremul);
    }

    internal sealed class RenderedPixels(byte[] buffer, int width, bool isRgba, bool isPremultiplied)
    {
        public Color At(int x, int y)
        {
            var i = (y * width + x) * 4;
            var a = buffer[i + 3];
            var r = isRgba ? buffer[i] : buffer[i + 2];
            var g = buffer[i + 1];
            var b = isRgba ? buffer[i + 2] : buffer[i];
            if (isPremultiplied && a is > 0 and < 255)
            {
                r = Unpremultiply(r, a);
                g = Unpremultiply(g, a);
                b = Unpremultiply(b, a);
            }

            return Color.FromArgb(a, r, g, b);
        }

        private static byte Unpremultiply(byte channel, byte alpha)
        {
            return (byte)Math.Min(255, Math.Round(channel * 255.0 / alpha));
        }
    }
}
