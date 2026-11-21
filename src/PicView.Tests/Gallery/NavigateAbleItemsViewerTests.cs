using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;

namespace PicView.Tests.Gallery;

[Collection("Sequential")]
public class NavigateAbleItemsViewerTests
{
    public NavigateAbleItemsViewerTests()
    {
        SetDefaults();
    }

    [AvaloniaFact]
    public void CurrentItemIndex_DoesNotModify_SelectedItemIndex()
    {
        var viewer = new NavigateAbleItemsViewer();
        Assert.Equal(-1, viewer.SelectedItemIndex);
        Assert.Equal(-1, viewer.CurrentItemIndex);

        viewer.CurrentItemIndex = 2;

        Assert.Equal(2, viewer.CurrentItemIndex);
        Assert.Equal(-1, viewer.SelectedItemIndex);
    }

    [AvaloniaFact]
    public void SelectedItemIndex_DoesNotModify_CurrentItemIndex()
    {
        var viewer = new NavigateAbleItemsViewer();
        viewer.CurrentItemIndex = 1;
        Assert.Equal(1, viewer.CurrentItemIndex);

        viewer.SelectedItemIndex = 4;

        Assert.Equal(1, viewer.CurrentItemIndex);
        Assert.Equal(4, viewer.SelectedItemIndex);
    }

    [AvaloniaFact]
    public void Viewer_EnforcesSingleCurrentAndSingleSelected()
    {
        RunWithDispatcher(Dispatcher.UIThread.InvokeAsync(() =>
        {
            var gallery = new VirtualizingGallery
            {
                ItemHeight = 50,
                Orientation = Orientation.Horizontal
            };

            var viewer = new NavigateAbleItemsViewer
            {
                ItemsPanel = new FuncTemplate<Panel?>(() => gallery),
                ItemTemplate = new FuncDataTemplate<string>((_, _) => new NavigateAbleItem()),
                ItemsSource = new[] { "A", "B", "C", "D", "E" }
            };

            var window = new Window { Width = 1000, Height = 500, Content = viewer };
            window.Show();

            viewer.CurrentItemIndex = 1;
            viewer.SelectedItemIndex = 3;

            for (int i = 0; i < 5; i++)
            {
                var presenter = viewer.ContainerFromIndex(i) as ContentPresenter;
                Assert.NotNull(presenter);
                var navItem = presenter.Child as NavigateAbleItem;
                Assert.NotNull(navItem);

                var isCurrent = navItem.Classes.Contains(":currentItem");
                var isSelected = navItem.Classes.Contains(":selectedItem");

                Assert.Equal(i == 1, isCurrent);
                Assert.Equal(i == 3, isSelected);
            }

            // Change current to 4
            viewer.CurrentItemIndex = 4;

            for (int i = 0; i < 5; i++)
            {
                var presenter = (ContentPresenter)viewer.ContainerFromIndex(i)!;
                var navItem = (NavigateAbleItem)presenter.Child!;

                Assert.Equal(i == 4, navItem.Classes.Contains(":currentItem"));
                Assert.Equal(i == 3, navItem.Classes.Contains(":selectedItem"));
            }

            // Clear selection
            viewer.SelectedItemIndex = -1;

            for (int i = 0; i < 5; i++)
            {
                var presenter = (ContentPresenter)viewer.ContainerFromIndex(i)!;
                var navItem = (NavigateAbleItem)presenter.Child!;

                Assert.Equal(i == 4, navItem.Classes.Contains(":currentItem"));
                Assert.DoesNotContain(":selectedItem", navItem.Classes);
            }
        }));
    }

    private const double Tolerance = 1;

    private sealed record Shown(Window Window, NavigateAbleItemsViewer Viewer, VirtualizingGallery Gallery)
    {
        public ScrollViewer ScrollViewer => Gallery.FindAncestorOfType<ScrollViewer>()!;
    }

    private static GalleryItemViewModel Item(uint width, uint height)
    {
        var item = new GalleryItemViewModel();
        item.SetDetails(new GalleryFileDetails(width, height));
        return item;
    }

    private static List<GalleryItemViewModel> Unsized(int count) =>
        Enumerable.Range(0, count).Select(_ => new GalleryItemViewModel()).ToList();

    /// <summary>
    /// The items in an Expanded Gallery in a 1000 by 400 window, with the app's margin. Columns is as
    /// VirtualizingGalleryTests.ShowExpanded shows it: 100-high items, three to a column. Masonry and Rows wrap at 300
    /// with an item size of 60, which gives a masonry three columns of 100, and put 10 between the items of a column.
    /// </summary>
    private static Shown ShowExpanded(GalleryDisplayMode displayMode, IReadOnlyList<GalleryItemViewModel> items,
        int currentIndex = 0)
    {
        var tiles = displayMode is not GalleryDisplayMode.Columns;
        var gallery = new VirtualizingGallery
        {
            IsExpanded = true,
            Orientation = Orientation.Vertical,
            DisplayMode = displayMode,
            ItemHeight = tiles ? 60 : 100,
            ItemSpacing = tiles ? 10 : 0,
            WrapWidthOverride = tiles ? 300 : double.NaN,
            Margin = new Thickness(15, 40, 15, 5)
        };
        var viewer = new NavigateAbleItemsViewer
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => gallery),
            ItemTemplate = new FuncDataTemplate<GalleryItemViewModel>((_, _) => new Border()),
            ItemsSource = items,
            CurrentItemIndex = currentIndex
        };
        var window = new Window { Width = 1000, Height = 400, Content = viewer };
        window.Show();
        if (tiles)
        {
            viewer.SetVerticalScrolling();
        }

        Dispatcher.UIThread.RunJobs();
        return new Shown(window, viewer, gallery);
    }

    // Pins today's Columns rules
    [AvaloniaFact]
    public void Navigate_Columns_KeepsItsRules()
    {
        var shown = ShowExpanded(GalleryDisplayMode.Columns, Unsized(200), currentIndex: 150);
        try
        {
            // What the test relies on: three squares to a column, so item 150 is the top of column 50
            var bounds = shown.Gallery.ItemBounds;
            Assert.Equal(new Rect(5000, 0, 100, 100), bounds[150]);
            Assert.Equal(new Rect(5000, 200, 100, 100), bounds[152]);
            Assert.Equal(new Rect(5100, 0, 100, 100), bounds[153]);

            // Nothing is selected, so the arrows start from the current item
            shown.Viewer.Navigate(NavigationDirection.Down);
            Assert.Equal(151, shown.Viewer.SelectedItemIndex);
            shown.Viewer.Navigate(NavigationDirection.Down);
            Assert.Equal(152, shown.Viewer.SelectedItemIndex);
            // Down from the bottom of a column wraps to the top of the next one
            shown.Viewer.Navigate(NavigationDirection.Down);
            Assert.Equal(153, shown.Viewer.SelectedItemIndex);
            // Up from the top of a column wraps to the bottom of the one before
            shown.Viewer.Navigate(NavigationDirection.Up);
            Assert.Equal(152, shown.Viewer.SelectedItemIndex);
            shown.Viewer.Navigate(NavigationDirection.Right);
            Assert.Equal(155, shown.Viewer.SelectedItemIndex);
            shown.Viewer.Navigate(NavigationDirection.Left);
            Assert.Equal(152, shown.Viewer.SelectedItemIndex);
        }
        finally
        {
            shown.Window.Close();
        }
    }

    // Pins today's Columns scroll for a target without a container: the panel's coordinates, without its margin
    [AvaloniaFact]
    public void Navigate_ColumnsToAnItemWithoutAContainer_ScrollsToItsPanelCoordinates()
    {
        var shown = ShowExpanded(GalleryDisplayMode.Columns, Unsized(200), currentIndex: 150);
        try
        {
            Assert.Null(shown.Viewer.ContainerFromIndex(199));

            shown.Viewer.Navigate(NavigationDirection.Last);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(199, shown.Viewer.SelectedItemIndex);
            var last = shown.Gallery.ItemBounds[199];
            Assert.Equal(last.Right - shown.ScrollViewer.Viewport.Width, shown.ScrollViewer.Offset.X, Tolerance);
        }
        finally
        {
            shown.Window.Close();
        }
    }

    [AvaloniaFact]
    public void Navigate_MasonryDownAndUp_StayInTheColumnAndDoNotWrap()
    {
        var shown = ShowExpanded(GalleryDisplayMode.Masonry,
        [
            Item(100, 200), Item(100, 100), Item(100, 100), Item(500, 400),
            Item(100, 100), Item(100, 100), Item(100, 100), Item(100, 100)
        ]);
        try
        {
            // What the test relies on, from the placement task: item 6 is under item 0, and item 5 starts higher than
            // item 6 but in the next column
            var bounds = shown.Gallery.ItemBounds;
            Assert.Equal(new Rect(0, 0, 100, 200), bounds[0]);
            Assert.Equal(new Rect(100, 200, 100, 100), bounds[5]);
            Assert.Equal(new Rect(0, 210, 100, 100), bounds[6]);

            shown.Viewer.SelectedItemIndex = 0;
            shown.Viewer.Navigate(NavigationDirection.Down);
            Assert.Equal(6, shown.Viewer.SelectedItemIndex);

            // Nothing is under item 6, and the selection stays rather than wrap to the next column
            shown.Viewer.Navigate(NavigationDirection.Down);
            Assert.Equal(6, shown.Viewer.SelectedItemIndex);

            shown.Viewer.Navigate(NavigationDirection.Up);
            Assert.Equal(0, shown.Viewer.SelectedItemIndex);
        }
        finally
        {
            shown.Window.Close();
        }
    }

    [AvaloniaFact]
    public void Navigate_RowsRightAtTheEndOfARow_SelectsTheNextItem()
    {
        var shown = ShowExpanded(GalleryDisplayMode.Rows, Unsized(12));
        try
        {
            var bounds = shown.Gallery.ItemBounds;
            var rowEnd = 0;
            while (bounds[rowEnd + 1].Y <= bounds[rowEnd].Y)
            {
                rowEnd++;
            }

            shown.Viewer.SelectedItemIndex = rowEnd;
            shown.Viewer.Navigate(NavigationDirection.Right);
            Assert.Equal(rowEnd + 1, shown.Viewer.SelectedItemIndex);

            shown.Viewer.Navigate(NavigationDirection.Left);
            Assert.Equal(rowEnd, shown.Viewer.SelectedItemIndex);
        }
        finally
        {
            shown.Window.Close();
        }
    }

    [AvaloniaFact]
    public void Navigate_MasonryToAnItemWithoutAContainer_ScrollsAllOfItIntoView()
    {
        var shown = ShowExpanded(GalleryDisplayMode.Masonry, Unsized(200));
        try
        {
            Assert.Null(shown.Viewer.ContainerFromIndex(199));

            shown.Viewer.Navigate(NavigationDirection.Last);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(199, shown.Viewer.SelectedItemIndex);
            var viewportHeight = shown.ScrollViewer.Viewport.Height;
            var container = shown.Viewer.ContainerFromIndex(199);
            Assert.NotNull(container);
            var top = container.TranslatePoint(default, shown.ScrollViewer)!.Value.Y;
            Assert.InRange(top, -Tolerance, viewportHeight - container.Bounds.Height + Tolerance);

            // The scroll viewer scrolls over the panel's top margin of 40 too
            var last = shown.Gallery.ItemBounds[199];
            Assert.Equal(40 + last.Bottom - viewportHeight, shown.ScrollViewer.Offset.Y, Tolerance);
        }
        finally
        {
            shown.Window.Close();
        }
    }

    private static void RunWithDispatcher(DispatcherOperation operation)
    {
        while (operation.Status is not DispatcherOperationStatus.Completed and not DispatcherOperationStatus.Aborted)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
        operation.GetAwaiter().GetResult();
    }
}
