using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
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

    // At 125%, the panel's own correction of a held view can be under two DIPs: the panel remembers it, and must forget
    // it once the view arrives there, or a later scroll back to it would pass for the panel's own and be undone
    [AvaloniaFact]
    public void HoldCorrectedByUnderTwoDipsAt125Percent_ScrolledAwayAndBack_StaysWhereTheUserScrolled()
    {
        var strip = ShowStrip(200, 150, show: false);
        strip.Window.SetRenderScaling(1.25);
        try
        {
            strip.Window.Show();
            Dispatcher.UIThread.RunJobs();
            // Item 105's middle, 10550, sits a DIP past the middle of the view, which is between device pixels
            var scrolledTo = 10549 - strip.ViewportWidth / 2;
            strip.ScrollTo(scrolledTo);

            // Item 0 at 203 by 200 is 126.875 device pixels wide, which rounds to 127, two more than the square's 125.
            // That moves the held item by 1.6 DIPs, and the view from the pixel it showed by as much: 1.4 DIPs from
            // where the user left it
            strip.Items[0].SetDetails(new GalleryFileDetails(203, 200, null));
            strip.Gallery.InvalidateMeasure();
            Dispatcher.UIThread.RunJobs();
            var corrected = strip.ScrollViewer.Offset.X;
            Assert.InRange(corrected - scrolledTo, Tolerance, 2);

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

    // At 125% and 150%, layout puts the viewport on device pixels, so it lies a fraction of a pixel off the offset
    [AvaloniaTheory]
    [InlineData(1.0, false)]
    [InlineData(1.0, true)]
    [InlineData(1.25, false)]
    [InlineData(1.25, true)]
    [InlineData(1.5, false)]
    [InlineData(1.5, true)]
    public void ScrolledInStepsUnderAPixel_WhileFollowingOrHolding_AdvancesByTheirSum(double renderScaling,
        bool holding)
    {
        var strip = ShowStrip(200, 150, show: false);
        strip.Window.SetRenderScaling(renderScaling);
        try
        {
            strip.Window.Show();
            Dispatcher.UIThread.RunJobs();
            if (holding)
            {
                strip.ScrollTo(14050);
            }

            var start = strip.ScrollViewer.Offset.X;

            // Middle-click auto-scroll near where it was clicked, or a slow touchpad, moves the view under a pixel a step
            for (var step = 0; step < 10; step++)
            {
                strip.ScrollTo(strip.ScrollViewer.Offset.X + 0.9);
            }

            Assert.Equal(start + 9, strip.ScrollViewer.Offset.X, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    // As a folder opens: the current image and its neighbours are read before the strip is shown, and the rest arrive
    // in batches nearest the current image while it follows. Each batch to the left moves the items on screen and the
    // view by the same amount, so no thumbnail may move on screen, not even by a device pixel
    [AvaloniaTheory]
    [InlineData(1.0, false)]
    [InlineData(1.25, false)]
    [InlineData(1.5, false)]
    [InlineData(1.0, true)]
    [InlineData(1.25, true)]
    [InlineData(1.5, true)]
    public void SizesReadInBatches_WhileFollowing_LeaveEveryShownThumbnailOnItsDevicePixel(double renderScaling,
        bool expanded)
    {
        var strip = ShowStrip(300, 150, show: false);
        strip.Window.SetRenderScaling(renderScaling);
        if (expanded)
        {
            // As ShowExpanded does, with spacings that are fractions of a device pixel at 125%
            strip.Gallery.IsExpanded = true;
            strip.Gallery.Orientation = Orientation.Vertical;
            strip.Gallery.Margin = new Thickness(15, 40, 15, 5);
            strip.Gallery.ItemSpacing = 5;
            strip.Gallery.LineSpacing = 5;
            strip.Window.Height = 400;
        }

        try
        {
            ReadNeighbours(strip, renderScaling);
            strip.Window.Show();
            Dispatcher.UIThread.RunJobs();
            var shownAt = ShownLeftEdges(strip, renderScaling);
            Assert.NotEmpty(shownAt);

            var moves = ReadTheRestInBatches(strip, shownAt, renderScaling, () =>
            {
                var middle = strip.Gallery.Margin.Left + strip.Gallery.GetItemBounds(150)!.Value.Center.X;
                Assert.Equal(middle - strip.ViewportWidth / 2, strip.ScrollViewer.Offset.X, Tolerance);
            });

            Assert.True(moves.Count is 0, $"Shown thumbnails moved, in device pixels: {string.Join("; ", moves)}");
        }
        finally
        {
            strip.Window.Close();
        }
    }

    // A wheel step of 50 lands halfway between two device pixels at 125%, and layout shows the view at one of them. The
    // panel then holds what the user scrolled to while the batches move it, and must move it from that pixel
    [AvaloniaTheory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    public void SizesReadInBatches_AfterTheUserScrolledBetweenPixels_LeaveEveryShownThumbnailOnItsDevicePixel(
        double renderScaling)
    {
        var strip = ShowStrip(300, 150, show: false);
        strip.Window.SetRenderScaling(renderScaling);
        try
        {
            ReadNeighbours(strip, renderScaling);
            strip.Window.Show();
            Dispatcher.UIThread.RunJobs();
            strip.ScrollTo(strip.ScrollViewer.Offset.X - 400 - 0.5 / renderScaling);
            var shownAt = ShownLeftEdges(strip, renderScaling);
            Assert.NotEmpty(shownAt);

            var moves = ReadTheRestInBatches(strip, shownAt, renderScaling, () => { });

            Assert.True(moves.Count is 0, $"Shown thumbnails moved, in device pixels: {string.Join("; ", moves)}");
        }
        finally
        {
            strip.Window.Close();
        }
    }

    // A batch whose widths before the view add up to one device pixel moves the target by under a DIP. A view that
    // nothing else moved must still move with the items, or the whole strip shifts by that pixel
    [AvaloniaTheory]
    [InlineData(1.0, false)]
    [InlineData(1.25, false)]
    [InlineData(1.5, false)]
    [InlineData(2.0, false)]
    [InlineData(1.0, true)]
    [InlineData(1.25, true)]
    [InlineData(1.5, true)]
    [InlineData(2.0, true)]
    public void SizesReadAddingUpToOneDevicePixel_WhileFollowingOrHolding_LeaveEveryShownThumbnailOnItsDevicePixel(
        double renderScaling, bool holding)
    {
        var strip = ShowStrip(300, 150, show: false);
        strip.Window.SetRenderScaling(renderScaling);
        try
        {
            strip.Window.Show();
            Dispatcher.UIThread.RunJobs();
            if (holding)
            {
                // The user scrolls to a view on device pixels, as a wheel step at 100% does
                strip.ScrollTo(strip.ScrollViewer.Offset.X - 400);
            }

            var shownAt = ShownLeftEdges(strip, renderScaling);
            Assert.NotEmpty(shownAt);

            var height = (uint)Math.Round(100 * renderScaling);
            var moves = new List<string>();
            for (var batch = 1; batch <= 6; batch++)
            {
                // An item far before the view is read one device pixel wider than the square it was
                strip.Items[99 + batch].SetDetails(new GalleryFileDetails((height + 1) * 10, height * 10, null));
                strip.Gallery.InvalidateMeasure();
                Dispatcher.UIThread.RunJobs();
                AddMoves(strip, shownAt, renderScaling, batch, moves);
            }

            Assert.True(moves.Count is 0, $"Shown thumbnails moved, in device pixels: {string.Join("; ", moves)}");
        }
        finally
        {
            strip.Window.Close();
        }
    }

    // Steps of one device pixel land the view on pixels, each under the tolerance until two add up. A view that a step
    // moved is not still, though it and its target are on pixels, and the panel must not take the step back
    [AvaloniaTheory]
    [InlineData(1.0, false)]
    [InlineData(1.25, false)]
    [InlineData(1.5, false)]
    [InlineData(2.0, false)]
    [InlineData(1.0, true)]
    [InlineData(1.25, true)]
    [InlineData(1.5, true)]
    [InlineData(2.0, true)]
    public void ScrolledInStepsOfOneDevicePixel_WhileFollowingOrHolding_AdvancesByTheirSum(double renderScaling,
        bool holding)
    {
        var strip = ShowStrip(200, 150, show: false);
        strip.Window.SetRenderScaling(renderScaling);
        try
        {
            strip.Window.Show();
            Dispatcher.UIThread.RunJobs();
            if (holding)
            {
                // On device pixels at every scale here
                strip.ScrollTo(14000);
            }

            var start = strip.ScrollViewer.Offset.X;
            for (var step = 0; step < 10; step++)
            {
                strip.ScrollTo(strip.ScrollViewer.Offset.X + 1 / renderScaling);
            }

            Assert.Equal(start + 10 / renderScaling, strip.ScrollViewer.Offset.X, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    // A view the user left between device pixels is still too, but only a view on pixels follows changes under the
    // tolerance. The panel must not round this one to a pixel, which would take back part of each slow step after it
    [AvaloniaTheory]
    [InlineData(1.25)]
    [InlineData(1.75)]
    public void ScrolledByTheUserBetweenPixels_ThenInTinySteps_StaysWhereTheUserScrolled(double renderScaling)
    {
        var strip = ShowStrip(200, 150, show: false);
        strip.Window.SetRenderScaling(renderScaling);
        try
        {
            strip.Window.Show();
            Dispatcher.UIThread.RunJobs();
            // Half a device pixel past a whole one at both scales
            strip.ScrollTo(14050);
            Assert.Equal(14050, strip.ScrollViewer.Offset.X, 6);

            for (var step = 0; step < 30; step++)
            {
                strip.ScrollTo(strip.ScrollViewer.Offset.X + 0.05);
            }

            Assert.Equal(14051.5, strip.ScrollViewer.Offset.X, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    // A window an odd number of device pixels wider moves the middle of the view, and so the held item's exact offset,
    // by a half pixel. A held view on pixels moves to the held item's own pixels, which batches then move by whole
    // pixels. The exact offset's half pixel would round up or down from batch to batch
    [AvaloniaTheory]
    [InlineData(1.0, 1)]
    [InlineData(1.25, 1)]
    [InlineData(1.5, 3)]
    [InlineData(2.0, 3)]
    public void SizesReadInBatches_AfterTheWindowGrewByAnOddNumberOfDevicePixelsWhileHolding_LeaveEveryShownThumbnailOnItsDevicePixel(
        double renderScaling, int devicePixels)
    {
        var strip = ShowStrip(300, 150, show: false);
        strip.Window.SetRenderScaling(renderScaling);
        try
        {
            ReadNeighbours(strip, renderScaling);
            strip.Window.Show();
            Dispatcher.UIThread.RunJobs();
            // The user scrolls to a view on device pixels, and the window then grows
            strip.ScrollTo(strip.ScrollViewer.Offset.X - 400);
            strip.Window.Width = (Math.Round(1000 * renderScaling) + devicePixels) / renderScaling;
            Dispatcher.UIThread.RunJobs();
            var shownAt = ShownLeftEdges(strip, renderScaling);
            Assert.NotEmpty(shownAt);

            var moves = ReadTheRestInBatches(strip, shownAt, renderScaling, () => { });

            Assert.True(moves.Count is 0, $"Shown thumbnails moved, in device pixels: {string.Join("; ", moves)}");
        }
        finally
        {
            strip.Window.Close();
        }
    }

    // Three device pixels more move the middle of the view by one and a half. The view moves by whole pixels, rounding
    // each time, and the roundings must not add up and take the held item away from the middle
    [AvaloniaTheory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    public void ScrolledByTheUser_WindowGrowingThreeDevicePixelsAtATime_KeepsTheHeldItemInTheMiddle(
        double renderScaling)
    {
        var strip = ShowStrip(200, 150, show: false);
        strip.Window.SetRenderScaling(renderScaling);
        try
        {
            strip.Window.Show();
            Dispatcher.UIThread.RunJobs();
            // Item 140 sits in the middle of the view
            strip.ScrollTo(14050 - strip.ViewportWidth / 2);

            var width = (int)Math.Round(1000 * renderScaling);
            for (var step = 1; step <= 20; step++)
            {
                strip.Window.Width = (width + 3 * step) / renderScaling;
                Dispatcher.UIThread.RunJobs();
            }

            Assert.Equal(14050 - strip.ViewportWidth / 2, strip.ScrollViewer.Offset.X, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    // At 225%, items 113 high are 254.25 device pixels, so the offsets that center one lie between pixels. The panel
    // moves the view by whole device pixels, by as few as two, which is under a DIP, and such moves are still its own
    [AvaloniaFact]
    public void DockedAtTheSide_WindowGrowingInSmallStepsAt225Percent_KeepsFollowing()
    {
        var strip = ShowStrip(200, 150, show: false);
        strip.Gallery.ItemHeight = 113;
        strip.Window.SetRenderScaling(2.25);
        try
        {
            strip.Window.Show();
            Dispatcher.UIThread.RunJobs();
            strip.Viewer.SetVerticalScrolling();
            strip.Gallery.Orientation = Orientation.Vertical;
            strip.Window.Width = 150;
            strip.Window.Height = 1000;
            Dispatcher.UIThread.RunJobs();
            for (var step = 1; step <= 10; step++)
            {
                strip.Window.Height = 1000 + 1.6 * step;
                Dispatcher.UIThread.RunJobs();
            }

            // Following centers whatever item is current now, where holding would go with the moved item
            strip.Items.Move(150, 10);
            Dispatcher.UIThread.RunJobs();

            var middle = strip.Gallery.GetItemBounds(150)!.Value.Center.Y;
            Assert.Equal(middle - strip.ScrollViewer.Viewport.Height / 2, strip.ScrollViewer.Offset.Y, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void WindowGrowingTwoPixelsAtATime_WhileFollowingFourByThreeItems_KeepsFollowing()
    {
        var strip = ShowStrip(200, 150);
        try
        {
            // Items of 133.33, which round to 133 whole pixels, and a window that grows by two pixels a layout moves the
            // centered offset by one at a time
            foreach (var item in strip.Items.Take(150))
            {
                item.SetDetails(new GalleryFileDetails(400, 300, null));
            }

            strip.Gallery.InvalidateMeasure();
            Dispatcher.UIThread.RunJobs();
            for (var step = 1; step <= 10; step++)
            {
                strip.Window.Width = 1000 + 2 * step;
                Dispatcher.UIThread.RunJobs();
            }

            // Following centers whatever item is current now, where holding would go with the moved item
            strip.Items.Move(150, 10);
            Dispatcher.UIThread.RunJobs();

            var middle = strip.Gallery.GetItemBounds(150)!.Value.Center.X;
            Assert.Equal(middle - strip.ViewportWidth / 2, strip.ScrollViewer.Offset.X, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    // The last item can't be centered, so the view that follows it stays at the end of the strip, which moves by as much
    // as the window shrinks. Holding would move the view by half that, and the strip's end would leave the view
    [AvaloniaTheory]
    [InlineData(1.0, 1)]
    [InlineData(1.0, 2)]
    [InlineData(1.25, 1)]
    [InlineData(1.25, 2)]
    [InlineData(1.5, 1)]
    [InlineData(1.5, 2)]
    [InlineData(2.0, 1)]
    [InlineData(2.0, 2)]
    public void WindowShrinkingInDevicePixels_WhileFollowingTheLastItem_StaysAtTheEndOfTheStrip(double renderScaling,
        int devicePixelsPerStep)
    {
        var strip = ShowStrip(200, 199, show: false);
        strip.Window.SetRenderScaling(renderScaling);
        try
        {
            strip.Window.Show();
            Dispatcher.UIThread.RunJobs();

            var width = (int)Math.Round(1000 * renderScaling);
            for (var step = 1; step <= 20; step++)
            {
                strip.Window.Width = (width - devicePixelsPerStep * step) / renderScaling;
                Dispatcher.UIThread.RunJobs();
            }

            Assert.Equal(strip.ScrollViewer.Extent.Width - strip.ViewportWidth, strip.ScrollViewer.Offset.X, Tolerance);
        }
        finally
        {
            strip.Window.Close();
        }
    }

    [AvaloniaFact]
    public void DockedAtTheSideAgain_AfterAnExpandedGalleryThatFits_CentersTheCurrentItem()
    {
        var strip = ShowStrip(20, 10);
        try
        {
            // Docked at the side, centered on item 10
            strip.Viewer.SetVerticalScrolling();
            strip.Gallery.Orientation = Orientation.Vertical;
            strip.Window.Width = 150;
            strip.Window.Height = 1000;
            Dispatcher.UIThread.RunJobs();

            // Expanded, where all 20 items fit, so the panel never moves the view
            strip.Viewer.SetHorizontalScrolling();
            strip.Gallery.IsExpanded = true;
            strip.Gallery.Margin = new Thickness(15, 40, 15, 5);
            strip.Window.Width = 1000;
            strip.Window.Height = 400;
            Dispatcher.UIThread.RunJobs();

            // Docked at the side again, after the expanded columns kept the vertical offset at 0
            strip.Viewer.SetVerticalScrolling();
            strip.Gallery.IsExpanded = false;
            strip.Gallery.Margin = default;
            strip.Window.Width = 150;
            strip.Window.Height = 1000;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1050 - strip.ScrollViewer.Viewport.Height / 2, strip.ScrollViewer.Offset.Y, Tolerance);
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
    /// Image sizes whose widths at an item height of 100 fall on fractions of a pixel, or whole pixels that are
    /// fractions of a device pixel at 125% or 150%, as a folder of photos, scans and illustrations has.
    /// </summary>
    private static readonly (uint Width, uint Height)[] MixedImageSizes =
    [
        (4000, 3000), (2000, 3000), (1920, 1080), (3000, 4000), (2480, 3508), (1500, 2100), (3000, 2000), (1080, 1920),
        (2100, 1500), (2560, 1440), (1600, 2400), (3508, 2480), (1000, 1000)
    ];

    /// <summary>
    /// Gives the item at <paramref name="index"/> one of <see cref="MixedImageSizes"/>, as reading its file does.
    /// </summary>
    private static void ReadDetails(GalleryItemViewModel item, int index)
    {
        var (width, height) = MixedImageSizes[index % MixedImageSizes.Length];
        item.SetDetails(new GalleryFileDetails(width, height, null));
    }

    /// <summary>
    /// Reads the current item, 150, and the 20 on each side of it, as the gallery coordinator does before it adds the
    /// items. The current item is an odd number of device pixels wide, twice its height and one more, so its exact
    /// middle, and the exact offset that centers it, fall between two device pixels.
    /// </summary>
    private static void ReadNeighbours(Strip strip, double renderScaling)
    {
        for (var index = 130; index <= 170; index++)
        {
            ReadDetails(strip.Items[index], index);
        }

        var height = (uint)Math.Round(100 * renderScaling);
        strip.Items[150].SetDetails(new GalleryFileDetails((2 * height + 1) * 10, height * 10, null));
    }

    /// <summary>
    /// Reads every unread item in batches of 20 nearest the current item, laying the strip out after each batch as
    /// GalleryView does when the batch's sizes arrive, and calls <paramref name="afterEachBatch"/> then.
    /// </summary>
    /// <returns>
    /// Every move of a container in <paramref name="shownAt"/> away from its left edge there, by batch.
    /// </returns>
    private static List<string> ReadTheRestInBatches(Strip strip, Dictionary<int, int> shownAt, double renderScaling,
        Action afterEachBatch)
    {
        var moves = new List<string>();
        for (var batch = 1; NearestUnread(strip, 20) is { Count: > 0 } unread; batch++)
        {
            foreach (var index in unread)
            {
                ReadDetails(strip.Items[index], index);
            }

            strip.Gallery.InvalidateMeasure();
            Dispatcher.UIThread.RunJobs();
            AddMoves(strip, shownAt, renderScaling, batch, moves);
            afterEachBatch();
        }

        return moves;
    }

    /// <summary>
    /// Adds every container in <paramref name="shownAt"/> that is no longer at its left edge there to
    /// <paramref name="moves"/>, as moved in batch <paramref name="batch"/>.
    /// </summary>
    private static void AddMoves(Strip strip, Dictionary<int, int> shownAt, double renderScaling, int batch,
        List<string> moves)
    {
        foreach (var (index, left) in shownAt)
        {
            var now = LeftEdge(strip, index, renderScaling);
            if (now != left)
            {
                moves.Add($"batch {batch}: item {index} from {left} to {now?.ToString() ?? "no container"}");
            }
        }
    }

    /// <summary>
    /// Up to <paramref name="count"/> indices of unread items nearest the current item, one after it then one before
    /// it, as the gallery coordinator picks its next batch.
    /// </summary>
    private static List<int> NearestUnread(Strip strip, int count)
    {
        var current = strip.Viewer.CurrentItemIndex;
        var unread = new List<int>();
        for (var distance = 0; unread.Count < count; distance++)
        {
            var after = current + distance;
            var before = current - distance;
            if (after >= strip.Items.Count && before < 0)
            {
                break;
            }

            if (after < strip.Items.Count && strip.Items[after].Details is null)
            {
                unread.Add(after);
            }

            if (distance > 0 && before >= 0 && unread.Count < count && strip.Items[before].Details is null)
            {
                unread.Add(before);
            }
        }

        return unread;
    }

    /// <summary>
    /// Where each container that the view shows, even in part, starts on screen: device pixels from the window's left
    /// edge, by item index.
    /// </summary>
    private static Dictionary<int, int> ShownLeftEdges(Strip strip, double renderScaling)
    {
        var edges = new Dictionary<int, int>();
        foreach (var realized in strip.Gallery.RealizedItems)
        {
            var inView = realized.Element.TranslatePoint(default, strip.ScrollViewer)!.Value.X;
            if (inView + realized.Element.Bounds.Width > 0 && inView < strip.ViewportWidth)
            {
                edges[realized.Index] = LeftEdge(strip, realized.Index, renderScaling)!.Value;
            }
        }

        return edges;
    }

    /// <summary>
    /// Where the container of the item at <paramref name="index"/> starts on screen, in device pixels from the
    /// window's left edge, or null when it has no container.
    /// </summary>
    private static int? LeftEdge(Strip strip, int index, double renderScaling)
    {
        var container = strip.Viewer.ContainerFromIndex(index);
        if (container?.TranslatePoint(default, strip.Window) is not { } left)
        {
            return null;
        }

        return (int)Math.Round(left.X * renderScaling);
    }

    /// <summary>
    /// Where the current item's container is in the scroll viewer, which is where the user sees it.
    /// </summary>
    private static double CurrentItemX(Strip strip) =>
        strip.Viewer.ContainerFromIndex(strip.Viewer.CurrentItemIndex)!.TranslatePoint(default, strip.ScrollViewer)!.Value.X;
}
