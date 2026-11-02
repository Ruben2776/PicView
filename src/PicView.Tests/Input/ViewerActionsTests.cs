using Avalonia;
using PicView.Avalonia.Input;

namespace PicView.Tests.Input;

public class ViewerActionsTests
{
    [Fact]
    public void ScrollLine_Down_CallsScrollLineDown()
    {
        var viewer = new RecordingViewer();

        ViewerActions.ScrollLine(viewer, down: true);

        Assert.Equal(["ScrollLine(down)"], viewer.Calls);
    }

    [Fact]
    public void ScrollLine_Up_CallsScrollLineUp()
    {
        var viewer = new RecordingViewer();

        ViewerActions.ScrollLine(viewer, down: false);

        Assert.Equal(["ScrollLine(up)"], viewer.Calls);
    }

    [Fact]
    public void ScrollToTop_CallsScrollToHome()
    {
        var viewer = new RecordingViewer();

        ViewerActions.ScrollToTop(viewer);

        Assert.Equal(["ScrollToHome"], viewer.Calls);
    }

    [Fact]
    public void ScrollToBottom_CallsScrollToEnd()
    {
        var viewer = new RecordingViewer();

        ViewerActions.ScrollToBottom(viewer);

        Assert.Equal(["ScrollToEnd"], viewer.Calls);
    }

    [Fact]
    public void ZoomIn_CallsZoomIn()
    {
        var viewer = new RecordingViewer();

        ViewerActions.ZoomIn(viewer);

        Assert.Equal(["ZoomIn"], viewer.Calls);
    }

    [Fact]
    public void ZoomOut_CallsZoomOut()
    {
        var viewer = new RecordingViewer();

        ViewerActions.ZoomOut(viewer);

        Assert.Equal(["ZoomOut"], viewer.Calls);
    }

    [Fact]
    public void AnyAction_NoViewer_DoesNothingAndDoesNotThrow()
    {
        IZoomScrollTarget? none = null;

        ViewerActions.ScrollLine(none, down: true);
        ViewerActions.ScrollLine(none, down: false);
        ViewerActions.ScrollToTop(none);
        ViewerActions.ScrollToBottom(none);
        ViewerActions.ZoomIn(none);
        ViewerActions.ZoomOut(none);
    }

    private sealed class RecordingViewer : IZoomScrollTarget
    {
        public List<string> Calls { get; } = [];

        public bool CanScrollVertically => false;

        public void ScrollLine(bool down) => Calls.Add(down ? "ScrollLine(down)" : "ScrollLine(up)");

        public void ScrollToHome() => Calls.Add("ScrollToHome");

        public void ScrollToEnd() => Calls.Add("ScrollToEnd");

        public void ZoomIn() => Calls.Add("ZoomIn");

        public void ZoomOut() => Calls.Add("ZoomOut");

        public void ZoomAt(bool zoomIn, Point pointer)
        {
        }
    }
}
