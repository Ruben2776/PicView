using ImageMagick;
using PicView.Core.Exif;

namespace PicView.Tests.Exif;

[Collection("Sequential")]
public class UserCommentEncoderTests
{
    [Fact]
    public void Encode_AsciiText_StartsWithAsciiCharacterCode()
    {
        var bytes = UserCommentEncoder.Encode("abc", false);

        Assert.True(bytes.AsSpan().StartsWith("ASCII\0\0\0"u8));
        Assert.Equal("abc", System.Text.Encoding.ASCII.GetString(bytes, 8, bytes.Length - 8));
    }

    [Theory]
    [InlineData("中文 prompt", true)]
    [InlineData("中文 prompt", false)]
    [InlineData("🐱", true)]
    [InlineData("🐱", false)]
    public void Encode_NonAsciiText_ReadsBackFromTheFile(string text, bool useUtf8)
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview-uc-{Guid.NewGuid():N}.jpg");
        try
        {
            using (var image = new MagickImage(MagickColors.White, 1, 1))
            {
                image.Format = MagickFormat.Jpeg;
                var profile = new ExifProfile();
                profile.SetValue(ExifTag.ExifVersion, System.Text.Encoding.ASCII.GetBytes(useUtf8 ? "0300" : "0230"));
                profile.SetValue(ExifTag.UserComment, UserCommentEncoder.Encode(text, useUtf8));
                image.SetProfile(profile);
                image.Write(path);
            }

            using var reloaded = new MagickImage(path);
            Assert.Equal(text, ExifReader.GetUserComment(reloaded.GetExifProfile()));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
