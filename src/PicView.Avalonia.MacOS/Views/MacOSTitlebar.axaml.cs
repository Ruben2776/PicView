using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.Helpers;
using System.Threading.Tasks;

namespace PicView.Avalonia.MacOS.Views;

public partial class MacOSTitlebar : UserControl
{
    public MacOSTitlebar()
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