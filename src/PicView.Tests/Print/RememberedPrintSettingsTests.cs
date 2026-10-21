using PicView.Core.Printing;
using PrintWindowProperties = PicView.Core.Config.PrintWindowConfig.PrintWindowProperties;

namespace PicView.Tests.Print;

public class RememberedPrintSettingsTests
{
    private static readonly Printer Office = Printer.FromSystem("Office printer");
    private static readonly Printer Photo = Printer.FromSystem("Photo printer");

    [Fact]
    public void Restore_NothingRemembered_UsesDefaultPrinterAndDefaults()
    {
        var settings = RememberedPrintSettings.Restore(null, [Printer.SaveAsPdf, Office, Photo], Photo,
            _ => ["Letter", "A4"]);

        Assert.Equal(Photo, settings.Printer.Value);
        Assert.Equal("Letter", settings.PaperSize.Value);
        Assert.Equal((int)ScaleModes.Fit, settings.ScaleMode.Value);
        Assert.Equal((int)Orientations.Portrait, settings.Orientation.Value);
        Assert.Equal(1, settings.Copies.Value);
        Assert.Equal(10, settings.MarginLeft.Value);
    }

    [Fact]
    public void Restore_RememberedPrinterStillThere_RestoresItAndItsSettings()
    {
        var remembered = new PrintWindowProperties
        {
            PrinterName = Office.Id, PaperSize = "A4", ScaleMode = (int)ScaleModes.Fill, Copies = 3, MarginTop = 25
        };

        var settings = RememberedPrintSettings.Restore(remembered, [Printer.SaveAsPdf, Office, Photo], Photo,
            _ => ["Letter", "A4"]);

        Assert.Equal(Office, settings.Printer.Value);
        Assert.Equal("A4", settings.PaperSize.Value);
        Assert.Equal((int)ScaleModes.Fill, settings.ScaleMode.Value);
        Assert.Equal(3, settings.Copies.Value);
        Assert.Equal(25, settings.MarginTop.Value);
    }

    [Fact]
    public void Restore_RememberedPrinterGone_UsesDefaultPrinter()
    {
        var remembered = new PrintWindowProperties { PrinterName = "Printer that was removed" };

        var settings = RememberedPrintSettings.Restore(remembered, [Printer.SaveAsPdf, Office], Office, _ => []);

        Assert.Equal(Office, settings.Printer.Value);
    }

    [Fact]
    public void RememberThenRestore_SaveAsPdfInAnotherLanguage_RestoresSaveAsPdf()
    {
        var remembered = new PrintWindowProperties();
        RememberedPrintSettings.Remember(new PrintSettings { Printer = { Value = Printer.SaveAsPdf } }, remembered);

        // The same Printer, as the next session names it in another language
        var saveAsPdfInGerman = new Printer(Printer.SaveAsPdfId, "Als PDF speichern");
        var settings = RememberedPrintSettings.Restore(remembered, [saveAsPdfInGerman, Office], Office, _ => []);

        Assert.Equal(Printer.SaveAsPdfId, remembered.PrinterName);
        Assert.True(settings.Printer.Value!.IsSaveAsPdf);
    }

    [Theory]
    [InlineData("Letter", new[] { "A4", "Letter" }, "Letter")]
    [InlineData("Legal", new[] { "A4", "Letter" }, "A4")]
    [InlineData(null, new[] { "Letter" }, "Letter")]
    [InlineData("Letter", new string[0], RememberedPrintSettings.FallbackPaperSize)]
    public void ChoosePaper_KeepsWantedPaperOnlyWhenOffered(string? wanted, string[] paperSizes, string expected)
    {
        Assert.Equal(expected, RememberedPrintSettings.ChoosePaper(wanted, paperSizes));
    }

    [Fact]
    public void Remember_WritesEverySetting()
    {
        var settings = new PrintSettings
        {
            Printer = { Value = Photo },
            PaperSize = { Value = "A3" },
            Orientation = { Value = (int)Orientations.Landscape },
            ScaleMode = { Value = (int)ScaleModes.Center },
            ColorMode = { Value = (int)ColorModes.BlackAndWhite },
            Copies = { Value = 2 },
            MarginTop = { Value = 1 },
            MarginBottom = { Value = 2 },
            MarginLeft = { Value = 3 },
            MarginRight = { Value = 4 }
        };
        var remembered = new PrintWindowProperties();

        RememberedPrintSettings.Remember(settings, remembered);

        Assert.Equal(Photo.Id, remembered.PrinterName);
        Assert.Equal("A3", remembered.PaperSize);
        Assert.Equal((int)Orientations.Landscape, remembered.Orientation);
        Assert.Equal((int)ScaleModes.Center, remembered.ScaleMode);
        Assert.Equal((int)ColorModes.BlackAndWhite, remembered.ColorMode);
        Assert.Equal(2, remembered.Copies);
        Assert.Equal((1, 2, 3, 4),
            (remembered.MarginTop, remembered.MarginBottom, remembered.MarginLeft, remembered.MarginRight));
    }
}
