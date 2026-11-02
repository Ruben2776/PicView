using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.Win32.Views;

public partial class KeybindingsWindow : GenericWindow
{
    public KeybindingsWindow()
    {
        InitializeComponent();
        KeyDown += (_, e) => KeybindingsView.Controller?.HandleKeyPressed(e);
    }
}