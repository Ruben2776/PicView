using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
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

    [Theory]
    // host, left pressed, scale, modifiers, Space held => pans
    [InlineData(ZoomPanHost.ImageViewer, true, 2.0, KeyModifiers.None, false, true)]
    [InlineData(ZoomPanHost.ImageViewer, true, 2.0, KeyModifiers.Shift, false, false)]
    [InlineData(ZoomPanHost.ImageViewer, true, 1.0, KeyModifiers.None, false, false)]
    [InlineData(ZoomPanHost.ImageViewer, false, 2.0, KeyModifiers.None, false, false)]
    [InlineData(ZoomPanHost.Crop, true, 2.0, KeyModifiers.None, false, false)]
    [InlineData(ZoomPanHost.Crop, true, 2.0, KeyModifiers.None, true, true)]
    [InlineData(ZoomPanHost.Crop, true, 2.0, KeyModifiers.Shift, true, true)]
    [InlineData(ZoomPanHost.Crop, true, 1.0, KeyModifiers.None, true, false)]
    [InlineData(ZoomPanHost.Crop, true, 0.5, KeyModifiers.None, true, false)]
    internal void StartsPan_HostAndInput_FollowsTheHost(ZoomPanHost host, bool left, double scale,
        KeyModifiers modifiers, bool space, bool expected)
        => Assert.Equal(expected, ZoomPanControl.StartsPan(host, left, scale, modifiers, space));

    [AvaloniaFact]
    public void Press_CropHostWithoutSpace_DoesNotPan()
        => Hosted(ZoomPanHost.Crop, (window, control, _) =>
        {
            Drag(window);

            Assert.Equal(-200, control.TranslateX);
        });

    [AvaloniaFact]
    public void Press_CropHostWithSpaceHeld_PansAndHandlesThePress()
        => Hosted(ZoomPanHost.Crop, (window, control, child) =>
        {
            var pressed = 0;
            child.PointerPressed += (_, _) => pressed++;
            Space(window, down: true);

            Drag(window);

            Assert.Equal(-250, control.TranslateX);
            Assert.Equal(0, pressed);
        });

    [AvaloniaFact]
    public void Press_ImageViewerHost_PansWithoutSpace()
        => Hosted(ZoomPanHost.ImageViewer, (window, control, _) =>
        {
            Drag(window);

            Assert.Equal(-250, control.TranslateX);
        });

    [AvaloniaFact]
    public void SpaceReleased_ClearsIsSpaceHeld()
        => Hosted(ZoomPanHost.Crop, (window, control, _) =>
        {
            var changes = new List<bool>();
            control.SpaceHeldChanged += changes.Add;

            Space(window, down: true);
            Space(window, down: true);
            Space(window, down: false);

            Assert.False(control.IsSpaceHeld);
            Assert.Equal([true, false], changes);
        });

    [AvaloniaFact]
    public void OnWindowDeactivated_WithSpaceHeld_ClearsIt()
        => Hosted(ZoomPanHost.Crop, (window, control, _) =>
        {
            // The window never sees Space released once another window, a dialog or a menu has the keyboard
            Space(window, down: true);

            control.OnWindowDeactivated();
            Drag(window);

            Assert.False(control.IsSpaceHeld);
            Assert.Equal(-200, control.TranslateX);
        });

    [AvaloniaFact]
    public void Space_ImageViewerHost_IsNotTracked()
        => Hosted(ZoomPanHost.ImageViewer, (window, control, _) =>
        {
            Space(window, down: true);

            Assert.False(control.IsSpaceHeld);
        });

    [AvaloniaFact]
    public void SetView_ThenViewCentre_RoundTrips()
        => Hosted(ZoomPanHost.Crop, (_, control, _) =>
        {
            control.SetView(2, new Point(100, 75));

            Assert.Equal(2, control.Scale);
            Assert.Equal(new Point(100, 75), control.ViewCentre);
            Assert.Equal(new Rect(0, 0, 200, 150), control.VisibleChildRect);
        });

    [AvaloniaFact]
    public void ZoomIn_CropHostWithAnimatedZoomOn_SetsNoTransitions()
    {
        Settings.Zoom.IsZoomAnimated = true;
        Hosted(ZoomPanHost.Crop, (_, control, child) =>
        {
            control.ZoomIn();
            control.ZoomIn();

            var group = Assert.IsType<TransformGroup>(child.RenderTransform);
            Assert.All(group.Children, transform => Assert.Null(((Animatable)transform).Transitions));
        });
    }

    [AvaloniaFact]
    public void ZoomIn_ImageViewerHostWithAnimatedZoomOn_SetsTransitions()
    {
        Settings.Zoom.IsZoomAnimated = true;
        Hosted(ZoomPanHost.ImageViewer, (_, control, child) =>
        {
            control.ZoomIn();
            control.ZoomIn();

            var group = Assert.IsType<TransformGroup>(child.RenderTransform);
            Assert.All(group.Children, transform => Assert.NotNull(((Animatable)transform).Transitions));
        });
    }

    [AvaloniaFact]
    public void Initialize_WithoutAPreviewer_Zooms()
        => Hosted(ZoomPanHost.Crop, (_, control, _) =>
        {
            control.SetScaleImmediate(1);
            var raised = new List<double>();
            control.ScaleChanged += raised.Add;

            control.ZoomIn();

            Assert.Equal([1.2], raised);
        });

    // A press at the centre of the view, dragged 50 to the left
    private static void Drag(Window window)
    {
        window.MouseDown(new Point(200, 150), MouseButton.Left);
        window.MouseMove(new Point(150, 150));
        window.MouseUp(new Point(150, 150), MouseButton.Left);
    }

    private static void Space(Window window, bool down)
    {
        if (down)
        {
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        }
        else
        {
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        }
    }

    /// <summary>
    /// Runs <paramref name="test"/> on a control that fills a shown 400 by 300 window, holds a child of that size
    /// and is zoomed to 2 about its centre, so its translation starts at -200, -150.
    /// </summary>
    private static void Hosted(ZoomPanHost host, Action<Window, ZoomPanControl, Border> test)
    {
        var child = new Border { Width = 400, Height = 300, Background = Brushes.Black };
        var control = new ZoomPanControl { Host = host, Child = child };
        var window = new Window { Width = 400, Height = 300, Content = control };
        try
        {
            window.Show();
            control.Initialize();
            Dispatcher.UIThread.RunJobs();
            control.SetScaleImmediate(2);
            test(window, control, child);
        }
        finally
        {
            window.Close();
        }
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
