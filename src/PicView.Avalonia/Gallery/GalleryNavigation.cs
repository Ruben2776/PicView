using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;
using PicView.Core.Gallery;

namespace PicView.Avalonia.Gallery;

public static class GalleryNavigation
{
    #region Position and calculations
    
    private class GalleryItemPosition
    {
        public int Index { get; init; }
        public Point Position { get; init; }
        public Size Size { get; init; }
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

    
    #endregion

    public static void CenterScrollToSelectedItem(MainViewModel vm)
    {
        if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            // Use post to ensure the UI update takes place after resize
            Dispatcher.UIThread.Post(ScrollToSelected);;
        }
        else
        {
            Dispatcher.UIThread.Invoke(ScrollToSelected);
        }

        return;
        void ScrollToSelected()
        {
            var listbox = UIHelper.GetGalleryView.GalleryListBox;

            if (listbox is null || vm.SelectedGalleryItemIndex < 0 || vm.SelectedGalleryItemIndex >= listbox.Items.Count)
            {
                return;
            }

            try
            {
                listbox.ScrollToCenterOfItem(listbox.Items[vm.SelectedGalleryItemIndex] as GalleryItem);
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e);
#endif
            }
        }
    }
    
    public static void NavigateGallery(Direction direction, MainViewModel vm)
    {
        var highlightedGalleryItem = vm.SelectedGalleryItemIndex;
        var galleryItems = GetGalleryItems();

        if (highlightedGalleryItem < 0 || highlightedGalleryItem >= galleryItems.Count)
        {
            return;
        }

        var currentItem = galleryItems[highlightedGalleryItem];

        var targetItem = direction switch
        {
            Direction.Up => GetClosestItemAbove(currentItem, galleryItems),
            Direction.Down => GetClosestItemBelow(currentItem, galleryItems),
            Direction.Left => GetClosestItemLeft(currentItem, galleryItems),
            Direction.Right => GetClosestItemRight(currentItem, galleryItems),
            _ => null
        };

        if (targetItem != null)
        {
            SetHighlightedGalleryItem(vm, targetItem.Index);
        }
    }
    
    public static void NavigateGallery(bool last, MainViewModel vm)
    {
        var highlightedGalleryItem = vm.SelectedGalleryItemIndex;
        var galleryItems = GetGalleryItems();
        
        if (highlightedGalleryItem < 0 || highlightedGalleryItem >= galleryItems.Count)
        {
            return;
        }
        
        if (last)
        {
            SetHighlightedGalleryItem(vm, galleryItems.Count - 1);
        }
        else
        {
            SetHighlightedGalleryItem(vm, 0);
        }
    }

    private static List<GalleryItemPosition> GetGalleryItems()
    {
        var galleryItems = new List<GalleryItemPosition>();
        var galleryView = UIHelper.GetGalleryView;
        var listBox = galleryView.GalleryListBox;
        for (var i = 0; i < listBox.Items.Count; i++)
        {
            if (listBox.ContainerFromIndex(i) is not { } container)
            {
                continue;
            }

            var position = container.TranslatePoint(new Point(0, 0), galleryView);
            var size = container.Bounds.Size;
            if (position.HasValue)
            {
                galleryItems.Add(new GalleryItemPosition
                {
                    Index = i,
                    Position = position.Value,
                    Size = size
                });
            }
        }
        return galleryItems;
    }



    public static void SetHighlightedGalleryItem(MainViewModel vm, int index)
    {
        vm.SelectedGalleryItemIndex = index;
        CenterScrollToSelectedItem(vm); // Ensure the selected item is in view
    }


    public static async Task GalleryClick(MainViewModel? vm)
    {
        if (vm is null)
        {
            return;
        }

        if (!GalleryFunctions.IsFullGalleryOpen)
        {
            return;
        }
        await GalleryFunctions.ToggleGallery(vm);
        if (vm.SelectedGalleryItemIndex != vm.ImageIterator.CurrentIndex) 
        {
            await NavigationHelper.Navigate(vm.SelectedGalleryItemIndex, vm).ConfigureAwait(false);
        }
    }
}

