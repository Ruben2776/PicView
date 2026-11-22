using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageMagick;
using PicView.Avalonia.ImageHandling;
using PicView.Core.FileSorting;
using PicView.Core.IPlatform;

namespace PicView.Tests.Thumbnailing;

public class AvaloniaThumbnailDecoderTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), $"PicView.AvaloniaThumbnailDecoderTests.{Guid.NewGuid():N}");

    private readonly CountingShellPlatform _platform = new();
    private readonly AvaloniaThumbnailDecoder _decoder;
    private readonly FileInfo _jpeg;

    public AvaloniaThumbnailDecoderTests()
    {
        _decoder = new AvaloniaThumbnailDecoder(_platform);
        Directory.CreateDirectory(_directory);

        // 800 x 600, carrying a 160 x 120 EXIF thumbnail
        var path = Path.Combine(_directory, "photo.jpg");
        using (var image = new MagickImage(MagickColors.SteelBlue, 800, 600))
        {
            image.SetProfile(new ExifProfile(ExifWithThumbnail(160, 120)));
            image.Write(path, MagickFormat.Jpeg);
        }

        _jpeg = new FileInfo(path);
    }

    /// <summary>
    /// An EXIF block with an embedded thumbnail, which Magick.NET cannot write: IFD0 holds the orientation, IFD1 says
    /// where the thumbnail's JPEG bytes are.
    /// </summary>
    private static byte[] ExifWithThumbnail(uint width, uint height)
    {
        using var embedded = new MagickImage(MagickColors.Red, width, height);
        var thumbnail = embedded.ToByteArray(MagickFormat.Jpeg);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("Exif\0\0"u8);
        writer.Write("II"u8); // Little-endian TIFF header; the offsets below count from here
        writer.Write((ushort)42);
        writer.Write(8u); // IFD0 starts at 8
        writer.Write((ushort)1); // One entry
        writer.Write((ushort)0x0112); // Orientation
        writer.Write((ushort)3); // SHORT
        writer.Write(1u);
        writer.Write(1u); // Upright
        writer.Write(26u); // IFD1 starts at 26
        writer.Write((ushort)2); // Two entries
        writer.Write((ushort)0x0201); // JPEGInterchangeFormat: where the thumbnail starts
        writer.Write((ushort)4); // LONG
        writer.Write(1u);
        writer.Write(56u);
        writer.Write((ushort)0x0202); // JPEGInterchangeFormatLength
        writer.Write((ushort)4);
        writer.Write(1u);
        writer.Write((uint)thumbnail.Length);
        writer.Write(0u); // No further IFD
        writer.Write(thumbnail);
        writer.Flush();
        return stream.ToArray();
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [AvaloniaFact]
    public void DecodeWithoutReadyMade_CommonFormat_IsDecodedAtTheHeightAskedFor()
    {
        var bitmap = Assert.IsAssignableFrom<Bitmap>(_decoder.DecodeWithoutReadyMade(_jpeg, 300));
        Assert.Equal(300, bitmap.PixelSize.Height);
    }

    [AvaloniaFact]
    public void DecodeWithoutReadyMade_NeverAsksTheShell()
    {
        // _platform counts the shell thumbnails asked for and would return a 10 x 10 bitmap
        _decoder.DecodeWithoutReadyMade(_jpeg, 300);
        Assert.Equal(0, _platform.ShellThumbnailCalls);
        Assert.Equal(0, _platform.CachedShellThumbnailCalls);
    }

    [AvaloniaFact]
    public void DecodeWithoutReadyMade_FileWithAnExifThumbnail_IgnoresIt()
    {
        // The EXIF thumbnail is 120 tall, tall enough for 100: Decode would give it
        using var exifThumb = GetThumbnails.Decode(_jpeg, 100);
        Assert.Equal(120, Assert.IsAssignableFrom<Bitmap>(exifThumb).PixelSize.Height);

        var bitmap = Assert.IsAssignableFrom<Bitmap>(_decoder.DecodeWithoutReadyMade(_jpeg, 100));
        Assert.Equal(100, bitmap.PixelSize.Height);
        Assert.Equal(133, bitmap.PixelSize.Width);
    }

    [AvaloniaFact]
    public void Decode_CommonFormat_AsksTheShellFirstForOneItHasAlready()
    {
        // Pins what DecodeWithoutReadyMade is not: Decode keeps going to the shell on Windows and macOS, but only for a
        // thumbnail it has made already, as Decode can decode the file itself
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS())
        {
            return;
        }

        var bitmap = Assert.IsAssignableFrom<Bitmap>(_decoder.Decode(_jpeg, 300));
        Assert.Equal(1, _platform.CachedShellThumbnailCalls);
        Assert.Equal(0, _platform.ShellThumbnailCalls);
        Assert.Equal(new PixelSize(10, 10), bitmap.PixelSize);
    }

    [AvaloniaFact]
    public void GetReadyMade_CommonFormat_LetsTheShellMakeOne()
    {
        // The quick stand-in decodes nothing itself, so the shell may extract a thumbnail it has not made yet
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS())
        {
            return;
        }

        var bitmap = Assert.IsAssignableFrom<Bitmap>(_decoder.GetReadyMade(_jpeg, 300));
        Assert.Equal(1, _platform.ShellThumbnailCalls);
        Assert.Equal(0, _platform.CachedShellThumbnailCalls);
        Assert.Equal(new PixelSize(10, 10), bitmap.PixelSize);
    }

    [AvaloniaFact]
    public void SizeInBytes_Bitmap_IsItsPixelsTimesFour()
    {
        var bitmap = Assert.IsAssignableFrom<Bitmap>(_decoder.DecodeWithoutReadyMade(_jpeg, 300));
        Assert.Equal(bitmap.PixelSize.Width * 300L * 4, _decoder.SizeInBytes(bitmap));
    }

    [Fact]
    public void SizeInBytes_NotABitmap_IsZero() => Assert.Equal(0, new AvaloniaThumbnailDecoder(null).SizeInBytes("x"));

    // Counts the shell thumbnails asked for, of each kind, and gives a 10 x 10 bitmap for each
    private sealed class CountingShellPlatform : IPlatformSpecificService
    {
        private int _shellThumbnailCalls;
        private int _cachedShellThumbnailCalls;

        public int ShellThumbnailCalls => Volatile.Read(ref _shellThumbnailCalls);
        public int CachedShellThumbnailCalls => Volatile.Read(ref _cachedShellThumbnailCalls);

        public byte[]? GetShellThumbnail(string path, int width, int height, out int pixelWidth, out int pixelHeight)
        {
            Interlocked.Increment(ref _shellThumbnailCalls);
            pixelWidth = pixelHeight = 10;
            return new byte[10 * 10 * 4];
        }

        public byte[]? GetCachedShellThumbnail(string path, int width, int height, out int pixelWidth,
            out int pixelHeight)
        {
            Interlocked.Increment(ref _cachedShellThumbnailCalls);
            pixelWidth = pixelHeight = 10;
            return new byte[10 * 10 * 4];
        }

        public void SetTaskbarProgress(ulong progress, ulong maximum) { }
        public void StopTaskbarProgress() { }
        public void SetCursorPos(int x, int y) { }
        public List<FileInfo> GetFiles(FileInfo fileInfo, FileOrder order) => [];
        public int CompareStrings(string str1, string str2) => string.CompareOrdinal(str1, str2);
        public void OpenWith(string path) { }
        public void LocateOnDisk(string path) { }
        public void ShowFileProperties(string path) { }
        public Task SetAsWallpaper(string path, int wallpaperStyle) => Task.CompletedTask;
        public Task<bool> SetAsLockScreen(string path) => Task.FromResult(false);
        public Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory) => Task.FromResult(false);
        public string DefaultJsonKeyMap() => "{}";
        public void InitiateFileAssociationService() { }
        public Task<bool> DeleteFile(string path, bool recycle) => Task.FromResult(false);
        public bool CanMoveToRecycleBin(string path) => false;
        public Task<bool> CutFile(string path) => Task.FromResult(false);

        public byte[]? GetPixelsFromNativeImagingComponent(string path, out int pixelWidth, out int pixelHeight)
        {
            pixelWidth = pixelHeight = 0;
            return null;
        }
    }

    private static WriteableBitmap Image(int width, int height) =>
        new(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

    [AvaloniaFact]
    public async Task ScaleDown_OffTheUiThread_GivesANewBitmapAtTheHeight()
    {
        using var image = Image(150, 200);

        var scaled = await Task.Run(() => _decoder.ScaleDown(image, 100), TestContext.Current.CancellationToken);

        var bitmap = Assert.IsAssignableFrom<Bitmap>(scaled);
        Assert.NotSame(image, bitmap);
        Assert.Equal(new PixelSize(75, 100), bitmap.PixelSize);
    }

    [AvaloniaFact]
    public async Task ScaleDown_ImageShorterThanTheHeight_KeepsItsSizeInANewBitmap()
    {
        using var image = Image(30, 40);

        var scaled = await Task.Run(() => _decoder.ScaleDown(image, 100), TestContext.Current.CancellationToken);

        var bitmap = Assert.IsAssignableFrom<Bitmap>(scaled);
        Assert.NotSame(image, bitmap);
        Assert.Equal(new PixelSize(30, 40), bitmap.PixelSize);
    }

    [AvaloniaFact]
    public async Task ScaleDown_SolidColor_KeepsTheColor()
    {
        using var image = Image(40, 40);
        using (var framebuffer = image.Lock())
        {
            // Opaque red, in BGRA order
            var pixels = Enumerable.Repeat(new byte[] { 0, 0, 255, 255 }, 40 * 40).SelectMany(p => p).ToArray();
            Marshal.Copy(pixels, 0, framebuffer.Address, pixels.Length);
        }

        var scaled = Assert.IsAssignableFrom<Bitmap>(
            await Task.Run(() => _decoder.ScaleDown(image, 10), TestContext.Current.CancellationToken));

        var center = new byte[4];
        var handle = GCHandle.Alloc(center, GCHandleType.Pinned);
        try
        {
            scaled.CopyPixels(new PixelRect(5, 5, 1, 1), handle.AddrOfPinnedObject(), 4, 4);
        }
        finally
        {
            handle.Free();
        }

        Assert.Equal(new byte[] { 0, 0, 255, 255 }, center);
    }

    [Fact]
    public void ScaleDown_NotABitmap_GivesNothing()
    {
        Assert.Null(_decoder.ScaleDown(new object(), 100));
    }
}
