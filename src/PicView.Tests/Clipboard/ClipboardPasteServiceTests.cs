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
}
