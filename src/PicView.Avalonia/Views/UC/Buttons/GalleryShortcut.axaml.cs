using Avalonia.Controls;
using Avalonia.Media;
using PicView.Avalonia.UI;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Views.UC.Buttons;

public partial class GalleryShortcut : UserControl
{
    public GalleryShortcut()
    {
        InitializeComponent();
        if (!Settings.Theme.Dark)
        {
            ApplyLightTheme();
        }
    }
    
    private void ApplyLightTheme()
    {
        PointerEntered += (_, _) =>
        {
            Path1.Fill = Brushes.White;
            Path2.Fill = Brushes.White;
            Path3.Fill = Brushes.White;
        };

        PointerExited += (_, _) =>
        {
            var mainBrush = UIHelper.GetBrush("MainTextColor");
            Path1.Fill = mainBrush;
            Path2.Fill = mainBrush;
            Path3.Fill = mainBrush;
        };
    }
}