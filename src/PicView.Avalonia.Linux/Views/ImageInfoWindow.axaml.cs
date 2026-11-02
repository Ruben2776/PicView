using Avalonia;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using R3;

namespace PicView.Avalonia.Linux.Views;

public partial class ImageInfoWindow : GenericWindow
{
    public ImageInfoWindow(ImageInfoWindowConfig config)
    {
        InitializeComponent();
    }
}