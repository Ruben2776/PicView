using System.Text;
using ImageMagick;
using PicView.Core.Exif;
using PicView.Core.Localization;

namespace PicView.Tests.Exif;

[Collection("Sequential")]
public class JpegMetadataWriterTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"picview-jpeg-meta-{Guid.NewGuid():N}.jpg");

    public JpegMetadataWriterTests() => TranslationManager.LoadLanguage("en");

    public void Dispose()
    {
        File.Delete(_path);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ExifEdit_Jpeg_KeepsTheImageDataByteForByte()
    {
        WriteJpeg(withExif: true);
        var scanBefore = ImageData(File.ReadAllBytes(_path));

        Assert.True(await ExifWriter.AddAuthors(new FileInfo(_path), "Ann"));

        Assert.Equal(scanBefore, ImageData(File.ReadAllBytes(_path)));
        Assert.Equal("Ann", ReadExif()?.GetValue(ExifTag.Artist)?.Value);
        Assert.Equal("PicViewTest", ReadExif()?.GetValue(ExifTag.Software)?.Value);
    }

    [Fact]
    public async Task ExifEdit_JpegWithoutExif_AddsAnExifSegment()
    {
        WriteJpeg(withExif: false);
        var scanBefore = ImageData(File.ReadAllBytes(_path));

        Assert.True(await ExifWriter.AddCopyright(new FileInfo(_path), "PicView"));

        Assert.Equal(scanBefore, ImageData(File.ReadAllBytes(_path)));
        Assert.Equal("PicView", ReadExif()?.GetValue(ExifTag.Copyright)?.Value);
    }

    [Fact]
    public async Task ExifEdit_DataAfterTheImage_IsKept()
    {
        // A motion photo keeps its video after the JPEG's end
        WriteJpeg(withExif: true);
        var trailer = Encoding.ASCII.GetBytes("ftypmp42-motion-photo-video");
        await File.AppendAllBytesAsync(_path, trailer, TestContext.Current.CancellationToken);

        Assert.True(await ExifWriter.AddAuthors(new FileInfo(_path), "Ann"));

        Assert.EndsWith(Convert.ToHexString(trailer), Convert.ToHexString(File.ReadAllBytes(_path)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveImageMetaData_Jpeg_KeepsTheColorProfileAndTheImageData()
    {
        WriteJpeg(withExif: true, comment: "a comment");
        var scanBefore = ImageData(File.ReadAllBytes(_path));

        Assert.True(await ExifWriter.RemoveImageMetaData(new FileInfo(_path)));

        Assert.Equal(scanBefore, ImageData(File.ReadAllBytes(_path)));
        using var image = new MagickImage(_path);
        Assert.Null(image.GetExifProfile());
        Assert.NotNull(image.GetColorProfile());
        Assert.True(string.IsNullOrEmpty(image.Comment));
    }

    [Fact]
    public void Rewrite_NotAJpeg_ReturnsNull()
    {
        Assert.Null(JpegMetadataWriter.Rewrite("\x89PNG\r\n\x1a\n"u8, [], false, JpegMetadataWriter.Drop.None));
    }

    [Fact]
    public void Rewrite_TruncatedSegment_ReturnsNull()
    {
        // SOI, then an APP1 segment whose length runs past the end
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE1, 0x01, 0x00, 0x45, 0x78];

        Assert.Null(JpegMetadataWriter.Rewrite(jpeg, [], false, JpegMetadataWriter.Drop.None));
    }

    [Fact]
    public void Rewrite_ExifTooLargeForASegment_ReturnsNull()
    {
        WriteJpeg(withExif: false);

        Assert.Null(JpegMetadataWriter.Rewrite(File.ReadAllBytes(_path), new byte[70_000], true, JpegMetadataWriter.Drop.None));
    }

    private void WriteJpeg(bool withExif, string? comment = null)
    {
        // Noise, so encoding the image again would not give the same bytes
        using var image = new MagickImage(MagickColors.CornflowerBlue, 64, 64);
        image.AddNoise(NoiseType.Gaussian);
        image.Format = MagickFormat.Jpeg;
        image.Quality = 80;
        if (withExif)
        {
            var profile = new ExifProfile();
            profile.SetValue(ExifTag.Software, "PicViewTest");
            image.SetProfile(profile);
        }

        image.SetProfile(ColorProfiles.SRGB);
        if (comment is not null)
        {
            image.Comment = comment;
        }

        image.Write(_path);
    }

    private IExifProfile? ReadExif()
    {
        using var image = new MagickImage(_path);
        return image.GetExifProfile();
    }

    // The bytes from the start of scan on: the image data, which no metadata edit may touch
    private static string ImageData(byte[] jpeg)
    {
        var position = 2;
        while (jpeg[position + 1] != 0xDA)
        {
            position += 2 + ((jpeg[position + 2] << 8) | jpeg[position + 3]);
        }

        return Convert.ToHexString(jpeg.AsSpan(position));
    }
}
