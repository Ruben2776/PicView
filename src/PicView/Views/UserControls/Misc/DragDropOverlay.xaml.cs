using System.Windows;

namespace PicView.Views.UserControls.Misc;

public partial class DragDropOverlay
{
    public DragDropOverlay(UIElement element)
    {
        InitializeComponent();

        ContentHolder.Content = element;
    }
}