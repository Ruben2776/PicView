using PicView.Avalonia.Printing;
using PicView.Core.FileHandling;
using PicView.Core.Linux.Printing;

namespace PicView.Avalonia.Linux.Printing;

/// <summary>
/// The printers of Linux, through CUPS. A page goes to CUPS as a PDF of the paper's size.
/// </summary>
/// <remarks>Linux printing is not written yet: listing throws, so the Print window offers only Save as PDF.</remarks>
public sealed class LinuxPrinters : ISystemPrinters
{
    public string[] GetPrinterNames() => LinuxPrint.GetAvailablePrinters();

    public string? GetDefaultPrinterName() => null;

    public string[] GetPaperSizes(string printerName) => CupsPaperQuery.GetPaperSizes(printerName).ToArray();

    public async ValueTask<bool> PrintAsync(string printerName, PrintedPage page)
    {
        // A temporary file, which App Shutdown deletes, as CUPS may still read it once this returns
        var path = TempFileManager.GetNewTempFilePath("print.pdf");
        await page.WritePdfAsync(path).ConfigureAwait(false);
        LinuxPrint.Print(printerName, path, page.Title, page.Copies);
        return true;
    }
}
