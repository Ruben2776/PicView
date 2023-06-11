using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using PicView.Views.UserControls.Gallery;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.UC;

namespace PicView.PicGallery;

internal static class GalleryNavigation
{
    #region int calculations

    internal static void SetSize(int numberOfItems)
    {
        PicGalleryItemSize = WindowSizing.MonitorInfo.WorkArea.Width / numberOfItems;

        PicGalleryItemSizeS = !Settings.Default.FullscreenGallery ? PicGalleryItemSize - 20 : PicGalleryItemSize - 10;
    }

    internal static double PicGalleryItemSize { get; private set; }
    internal static double PicGalleryItemSizeS { get; private set; }

    private static int HorizontalItems
    {
        get
        {
            if (GetPicGallery == null || PicGalleryItemSize == 0) { return 0; }

            return (int)Math.Floor(GetPicGallery.Width / PicGalleryItemSize);
        }
    }

    private static int VerticalItems
    {
        get
        {
            if (GetPicGallery == null || PicGalleryItemSize == 0) { return 0; }

            return (int)Math.Floor((GetPicGallery.Scroller.ViewportHeight - GetPicGallery.Container.Margin.Top) / PicGalleryItemSize);
        }
    }

    internal static double CenterScrollPosition
    {
        get
        {
            if (GetPicGallery == null || PicGalleryItemSize == 0) { return 0; }
            if (GetPicGallery.Container.Children.Count <= SelectedGalleryItem) { return 0; }

            var selectedScrollTo = GetPicGallery.Container.Children[SelectedGalleryItem].TranslatePoint(new Point(), GetPicGallery.Container);

            // ReSharper disable once PossibleLossOfFraction
            return selectedScrollTo.X - (HorizontalItems / 2) * PicGalleryItemSize + (PicGalleryItemSizeS / 2); // Scroll to overlap half of item
        }
    }

    #endregion int calculations

    #region ScrollToGalleryCenter

    /// <summary>
    /// Scrolls to center of current item
    /// </summary>
    internal static void ScrollToGalleryCenter()
    {
        if (GetPicGallery == null || PicGalleryItemSize < 1) { return; }
        if (!GalleryFunctions.IsGalleryOpen) return;

        if (GetPicGallery.Container.Children.Count < FolderIndex || GetPicGallery.Container.Children.Count <= 0) { return; }

        GetPicGallery.Scroller.CanContentScroll = false; // Enable animations
        GetPicGallery.Scroller.ScrollToHorizontalOffset(CenterScrollPosition);
        GetPicGallery.Scroller.ScrollToHorizontalOffset(CenterScrollPosition);
    }

    /// <summary>
    /// Scrolls the gallery horizontally based on the specified parameters.
    /// </summary>
    /// <param name="next">Specifies whether to scroll to the next item or the previous item.</param>
    /// <param name="end">Specifies whether to scroll to the end of the gallery.</param>
    /// <param name="speedUp">Specifies whether to scroll at a faster speed.</param>
    /// <param name="animate">Specifies whether to animate the scrolling.</param>
    internal static void ScrollGallery(bool next, bool end, bool speedUp, bool animate)
    {
        GetPicGallery.Scroller.CanContentScroll = !animate; // Base animations on CanContentScroll

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
            if (Settings.Default.FullscreenGallery)
            {
                if (animate)
                {
                    var speed = speedUp ? PicGalleryItemSize * HorizontalItems * 0.8 : PicGalleryItemSize * HorizontalItems * 0.1;
                    var offset = next ? -speed : speed;

                    var direction = GetPicGallery.Scroller.HorizontalOffset + offset;
                    GetPicGallery.Scroller.ScrollToHorizontalOffset(direction);
                }
                else
                {
                    var speed = speedUp ? PicGalleryItemSize * HorizontalItems * 1.2 : PicGalleryItemSize * HorizontalItems * 0.2;
                    var offset = next ? -speed : speed;

                    var direction = GetPicGallery.Scroller.HorizontalOffset + offset;
                    GetPicGallery.Scroller.ScrollToHorizontalOffset(direction);
                }
            }
            else
            {
                if (animate)
                {
                    var speed = speedUp ? PicGalleryItemSize * HorizontalItems * 0.8 : PicGalleryItemSize * HorizontalItems / 1.2;
                    var offset = next ? -speed : speed;

                    var newOffset = GetPicGallery.Scroller.HorizontalOffset + offset;

                    GetPicGallery.Scroller.ScrollToHorizontalOffset(newOffset);
                }
                else
                {
                    var speed = speedUp ? PicGalleryItemSize * HorizontalItems * 1.2 : PicGalleryItemSize * HorizontalItems * 0.3;
                    var offset = next ? -speed : speed;

                    var newOffset = GetPicGallery.Scroller.HorizontalOffset + offset;

                    GetPicGallery.Scroller.ScrollToHorizontalOffset(newOffset);
                }
            }
        }
    }

    #endregion ScrollToGalleryCenter

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
                nextItem.InnerBorder.BorderBrush = Application.Current.Resources["ChosenColorBrush"] as SolidColorBrush;
                nextItem.InnerBorder.Width = nextItem.InnerBorder.Height = PicGalleryItemSize;
            }
            else
            {
                nextItem.InnerBorder.BorderBrush = Application.Current.Resources["BorderBrush"] as SolidColorBrush;
                nextItem.InnerBorder.Width = nextItem.InnerBorder.Height = PicGalleryItemSizeS;
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
                SelectedGalleryItem -= VerticalItems;
                break;

            case Direction.Right:
                SelectedGalleryItem += VerticalItems;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
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

        if (direction is Direction.Up or Direction.Down)
        {
            return;
        }
        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
        {
            // Keep item in center of ScrollViewer
            GetPicGallery.Scroller.ScrollToHorizontalOffset(CenterScrollPosition);
        });
    }

    internal static void FullscreenGalleryNavigation()
    {
        SetSelected(FolderIndex, true);
        SelectedGalleryItem = FolderIndex;

        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
        {
            GetPicGallery.Scroller.CanContentScroll = true; // Disable animations
            GetPicGallery.Scroller.ScrollToHorizontalOffset(CenterScrollPosition);
        });

        Tooltip.CloseToolTipMessage();
    }

    #endregion Horizontal Gallery Navigation
}