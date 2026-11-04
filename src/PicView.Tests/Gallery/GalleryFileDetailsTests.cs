using ImageMagick;
using PicView.Core.Gallery;

namespace PicView.Tests.Gallery;

/// <summary>
/// Pins what <see cref="GalleryFileDetails.Read"/> gives for a file: its pixel size and XMP packet, whatever the
/// format's signature, and null for a file that is not an image.
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
    public void Read_JpegWithXmp_GivesItsSizeAndPacket()
    {
        const string packet = "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"/></x:xmpmeta>";
        var file = WriteImage("photo.jpg", MagickFormat.Jpeg, 64, 48,
            image => image.SetProfile(new XmpProfile(System.Text.Encoding.UTF8.GetBytes(packet))));

        var details = GalleryFileDetails.Read(file);

        Assert.NotNull(details);
        Assert.Equal(64u, details.PixelWidth);
        Assert.Equal(48u, details.PixelHeight);
        Assert.Contains("x:xmpmeta", details.XmpPacket);
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
        Assert.Null(details.XmpPacket);
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
