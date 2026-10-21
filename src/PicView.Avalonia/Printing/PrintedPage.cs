using ImageMagick;
using PicView.Core.Printing;
using SkiaSharp;

namespace PicView.Avalonia.Printing;

/// <summary>
/// A Printed Page ready for a Printer: its pixels at <see cref="PrintLayout.PrintDpi"/>, and what the Printer needs
/// to put it on paper.
/// </summary>
public sealed class PrintedPage : IDisposable
{
    private readonly SKBitmap _bitmap;

    /// <param name="bitmap">The page, owned by this page from now on: 32-bit BGRA, premultiplied and opaque.</param>
    internal PrintedPage(SKBitmap bitmap, string paperSize, Orientations orientation, int copies, string title)
    {
        _bitmap = bitmap;
        PaperSize = paperSize;
        Orientation = orientation;
        Copies = copies;
        Title = title;
    }

    public int PixelWidth => _bitmap.Width;

    public int PixelHeight => _bitmap.Height;

    /// <summary>
    /// The first pixel: rows of <see cref="RowBytes"/> bytes, 4 bytes per pixel in BGRA order.
    /// </summary>
    public IntPtr Pixels => _bitmap.GetPixels();

    public int RowBytes => _bitmap.RowBytes;

    /// <summary>
    /// The paper, as the Printer names it.
    /// </summary>
    public string PaperSize { get; }

    public Orientations Orientation { get; }

    public int Copies { get; }

    /// <summary>
    /// The name of what is printed, for the printer's queue and the PDF file.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Writes the page to a PDF file whose page is the paper's size.
    /// </summary>
    public async Task WritePdfAsync(string path)
    {
        var settings = new PixelReadSettings((uint)PixelWidth, (uint)PixelHeight, StorageType.Char, PixelMapping.BGRA);
        using var image = new MagickImage();
        image.ReadPixels(_bitmap.GetPixelSpan(), settings);
        image.Alpha(AlphaAction.Off);
        image.Density = new Density(PrintLayout.PrintDpi, PrintLayout.PrintDpi, DensityUnit.PixelsPerInch);
        image.Quality = 100;
        image.Page = new MagickGeometry((uint)PixelWidth, (uint)PixelHeight);
        await image.WriteAsync(path, MagickFormat.Pdf).ConfigureAwait(false);
    }

    public void Dispose() => _bitmap.Dispose();
}
