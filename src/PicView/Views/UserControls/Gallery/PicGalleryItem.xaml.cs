using PicView.Animations;
using PicView.Properties;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.PicGallery.GalleryNavigation;

namespace PicView.Views.UserControls.Gallery;

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

        OuterBorder.Width = OuterBorder.Height = PicGalleryItemSize;
        InnerBorder.Width = InnerBorder.Height = Settings.Default.IsBottomGalleryShown ? PicGalleryItemSize : PicGalleryItemSizeS;

        ThumbImage.MouseEnter += (s, y) => AnimationHelper.AnimateGalleryItemSize(
            this,
            false,
            PicGalleryItemSizeS,
            PicGalleryItemSize
        );

        ThumbImage.MouseLeave += (s, y) => AnimationHelper.AnimateGalleryItemSize(
            this,
            true,
            PicGalleryItemSize,
            PicGalleryItemSizeS
        );

        if (!selected) return;
        InnerBorder.BorderBrush = new SolidColorBrush(AnimationHelper.GetPreferredColor());
        InnerBorder.Width = InnerBorder.Height = PicGalleryItemSize;
    }
}