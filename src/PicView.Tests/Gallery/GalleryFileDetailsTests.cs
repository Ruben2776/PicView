using System.Runtime.ExceptionServices;
using System.Text;
using ImageMagick;
using PicView.Core.Gallery;

namespace PicView.Tests.Gallery;

/// <summary>
/// Pins what <see cref="GalleryFileDetails.Read"/> gives for a file: its upright pixel size, whether its header answers
/// or ImageMagick does, and null for a file that is not an image.
/// </summary>
public sealed class GalleryFileDetailsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "PicViewGalleryDetailsTests_" + Guid.NewGuid());

    public GalleryFileDetailsTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, true);
    }

    [Fact]
    public void Read_JpegWithXmp_GivesItsSizeWithoutThePacket()
    {
        // The motion photo detector reads the packet itself, for the items shown only
        const string packet = "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"/></x:xmpmeta>";
        var file = WriteImage("photo.jpg", MagickFormat.Jpeg, 64, 48,
            image => image.SetProfile(new XmpProfile(Encoding.UTF8.GetBytes(packet))));

        var details = GalleryFileDetails.Read(file);

        Assert.Equal(new GalleryFileDetails(64, 48), details);
    }

    [Theory]
    [InlineData(MagickFormat.Png, "turned.png")]
    [InlineData(MagickFormat.WebP, "turned.webp")]
    [InlineData(MagickFormat.Psd, "turned.psd")]
    public void Read_ExifOrientationTheHeaderDoesNotRead_GivesTheSizeImageMagickGives(MagickFormat format,
        string name)
    {
        var file = WriteImage(name, format, 64, 48, image =>
        {
            var exif = new ExifProfile();
            exif.SetValue(ExifTag.Orientation, (ushort)6);
            image.SetProfile(exif);
            image.Orientation = Orientation.RightTop;
        });
        using var magick = new MagickImage();
        magick.Ping(file);
        var turned = magick.Orientation is Orientation.RightTop ||
                     magick.GetExifProfile()?.GetValue(ExifTag.Orientation)?.Value is 6;

        var details = GalleryFileDetails.Read(file);

        Assert.Equal(turned ? new GalleryFileDetails(48, 64) : new GalleryFileDetails(64, 48), details);
    }

    [Theory]
    [InlineData(1, Orientation.TopLeft, 64u, 48u)]
    [InlineData(2, Orientation.TopRight, 64u, 48u)]
    [InlineData(3, Orientation.BottomRight, 64u, 48u)]
    [InlineData(4, Orientation.BottomLeft, 64u, 48u)]
    [InlineData(5, Orientation.LeftTop, 48u, 64u)]
    [InlineData(6, Orientation.RightTop, 48u, 64u)]
    [InlineData(7, Orientation.RightBottom, 48u, 64u)]
    [InlineData(8, Orientation.LeftBottom, 48u, 64u)]
    public void Read_JpegWithExifOrientation_GivesTheUprightSize(int exifOrientation, Orientation orientation,
        uint width, uint height)
    {
        // Stored 64 wide and 48 tall
        var file = WriteImage($"turned{exifOrientation}.jpg", MagickFormat.Jpeg, 64, 48, image =>
        {
            // Magick writes the tag only with both an EXIF profile and the property set
            var exif = new ExifProfile();
            exif.SetValue(ExifTag.Orientation, (ushort)exifOrientation);
            image.SetProfile(exif);
            image.Orientation = orientation;
        });

        var details = GalleryFileDetails.Read(file);

        Assert.NotNull(details);
        Assert.Equal(width, details.PixelWidth);
        Assert.Equal(height, details.PixelHeight);
    }

    [Fact]
    public void Read_TgaWithoutSignature_GivesItsSize()
    {
        // A TGA has no signature at its start, so only its extension says what it is
        var file = WriteImage("texture.tga", MagickFormat.Tga, 32, 16);

        var details = GalleryFileDetails.Read(file);

        Assert.NotNull(details);
        Assert.Equal(32u, details.PixelWidth);
        Assert.Equal(16u, details.PixelHeight);
    }

    [Theory]
    [InlineData(MagickFormat.Tga, "texture.tga")]
    [InlineData(MagickFormat.Ico, "icon.ico")]
    [InlineData(MagickFormat.Wbmp, "mono.wbmp")]
    public void Read_FormatWithoutSignature_GivesItsSizeWithoutAFailedStreamPing(MagickFormat format, string name)
    {
        // ImageMagick cannot tell these from their bytes, so a stream ping would only throw before the path ping
        var file = WriteImage(name, format, 32, 16);

        var (details, thrown) = ReadCountingMagickExceptions(file);

        Assert.Equal(new GalleryFileDetails(32, 16), details);
        Assert.Equal(0, thrown);
    }

    [Fact]
    public void Read_Container_GivesNullWithoutPinging()
    {
        var path = Path.Combine(_directory, "image.b64");
        File.WriteAllText(path, "data:image/png;base64,iVBORw0KGgo=");

        var (details, thrown) = ReadCountingMagickExceptions(new FileInfo(path));

        Assert.Null(details);
        Assert.Equal(0, thrown);
    }

    /// <summary>
    /// Reads <paramref name="file"/>'s details, counting the ImageMagick exceptions thrown on this thread meanwhile,
    /// caught or not: other tests may run on other threads.
    /// </summary>
    private static (GalleryFileDetails? Details, int Thrown) ReadCountingMagickExceptions(FileInfo file)
    {
        var thread = Environment.CurrentManagedThreadId;
        var thrown = 0;
        void Count(object? sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is MagickException && Environment.CurrentManagedThreadId == thread)
            {
                Interlocked.Increment(ref thrown);
            }
        }

        AppDomain.CurrentDomain.FirstChanceException += Count;
        try
        {
            var details = GalleryFileDetails.Read(file);
            return (details, Volatile.Read(ref thrown));
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= Count;
        }
    }

    [Fact]
    public void Read_RawExtension_ReadsWithTheRawCoder()
    {
        // A camera RAW file is a TIFF container whose first image is a small preview, so read by its bytes alone it
        // would give the preview's size. Its extension sends it to the raw coder instead, which also means a plain
        // TIFF named as a RAW is not read as a TIFF
        var file = WriteImage("plain.nef", MagickFormat.Tiff, 32, 16);

        var details = GalleryFileDetails.Read(file);

        Assert.Null(details);
    }

    [Fact]
    public void Read_NotAnImage_GivesNull()
    {
        var path = Path.Combine(_directory, "notes.jpg");
        File.WriteAllText(path, "not an image");

        var details = GalleryFileDetails.Read(new FileInfo(path));

        Assert.Null(details);
    }

    private FileInfo WriteImage(string name, MagickFormat format, uint width, uint height,
        Action<MagickImage>? configure = null)
    {
        var path = Path.Combine(_directory, name);
        using (var image = new MagickImage(MagickColors.Red, width, height))
        {
            configure?.Invoke(image);
            image.Write(path, format);
        }
        return new FileInfo(path);
    }
}
