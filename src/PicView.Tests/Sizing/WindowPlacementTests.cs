using PicView.Core.Sizing;

namespace PicView.Tests.Sizing;

public class WindowPlacementTests
{
    // A 1920×1080 monitor at 100% whose taskbar takes the bottom 40 pixels
    private static readonly ScreenSize Primary = new()
    {
        Width = 1920, Height = 1080, WorkingAreaWidth = 1920, WorkingAreaHeight = 1040, Scaling = 1
    };

    // A 2560×1440 monitor at 150% to the right of it, whose work area starts at pixel 1920 and leaves 40 pixels
    // at the bottom; in DIP that is 1706.67×933.33 starting at 1280
    private static readonly ScreenSize RightAt150 = new()
    {
        X = 1280, Y = 0, WorkingAreaX = 1280, WorkingAreaY = 0,
        Width = 2560 / 1.5, Height = 1440 / 1.5, WorkingAreaWidth = 2560 / 1.5, WorkingAreaHeight = 1400 / 1.5,
        Scaling = 1.5
    };

    [Fact]
    public void Centered_AtOneHundredPercent_CentersInTheWorkArea()
    {
        Assert.Equal(new PixelPosition(560, 220), WindowPlacement.Centered(Primary, 800, 600));
    }

    [Fact]
    public void Centered_OnAScaledMonitor_CentersInItsWorkAreaInPixels()
    {
        // The 800×600 window is 1200×900 pixels: (1920 + (2560 - 1200) / 2, (1400 - 900) / 2)
        Assert.Equal(new PixelPosition(2600, 250), WindowPlacement.Centered(RightAt150, 800, 600));
    }

    [Fact]
    public void Centered_WorkAreaBelowAMenuBar_CentersBelowIt()
    {
        var screen = Primary with { WorkingAreaY = 25, WorkingAreaHeight = 1055 };

        Assert.Equal(new PixelPosition(560, 252), WindowPlacement.Centered(screen, 800, 600));
    }

    [Fact]
    public void Centered_AtTheTop_KeepsTheWindowAtTheTopOfTheWorkArea()
    {
        var screen = Primary with { WorkingAreaY = 25 };

        Assert.Equal(new PixelPosition(560, 25), WindowPlacement.Centered(screen, 800, 600, top: true));
    }

    [Fact]
    public void AfterResize_WithoutStayCentered_KeepsTheWindowsOwnCenter()
    {
        // Shrinking by 200×200 DIP is 300×300 pixels at 150%, so the corner moves by half of that
        var position = WindowPlacement.AfterResize(RightAt150, new PixelPosition(2000, 100), 800, 600, 600, 400,
            keepCentered: false);

        Assert.Equal(new PixelPosition(2150, 250), position);
    }

    [Fact]
    public void AfterResize_WithStayCentered_CentersTheNewSizeInTheWorkArea()
    {
        var position = WindowPlacement.AfterResize(Primary, new PixelPosition(10, 10), 800, 600, 1000, 700,
            keepCentered: true);

        Assert.Equal(new PixelPosition(460, 170), position);
    }

    [Fact]
    public void CenteredVertically_KeepsTheHorizontalCenter()
    {
        Assert.Equal(new PixelPosition(100, 370), WindowPlacement.CenteredVertically(Primary, 300, 400, 300));
    }

    [Fact]
    public void CenteredVertically_PastTheLeftEdge_IsClampedInside()
    {
        Assert.Equal(new PixelPosition(0, 220), WindowPlacement.CenteredVertically(Primary, 100, 800, 600));
    }

    [Fact]
    public void CenteredVertically_PastTheRightEdge_IsClampedInside()
    {
        Assert.Equal(new PixelPosition(1120, 220), WindowPlacement.CenteredVertically(Primary, 1800, 800, 600));
    }

    [Fact]
    public void CenteredVertically_WiderThanTheWorkArea_AlignsWithItsLeftEdge()
    {
        Assert.Equal(new PixelPosition(0, 220), WindowPlacement.CenteredVertically(Primary, 960, 2000, 600));
    }

    [Fact]
    public void CenteredVertically_OnAScaledMonitor_PlacesInPixels()
    {
        // Left edge 1880 - 400 = 1480 DIP is 2220 pixels; vertically as Centered_OnAScaledMonitor
        Assert.Equal(new PixelPosition(2220, 250), WindowPlacement.CenteredVertically(RightAt150, 1880, 800, 600));
    }

    [Fact]
    public void CenteredVertically_WorkAreaBelowAMenuBar_CentersBelowIt()
    {
        var screen = Primary with { WorkingAreaY = 25, WorkingAreaHeight = 1055 };

        Assert.Equal(new PixelPosition(560, 252), WindowPlacement.CenteredVertically(screen, 960, 800, 600));
    }
}
