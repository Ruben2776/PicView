using System.Diagnostics;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.MacOS.Views;

public partial class ImageInfoWindow : GenericWindow
{
    public ImageInfoWindow(MainWindowViewModel viewModel)
    {
        Debug.Assert(viewModel.InfoWindow.ImageInfoWindowConfig != null);
        var config = viewModel.InfoWindow.ImageInfoWindowConfig;
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
        GenericWindowHelper.GenericWindowInitialize(this, StringExtensions.CombineWithAppName(TranslationManager.Translation.ImageInfo), false, config.WindowProperties);
    }
}