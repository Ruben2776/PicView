using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Core.DebugTools;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Navigation;

public static class UINavigationHelper
{
    public static async ValueTask OpenPathAsync(MainWindow mainWindow, MainWindowViewModel vm, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        vm.TopTitlebarViewModel.DropDownMenu.IsDropDownMenuVisible.Value = false;

        var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current?.DataContext as CoreViewModel);
        if (core is null)
        {
            return;
        }

        // Continue from the remembered start-up directory only when opening into a fresh tab
        var isFreshTab = vm.WindowTabs.ActiveTab.CurrentValue.IsFresh;
        await SourceOpener.OpenAsync(mainWindow, core, path, new OpenOptions(ContinueFromLeftOff: isFreshTab)).ConfigureAwait(false);
    }

    /// <summary>
    /// Forward: shows the entry after the one the active tab shows in its View History, in that tab. Does nothing at the
    /// newest end.
    /// </summary>
    public static async ValueTask OpenNextFileHistoryEntry(MainWindow mainWindow, MainWindowViewModel vm)
    {
        await StepAsync(mainWindow, vm, forwards: true).ConfigureAwait(false);
    }

    /// <summary>
    /// Back: shows the entry before the one the active tab shows in its View History, in that tab. Does nothing at the
    /// oldest end.
    /// </summary>
    public static async ValueTask OpenPreviousFileHistoryEntry(MainWindow mainWindow, MainWindowViewModel vm)
    {
        await StepAsync(mainWindow, vm, forwards: false).ConfigureAwait(false);
    }

    public static async Task OpenLastFile(MainWindow mainWindow, MainWindowViewModel vm)
    {
        await OpenPathAsync(mainWindow, vm, Settings.StartUp.LastFile).ConfigureAwait(false);
    }

    private static async ValueTask StepAsync(MainWindow mainWindow, MainWindowViewModel vm, bool forwards)
    {
        // The same tab for the whole step, even if the user switches tabs meanwhile
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;

        // BeginStep checks files on disk, so it stays off the UI thread
        var step = await Task.Run(() => tab.ViewHistory.BeginStep(forwards)).ConfigureAwait(false);
        if (step is not { } begun)
        {
            return;
        }

        try
        {
            await ShowAgainAsync(mainWindow, vm, tab, begun.Target).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(UINavigationHelper), nameof(StepAsync), e);
        }
        finally
        {
            // Shown or not, the step is over. The cursor moved only if the target showed
            tab.ViewHistory.EndStep(begun);
        }
    }

    private static async ValueTask ShowAgainAsync(MainWindow mainWindow, MainWindowViewModel vm, TabViewModel tab,
        ViewHistoryEntry entry)
    {
        switch (entry.Kind)
        {
            case ViewHistoryKind.SingleImage:
                // Shown from the kept model, with no new download or paste. A download's URL goes first, since the tab
                // records it and titles itself from it when it shows the image
                if (entry.SingleImageType is SingleImageType.Url)
                {
                    tab.SourceURL = entry.Path;
                }

                UpdateImage.SetSingleImage(vm, mainWindow, entry.Model!, entry.SingleImageType, entry.Title ?? string.Empty);
                break;
            case ViewHistoryKind.File:
                await OpenPathAsync(mainWindow, vm, entry.Path).ConfigureAwait(false);
                break;
            case ViewHistoryKind.ArchivePage:
                // The archive opens at its first page, until a page can be shown on its own
                await OpenPathAsync(mainWindow, vm, entry.Path).ConfigureAwait(false);
                break;
        }
    }
}
