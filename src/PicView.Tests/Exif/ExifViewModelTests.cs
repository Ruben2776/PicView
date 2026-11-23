using System.Text;
using ImageMagick;
using PicView.Core.Exif;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Titles;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Exif;

[Collection("Sequential")]
public class ExifViewModelTests
{
    [Fact]
    public void UpdateExifValues_LegacyBigEndianComment_DecodesBeforeOtherExifTags()
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview-exif-{Guid.NewGuid():N}.jpg");
        try
        {
            TranslationManager.LoadLanguage("en");
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
            TranslationManager.LoadLanguage("en");
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
        TranslationManager.LoadLanguage("en");
        using var viewModel = new ExifViewModel();
        var fileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), $"picview-missing-{Guid.NewGuid():N}.jpg"));
        var rated = WaitForRating(viewModel, 4, TimeSpan.FromSeconds(2));

        viewModel.SetExifRating4Command!.Execute(fileInfo);

        await Assert.ThrowsAsync<TimeoutException>(async () => await rated);
        Assert.Equal((uint)0, viewModel.ExifRating.Value);
    }

    [Fact]
    public async Task CommitAsync_MalformedFNumber_ShowsTheFilesValueAgain()
    {
        var path = WriteJpeg(profile => profile.SetValue(ExifTag.FNumber, new Rational(28, 10)));
        try
        {
            using var viewModel = Show(path);
            var fileValue = viewModel.FNumber.Value;
            Assert.False(string.IsNullOrEmpty(fileValue));

            // The box's two-way binding has already put the typed text in the property
            viewModel.FNumber.Value = "not a number";
            var written = await viewModel.CommitAsync(f => ExifWriter.AddFNumber(f, "not a number"));

            Assert.False(written);
            Assert.Equal(fileValue, viewModel.FNumber.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CommitAsync_Written_ShowsWhatTheFileHolds()
    {
        var path = WriteJpeg(_ => { });
        try
        {
            using var viewModel = Show(path);

            viewModel.Authors.Value = "Ann";
            var written = await viewModel.CommitAsync(f => ExifWriter.AddAuthors(f, "Ann"));

            Assert.True(written);
            Assert.Equal("Ann", viewModel.Authors.Value);
            using var image = new MagickImage(path);
            Assert.Equal("Ann", image.GetExifProfile()?.GetValue(ExifTag.Artist)?.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CommitAsync_FileGone_DoesNotKeepTheTypedText()
    {
        var path = WriteJpeg(_ => { });
        using var viewModel = Show(path);
        File.Delete(path);

        viewModel.MaxAperture.Value = "4";
        var written = await viewModel.CommitAsync(f => ExifWriter.AddMaxAperture(f, "4"));

        Assert.False(written);
        Assert.Equal(string.Empty, viewModel.MaxAperture.Value);
    }

    [Fact]
    public async Task CommitAsync_ShowingAnotherImageMeanwhile_KeepsItsValues()
    {
        var first = WriteJpeg(_ => { });
        var second = WriteJpeg(profile => profile.SetValue(ExifTag.Artist, "Bob"));
        try
        {
            using var viewModel = Show(first);

            var written = await viewModel.CommitAsync(async f =>
            {
                // The tab moves on to another image while the write runs
                viewModel.UpdateExifValues(new ImageModel { FileInfo = new FileInfo(second), PixelWidth = 4, PixelHeight = 4 });
                return await ExifWriter.AddAuthors(f, "Ann");
            });

            Assert.True(written);
            Assert.Equal("Bob", viewModel.Authors.Value);
            using var image = new MagickImage(first);
            Assert.Equal("Ann", image.GetExifProfile()?.GetValue(ExifTag.Artist)?.Value);
        }
        finally
        {
            File.Delete(first);
            File.Delete(second);
        }
    }

    private static string WriteJpeg(Action<ExifProfile> fill, IColorProfile? colorProfile = null)
    {
        TranslationManager.LoadLanguage("en");
        var path = Path.Combine(Path.GetTempPath(), $"picview-exif-commit-{Guid.NewGuid():N}.jpg");
        using var image = new MagickImage(MagickColors.White, 4, 4);
        image.Format = MagickFormat.Jpeg;
        var profile = new ExifProfile();
        profile.SetValue(ExifTag.Software, "PicViewTest");
        fill(profile);
        image.SetProfile(profile);
        if (colorProfile is not null)
        {
            image.SetProfile(colorProfile);
        }
        image.Write(path);
        return path;
    }

    private static ExifViewModel Show(string path)
    {
        var viewModel = new ExifViewModel();
        viewModel.UpdateExifValues(new ImageModel { FileInfo = new FileInfo(path), PixelWidth = 4, PixelHeight = 4 });
        return viewModel;
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

    [Fact]
    public void UpdateExifValues_NullFileInfo_DoesNotThrow()
    {
        TranslationManager.LoadLanguage("en");
        using var viewModel = new ExifViewModel();
        var model = new ImageModel { FileInfo = null, PixelWidth = 800, PixelHeight = 600 };
        
        viewModel.UpdateExifValues(model, null);
        
        Assert.Equal((uint)800, viewModel.PixelWidth.Value);
        Assert.Equal((uint)600, viewModel.PixelHeight.Value);
    }

    [Fact]
    public void UpdateExifValues_NoFileAfterAFileWithExif_KeepsThePixelSizeAndClearsTheRest()
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview-exif-then-none-{Guid.NewGuid():N}.jpg");
        try
        {
            TranslationManager.LoadLanguage("en");
            using (var image = new MagickImage(MagickColors.White, 100, 100))
            {
                image.Format = MagickFormat.Jpeg;
                image.Density = new Density(72, 72);
                var profile = new ExifProfile();
                profile.SetValue(ExifTag.Make, "TestMake");
                image.SetProfile(profile);
                image.Write(path);
            }
            using var viewModel = new ExifViewModel();
            viewModel.UpdateExifValues(new ImageModel { FileInfo = new FileInfo(path), PixelWidth = 100, PixelHeight = 100 });
            Assert.Equal("TestMake", viewModel.CameraMaker.Value);

            // A clipboard image
            viewModel.UpdateExifValues(new ImageModel { FileInfo = null, PixelWidth = 800, PixelHeight = 600 });

            Assert.Equal((uint)800, viewModel.PixelWidth.Value);
            Assert.Equal((uint)600, viewModel.PixelHeight.Value);
            Assert.Equal(string.Empty, viewModel.CameraMaker.Value);
            Assert.Null(viewModel.ImageFormat.Value);
            Assert.Equal(0.0, viewModel.DpiX.Value);
            Assert.Equal(string.Empty, viewModel.PrintSizeCm.Value);
            Assert.Equal(AspectRatioFormatter.GetFormattedAspectRatio(200, 800, 600), viewModel.AspectRatio.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void UpdateExifValues_ImageWithoutExif_SetsDefaultValues()
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview-noexif-{Guid.NewGuid():N}.jpg");
        try
        {
            TranslationManager.LoadLanguage("en");
            using (var image = new MagickImage(MagickColors.White, 10, 20))
            {
                image.Format = MagickFormat.Jpeg;
                image.Density = new Density(72, 72);
                image.Write(path);
            }

            var model = new ImageModel { FileInfo = new FileInfo(path), PixelWidth = 10, PixelHeight = 20 };
            using var viewModel = new ExifViewModel();

            viewModel.UpdateExifValues(model);

            Assert.Equal(72.0, viewModel.DpiX.Value);
            Assert.Equal(72.0, viewModel.DpiY.Value);
            Assert.Equal(-1, viewModel.Orientation.SelectedIndex.Value);
            Assert.Equal(MagickFormat.Jpeg, viewModel.ImageFormat.Value);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void UpdateExifValues_WithValidExifProfile_PopulatesProperties()
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview-exif-full-{Guid.NewGuid():N}.jpg");
        try
        {
            TranslationManager.LoadLanguage("en");
            using (var image = new MagickImage(MagickColors.White, 100, 100))
            {
                image.Format = MagickFormat.Jpeg;
                var profile = new ExifProfile();
                profile.SetValue(ExifTag.Make, "TestMake");
                profile.SetValue(ExifTag.Model, "TestModel");
                profile.SetValue(ExifTag.Software, "PicViewTest");
                profile.SetValue(ExifTag.Artist, "TestAuthor");
                profile.SetValue(ExifTag.Copyright, "TestCopyright");
                profile.SetValue(ExifTag.XPTitle, Encoding.Unicode.GetBytes("TestTitle\0"));
                profile.SetValue(ExifTag.ImageDescription, "TestTitle");
                profile.SetValue(ExifTag.Rating, (ushort)3);

                image.SetProfile(profile);
                image.Write(path);
            }

            var model = new ImageModel { FileInfo = new FileInfo(path), PixelWidth = 100, PixelHeight = 100 };
            using var viewModel = new ExifViewModel();

            viewModel.UpdateExifValues(model);

            Assert.Equal("TestMake", viewModel.CameraMaker.Value);
            Assert.Equal("TestModel", viewModel.CameraModel.Value);
            Assert.Equal("PicViewTest", viewModel.Software.Value);
            Assert.Equal("TestAuthor", viewModel.Authors.Value);
            Assert.Equal("TestCopyright", viewModel.Copyright.Value);
            Assert.Equal("TestTitle", viewModel.Title.Value);
            Assert.Equal((uint)3, viewModel.ExifRating.Value);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task RemoveImageMetaDataCommand_SetsPropertiesToDefault()
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview-removeexif-{Guid.NewGuid():N}.jpg");
        try
        {
            TranslationManager.LoadLanguage("en");
            using (var image = new MagickImage(MagickColors.White, 10, 10))
            {
                image.Format = MagickFormat.Jpeg;
                var profile = new ExifProfile();
                profile.SetValue(ExifTag.Artist, "TestAuthor");
                profile.SetValue(ExifTag.Rating, (ushort)5);
                image.SetProfile(profile);
                await image.WriteAsync(path, TestContext.Current.CancellationToken);
            }

            var model = new ImageModel { FileInfo = new FileInfo(path), PixelWidth = 10, PixelHeight = 10 };
            using var viewModel = new ExifViewModel();

            viewModel.UpdateExifValues(model);
            Assert.Equal("TestAuthor", viewModel.Authors.Value);
            Assert.Equal((uint)5, viewModel.ExifRating.Value);

            viewModel.RemoveImageMetaDataCommand!.Execute(new FileInfo(path));
            
            // Wait a little for the command to finish if it's async under the hood
            await Task.Delay(500, TestContext.Current.CancellationToken);

            Assert.Equal(string.Empty, viewModel.Authors.Value);
            Assert.Equal((uint)0, viewModel.ExifRating.Value);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task RemoveImageMetaDataCommand_KeepsTheColorProfileAndShowsTheFileAgain()
    {
        var path = WriteJpeg(profile =>
        {
            profile.SetValue(ExifTag.Artist, "Ann");
            profile.SetValue(ExifTag.ColorSpace, (ushort)1);
            profile.SetValue(ExifTag.MeteringMode, (ushort)3);
        }, ColorProfiles.SRGB);
        try
        {
            using var viewModel = Show(path);
            Assert.NotEqual(0, viewModel.ColorRepresentation.SelectedIndex.Value);

            var written = await viewModel.CommitAsync(ExifWriter.RemoveImageMetaData);

            Assert.True(written);
            Assert.Equal(string.Empty, viewModel.Authors.Value);
            Assert.Equal(0, viewModel.ColorRepresentation.SelectedIndex.Value);
            Assert.Equal(0, viewModel.MeteringMode.SelectedIndex.Value);
            using var image = new MagickImage(path);
            Assert.Null(image.GetExifProfile());
            Assert.NotNull(image.GetColorProfile());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void OpenGoogleLinkCommand_IsInitialized()
    {
        TranslationManager.LoadLanguage("en");
        using var viewModel = new ExifViewModel();
        Assert.NotNull(viewModel.OpenGoogleLinkCommand);
    }

    [Fact]
    public void OpenBingLinkCommand_IsInitialized()
    {
        TranslationManager.LoadLanguage("en");
        using var viewModel = new ExifViewModel();
        Assert.NotNull(viewModel.OpenBingLinkCommand);
    }

    [Fact]
    public void UpdateImageFormatDisplay_NullFormat_SetsNullDisplay()
    {
        using var viewModel = new ExifViewModel();
        viewModel.ImageFormat.Value = null;
        viewModel.UpdateImageFormatDisplay();

        Assert.Null(viewModel.ImageFormatDisplay.Value);
    }

    [Fact]
    public void UpdateImageFormatDisplay_StandardFormat_SetsFormatWithoutSuffix()
    {
        TranslationManager.LoadLanguage("en");
        using var viewModel = new ExifViewModel();
        viewModel.ImageFormat.Value = MagickFormat.Jpeg;
        viewModel.IsMotionPhoto.Value = false;
        viewModel.UpdateImageFormatDisplay();

        Assert.Equal("Jpeg", viewModel.ImageFormatDisplay.Value);
    }

    [Fact]
    public void UpdateImageFormatDisplay_MotionPhoto_SetsFormatWithSuffix()
    {
        TranslationManager.LoadLanguage("en");
        using var viewModel = new ExifViewModel();
        viewModel.ImageFormat.Value = MagickFormat.Jpeg;
        viewModel.IsMotionPhoto.Value = true;
        viewModel.UpdateImageFormatDisplay();

        var expectedMarker = TranslationManager.Translation.MotionPhoto;
        Assert.False(string.IsNullOrEmpty(expectedMarker));
        Assert.Equal($"Jpeg ({expectedMarker})", viewModel.ImageFormatDisplay.Value);
    }
}
