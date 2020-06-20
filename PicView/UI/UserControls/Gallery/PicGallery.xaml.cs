using PicView.UI.PicGallery;
using System.Windows.Controls;

namespace PicView.UI.UserControls
{
    /// <summary>
    /// Interaction logic for PicGallery.xaml
    /// </summary>
    public partial class PicGallery : UserControl
    {
        public PicGallery()
        {
            InitializeComponent();
            //PicGalleryLogic.IsLoading = PicGalleryLogic.IsOpen = false;
            Loaded += GalleryLoad.PicGallery_Loaded;
        }
    }
}