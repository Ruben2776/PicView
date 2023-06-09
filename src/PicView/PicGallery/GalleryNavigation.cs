using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using PicView.Views.UserControls.Gallery;
using System.Windows;
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

    private static double CenterScrollPosition
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

    #region ScrollTo

    /// <summary>
    /// Scrolls to center of current item
    /// </summary>
    internal static void ScrollTo()
    {
        if (GetPicGallery == null || PicGalleryItemSize < 1) { return; }
        if (!GalleryFunctions.IsGalleryOpen) return;

        if (Settings.Default.FullscreenGallery)
        {
            GetPicGallery.Scroller.ScrollToHorizontalOffset(CenterScrollPosition);
        }
        else
        {
            if (GetPicGallery.Container.Children.Count < FolderIndex) { return; }

            var selectedItem = GetPicGallery.Container.Children[FolderIndex];
            var selectedScrollTo = selectedItem.TranslatePoint(new Point(), GetPicGallery.Container);
            // ReSharper disable once PossibleLossOfFraction
            GetPicGallery.Scroller.ScrollToHorizontalOffset(selectedScrollTo.X - HorizontalItems / 2 * PicGalleryItemSize + (PicGalleryItemSizeS / 2));
        }
    }

    /// <summary>
    /// Scrolls a page back or forth
    /// </summary>
    /// <param name="next"></param>
    /// <param name="end"></param>
    /// <param name="speedUp"></param>
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
            var speed = speedUp ? PicGalleryItemSize * 4.7 : PicGalleryItemSize / 2;
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
            if (Settings.Default.FullscreenGallery)
            {
                GetPicGallery.Scroller.ScrollToHorizontalOffset(CenterScrollPosition);
            }
            else
            {
                GetPicGallery.Scroller.ScrollToVerticalOffset(CenterScrollPosition);
            }
        });

        Tooltip.CloseToolTipMessage();
    }

    #endregion Horizontal Gallery Navigation
}