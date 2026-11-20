using System.Buffers.Binary;
using ImageMagick;
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
