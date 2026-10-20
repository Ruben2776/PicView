using PicView.Core.Printing;

namespace PicView.Tests.Print;

public class PrintLayoutTests
{
    // At 25.4 DPI one millimeter is one pixel, so page values read as millimeters
    private const double OnePxPerMm = 25.4;

    private static PrintSettings A4Settings(ScaleModes scaleMode, int margin = 10,
        Orientations orientation = Orientations.Portrait) => new()
    {
        PaperSize = { Value = "A4" },
        Orientation = { Value = (int)orientation },
        ScaleMode = { Value = (int)scaleMode },
        MarginTop = { Value = margin },
        MarginBottom = { Value = margin },
        MarginLeft = { Value = margin },
        MarginRight = { Value = margin }
    };

    private static void AssertRect(PrintRect expected, PrintRect actual)
    {
        Assert.Equal(expected.X, actual.X, 6);
        Assert.Equal(expected.Y, actual.Y, 6);
        Assert.Equal(expected.Width, actual.Width, 6);
        Assert.Equal(expected.Height, actual.Height, 6);
    }

    [Fact]
    public void Compute_Portrait_PageAndContentFollowPaperAndMargins()
    {
        var layout = PrintLayout.Compute(A4Settings(ScaleModes.Fit), 100, 100, OnePxPerMm);

        Assert.Equal(210, layout.PageWidthPx, 6);
        Assert.Equal(297, layout.PageHeightPx, 6);
        AssertRect(new PrintRect(10, 10, 190, 277), layout.Content);
    }

    [Fact]
    public void Compute_Landscape_SwapsPageSides()
    {
        var layout = PrintLayout.Compute(A4Settings(ScaleModes.Fit, orientation: Orientations.Landscape),
            100, 100, OnePxPerMm);

        Assert.Equal(297, layout.PageWidthPx, 6);
        Assert.Equal(210, layout.PageHeightPx, 6);
        AssertRect(new PrintRect(10, 10, 277, 190), layout.Content);
    }

    [Fact]
    public void Compute_Fit_ScalesWholeImageIntoContentAndCenters()
    {
        var layout = PrintLayout.Compute(A4Settings(ScaleModes.Fit), 1900, 950, OnePxPerMm);

        AssertRect(new PrintRect(0, 0, 1900, 950), layout.Source);
        AssertRect(new PrintRect(10, 101, 190, 95), layout.Destination);
    }

    [Fact]
    public void Compute_Fill_CoversContentAndCropsImageToIt()
    {
        var layout = PrintLayout.Compute(A4Settings(ScaleModes.Fill), 1900, 950, OnePxPerMm);

        AssertRect(layout.Content, layout.Destination);

        // The full height is kept; the sides are cropped evenly to the content's aspect ratio
        var expectedSourceWidth = 950 * 190.0 / 277;
        AssertRect(new PrintRect((1900 - expectedSourceWidth) / 2, 0, expectedSourceWidth, 950), layout.Source);
    }

    [Fact]
    public void Compute_Stretch_DrawsWholeImageOverContent()
    {
        var layout = PrintLayout.Compute(A4Settings(ScaleModes.Stretch), 1900, 950, OnePxPerMm);

        AssertRect(new PrintRect(0, 0, 1900, 950), layout.Source);
        AssertRect(layout.Content, layout.Destination);
    }

    [Fact]
    public void Compute_CenterWithImageThatFits_KeepsPrintDpiSizeAndCenters()
    {
        // 300 x 600 px at 300 DPI is 1 x 2 inches
        var layout = PrintLayout.Compute(A4Settings(ScaleModes.Center), 300, 600, OnePxPerMm);

        AssertRect(new PrintRect(0, 0, 300, 600), layout.Source);
        AssertRect(new PrintRect(10 + (190 - 25.4) / 2, 10 + (277 - 50.8) / 2, 25.4, 50.8), layout.Destination);
    }

    [Fact]
    public void Compute_CenterWithImageLargerThanContent_ShrinksLikeFit()
    {
        var center = PrintLayout.Compute(A4Settings(ScaleModes.Center), 6000, 3000, OnePxPerMm);
        var fit = PrintLayout.Compute(A4Settings(ScaleModes.Fit), 6000, 3000, OnePxPerMm);

        AssertRect(fit.Source, center.Source);
        AssertRect(fit.Destination, center.Destination);
    }

    [Theory]
    [InlineData(ScaleModes.Fit)]
    [InlineData(ScaleModes.Fill)]
    [InlineData(ScaleModes.Stretch)]
    [InlineData(ScaleModes.Center)]
    public void Compute_PreviewAndPrintDpi_GiveSamePhysicalLayout(ScaleModes scaleMode)
    {
        var settings = A4Settings(scaleMode);
        var preview = PrintLayout.Compute(settings, 400, 300, 96);
        var print = PrintLayout.Compute(settings, 400, 300, PrintLayout.PrintDpi);

        var ratio = PrintLayout.PrintDpi / 96.0;
        AssertRect(preview.Source, print.Source);
        AssertRect(
            new PrintRect(preview.Destination.X * ratio, preview.Destination.Y * ratio,
                preview.Destination.Width * ratio, preview.Destination.Height * ratio),
            print.Destination);
    }

    [Theory]
    [InlineData(ScaleModes.Fit)]
    [InlineData(ScaleModes.Fill)]
    [InlineData(ScaleModes.Stretch)]
    [InlineData(ScaleModes.Center)]
    public void Compute_AnyScaleMode_DestinationStaysInsideContent(ScaleModes scaleMode)
    {
        var layout = PrintLayout.Compute(A4Settings(scaleMode), 5000, 200, OnePxPerMm);

        Assert.True(layout.Destination.X >= layout.Content.X - 1e-9);
        Assert.True(layout.Destination.Y >= layout.Content.Y - 1e-9);
        Assert.True(layout.Destination.X + layout.Destination.Width <= layout.Content.X + layout.Content.Width + 1e-9);
        Assert.True(layout.Destination.Y + layout.Destination.Height <= layout.Content.Y + layout.Content.Height + 1e-9);
    }

    [Fact]
    public void Compute_MarginsWiderThanPage_DrawsNothing()
    {
        var layout = PrintLayout.Compute(A4Settings(ScaleModes.Fit, margin: 200), 100, 100, OnePxPerMm);

        Assert.Equal(0, layout.Content.Width);
        Assert.Equal(0, layout.Destination.Width);
        Assert.Equal(0, layout.Destination.Height);
    }

    [Fact]
    public void Compute_EmptyImage_DrawsNothing()
    {
        var layout = PrintLayout.Compute(A4Settings(ScaleModes.Fit), 0, 0, OnePxPerMm);

        Assert.Equal(0, layout.Destination.Width);
        Assert.Equal(0, layout.Destination.Height);
    }

    [Fact]
    public void Compute_WithExplicitPaper_IgnoresSettingsPaperAndOrientation()
    {
        var settings = A4Settings(ScaleModes.Fit, orientation: Orientations.Landscape);

        var layout = PrintLayout.Compute(new Paper(100, 150), settings, 100, 100, OnePxPerMm);

        Assert.Equal(100, layout.PageWidthPx, 6);
        Assert.Equal(150, layout.PageHeightPx, 6);
    }
}
