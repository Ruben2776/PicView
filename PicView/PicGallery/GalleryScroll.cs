using PicView.ChangeImage;
using System;
using System.Windows.Input;
using static PicView.UI.PicGallery.GalleryFunctions;
using static PicView.UI.UserControls.UC;

namespace PicView.UI.PicGallery
{
    internal static class GalleryScroll
    {
        #region int calculations

        private static int Horizontal_items
        {
            get
            {
                if (GetPicGallery == null)
                {
                    return 0;
                }

                return (int)Math.Floor(GetPicGallery.Width / picGalleryItem_Size);
            }
        }

        private static int Vertical_items
        {
            get
            {
                if (GetPicGallery == null)
                {
                    return 0;
                }

                return Properties.Settings.Default.PicGallery == 1 ?
                    (int)Math.Floor(GetPicGallery.Height / picGalleryItem_Size) : Navigation.Pics.Count;
            }
        }

        private static int Items_per_page
        {
            get
            {
                if (GetPicGallery == null)
                {
                    return 0;
                }

                return Properties.Settings.Default.PicGallery == 1 ?
                    Horizontal_items * Vertical_items :
                    (int)Math.Floor(GetPicGallery.Height / picGalleryItem_Size);
            }
        }

        private static int Current_page
        {
            get
            {
                return (int)Math.Floor((double)Navigation.FolderIndex / Items_per_page);
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
                GetPicGallery.Scroller.ScrollToHorizontalOffset((picGalleryItem_Size * Horizontal_items) * Current_page);
            }
            else
            {
                GetPicGallery.Scroller.ScrollToVerticalOffset((picGalleryItem_Size * Items_per_page) * Current_page);
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
    }
}