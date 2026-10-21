using Avalonia.Media.Imaging;
using PicView.Avalonia.FileSystem;
using PicView.Core.Config;
using PicView.Core.ViewModels;
using PrintWindowProperties = PicView.Core.Config.PrintWindowConfig.PrintWindowProperties;

namespace PicView.Avalonia.Printing;

/// <summary>
/// Starts a Main Window's Print Jobs, for the Print and Save as PDF Actions: in the Print window, or straight away
/// with the remembered Print Settings. There is none while Side-by-Side is on, or while the tab shows no still image.
/// </summary>
/// <param name="systemPrinters">This platform's printers.</param>
/// <param name="pdfPathPicker">Asks where Save as PDF writes, with the Main Window's file picker.</param>
/// <param name="readRemembered">Reads the remembered Print Settings.</param>
/// <param name="showPrintWindow">Opens the Print window on a Print Job of what the Main Window shows.</param>
public sealed class PrintJobStarter(
    ISystemPrinters systemPrinters,
    Func<ISavePathPicker> pdfPathPicker,
    Func<Task<PrintWindowProperties?>> readRemembered,
    Func<MainWindowViewModel, Task> showPrintWindow)
{
    /// <summary>
    /// The Print Action: opens the Print window, or when the preview is off, sends the Print Job to the remembered
    /// Printer.
    /// </summary>
    public async Task PrintAsync(MainWindowViewModel vm)
    {
        if (!HasPrintJob(vm))
        {
            return;
        }

        if (Settings.UIProperties.ShowPrintPreview)
        {
            await showPrintWindow(vm).ConfigureAwait(false);
            return;
        }

        await SendNowAsync(vm, toSaveAsPdf: false).ConfigureAwait(false);
    }

    /// <summary>
    /// The Save as PDF Action: sends the Print Job to Save as PDF, without the Print window.
    /// </summary>
    public async Task SaveAsPdfAsync(MainWindowViewModel vm)
    {
        if (!HasPrintJob(vm))
        {
            return;
        }

        await SendNowAsync(vm, toSaveAsPdf: true).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the remembered Print Settings from the Print window's config file.
    /// </summary>
    public static async Task<PrintWindowProperties?> ReadRememberedAsync()
    {
        var config = new PrintWindowConfig();
        await config.LoadAsync().ConfigureAwait(false);
        return config.WindowProperties;
    }

    private async Task SendNowAsync(MainWindowViewModel vm, bool toSaveAsPdf)
    {
        var remembered = await readRemembered().ConfigureAwait(false);
        await PrintJob.SendNowAsync(vm, systemPrinters, pdfPathPicker(), remembered, toSaveAsPdf)
            .ConfigureAwait(false);
    }

    private static bool HasPrintJob(MainWindowViewModel vm) =>
        !Settings.ImageScaling.ShowImageSideBySide && vm.WindowTabs.ActiveTab.CurrentValue.Image.CurrentValue is Bitmap;
}
