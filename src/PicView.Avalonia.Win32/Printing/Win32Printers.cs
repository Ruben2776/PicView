using PicView.Avalonia.Printing;
using PicView.Core.Printing;
using PicView.Core.WindowsNT.Printing;

namespace PicView.Avalonia.Win32.Printing;

/// <summary>
/// The printers of Windows, through GDI.
/// </summary>
public sealed class Win32Printers : ISystemPrinters
{
    public string[] GetPrinterNames() => Win32Print.GetAvailablePrinters().ToArray();

    public string? GetDefaultPrinterName() => Win32Print.GetDefaultPrinter();

    public string[] GetPaperSizes(string printerName) => Win32Print.GetPaperSizes(printerName).ToArray();

    public ValueTask<bool> PrintAsync(string printerName, PrintedPage page) =>
        ValueTask.FromResult(Win32Print.Print(printerName, page.Title, page.Copies, page.PaperSize,
            page.Orientation == Orientations.Landscape, page.PixelWidth, page.PixelHeight, page.Pixels));
}
