using PicView.Avalonia.Clipboard;

namespace PicView.Tests.Clipboard;

public class ClipboardPasteServiceTests
{
    [Fact]
    public async Task PasteAsync_WhenClipboardIsNull_ReturnsFalse()
    {
        var service = new ClipboardPasteService(clipboard: null);

        var result = await service.PasteAsync(null!);

        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PasteTextAsync_WhenTextIsNullOrWhitespace_ReturnsFalse(string? text)
    {
        var service = new ClipboardPasteService(clipboard: null);

        var result = await service.PasteTextAsync(text!, null!);

        Assert.False(result);
    }

    [Fact]
    public async Task PasteFilesAsync_WhenFilePathsEmpty_ReturnsFalse()
    {
        var service = new ClipboardPasteService(clipboard: null);

        var result = await service.PasteFilesAsync([], null!);

        Assert.False(result);
    }

    [Fact]
    public async Task PasteImageAsync_WhenClipboardIsNull_ReturnsFalse()
    {
        var service = new ClipboardPasteService(clipboard: null);

        var result = await service.PasteImageAsync(null!);

        Assert.False(result);
    }

    [Theory]
    [InlineData("<?xml version='1.0' encoding='UTF-16'?><svg/>", "<svg/>")]
    [InlineData("<?xml version='1.0'?>   <svg/>", "<svg/>")]
    [InlineData("<?XML version='1.0'?><svg/>", "<svg/>")]
    [InlineData("<svg/>", "<svg/>")]
    [InlineData("<?xml-stylesheet href='a.css'?><svg/>", "<?xml-stylesheet href='a.css'?><svg/>")]
    [InlineData("<?xml version='1.0'", "<?xml version='1.0'")]
    [InlineData("<?xml", "<?xml")]
    [InlineData("", "")]
    public void WithoutXmlDeclaration_Code_RemovesOnlyALeadingDeclaration(string code, string expected)
    {
        Assert.Equal(expected, ClipboardPasteService.WithoutXmlDeclaration(code));
    }
}
