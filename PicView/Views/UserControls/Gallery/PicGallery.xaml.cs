using PicView.UILogic.PicGallery;
using System.Windows.Controls;

namespace PicView.UILogic.UserControls
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