using Avalonia.Controls;
using Avalonia.Input;

namespace PicView.Avalonia.Views.Main;

public partial class KeybindView : UserControl
{
    public KeybindView()
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