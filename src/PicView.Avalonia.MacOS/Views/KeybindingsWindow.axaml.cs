using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.MacOS.Views;

public partial class KeybindingsWindow : GenericWindow
{
    public KeybindingsWindow()
    {
        InitializeComponent();
        KeyUp += (_, e) => KeybindingsView.Controller?.HandleKeyPressed(e);
    }
}