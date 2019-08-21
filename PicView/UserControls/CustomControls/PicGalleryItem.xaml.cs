using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PicView.UserControls
{
    /// <summary>
    /// Interaction logic for PicGalleryItem.xaml
    /// </summary>
    public partial class PicGalleryItem : UserControl
    {
        internal bool Selected { get; set; }

        internal readonly int Id;

        internal const int picGalleryItem_Size = 230;
        internal const int picGalleryItem_Size_s = 200;
        public PicGalleryItem(BitmapSource pic, int id, bool selected = false)
        {
            InitializeComponent();

            if (pic != null)
                img.Source = pic;

            Selected = selected;
            Id = id;
            Loaded += PicGalleryItem_Loaded;
        }

        private void PicGalleryItem_Loaded(object sender, RoutedEventArgs e)
        {
            img.MouseEnter += (s, y) => AnimationHelper.HoverSizeAnim(
                border,
                false,
                picGalleryItem_Size_s,
                picGalleryItem_Size
            );
            img.MouseLeave += (s, y) => AnimationHelper.HoverSizeAnim(
                border,
                true,
                picGalleryItem_Size,
                picGalleryItem_Size_s
            );

            if (Selected)
                border.BorderBrush = new SolidColorBrush(AnimationHelper.GetPrefferedColorOverAlpha());
        }

        internal void Setselected(bool b)
        {
            if (b)
            {
                Selected = true;
                border.BorderBrush = new SolidColorBrush(AnimationHelper.GetPrefferedColorOverAlpha());
            }
            else
            {
                Selected = false;
                if (Application.Current.Resources["BorderBrush"] is SolidColorBrush bgBrush)
                    border.BorderBrush = bgBrush;
            }
        }
    }
}
