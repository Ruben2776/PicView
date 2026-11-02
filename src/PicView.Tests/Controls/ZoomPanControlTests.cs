using Avalonia;
using Avalonia.Controls;
using PicView.Avalonia.CustomControls;

namespace PicView.Tests.Controls;

/// <summary>
/// The zoom control reports each change of its scale through one event (ADR-0016), so the viewer can set the tab's
/// Zoom Factor at once instead of polling the scale every frame.
/// </summary>
[Collection("Sequential")]
public class ZoomPanControlTests
{
    public ZoomPanControlTests()
    {
        SetDefaults();
    }

    [AvaloniaFact]
    public void ZoomIn_FromOne_RaisesTheNewScale()
    {
        var control = Control(out var raised);

        control.ZoomIn();

        Assert.Equal([1.2], raised);
        Assert.Equal(1.2, control.Scale);
    }

    [AvaloniaFact]
    public void ResetZoomSlim_FromAZoomedControl_RaisesOne()
    {
        var control = Control(out var raised);
        control.ZoomIn();
        raised.Clear();

        control.ResetZoomSlim();
        control.ResetZoomSlim();

        Assert.Equal([1.0], raised);
    }

    [AvaloniaFact]
    public void ZoomOut_AtOneWithAvoidZoomingOut_RaisesNothing()
    {
        Settings.Zoom.AvoidZoomingOut = true;
        var control = Control(out var raised);

        control.ZoomOut();

        Assert.Empty(raised);
    }

    [AvaloniaFact]
    public void ZoomWithPointerWheelCore_In_RaisesOnePlusTheZoomSpeed()
    {
        Settings.Zoom.ZoomSpeed = 0.3;
        var control = Control(out var raised);

        control.ZoomWithPointerWheelCore(isZoomIn: true, new Point(0, 0));

        Assert.Equal([1.3], raised);
    }

    private static ZoomPanControl Control(out List<double> raised)
    {
        var control = new ZoomPanControl { Child = new Border() };
        var scales = new List<double>();
        control.ScaleChanged += scales.Add;
        raised = scales;
        return control;
    }
}
