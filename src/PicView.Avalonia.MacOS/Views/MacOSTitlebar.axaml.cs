using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.MacOS.Views;

public partial class MacOSTitlebar : UserControl
{
    public MacOSTitlebar()
    {
        InitializeComponent();
        PointerPressed += (_, e) => MoveWindow(e);
    }

    private void MoveWindow(PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }

        var hostWindow = (Window)VisualRoot;
        WindowHelper.WindowDragAndDoubleClickBehavior(hostWindow, e);
    }
}