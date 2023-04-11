using System.Windows.Controls;
using System.Windows.Media;
using PicView.Animations;
using static PicView.PicGallery.GalleryNavigation;

namespace PicView.Views.UserControls.Gallery
{
    public partial class PicGalleryItem : UserControl
    {
        internal int Id { get; set; }

        public PicGalleryItem(ImageSource? pic, int id, bool selected)
        {
            InitializeComponent();

            if (pic != null)
            {
                ThumbImage.Source = pic;
            }

            Id = id;

            OuterBorder.Width = OuterBorder.Height = PicGalleryItem_Size;
            InnerBorder.Width = InnerBorder.Height = PicGalleryItem_Size_s;

            ThumbImage.MouseEnter += (s, y) => AnimationHelper.HoverSizeAnim(
                this,
                false,
                PicGalleryItem_Size_s,
                PicGalleryItem_Size
            );

            ThumbImage.MouseLeave += (s, y) => AnimationHelper.HoverSizeAnim(
                this,
                true,
                PicGalleryItem_Size,
                PicGalleryItem_Size_s
            );

            if (!selected) return;
            InnerBorder.BorderBrush = new SolidColorBrush(AnimationHelper.GetPreferredColor());
            InnerBorder.Width = InnerBorder.Height = PicGalleryItem_Size;
        }
    }
}