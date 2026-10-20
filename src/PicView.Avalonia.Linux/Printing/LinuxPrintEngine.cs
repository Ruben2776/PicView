using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.Printing;
using PicView.Core.Localization;
using PicView.Core.Linux.Printing;
using PicView.Core.Printing;

namespace PicView.Avalonia.Linux.Printing;

public class LinuxPrintEngine : AbstractPrintEngine
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

        try
        {
            // 4. Handle PDF exporting or printing
            var printerName = settings.PrinterName.Value ?? "";
            var saveAsPdf = TranslationManager.Translation.SaveAsPdf;

            switch (string.IsNullOrWhiteSpace(printerName))
            {
                case false
                    when string.Equals(printerName, saveAsPdf, StringComparison.Ordinal):
                    await PdfExport.SavePdfWithFilePicker(null, rtb);
                    break;
                case false:
                {
                    var title = Path.GetFileName(settings.ImagePath.Value) ?? "PicView";
                    var copies = settings.Copies.Value;

                    LinuxPrint.Print(printerName, settings.ImagePath.Value, title, copies);
                    break;
                }
            }
        }
        finally
        {
            // 5. Cleanup, delete temporary file
            try { File.Delete(settings.ImagePath.Value); } catch { /* ignore */ }
        }
    }

    public static IEnumerable<string> GetPaperSizes(string printerName)
        => CupsPaperQuery.GetPaperSizes(printerName);
}
