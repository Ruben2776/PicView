using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.Printing;
using PicView.Core.Localization;
using PicView.Core.MacOS.Printing;
using PicView.Core.Printing;

namespace PicView.Avalonia.MacOS.Printing;

public class MacPrintEngine : AbstractPrintEngine
{
    protected override async ValueTask RunPrintJob(PrintSettings settings, Bitmap avaloniaBmp)
    {
        ArgumentNullException.ThrowIfNull(avaloniaBmp);

        // 1. Convert grayscale when requested
        if (settings.ColorMode.Value == (int)ColorModes.BlackAndWhite)
        {
            avaloniaBmp = PrintCore.ToGrayScale(avaloniaBmp, PrintDpi);
        }

        // 2. Determine paper size (mm) using shared helper
        var paperInfo = ResolvePaper(settings);

        // 3. Compute final print layout (same logic as preview)
        var layout = PrintCore.ComputeLayout(
            paperInfo.WidthMm,
            paperInfo.HeightMm,
            settings,
            avaloniaBmp.PixelSize.Width,
            avaloniaBmp.PixelSize.Height,
            PrintDpi);

        // 4. Render the final image at print DPI
        var pageSize = new PixelSize(
            (int)Math.Round(layout.PageWidthPx),
            (int)Math.Round(layout.PageHeightPx));

        var rtb = new RenderTargetBitmap(pageSize);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var ctx = rtb.CreateDrawingContext();

            // background
            ctx.FillRectangle(Brushes.White,
                new Rect(0, 0, layout.PageWidthPx, layout.PageHeightPx));

            var dest = new Rect(layout.DrawX, layout.DrawY, layout.DrawWidth, layout.DrawHeight);

            ctx.DrawImage(
                avaloniaBmp,
                new Rect(0, 0, avaloniaBmp.PixelSize.Width, avaloniaBmp.PixelSize.Height),
                dest);
        });

        // 6. Handle PDF exporting or printing
        var printerName = settings.PrinterName.Value ?? "";
        var saveAsPdf = TranslationManager.Translation.SaveAsPdf;

        switch (string.IsNullOrWhiteSpace(printerName))
        {
            case false
                when string.Equals(printerName, saveAsPdf, StringComparison.Ordinal):
                var outputFilename = Path.GetFileNameWithoutExtension(settings.ImagePath.Value) + ".pdf";
                await PdfExport.SavePdfWithFilePicker(outputFilename, rtb);
                break;
            case false:
            {
                var title = Path.GetFileName(settings.ImagePath.Value) ?? "PicView";
                var copies = settings.Copies.Value;

                MacOSPrint.Print(printerName, settings.ImagePath.Value, title, copies);
                break;
            }
        }
    }

    public override PaperInfo ResolvePaper(PrintSettings settings)
    {
        var requestedName = settings.PaperSize.Value ?? "A4";

        // Convert from CUPS naming patterns to PaperSizeHelper known names
        // Examples:
        //   "iso_a4_210x297mm"  → "A4"
        //   "A4.Borderless"     → "A4"
        //   "na_letter_8.5x11in"→ "Letter"

        var normalized = NormalizePaperName(requestedName);

        var (w, h) = PaperSizeHelper.GetMmSize(normalized);

        var landscape = settings.Orientation.Value == (int)Orientations.Landscape;

        return landscape
            ? new PaperInfo(normalized, h, w)
            : new PaperInfo(normalized, w, h);
    }

    private static string NormalizePaperName(string cupsName)
    {
        cupsName = cupsName.ToLowerInvariant();

        return cupsName switch
        {
            _ when cupsName.Contains("a4") => "A4",
            _ when cupsName.Contains("a3") => "A3",
            _ when cupsName.Contains("a5") => "A5",
            _ when cupsName.Contains("letter") => "Letter",
            _ when cupsName.Contains("legal") => "Legal",
            _ when cupsName.Contains("tabloid") => "Tabloid",
            _ when cupsName.Contains("4x6") => "4x6",
            _ when cupsName.Contains("5x7") => "5x7",
            _ when cupsName.Contains("8x10") => "8x10",
            // unknown → fallback
            _ => cupsName
        };
    }

    public static IEnumerable<string> GetPaperSizes(string printerName)
        => CupsPaperQuery.GetPaperSizes(printerName);
}
