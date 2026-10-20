using PicView.Core.Printing;

namespace PicView.Tests.Print;

public class PaperSizeHelperTests
{
    [Theory]
    [InlineData("A4", 210, 297)]
    [InlineData("a3", 297, 420)]
    [InlineData("Letter", 215.9, 279.4)]
    [InlineData("8x10", 203.2, 254)]
    // Windows paper names
    [InlineData("Letter Small", 215.9, 279.4)]
    [InlineData("A4 (Borderless)", 210, 297)]
    // CUPS paper names
    [InlineData("iso_a4_210x297mm", 210, 297)]
    [InlineData("iso_a5_148x210mm", 148, 210)]
    [InlineData("na_letter_8.5x11in", 215.9, 279.4)]
    [InlineData("na_legal_8.5x14in", 215.9, 355.6)]
    [InlineData("A4.Borderless", 210, 297)]
    [InlineData("na_index-4x6_4x6in", 101.6, 152.4)]
    public void Resolve_KnownPaperName_ReturnsPortraitSize(string name, double width, double height)
    {
        var paper = PaperSizeHelper.Resolve(name, (int)Orientations.Portrait);

        Assert.Equal(new Paper(width, height), paper);
    }

    [Fact]
    public void Resolve_Landscape_SwapsSides()
    {
        var paper = PaperSizeHelper.Resolve("A4", (int)Orientations.Landscape);

        Assert.Equal(new Paper(297, 210), paper);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Envelope #10")]
    public void Resolve_UnknownOrMissingName_FallsBackToA4(string? name)
    {
        var paper = PaperSizeHelper.Resolve(name, (int)Orientations.Portrait);

        Assert.Equal(new Paper(210, 297), paper);
    }

    [Fact]
    public void GetAllNames_ListsEveryKnownSize()
    {
        Assert.Equal(["A4", "A3", "A5", "Letter", "Legal", "Tabloid", "4x6", "5x7", "8x10"],
            PaperSizeHelper.GetAllNames());
    }
}
