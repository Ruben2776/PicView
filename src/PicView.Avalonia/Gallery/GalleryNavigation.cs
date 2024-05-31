using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;

namespace PicView.Avalonia.Gallery;
public enum Direction
{
    Up,
    Down,
    Left,
    Right
}
public static class GalleryNavigation
{
    public static void CenterScrollToSelectedItem(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            ScrollToSelected();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(ScrollToSelected);
        }
        
        return;
        void ScrollToSelected()
        {
            var mainView = desktop.MainWindow.GetControl<MainView>("MainView");
            var mainGrid = mainView.GetControl<Panel>("MainGrid");
            var galleryView = mainGrid.GetControl<GalleryAnimationControlView>("GalleryView");
            galleryView.GalleryListBox.ScrollIntoView(vm.SelectedGalleryItemIndex);
            
        }
    }
    
    public static void NavigateGallery(Direction direction, MainViewModel vm)
    {
        var highlightedGalleryItem = vm.SelectedGalleryItemIndex;
        var galleryItems = GetGalleryItems(vm);

        if (highlightedGalleryItem < 0 || highlightedGalleryItem >= galleryItems.Count)
        {
            return;
        }

        var currentItem = galleryItems[highlightedGalleryItem];

        GalleryItemPosition? targetItem = direction switch
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

    private static List<GalleryItemPosition> GetGalleryItems(MainViewModel vm)
    {
        var galleryItems = new List<GalleryItemPosition>();
        var galleryView = GetGallery(Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);
        var listBox = galleryView.GalleryListBox;
        for (int i = 0; i < listBox.Items.Count; i++)
        {
            if (listBox.ItemContainerGenerator.ContainerFromIndex(i) is Control container)
            {
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

    public static void SetHighlightedGalleryItem(MainViewModel vm, int index)
    {
        vm.SelectedGalleryItemIndex = index;
        CenterScrollToSelectedItem(vm); // Ensure the selected item is in view
    }

    private static GalleryAnimationControlView GetGallery(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var mainView = desktop.MainWindow.GetControl<MainView>("MainView");
        var mainGrid = mainView.GetControl<Panel>("MainGrid");
        var galleryView = mainGrid.GetControl<GalleryAnimationControlView>("GalleryView");

        return galleryView;
    }

    private class GalleryItemPosition
    {
        public int Index { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; }
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
        if (vm.SelectedGalleryItemIndex != vm.ImageIterator.Index) 
        {
            await vm.ImageIterator.LoadPicAtIndex(vm.SelectedGalleryItemIndex, vm);
        }
    }
}

