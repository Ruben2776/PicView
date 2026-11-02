using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.MacOS.Views;

public partial class ImageInfoWindow : GenericWindow
{
    public ImageInfoWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        if (Settings.Theme.GlassTheme)
        {
            WindowBorder.Background = Brushes.Transparent;
        }
        else if (!Settings.Theme.Dark)
        {
            XExifView.Background = UIHelper.GetMenuBackgroundColor();
        }
    }
}