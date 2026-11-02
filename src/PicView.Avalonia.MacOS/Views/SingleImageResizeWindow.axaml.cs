using PicView.Avalonia.CustomControls;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.MacOS.Views;

public partial class SingleImageResizeWindow : GenericWindow
{
    public SingleImageResizeWindow(MainWindowViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
    }
}