using PicView.UILogic.PicGallery;
using System.Windows;
using System.Windows.Media;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.UC;

namespace PicView.PicGallery
{
    internal static class GalleryNavigation
    {
        internal static int picGalleryItem_Size;
        internal static int picGalleryItem_Size_s;
        private static int index;

        internal static void Up()
        {
            if (index != FolderIndex)
            {
                SetUnselected(index);
            }

            var x = index - 1 < 0 ? 0 : index - 1;

            if (true) // TODO figure out how to determine if out of sight
            {
                // TODO change page
            }

            SetSelected(x);
        }

        internal static void Down()
        {
            if (index != FolderIndex)
            {
                SetUnselected(index);
            }

            var x = index + 1 >=
                GetPicGallery.Container.Children.Count ? GetPicGallery.Container.Children.Count - 1
                : index + 1;

            if (true) // TODO figure out how to determine if out of sight
            {
                // TODO change page
            }

            SetSelected(x);
        }

        internal static void Left()
        {
            if (index != FolderIndex)
            {
                SetUnselected(index);
            }

            var x = index - 1 - GalleryScroll.Horizontal_items < 0 ? 0 : index - 1 - GalleryScroll.Horizontal_items;

            if (true) // TODO figure out how to determine if out of sight
            {
                // TODO change page
            }

            SetSelected(x);
        }

        internal static void Right()
        {
            if (index != FolderIndex)
            {
                SetUnselected(index);
            }

            var x = index + GalleryScroll.Horizontal_items + 1 > GetPicGallery.Container.Children.Count - 1 ?
                GetPicGallery.Container.Children.Count - 1 : index + GalleryScroll.Horizontal_items;

            if (true) // TODO figure out how to determine if out of sight
            {
                // TODO change page
            }

            SetSelected(x);
        }

        internal static void LoadSelected()
        {
            if (index == FolderIndex)
            {
                GalleryToggle.CloseHorizontalGallery();
            }
            else
            {
                SetUnselected(FolderIndex);
                GalleryClick.Click(index);
            }
        }

        internal static void SetSelected(int x)
        {
            if (x > GetPicGallery.Container.Children.Count) { return; }

            // Select next item
            var nextItem = GetPicGallery.Container.Children[x] as Views.UserControls.PicGalleryItem;
            nextItem.innerborder.BorderBrush = Application.Current.Resources["ChosenColorBrush"] as SolidColorBrush;
            nextItem.innerborder.Width = nextItem.innerborder.Height = picGalleryItem_Size;

            index = x;
        }

        internal static void SetUnselected(int x)
        {
            if (x > GetPicGallery.Container.Children.Count) { return; }

            // Deselect current item
            var prevItem = GetPicGallery.Container.Children[x] as Views.UserControls.PicGalleryItem;
            prevItem.innerborder.BorderBrush = Application.Current.Resources["BorderBrush"] as SolidColorBrush;
            prevItem.innerborder.Width = prevItem.innerborder.Height = picGalleryItem_Size_s;
        }
    }
}
