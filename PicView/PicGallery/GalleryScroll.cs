using System;
using System.Windows.Input;
using static PicView.Fields;
using static PicView.UC;


namespace PicView
{
    // TODO Get scrolling calculation working for PicGallery 2

    internal static class GalleryScroll
    {
        #region int calculations

        static int Horizontal_items
        {
            get
            {
                if (picGallery == null)
                {
                    return 0;
                }

                return (int)Math.Floor(picGallery.Width / picGalleryItem_Size);
            }
        }

        static int Vertical_items
        {
            get
            {
                if (picGallery == null)
                {
                    return 0;
                }

                return Properties.Settings.Default.PicGallery == 1 ?
                    (int)Math.Floor(picGallery.Height / picGalleryItem_Size) :
                    Pics.Count;
            }
        }

        static int Items_per_page
        {
            get
            {
                if (picGallery == null)
                {
                    return 0;
                }

                return Properties.Settings.Default.PicGallery == 1 ?
                    Horizontal_items * Vertical_items :
                    (int)Math.Floor(picGallery.Height / picGalleryItem_Size);
            }
        }

        static int Current_page
        {
            get
            {
                return (int)Math.Floor((double)FolderIndex / Items_per_page);
            }
        }

        //static int Total_pages
        // {
        //     get
        //     {
        //         return (int)Math.Floor((double)Pics.Count / Items_per_page);
        //     }
        // }

        #endregion

        #region ScrollTo

        /// <summary>
        /// Scrolls to center of current item
        /// </summary>
        /// <param name="item">The index of picGalleryItem</param>
        internal static void ScrollTo()
        {
            if (Properties.Settings.Default.PicGallery == 1)
            {
                picGallery.Scroller.ScrollToHorizontalOffset((picGalleryItem_Size * Horizontal_items) * Current_page);
            }
            else
            {
                picGallery.Scroller.ScrollToVerticalOffset((picGalleryItem_Size * Items_per_page) * Current_page);
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
                    picGallery.Scroller.ScrollToRightEnd();
                }
                else
                {
                    picGallery.Scroller.ScrollToLeftEnd();
                }
            }
            else
            {
                var speed = speedUp ? picGalleryItem_Size * 4.7 : picGalleryItem_Size;
                var direction = next ? picGallery.Scroller.HorizontalOffset - speed : picGallery.Scroller.HorizontalOffset + speed;

                if (Properties.Settings.Default.PicGallery == 1)
                {
                    picGallery.Scroller.ScrollToHorizontalOffset(direction);
                }
                else
                {
                    if (next)
                    {
                        picGallery.Scroller.ScrollToVerticalOffset(picGallery.Scroller.VerticalOffset - speed);
                    }
                    else
                    {
                        picGallery.Scroller.ScrollToVerticalOffset(picGallery.Scroller.VerticalOffset + speed);
                    }
                }
            }
        }

        #endregion

    }
}
