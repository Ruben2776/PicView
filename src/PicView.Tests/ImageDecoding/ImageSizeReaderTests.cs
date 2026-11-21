using System.Buffers.Binary;
using System.Text;
using ImageMagick;
using PicView.Core.Exif;
using PicView.Core.ImageDecoding;

namespace PicView.Tests.ImageDecoding;

/// <summary>
/// Pins that <see cref="ImageSizeReader"/> reads an image's pixel size from its header where it knows the format,
/// agrees with the ImageMagick ping, leaves the rest to that ping, and returns an empty size instead of throwing.
/// </summary>
public class ImageSizeReaderTests : IDisposable
{
    private const uint Width = 37;
    private const uint Height = 23;

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "PicViewImageSizeReaderTests_" + Guid.NewGuid());

    public ImageSizeReaderTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Theory]
    [InlineData(MagickFormat.Ico, "image.ico")]
    [InlineData(MagickFormat.Wbmp, "image.wbmp")]
    [InlineData(MagickFormat.Pcx, "image.pcx")]
    [InlineData(MagickFormat.Hdr, "image.hdr")]
    public void Read_OtherFormatsWithoutParser_ReturnSizeThroughFallback(MagickFormat format, string fileName)
    {
        // With and without a signature: the ones without are pinged by path, the rest as a buffered stream
        var path = Write(format, fileName);

        Assert.False(ImageSizeReader.TryReadHeader(path, out _));
        Assert.Equal(new ImageSize(Width, Height), ImageSizeReader.Read(path));
    }

    [Fact]
    public void Read_FormatWithoutParser_ReturnsSizeThroughFallback()
    {
        var path = Write(MagickFormat.Tga, "image.tga");

        Assert.False(ImageSizeReader.TryReadHeader(path, out _));
        Assert.Equal(new ImageSize(Width, Height), ImageSizeReader.Read(path));
        Assert.Equal(new ImageSize(Width, Height), ImageSizeReader.Read(new FileInfo(path)));
    }

    [Fact]
    public void Read_MissingFile_ReturnsEmpty() =>
        Assert.Equal(default, ImageSizeReader.Read(Path.Combine(_directory, "missing.png")));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Read_NoPath_ReturnsEmpty(string? path) => Assert.Equal(default, ImageSizeReader.Read(path));

    [Fact]
    public void Read_NullFileInfo_ReturnsEmpty() => Assert.Equal(default, ImageSizeReader.Read((FileInfo?)null));

    [Fact]
    public void Read_Directory_ReturnsEmpty() => Assert.Equal(default, ImageSizeReader.Read(_directory));

    [Fact]
    public void Read_EmptyFile_ReturnsEmpty()
    {
        var path = Path.Combine(_directory, "empty.png");
        File.WriteAllBytes(path, []);

        Assert.Equal(default, ImageSizeReader.Read(path));
    }

    [Fact]
    public void Read_TextFile_ReturnsEmpty()
    {
        var path = Path.Combine(_directory, "notes.jpg");
        File.WriteAllText(path, "not an image");

        Assert.Equal(default, ImageSizeReader.Read(path));
    }

    [Theory]
    [InlineData("image.b64")]
    [InlineData("image.livp")]
    [InlineData("IMAGE.B64")]
    public void Read_ContainerExtension_ReturnsEmptyWithoutReading(string fileName)
    {
        var path = Write(MagickFormat.Png, fileName);

        Assert.Equal(default, ImageSizeReader.Read(path));
    }

    [Fact]
    public void Read_FileLockedExclusively_ReturnsEmpty()
    {
        var path = Write(MagickFormat.Png, "locked.png");
        using var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);

        Assert.Equal(default, ImageSizeReader.Read(path));
    }

    [Theory]
    [InlineData(MagickFormat.Png, "image.png")]
    [InlineData(MagickFormat.Gif, "image.gif")]
    [InlineData(MagickFormat.Bmp, "image.bmp")]
    [InlineData(MagickFormat.Bmp3, "image3.bmp")]
    [InlineData(MagickFormat.Bmp2, "image2.bmp")]
    [InlineData(MagickFormat.Psd, "image.psd")]
    [InlineData(MagickFormat.Psb, "image.psb")]
    [InlineData(MagickFormat.Qoi, "image.qoi")]
    [InlineData(MagickFormat.Dds, "image.dds")]
    public void TryReadHeader_FixedHeaderFormat_AgreesWithTheMagickPing(MagickFormat format, string fileName)
    {
        var path = Write(format, fileName);

        AssertReadsFromTheHeaderLikeThePing(path);
    }

    [Theory]
    [InlineData(MagickFormat.Jpeg, "turned.jpg", 1, Orientation.TopLeft)]
    [InlineData(MagickFormat.Jpeg, "turned.jpg", 3, Orientation.BottomRight)]
    [InlineData(MagickFormat.Jpeg, "turned.jpg", 5, Orientation.LeftTop)]
    [InlineData(MagickFormat.Jpeg, "turned.jpg", 6, Orientation.RightTop)]
    [InlineData(MagickFormat.Jpeg, "turned.jpg", 7, Orientation.RightBottom)]
    [InlineData(MagickFormat.Jpeg, "turned.jpg", 8, Orientation.LeftBottom)]
    [InlineData(MagickFormat.Tiff, "turned.tif", 1, Orientation.TopLeft)]
    [InlineData(MagickFormat.Tiff, "turned.tif", 6, Orientation.RightTop)]
    [InlineData(MagickFormat.Tiff, "turned.tif", 8, Orientation.LeftBottom)]
    public void TryReadUprightHeader_JpegOrTiffWithOrientation_ReadsFromTheHeaderLikeThePing(MagickFormat format,
        string fileName, int exifOrientation, Orientation orientation)
    {
        var path = Write(format, fileName, image => SetOrientation(image, exifOrientation, orientation));

        Assert.True(ImageSizeReader.TryReadUprightHeader(path, out var size));
        Assert.Equal(UprightByPing(path), size);
        // The stored size stays what the plain read gives
        Assert.True(ImageSizeReader.TryReadHeader(path, out var stored));
        Assert.Equal(new ImageSize(Width, Height), stored);
    }

    [Theory]
    [InlineData(MagickFormat.Png, "turned.png")]
    [InlineData(MagickFormat.WebP, "turned.webp")]
    public void TryReadUprightHeader_PngOrWebpWithExif_NeverDisagreesWithThePing(MagickFormat format, string fileName)
    {
        // Their EXIF data is not parsed here, so the header may only answer when it agrees with ImageMagick
        var path = Write(format, fileName, image => SetOrientation(image, 6, Orientation.RightTop));

        if (ImageSizeReader.TryReadUprightHeader(path, out var size))
        {
            Assert.Equal(UprightByPing(path), size);
        }
    }

    [Theory]
    [InlineData(MagickFormat.Png, "image.png")]
    [InlineData(MagickFormat.WebP, "image.webp")]
    [InlineData(MagickFormat.Gif, "image.gif")]
    [InlineData(MagickFormat.Bmp, "image.bmp")]
    [InlineData(MagickFormat.Jpeg, "image.jpg")]
    [InlineData(MagickFormat.Tiff, "image.tif")]
    public void TryReadUprightHeader_WithoutExif_ReadsTheStoredSize(MagickFormat format, string fileName)
    {
        var path = Write(format, fileName, static image => image.Strip());

        Assert.True(ImageSizeReader.TryReadUprightHeader(path, out var size));
        Assert.Equal(new ImageSize(Width, Height), size);
    }

    [Fact]
    public void TryReadUprightHeader_Psd_LeavesItToImageMagick()
    {
        var path = Write(MagickFormat.Psd, "image.psd");

        Assert.False(ImageSizeReader.TryReadUprightHeader(path, out _));
        Assert.True(ImageSizeReader.TryReadHeader(path, out _));
    }

    [Fact]
    public void TryReadHeader_PngNamedAsJpeg_ReadsOnTheFastPath()
    {
        var path = Write(MagickFormat.Png, "misnamed.jpg");

        Assert.True(ImageSizeReader.TryReadHeader(path, out var size));
        Assert.Equal(new ImageSize(Width, Height), size);
    }

    [Fact]
    public void TryReadHeader_TopDownBmp_ReportsPositiveHeight()
    {
        var path = Write(MagickFormat.Bmp, "topdown.bmp");
        var bytes = File.ReadAllBytes(path);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(22), -(int)Height);
        File.WriteAllBytes(path, bytes);

        Assert.True(ImageSizeReader.TryReadHeader(path, out var size));
        Assert.Equal(new ImageSize(Width, Height), size);
    }

    [Fact]
    public void TryReadHeader_TextStartingLikeABmp_Fails()
    {
        var path = Path.Combine(_directory, "notes.bmp");
        File.WriteAllText(path, "BMW service notes, kept next to the photos of the car");

        Assert.False(ImageSizeReader.TryReadHeader(path, out _));
        Assert.Equal(default, ImageSizeReader.Read(path));
    }

    [Fact]
    public void TryReadHeader_PngDeclaringZeroWidth_Fails()
    {
        var path = Write(MagickFormat.Png, "zerowidth.png");
        var bytes = File.ReadAllBytes(path);
        bytes.AsSpan(16, 4).Clear();
        File.WriteAllBytes(path, bytes);

        Assert.False(ImageSizeReader.TryReadHeader(path, out _));
        Assert.True(ImageSizeReader.Read(path).IsEmpty);
    }

    [Fact]
    public void TryReadHeader_PngCutOffInsideTheHeader_Fails()
    {
        var path = Write(MagickFormat.Png, "cutoff.png");
        File.WriteAllBytes(path, File.ReadAllBytes(path)[..20]);

        Assert.False(ImageSizeReader.TryReadHeader(path, out _));
        Assert.Equal(default, ImageSizeReader.Read(path));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TryReadHeader_Jpeg_AgreesWithTheMagickPing(bool progressive)
    {
        var path = Write(MagickFormat.Jpeg, progressive ? "progressive.jpg" : "baseline.jpg", image =>
        {
            if (progressive)
            {
                image.Settings.Interlace = Interlace.Jpeg;
            }
        });

        AssertReadsFromTheHeaderLikeThePing(path);
    }

    [Fact]
    public void TryReadHeader_JpegWithLargeMetadataBeforeTheFrame_ReadsTheSize()
    {
        // Four of the largest segments a JPEG can hold, as a camera's EXIF and embedded preview take up
        byte[] segment = [0xFF, 0xE1, 0xFF, 0xFF, .. new byte[65533]];
        var path = WriteJpegWithBytesAfterTheStart("metadata.jpg", [.. segment, .. segment, .. segment, .. segment]);

        Assert.True(ImageSizeReader.TryReadHeader(path, out var size));
        Assert.Equal(new ImageSize(Width, Height), size);
    }

    [Fact]
    public void TryReadHeader_JpegWithFillBytesBeforeAMarker_ReadsTheSize()
    {
        var path = WriteJpegWithBytesAfterTheStart("fill.jpg", [0xFF, 0xFF, 0xFF]);

        Assert.True(ImageSizeReader.TryReadHeader(path, out var size));
        Assert.Equal(new ImageSize(Width, Height), size);
    }

    [Fact]
    public void TryReadHeader_JpegSegmentRunningPastTheEnd_Fails()
    {
        var path = Path.Combine(_directory, "overrun.jpg");
        File.WriteAllBytes(path, [0xFF, 0xD8, 0xFF, 0xE1, 0xFF, 0xFF, 0x00, 0x00]);

        Assert.False(ImageSizeReader.TryReadHeader(path, out _));
        Assert.Equal(default, ImageSizeReader.Read(path));
    }

    [Fact]
    public void TryReadHeader_JpegWithoutAFrame_Fails()
    {
        var path = Path.Combine(_directory, "noframe.jpg");
        File.WriteAllBytes(path, [0xFF, 0xD8, 0xFF, 0xD9]);

        Assert.False(ImageSizeReader.TryReadHeader(path, out _));
    }

    [Theory]
    [InlineData("lossy", "VP8 ")]
    [InlineData("lossless", "VP8L")]
    [InlineData("extended", "VP8X")]
    public void TryReadHeader_Webp_AgreesWithTheMagickPing(string kind, string firstChunk)
    {
        var path = Write(MagickFormat.WebP, kind + ".webp", image =>
        {
            image.Settings.SetDefine(MagickFormat.WebP, "lossless", kind is "lossless");
            if (kind is "extended")
            {
                // A lossy image with transparency needs the extended format to hold its alpha chunk
                image.Alpha(AlphaAction.On);
                image.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, 0.5);
            }
        });

        Assert.Equal(firstChunk, Encoding.ASCII.GetString(File.ReadAllBytes(path), 12, 4));
        AssertReadsFromTheHeaderLikeThePing(path);
    }

    [Fact]
    public void TryReadHeader_AnimatedWebp_ReportsTheCanvas()
    {
        var path = Path.Combine(_directory, "animated.webp");
        using (var frames = new MagickImageCollection())
        {
            frames.Add(new MagickImage(MagickColors.CornflowerBlue, Width, Height));
            frames.Add(new MagickImage(MagickColors.Tomato, Width, Height));
            frames.Add(new MagickImage(MagickColors.SeaGreen, Width, Height));
            frames.Write(path, MagickFormat.WebP);
        }

        Assert.True(ImageSizeReader.TryReadHeader(path, out var size));
        Assert.Equal(new ImageSize(Width, Height), size);
    }

    [Theory]
    [InlineData(Endian.LSB, "II")]
    [InlineData(Endian.MSB, "MM")]
    public void TryReadHeader_Tiff_AgreesWithTheMagickPing(Endian endian, string byteOrder)
    {
        var path = Write(MagickFormat.Tiff, byteOrder + ".tif", image => image.Settings.Endian = endian);

        Assert.Equal(byteOrder, Encoding.ASCII.GetString(File.ReadAllBytes(path), 0, 2));
        AssertReadsFromTheHeaderLikeThePing(path);
    }

    [Theory]
    [InlineData("photo.dng")]
    [InlineData("photo.NEF")]
    [InlineData("photo.cr2")]
    [InlineData("photo.sr2")]
    [InlineData("photo.iiq")]
    public void TryReadHeader_TiffBytesUnderACameraRawExtension_Fails(string fileName)
    {
        var path = Write(MagickFormat.Tiff, fileName);

        Assert.False(ImageSizeReader.TryReadHeader(path, out _));
        // Whatever the raw coder makes of it, it is not thrown at the caller
        _ = ImageSizeReader.Read(path);
    }

    [Fact]
    public void TryReadHeader_BigTiff_Fails()
    {
        var path = Path.Combine(_directory, "big.tif");
        File.WriteAllBytes(path, [0x49, 0x49, 0x2B, 0x00, 0x08, 0x00, 0x00, 0x00, 0x10, 0, 0, 0, 0, 0, 0, 0]);

        Assert.False(ImageSizeReader.TryReadHeader(path, out _));
    }

    [Fact]
    public void TryReadHeader_TiffWithIfdOffsetPastTheEnd_Fails()
    {
        var path = Path.Combine(_directory, "offset.tif");
        File.WriteAllBytes(path, [0x49, 0x49, 0x2A, 0x00, 0xFF, 0xFF, 0xFF, 0x7F]);

        Assert.False(ImageSizeReader.TryReadHeader(path, out _));
        Assert.Equal(default, ImageSizeReader.Read(path));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // A temporary directory another process still reads is left for the system to clean
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Writes a JPEG with <paramref name="bytes"/> put between its start marker and its first segment.
    /// </summary>
    private string WriteJpegWithBytesAfterTheStart(string fileName, byte[] bytes)
    {
        var path = Write(MagickFormat.Jpeg, fileName);
        var jpeg = File.ReadAllBytes(path);
        File.WriteAllBytes(path, [.. jpeg[..2], .. bytes, .. jpeg[2..]]);
        return path;
    }

    /// <summary>
    /// Asserts the header alone gives the size of the image at <paramref name="path"/>, and that it is the size the
    /// ImageMagick ping reports.
    /// </summary>
    private static void AssertReadsFromTheHeaderLikeThePing(string path)
    {
        using var magick = new MagickImage();
        magick.Ping(path);

        Assert.True(ImageSizeReader.TryReadHeader(path, out var size));
        Assert.Equal(new ImageSize(magick.Width, magick.Height), size);
        Assert.Equal(new ImageSize(Width, Height), size);
        Assert.Equal(size, ImageSizeReader.Read(path));
    }

    private static void SetOrientation(MagickImage image, int exifOrientation, Orientation orientation)
    {
        // Magick writes the tag only with both an EXIF profile and the property set
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Orientation, (ushort)exifOrientation);
        image.SetProfile(exif);
        image.Orientation = orientation;
    }

    /// <summary>
    /// The upright size ImageMagick gives, as the gallery read it before the header parsers knew orientations.
    /// </summary>
    private static ImageSize UprightByPing(string path)
    {
        using var magick = new MagickImage();
        magick.Ping(path);
        var quarterTurned = ExifOrientationHelper.GetImageOrientation(magick) is
            ExifOrientation.MirrorHorizontalRotate270Cw or ExifOrientation.Rotate90Cw or
            ExifOrientation.MirrorHorizontalRotate90Cw or ExifOrientation.Rotated270Cw;
        return quarterTurned
            ? new ImageSize(magick.Height, magick.Width)
            : new ImageSize(magick.Width, magick.Height);
    }

    /// <summary>
    /// Writes a solid image of <see cref="Width"/> by <see cref="Height"/> as <paramref name="format"/>, whatever the
    /// extension of <paramref name="fileName"/> says.
    /// </summary>
    private string Write(MagickFormat format, string fileName, Action<MagickImage>? configure = null)
    {
        var path = Path.Combine(_directory, fileName);
        using var image = new MagickImage(MagickColors.CornflowerBlue, Width, Height);
        configure?.Invoke(image);
        File.WriteAllBytes(path, image.ToByteArray(format));
        return path;
    }
}
