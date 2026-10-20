using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.Printing;
using PicView.Core.Localization;
using PicView.Core.Printing;
using PicView.Core.WindowsNT.Printing;
using Bitmap = Avalonia.Media.Imaging.Bitmap;


namespace PicView.Avalonia.Win32.Printing;

public class Win32PrintEngine : AbstractPrintEngine
{
    protected override async ValueTask RunPrintJob(PrintSettings settings, Bitmap avaloniaBmp)
    {
        ArgumentNullException.ThrowIfNull(avaloniaBmp);

        // 1. Grayscale
        if (settings.ColorMode.Value == (int)ColorModes.BlackAndWhite)
        {
            avaloniaBmp = PrintCore.ToGrayScale(avaloniaBmp, PrintDpi);
        }

        // 2. Compute layout
        var layout = PrintLayout.Compute(
            settings, avaloniaBmp.PixelSize.Width, avaloniaBmp.PixelSize.Height, PrintDpi);

        // 3. Render to RenderTargetBitmap
        var pageSize = new PixelSize((int)Math.Round(layout.PageWidthPx), (int)Math.Round(layout.PageHeightPx));
        var rtb = new RenderTargetBitmap(pageSize);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var ctx = rtb.CreateDrawingContext();
            ctx.FillRectangle(Brushes.White, new Rect(0, 0, layout.PageWidthPx, layout.PageHeightPx));
            ctx.DrawImage(avaloniaBmp, layout.Source.ToRect(), layout.Destination.ToRect());
        });

        // 4. Handle Printing or PDF Export
        var printerName = settings.PrinterName.Value ?? "";
        if (printerName == TranslationManager.Translation.SaveAsPdf)
        {
            var outputFilename = Path.GetFileNameWithoutExtension(settings.ImagePath.Value) + ".pdf";
            await PdfExport.SavePdfWithFilePicker(outputFilename, rtb);
        }
        else
        {
            // Convert to a format Win32 can handle (Bgra8888)
            var width = rtb.PixelSize.Width;
            var height = rtb.PixelSize.Height;
            var stride = width * 4;
            var bufferSize = height * stride;
            var bufferPtr = Marshal.AllocHGlobal(bufferSize);
            try
            {
                rtb.CopyPixels(new PixelRect(0, 0, width, height), bufferPtr, bufferSize, stride);
                var title = Path.GetFileName(settings.ImagePath.Value) ?? "PicView";
                Win32Print.Print(printerName, title, settings.Copies.Value, width, height, bufferPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }
    }

    public static IEnumerable<string> GetPaperSizes(string printerName)
    {
        return Win32Print.GetPaperSizes(printerName);
    }
}
