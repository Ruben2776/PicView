using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls.Zoom;

namespace PicView.Tests.Controls;

/// <summary>
/// While zoom resizes the window (ADR-0047) the Fit gives the zoom control a size of its own, the area the zoomed
/// image is shown in: the child keeps its fitted size, the image is placed again each time that area changes, and a
/// zoom step that resizes the window is not animated.
/// </summary>
[Collection("Sequential")]
public class ZoomPanControlViewportTests
{
    public ZoomPanControlViewportTests() => SetDefaults();

    private static (ZoomPanControl Control, Border Child, Window Window) Show(double width, double height)
    {
        var child = new Border { Width = 400, Height = 300 };
        var control = new ZoomPanControl { Child = child, Width = width, Height = height };
        var window = new Window { Width = 1200, Height = 900, Content = control };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (control, child, window);
    }

    [AvaloniaFact]
    public void ViewportGrowsWithTheZoom_ImageFillsItFromItsOrigin()
    {
        var (control, child, window) = Show(400, 300);
        // As a wheel step does, around the cursor, before the Fit resizes the control
        control.SetScaleImmediate(2, new Point(350, 250));

        control.Width = 800;
        control.Height = 600;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Size(800, 600), control.Bounds.Size);
        Assert.Equal(new Rect(0, 0, 400, 300), child.Bounds);
        Assert.Equal(0, control.TranslateX, 6);
        Assert.Equal(0, control.TranslateY, 6);
        window.Close();
    }

    [AvaloniaFact]
    public void ViewportShrinksWithTheZoom_ChildKeepsItsFittedSize()
    {
        var (control, child, window) = Show(400, 300);
        control.SetScaleImmediate(0.5);

        control.Width = 200;
        control.Height = 150;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Size(200, 150), control.Bounds.Size);
        Assert.Equal(new Rect(0, 0, 400, 300), child.Bounds);
        Assert.Equal(0, control.TranslateX, 6);
        Assert.Equal(0, control.TranslateY, 6);
        window.Close();
    }

    [AvaloniaFact]
    public void ViewportStoppedGrowing_ImageLargerThanIt_KeepsItsPanInsideIt()
    {
        var (control, _, window) = Show(400, 300);
        control.SetScaleImmediate(4, new Point(400, 300));

        // The window stopped at 1000 by 600, smaller than the 1600 by 1200 image
        control.Width = 1000;
        control.Height = 600;
        Dispatcher.UIThread.RunJobs();

        Assert.InRange(control.TranslateX, 1000 - 1600, 0);
        Assert.InRange(control.TranslateY, 600 - 1200, 0);
        window.Close();
    }

    [AvaloniaFact]
    public void NoViewportSize_ChildFillsTheControlAsBefore()
    {
        var child = new Border();
        var control = new ZoomPanControl { Child = child };
        var window = new Window { Width = 800, Height = 600, Content = control };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(control.Bounds.Size, child.Bounds.Size);
        window.Close();
    }

    [Theory]
    // The window does not follow the zoom
    [InlineData(1, 1.3, 0, false)]
    // Still growing, or shrinking back below where it stopped
    [InlineData(1, 1.3, 2, true)]
    [InlineData(1.9, 2.2, 2, true)]
    [InlineData(2.2, 1.9, 2, true)]
    [InlineData(1, 0.7, 2, true)]
    // Zooming inside a window that has stopped growing
    [InlineData(2, 2.3, 2, false)]
    [InlineData(3, 2.5, 2, false)]
    public void ResizesWindow_OnlyBelowTheGrowthLimit(double from, double to, double growthLimit, bool expected) =>
        Assert.Equal(expected, ZoomPanControl.ResizesWindow(from, to, growthLimit));
}
