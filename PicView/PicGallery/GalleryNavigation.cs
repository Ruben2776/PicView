using PicView.UILogic;
using System;
using System.Threading.Tasks;
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

                if (GalleryFunctions.IsHorizontalOpen)
                {
                    return (int)Math.Floor((GetPicGallery.Height - (GetPicGallery.Container.Margin.Top - GetPicGallery.Container.Margin.Bottom)) / PicGalleryItem_Size);
                }

                return GetPicGallery.Container.Children.Count;
            }
        }

        internal static int Items_per_page
        {
            get
            {
                if (GetPicGallery == null) { return 0; }

                if (GalleryFunctions.IsHorizontalOpen)
                {
                    return Horizontal_items * Vertical_items;
                }
                else if (GalleryFunctions.IsHorizontalFullscreenOpen)
                {
                    return (int)Math.Floor(GetPicGallery.Width / PicGalleryItem_Size);
                }
                else
                {
                    return (int)Math.Floor(GetPicGallery.Height / PicGalleryItem_Size);
                }
            }
        }

        internal static int Current_page
        {
            get
            {
                if (GalleryFunctions.IsHorizontalOpen)
                {
                    return (int)Math.Floor((double)SelectedGalleryItem / Items_per_page);
                }
                else
                {
                    return (int)Math.Floor((double)FolderIndex / Items_per_page);
                }
                
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

            if (GalleryFunctions.IsHorizontalOpen || GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                GetPicGallery.Scroller.ScrollToHorizontalOffset(PicGalleryItem_Size * Horizontal_items * Current_page);
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

                if (GalleryFunctions.IsHorizontalOpen)
                {
                    GetPicGallery.Scroller.ScrollToHorizontalOffset(direction);
                }
                else
                {
                    if (next)
                    {
                        if (Properties.Settings.Default.FullscreenGalleryVertical)
                        {
                            GetPicGallery.Scroller.ScrollToVerticalOffset(GetPicGallery.Scroller.VerticalOffset - speed);
                        }
                        else
                        {
                            GetPicGallery.Scroller.ScrollToHorizontalOffset(GetPicGallery.Scroller.HorizontalOffset - speed);
                        }
                    }
                    else
                    {
                        if (Properties.Settings.Default.FullscreenGalleryVertical)
                        {
                            GetPicGallery.Scroller.ScrollToVerticalOffset(GetPicGallery.Scroller.VerticalOffset + speed);
                        }
                        else
                        {
                            GetPicGallery.Scroller.ScrollToHorizontalOffset(GetPicGallery.Scroller.HorizontalOffset + speed);
                        }
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
            if (x > GetPicGallery.Container.Children.Count - 1 || x < 0) { return; }

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
            var backup = SelectedGalleryItem;

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

            if (SelectedGalleryItem >= Pics.Count -1)
            {
                SelectedGalleryItem = Pics.Count -1;
            }

            if (SelectedGalleryItem < 0)
            {
                SelectedGalleryItem = 0;
            }

            SetSelected(SelectedGalleryItem, true);
            if (backup != SelectedGalleryItem)
            {
                SetSelected(backup, false); // deselect
            }
            if (SelectedGalleryItem != FolderIndex)
            {
                SetSelected(FolderIndex, false); // deselect
            }            

            if (direction == Direction.Up || direction == Direction.Down)
            {
                return;
            }

            if (GetPicGallery.Scroller.HorizontalOffset > PicGalleryItem_Size * Horizontal_items * Current_page 
                || GetPicGallery.Scroller.HorizontalOffset < PicGalleryItem_Size * Horizontal_items * Current_page)
            {
                var offset = direction == Direction.Left ? GetPicGallery.Scroller.HorizontalOffset - PicGalleryItem_Size : GetPicGallery.Scroller.HorizontalOffset + PicGalleryItem_Size;
                GetPicGallery.Scroller.ScrollToHorizontalOffset(offset);
            }
        }

        internal static void FullscreenGalleryNavigation(int lastItem)
        {
            if (!GalleryFunctions.IsHorizontalFullscreenOpen && !GalleryFunctions.IsVerticalFullscreenOpen)
            {
                return;
            }

            if (GetPicGallery?.Container.Children.Count > FolderIndex && GetPicGallery.Container.Children.Count > lastItem)
            {
                if (lastItem != FolderIndex)
                {
                    SetSelected(lastItem, false);
                }

                SetSelected(FolderIndex, true);
            }
            else
            {
                // TODO Find way to get PicGalleryItem an alternative way...
            }

            if (Properties.Settings.Default.FullscreenGalleryHorizontal)
            {
                if (GetPicGallery.Scroller.HorizontalOffset > PicGalleryItem_Size * Horizontal_items * Current_page
                    || GetPicGallery.Scroller.HorizontalOffset < PicGalleryItem_Size * Horizontal_items * Current_page)
                {
                    var offset = ChangeImage.Navigation.Reverse ? GetPicGallery.Scroller.HorizontalOffset - PicGalleryItem_Size : GetPicGallery.Scroller.HorizontalOffset + PicGalleryItem_Size;
                    GetPicGallery.Scroller.ScrollToHorizontalOffset(offset);
                }
            }
            else
            {
                if (GetPicGallery.Scroller.VerticalOffset > PicGalleryItem_Size * Vertical_items * Current_page
                    || GetPicGallery.Scroller.VerticalOffset < PicGalleryItem_Size * Vertical_items * Current_page)
                {
                    var offset = ChangeImage.Navigation.Reverse ? GetPicGallery.Scroller.VerticalOffset - PicGalleryItem_Size : GetPicGallery.Scroller.VerticalOffset + PicGalleryItem_Size;
                    GetPicGallery.Scroller.ScrollToVerticalOffset(offset);
                }
            }

            Tooltip.CloseToolTipMessage();
        }

        internal static void FullscreenGallerySelection(Direction direction)
        {
            var backup = SelectedGalleryItem;
            if (direction == Direction.Up)
            {
                SelectedGalleryItem--;
            }
            else
            {
                SelectedGalleryItem++;
            }

            if (SelectedGalleryItem >= Pics.Count - 1)
            {
                SelectedGalleryItem = Pics.Count - 1;
            }

            if (SelectedGalleryItem < 0)
            {
                SelectedGalleryItem = 0;
            }

            SetSelected(SelectedGalleryItem, true);
            if (backup != SelectedGalleryItem)
            {
                SetSelected(backup, false); // deselect
            }
            if (SelectedGalleryItem != FolderIndex)
            {
                SetSelected(FolderIndex, false); // deselect
            }

            if (Reverse)
            {
                GetPicGallery.Scroller.ScrollToVerticalOffset(GetPicGallery.Scroller.VerticalOffset + PicGalleryItem_Size);
            }
            else
            {
                GetPicGallery.Scroller.ScrollToVerticalOffset(GetPicGallery.Scroller.VerticalOffset - PicGalleryItem_Size);
            }            
        }

        #endregion
    }
}