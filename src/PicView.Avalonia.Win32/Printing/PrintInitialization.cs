using System.Drawing;
using System.Drawing.Printing;
using PicView.Avalonia.Printing;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Win32.Views;

namespace PicView.Avalonia.Win32.Printing;

public static class PrintInitialization
{
    public static void Initialize(MainViewModel vm, string path, PrintPreviewWindow printPreviewWindow)
    {
        if (vm.PicViewer.FileInfo.Value != null && File.Exists(path))
        {
            using var fs = File.OpenRead(path);
            vm.PrintPreview.PreviewImage.Value = new Bitmap(fs);
            // Prefill page sizes to avoid excessive resize
            vm.PrintPreview.PageWidth.Value = 650;
            vm.PrintPreview.PageHeight.Value = 950;
        }
        
        var printerSettings = new PrinterSettings();

        // Load installed printers
        vm.PrintPreview.Printers.Value = new List<string>(PrinterSettings.InstalledPrinters);
        vm.PrintPreview.PaperSizes.Value = new List<string>(PrintEngine.GetPaperSizes(printerSettings.PrinterName));


        // Pre-select default printer settings
        var pageSettings = printerSettings.DefaultPageSettings;

        var currentPrintSettings =
            new PrintSettings // TODO: Add print settings to its own config class to remember user preference
            {
                ImagePath = { Value = vm.PicViewer.FileInfo?.Value?.FullName },
                PrinterName = { Value = printerSettings.PrinterName },
                PaperSize = { Value = pageSettings.PaperSize.PaperName },
                ColorMode =
                {
                    Value = printerSettings.SupportsColor ? (int)ColorModes.Auto : (int)ColorModes.BlackAndWhite
                },
                Orientation =
                    { Value = pageSettings.Landscape ? (int)Orientations.Landscape : (int)Orientations.Portrait },
                MarginTop = { Value = PrintSettings.HundredthsInchToMm(pageSettings.Margins.Top) },
                MarginBottom = { Value = PrintSettings.HundredthsInchToMm(pageSettings.Margins.Bottom) },
                MarginLeft = { Value = PrintSettings.HundredthsInchToMm(pageSettings.Margins.Left) },
                MarginRight = { Value = PrintSettings.HundredthsInchToMm(pageSettings.Margins.Right) }
            };

        vm.PrintPreview.PrintSettings.Value = currentPrintSettings;
        printPreviewWindow.Initialize();
    }
}