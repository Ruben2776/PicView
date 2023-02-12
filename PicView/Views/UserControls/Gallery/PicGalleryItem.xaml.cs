using PicView.Animations;
using System.Windows.Controls;
using System.Windows.Media;
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
                img.Source = pic;
            }

            Id = id;

            outterborder.Width = outterborder.Height = PicGalleryItem_Size;
            innerborder.Width = innerborder.Height = PicGalleryItem_Size_s;

            img.MouseEnter += (s, y) => AnimationHelper.HoverSizeAnim(
                this,
                false,
                PicGalleryItem_Size_s,
                PicGalleryItem_Size
            );

            img.MouseLeave += (s, y) => AnimationHelper.HoverSizeAnim(
                this,
                true,
                PicGalleryItem_Size,
                PicGalleryItem_Size_s
            );

            if (!selected) return;
            innerborder.BorderBrush = new SolidColorBrush(AnimationHelper.GetPrefferedColor());
            innerborder.Width = innerborder.Height = PicGalleryItem_Size;
        }
    }
}