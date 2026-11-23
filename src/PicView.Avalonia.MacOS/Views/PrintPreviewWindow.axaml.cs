using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.MacOS.Views;

public partial class PrintPreviewWindow : GenericWindow
{

    public PrintPreviewWindow()
    {
        InitializeComponent();
        ThemeManager.ApplyAcrylic(this, PrintPreviewView);
    }

}