using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.Linux.Views;

public partial class KeybindingsWindow : GenericWindow
{
    public KeybindingsWindow()
    {
        InitializeComponent();
        KeyDown += (_, e) => KeybindingsView.Controller?.HandleKeyPressed(e);
    }
}