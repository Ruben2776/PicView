using PicView.Core.Sizing;

namespace PicView.Tests.Sizing;

public class ImageFitterTests
{
    private const int Precision = 6;

    private static readonly ScreenSize Screen = new()
    {
        Width = 1920,
        Height = 1080,
        WorkingAreaWidth = 1920,
        WorkingAreaHeight = 1040,
        Scaling = 1
    };

    // Titlebar (30 + 2) on top and the bottom bar below, no docked gallery
    private static readonly ReservedSpace Bars = new(Top: 32, Bottom: 26, GalleryWidth: 0, GalleryHeight: 0);

    private static FitInput Input(double width, double height, FitMode mode = FitMode.AutoFit) => new()
    {
        ImageWidth = width,
        ImageHeight = height,
        Mode = mode,
        Screen = Screen,
        ScreenMargin = 15,
        ReservedSpace = Bars
    };

    private static Fit Fit(FitInput input) => ImageFitter.Fit(input);

    private static void AssertSizes(Fit fit, double windowWidth, double windowHeight, double width, double height)
    {
        Assert.Equal(windowWidth, fit.WindowWidth, Precision);
        Assert.Equal(windowHeight, fit.WindowHeight, Precision);
        Assert.Equal(width, fit.Width, Precision);
        Assert.Equal(height, fit.Height, Precision);
    }

    #region Single image

    [Fact]
    public void Fit_ImageSmallerThanAvailableArea_ShowsAtNativeSize()
    {
        var fit = Fit(Input(1000, 500));

        // Window adds the 32 + 26 of the bars below and above the image
        AssertSizes(fit, 1000, 558, 1000, 500);
        Assert.Equal(1, fit.Scale, Precision);
        Assert.True(double.IsNaN(fit.ScrollViewerWidth));
        Assert.True(double.IsNaN(fit.ScrollViewerHeight));
    }

    [Fact]
    public void Fit_ImageLargerThanAvailableArea_ShrinksToWorkAreaMinusMarginAndBars()
    {
        // Available area: 1920 - 15 wide, 1040 - 15 - 58 high; width is the tighter limit
        var fit = Fit(Input(4000, 2000));

        AssertSizes(fit, 1905, 1010.5, 1905, 952.5);
        Assert.Equal(0.47625, fit.Scale, Precision);
    }

    [Fact]
    public void Fit_ZoomToFit_ScalesSmallImageUpToAvailableHeight()
    {
        var fit = Fit(Input(400, 300) with { ZoomToFit = true });

        AssertSizes(fit, 1289.333333, 1025, 1289.333333, 967);
    }

    [Fact]
    public void Fit_TinyImage_WindowKeepsMinimumSize()
    {
        var fit = Fit(Input(100, 50));

        AssertSizes(fit, SizeDefaults.WindowMinSize, SizeDefaults.WindowMinSize, 100, 50);
    }

    [Fact]
    public void Fit_FixedWindow_FitsInsideCurrentContentSize()
    {
        var fit = Fit(Input(4000, 2000, FitMode.FixedWindow) with
        {
            WindowContentWidth = 800,
            WindowContentHeight = 600
        });

        AssertSizes(fit, 800, 458, 800, 400);
    }

    [Fact]
    public void Fit_FixedWindowWithDockedGallery_LeavesRoomForGallery()
    {
        var fit = Fit(Input(4000, 2000, FitMode.FixedWindow) with
        {
            WindowContentWidth = 800,
            WindowContentHeight = 600,
            ReservedSpace = Bars with { GalleryWidth = 100 }
        });

        AssertSizes(fit, 800, 408, 700, 350);
    }

    [Fact]
    public void Fit_Fullscreen_FillsWholeScreenIgnoringBars()
    {
        var fit = Fit(Input(4000, 2000, FitMode.Fullscreen));

        AssertSizes(fit, 1920, 1018, 1920, 960);
    }

    [Fact]
    public void Fit_OneQuarterTurn_SwapsWindowWidthAndHeight()
    {
        // Shown 2000 wide and 4000 high, so the available height is the limit
        var fit = Fit(Input(4000, 2000) with { QuarterTurns = 1 });

        AssertSizes(fit, 483.5, 1025, 967, 483.5);
    }

    [Fact]
    public void Fit_QuarterTurnOfTallImage_FillsAvailableWidth()
    {
        // Shown 3000 wide and 1000 high, so it shrinks to 1905 wide and 635 high
        var fit = Fit(Input(1000, 3000) with { QuarterTurns = 1 });

        AssertSizes(fit, 1905, 693, 635, 1905);
        Assert.Equal(0.635, fit.Scale, Precision);
    }

    [Fact]
    public void Fit_Scroll_FitsWidthAndScrollsTheRest()
    {
        var fit = Fit(Input(1000, 3000) with { ScrollEnabled = true });

        AssertSizes(fit, 1000, 1025, 1000, 3000);
        Assert.Equal(1000, fit.ScrollViewerWidth, Precision);
        Assert.Equal(967, fit.ScrollViewerHeight, Precision);
    }

    [Fact]
    public void Fit_ScrollWithQuarterTurn_FitsShownWidthAndScrollsShownHeight()
    {
        // Shown 1000 wide and 3000 high, the same as an unturned 1000x3000 image
        var fit = Fit(Input(3000, 1000) with { ScrollEnabled = true, QuarterTurns = 1 });

        AssertSizes(fit, 1000, 1025, 3000, 1000);
        Assert.Equal(1, fit.Scale, Precision);
        Assert.Equal(1000, fit.ScrollViewerWidth, Precision);
        Assert.Equal(967, fit.ScrollViewerHeight, Precision);
    }

    [Theory]
    [InlineData(0, 500)]
    [InlineData(500, 0)]
    public void Fit_NoPixelSize_ReturnsEmptyFit(double width, double height)
    {
        var fit = Fit(Input(width, height));

        Assert.Equal(default, fit);
    }

    #endregion

    #region Side by side

    private static FitInput Pair(double width, double height, double secondWidth, double secondHeight,
        FitMode mode = FitMode.AutoFit) =>
        Input(width, height, mode) with { SecondImageWidth = secondWidth, SecondImageHeight = secondHeight };

    [Fact]
    public void Fit_PairOfDifferentHeights_ScalesBothToTheTallerHeight()
    {
        // 1000x500 is scaled to 2000x1000 to match, so the pair is 3000x1000
        var fit = Fit(Pair(1000, 500, 1000, 1000));

        AssertSizes(fit, 1905, 693, 1905, 635);
    }

    [Fact]
    public void Fit_PairWithoutZoomToFit_ShowsAtNativeSize()
    {
        var fit = Fit(Pair(1000, 500, 500, 500));

        AssertSizes(fit, 1500, 558, 1500, 500);
        Assert.Equal(1, fit.Scale, Precision);
    }

    [Fact]
    public void Fit_PairWithZoomToFit_ScalesUpToAvailableWidth()
    {
        var fit = Fit(Pair(1000, 500, 500, 500) with { ZoomToFit = true });

        AssertSizes(fit, 1905, 693, 1905, 635);
        Assert.Equal(1.27, fit.Scale, Precision);
    }

    [Fact]
    public void Fit_PairInFixedWindowWithDockedGallery_LeavesRoomForGallery()
    {
        var fit = Fit(Pair(1000, 500, 500, 500, FitMode.FixedWindow) with
        {
            WindowContentWidth = 800,
            WindowContentHeight = 600,
            ReservedSpace = Bars with { GalleryWidth = 100 }
        });

        AssertSizes(fit, 800, SizeDefaults.WindowMinSize, 700, 233.333333);
    }

    [Fact]
    public void Fit_SmallPair_WindowKeepsMinimumSize()
    {
        var fit = Fit(Pair(100, 100, 100, 100, FitMode.FixedWindow) with
        {
            WindowContentWidth = 200,
            WindowContentHeight = 200
        });

        AssertSizes(fit, SizeDefaults.WindowMinSize, SizeDefaults.WindowMinSize, 200, 100);
    }

    [Fact]
    public void Fit_PairInFullscreen_FillsWholeScreen()
    {
        var fit = Fit(Pair(2000, 1000, 2000, 1000, FitMode.Fullscreen) with
        {
            WindowContentWidth = 1000,
            WindowContentHeight = 800
        });

        AssertSizes(fit, 1920, 538, 1920, 480);
    }

    [Fact]
    public void Fit_PairWithQuarterTurn_SwapsWindowWidthAndHeight()
    {
        // The 1500x500 pair is shown 500 wide and 1500 high, so the available height is the limit
        var fit = Fit(Pair(1000, 500, 500, 500) with { QuarterTurns = 1 });

        AssertSizes(fit, 322.333333, 1025, 967, 322.333333);
    }

    #endregion
}
