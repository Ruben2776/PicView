using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using PicView.Views.UserControls.Gallery;
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

        internal static void SetSize(int numberOfItems)
        {
            PicGalleryItem_Size = WindowSizing.MonitorInfo.WorkArea.Width / numberOfItems;

            PicGalleryItem_Size_s = !Properties.Settings.Default.FullscreenGalleryHorizontal ? PicGalleryItem_Size - 20 : PicGalleryItem_Size - 10;
        }

        internal static double PicGalleryItem_Size { get; private set; }
        internal static double PicGalleryItem_Size_s { get; private set; }

        internal static int Horizontal_items
        {
            get
            {
                if (GetPicGallery == null || PicGalleryItem_Size == 0) { return 0; }

                return (int)Math.Floor(GetPicGallery.Width / PicGalleryItem_Size);
            }
        }

        internal static int Vertical_items
        {
            get
            {
                if (GetPicGallery == null || PicGalleryItem_Size == 0) { return 0; }

                return (int)Math.Floor((GetPicGallery.Scroller.ViewportHeight - GetPicGallery.Container.Margin.Top) / PicGalleryItem_Size);
            }
        }

        internal static double CenterScrollPosition
        {
            get
            {
                if (GetPicGallery == null || PicGalleryItem_Size == 0) { return 0; }
                if (GetPicGallery.Container.Children.Count <= SelectedGalleryItem) { return 0; }

                var selectedScrollTo = GetPicGallery.Container.Children[SelectedGalleryItem].TranslatePoint(new Point(), GetPicGallery.Container);

                return selectedScrollTo.X - (Horizontal_items / 2) * PicGalleryItem_Size + (PicGalleryItem_Size_s / 2); // Scroll to overlap half of item
            }
        }

        #endregion int calculations

        #region ScrollTo

        /// <summary>
        /// Scrolls to center of current item
        /// </summary>
        internal static void ScrollTo()
        {
            if (GetPicGallery == null || PicGalleryItem_Size < 1) { return; }

            if (GalleryFunctions.IsHorizontalOpen)
            {
                if (GetPicGallery.Container.Children.Count < FolderIndex) { return; }

                var selectedItem = GetPicGallery.Container.Children[FolderIndex];
                var selectedScrollTo = selectedItem.TranslatePoint(new Point(), GetPicGallery.Container);
                GetPicGallery.Scroller.ScrollToHorizontalOffset(selectedScrollTo.X - (Horizontal_items / 2) * PicGalleryItem_Size + (PicGalleryItem_Size_s / 2));

                if (SelectedGalleryItem != FolderIndex)
                {
                    SetSelected(SelectedGalleryItem, false);
                    SelectedGalleryItem = FolderIndex;
                }
            }
            else if (GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                GetPicGallery.Scroller.ScrollToHorizontalOffset(CenterScrollPosition);
            }
            else
            {
                GetPicGallery.Scroller.ScrollToVerticalOffset(CenterScrollPosition);
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
                var speed = speedUp ? PicGalleryItem_Size * 4.7 : PicGalleryItem_Size / 2;
                var offset = next ? -speed : speed;

                var direction = GetPicGallery.Scroller.HorizontalOffset + offset;
                GetPicGallery.Scroller.ScrollToHorizontalOffset(direction);
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
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
            {
                if (GetPicGallery is not null && x > GetPicGallery.Container.Children.Count - 1 || x < 0) { return; }

                // Select next item
                var nextItem = GetPicGallery.Container.Children[x] as PicGalleryItem;

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
            });
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
            if (backup != SelectedGalleryItem && backup != FolderIndex)
            {
                SetSelected(backup, false); // deselect
            }

            if (direction == Direction.Up || direction == Direction.Down)
            {
                return;
            }
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
            {
                // Keep item in center of scrollviewer
                GetPicGallery.Scroller.ScrollToHorizontalOffset(CenterScrollPosition);
            });
        }

        internal static void FullscreenGalleryNavigation()
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
            {
                SetSelected(FolderIndex, true);
                SelectedGalleryItem = FolderIndex;

                if (Settings.Default.FullscreenGalleryHorizontal)
                {
                    GetPicGallery.Scroller.ScrollToHorizontalOffset(CenterScrollPosition);
                }
                else
                {
                    GetPicGallery.Scroller.ScrollToVerticalOffset(CenterScrollPosition);
                }

                Tooltip.CloseToolTipMessage();
            });
        }

        #endregion Horizontal Gallery Navigation
    }
}