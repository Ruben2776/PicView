using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.Win32.Views;

public partial class ConvertWindow : GenericWindow
{
    public ConvertWindow()
    {
        InitializeComponent();
        ThemeManager.ApplyAcrylic(this, ConvertView);
    }
}