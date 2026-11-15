using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageMagick;
using PicView.Avalonia.ImageHandling;

namespace PicView.Tests.Imaging;

public class GetImageTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"PicView.GetImageTests.{Guid.NewGuid():N}");

    public GetImageTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [AvaloniaTheory]
    [InlineData("png")]
    [InlineData("jpg")]
    [InlineData("webp")]
    public void GetSkBitmap_CommonFormat_MatchesAvaloniaStreamDecode(string extension)
    {
        var path = CreateGradientImage(extension, withAlpha: extension != "jpg");

        using var expected = new Bitmap(path);
        using var actual = GetImage.GetSkBitmap(new FileInfo(path));

        Assert.NotNull(actual);
        Assert.Equal(expected.PixelSize, actual.PixelSize);
        Assert.Equal(ReadPremulBgra(expected), ReadPremulBgra(actual));
    }

    [AvaloniaFact]
    public void GetSkBitmap_UnrecognizedFile_FallsBackToStreamDecodeAndThrows()
    {
        var path = Path.Combine(_directory, "not-an-image.png");
        File.WriteAllText(path, "not an image");

        Assert.ThrowsAny<Exception>(() => GetImage.GetSkBitmap(new FileInfo(path)));
    }

    [AvaloniaTheory]
    [InlineData(ColorType.TrueColorAlpha)]
    [InlineData(ColorType.TrueColor)]
    [InlineData(ColorType.Grayscale)]
    [InlineData(ColorType.GrayscaleAlpha)]
    public async Task GetNonStandardBitmapAsync_ColorType_MatchesMagickToWriteableBitmap(ColorType colorType)
    {
        var path = CreateGradientImage("png", withAlpha: true);
        using (var image = new MagickImage(path))
        {
            image.ColorType = colorType;
            path = Path.Combine(_directory, $"{colorType}.tif");
            image.Write(path, MagickFormat.Tiff);
        }

        using var magickImage = new MagickImage(path);
        using var expected = magickImage.ToWriteableBitmap();
        using var actual = await GetImage.GetNonStandardBitmapAsync(new FileInfo(path), new MagickImage());

        Assert.NotNull(actual);
        // ToWriteableBitmap produces Rgba8888, so Bgra8888 shows the direct framebuffer copy was used
        Assert.Equal(PixelFormat.Bgra8888, actual.Format);
        Assert.Equal(expected.PixelSize, actual.PixelSize);
        Assert.Equal(ReadPremulBgra(expected), ReadPremulBgra(actual));
    }

    [AvaloniaTheory]
    [InlineData("jpg", 150u)] // JPEG decodes 1/4 size straight into the target
    [InlineData("jpg", 100u)] // JPEG decodes 1/4 size, then resizes
    [InlineData("png", 100u)]
    [InlineData("png", 900u)] // Upscale
    public async Task GetThumbAsync_CommonFormat_MatchesDecodeToHeight(string extension, uint height)
    {
        var path = CreateGradientImage(extension, withAlpha: extension != "jpg", width: 800, height: 600);

        using var stream = File.OpenRead(path);
        using var expected = Bitmap.DecodeToHeight(stream, (int)height);
        using var actual = GetThumbnails.Decode(new FileInfo(path), height);

        Assert.NotNull(actual);
        Assert.Equal(expected.PixelSize, actual.PixelSize);
        Assert.Equal(ReadPremulBgra(expected), ReadPremulBgra(actual));
    }

    [AvaloniaFact]
    public async Task GetThumbAsync_MagickFormat_MatchesMagickToWriteableBitmap()
    {
        var path = CreateGradientImage("png", withAlpha: true, width: 800, height: 600);
        using (var image = new MagickImage(path))
        {
            path = Path.Combine(_directory, "gradient.qoi");
            image.Write(path, MagickFormat.Qoi);
        }

        using var magickImage = new MagickImage(path);
        magickImage.AutoOrient();
        magickImage.Thumbnail(new MagickGeometry(0, 100));
        using var expected = magickImage.ToWriteableBitmap();
        using var actual = GetThumbnails.Decode(new FileInfo(path), 100);

        Assert.NotNull(actual);
        Assert.Equal(PixelFormat.Bgra8888, actual.Format);
        Assert.Equal(expected.PixelSize, actual.PixelSize);
        Assert.Equal(ReadPremulBgra(expected), ReadPremulBgra(actual));
    }

    [AvaloniaTheory]
    [InlineData("tif", MagickFormat.Tiff)]
    [InlineData("TIF", MagickFormat.Tiff)]
    [InlineData("tga", MagickFormat.Tga)]
    public async Task GetThumbAsync_FormatSkiaCannotDecode_ReturnsThumbnail(string extension, MagickFormat format)
    {
        var path = CreateGradientImage("png", withAlpha: true, width: 800, height: 600);
        using (var image = new MagickImage(path))
        {
            path = Path.Combine(_directory, $"gradient.{extension}");
            image.Write(path, format);
        }

        using var actual = GetThumbnails.Decode(new FileInfo(path), 100);

        Assert.NotNull(actual);
        Assert.Equal(new PixelSize(133, 100), actual.PixelSize);
    }

    [AvaloniaTheory]
    [InlineData("svg")]
    [InlineData("svgz")]
    public async Task GetThumbAsync_Svg_ReturnsThumbnailAtHeight(string extension)
    {
        const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"100\">" +
                           "<rect x=\"10\" y=\"10\" width=\"80\" height=\"80\" fill=\"red\"/></svg>";
        var path = Path.Combine(_directory, $"shape.{extension}");
        if (extension == "svgz")
        {
            using var file = File.Create(path);
            using var gzip = new System.IO.Compression.GZipStream(file, System.IO.Compression.CompressionLevel.Fastest);
            gzip.Write(System.Text.Encoding.UTF8.GetBytes(svg));
        }
        else
        {
            File.WriteAllText(path, svg);
        }

        using var actual = GetThumbnails.Decode(new FileInfo(path), 100);

        Assert.NotNull(actual);
        Assert.Equal(new PixelSize(200, 100), actual.PixelSize);
    }

    [AvaloniaTheory]
    [InlineData(100u)] // 128 is the closest stored size
    [InlineData(40u)] // 48 is the closest stored size
    public async Task GetThumbAsync_IconWithDifferentPicturePerSize_ShowsTheFrameTheViewerShows(uint height)
    {
        // Each size has its own color, so a thumbnail made from another size than the main image shows it
        var path = CreateIcon((256, MagickColors.White), (128, MagickColors.Black), (48, MagickColors.Red));

        using var viewed = GetImage.GetSkBitmap(new FileInfo(path));
        using var actual = GetThumbnails.Decode(new FileInfo(path), height);

        Assert.NotNull(viewed);
        Assert.Equal(new PixelSize(256, 256), viewed.PixelSize);
        Assert.NotNull(actual);
        Assert.Equal(new PixelSize((int)height, (int)height), actual.PixelSize);
        var pixels = ReadPremulBgra(actual);
        var center = (actual.PixelSize.Height / 2 * actual.PixelSize.Width + actual.PixelSize.Width / 2) * 4;
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, pixels[center..(center + 4)]);
    }

    /// <summary>
    /// Writes an .ico file that stores each size as a PNG filled with its own color.
    /// </summary>
    private string CreateIcon(params (int Size, MagickColor Color)[] frames)
    {
        var images = frames.Select(f =>
        {
            using var image = new MagickImage(f.Color, (uint)f.Size, (uint)f.Size);
            return image.ToByteArray(MagickFormat.Png32);
        }).ToArray();

        var path = Path.Combine(_directory, "sizes.ico");
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write((ushort)0); // Reserved
        writer.Write((ushort)1); // Icon
        writer.Write((ushort)frames.Length);
        var offset = 6 + 16 * frames.Length;
        for (var i = 0; i < frames.Length; i++)
        {
            writer.Write((byte)(frames[i].Size % 256)); // 0 means 256
            writer.Write((byte)(frames[i].Size % 256));
            writer.Write((byte)0); // Palette colors
            writer.Write((byte)0); // Reserved
            writer.Write((ushort)1); // Planes
            writer.Write((ushort)32); // Bits per pixel
            writer.Write(images[i].Length);
            writer.Write(offset);
            offset += images[i].Length;
        }

        foreach (var image in images)
        {
            writer.Write(image);
        }

        return path;
    }

    private string CreateGradientImage(string extension, bool withAlpha, int width = 61, int height = 37)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4;
                pixels[i] = (byte)(x * 4);
                pixels[i + 1] = (byte)(y * 6);
                pixels[i + 2] = (byte)((x + y) * 2);
                pixels[i + 3] = withAlpha ? (byte)((x * 7 + y * 3) % 256) : byte.MaxValue;
            }
        }

        using var image = new MagickImage();
        image.ReadPixels(pixels, new PixelReadSettings((uint)width, (uint)height, StorageType.Char, PixelMapping.RGBA));
        if (extension == "webp")
        {
            image.Settings.SetDefine(MagickFormat.WebP, "lossless", true);
        }

        var path = Path.Combine(_directory, $"gradient.{extension}");
        image.Write(path);
        return path;
    }

    /// <summary>
    /// Reads any bitmap as premultiplied BGRA, so bitmaps stored in different formats can be compared.
    /// </summary>
    private static byte[] ReadPremulBgra(Bitmap bitmap)
    {
        var size = bitmap.PixelSize;
        var stride = size.Width * 4;
        var pixels = new byte[stride * size.Height];
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(new PixelRect(size), handle.AddrOfPinnedObject(), pixels.Length, stride);
        }
        finally
        {
            handle.Free();
        }

        var isRgba = bitmap.Format == PixelFormat.Rgba8888;
        Assert.True(isRgba || bitmap.Format == PixelFormat.Bgra8888, $"Unexpected format {bitmap.Format}");
        var isUnpremul = bitmap.AlphaFormat == AlphaFormat.Unpremul;

        for (var i = 0; i < pixels.Length; i += 4)
        {
            if (isRgba)
            {
                (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
            }

            if (isUnpremul)
            {
                var a = pixels[i + 3];
                pixels[i] = (byte)((pixels[i] * a + 127) / 255);
                pixels[i + 1] = (byte)((pixels[i + 1] * a + 127) / 255);
                pixels[i + 2] = (byte)((pixels[i + 2] * a + 127) / 255);
            }
        }

        return pixels;
    }

    private static (byte R, byte G, byte B) Red => (255, 0, 0);
    private static (byte R, byte G, byte B) Blue => (0, 0, 255);
    private static (byte R, byte G, byte B) Green => (0, 255, 0);
    private static (byte R, byte G, byte B) White => (255, 255, 255);

    /// <summary>
    /// A picture whose left half is one color and whose right half is another, so where the colors end up shows how
    /// it was turned.
    /// </summary>
    private static MagickImage CreateSplitImage(int width, int height, (byte R, byte G, byte B) left,
        (byte R, byte G, byte B) right)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var (r, g, b) = x < width / 2 ? left : right;
                var i = (y * width + x) * 4;
                pixels[i] = r;
                pixels[i + 1] = g;
                pixels[i + 2] = b;
                pixels[i + 3] = byte.MaxValue;
            }
        }

        var image = new MagickImage();
        image.ReadPixels(pixels, new PixelReadSettings((uint)width, (uint)height, StorageType.Char, PixelMapping.RGBA));
        return image;
    }

    /// <summary>
    /// Writes a JPEG stored 800 wide and 600 tall, red on the left and blue on the right, that says how to turn it.
    /// </summary>
    private string WriteSplitJpeg(string name, Orientation orientation, ExifProfile exif)
    {
        using var image = CreateSplitImage(800, 600, Red, Blue);
        // Magick writes the orientation tag only with both an EXIF profile and the property set
        image.SetProfile(exif);
        image.Orientation = orientation;
        var path = Path.Combine(_directory, name);
        image.Write(path, MagickFormat.Jpeg);
        return path;
    }

    private static ExifProfile OrientationProfile(int exifOrientation)
    {
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Orientation, (ushort)exifOrientation);
        return exif;
    }

    /// <summary>
    /// The split JPEG, turned a quarter clockwise to show, with an embedded EXIF thumbnail stored 200 wide and 150 tall,
    /// green on the left and white on the right. Magick.NET cannot write an embedded thumbnail, so the EXIF block is
    /// written by hand: IFD0 holds the orientation, IFD1 says where the thumbnail's JPEG bytes are.
    /// </summary>
    private string WriteTurnedJpegWithExifThumbnail()
    {
        using var embedded = CreateSplitImage(200, 150, Green, White);
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
        writer.Write(6u); // Rotate 90 CW
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

        return WriteSplitJpeg("with-thumbnail.jpg", Orientation.RightTop, new ExifProfile(stream.ToArray()));
    }

    /// <summary>
    /// Checks the pixel at the given fractions of the bitmap's width and height, loosely, as JPEG shifts colors a little.
    /// </summary>
    private static void AssertColor(Bitmap bitmap, double x, double y, (byte R, byte G, byte B) expected)
    {
        var pixels = ReadPremulBgra(bitmap);
        var size = bitmap.PixelSize;
        var i = ((int)(y * size.Height) * size.Width + (int)(x * size.Width)) * 4;
        var (b, g, r) = (pixels[i], pixels[i + 1], pixels[i + 2]);
        Assert.True(Math.Abs(r - expected.R) < 60 && Math.Abs(g - expected.G) < 60 && Math.Abs(b - expected.B) < 60,
            $"Expected {expected} at ({x}, {y}) but found R{r} G{g} B{b}");
    }

    [AvaloniaTheory]
    // Turned a quarter clockwise to show: the stored left side is the top
    [InlineData(6, Orientation.RightTop, 75, 100, 0.5, 0.25, 0.5, 0.75)]
    // Turned a quarter counterclockwise: the stored left side is the bottom
    [InlineData(8, Orientation.LeftBottom, 75, 100, 0.5, 0.75, 0.5, 0.25)]
    // Turned upside down: the stored left side is the right
    [InlineData(3, Orientation.BottomRight, 133, 100, 0.75, 0.5, 0.25, 0.5)]
    public void Decode_JpegWithExifOrientation_GivesItUpright(int exifOrientation, Orientation orientation,
        int width, int height, double redX, double redY, double blueX, double blueY)
    {
        var path = WriteSplitJpeg($"turned{exifOrientation}.jpg", orientation, OrientationProfile(exifOrientation));

        using var actual = GetThumbnails.Decode(new FileInfo(path), 100);

        Assert.NotNull(actual);
        Assert.Equal(new PixelSize(width, height), actual.PixelSize);
        AssertColor(actual, redX, redY, Red);
        AssertColor(actual, blueX, blueY, Blue);
    }

    [AvaloniaFact]
    public void Decode_JpegThatSaysItIsUpright_MatchesDecodeToHeight()
    {
        // Most camera files carry orientation 1. They must stay on the Skia decode, pinned here byte for byte
        var path = WriteSplitJpeg("upright.jpg", Orientation.TopLeft, OrientationProfile(1));

        using var stream = File.OpenRead(path);
        using var expected = Bitmap.DecodeToHeight(stream, 100);
        using var actual = GetThumbnails.Decode(new FileInfo(path), 100);

        Assert.NotNull(actual);
        Assert.Equal(expected.PixelSize, actual.PixelSize);
        Assert.Equal(ReadPremulBgra(expected), ReadPremulBgra(actual));
    }

    [AvaloniaFact]
    public void Decode_TurnedJpegWhoseExifThumbnailIsTallEnoughUpright_GivesThatThumbnailUpright()
    {
        // Upright the embedded thumbnail is 200 tall: enough for 180, which its stored height of 150 is not
        var path = WriteTurnedJpegWithExifThumbnail();

        using var actual = GetThumbnails.Decode(new FileInfo(path), 180);

        Assert.NotNull(actual);
        Assert.Equal(new PixelSize(150, 200), actual.PixelSize);
        AssertColor(actual, 0.5, 0.25, Green);
        AssertColor(actual, 0.5, 0.75, White);
    }

    [AvaloniaFact]
    public void Decode_TurnedJpegWhoseExifThumbnailIsTooShortUpright_DecodesTheFileUpright()
    {
        var path = WriteTurnedJpegWithExifThumbnail();

        using var actual = GetThumbnails.Decode(new FileInfo(path), 220);

        Assert.NotNull(actual);
        Assert.Equal(new PixelSize(165, 220), actual.PixelSize);
        AssertColor(actual, 0.5, 0.25, Red);
        AssertColor(actual, 0.5, 0.75, Blue);
    }

    [AvaloniaFact]
    public void GetReadyMade_TurnedJpegWithExifThumbnail_GivesTheThumbnailUpright()
    {
        var path = WriteTurnedJpegWithExifThumbnail();

        using var actual = GetThumbnails.GetReadyMade(new FileInfo(path), 100);

        Assert.NotNull(actual);
        Assert.Equal(new PixelSize(150, 200), actual.PixelSize);
        AssertColor(actual, 0.5, 0.25, Green);
        AssertColor(actual, 0.5, 0.75, White);
    }
}
