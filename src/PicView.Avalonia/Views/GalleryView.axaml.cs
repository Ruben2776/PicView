using Avalonia.Controls;

namespace PicView.Avalonia.Views;

public partial class GalleryView : UserControl
{
    public GalleryView()
    {
        InitializeComponent();
        Loaded += delegate
        {
            for (int i = 0; i < 90; i++)
            {
                GalleryListBox.Items.Add(new object());
            }
        };
    }
}