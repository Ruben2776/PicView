using Avalonia.Controls;
using Avalonia.Input;

namespace PicView.Avalonia.MacOS.Views;

public partial class ExifWindow : Window
{
    public ExifWindow()
    {
        InitializeComponent();
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }

        var hostWindow = (Window)VisualRoot;
        hostWindow?.BeginMoveDrag(e);
    }
}