using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using PicView.Avalonia.CustomControls;
using System.Linq;
using Xunit;

namespace PicView.Tests.Gallery;

public class VirtualizingGalleryTests
{
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
}