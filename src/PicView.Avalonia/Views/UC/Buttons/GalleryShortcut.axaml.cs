using Avalonia.Controls;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Views.UC.Buttons;

public partial class GalleryShortcut : UserControl
{
    public GalleryShortcut()
    {
        InitializeComponent();
        Loaded += delegate
        {
            HideInterfaceLogic.AddHoverButtonEvents(this, InnerButton, DataContext as MainViewModel);
        };
    }
}
