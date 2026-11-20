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
