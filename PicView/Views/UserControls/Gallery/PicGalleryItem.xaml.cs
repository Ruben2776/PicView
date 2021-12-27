using PicView.Animations;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.PicGallery.GalleryNavigation;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// The usercontrol (UI element) of PicGallery
    /// </summary>
    public partial class PicGalleryItem : UserControl
    {
        public PicGalleryItem(ImageSource? pic, int id, bool selected)
        {
            InitializeComponent();

            if (pic != null)
            {
                img.Source = pic;
            }

            img.MouseEnter += (_, _) => Border.BorderBrush = new SolidColorBrush(AnimationHelper.GetPrefferedColor());
            img.MouseLeave += (_, _) =>
            { if (selected) { return; } Border.BorderBrush = (SolidColorBrush)Application.Current.Resources["BorderBrush"]; };

            if (selected)
            {
                Border.BorderBrush = new SolidColorBrush(AnimationHelper.GetPrefferedColor());
                Border.Height = PicGalleryItem_Size;
            }
            else
            {
                Border.Height = PicGalleryItem_Size_s;
            }

            Width = Height = PicGalleryItem_Size_s;
        }
    }
}