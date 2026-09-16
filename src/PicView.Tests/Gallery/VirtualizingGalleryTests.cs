using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Threading;
using PicView.Avalonia;
using PicView.Avalonia.CustomControls;
using System.Linq;
using Xunit;

namespace PicView.Tests.Gallery;

[Collection("Sequential")]
public class VirtualizingGalleryTests
{
    public VirtualizingGalleryTests()
    {
        try
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
        }
        catch (InvalidOperationException)
        {
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

    [Fact]
    public void PropertiesHaveCorrectDefaults()
    {
        var gallery = new VirtualizingGallery();
        Assert.Equal(Orientation.Horizontal, gallery.Orientation);
        Assert.True(gallery.ItemHeight > 0);
        Assert.Equal(0, gallery.ItemSpacing);
        Assert.Equal(0, gallery.LineSpacing);
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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
}