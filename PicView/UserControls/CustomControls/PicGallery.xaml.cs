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
            //PicGalleryLogic.IsLoading = PicGalleryLogic.IsOpen = false;
            Loaded += PicGalleryLoad.PicGallery_Loaded;
        }

    }
}