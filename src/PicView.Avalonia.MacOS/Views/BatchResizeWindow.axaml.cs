using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.MacOS.Views;

public partial class BatchResizeWindow : GenericWindow
{
    public BatchResizeWindow()
    {
        InitializeComponent();
        ThemeManager.ApplyAcrylic(this, XBatchResizeView);
    }
}