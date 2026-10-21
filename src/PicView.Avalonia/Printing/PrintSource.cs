using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Core.Printing;
using PicView.Core.ViewModels;
using SkiaSharp;
using Orientation = PicView.Core.ImageTransformations.Orientation;

namespace PicView.Avalonia.Printing;

/// <summary>
/// The image a Print Job prints: the bitmap a tab shows, turned and flipped by the tab's Orientation. That bitmap
/// holds the pixels of the tab's Save Source: the Effects Render while there is one, else the file's, else the
/// Single Image's, so nothing is decoded again.
/// </summary>
/// <remarks>It is immutable, so previews and the sent page can draw it from several threads at once.</remarks>
internal sealed class PrintSource : IDisposable
{
    // A preview is drawn from a copy no larger than this, so changing a setting re-renders it quickly
    private const int PreviewMaxSide = 2400;

    private readonly SKImage _image;
    private readonly Orientation _orientation;
    private readonly Lock _previewLock = new();
    private SKImage? _previewImage;

    private PrintSource(SKImage image, Orientation orientation, string title)
    {
        _image = image;
        _orientation = orientation;
        Title = title;
        var isTurnedSideways = orientation.QuarterTurns % 2 == 1;
        Width = isTurnedSideways ? image.Height : image.Width;
        Height = isTurnedSideways ? image.Width : image.Height;
    }

    /// <summary>
    /// The width of the image as the tab shows it, turned, in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// The height of the image as the tab shows it, turned, in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// The name of what is printed: the file's name, else the tab's title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The image <paramref name="tab"/> shows now, or <see langword="null"/> when it shows no still image.
    /// </summary>
    public static PrintSource? From(TabViewModel tab)
    {
        if (tab.Image.CurrentValue is not Bitmap { PixelSize: { Width: > 0, Height: > 0 } size } bitmap)
        {
            return null;
        }

        var pixels = new SKBitmap(new SKImageInfo(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        try
        {
            bitmap.CopyPixels(new SkiaFramebuffer(pixels));
            var image = SKImage.FromPixelCopy(pixels.Info, pixels.GetPixels(), pixels.RowBytes);
            var title = tab.FileInfo.CurrentValue?.Name ?? tab.TabTitle.CurrentValue;
            return new PrintSource(image, tab.Orientation.Current.CurrentValue,
                string.IsNullOrWhiteSpace(title) ? "PicView" : title);
        }
        finally
        {
            pixels.Dispose();
        }
    }

    /// <summary>
    /// Draws the part <paramref name="source"/> of the turned image into <paramref name="destination"/>.
    /// </summary>
    /// <param name="source">Where to draw from, in pixels of the turned image.</param>
    /// <param name="destination">Where to draw to, in pixels of <paramref name="canvas"/>.</param>
    /// <param name="forPreview">Whether to draw from the smaller copy made for previews.</param>
    public void Draw(SKCanvas canvas, PrintRect source, PrintRect destination, SKPaint paint, bool forPreview)
    {
        var image = forPreview ? GetPreviewImage() : _image;
        var scaleX = destination.Width / source.Width;
        var scaleY = destination.Height / source.Height;

        canvas.Save();
        canvas.ClipRect(SKRect.Create((float)destination.X, (float)destination.Y, (float)destination.Width,
            (float)destination.Height));

        // From the turned image's source rectangle to the destination
        canvas.Translate((float)destination.X, (float)destination.Y);
        canvas.Scale((float)scaleX, (float)scaleY);
        canvas.Translate((float)-source.X, (float)-source.Y);

        // From the image's own pixels to the turned image: turn about the centre, then mirror as the view does
        canvas.Translate(Width / 2f, Height / 2f);
        canvas.Scale(_orientation.IsFlipped ? -1 : 1, 1);
        canvas.RotateDegrees(_orientation.Degrees);
        canvas.Translate(-_image.Width / 2f, -_image.Height / 2f);
        canvas.Scale((float)_image.Width / image.Width, (float)_image.Height / image.Height);

        // Shrinking needs mipmaps to stay smooth; enlarging looks best with a cubic filter
        var drawnScale = Math.Min(scaleX, scaleY) * _image.Width / image.Width;
        var sampling = drawnScale < 1
            ? new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)
            : new SKSamplingOptions(SKCubicResampler.Mitchell);
        canvas.DrawImage(image, 0, 0, sampling, paint);
        canvas.Restore();
    }

    private SKImage GetPreviewImage()
    {
        lock (_previewLock)
        {
            if (_previewImage is not null)
            {
                return _previewImage;
            }

            var longestSide = Math.Max(_image.Width, _image.Height);
            if (longestSide <= PreviewMaxSide)
            {
                return _image;
            }

            var factor = (double)PreviewMaxSide / longestSide;
            var info = new SKImageInfo(Math.Max(1, (int)(_image.Width * factor)),
                Math.Max(1, (int)(_image.Height * factor)), SKColorType.Bgra8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info);
            surface.Canvas.DrawImage(_image, SKRect.Create(info.Width, info.Height),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
            return _previewImage = surface.Snapshot();
        }
    }

    public void Dispose()
    {
        lock (_previewLock)
        {
            _previewImage?.Dispose();
            _previewImage = null;
        }

        _image.Dispose();
    }

    /// <summary>
    /// Lets Avalonia copy a bitmap into Skia's pixels, converting its pixel and alpha format on the way.
    /// </summary>
    private sealed class SkiaFramebuffer(SKBitmap pixels) : ILockedFramebuffer
    {
        public IntPtr Address => pixels.GetPixels();

        public PixelSize Size => new(pixels.Width, pixels.Height);

        public int RowBytes => pixels.RowBytes;

        public Vector Dpi => new(96, 96);

        public PixelFormat Format => PixelFormat.Bgra8888;

        public AlphaFormat AlphaFormat => AlphaFormat.Premul;

        public void Dispose()
        {
        }
    }
}
