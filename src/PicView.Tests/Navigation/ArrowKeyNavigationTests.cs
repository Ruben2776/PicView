using PicView.Avalonia.Navigation;
using PicView.Core.Navigation;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class ArrowKeyNavigationTests
{
    private readonly List<string> _calls = [];

    public ArrowKeyNavigationTests()
    {
        SetDefaults();
    }

    [Fact]
    public async Task Up_GalleryExpanded_NavigatesUpAndDoesNotRotate()
    {
        await ArrowKeyNavigation.Up(() => true, Navigate, () => false, ScrollUp, RotateRight);

        Assert.Equal(["navigate Up"], _calls);
    }

    [Fact]
    public async Task Up_GalleryExpandedAndImageCanScroll_NavigatesUpAndDoesNotScroll()
    {
        Settings.Zoom.ScrollEnabled = true;

        await ArrowKeyNavigation.Up(() => true, Navigate, () => true, ScrollUp, RotateRight);

        Assert.Equal(["navigate Up"], _calls);
    }

    [Fact]
    public async Task Up_GalleryClosed_RotatesRightAndDoesNotNavigate()
    {
        await ArrowKeyNavigation.Up(() => false, Navigate, () => false, ScrollUp, RotateRight);

        Assert.Equal(["rotate right"], _calls);
    }

    [Fact]
    public async Task Up_ScrollingOnAndImageCanScroll_ScrollsUpAndDoesNotRotate()
    {
        Settings.Zoom.ScrollEnabled = true;

        await ArrowKeyNavigation.Up(() => false, Navigate, () => true, ScrollUp, RotateRight);

        Assert.Equal(["scroll up"], _calls);
    }

    [Fact]
    public async Task Up_ScrollingOnAndImageFits_RotatesRight()
    {
        Settings.Zoom.ScrollEnabled = true;

        await ArrowKeyNavigation.Up(() => false, Navigate, () => false, ScrollUp, RotateRight);

        Assert.Equal(["rotate right"], _calls);
    }

    [Fact]
    public async Task Up_ScrollingOffAndImageCanScroll_RotatesRight()
    {
        Settings.Zoom.ScrollEnabled = false;

        await ArrowKeyNavigation.Up(() => false, Navigate, () => true, ScrollUp, RotateRight);

        Assert.Equal(["rotate right"], _calls);
    }

    [Fact]
    public async Task Down_GalleryExpanded_NavigatesDownAndDoesNotRotate()
    {
        await ArrowKeyNavigation.Down(() => true, Navigate, () => false, ScrollDown, RotateLeft);

        Assert.Equal(["navigate Down"], _calls);
    }

    [Fact]
    public async Task Down_GalleryExpandedAndImageCanScroll_NavigatesDownAndDoesNotScroll()
    {
        Settings.Zoom.ScrollEnabled = true;

        await ArrowKeyNavigation.Down(() => true, Navigate, () => true, ScrollDown, RotateLeft);

        Assert.Equal(["navigate Down"], _calls);
    }

    [Fact]
    public async Task Down_GalleryClosed_RotatesLeftAndDoesNotNavigate()
    {
        await ArrowKeyNavigation.Down(() => false, Navigate, () => false, ScrollDown, RotateLeft);

        Assert.Equal(["rotate left"], _calls);
    }

    [Fact]
    public async Task Down_ScrollingOnAndImageCanScroll_ScrollsDownAndDoesNotRotate()
    {
        Settings.Zoom.ScrollEnabled = true;

        await ArrowKeyNavigation.Down(() => false, Navigate, () => true, ScrollDown, RotateLeft);

        Assert.Equal(["scroll down"], _calls);
    }

    [Fact]
    public async Task Down_ScrollingOnAndImageFits_RotatesLeft()
    {
        Settings.Zoom.ScrollEnabled = true;

        await ArrowKeyNavigation.Down(() => false, Navigate, () => false, ScrollDown, RotateLeft);

        Assert.Equal(["rotate left"], _calls);
    }

    [Fact]
    public async Task Down_ScrollingOffAndImageCanScroll_RotatesLeft()
    {
        Settings.Zoom.ScrollEnabled = false;

        await ArrowKeyNavigation.Down(() => false, Navigate, () => true, ScrollDown, RotateLeft);

        Assert.Equal(["rotate left"], _calls);
    }

    private ValueTask Navigate(NavigateTo direction)
    {
        _calls.Add($"navigate {direction}");
        return ValueTask.CompletedTask;
    }

    private ValueTask ScrollUp()
    {
        _calls.Add("scroll up");
        return ValueTask.CompletedTask;
    }

    private ValueTask ScrollDown()
    {
        _calls.Add("scroll down");
        return ValueTask.CompletedTask;
    }

    private ValueTask RotateRight()
    {
        _calls.Add("rotate right");
        return ValueTask.CompletedTask;
    }

    private ValueTask RotateLeft()
    {
        _calls.Add("rotate left");
        return ValueTask.CompletedTask;
    }
}
