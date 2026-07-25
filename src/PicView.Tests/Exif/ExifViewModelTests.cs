using ImageMagick;
using PicView.Core.Exif;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

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

    [Fact]
    public async Task SetExifRatingCommand_ValidRating_WritesRatingAndUpdatesProperty()
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview-exif-{Guid.NewGuid():N}.jpg");
        try
        {
            await TranslationManager.LoadLanguage("en");
            using (var image = new MagickImage(MagickColors.White, 1, 1))
            {
                image.Format = MagickFormat.Jpeg;
                image.Write(path);
            }

            using var viewModel = new ExifViewModel();
            var fileInfo = new FileInfo(path);
            var rated = WaitForRating(viewModel, 4);

            viewModel.SetExifRating4Command!.Execute(fileInfo);

            Assert.Equal((uint)4, await rated);
            using var ratedImage = new MagickImage(path);
            Assert.Equal((ushort)4, ratedImage.GetExifProfile()?.GetValue(ExifTag.Rating)?.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SetExifRatingCommand_MissingFile_DoesNotUpdateProperty()
    {
        await TranslationManager.LoadLanguage("en");
        using var viewModel = new ExifViewModel();
        var fileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), $"picview-missing-{Guid.NewGuid():N}.jpg"));
        var rated = WaitForRating(viewModel, 4, TimeSpan.FromSeconds(2));

        viewModel.SetExifRating4Command!.Execute(fileInfo);

        await Assert.ThrowsAsync<TimeoutException>(async () => await rated);
        Assert.Equal((uint)0, viewModel.ExifRating.Value);
    }

    private static Task<uint> WaitForRating(ExifViewModel viewModel, uint rating, TimeSpan? timeout = null)
    {
        var completion = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = viewModel.ExifRating.Where(value => value == rating)
            .Subscribe(value => completion.TrySetResult(value));

        return completion.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(10)).ContinueWith(task =>
        {
            subscription.Dispose();
            return task;
        }).Unwrap();
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
