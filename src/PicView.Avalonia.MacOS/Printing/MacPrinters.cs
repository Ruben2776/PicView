using PicView.Avalonia.Printing;
using PicView.Core.FileHandling;
using PicView.Core.MacOS.Printing;

namespace PicView.Avalonia.MacOS.Printing;

/// <summary>
/// The printers of macOS, through CUPS. A page goes to CUPS as a PDF of the paper's size.
/// </summary>
public sealed class MacPrinters : ISystemPrinters
{
    public string[] GetPrinterNames() => MacOSPrint.GetAvailablePrinters().ToArray();

    public string? GetDefaultPrinterName() => null;

    public string[] GetPaperSizes(string printerName) => CupsPaperQuery.GetPaperSizes(printerName).ToArray();

    public async ValueTask<bool> PrintAsync(string printerName, PrintedPage page)
    {
        // A temporary file, which App Shutdown deletes, as CUPS may still read it once this returns
        var path = TempFileManager.GetNewTempFilePath("print.pdf");
        await page.WritePdfAsync(path).ConfigureAwait(false);
        return MacOSPrint.Print(printerName, path, page.Title, page.Copies);
    }
}
