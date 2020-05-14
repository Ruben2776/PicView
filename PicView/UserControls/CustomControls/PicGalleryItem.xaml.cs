using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static PicView.Fields;

namespace PicView.UserControls
{
    /// <summary>
    /// Interaction logic for PicGalleryItem.xaml
    /// </summary>
    public partial class PicGalleryItem : UserControl
    {
        internal bool Selected { get; set; }

        internal readonly int Id;

        public PicGalleryItem(BitmapSource pic, int id, bool selected = false)
        {
            InitializeComponent();

            if (pic != null)
            {
                img.Source = pic;
            }

            Selected = selected;
            Id = id;

            outterborder.Width = outterborder.Height = picGalleryItem_Size;
            innerborder.Width = innerborder.Height = picGalleryItem_Size_s;

            img.MouseEnter += (s, y) => AnimationHelper.HoverSizeAnim(
                innerborder,
                false,
                picGalleryItem_Size_s,
                picGalleryItem_Size
            );

            img.MouseLeave += (s, y) => AnimationHelper.HoverSizeAnim(
                innerborder,
                true,
                picGalleryItem_Size,
                picGalleryItem_Size_s
            );

            if (Selected)
            {
                innerborder.BorderBrush = new SolidColorBrush(AnimationHelper.GetPrefferedColorOverAlpha());
                innerborder.Width = innerborder.Height = picGalleryItem_Size;
            }


        }

        internal void SetSelected(bool b)
        {
            if (b)
            {
                Selected = true;
                innerborder.BorderBrush = new SolidColorBrush(AnimationHelper.GetPrefferedColorOverAlpha());
                innerborder.Width = innerborder.Height = picGalleryItem_Size;
            }
            else
            {
                Selected = false;
                if (Application.Current.Resources["BorderBrush"] is SolidColorBrush bgBrush)
                {
                    innerborder.BorderBrush = bgBrush;
                }

                innerborder.Width = innerborder.Height = picGalleryItem_Size_s;
            }
        }
    }
}
