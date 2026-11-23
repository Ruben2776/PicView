using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.Win32.Views;

public partial class EffectsWindow : GenericWindow
{
    public EffectsWindow()
    {
        InitializeComponent();
        ThemeManager.ApplyAcrylic(this, EffectsView);
    }
}