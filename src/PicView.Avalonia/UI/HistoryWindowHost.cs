using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Localization;
using PicView.Core.Sizing;

namespace PicView.Avalonia.UI;

public static class HistoryWindowHost
{
    private static Panel? _mainPanel;
    private static HistoryWindow? _history;

    public static void Initialize(Panel mainPanel)
    {
        _mainPanel = mainPanel;
    }

    public static bool IsHistoryVisible => _history is not null;

    public static void ShowHistory(MainViewModel vm)
    {
        if (_mainPanel is null)
            return;

        if (_history is not null)
            return; // already visible

        _history = new HistoryWindow
        {
            Width = 300,
            Height = 340,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(12),
            DataContext = vm
        };

        _mainPanel.Children.Add(_history);
    }

    public static void HideHistory()
    {
        if (_mainPanel is null || _history is null) return;

        _mainPanel.Children.Remove(_history);
        _history = null;
    }

    public static async Task ToggleHistoryWindow(MainViewModel vm)
    {
        if (vm.MainWindow.IsHistoryWindowShown.Value)
        {
            vm.MainWindow.IsHistoryWindowShown.Value = false;
            HistoryWindowHost.HideHistory();
        }
        else
        {
            vm.MainWindow.IsHistoryWindowShown.Value = true;
            vm.HistoryWin = new HistoryWindowViewModel(vm, vm.History);
            HistoryWindowHost.ShowHistory(vm);
        }
    }
}
