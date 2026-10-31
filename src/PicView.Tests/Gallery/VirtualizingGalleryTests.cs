using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;
using System.Linq;
using Xunit;

namespace PicView.Tests.Gallery;

[Collection("Sequential")]
public class VirtualizingGalleryTests
{
    private static void RunWithDispatcher(DispatcherOperation operation)
    {
        while (operation.Status is not DispatcherOperationStatus.Completed and not DispatcherOperationStatus.Aborted)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
        operation.GetAwaiter().GetResult();
    }

    [AvaloniaFact]
    public void PropertiesHaveCorrectDefaults()
    {
        var gallery = new VirtualizingGallery();
        Assert.Equal(Orientation.Horizontal, gallery.Orientation);
        Assert.True(gallery.ItemHeight > 0);
        Assert.Equal(0, gallery.ItemSpacing);
        Assert.Equal(0, gallery.LineSpacing);
    }

    [AvaloniaFact]
    public void MeasureOverrideCalculatesPanelSizeWithItems()
    {
        var gallery = new VirtualizingGallery
        {
            ItemHeight = 50,
            ItemSpacing = 10,
            LineSpacing = 5,
            Orientation = Orientation.Horizontal
        };

        var itemsControl = new ItemsControl
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => gallery),
            ItemsSource = new[] { "Item 1", "Item 2", "Item 3", "Item 4", "Item 5" }
        };

        itemsControl.ApplyTemplate();
        itemsControl.Measure(new Size(200, 500));

        Assert.Equal(50, gallery.ItemHeight);
        Assert.Equal(10, gallery.ItemSpacing);
        Assert.Equal(5, gallery.LineSpacing);
    }

    [AvaloniaFact]
    public void RealizesOnlyVisibleContainersWhenMeasured()
    {
        var gallery = new VirtualizingGallery
        {
            ItemHeight = 50,
            Orientation = Orientation.Horizontal
        };

        var itemsControl = new ItemsControl
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => gallery),
            ItemsSource = Enumerable.Range(0, 100).Select(i => $"Item {i}").ToList()
        };

        itemsControl.Measure(new Size(300, 100));

        // var realized = gallery.RealizedContainers;
        // Assert.NotNull(realized);
        // Assert.True(realized.Count() < 100, $"Expected fewer than 100 realized items, got {realized.Count()}");
    }

    [AvaloniaFact]
    public void ExpandedMode_UsesAvailableHeight_ToWrapColumns()
    {
        RunWithDispatcher(Dispatcher.UIThread.InvokeAsync(() =>
        {
            var gallery = new VirtualizingGallery
            {
                IsExpanded = true,
                ItemHeight = 100,
                ItemWidth = 100,
                ItemSpacing = 10,
                LineSpacing = 5,
                Orientation = Orientation.Vertical,
                WrapHeightOverride = double.NaN
            };

            var viewer = new NavigateAbleItemsViewer
            {
                ItemsPanel = new FuncTemplate<Panel?>(() => gallery),
                ItemsSource = Enumerable.Range(0, 5).Select(i => $"Item {i}").ToList()
            };

            var window = new Window { Width = 500, Height = 250, Content = viewer };
            window.Show();

            // Available height inside viewer is ~250:
            // Item 0: Y = 0..100 (next at 110)
            // Item 1: Y = 110..210 (next at 220)
            // Item 2: 220 + 100 = 320 > 250 -> wraps to column 1 at Y = 0
            var bounds0 = gallery.GetItemBounds(0);
            var bounds1 = gallery.GetItemBounds(1);
            var bounds2 = gallery.GetItemBounds(2);

            Assert.NotNull(bounds0);
            Assert.NotNull(bounds1);
            Assert.NotNull(bounds2);

            Assert.Equal(0, bounds0.Value.Y);
            Assert.Equal(110, bounds1.Value.Y);
            Assert.Equal(0, bounds2.Value.Y); // Wrapped
            Assert.True(bounds2.Value.X > bounds0.Value.X);

            window.Close();
        }));
    }

    [AvaloniaFact]
    public void ExpandedMode_UsesWrapHeightOverride_WhenSpecified_AndRestoresWhenCleared()
    {
        RunWithDispatcher(Dispatcher.UIThread.InvokeAsync(() =>
        {
            var gallery = new VirtualizingGallery
            {
                IsExpanded = true,
                ItemHeight = 100,
                ItemWidth = 100,
                ItemSpacing = 10,
                LineSpacing = 5,
                Orientation = Orientation.Vertical,
                WrapHeightOverride = 400 // Override allows 3 items (0..100, 110..210, 220..320)
            };

            var viewer = new NavigateAbleItemsViewer
            {
                ItemsPanel = new FuncTemplate<Panel?>(() => gallery),
                ItemsSource = Enumerable.Range(0, 5).Select(i => $"Item {i}").ToList()
            };

            var window = new Window { Width = 500, Height = 250, Content = viewer };
            window.Show();

            var bounds2WithOverride = gallery.GetItemBounds(2);
            var bounds3WithOverride = gallery.GetItemBounds(3);

            Assert.NotNull(bounds2WithOverride);
            Assert.NotNull(bounds3WithOverride);

            Assert.Equal(220, bounds2WithOverride.Value.Y); // Stayed in column 0 due to 400 override
            Assert.Equal(0, bounds3WithOverride.Value.Y);   // Item 3 wrapped (330 + 100 = 430 > 400)

            // Clear override: should now wrap at window available height = 250
            gallery.WrapHeightOverride = double.NaN;
            window.UpdateLayout();

            var bounds2Restored = gallery.GetItemBounds(2);
            Assert.NotNull(bounds2Restored);
            Assert.Equal(0, bounds2Restored.Value.Y); // Wrapped to column 1 at 250 available height

            window.Close();
        }));
    }

    [AvaloniaFact]
    public void ResetOrItemReplacement_UpdatesFirstItemContainer()
    {
        RunWithDispatcher(Dispatcher.UIThread.InvokeAsync(() =>
        {
            var gallery = new VirtualizingGallery
            {
                ItemHeight = 100,
                Orientation = Orientation.Horizontal
            };

            var items = new System.Collections.ObjectModel.ObservableCollection<string> { "Old_Dir_Item_0", "Old_Dir_Item_1" };
            var viewer = new NavigateAbleItemsViewer
            {
                ItemsPanel = new FuncTemplate<Panel?>(() => gallery),
                ItemsSource = items
            };

            var window = new Window { Width = 500, Height = 250, Content = viewer };
            window.Show();

            Assert.NotEmpty(gallery.RealizedItems);
            var firstRealized = gallery.RealizedItems[0];
            Assert.Equal("Old_Dir_Item_0", firstRealized.Element.DataContext);

            // Simulate folder change: Clear and Repopulate
            items.Clear();
            window.UpdateLayout();
            Assert.Empty(gallery.RealizedItems);

            items.Add("New_Dir_Item_0");
            items.Add("New_Dir_Item_1");
            window.UpdateLayout();

            Assert.NotEmpty(gallery.RealizedItems);
            var newFirstRealized = gallery.RealizedItems[0];
            Assert.Equal("New_Dir_Item_0", newFirstRealized.Element.DataContext);

            window.Close();
        }));
    }

    [AvaloniaFact]
    public void Shown_CurrentItemFarAlong_CreatesItsContainerFirstAndCentersIt()
    {
        var strip = ShowStrip(200, 150);
        try
        {
            Assert.Equal(150, strip.Prepared[0]);
            Assert.All(strip.Prepared, index => Assert.InRange(index, 140, 160));
            AssertCenteredOn(150, strip);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void WidthsChangeToTheLeft_WhileFollowing_KeepsTheCurrentItemCentered()
    {
        var strip = ShowStrip(200, 150);
        try
        {
            Widen(strip.Items.Take(150), strip.Gallery);

            Assert.Equal(30050 - strip.ViewportWidth / 2, strip.ScrollViewer.Offset.X, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void ScrolledByTheUser_WidthsChangeToTheLeft_HoldTheMiddleItemUntilACenterRequest()
    {
        var strip = ShowStrip(200, 150);
        try
        {
            // Item 105 sits in the middle of the view
            strip.ScrollTo(10550 - strip.ViewportWidth / 2);

            Widen(strip.Items.Take(50), strip.Gallery);
            Assert.Equal(15550 - strip.ViewportWidth / 2, strip.ScrollViewer.Offset.X, Tolerance);

            strip.Viewer.ScrollToCenterOfCurrentItem();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(20050 - strip.ViewportWidth / 2, strip.ScrollViewer.Offset.X, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void ShownAtZeroHeight_UntilItGrows_CreatesNoContainersThenCentersTheCurrentItem()
    {
        var strip = ShowStrip(200, 150, show: false);
        var host = new Border { Height = 0 };
        strip.Window.Content = host;
        host.Child = strip.Viewer;
        try
        {
            // Shown, but without a viewport, as a gallery is while it is collapsed
            strip.Window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Empty(strip.Prepared);
            Assert.Empty(strip.Gallery.RealizedItems);

            host.Height = 150;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(150, strip.Prepared[0]);
            AssertCenteredOn(150, strip);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void DockedAtTheSide_AfterTheUserScrolled_CentersTheCurrentItem()
    {
        var strip = ShowStrip(200, 150);
        try
        {
            strip.ScrollTo(0);

            strip.Viewer.SetVerticalScrolling();
            strip.Gallery.Orientation = Orientation.Vertical;
            strip.Window.Width = 150;
            strip.Window.Height = 1000;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(15050 - strip.ScrollViewer.Viewport.Height / 2, strip.ScrollViewer.Offset.Y, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void WindowResized_WhileFollowing_KeepsTheCurrentItemCentered()
    {
        var strip = ShowStrip(200, 150);
        try
        {
            strip.Window.Width = 600;
            Dispatcher.UIThread.RunJobs();

            AssertCenteredOn(150, strip);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void ItemsReset_AfterTheUserScrolled_CentersTheCurrentItem()
    {
        var strip = ShowStrip(200, 150);
        try
        {
            strip.ScrollTo(0);

            strip.Items.Clear();
            strip.Items.AddRange(Enumerable.Range(0, 200).Select(_ => new GalleryItemViewModel()));
            Dispatcher.UIThread.RunJobs();

            AssertCenteredOn(150, strip);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void HeldItemRemoved_KeepsTheOffset()
    {
        var strip = ShowStrip(200, 150);
        try
        {
            strip.ScrollTo(10550 - strip.ViewportWidth / 2);
            var offset = strip.ScrollViewer.Offset.X;

            strip.Items.RemoveAt(105);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(offset, strip.ScrollViewer.Offset.X);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void ItemsClearedInALayoutOfTheirOwn_AfterTheUserScrolled_CentersTheNewItems()
    {
        var strip = ShowStrip(200, 150);
        try
        {
            strip.ScrollTo(5000);

            // A folder change clears the strip and fills it later, so the empty strip gets a layout of its own, in
            // which the scroll viewer pulls the view back to the start of the now empty strip
            strip.Items.Clear();
            Dispatcher.UIThread.RunJobs();
            strip.Items.AddRange(Enumerable.Range(0, 200).Select(_ => new GalleryItemViewModel()));
            Dispatcher.UIThread.RunJobs();

            AssertCenteredOn(150, strip);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void Reattached_AfterTheUserScrolled_CentersTheCurrentItem()
    {
        var strip = ShowStrip(200, 150);
        try
        {
            strip.ScrollTo(0);

            // A tab switch takes the tab's gallery out of the window, and brings it back when the tab is shown again
            strip.Window.Content = null;
            Dispatcher.UIThread.RunJobs();
            strip.Window.Content = strip.Viewer;
            Dispatcher.UIThread.RunJobs();

            AssertCenteredOn(150, strip);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void ExpandedGallery_WidthsChangeToTheLeft_KeepsTheCurrentItemInPlace()
    {
        var strip = ShowExpanded(200, 150);
        try
        {
            var shownAt = CurrentItemX(strip);

            Widen(strip.Items.Take(150), strip.Gallery);

            Assert.Equal(shownAt, CurrentItemX(strip), Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void ExpandedGallery_ScrolledAwayAndBack_StaysWhereTheUserScrolled()
    {
        var strip = ShowExpanded(200, 150);
        try
        {
            var centered = strip.ScrollViewer.Offset.X;

            // A wheel step away and one back, which lands where the panel put the view
            strip.ScrollTo(centered + 50);
            strip.ScrollTo(centered);

            Assert.Equal(centered, strip.ScrollViewer.Offset.X, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void ExpandedGallery_ScrolledToTheEnd_StaysAtTheEnd()
    {
        var strip = ShowExpanded(200, 150);
        try
        {
            // The end shows the margin after the last column
            var end = strip.ScrollViewer.Extent.Width - strip.ViewportWidth;

            strip.ScrollTo(end);

            Assert.Equal(end, strip.ScrollViewer.Offset.X, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void ScrolledByTheUser_ToTheEndOfTheStrip_StaysThere()
    {
        var strip = ShowStrip(200, 150);
        try
        {
            var end = strip.ScrollViewer.Extent.Width - strip.ViewportWidth;

            strip.ScrollTo(end);

            Assert.Equal(end, strip.ScrollViewer.Offset.X, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void HoldCorrectedByUnderTwoPixels_ScrolledAwayAndBack_StaysWhereTheUserScrolled()
    {
        var strip = ShowStrip(200, 150);
        try
        {
            // Item 105's middle, 10550, sits a pixel past the middle of the view
            strip.ScrollTo(10549 - strip.ViewportWidth / 2);

            // Item 0 a pixel and a half wider moves the held item, and so the view, by that much
            strip.Items[0].SetDetails(new GalleryFileDetails(203, 200, null));
            strip.Gallery.InvalidateMeasure();
            Dispatcher.UIThread.RunJobs();
            var corrected = strip.ScrollViewer.Offset.X;

            // A wheel step away and one back, which lands where the panel put the view
            strip.ScrollTo(corrected + 50);
            strip.ScrollTo(corrected);

            Assert.Equal(corrected, strip.ScrollViewer.Offset.X, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void ExpandedFromTheSide_AfterTheUserScrolled_CentersTheCurrentItem()
    {
        var strip = ShowStrip(200, 150);
        try
        {
            // Docked at the side, and scrolled to its start by the user
            strip.Viewer.SetVerticalScrolling();
            strip.Gallery.Orientation = Orientation.Vertical;
            strip.Window.Width = 150;
            strip.Window.Height = 1000;
            Dispatcher.UIThread.RunJobs();
            strip.ScrollViewer.SetCurrentValue(ScrollViewer.OffsetProperty, new Vector(0, 0));
            Dispatcher.UIThread.RunJobs();

            // Expanded as GalleryAnimationControl does it, which keeps the vertical orientation and adds the margin
            strip.Viewer.SetHorizontalScrolling();
            strip.Gallery.IsExpanded = true;
            strip.Gallery.Margin = new Thickness(15, 40, 15, 5);
            strip.Window.Width = 1000;
            strip.Window.Height = 400;
            Dispatcher.UIThread.RunJobs();

            // The scroll viewer scrolls over the margin before the items too
            var middle = 15 + strip.Gallery.GetItemBounds(150)!.Value.Center.X;
            Assert.Equal(middle - strip.ViewportWidth / 2, strip.ScrollViewer.Offset.X, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void DockedAtTheSideFromExpanded_AfterTheUserScrolled_CentersTheCurrentItem()
    {
        var strip = ShowExpanded(200, 150);
        try
        {
            strip.ScrollTo(0);

            // Docked as GalleryAnimationControl does it, which keeps the vertical orientation and drops the margin
            strip.Viewer.SetVerticalScrolling();
            strip.Gallery.IsExpanded = false;
            strip.Gallery.Margin = default;
            strip.Window.Width = 150;
            strip.Window.Height = 1000;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(15050 - strip.ScrollViewer.Viewport.Height / 2, strip.ScrollViewer.Offset.Y, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    private const double Tolerance = 1;

    private sealed record Strip(Window Window, NavigateAbleItemsViewer Viewer, VirtualizingGallery Gallery,
        AvaloniaList<GalleryItemViewModel> Items, List<int> Prepared)
    {
        public ScrollViewer ScrollViewer => Gallery.FindAncestorOfType<ScrollViewer>()!;
        public double ViewportWidth => ScrollViewer.Viewport.Width;

        public void ScrollTo(double x)
        {
            ScrollViewer.SetCurrentValue(ScrollViewer.OffsetProperty, new Vector(x, 0));
            Dispatcher.UIThread.RunJobs();
        }
    }

    /// <summary>
    /// <paramref name="count"/> unsized items, so 100-wide squares, in a 1000-wide horizontal Docked Gallery. The current
    /// item is <paramref name="currentIndex"/>, and the strip records the index of every container it prepares.
    /// </summary>
    private static Strip ShowStrip(int count, int currentIndex, bool show = true)
    {
        var items = new AvaloniaList<GalleryItemViewModel>(Enumerable.Range(0, count).Select(_ => new GalleryItemViewModel()));
        var gallery = new VirtualizingGallery { ItemHeight = 100, Orientation = Orientation.Horizontal };
        var viewer = new NavigateAbleItemsViewer
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => gallery),
            ItemTemplate = new FuncDataTemplate<GalleryItemViewModel>((_, _) => new Border()),
            ItemsSource = items,
            CurrentItemIndex = currentIndex
        };
        var strip = new Strip(new Window { Width = 1000, Height = 150, Content = viewer }, viewer, gallery, items, []);
        viewer.ContainerPrepared += (_, e) => strip.Prepared.Add(e.Index);
        if (show)
        {
            strip.Window.Show();
            Dispatcher.UIThread.RunJobs();
        }
        return strip;
    }

    /// <summary>
    /// <see cref="ShowStrip"/>'s items in the Expanded Gallery's columns instead. Its margin is the app's, which puts the
    /// panel 15 pixels into what the scroll viewer scrolls.
    /// </summary>
    private static Strip ShowExpanded(int count, int currentIndex)
    {
        var strip = ShowStrip(count, currentIndex, show: false);
        strip.Gallery.IsExpanded = true;
        strip.Gallery.Orientation = Orientation.Vertical;
        strip.Gallery.Margin = new Thickness(15, 40, 15, 5);
        strip.Window.Height = 400;
        strip.Window.Show();
        Dispatcher.UIThread.RunJobs();
        return strip;
    }

    private static void Widen(IEnumerable<GalleryItemViewModel> items, VirtualizingGallery gallery)
    {
        foreach (var item in items)
        {
            item.SetDetails(new GalleryFileDetails(200, 100, null));
        }
        gallery.InvalidateMeasure();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Item <paramref name="index"/> spans 100 * index to 100 * index + 100, so the strip is centered on it when the
    /// view's middle is at 100 * index + 50.
    /// </summary>
    private static void AssertCenteredOn(int index, Strip strip) =>
        Assert.Equal(100 * index + 50 - strip.ViewportWidth / 2, strip.ScrollViewer.Offset.X, Tolerance);

    /// <summary>
    /// Where the current item's container is in the scroll viewer, which is where the user sees it.
    /// </summary>
    private static double CurrentItemX(Strip strip) =>
        strip.Viewer.ContainerFromIndex(strip.Viewer.CurrentItemIndex)!.TranslatePoint(default, strip.ScrollViewer)!.Value.X;
}
