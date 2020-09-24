using PicView.UILogic.PicGallery;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.UC;

namespace PicView.PicGallery
{
    internal static class GalleryNavigation
    {
        #region int calculations

        internal static int picGalleryItem_Size;
        internal static int picGalleryItem_Size_s;
        internal static int index;

        internal static int Horizontal_items
        {
            get
            {
                if (GetPicGallery == null) { return 0; }

                return (int)Math.Floor(GetPicGallery.Width / picGalleryItem_Size);
            }
        }

        internal static int Vertical_items
        {
            get
            {
                if (GetPicGallery == null) { return 0; }

                return Properties.Settings.Default.PicGallery == 1 ?
                    (int)Math.Floor(GetPicGallery.Height / picGalleryItem_Size) : Pics.Count;
            }
        }

        internal static int Items_per_page
        {
            get
            {
                if (GetPicGallery == null) { return 0; }

                return Properties.Settings.Default.PicGallery == 1 ?
                    Horizontal_items * Vertical_items :
                    (int)Math.Floor(GetPicGallery.Height / picGalleryItem_Size);
            }
        }

        internal static int Current_page
        {
            get
            {
                return (int)Math.Floor((double)FolderIndex / Items_per_page);
            }
        }

        #endregion int calculations

        #region ScrollTo

        /// <summary>
        /// Scrolls to center of current item
        /// </summary>
        /// <param name="item">The index of picGalleryItem</param>
        internal static void ScrollTo()
        {
            if (Properties.Settings.Default.PicGallery == 1)
            {
                GetPicGallery.Scroller.ScrollToHorizontalOffset(picGalleryItem_Size * Horizontal_items * Current_page);
            }
            else
            {
                GetPicGallery.Scroller.ScrollToVerticalOffset(picGalleryItem_Size * Items_per_page * Current_page);
            }
        }

        internal static void ScrollTo(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ScrollTo(e.Delta > 0);
            }
            else
            {
                ScrollTo(e.Delta > 0, false);
            }
        }

        /// <summary>
        /// Scrolls a page back or forth
        /// </summary>
        /// <param name="next"></param>
        /// <param name="end"></param>
        internal static void ScrollTo(bool next, bool end = false, bool speedUp = false)
        {
            if (end)
            {
                if (next)
                {
                    GetPicGallery.Scroller.ScrollToRightEnd();
                }
                else
                {
                    GetPicGallery.Scroller.ScrollToLeftEnd();
                }
            }
            else
            {
                var speed = speedUp ? picGalleryItem_Size * 4.7 : picGalleryItem_Size;
                var direction = next ? GetPicGallery.Scroller.HorizontalOffset - speed : GetPicGallery.Scroller.HorizontalOffset + speed;

                if (Properties.Settings.Default.PicGallery == 1)
                {
                    GetPicGallery.Scroller.ScrollToHorizontalOffset(direction);
                }
                else
                {
                    if (next)
                    {
                        GetPicGallery.Scroller.ScrollToVerticalOffset(GetPicGallery.Scroller.VerticalOffset - speed);
                    }
                    else
                    {
                        GetPicGallery.Scroller.ScrollToVerticalOffset(GetPicGallery.Scroller.VerticalOffset + speed);
                    }
                }
            }
        }

        #endregion ScrollTo

        internal static void Up()
        {
            Deselect(FolderIndex);

            if (index != FolderIndex)
            {
                SetSelected(index, false);
            }

            var x = index - 1 < 0 ? 0 : index - 1;

            SetSelected(x, true);

            if (Vertical_items == 1)
            {
                GetPicGallery.Scroller.ScrollToHorizontalOffset(GetPicGallery.Scroller.HorizontalOffset - picGalleryItem_Size);
            }
        }

        internal static void Down()
        {
            Deselect(FolderIndex);

            if (index != FolderIndex)
            {
                SetSelected(index, false);
            }

            var x = index + 1 >=
                GetPicGallery.Container.Children.Count ? GetPicGallery.Container.Children.Count - 1
                : index + 1;

            SetSelected(x, true);

            if (Vertical_items == 1)
            {
                GetPicGallery.Scroller.ScrollToHorizontalOffset(GetPicGallery.Scroller.HorizontalOffset + picGalleryItem_Size);
            }
        }

        internal static void Left()
        {
            Deselect(FolderIndex);

            if (index != FolderIndex)
            {
                SetSelected(index, false);
            }

            int next;

            if (Vertical_items == 1)
            {
                next = index - 1 < 0 ? 0 : index - 1;
            }
            else
            {
                next = index - Vertical_items < 0 ? 0 : index - Vertical_items;
            }

            GetPicGallery.Scroller.ScrollToHorizontalOffset(GetPicGallery.Scroller.HorizontalOffset - picGalleryItem_Size);

            SetSelected(next, true);
        }

        internal static void Right()
        {
            Deselect(FolderIndex);

            if (index != FolderIndex)
            {
                SetSelected(index, false);
            }

            int next;

            if (Vertical_items == 1)
            {
                next = index + 1 > GetPicGallery.Container.Children.Count - 1 ?
                    GetPicGallery.Container.Children.Count - 1 : index + 1;
            }
            else
            {
                next = index + Vertical_items >= GetPicGallery.Container.Children.Count ?
                GetPicGallery.Container.Children.Count - 1 : index + Vertical_items;
            }

            SetSelected(next, true);

            GetPicGallery.Scroller.ScrollToHorizontalOffset(GetPicGallery.Scroller.HorizontalOffset + picGalleryItem_Size);
        }

        internal static void LoadSelected()
        {
            if (index == FolderIndex)
            {
                GalleryToggle.CloseHorizontalGallery();
            }
            else
            {
                SetSelected(FolderIndex, false);
                GalleryClick.Click(index);
            }
        }

        internal static void SetSelected(int x, bool selected)
        {
            if (x > GetPicGallery.Container.Children.Count || x < 0) { return; }

            if (index == 0)
            {
                if (GetPicGallery.Container.Children.Count == 0)
                {
                    return;
                }
                else if (FolderIndex != 0)
                {
                    index = FolderIndex;
                }
            }

            // Select next item
            var nextItem = GetPicGallery.Container.Children[x] as Views.UserControls.PicGalleryItem;

            if (selected)
            {
                nextItem.innerborder.BorderBrush = Application.Current.Resources["ChosenColorBrush"] as SolidColorBrush;
                nextItem.innerborder.Width = nextItem.innerborder.Height = picGalleryItem_Size;

                index = x;
            }
            else
            {
                nextItem.innerborder.BorderBrush = Application.Current.Resources["BorderBrush"] as SolidColorBrush;
                nextItem.innerborder.Width = nextItem.innerborder.Height = picGalleryItem_Size_s;
            }
        }

        private static void Deselect(int x)
        {
            if (x > GetPicGallery.Container.Children.Count || x < 0) { return; }

            // Deselect current item
            var deselectedItem = GetPicGallery.Container.Children[x] as Views.UserControls.PicGalleryItem;
            deselectedItem.innerborder.BorderBrush = Application.Current.Resources["BorderBrush"] as SolidColorBrush;
            deselectedItem.innerborder.Width = deselectedItem.innerborder.Height = picGalleryItem_Size;
        }
    }
}
