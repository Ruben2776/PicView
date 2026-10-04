using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.Printing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Printing;

public static class PrintInitialization
{
    public static async ValueTask InitializeAsync(
        MainWindowViewModel vm,
        string path,
        IPrintEngine printEngine,
        IEnumerable<string> printers,
        IEnumerable<string> paperSizes,
        string defaultPrinter,
        string? defaultPaperSize = null)
    {
        if (vm.PrintPreview.PrintWindowConfig is null)
        {
            vm.PrintPreview.PrintWindowConfig = new PrintWindowConfig();
            await vm.PrintPreview.PrintWindowConfig.LoadAsync().ConfigureAwait(false);
        }

        var configProps = vm.PrintPreview.PrintWindowConfig.WindowProperties;

        var printersList = printers.ToList();
        var paperSizesList = paperSizes.ToList();

        vm.PrintPreview.Printers.Value = printersList;
        vm.PrintPreview.PaperSizes.Value = paperSizesList;

        var printerToUse = defaultPrinter;
        var configPrinter = configProps?.PrinterName;
        if (!string.IsNullOrWhiteSpace(configPrinter) && printersList.Contains(configPrinter))
        {
            printerToUse = configPrinter;
        }

        var paperSizeToUse = defaultPaperSize ?? "A4";
        var configPaperSize = configProps?.PaperSize;
        if (!string.IsNullOrWhiteSpace(configPaperSize) && paperSizesList.Contains(configPaperSize))
        {
            paperSizeToUse = configPaperSize;
        }
        else if (string.IsNullOrEmpty(defaultPaperSize) && paperSizesList.Count != 0)
        {
            paperSizeToUse = paperSizesList.First();
        }

        var currentPrintSettings = new PrintSettings
        {
            ImagePath = { Value = path },
            PrinterName = { Value = printerToUse },
            PaperSize = { Value = paperSizeToUse },
            ColorMode = { Value = configProps?.ColorMode ?? (int)ColorModes.Auto },
            Orientation = { Value = configProps?.Orientation ?? (int)Orientations.Portrait },
            ScaleMode = { Value = configProps?.ScaleMode ?? (int)ScaleModes.Fit },
            Copies = { Value = configProps?.Copies ?? 1 },
            MarginTop = { Value = configProps?.MarginTop ?? 10 }, // mm
            MarginBottom = { Value = configProps?.MarginBottom ?? 10 },
            MarginLeft = { Value = configProps?.MarginLeft ?? 10 },
            MarginRight = { Value = configProps?.MarginRight ?? 10 },
        };

        vm.PrintPreview.PrintSettings.Value = currentPrintSettings;
        
        var ps = vm.PrintPreview.PrintSettings.Value;

        // Printer change
        ps.PrinterName
            .AsObservable()
            .DistinctUntilChanged(StringComparer.OrdinalIgnoreCase)
            .ObserveOnThreadPool()
            .SubscribeAwait(async (_, _) =>
            {
                await printEngine.UpdatePreviewAsync(vm.WindowTabs.ActiveTab.CurrentValue, vm.PrintPreview).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(PrintInitialization), nameof(InitializeAsync)))
            .AddTo(vm.PrintPreview.Disposables);
        
        // Any setting change triggers preview update
        // ReSharper disable once InvokeAsExtensionMethod
        Observable.CombineLatest(
                ps.Orientation.AsObservable(),
                ps.MarginTop.AsObservable(),
                ps.MarginBottom.AsObservable(),
                ps.MarginLeft.AsObservable(),
                ps.MarginRight.AsObservable(),
                ps.ScaleMode.AsObservable(),
                ps.ColorMode.AsObservable(),
                ps.PaperSize.AsObservable(),
                (orientation, top, bottom, left, right, scale, color, paper)
                    => (orientation, top, bottom, left, right, scale, color, paper))
            .ObserveOnThreadPool()
            .SubscribeAwait(async (_, _) =>
            {
                await printEngine.UpdatePreviewAsync(vm.WindowTabs.ActiveTab.CurrentValue, vm.PrintPreview).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(PrintInitialization), nameof(InitializeAsync)))
            .AddTo(vm.PrintPreview.Disposables);

        await printEngine.UpdatePreviewAsync(vm.WindowTabs.ActiveTab.CurrentValue, vm.PrintPreview).ConfigureAwait(false);
        
        vm.PrintPreview.PrintCommand.SubscribeAwait(async (_, _) =>
        {
            await printEngine.RunPrintAsync(vm.WindowTabs.ActiveTab.CurrentValue, vm.PrintPreview).ConfigureAwait(false);
        }, DebugHelper.LogError(nameof(PrintInitialization), nameof(InitializeAsync)))
        .AddTo(vm.PrintPreview.Disposables);
        
        vm.WindowTabs.ActiveTab.CurrentValue.Image.Skip(1).SubscribeAwait(async (_, _) =>
        {
            await printEngine.UpdatePreviewAsync(vm.WindowTabs.ActiveTab.CurrentValue, vm.PrintPreview).ConfigureAwait(false);
        }, DebugHelper.LogError(nameof(PrintInitialization), nameof(InitializeAsync)))
        .AddTo(vm.PrintPreview.Disposables);
    }
}
