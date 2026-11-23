using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.MacOS.Views;

public partial class KeybindingsWindow : GenericWindow
{
    public KeybindingsWindow()
    {
        InitializeComponent();
        ThemeManager.ApplyAcrylic(this, BackgroundBorder);
        KeyUp += (_, e) => KeybindingsView.Controller?.HandleKeyPressed(e);
    }
}