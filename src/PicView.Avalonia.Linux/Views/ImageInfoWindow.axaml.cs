using PicView.Avalonia.CustomControls;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Linux.Views;

public partial class ImageInfoWindow : GenericWindow
{
    public ImageInfoWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}