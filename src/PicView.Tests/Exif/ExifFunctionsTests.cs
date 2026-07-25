using ImageMagick;
using ImageMagick.Drawing;
using PicView.Core.Exif;

namespace PicView.Tests.Exif;

public class ExifFunctionsTests : IDisposable
{
    private readonly string _directory =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"picview-rating-{Guid.NewGuid():N}")).FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, true);
        }
        catch (IOException)
        {
            // Cleaning up the temp directory is best effort
        }
    }

    [Theory]
    [InlineData((ushort)0)]
    [InlineData((ushort)1)]
    [InlineData((ushort)2)]
    [InlineData((ushort)3)]
    [InlineData((ushort)4)]
    [InlineData((ushort)5)]
    public async Task SetExifRatingAsync_ValidRating_WritesRatingToProfile(ushort rating)
    {
        var fileInfo = CreateImage(MagickFormat.Jpeg);

        var actual = await ExifWriter.SetExifRatingAsync(fileInfo, rating);

        Assert.True(actual);
        Assert.Equal(rating, ReadRating(fileInfo));
    }

    [Fact]
    public async Task SetExifRatingAsync_Updates_Profile_And_Image_Pixel_Data_Remains_The_Same()
    {
        var fileInfo = CreateImage(MagickFormat.Jpeg);
        var expectedPixels = ReadPixels(fileInfo);

        var actual = await ExifWriter.SetExifRatingAsync(fileInfo, 3);

        Assert.True(actual);
        Assert.Equal((ushort)3, ReadRating(fileInfo));
        Assert.Equal(expectedPixels, ReadPixels(fileInfo));
    }

    [Fact]
    public async Task SetExifRatingAsync_ImageWithoutExifProfile_CreatesProfileWithRating()
    {
        var fileInfo = CreateImage(MagickFormat.Jpeg, image => image.Strip());
        Assert.Null(ReadProfile(fileInfo));

        var actual = await ExifWriter.SetExifRatingAsync(fileInfo, 5);

        Assert.True(actual);
        Assert.Equal((ushort)5, ReadRating(fileInfo));
    }

    [Fact]
    public async Task SetExifRatingAsync_ExistingRating_IsOverwritten()
    {
        var fileInfo = CreateImage(MagickFormat.Jpeg, image =>
        {
            var profile = new ExifProfile();
            profile.SetValue(ExifTag.Rating, (ushort)1);
            image.SetProfile(profile);
        });
        Assert.Equal((ushort)1, ReadRating(fileInfo));

        var actual = await ExifWriter.SetExifRatingAsync(fileInfo, 4);

        Assert.True(actual);
        Assert.Equal((ushort)4, ReadRating(fileInfo));
    }

    [Fact]
    public async Task SetExifRatingAsync_ValidRating_KeepsOtherExifValues()
    {
        var fileInfo = CreateImage(MagickFormat.Jpeg, image =>
        {
            var profile = new ExifProfile();
            profile.SetValue(ExifTag.Artist, "PicView");
            profile.SetValue(ExifTag.Model, "Test camera");
            profile.SetValue(ExifTag.ISOSpeedRatings, [(ushort)200]);
            image.SetProfile(profile);
        });

        var actual = await ExifWriter.SetExifRatingAsync(fileInfo, 2);

        Assert.True(actual);
        var profile = ReadProfile(fileInfo);
        Assert.NotNull(profile);
        Assert.Equal((ushort)2, profile.GetValue(ExifTag.Rating)?.Value);
        Assert.Equal("PicView", profile.GetValue(ExifTag.Artist)?.Value);
        Assert.Equal("Test camera", profile.GetValue(ExifTag.Model)?.Value);
        Assert.Equal([(ushort)200], profile.GetValue(ExifTag.ISOSpeedRatings)?.Value);
    }

    /// <summary>
    /// Documents current behaviour: writing a rating to a TIFF reports success, but the rating is lost because
    /// ImageMagick drops the EXIF profile when re-encoding the TIFF. Update this test if that gets fixed.
    /// </summary>
    [Fact]
    public async Task SetExifRatingAsync_Tiff_ReturnsTrueButDoesNotPersistTheRating()
    {
        var fileInfo = CreateImage(MagickFormat.Tiff);

        var actual = await ExifWriter.SetExifRatingAsync(fileInfo, 3);

        Assert.True(actual);
        Assert.Null(ReadRating(fileInfo));
    }

    [Theory]
    [InlineData((ushort)6)]
    [InlineData(ushort.MaxValue)]
    public async Task SetExifRatingAsync_RatingAboveFive_ReturnsFalseAndLeavesFileUntouched(ushort rating)
    {
        var fileInfo = CreateImage(MagickFormat.Jpeg);
        var expected = await ReadAllBytesAsync(fileInfo);

        var actual = await ExifWriter.SetExifRatingAsync(fileInfo, rating);

        Assert.False(actual);
        Assert.Equal(expected, await ReadAllBytesAsync(fileInfo));
    }

    [Fact]
    public async Task SetExifRatingAsync_NullRating_ReturnsFalseAndLeavesFileUntouched()
    {
        var fileInfo = CreateImage(MagickFormat.Jpeg);
        var expected = await ReadAllBytesAsync(fileInfo);

        var actual = await ExifWriter.SetExifRatingAsync(fileInfo, null);

        Assert.False(actual);
        Assert.Equal(expected, await ReadAllBytesAsync(fileInfo));
    }

    [Fact]
    public async Task SetExifRatingAsync_NullFileInfo_ReturnsFalse()
    {
        var actual = await ExifWriter.SetExifRatingAsync(null, 3);

        Assert.False(actual);
    }

    [Fact]
    public async Task SetExifRatingAsync_MissingFile_ReturnsFalse()
    {
        var fileInfo = new FileInfo(Path.Combine(_directory, $"missing-{Guid.NewGuid():N}.jpg"));

        var actual = await ExifWriter.SetExifRatingAsync(fileInfo, 3);

        Assert.False(actual);
        Assert.False(fileInfo.Exists);
    }

    [Fact]
    public async Task SetExifRatingAsync_NotAnImage_ReturnsFalse()
    {
        var path = Path.Combine(_directory, $"not-an-image-{Guid.NewGuid():N}.jpg");
        await File.WriteAllTextAsync(path, "This is not an image", TestContext.Current.CancellationToken);

        var actual = await ExifWriter.SetExifRatingAsync(new FileInfo(path), 3);

        Assert.False(actual);
    }

    private FileInfo CreateImage(MagickFormat format, Action<MagickImage>? configure = null)
    {
        var path = Path.Combine(_directory, $"image-{Guid.NewGuid():N}.{format.ToString().ToLowerInvariant()}");

        using var image = new MagickImage(MagickColors.White, 16, 16);
        image.Format = format;
        // Draw a couple of shapes so the pixel comparison covers more than a single flat color
        image.Draw(new DrawableFillColor(MagickColors.Black), new DrawableRectangle(0, 0, 7, 7));
        image.Draw(new DrawableFillColor(MagickColors.Red), new DrawableRectangle(8, 8, 15, 15));
        configure?.Invoke(image);
        image.Write(path);

        return new FileInfo(path);
    }

    private static Task<byte[]> ReadAllBytesAsync(FileInfo fileInfo) =>
        File.ReadAllBytesAsync(fileInfo.FullName, TestContext.Current.CancellationToken);

    private static IExifProfile? ReadProfile(FileInfo fileInfo)
    {
        using var image = new MagickImage(fileInfo);
        return image.GetExifProfile();
    }

    private static ushort? ReadRating(FileInfo fileInfo) => ReadProfile(fileInfo)?.GetValue(ExifTag.Rating)?.Value;

    private static byte[] ReadPixels(FileInfo fileInfo)
    {
        using var image = new MagickImage(fileInfo);
        using var pixels = image.GetPixels();
        return pixels.ToByteArray(PixelMapping.RGB) ?? [];
    }
}
