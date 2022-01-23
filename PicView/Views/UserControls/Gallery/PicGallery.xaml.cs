using System.Windows.Controls;
using PicView.PicGallery;

namespace PicView.Views.UserControls.Gallery
{
    /// <summary>
    /// Interaction logic for PicGallery.xaml
    /// </summary>
    public partial class PicGallery : UserControl
    {
        public PicGallery()
        {
            InitializeComponent();

            Loaded += GalleryLoad.PicGallery_Loaded;
        }
    }
}