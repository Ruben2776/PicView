using Avalonia.Threading;
using PicView.Avalonia.Views.UC;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.UI;

public static class ViewChangeHelper
{
    public static void SwitchToStartUpMenu(MainWindowViewModel vm) =>
        SwitchToStartUpMenu(vm, vm.WindowTabs.ActiveTab.CurrentValue);

    /// <summary>
    /// Shows the start-up menu in <paramref name="tab"/>, one of <paramref name="vm"/>'s tabs. May be called from any
    /// thread.
    /// </summary>
    public static void SwitchToStartUpMenu(MainWindowViewModel vm, TabViewModel tab)
    {
        // The old view is disposed on the UI thread too, as disposing it changes its controls
        Dispatcher.UIThread.Invoke(() =>
        {
            if (tab.CurrentView.CurrentValue is IDisposable disposable)
            {
                disposable.Dispose();
            }
            tab.CurrentView.Value = new StartUpMenu();
        });
        vm.IsLoadingIndicatorShown.Value = false;
    }
}
