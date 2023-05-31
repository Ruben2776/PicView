using System.Windows.Media;

namespace PicView.Views.UserControls.Misc;

public partial class DragDropOverlayPic
{
    public DragDropOverlayPic(ImageSource source)
    {
        InitializeComponent();

        UpdateSource(source);
    }

    private void UpdateSource(ImageSource source)
    {
        if (source == null) { return; }

        ContentHolderSource.ImageSource = source;
        ContentHolder.Width = source.Width;
        ContentHolder.Height = source.Height;
    }
}