using System.Windows.Input;
using PicView.PicGallery;

namespace PicView.Views.UserControls.Gallery;

/// <summary>
/// Interaction logic for PicGallery.xaml
/// </summary>
public partial class PicGallery
{
    public PicGallery()
    {
        InitializeComponent();
        Loaded += GalleryLoad.PicGallery_Loaded;
    }

    private void PART_HorizontalScrollBar_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Disable animation for scrollbar
        Scroller.CanContentScroll = true;
    }
}