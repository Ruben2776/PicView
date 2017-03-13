using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace PicView.lib.UserControls
{
    /// <summary>
    /// Interaction logic for PicGalleryItem.xaml
    /// </summary>
    public partial class PicGalleryItem : UserControl
    {
        public PicGalleryItem(BitmapSource pic, string file)
        {
            InitializeComponent();
            var x = Path.GetFileName(file);

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
            AnimationHelper.HoverSizeAnim(border, false, 230, 1000);
        }
    }
}
