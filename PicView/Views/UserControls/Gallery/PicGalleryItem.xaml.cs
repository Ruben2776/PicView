using PicView.UILogic.Animations;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static PicView.PicGallery.GalleryNavigation;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// The usercontrol (UI element) of PicGallery
    /// </summary>
    public partial class PicGalleryItem : UserControl
    {
        internal int Id { get; set; }

        public PicGalleryItem(ImageSource pic, int id, bool selected)
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

            if (selected)
            {
                innerborder.BorderBrush = new SolidColorBrush(AnimationHelper.GetPrefferedColorOverAlpha());
                innerborder.Width = innerborder.Height = PicGalleryItem_Size;
            }
        }
    }
}