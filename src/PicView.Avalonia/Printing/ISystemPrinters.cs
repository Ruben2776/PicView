namespace PicView.Avalonia.Printing;

/// <summary>
/// The system's printers on one platform, behind the printer seam. An adapter only does native work: it lists the
/// printers and their paper, and puts a finished Printed Page on paper. Save as PDF is not one of them; the Print Job
/// adds it.
/// </summary>
/// <remarks>Every member may block, so the Print Job calls them off the UI thread.</remarks>
public interface ISystemPrinters
{
    /// <summary>
    /// The system's printers, by the names the system gives them. It throws when the platform cannot list them.
    /// </summary>
    string[] GetPrinterNames();

    /// <summary>
    /// The system's default printer, or <see langword="null"/> when there is none or the platform cannot tell.
    /// </summary>
    string? GetDefaultPrinterName();

    /// <summary>
    /// The paper sizes of a printer, as the platform names them. Empty when the platform cannot tell.
    /// </summary>
    string[] GetPaperSizes(string printerName);

    /// <summary>
    /// Puts <paramref name="page"/> on paper in the printer named <paramref name="printerName"/>, as many times as
    /// its copies.
    /// </summary>
    /// <returns>Whether the system took the job.</returns>
    ValueTask<bool> PrintAsync(string printerName, PrintedPage page);
}
