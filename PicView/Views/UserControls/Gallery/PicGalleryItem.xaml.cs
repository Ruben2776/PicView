using PicView.UILogic.Animations;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static PicView.UILogic.PicGallery.GalleryFunctions;

namespace PicView.UILogic.UserControls
{
    /// <summary>
    /// The usercontrol (UI element) of PicGallery
    /// </summary>
    public partial class PicGalleryItem : UserControl
    {
        internal readonly int Id;

        public PicGalleryItem(BitmapSource pic, int id, bool selected = false)
        {
            InitializeComponent();

            if (pic != null)
            {
                img.Source = pic;
            }

            Id = id;

            outterborder.Width = outterborder.Height = picGalleryItem_Size;
            innerborder.Width = innerborder.Height = picGalleryItem_Size_s;

            img.MouseEnter += (s, y) => AnimationHelper.HoverSizeAnim(
                this,
                false,
                picGalleryItem_Size_s,
                picGalleryItem_Size
            );

            img.MouseLeave += (s, y) => AnimationHelper.HoverSizeAnim(
                this,
                true,
                picGalleryItem_Size,
                picGalleryItem_Size_s
            );

            if (selected)
            {
                innerborder.BorderBrush = new SolidColorBrush(AnimationHelper.GetPrefferedColorOverAlpha());
                innerborder.Width = innerborder.Height = picGalleryItem_Size;
            }
        }
    }
}