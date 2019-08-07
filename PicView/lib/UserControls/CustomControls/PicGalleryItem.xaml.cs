using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PicView.lib.UserControls
{
    /// <summary>
    /// Interaction logic for PicGalleryItem.xaml
    /// </summary>
    public partial class PicGalleryItem : UserControl
    {
        public PicGalleryItem(BitmapSource pic)
        {
            InitializeComponent();

            img.Source = pic;

            img.MouseEnter += (s, y) => AnimationHelper.HoverSizeAnim(
                border,
                false,
                200,
                230
            );
            img.MouseLeave += (s, y) => AnimationHelper.HoverSizeAnim(
                border,
                true,
                230,
                200
            );

            img.MouseLeftButtonUp += Img_MouseLeftButtonUp;
        }

        private void Img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Properties.Settings.Default.PicGallery == 1)
                AnimationHelper.HoverSizeAnim(border, false, 230, 1000);
        }
    }
}
