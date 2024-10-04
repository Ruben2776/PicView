using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Win32.Views;

public partial class WinTitleBar : UserControl
{
    public WinTitleBar()
    {
        InitializeComponent();
        PointerPressed += (_, e) => MoveWindow(e);
        PointerExited += (_, _) =>
        {
            DragAndDropHelper.RemoveDragDropView();
        };
    }

    private void MoveWindow(PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }

        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        if (vm.IsEditableTitlebarOpen)
        {
            return;
        }
        WindowHelper.WindowDragAndDoubleClickBehavior((Window)VisualRoot, e);
    }
}