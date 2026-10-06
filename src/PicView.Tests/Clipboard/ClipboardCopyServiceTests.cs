using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input.Platform;
using PicView.Avalonia;
using PicView.Avalonia.Clipboard;
using PicView.Core.IPlatform;
using Xunit;

namespace PicView.Tests.Clipboard;

[Collection("Sequential")]
public class ClipboardCopyServiceTests
{
    public ClipboardCopyServiceTests()
    {
        try
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private class TestPlatformService : IPlatformSpecificService
    {
        public bool CutFileResult { get; set; } = true;
        public string? LastCutFilePath { get; private set; }

        public Task<bool> CutFile(string path)
        {
            LastCutFilePath = path;
            return Task.FromResult(CutFileResult);
        }

        public void SetTaskbarProgress(ulong progress, ulong maximum) { }
        public void StopTaskbarProgress() { }
        public void SetCursorPos(int x, int y) { }
        public void DisableScreensaver() { }
        public void EnableScreensaver() { }
        public List<FileInfo> GetFiles(FileInfo fileInfo) => new();
        public int CompareStrings(string str1, string str2) => string.CompareOrdinal(str1, str2);
        public void OpenWith(string path) { }
        public void LocateOnDisk(string path) { }
        public void ShowFileProperties(string path) { }
        public ValueTask Print(string path) => ValueTask.CompletedTask;
        public Task SetAsWallpaper(string path, int wallpaperStyle) => Task.CompletedTask;
        public bool SetAsLockScreen(string path) => false;
        public bool CopyFile(string path) => false;
        public Task CopyImageToClipboard(object bitmap) => Task.CompletedTask;
        public Task<object?> GetImageFromClipboard() => Task.FromResult<object?>(null);
        public Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory) => Task.FromResult(false);
        public string DefaultJsonKeyMap() => "{}";
        public void InitiateFileAssociationService() { }
        public Task<bool> DeleteFile(string path, bool recycle) => Task.FromResult(false);
        public byte[]? GetShellThumbnail(string path, int width, int height, out int pixelWidth, out int pixelHeight)
        {
            pixelWidth = 0;
            pixelHeight = 0;
            return null;
        }
        public byte[]? GetPixelsFromNativeImagingComponent(string path, out int pixelWidth, out int pixelHeight)
        {
            pixelWidth = 0;
            pixelHeight = 0;
            return null;
        }
    }

    [Fact]
    public async Task CopyTextAsync_WithValidText_SetsClipboardAndInvokesOnCopied()
    {
        var window = new Window();
        var clipboard = window.Clipboard;
        var copiedInvoked = false;
        var service = new ClipboardCopyService(clipboard, onCopied: () => copiedInvoked = true);

        var result = await service.CopyTextAsync("Hello World");

        Assert.True(result);
        Assert.True(copiedInvoked);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CopyTextAsync_WithNullOrWhitespace_ReturnsFalse(string? text)
    {
        var window = new Window();
        var clipboard = window.Clipboard;
        var copiedInvoked = false;
        var service = new ClipboardCopyService(clipboard, onCopied: () => copiedInvoked = true);

        var result = await service.CopyTextAsync(text);

        Assert.False(result);
        Assert.False(copiedInvoked);
    }

    [Fact]
    public async Task CopyImageAsync_WithNonBitmap_ReturnsFalse()
    {
        var window = new Window();
        var clipboard = window.Clipboard;
        var copiedInvoked = false;
        var service = new ClipboardCopyService(clipboard, onCopied: () => copiedInvoked = true);

        var result = await service.CopyImageAsync("Not a bitmap");

        Assert.False(result);
        Assert.False(copiedInvoked);
    }

    [Fact]
    public async Task CutFileAsync_WithValidPath_DelegatesToPlatformServiceAndInvokesOnCopied()
    {
        var platform = new TestPlatformService { CutFileResult = true };
        var copiedInvoked = false;
        var service = new ClipboardCopyService(clipboard: null, platformService: platform, onCopied: () => copiedInvoked = true);

        var result = await service.CutFileAsync("C:/test/file.png");

        Assert.True(result);
        Assert.Equal("C:/test/file.png", platform.LastCutFilePath);
        Assert.True(copiedInvoked);
    }

    [Fact]
    public async Task CutFileAsync_WhenPlatformServiceFails_ReturnsFalseAndDoesNotInvokeOnCopied()
    {
        var platform = new TestPlatformService { CutFileResult = false };
        var copiedInvoked = false;
        var service = new ClipboardCopyService(clipboard: null, platformService: platform, onCopied: () => copiedInvoked = true);

        var result = await service.CutFileAsync("C:/test/file.png");

        Assert.False(result);
        Assert.False(copiedInvoked);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CutFileAsync_WithNullOrEmpty_ReturnsFalse(string? path)
    {
        var platform = new TestPlatformService();
        var service = new ClipboardCopyService(clipboard: null, platformService: platform);

        var result = await service.CutFileAsync(path);

        Assert.False(result);
        Assert.Null(platform.LastCutFilePath);
    }
}
