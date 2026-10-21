using PrintWindowProperties = PicView.Core.Config.PrintWindowConfig.PrintWindowProperties;

namespace PicView.Core.Printing;

/// <summary>
/// The Print Settings last used in the Print window, which every later Print Job starts from.
/// </summary>
public static class RememberedPrintSettings
{
    /// <summary>
    /// The paper a Print Job uses when the Printer names none.
    /// </summary>
    public const string FallbackPaperSize = "A4";

    private const int DefaultMarginMm = 10;

    /// <summary>
    /// The Print Settings to start a Print Job from: the remembered ones, where the Printer and its paper still offer
    /// them, else the defaults.
    /// </summary>
    /// <param name="remembered">The remembered Print Settings, or <see langword="null"/> when none were.</param>
    /// <param name="printers">The Printers to choose from, Save as PDF first.</param>
    /// <param name="defaultPrinter">The Printer to use when the remembered one is not among them.</param>
    /// <param name="paperSizesOf">The paper sizes a Printer offers.</param>
    public static PrintSettings Restore(PrintWindowProperties? remembered, Printer[] printers, Printer defaultPrinter,
        Func<Printer, string[]> paperSizesOf)
    {
        var printer = Array.Find(printers,
            p => string.Equals(p.Id, remembered?.PrinterName, StringComparison.Ordinal)) ?? defaultPrinter;
        return new PrintSettings
        {
            Printer = { Value = printer },
            PaperSize = { Value = ChoosePaper(remembered?.PaperSize, paperSizesOf(printer)) },
            ColorMode = { Value = remembered?.ColorMode ?? (int)ColorModes.Auto },
            Orientation = { Value = remembered?.Orientation ?? (int)Orientations.Portrait },
            ScaleMode = { Value = remembered?.ScaleMode ?? (int)ScaleModes.Fit },
            Copies = { Value = remembered?.Copies ?? 1 },
            MarginTop = { Value = remembered?.MarginTop ?? DefaultMarginMm },
            MarginBottom = { Value = remembered?.MarginBottom ?? DefaultMarginMm },
            MarginLeft = { Value = remembered?.MarginLeft ?? DefaultMarginMm },
            MarginRight = { Value = remembered?.MarginRight ?? DefaultMarginMm }
        };
    }

    /// <summary>
    /// The paper to keep when the Printer offers <paramref name="paperSizes"/>: <paramref name="wanted"/> when it is
    /// one of them, else the first, else <see cref="FallbackPaperSize"/>.
    /// </summary>
    public static string ChoosePaper(string? wanted, string[] paperSizes)
    {
        if (wanted is not null && Array.IndexOf(paperSizes, wanted) >= 0)
        {
            return wanted;
        }

        return paperSizes.Length > 0 ? paperSizes[0] : FallbackPaperSize;
    }

    /// <summary>
    /// Remembers <paramref name="settings"/> in <paramref name="remembered"/>, for the next Print Job.
    /// </summary>
    public static void Remember(PrintSettings settings, PrintWindowProperties remembered)
    {
        remembered.PrinterName = settings.Printer.Value?.Id;
        remembered.PaperSize = settings.PaperSize.Value;
        remembered.Orientation = settings.Orientation.Value;
        remembered.ScaleMode = settings.ScaleMode.Value;
        remembered.ColorMode = settings.ColorMode.Value;
        remembered.Copies = settings.Copies.Value;
        remembered.MarginTop = settings.MarginTop.Value;
        remembered.MarginBottom = settings.MarginBottom.Value;
        remembered.MarginLeft = settings.MarginLeft.Value;
        remembered.MarginRight = settings.MarginRight.Value;
    }
}
