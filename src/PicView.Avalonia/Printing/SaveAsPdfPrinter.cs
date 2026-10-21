using PicView.Avalonia.FileSystem;

namespace PicView.Avalonia.Printing;

/// <summary>
/// Save as PDF: the Printer that writes the Printed Page to a PDF file the user picks, on every platform.
/// </summary>
internal static class SaveAsPdfPrinter
{
    private const string PdfExtension = ".pdf";

    public static async ValueTask<PrintOutcome> SendAsync(PrintedPage page, ISavePathPicker pathPicker)
    {
        var suggestedName = Path.GetFileNameWithoutExtension(page.Title) + PdfExtension;
        var path = await pathPicker.PickSavePathAsync(suggestedName, PdfExtension).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(path))
        {
            return PrintOutcome.Cancelled;
        }

        await page.WritePdfAsync(path).ConfigureAwait(false);
        return PrintOutcome.Sent;
    }
}
