using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Views.UC;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class BottomBarTests
{
    public BottomBarTests()
    {
        SetDefaults();
    }

    [AvaloniaFact]
    public void IsVisible_TurnedOnInANarrowWindow_SizesItsButtons()
    {
        // The window's own resize hook never runs here, so only the bar's observer can size it
        var bar = new BottomBar { IsVisible = false };
        var window = new Window { Width = 400, Height = 300, Content = bar };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(80, bar.PreviousButton.Width);

        bar.IsVisible = true;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(60, bar.PreviousButton.Width);
        Assert.False(bar.ResetZoomButton.IsVisible);
        window.Close();
    }
}
