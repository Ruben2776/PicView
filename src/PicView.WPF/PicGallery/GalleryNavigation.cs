using PicView.WPF.UILogic;
using PicView.WPF.Views.UserControls.Gallery;
using System.Windows;
using System.Windows.Media;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.PicGallery;

internal static class GalleryNavigation
{
    #region int calculations

    internal static void SetSize(double newSize)
    {
        PicGalleryItemSize = newSize;

        PicGalleryItemSizeS = PicGalleryItemSize - 15;
    }

    internal static double PicGalleryItemSize { get; private set; }
    internal static double PicGalleryItemSizeS { get; private set; }

    internal const int ScrollbarSize = 22;

    internal static int HorizontalItems
    {
        get
        {
            if (GetPicGallery == null || PicGalleryItemSize == 0)
            {
                return 0;
            }

            return (int)Math.Floor(GetPicGallery.Width / PicGalleryItemSize);
        }
    }

    internal static int VerticalItems
    {
        get
        {
            if (GetPicGallery == null || PicGalleryItemSize == 0)
            {
                return 0;
            }

            return (int)Math.Floor((ConfigureWindows.GetMainWindow.ParentContainer.ActualHeight -
                                    GetPicGallery.Container.Margin.Top) / PicGalleryItemSize);
        }
    }

    internal static int ItemsPerPage
    {
        get
        {
            if (GetPicGallery == null)
            {
                return 0;
            }

            return (int)Math.Floor(HorizontalItems * GetPicGallery.Height / PicGalleryItemSize);
        }
    }

    internal static double CenterScrollPosition
    {
        get
        {
            if (GetPicGallery == null || PicGalleryItemSize <= 0)
            {
                return 0;
            }

            if (GetPicGallery.Container.Children.Count <= SelectedGalleryItem)
            {
                return 0;
            }

            if (SelectedGalleryItem < 0)
            {
                return 0;
            }

            var selectedScrollTo = GetPicGallery.Container.Children[SelectedGalleryItem]
                .TranslatePoint(new Point(), GetPicGallery.Container);

            // ReSharper disable once PossibleLossOfFraction
            return selectedScrollTo.X - (HorizontalItems + 1) / 2 * PicGalleryItemSize +
                   PicGalleryItemSizeS / 2; // Scroll to overlap half of item
        }
    }

    #endregion int calculations

    #region ScrollToGalleryCenter

    /// <summary>
    /// Scrolls to center of current item
    /// </summary>
    internal static void ScrollToGalleryCenter()
    {
        var centerScrollPosition = CenterScrollPosition;
        if (centerScrollPosition == 0)
        {
            return;
        }

        GetPicGallery.Scroller.ScrollToHorizontalOffset(centerScrollPosition);
    }

    /// <summary>
    /// Scrolls the gallery horizontally based on the specified parameters.
    /// </summary>
    /// <param name="next">Specifies whether to scroll to the next or the previous page.</param>
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
            if (animate)
            {
                var speed = speedUp
                    ? PicGalleryItemSize * HorizontalItems * 0.8
                    : PicGalleryItemSize * HorizontalItems / 1.2;
                var offset = next ? -speed : speed;

                var newOffset = GetPicGallery.Scroller.HorizontalOffset + offset;

                GetPicGallery.Scroller.ScrollToHorizontalOffset(newOffset);
            }
            else
            {
                var speed = speedUp
                    ? PicGalleryItemSize * HorizontalItems * 1.2
                    : PicGalleryItemSize * HorizontalItems * 0.3;
                var offset = next ? -speed : speed;

                var newOffset = GetPicGallery.Scroller.HorizontalOffset + offset;

                GetPicGallery.Scroller.ScrollToHorizontalOffset(newOffset);
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
        if (GetPicGallery is not null && x > GetPicGallery.Container.Children.Count - 1 || x < 0)
        {
            return;
        }

        // Select next item
        if (GetPicGallery?.Container?.Children[x] is not PicGalleryItem nextItem)
        {
            return;
        }

        if (selected && x == FolderIndex)
        {
            nextItem.InnerBorder.BorderBrush = Application.Current.Resources["ChosenColorBrush"] as SolidColorBrush;
        }
        else
        {
            nextItem.InnerBorder.BorderBrush = Application.Current.Resources["BorderBrush"] as SolidColorBrush;
        }

        while (GetPicGallery.Container.Children.Count > Pics.Count)
        {
            GetPicGallery.Container.Children.RemoveAt(GetPicGallery.Container.Children.Count - 1);
        }
    }

    #endregion Select and deselect behaviour

    #region Gallery Navigation

    internal enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    internal static int SelectedGalleryItem { get; set; }

    internal static void NavigateGallery(Direction direction)
    {
        var backup = SelectedGalleryItem;
        var galleryItems = GetGalleryItems();

        if (SelectedGalleryItem < 0 || SelectedGalleryItem >= galleryItems.Count)
        {
            return;
        }

        var currentItem = galleryItems[SelectedGalleryItem];

        var targetItem = direction switch
        {
            Direction.Up => GetClosestItemAbove(currentItem, galleryItems),
            Direction.Down => GetClosestItemBelow(currentItem, galleryItems),
            Direction.Left => GetClosestItemLeft(currentItem, galleryItems),
            Direction.Right => GetClosestItemRight(currentItem, galleryItems),
            _ => null
        };

        SelectedGalleryItem = targetItem?.Index ?? SelectedGalleryItem;
        
        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
        {
            if (GetPicGallery?.Container?.Children[SelectedGalleryItem] is PicGalleryItem nextItem && 
                GetPicGallery?.Container?.Children[backup] is PicGalleryItem prevItem)
            {
                nextItem.InnerBorder.BorderBrush = Application.Current.Resources["ChosenColorBrush"] as SolidColorBrush;
                prevItem.InnerBorder.BorderBrush = Application.Current.Resources["BorderBrush"] as SolidColorBrush;
            }

            // Keep item in center of ScrollViewer
            GetPicGallery.Scroller.ScrollToHorizontalOffset(CenterScrollPosition);
        });
    }
    
    private class GalleryItemPosition
    {
        public int Index { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; }
    }
    
    private static List<GalleryItemPosition> GetGalleryItems()
    {
        var galleryItems = new List<GalleryItemPosition>();
        for (var i = 0; i < GetPicGallery.Container.Children.Count; i++)
        {
            if (GetPicGallery.Container.Children[i] is not PicGalleryItem item)
            {
                continue;
            }
            var position = GetPicGallery.Container.Children[i].TranslatePoint(new Point(), GetPicGallery.Container);
            galleryItems.Add(new GalleryItemPosition
            {
                Index = i,
                Position = position,
                Size = new Size(item.ActualWidth, item.ActualHeight)
            });
        }
        return galleryItems;
    }
    
    private static GalleryItemPosition? GetClosestItemAbove(GalleryItemPosition currentItem, IEnumerable<GalleryItemPosition> items)
    {
        var candidates = items.Where(item => item.Position.Y + item.Size.Height <= currentItem.Position.Y).ToList();
        return candidates.OrderByDescending(item => item.Position.Y).ThenBy(item => Math.Abs(item.Position.X - currentItem.Position.X)).FirstOrDefault();
    }

    private static GalleryItemPosition? GetClosestItemBelow(GalleryItemPosition currentItem, IEnumerable<GalleryItemPosition> items)
    {
        var candidates = items.Where(item => item.Position.Y >= currentItem.Position.Y + currentItem.Size.Height).ToList();
        return candidates.OrderBy(item => item.Position.Y).ThenBy(item => Math.Abs(item.Position.X - currentItem.Position.X)).FirstOrDefault();
    }

    private static GalleryItemPosition? GetClosestItemLeft(GalleryItemPosition currentItem, IEnumerable<GalleryItemPosition> items)
    {
        var candidates = items.Where(item => item.Position.X + item.Size.Width <= currentItem.Position.X).ToList();
        return candidates.OrderByDescending(item => item.Position.X).ThenBy(item => Math.Abs(item.Position.Y - currentItem.Position.Y)).FirstOrDefault();
    }

    private static GalleryItemPosition? GetClosestItemRight(GalleryItemPosition currentItem, IEnumerable<GalleryItemPosition> items)
    {
        var candidates = items.Where(item => item.Position.X >= currentItem.Position.X + currentItem.Size.Width).ToList();
        return candidates.OrderBy(item => item.Position.X).ThenBy(item => Math.Abs(item.Position.Y - currentItem.Position.Y)).FirstOrDefault();
    }

    #endregion Gallery Navigation
}