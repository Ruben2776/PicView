using PicView.Core.Printing;

namespace PicView.Tests.Print;

public class PrintSettingsTests
{
    [Fact]
    public void CopyFrom_CopiesEverySettingIntoTheSameProperties()
    {
        var photo = Printer.FromSystem("Photo printer");
        var settings = new PrintSettings();
        var printerProperty = settings.Printer;
        var other = new PrintSettings
        {
            Printer = { Value = photo },
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

        settings.CopyFrom(other);

        Assert.Same(printerProperty, settings.Printer);
        Assert.Equal(photo, settings.Printer.Value);
        Assert.Equal("A3", settings.PaperSize.Value);
        Assert.Equal((int)Orientations.Landscape, settings.Orientation.Value);
        Assert.Equal((int)ScaleModes.Center, settings.ScaleMode.Value);
        Assert.Equal((int)ColorModes.BlackAndWhite, settings.ColorMode.Value);
        Assert.Equal(2, settings.Copies.Value);
        Assert.Equal((1, 2, 3, 4),
            (settings.MarginTop.Value, settings.MarginBottom.Value, settings.MarginLeft.Value,
                settings.MarginRight.Value));
    }

    private static PrintSettings Letter() => new()
    {
        Printer = { Value = Printer.FromSystem("Office printer") },
        PaperSize = { Value = "Letter" },
        MarginTop = { Value = 10 },
        MarginBottom = { Value = 10 },
        MarginLeft = { Value = 10 },
        MarginRight = { Value = 10 }
    };

    [Fact]
    public void Matches_SameSettings_IsTrue()
    {
        Assert.True(Letter().Matches(Letter()));
    }

    [Theory]
    [InlineData(nameof(PrintSettings.Printer))]
    [InlineData(nameof(PrintSettings.PaperSize))]
    [InlineData(nameof(PrintSettings.Orientation))]
    [InlineData(nameof(PrintSettings.ScaleMode))]
    [InlineData(nameof(PrintSettings.ColorMode))]
    [InlineData(nameof(PrintSettings.Copies))]
    [InlineData(nameof(PrintSettings.MarginTop))]
    [InlineData(nameof(PrintSettings.MarginBottom))]
    [InlineData(nameof(PrintSettings.MarginLeft))]
    [InlineData(nameof(PrintSettings.MarginRight))]
    public void Matches_OneSettingDiffers_IsFalse(string setting)
    {
        var other = Letter();
        switch (setting)
        {
            case nameof(PrintSettings.Printer):
                other.Printer.Value = Printer.SaveAsPdf;
                break;
            case nameof(PrintSettings.PaperSize):
                other.PaperSize.Value = "A4";
                break;
            case nameof(PrintSettings.Orientation):
                other.Orientation.Value = (int)Orientations.Landscape;
                break;
            case nameof(PrintSettings.ScaleMode):
                other.ScaleMode.Value = (int)ScaleModes.Fill;
                break;
            case nameof(PrintSettings.ColorMode):
                other.ColorMode.Value = (int)ColorModes.Color;
                break;
            case nameof(PrintSettings.Copies):
                other.Copies.Value = 2;
                break;
            case nameof(PrintSettings.MarginTop):
                other.MarginTop.Value = 11;
                break;
            case nameof(PrintSettings.MarginBottom):
                other.MarginBottom.Value = 11;
                break;
            case nameof(PrintSettings.MarginLeft):
                other.MarginLeft.Value = 11;
                break;
            case nameof(PrintSettings.MarginRight):
                other.MarginRight.Value = 11;
                break;
        }

        Assert.False(Letter().Matches(other));
    }

    [Fact]
    public void Matches_SaveAsPdfNamedInAnotherLanguage_IsTrue()
    {
        var settings = Letter();
        settings.Printer.Value = Printer.SaveAsPdf;
        var other = Letter();
        other.Printer.Value = new Printer(Printer.SaveAsPdfId, "Als PDF speichern");

        Assert.True(settings.Matches(other));
    }
}
