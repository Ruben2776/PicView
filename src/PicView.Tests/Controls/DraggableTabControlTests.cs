using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class DraggableTabControlTests
{
    public DraggableTabControlTests()
    {
        SetDefaults();
    }

    [AvaloniaTheory]
    // A touchpad swipe toward the left end gives a positive X delta, toward the right end a negative one
    [InlineData(0.1, true)]
    [InlineData(-0.1, false)]
    public void PointerWheel_TouchpadSwipe_ScrollsTheSwipedWay(double deltaX, bool expectLeft)
        => WithScrolledStrip((window, viewer, point) =>
        {
            var before = viewer.Offset.X;

            window.MouseWheel(point, new Vector(deltaX, 0));
            Dispatcher.UIThread.RunJobs();

            if (expectLeft)
            {
                Assert.True(viewer.Offset.X < before, $"{viewer.Offset.X} should be left of {before}");
            }
            else
            {
                Assert.True(viewer.Offset.X > before, $"{viewer.Offset.X} should be right of {before}");
            }
        });

    [AvaloniaTheory]
    // A mouse wheel turned up scrolls the strip left, turned down right
    [InlineData(1.0, true)]
    [InlineData(-1.0, false)]
    public void PointerWheel_MouseWheel_ScrollsTheStripSideways(double deltaY, bool expectLeft)
        => WithScrolledStrip((window, viewer, point) =>
        {
            var before = viewer.Offset.X;

            window.MouseWheel(point, new Vector(0, deltaY));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(expectLeft, viewer.Offset.X < before);
            Assert.NotEqual(before, viewer.Offset.X);
        });

    // Hosts a strip of more tabs than fit, scrolled to its middle, and passes a point over one of its tabs
    private static void WithScrolledStrip(Action<Window, AutoScrollViewer, Point> test)
    {
        var tabs = new DraggableTabControl
        {
            ItemsSource = Enumerable.Range(0, 20).Select(i => $"Tab {i}").ToArray()
        };
        var window = new Window { Width = 600, Height = 300, Content = tabs };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            var viewer = tabs.GetVisualDescendants().OfType<AutoScrollViewer>().First();
            var max = viewer.Extent.Width - viewer.Viewport.Width;
            Assert.True(max > 200, $"The strip should scroll, extent {viewer.Extent.Width}");
            viewer.Offset = new Vector(max / 2, 0);
            Dispatcher.UIThread.RunJobs();

            var tab = tabs.GetVisualDescendants().OfType<TabItem>()
                .First(t => t.TranslatePoint(new Point(t.Bounds.Width / 2, 0), viewer) is { X: > 0 } p
                            && p.X < viewer.Viewport.Width);
            var point = tab.TranslatePoint(new Point(tab.Bounds.Width / 2, tab.Bounds.Height / 2), window)!.Value;

            test(window, viewer, point);
        }
        finally
        {
            window.Close();
        }
    }
}
