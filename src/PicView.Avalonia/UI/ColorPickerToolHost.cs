using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Localization;
using PicView.Core.Sizing;

namespace PicView.Avalonia.UI;

public static class ColorPickerToolHost
{
    private static Panel? _mainPanel;
    private static ColorPickerTool? _colorPicker;

    private static ColorPickerToolManager? _colorPickerManager;

    public static void Initialize(Panel mainPanel)
    {
        _mainPanel = mainPanel;
    }

    public static bool IsColorPickerVisible => _colorPicker is not null;

    public static void ShowColorPicker(MainViewModel vm)
    {
        if (_mainPanel is null)
            return;

        if (_colorPicker is not null)
            return; // already visible

        _colorPicker = new ColorPickerTool
        {
            Width = 160,
            Height = 160
        };

        _mainPanel.Children.Add(_colorPicker);

        _colorPickerManager = new ColorPickerToolManager(_mainPanel, null)
        {
            isColorPicking = true
        };

        _colorPickerManager.AttachTool(_colorPicker);

        _mainPanel.PointerMoved += _colorPickerManager.OnPointerMoved;
        _mainPanel.PointerPressed += _colorPickerManager.OnPointerPressed;


        _colorPickerManager.isColorPicking = true;
       
    }

    public static void HideColorPicker()
    {
        if (_mainPanel is null || _colorPicker is null) return;

        _mainPanel.Children.Remove(_colorPicker);
        _colorPicker = null;
    }

    public static async Task ToggleColorPickerToolWindow(MainViewModel vm)
    {
        // if (vm.MainWindow.IsColorPickerToolWindowShown.Value)
        // {
        //     vm.MainWindow.IsHistoryWindowShown.Value = false;
        //     HistoryWindowHost.HideHistory();
        // }
        // else
        // {
        //     vm.MainWindow.IsHistoryWindowShown.Value = true;
        //     vm.HistoryWin = new HistoryWindowViewModel(vm, vm.History);
            ColorPickerToolHost.ShowColorPicker(vm);
        //}
    }
}
