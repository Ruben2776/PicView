using System.Windows.Controls;
using System.Windows.Media;

namespace PicView.Views.UserControls
{
    public partial class DragDropOverlayPic : UserControl
    {
        public DragDropOverlayPic(ImageSource source)
        {
            InitializeComponent();

            if (source == null) { return; }

            ContentHolderSource.ImageSource = source;
            ContentHolder.Width = source.Width;
            ContentHolder.Height = source.Height;
        }
    }
}
