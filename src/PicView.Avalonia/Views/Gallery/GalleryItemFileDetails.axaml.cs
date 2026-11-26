using Avalonia.Controls;

namespace PicView.Avalonia.Views.Gallery;

/// <summary>
/// The name, file size and image size at the bottom of an Expanded Grid item. Only a Grid cell has one, so the Docked
/// Gallery, Masonry and Rows realize items without it.
/// </summary>
public partial class GalleryItemFileDetails : Border
{
    public GalleryItemFileDetails()
    {
        InitializeComponent();
    }
}
