using PicView.UILogic;
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

        internal static int PicGalleryItem_Size { get; set; }
        internal static int PicGalleryItem_Size_s { get; set; }

        internal static int Horizontal_items
        {
            get
            {
                if (GetPicGallery == null) { return 0; }

                return (int)Math.Floor(GetPicGallery.Width / PicGalleryItem_Size);
            }
        }

        internal static int Vertical_items
        {
            get
            {
                if (GetPicGallery == null) { return 0; }

                return Properties.Settings.Default.FullscreenGalleryVertical == false ?
                    (int)Math.Round((GetPicGallery.Height - (GetPicGallery.Margin.Top - GetPicGallery.Margin.Bottom)) / PicGalleryItem_Size) : Pics.Count;
            }
        }

        internal static int Items_per_page
        {
            get
            {
                if (GetPicGallery == null) { return 0; }

                if (ShouldHorizontalNavigate())
                {
                    return (int)Math.Floor(GetPicGallery.Height / PicGalleryItem_Size);
                }

                return Horizontal_items * Vertical_items;
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
            if (GetPicGallery == null) { return; }

            if (ShouldHorizontalNavigate())
            {
                GetPicGallery.Scroller.ScrollToHorizontalOffset(PicGalleryItem_Size * Items_per_page * Current_page);
            }
            else
            {
                GetPicGallery.Scroller.ScrollToVerticalOffset(PicGalleryItem_Size * Items_per_page * Current_page);
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
                var speed = speedUp ? PicGalleryItem_Size * 4.7 : PicGalleryItem_Size;
                var direction = next ? GetPicGallery.Scroller.HorizontalOffset - speed : GetPicGallery.Scroller.HorizontalOffset + speed;

                if (ShouldHorizontalNavigate())
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

        #region Select and deselect behaviour

        /// <summary>
        /// Select and deselect PicGalleryItem
        /// </summary>
        /// <param name="x">location</param>
        /// <param name="selected">selected or deselected</param>
        internal static void SetSelected(int x, bool selected)
        {
            if (x > GetPicGallery.Container.Children.Count || x < 0) { return; }

            // Select next item
            var nextItem = GetPicGallery.Container.Children[x] as Views.UserControls.PicGalleryItem;

            if (selected)
            {
                nextItem.innerborder.BorderBrush = Application.Current.Resources["ChosenColorBrush"] as SolidColorBrush;
                nextItem.innerborder.Width = nextItem.innerborder.Height = PicGalleryItem_Size;
            }
            else
            {
                nextItem.innerborder.BorderBrush = Application.Current.Resources["BorderBrush"] as SolidColorBrush;
                nextItem.innerborder.Width = nextItem.innerborder.Height = PicGalleryItem_Size_s;
            }

#if DEBUG
            System.Diagnostics.Trace.WriteLine(nameof(SetSelected) + " " + x.ToString() + " = " + nameof(selected) + " " + selected.ToString());
#endif
        }

        #endregion Select and deselect behaviour

        #region Horizontal Gallery Navigation

        internal enum Direction
        {
            Up,
            Down,
            Left,
            Right
        }

        internal static int SelectedGalleryItem { get; set; }
        internal static void HorizontalNavigation(Direction direction)
        {
            SetSelected(SelectedGalleryItem, false); // deselect

            switch (direction)
            {
                case Direction.Up:
                    SelectedGalleryItem--;
                    break;
                case Direction.Down:
                    SelectedGalleryItem++;
                    break;
                case Direction.Left:
                    SelectedGalleryItem = SelectedGalleryItem - Vertical_items;
                    break;
                case Direction.Right:
                    SelectedGalleryItem = SelectedGalleryItem + Vertical_items;
                    break;
                default:
                    break;
            }

            SelectedGalleryItem = SelectedGalleryItem > Pics.Count ? Pics.Count : SelectedGalleryItem;
            SelectedGalleryItem = SelectedGalleryItem < 0 ? 0 : SelectedGalleryItem;
            SetSelected(SelectedGalleryItem, true);

            //if (false) // Figure out how to calculate when to change page
            //{
            //    var offset = direction == Direction.Left ? GetPicGallery.Scroller.HorizontalOffset - PicGalleryItem_Size : GetPicGallery.Scroller.HorizontalOffset + PicGalleryItem_Size;
            //    GetPicGallery.Scroller.ScrollToHorizontalOffset(offset);
            //}
        }

        internal static bool ShouldHorizontalNavigate()
        {
            if (GetPicGallery != null && GalleryFunctions.IsOpen)
            {
                if (ConfigureWindows.GetFakeWindow is null || ConfigureWindows.GetFakeWindow is not null && ConfigureWindows.GetFakeWindow.IsVisible == false)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}