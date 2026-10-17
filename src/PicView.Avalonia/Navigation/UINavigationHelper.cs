using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
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

        var tabs = vm.WindowTabs;
        var tab = tabs.ActiveTab.CurrentValue;
        if (!tab.IsInitialized)
        {
            var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current?.DataContext as CoreViewModel);
            if (core is null)
            {
                return;
            }

            await QuickLoad.QuickLoadAsync(mainWindow, core, path, true).ConfigureAwait(false);
            return;
        }

        var isViewStartUpMenu = false;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (tab.CurrentView.CurrentValue is StartUpMenu)
            {
                tab.CurrentView.Value = new ImageViewer();
                isViewStartUpMenu = true;
            }
        });

        vm.IsLoadingIndicatorShown.Value = true;
        var isLoadedSuccessfully = (await tabs.LoadFromStringAsync(path).ConfigureAwait(false)).IsLoaded;
        vm.IsLoadingIndicatorShown.Value = false;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!isLoadedSuccessfully && isViewStartUpMenu)
            {
                tab.CurrentView.Value = new StartUpMenu();
            }
            else if (isLoadedSuccessfully)
            {
                tab.UpdateTabTitle();
                WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);
            }
        });
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