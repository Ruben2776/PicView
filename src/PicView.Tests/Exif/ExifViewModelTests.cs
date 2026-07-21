using ImageMagick;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Tests.Exif;

public class ExifViewModelTests
{
    [Fact]
    public async Task UpdateExifValues_LegacyBigEndianComment_DecodesBeforeOtherExifTags()
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview-exif-{Guid.NewGuid():N}.jpg");
        try
        {
            await TranslationManager.LoadLanguage("en");
            using (var image = new MagickImage(MagickColors.White, 1, 1))
            {
                image.Format = MagickFormat.Jpeg;
                File.WriteAllBytes(path, AddExifProfile(image.ToByteArray(), BigEndianExif));
            }

            var model = new ImageModel
            {
                FileInfo = new FileInfo(path),
                PixelWidth = 1,
                PixelHeight = 1
            };
            using var viewModel = new ExifViewModel();
            var imageWithExif = new MagickImage(path);

            viewModel.UpdateExifValues(model, imageWithExif);

            Assert.Equal("中文", viewModel.Comment.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static readonly byte[] BigEndianExif = Convert.FromHexString(
        "4578696600004D4D002A00000008000187690004000000010000001A000000000002900000070000000430323332928600070000000C0000003800000000554E49434F4445004E2D6587");

    private static byte[] AddExifProfile(byte[] jpeg, byte[] exif)
    {
        var segmentLength = exif.Length + 2;
        var result = new byte[jpeg.Length + exif.Length + 4];
        jpeg.AsSpan(0, 2).CopyTo(result);
        result[2] = 0xff;
        result[3] = 0xe1;
        result[4] = (byte)(segmentLength >> 8);
        result[5] = (byte)segmentLength;
        exif.CopyTo(result, 6);
        jpeg.AsSpan(2).CopyTo(result.AsSpan(6 + exif.Length));
        return result;
    }
}
