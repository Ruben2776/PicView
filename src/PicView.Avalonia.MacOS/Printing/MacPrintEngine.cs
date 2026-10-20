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

        // 2. Compute final print layout (same logic as preview)
        var layout = PrintLayout.Compute(
            settings,
            avaloniaBmp.PixelSize.Width,
            avaloniaBmp.PixelSize.Height,
            PrintDpi);

        // 3. Render the final image at print DPI
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

            ctx.DrawImage(avaloniaBmp, layout.Source.ToRect(), layout.Destination.ToRect());
        });

        // 4. Handle PDF exporting or printing
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

    public static IEnumerable<string> GetPaperSizes(string printerName)
        => CupsPaperQuery.GetPaperSizes(printerName);
}
