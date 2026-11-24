using ImageMagick;
using PicView.Core.Exif;
using PicView.Tests.Metadata;

namespace PicView.Tests.Exif;

[Collection("Sequential")]
public class WebPExifReaderTests
{
    [Fact]
    public void ReadProfile_WebPWithExif_ReturnsItsProfile()
    {
        var path = SdTestFiles.WriteWebPWithUserComment(SdTestFiles.AsciiUserComment("hello"));
        try
        {
            var profile = WebPExifReader.ReadProfile(new FileInfo(path));

            Assert.Equal("hello", ExifReader.GetUserComment(profile));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadProfile_WebPWithoutExif_ReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview-webp-{Guid.NewGuid():N}.webp");
        try
        {
            using (var image = new MagickImage(MagickColors.Gray, 8, 8))
            {
                image.Format = MagickFormat.WebP;
                image.Write(path);
            }

            Assert.Null(WebPExifReader.ReadProfile(new FileInfo(path)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadProfile_Jpeg_ReturnsNull()
    {
        var path = SdTestFiles.WriteJpegWithUserComment(SdTestFiles.AsciiUserComment("hello"));
        try
        {
            Assert.Null(WebPExifReader.ReadProfile(new FileInfo(path)));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
