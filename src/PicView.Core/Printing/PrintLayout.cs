using System.Runtime.InteropServices;

namespace PicView.Core.Printing;

/// <summary>
/// Where an image goes on a printed page: which part of the image is drawn, and where on the page.
/// All page values are in pixels at the DPI the layout was computed for; <see cref="Source"/> is in image pixels.
/// </summary>
/// <param name="PageWidthPx">The page width, after orientation.</param>
/// <param name="PageHeightPx">The page height, after orientation.</param>
/// <param name="Content">The area inside the margins.</param>
/// <param name="Source">The part of the image to draw. Smaller than the image only when Fill crops it.</param>
/// <param name="Destination">Where <see cref="Source"/> is drawn. Always inside <see cref="Content"/>.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct PrintLayout(
    double PageWidthPx,
    double PageHeightPx,
    PrintRect Content,
    PrintRect Source,
    PrintRect Destination)
{
    /// <summary>
    /// The DPI a page is rendered at for printing and PDF export.
    /// It is also the size Center keeps an image at when the image fits.
    /// </summary>
    public const float PrintDpi = 300f;

    private const double MmPerInch = 25.4;

    /// <summary>
    /// Lays out an image on the paper, orientation, margins and scale mode of <paramref name="settings"/>.
    /// </summary>
    public static PrintLayout Compute(PrintSettings settings, double imageWidthPx, double imageHeightPx, double dpi)
    {
        var paper = PaperSizeHelper.Resolve(settings.PaperSize.Value, settings.Orientation.Value);
        return Compute(paper, settings, imageWidthPx, imageHeightPx, dpi);
    }

    /// <summary>
    /// Lays out an image on <paramref name="paper"/>, using the margins and scale mode of <paramref name="settings"/>.
    /// The paper size and orientation of <paramref name="settings"/> are ignored.
    /// </summary>
    public static PrintLayout Compute(Paper paper, PrintSettings settings, double imageWidthPx, double imageHeightPx,
        double dpi)
    {
        var mmToPx = dpi / MmPerInch;
        var pageWidth = paper.WidthMm * mmToPx;
        var pageHeight = paper.HeightMm * mmToPx;

        var marginLeft = settings.MarginLeft.Value * mmToPx;
        var marginRight = settings.MarginRight.Value * mmToPx;
        var marginTop = settings.MarginTop.Value * mmToPx;
        var marginBottom = settings.MarginBottom.Value * mmToPx;

        var content = new PrintRect(
            marginLeft,
            marginTop,
            Math.Max(0, pageWidth - marginLeft - marginRight),
            Math.Max(0, pageHeight - marginTop - marginBottom));

        var image = new PrintRect(0, 0, imageWidthPx, imageHeightPx);

        if (imageWidthPx <= 0 || imageHeightPx <= 0 || content.Width <= 0 || content.Height <= 0)
        {
            return new PrintLayout(pageWidth, pageHeight, content, image,
                new PrintRect(content.X, content.Y, 0, 0));
        }

        if (settings.ScaleMode.Value == (int)ScaleModes.Stretch)
        {
            return new PrintLayout(pageWidth, pageHeight, content, image, content);
        }

        var scaleX = content.Width / imageWidthPx;
        var scaleY = content.Height / imageHeightPx;
        var scale = settings.ScaleMode.Value switch
        {
            (int)ScaleModes.Fill => Math.Max(scaleX, scaleY),
            (int)ScaleModes.Center => Math.Min(Math.Min(scaleX, scaleY), dpi / PrintDpi),
            _ => Math.Min(scaleX, scaleY) // Fit
        };

        // Fill scales past the content area; crop the image so it stays inside the margins
        var sourceWidth = Math.Min(imageWidthPx, content.Width / scale);
        var sourceHeight = Math.Min(imageHeightPx, content.Height / scale);
        var source = new PrintRect(
            (imageWidthPx - sourceWidth) / 2,
            (imageHeightPx - sourceHeight) / 2,
            sourceWidth,
            sourceHeight);

        var drawWidth = sourceWidth * scale;
        var drawHeight = sourceHeight * scale;
        var destination = new PrintRect(
            content.X + (content.Width - drawWidth) / 2,
            content.Y + (content.Height - drawHeight) / 2,
            drawWidth,
            drawHeight);

        return new PrintLayout(pageWidth, pageHeight, content, source, destination);
    }
}

/// <summary>
/// A rectangle on a printed page or in an image, in pixels.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct PrintRect(double X, double Y, double Width, double Height);

/// <summary>
/// The size of a sheet of paper as it lies for printing, in millimeters.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct Paper(double WidthMm, double HeightMm);
