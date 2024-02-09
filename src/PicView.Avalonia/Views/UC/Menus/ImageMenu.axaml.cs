using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Views.UC.Menus;

public partial class ImageMenu : UserControl
{
    public ImageMenu()
    {
        InitializeComponent();
    }

    private void RotationButton_OnClick(object? sender, RoutedEventArgs e)
    {
        RotationPopup.IsOpen = !RotationPopup.IsOpen;
    }

    private void SlideShowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        SlideShowPopup.IsOpen = !SlideShowPopup.IsOpen;
    }
}