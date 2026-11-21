#if WINDOWS
using System.Runtime.InteropServices;
using ImageMagick;
using PicView.Core.WindowsNT.Imaging;

namespace PicView.Tests.Thumbnailing;

/// <summary>
/// Pins that a cache-only Windows shell thumbnail comes only from the Windows thumbnail cache, at least as big as asked
/// for, while the other kind is still extracted, and that a file is found by its
/// name in its folder: the folder's shell item is kept between files, so the right file must still come back when
/// folders alternate and names repeat.
/// </summary>
public sealed class ShellThumbnailNativeTests : IDisposable
{
    private const int Height = 200;

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "PicViewShellThumbnailTests_" + Guid.NewGuid());

    public ShellThumbnailNativeTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // The shell may still hold a file it thumbnailed; the system cleans the temporary folder
        }
    }

    [Fact]
    public void GetShellThumbnailBytes_FileWindowsHasNotThumbnailed_GivesNull()
    {
        var path = WriteJpeg("fresh.jpg", 600, 400);

        Assert.Null(ShellThumbnailNative.GetShellThumbnailBytes(path, 0, Height, cacheOnly: true, out _, out _));
    }

    [Fact]
    public void GetShellThumbnailBytes_FileInTheThumbnailCache_GivesThatThumbnail()
    {
        var path = WriteJpeg("cached.jpg", 600, 400);
        ShellExtracts(path);

        var pixels = ShellThumbnailNative.GetShellThumbnailBytes(path, 0, Height, cacheOnly: true, out var width, out var height);

        Assert.NotNull(pixels);
        Assert.True(width > height, $"{width} x {height}");
    }

    [Fact]
    public void GetShellThumbnailBytes_SameNameInFoldersAskedInTurn_GivesEachFoldersFile()
    {
        var wide = WriteJpeg(Path.Combine("a", "photo.jpg"), 600, 300);
        var tall = WriteJpeg(Path.Combine("b", "photo.jpg"), 300, 600);
        ShellExtracts(wide);
        ShellExtracts(tall);

        foreach (var (path, isWide) in new[] { (wide, true), (tall, false), (wide, true), (tall, false) })
        {
            Assert.NotNull(ShellThumbnailNative.GetShellThumbnailBytes(path, 0, Height, cacheOnly: true, out var width, out var height));
            Assert.Equal(isWide, width > height);
        }
    }

    [Fact]
    public void GetShellThumbnailBytes_FilesOfOneFolder_GivesEachItsOwn()
    {
        var files = Enumerable.Range(0, 6)
            .Select(i => (Path: WriteJpeg($"{i}.jpg", i % 2 is 0 ? 600u : 300u, i % 2 is 0 ? 300u : 600u),
                IsWide: i % 2 is 0))
            .ToList();
        files.ForEach(file => ShellExtracts(file.Path));

        foreach (var (path, isWide) in files)
        {
            Assert.NotNull(ShellThumbnailNative.GetShellThumbnailBytes(path, 0, Height, cacheOnly: true, out var width, out var height));
            Assert.Equal(isWide, width > height);
        }
    }

    [Fact]
    public void GetShellThumbnailBytes_NotCacheOnly_ExtractsAFileWindowsHasNotThumbnailed()
    {
        // The quick stand-in while an image loads has nothing to decode the file with in place of the shell
        var path = WriteJpeg("fresh.jpg", 600, 400);

        var pixels = ShellThumbnailNative.GetShellThumbnailBytes(path, 0, Height, cacheOnly: false, out var width,
            out var height);

        Assert.NotNull(pixels);
        Assert.True(Math.Max(width, height) >= Height, $"{width} x {height}");
    }

    [Fact]
    public void GetShellThumbnailBytes_CachedOnlySmallerThanAskedFor_GivesNullWhereAnExtractionIsBigEnough()
    {
        // As after Explorer showed the folder with medium icons: only the 96 pixel thumbnail is cached
        var path = WriteJpeg("small.jpg", 600, 400);
        ShellExtracts(path, 96);

        Assert.Null(ShellThumbnailNative.GetShellThumbnailBytes(path, 0, Height, cacheOnly: true, out _, out _));
        Assert.NotNull(ShellThumbnailNative.GetShellThumbnailBytes(path, 0, Height, cacheOnly: false, out var width,
            out var height));
        Assert.True(Math.Max(width, height) >= Height, $"{width} x {height}");
    }

    [Fact]
    public void GetShellThumbnailBytes_MissingFile_GivesNull() =>
        Assert.Null(ShellThumbnailNative.GetShellThumbnailBytes(Path.Combine(_directory, "missing.jpg"), 0, Height,
            cacheOnly: true, out _, out _));

    private string WriteJpeg(string name, uint width, uint height)
    {
        var path = Path.Combine(_directory, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // A colour of its own, so the thumbnail cache cannot already hold an identical file from another run
        var shade = (byte)Random.Shared.Next(256);
        using var image = new MagickImage(new MagickColor(shade, (byte)(255 - shade), 128), width, height);
        image.Write(path, MagickFormat.Jpeg);
        return path;
    }

    #region The shell's own extraction, which writes the Windows thumbnail cache

    [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(NativeSize size, int flags, out IntPtr bitmap);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSize
    {
        public int Width;
        public int Height;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName(string path, IntPtr bindContext, in Guid interfaceId,
        out IntPtr item);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr gdiObject);

    private static void ShellExtracts(string path, int height = Height)
    {
        var interfaceId = typeof(IShellItemImageFactory).GUID;
        Assert.Equal(0, SHCreateItemFromParsingName(path, IntPtr.Zero, in interfaceId, out var item));
        try
        {
            var factory = (IShellItemImageFactory)Marshal.GetObjectForIUnknown(item);
            const int thumbnailOnly = 0x8, biggerSizeOk = 0x1;
            Assert.Equal(0, factory.GetImage(new NativeSize { Height = height }, thumbnailOnly | biggerSizeOk,
                out var bitmap));
            DeleteObject(bitmap);
            Marshal.ReleaseComObject(factory);
        }
        finally
        {
            Marshal.Release(item);
        }
    }

    #endregion
}
#endif
