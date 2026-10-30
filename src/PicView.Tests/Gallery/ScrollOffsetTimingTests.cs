using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace PicView.Tests.Gallery;

/// <summary>
/// Pins the Avalonia behavior the gallery panel's centering relies on: an offset set when a layout pass completes is
/// laid out before any other queued work, so the frame committed after that layout already shows it.
/// </summary>
[Collection("Sequential")]
public class ScrollOffsetTimingTests
{
    [AvaloniaFact]
    public void OffsetSetWhenALayoutPassCompletes_IsLaidOutBeforeOtherQueuedWork()
    {
        var content = new Border { Width = 4000, Height = 50 };
        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = content
        };
        var window = new Window { Width = 200, Height = 50, Content = scrollViewer };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var offsetSet = false;
        var passesAfterOffset = 0;
        bool? laidOutFirst = null;
        scrollViewer.LayoutUpdated += (_, _) =>
        {
            if (offsetSet)
            {
                passesAfterOffset++;
                return;
            }
            offsetSet = true;
            scrollViewer.SetCurrentValue(ScrollViewer.OffsetProperty, new Vector(500, 0));
            Dispatcher.UIThread.Post(() => laidOutFirst = passesAfterOffset > 0, DispatcherPriority.Send);
        };
        content.Width = 5000;
        Dispatcher.UIThread.RunJobs();

        Assert.True(laidOutFirst);
        Assert.Equal(new Vector(500, 0), scrollViewer.Offset);
        window.Close();
    }
}
