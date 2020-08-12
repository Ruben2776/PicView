using PicView.UILogic.PicGallery;
using System.Windows.Controls;

namespace PicView.Views.UserControls
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