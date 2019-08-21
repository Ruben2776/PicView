using System.Windows.Controls;

namespace PicView.UserControls
{
    /// <summary>
    /// Interaction logic for PicGallery.xaml
    /// </summary>
    public partial class PicGallery : UserControl
    {
        public PicGallery()
        {
            InitializeComponent();
            Loaded += PicView.PicGalleryLogic.PicGallery_Loaded;
        }

    }
}