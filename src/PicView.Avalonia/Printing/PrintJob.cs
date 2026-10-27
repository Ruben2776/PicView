using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.FileSystem;
using PicView.Core.DebugTools;
using PicView.Core.Printing;
using PicView.Core.ViewModels;
using R3;
using PrintWindowProperties = PicView.Core.Config.PrintWindowConfig.PrintWindowProperties;

namespace PicView.Avalonia.Printing;

/// <summary>
/// A Main Window's Print Job: the Save Source of the tab it shows, the Print Settings, and the Printed Page they make,
/// until it is sent to a Printer. In the Print window it shows a preview and follows the Main Window's active tab and
/// that tab's navigation; elsewhere it is sent straight away with the remembered Print Settings.
/// </summary>
/// <remarks>
/// Its work runs off the UI thread. The Print window shows its spinner until the first preview is ready, and again
/// while the Print Job is sent; a changed setting or image re-renders the preview without it.
/// </remarks>
public sealed class PrintJob : IDisposable
{
    private readonly MainWindowViewModel _vm;
    private readonly ISystemPrinters _systemPrinters;
    private readonly ISavePathPicker _pdfPathPicker;
    private readonly PrintPreviewViewModel? _preview;
    private readonly Action? _closeWindow;
    private readonly CancellationTokenSource _disposed = new();
    private readonly CompositeDisposable _subscriptions = new();

    // One page is drawn at a time, so the image is never replaced while a page is drawn from it
    private readonly SemaphoreSlim _drawing = new(1, 1);

    private Printer[] _printers = [];
    private string[] _paperSizes = [];
    private PrintSettings _settings = new();

    // What Reset puts back; the Print window can reset while the Print Settings differ from it
    private PrintSettings _defaults = new();
    private PrintSource? _source;
    private bool _isSourceStale = true;

    private PrintJob(MainWindowViewModel vm, ISystemPrinters systemPrinters, ISavePathPicker pdfPathPicker,
        PrintPreviewViewModel? preview, Action? closeWindow)
    {
        _vm = vm;
        _systemPrinters = systemPrinters;
        _pdfPathPicker = pdfPathPicker;
        _preview = preview;
        _closeWindow = closeWindow;
    }

    /// <summary>
    /// Completes once the first preview is shown, or loading it failed.
    /// </summary>
    public Task Loaded { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// Starts the Print Job the Print window shows, once that window is shown with its spinner. It loads off the UI
    /// thread, and fills <paramref name="preview"/> in one go when the first preview is ready, which hides the spinner.
    /// </summary>
    /// <param name="pdfPathPicker">Asks where Save as PDF writes, with the Print window's file picker.</param>
    /// <param name="remembered">The remembered Print Settings, read before the window was shown.</param>
    /// <param name="closeWindow">Closes the Print window: on Cancel, and once the Print Job is sent.</param>
    public static PrintJob Open(MainWindowViewModel vm, PrintPreviewViewModel preview, ISystemPrinters systemPrinters,
        ISavePathPicker pdfPathPicker, PrintWindowProperties? remembered, Action closeWindow)
    {
        var job = new PrintJob(vm, systemPrinters, pdfPathPicker, preview, closeWindow);
        preview.CancelCommand.Subscribe(_ => Dispatcher.UIThread.Post(closeWindow)).AddTo(job._subscriptions);
        job.Loaded = Task.Run(() => job.LoadPreviewAsync(remembered));
        return job;
    }

    /// <summary>
    /// Sends a Print Job of what <paramref name="vm"/> shows straight away, with the remembered Print Settings: to the
    /// remembered Printer, or to Save as PDF.
    /// </summary>
    /// <param name="pdfPathPicker">Asks where Save as PDF writes, with the Main Window's file picker.</param>
    public static async Task<PrintOutcome> SendNowAsync(MainWindowViewModel vm, ISystemPrinters systemPrinters,
        ISavePathPicker pdfPathPicker, PrintWindowProperties? remembered, bool toSaveAsPdf)
    {
        using var job = new PrintJob(vm, systemPrinters, pdfPathPicker, preview: null, closeWindow: null);
        return await Task.Run(() =>
        {
            job.Restore(remembered, toSaveAsPdf);
            return job.SendPageAsync();
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the Printed Page the preview shows to the chosen Printer. The Print window shows its spinner meanwhile,
    /// and closes once the page is sent; when Save as PDF is cancelled or sending fails, it stays open.
    /// </summary>
    public async Task<PrintOutcome> SendAsync()
    {
        await SetProcessingAsync(true).ConfigureAwait(false);
        var outcome = await Task.Run(SendPageAsync).ConfigureAwait(false);
        if (outcome is PrintOutcome.Sent && _closeWindow is { } closeWindow)
        {
            await Dispatcher.UIThread.InvokeAsync(closeWindow);
            return outcome;
        }

        await SetProcessingAsync(false).ConfigureAwait(false);
        return outcome;
    }

    public void Dispose()
    {
        if (_disposed.IsCancellationRequested)
        {
            return;
        }

        _disposed.Cancel();
        _subscriptions.Dispose();
        _ = DisposeSourceWhenIdleAsync();
    }

    private async Task LoadPreviewAsync(PrintWindowProperties? remembered)
    {
        try
        {
            Restore(remembered, toSaveAsPdf: false);
            _defaults = RememberedPrintSettings.Restore(null, _printers, GetDefaultPrinter(), GetPaperSizes);
            var image = await DrawPreviewAsync().ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_disposed.IsCancellationRequested)
                {
                    image?.Dispose();
                    return;
                }

                _preview!.Printers.Value = _printers;
                _preview.PaperSizes.Value = _paperSizes;
                _preview.PrintSettings.Value = _settings;
                _preview.CanReset.Value = !_settings.Matches(_defaults);
                if (image is not null)
                {
                    ShowPreview(image);
                }

                _preview.IsProcessing.Value = false;
            });

            if (!_disposed.IsCancellationRequested)
            {
                FollowChanges();
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(PrintJob), nameof(LoadPreviewAsync), e);
            await SetProcessingAsync(false).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Chooses the Printers, the paper they offer and the Print Settings to start from.
    /// </summary>
    private void Restore(PrintWindowProperties? remembered, bool toSaveAsPdf)
    {
        _printers = toSaveAsPdf ? [Printer.SaveAsPdf] : ListPrinters();
        _settings = RememberedPrintSettings.Restore(remembered, _printers, GetDefaultPrinter(),
            printer => _paperSizes = GetPaperSizes(printer));
    }

    private Printer[] ListPrinters()
    {
        try
        {
            var names = _systemPrinters.GetPrinterNames();
            var printers = new Printer[names.Length + 1];
            printers[0] = Printer.SaveAsPdf;
            for (var i = 0; i < names.Length; i++)
            {
                printers[i + 1] = Printer.FromSystem(names[i]);
            }

            return printers;
        }
        catch (Exception e)
        {
            // The platform cannot list its printers, as on Linux so far; Save as PDF still works
            DebugHelper.LogDebug(nameof(PrintJob), nameof(ListPrinters), e);
            return [Printer.SaveAsPdf];
        }
    }

    private Printer GetDefaultPrinter()
    {
        string? name = null;
        try
        {
            name = _systemPrinters.GetDefaultPrinterName();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(PrintJob), nameof(GetDefaultPrinter), e);
        }

        return Array.Find(_printers,
            printer => !printer.IsSaveAsPdf && string.Equals(printer.Id, name, StringComparison.Ordinal)) ?? _printers[0];
    }

    private string[] GetPaperSizes(Printer printer)
    {
        if (!printer.IsSaveAsPdf)
        {
            try
            {
                var paperSizes = _systemPrinters.GetPaperSizes(printer.Id);
                if (paperSizes.Length > 0)
                {
                    return paperSizes;
                }
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(PrintJob), nameof(GetPaperSizes), e);
            }
        }

        // Save as PDF, and a printer that cannot tell, offer every size PicView knows
        return PaperSizeHelper.GetAllNames().ToArray();
    }

    private void FollowChanges()
    {
        var tabs = _vm.WindowTabs;
        var imageChanges = Observable.Merge(
                tabs.ActiveTab.Skip(1).AsUnitObservable(),
                tabs.ActiveTab.Select(static tab => Observable.Merge(
                    tab.Image.Skip(1).AsUnitObservable(),
                    tab.Orientation.Current.Skip(1).AsUnitObservable())).Switch())
            .Do(_ => Volatile.Write(ref _isSourceStale, true));

        var settingChanges = Observable.Merge(
            _settings.PaperSize.Skip(1).AsUnitObservable(),
            _settings.Orientation.Skip(1).AsUnitObservable(),
            _settings.ScaleMode.Skip(1).AsUnitObservable(),
            _settings.ColorMode.Skip(1).AsUnitObservable(),
            _settings.MarginTop.Skip(1).AsUnitObservable(),
            _settings.MarginBottom.Skip(1).AsUnitObservable(),
            _settings.MarginLeft.Skip(1).AsUnitObservable(),
            _settings.MarginRight.Skip(1).AsUnitObservable());

        Observable.Merge(imageChanges, settingChanges)
            .ObserveOnThreadPool()
            .SubscribeAwait(async (_, ct) => await RefreshPreviewAsync(ct).ConfigureAwait(false),
                DebugHelper.LogError(nameof(PrintJob), nameof(RefreshPreviewAsync)), AwaitOperation.Switch, false)
            .AddTo(_subscriptions);

        Observable.Merge(
                settingChanges,
                _settings.Printer.Skip(1).AsUnitObservable(),
                _settings.Copies.Skip(1).AsUnitObservable())
            .Subscribe(_ => _preview!.CanReset.Value = !_settings.Matches(_defaults))
            .AddTo(_subscriptions);

        _settings.Printer.Skip(1)
            .Where(static printer => printer is not null)
            .DistinctUntilChanged()
            .ObserveOnThreadPool()
            .SubscribeAwait(async (printer, ct) => await RefreshPaperSizesAsync(printer!, ct).ConfigureAwait(false),
                DebugHelper.LogError(nameof(PrintJob), nameof(RefreshPaperSizesAsync)), AwaitOperation.Switch, false)
            .AddTo(_subscriptions);

        _preview!.PrintCommand
            .SubscribeAwait(async (_, _) => await SendAsync().ConfigureAwait(false),
                DebugHelper.LogError(nameof(PrintJob), nameof(SendAsync)), AwaitOperation.Drop)
            .AddTo(_subscriptions);

        _preview.ResetCommand
            .ObserveOnThreadPool()
            .SubscribeAwait(async (_, ct) => await ResetAsync(ct).ConfigureAwait(false),
                DebugHelper.LogError(nameof(PrintJob), nameof(ResetAsync)), AwaitOperation.Switch, false)
            .AddTo(_subscriptions);
    }

    /// <summary>
    /// Puts the Print Settings back to the defaults, with the default Printer and its paper. They are remembered when
    /// the Print window closes.
    /// </summary>
    private async Task ResetAsync(CancellationToken ct)
    {
        var paperSizes = _paperSizes;
        var defaults = RememberedPrintSettings.Restore(null, _printers, GetDefaultPrinter(),
            printer => paperSizes = GetPaperSizes(printer));
        ct.ThrowIfCancellationRequested();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (ct.IsCancellationRequested || _disposed.IsCancellationRequested)
            {
                return;
            }

            // The paper is offered before it is chosen, as a list without it clears the selection
            _paperSizes = paperSizes;
            _preview!.PaperSizes.Value = paperSizes;
            _defaults = defaults;
            _settings.CopyFrom(defaults);

            // Set here too, as nothing changes when the settings were already these defaults
            _preview.CanReset.Value = false;
        });
    }

    private async Task RefreshPreviewAsync(CancellationToken ct)
    {
        var image = await DrawPreviewAsync(ct).ConfigureAwait(false);
        if (image is null)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (ct.IsCancellationRequested || _disposed.IsCancellationRequested)
            {
                image.Dispose();
                return;
            }

            ShowPreview(image);
        });
    }

    /// <summary>
    /// Offers the paper of another Printer, keeping the chosen paper when it has it.
    /// </summary>
    private async Task RefreshPaperSizesAsync(Printer printer, CancellationToken ct)
    {
        var paperSizes = GetPaperSizes(printer);
        ct.ThrowIfCancellationRequested();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (ct.IsCancellationRequested || _disposed.IsCancellationRequested)
            {
                return;
            }

            // Chosen before the list changes, which clears a selection the new list lacks
            var paperSize = RememberedPrintSettings.ChoosePaper(_settings.PaperSize.Value, paperSizes);
            _paperSizes = paperSizes;
            _preview!.PaperSizes.Value = paperSizes;
            _settings.PaperSize.Value = paperSize;
        });
    }

    /// <returns>The preview, or <see langword="null"/> when another change came first.</returns>
    private async Task<WriteableBitmap?> DrawPreviewAsync(CancellationToken ct = default)
    {
        await _drawing.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (ct.IsCancellationRequested || _disposed.IsCancellationRequested)
            {
                return null;
            }

            return PageRenderer.RenderPreview(GetSource(), _settings);
        }
        finally
        {
            _drawing.Release();
        }
    }

    private void ShowPreview(WriteableBitmap image)
    {
        var shown = _preview!.PreviewImage.Value as IDisposable;
        _preview.PreviewImage.Value = image;
        _preview.PageWidth.Value = image.PixelSize.Width;
        _preview.PageHeight.Value = image.PixelSize.Height;
        shown?.Dispose();
    }

    /// <summary>
    /// The image the preview shows and the Printed Page is drawn from, taken again from the active tab after it, its
    /// image or its Orientation changed. Call while drawing.
    /// </summary>
    private PrintSource? GetSource()
    {
        if (!Volatile.Read(ref _isSourceStale))
        {
            return _source;
        }

        Volatile.Write(ref _isSourceStale, false);
        var previous = _source;
        try
        {
            _source = PrintSource.From(_vm.WindowTabs.ActiveTab.CurrentValue);
        }
        catch (Exception e)
        {
            // The tab's bitmap went away as it was copied; the next change takes it again
            DebugHelper.LogDebug(nameof(PrintJob), nameof(GetSource), e);
            _source = null;
        }

        previous?.Dispose();
        return _source;
    }

    private async Task<PrintOutcome> SendPageAsync()
    {
        try
        {
            Printer printer;
            PrintedPage page;
            await _drawing.WaitAsync(_disposed.Token).ConfigureAwait(false);
            try
            {
                // With a preview, the page is drawn from the image it shows, so what is sent is what was seen
                var source = _preview is not null && _source is not null ? _source : GetSource();
                if (source is null)
                {
                    DebugHelper.LogDebug(nameof(PrintJob), nameof(SendPageAsync), "The tab shows no still image");
                    return PrintOutcome.Failed;
                }

                printer = _settings.Printer.Value ?? _printers[0];
                page = PageRenderer.RenderForPrinter(source, _settings);
            }
            finally
            {
                _drawing.Release();
            }

            using (page)
            {
                if (printer.IsSaveAsPdf)
                {
                    return await SaveAsPdfPrinter.SendAsync(page, _pdfPathPicker).ConfigureAwait(false);
                }

                if (await _systemPrinters.PrintAsync(printer.Id, page).ConfigureAwait(false))
                {
                    return PrintOutcome.Sent;
                }
            }

            // TODO: Tell the user that the Print Job could not be sent. For now it is only logged.
            DebugHelper.LogDebug(nameof(PrintJob), nameof(SendPageAsync), $"{printer.Id} did not take the job");
            return PrintOutcome.Failed;
        }
        catch (Exception e)
        {
            // TODO: Tell the user that the Print Job could not be sent. For now it is only logged.
            DebugHelper.LogDebug(nameof(PrintJob), nameof(SendPageAsync), e);
            return PrintOutcome.Failed;
        }
    }

    private async Task SetProcessingAsync(bool isProcessing)
    {
        if (_preview is null)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!_disposed.IsCancellationRequested)
            {
                _preview.IsProcessing.Value = isProcessing;
            }
        });
    }

    private async Task DisposeSourceWhenIdleAsync()
    {
        await _drawing.WaitAsync().ConfigureAwait(false);
        try
        {
            _source?.Dispose();
            _source = null;
        }
        finally
        {
            _drawing.Release();
        }
    }
}
