using Avalonia.Controls;
using Avalonia.Input;

namespace PicView.Avalonia.Views.Main;

public partial class PrintPreviewView : UserControl
{
    public PrintPreviewView()
    {
        InitializeComponent();
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.BeginMoveDrag(e);
        }
    }
}