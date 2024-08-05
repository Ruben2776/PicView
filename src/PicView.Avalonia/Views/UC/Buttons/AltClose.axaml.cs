using Avalonia.Controls;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Views.UC.Buttons;
public partial class AltClose : UserControl
{
    public AltClose()
    {
        InitializeComponent();
        Loaded += delegate
        {
            HideInterfaceLogic.AddHoverButtonEvents(this, XButton, DataContext as MainViewModel);
        };
    }
}
