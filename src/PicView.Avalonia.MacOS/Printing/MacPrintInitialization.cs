using Avalonia.Threading;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.ViewModels;
using PicView.Core.MacOS.Printing;
using PicView.Core.Printing;

namespace PicView.Avalonia.MacOS.Printing;

public static class MacPrintInitialization
{
    public static async Task Initialize(MainViewModel vm, string path, PrintPreviewWindow printPreviewWindow)
    {
        // 1. Printers via CUPS
        var printers = MacOSPrint.GetAvailablePrinters().ToList(); // includes "Save as PDF" first
        vm.PrintPreview.Printers.Value = printers;

        var defaultPrinter = printers.FirstOrDefault() ?? string.Empty;

        // 2. Paper sizes - from printer or fallback
        vm.PrintPreview.PaperSizes.Value =
            CupsPaperQuery.GetPaperSizes(defaultPrinter).ToList();
        
        // Allow every format that is viewable to also be printed, or just make sure the image effect stays applied on print
        var commonSupportedFormat = await ImageFormatConverter.ConvertToCommonSupportedFormatAsync(path, vm)
            .ConfigureAwait(false);

        // 3. Build initial PrintSettings
        var currentPrintSettings = new PrintSettings
        {
            ImagePath = { Value = commonSupportedFormat },
            PrinterName = { Value = defaultPrinter },
            PaperSize = { Value = "A4" },
            ColorMode = { Value = (int)ColorModes.Auto },
            Orientation = { Value = (int)Orientations.Portrait },
            MarginTop = { Value = 10 },     // mm
            MarginBottom = { Value = 10 },
            MarginLeft = { Value = 10 },
            MarginRight = { Value = 10 },
        };

        vm.PrintPreview.PrintSettings.Value = currentPrintSettings;

        await Dispatcher.UIThread.InvokeAsync(() => printPreviewWindow.Initialize(commonSupportedFormat));
    }
}