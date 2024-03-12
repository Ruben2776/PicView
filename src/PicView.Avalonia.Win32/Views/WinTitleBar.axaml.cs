using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.Helpers;

namespace PicView.Avalonia.Win32.Views;

public partial class WinTitleBar : UserControl
{
    public WinTitleBar()
    {
        InitializeComponent();
        PointerPressed += async (_, e) => await MoveWindow(e);
    }

    private async Task MoveWindow(PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }

        var hostWindow = (Window)VisualRoot;
        hostWindow?.BeginMoveDrag(e);
        await WindowHelper.UpdateWindowPosToSettings();
    }
}