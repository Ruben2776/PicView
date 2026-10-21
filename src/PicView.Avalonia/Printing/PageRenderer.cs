using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Core.Printing;
using SkiaSharp;

namespace PicView.Avalonia.Printing;

/// <summary>
/// Draws a Printed Page: white paper, and the Print Job's image placed inside the margins by the Scale Mode. A preview
/// and the sent page come from the same drawing, at their own DPI.
/// </summary>
internal static class PageRenderer
{
    /// <summary>
    /// The DPI a preview is drawn at, so it shows at the paper's size on a 100% screen.
    /// </summary>
    public const float PreviewDpi = 96f;

    // Rec. 709 luma, the gray a black and white printer would give
    private static readonly float[] GrayscaleMatrix =
    [
        0.2126f, 0.7152f, 0.0722f, 0, 0,
        0.2126f, 0.7152f, 0.0722f, 0, 0,
        0.2126f, 0.7152f, 0.0722f, 0, 0,
        0, 0, 0, 1, 0
    ];

    /// <summary>
    /// The preview of a Printed Page, with the paper's edge and the margins marked.
    /// </summary>
    public static WriteableBitmap RenderPreview(PrintSource? source, PrintSettings settings)
    {
        var page = Snapshot.Of(settings);
        var layout = Layout(source, page, settings, PreviewDpi);
        var (width, height) = PixelSizeOf(layout);
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(PreviewDpi, PreviewDpi),
            PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var framebuffer = bitmap.Lock())
        using (var surface = SKSurface.Create(
                   new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul),
                   framebuffer.Address, framebuffer.RowBytes))
        {
            Draw(surface.Canvas, layout, source, page.IsGrayscale, forPreview: true);
        }

        return bitmap;
    }

    /// <summary>
    /// The Printed Page at <see cref="PrintLayout.PrintDpi"/>, ready for a Printer.
    /// </summary>
    public static PrintedPage RenderForPrinter(PrintSource? source, PrintSettings settings)
    {
        var page = Snapshot.Of(settings);
        var layout = Layout(source, page, settings, PrintLayout.PrintDpi);
        var (width, height) = PixelSizeOf(layout);
        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bitmap))
        {
            Draw(canvas, layout, source, page.IsGrayscale, forPreview: false);
        }

        return new PrintedPage(bitmap, page.PaperSize, page.Orientation, page.Copies, source?.Title ?? "PicView");
    }

    private static PrintLayout Layout(PrintSource? source, Snapshot page, PrintSettings settings, double dpi) =>
        PrintLayout.Compute(PaperSizeHelper.Resolve(page.PaperSize, (int)page.Orientation), settings,
            source?.Width ?? 0, source?.Height ?? 0, dpi);

    private static (int Width, int Height) PixelSizeOf(PrintLayout layout) =>
        (Math.Max(1, (int)Math.Round(layout.PageWidthPx)), Math.Max(1, (int)Math.Round(layout.PageHeightPx)));

    private static void Draw(SKCanvas canvas, PrintLayout layout, PrintSource? source, bool isGrayscale,
        bool forPreview)
    {
        canvas.Clear(SKColors.White);
        if (forPreview)
        {
            DrawPaperEdgeAndMargins(canvas, layout);
        }

        if (source is null || layout.Destination.Width <= 0 || layout.Destination.Height <= 0)
        {
            return;
        }

        using var paint = new SKPaint();
        if (isGrayscale)
        {
            paint.ColorFilter = SKColorFilter.CreateColorMatrix(GrayscaleMatrix);
        }

        source.Draw(canvas, layout.Source, layout.Destination, paint, forPreview);
    }

    private static void DrawPaperEdgeAndMargins(SKCanvas canvas, PrintLayout layout)
    {
        using var edge = new SKPaint();
        edge.Style = SKPaintStyle.Stroke;
        edge.Color = new SKColor(211, 211, 211);
        edge.StrokeWidth = 1;
        canvas.DrawRect(0.5f, 0.5f, (float)layout.PageWidthPx - 1, (float)layout.PageHeightPx - 1, edge);

        using var margins = new SKPaint();
        margins.Style = SKPaintStyle.Stroke;
        margins.Color = new SKColor(128, 128, 128);
        margins.StrokeWidth = 2;
        margins.PathEffect = SKPathEffect.CreateDash([8, 4], 0);
        var content = layout.Content;
        canvas.DrawRect((float)content.X, (float)content.Y, (float)content.Width, (float)content.Height, margins);
    }

    /// <summary>
    /// The settings a page is drawn with, read once, as the user may change them while it is drawn.
    /// </summary>
    private readonly record struct Snapshot(bool IsGrayscale, string PaperSize, Orientations Orientation, int Copies)
    {
        public static Snapshot Of(PrintSettings settings) => new(
            settings.ColorMode.Value == (int)ColorModes.BlackAndWhite,
            settings.PaperSize.Value ?? RememberedPrintSettings.FallbackPaperSize,
            (Orientations)settings.Orientation.Value,
            settings.Copies.Value);
    }
}
