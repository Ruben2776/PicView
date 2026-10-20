using PicView.Avalonia.Navigation;
using PicView.Core.Navigation;

namespace PicView.Tests.Navigation;

public class ArrowKeyNavigationTests
{
    private readonly List<string> _calls = [];

    [Fact]
    public async Task Up_GalleryExpanded_NavigatesUpAndDoesNotRotate()
    {
        await ArrowKeyNavigation.Up(() => true, Navigate, RotateRight);

        Assert.Equal(["navigate Up"], _calls);
    }

    [Fact]
    public async Task Up_GalleryClosed_RotatesRightAndDoesNotNavigate()
    {
        await ArrowKeyNavigation.Up(() => false, Navigate, RotateRight);

        Assert.Equal(["rotate right"], _calls);
    }

    [Fact]
    public async Task Down_GalleryExpanded_NavigatesDownAndDoesNotRotate()
    {
        await ArrowKeyNavigation.Down(() => true, Navigate, RotateLeft);

        Assert.Equal(["navigate Down"], _calls);
    }

    [Fact]
    public async Task Down_GalleryClosed_RotatesLeftAndDoesNotNavigate()
    {
        await ArrowKeyNavigation.Down(() => false, Navigate, RotateLeft);

        Assert.Equal(["rotate left"], _calls);
    }

    private ValueTask Navigate(NavigateTo direction)
    {
        _calls.Add($"navigate {direction}");
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
