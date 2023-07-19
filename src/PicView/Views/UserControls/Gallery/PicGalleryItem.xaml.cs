using PicView.Animations;
using PicView.ChangeImage;
using PicView.PicGallery;
using PicView.Properties;
using System.Windows;
using System.Windows.Media;
using static PicView.PicGallery.GalleryNavigation;

namespace PicView.Views.UserControls.Gallery;

public partial class PicGalleryItem
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

        ThumbImage.MouseEnter += delegate
        {
            InnerBorder.BorderBrush = (SolidColorBrush)Application.Current.Resources["ChosenColorBrush"];
        };

        ThumbImage.MouseLeave += delegate
        {
            if (id == Navigation.FolderIndex) return;
            if (GalleryFunctions.IsGalleryOpen)
            {
                if (InnerBorder.Width == PicGalleryItemSize && InnerBorder.Height == PicGalleryItemSize)
                {
                    return;
                }
            }
            InnerBorder.BorderBrush = (SolidColorBrush)Application.Current.Resources["BorderBrush"];
        };

        if (!selected) return;
        InnerBorder.BorderBrush = new SolidColorBrush(AnimationHelper.GetPreferredColor());
        InnerBorder.Width = InnerBorder.Height = PicGalleryItemSize;
    }
}