using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Avalonia.Clipboard;
using PicView.Core.IPlatform;
using PicView.Core.ViewModels;
using Xunit;

namespace PicView.Tests.Clipboard;

[Collection("Sequential")]
public class ClipboardCopyServiceTests
{
    public ClipboardCopyServiceTests()
    {
        SetDefaults();
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
        public Task SetAsWallpaper(string path, int wallpaperStyle) => Task.CompletedTask;
        public bool SetAsLockScreen(string path) => false;
        public bool CopyFile(string path) => false;
        public Task CopyImageToClipboard(object bitmap) => Task.CompletedTask;
        public Task<object?> GetImageFromClipboard() => Task.FromResult<object?>(null);
        public Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory) => Task.FromResult(false);
        public string DefaultJsonKeyMap() => "{}";
        public void InitiateFileAssociationService() { }
        public Task<bool> DeleteFile(string path, bool recycle) => Task.FromResult(false);
        public bool CanMoveToRecycleBin(string path) => true;
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

    [AvaloniaFact]
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

    [AvaloniaTheory]
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

    [AvaloniaFact]
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

    private const int Black = unchecked((int)0xFF000000);
    private const int Red = unchecked((int)0xFFFF0000);

    /// <summary>
    /// A tab showing <paramref name="image"/> upright and unflipped.
    /// </summary>
    private static TabViewModel CreateTab(Bitmap? image)
    {
        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(vm);
        tab.Image.Value = image;
        return tab;
    }

    private static WriteableBitmap CreateBitmap(int width, int height) =>
        new(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

    /// <summary>
    /// A black bitmap 3 pixels wide and 2 high with its top-left pixel red, so where that pixel ends up shows how it
    /// was turned and flipped.
    /// </summary>
    private static WriteableBitmap CreateMarkedBitmap()
    {
        const int width = 3;
        const int height = 2;
        var bitmap = CreateBitmap(width, height);
        var pixels = new int[width * height];
        Array.Fill(pixels, Black);
        pixels[0] = Red;
        using var buffer = bitmap.Lock();
        for (var y = 0; y < height; y++)
        {
            Marshal.Copy(pixels, y * width, buffer.Address + y * buffer.RowBytes, width);
        }

        return bitmap;
    }

    /// <summary>
    /// Every pixel of <paramref name="bitmap"/> as premultiplied BGRA, row by row.
    /// </summary>
    private static int[] ReadPixels(Bitmap bitmap)
    {
        var (width, height) = (bitmap.PixelSize.Width, bitmap.PixelSize.Height);
        using var copy = CreateBitmap(width, height);
        using var buffer = copy.Lock();
        bitmap.CopyPixels(buffer);
        var pixels = new int[width * height];
        for (var y = 0; y < height; y++)
        {
            Marshal.Copy(buffer.Address + y * buffer.RowBytes, pixels, y * width, width);
        }

        return pixels;
    }

    [AvaloniaFact]
    public async Task CopyImageAsync_TabWithoutBitmap_ReturnsFalse()
    {
        var window = new Window();
        var copiedInvoked = false;
        var service = new ClipboardCopyService(window.Clipboard, onCopied: () => copiedInvoked = true);
        var tab = CreateTab(null);
        tab.Orientation.TurnTo(1);

        var result = await service.CopyImageAsync(tab);

        Assert.False(result);
        Assert.False(copiedInvoked);
    }

    [AvaloniaTheory]
    [InlineData(0, 3, 2, 0, 0)]
    [InlineData(1, 2, 3, 1, 0)]
    [InlineData(2, 3, 2, 2, 1)]
    [InlineData(3, 2, 3, 0, 2)]
    public async Task CopyImageAsync_TurnedTab_CopiesItTurnedClockwise(int quarterTurns, int width, int height,
        int markedX, int markedY)
    {
        var window = new Window();
        var service = new ClipboardCopyService(window.Clipboard);
        var tab = CreateTab(CreateMarkedBitmap());
        tab.Orientation.TurnTo(quarterTurns);

        var result = await service.CopyImageAsync(tab);

        Assert.True(result);
        using var copied = await window.Clipboard!.TryGetBitmapAsync();
        Assert.NotNull(copied);
        Assert.Equal(new PixelSize(width, height), copied.PixelSize);
        var pixels = ReadPixels(copied);
        Assert.Equal(markedY * width + markedX, Array.IndexOf(pixels, Red));
        Assert.Equal(width * height - 1, pixels.Count(p => p == Black));
    }

    [AvaloniaTheory]
    [InlineData(0, 3, 2, 2, 0)]
    [InlineData(1, 2, 3, 0, 0)]
    [InlineData(2, 3, 2, 0, 1)]
    [InlineData(3, 2, 3, 1, 2)]
    public async Task CopyImageAsync_FlippedTab_CopiesItTurnedThenMirrored(int quarterTurns, int width, int height,
        int markedX, int markedY)
    {
        var window = new Window();
        var service = new ClipboardCopyService(window.Clipboard);
        var tab = CreateTab(CreateMarkedBitmap());
        tab.Orientation.TurnTo(quarterTurns);
        tab.Orientation.Flip();

        var result = await service.CopyImageAsync(tab);

        Assert.True(result);
        using var copied = await window.Clipboard!.TryGetBitmapAsync();
        Assert.NotNull(copied);
        Assert.Equal(new PixelSize(width, height), copied.PixelSize);
        var pixels = ReadPixels(copied);
        Assert.Equal(markedY * width + markedX, Array.IndexOf(pixels, Red));
        Assert.Equal(width * height - 1, pixels.Count(p => p == Black));
    }

    [AvaloniaFact]
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

    [AvaloniaFact]
    public async Task CutFileAsync_WhenPlatformServiceFails_ReturnsFalseAndDoesNotInvokeOnCopied()
    {
        var platform = new TestPlatformService { CutFileResult = false };
        var copiedInvoked = false;
        var service = new ClipboardCopyService(clipboard: null, platformService: platform, onCopied: () => copiedInvoked = true);

        var result = await service.CutFileAsync("C:/test/file.png");

        Assert.False(result);
        Assert.False(copiedInvoked);
    }

    [AvaloniaTheory]
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
