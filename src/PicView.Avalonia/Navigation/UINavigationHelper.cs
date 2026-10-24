using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Core.FileHistory;
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

    public static async ValueTask OpenNextFileHistoryEntry(MainWindow mainWindow, MainWindowViewModel vm)
    {
        var nextEntry = FileHistoryManager.GetNextEntry();
        var pathToLoad = !string.IsNullOrWhiteSpace(nextEntry) ? nextEntry : Settings.StartUp.LastFile;
        await OpenPathAsync(mainWindow, vm, pathToLoad).ConfigureAwait(false);
    }
    
    public static async ValueTask OpenPreviousFileHistoryEntry(MainWindow mainWindow, MainWindowViewModel vm)
    {
        var prevEntry = FileHistoryManager.GetPreviousEntry();
        var pathToLoad = !string.IsNullOrWhiteSpace(prevEntry) ? prevEntry : Settings.StartUp.LastFile;
        await OpenPathAsync(mainWindow, vm, pathToLoad).ConfigureAwait(false);
    }
    
    public static async Task OpenLastFile(MainWindow mainWindow, MainWindowViewModel vm)
    {
        var lastEntry = FileHistoryManager.GetLastEntry();
        var pathToLoad = !string.IsNullOrWhiteSpace(lastEntry) ? lastEntry : Settings.StartUp.LastFile;
        await OpenPathAsync(mainWindow, vm, pathToLoad).ConfigureAwait(false);
    }
}