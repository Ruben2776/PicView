using ImageMagick;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Tests.Exif;

[Collection("Sequential")]
public class ExifChoiceFieldTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"picview-exif-choice-{Guid.NewGuid():N}.jpg");

    public ExifChoiceFieldTests() => TranslationManager.LoadLanguage("en");

    public void Dispose()
    {
        File.Delete(_path);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Choices_CodesSharingAWord_AreOfferedOnce()
    {
        using var viewModel = new ExifViewModel();

        var labels = viewModel.FlashMode.Choices.Select(c => c.Label).ToList();

        Assert.Equal(labels.Distinct(StringComparer.Ordinal).Count(), labels.Count);
        Assert.Equal((ushort)0, viewModel.FlashMode.Choices.Single(c => string.Equals(c.Label, TranslationManager.Translation.FlashDidNotFire, StringComparison.Ordinal)).Code);
    }

    [Fact]
    public void UpdateExifValues_FileWithHardContrast_SelectsHard()
    {
        using var viewModel = ShowFile(ExifTag.Contrast, 2);

        Assert.Equal(TranslationManager.Translation.Hard, viewModel.Contrast.Choices[viewModel.Contrast.SelectedIndex.Value].Label);
        Assert.Equal(TranslationManager.Translation.Hard, viewModel.Contrast.Text.Value);
    }

    [Fact]
    public void UpdateExifValues_CodeOfASharedWord_SelectsThatWord()
    {
        // 24 is "auto, did not fire", which shows as the same word as 0
        using var viewModel = ShowFile(ExifTag.Flash, 24);

        Assert.Equal(TranslationManager.Translation.FlashDidNotFire,
            viewModel.FlashMode.Choices[viewModel.FlashMode.SelectedIndex.Value].Label);
    }

    [Fact]
    public async Task CommitChoiceAsync_AnotherChoice_WritesItsCode()
    {
        using var viewModel = ShowFile(ExifTag.Contrast, 2);
        viewModel.Contrast.SelectedIndex.Value =
            Array.FindIndex(viewModel.Contrast.Choices, c => string.Equals(c.Label, TranslationManager.Translation.Soft, StringComparison.Ordinal));

        var written = await viewModel.CommitChoiceAsync(viewModel.Contrast);

        Assert.True(written);
        Assert.Equal((ushort)1, ReadTag(ExifTag.Contrast));
        Assert.Equal(TranslationManager.Translation.Soft, viewModel.Contrast.Text.Value);
    }

    [Fact]
    public async Task CommitChoiceAsync_TheFilesOwnChoice_WritesNothing()
    {
        // A dropdown opened and closed on what the file holds must not rewrite it, nor turn 24 into 0
        using var viewModel = ShowFile(ExifTag.Flash, 24);
        var before = File.GetLastWriteTimeUtc(_path);

        var written = await viewModel.CommitChoiceAsync(viewModel.FlashMode);

        Assert.False(written);
        Assert.Equal((ushort)24, ReadTag(ExifTag.Flash));
        Assert.Equal(before, File.GetLastWriteTimeUtc(_path));
    }

    [Fact]
    public async Task CommitChoiceAsync_FileGone_ShowsTheFilesChoiceAgain()
    {
        using var viewModel = ShowFile(ExifTag.Contrast, 2);
        var fileIndex = viewModel.Contrast.SelectedIndex.Value;
        File.Delete(_path);
        viewModel.Contrast.SelectedIndex.Value = fileIndex is 0 ? 1 : 0;

        var written = await viewModel.CommitChoiceAsync(viewModel.Contrast);

        Assert.False(written);
        Assert.Equal(fileIndex, viewModel.Contrast.SelectedIndex.Value);
    }

    [Fact]
    public void RemovedMetadata_ClearsTheChoices()
    {
        using var viewModel = ShowFile(ExifTag.ExposureProgram, 3);
        Assert.NotEqual(-1, viewModel.ExposureProgram.SelectedIndex.Value);

        viewModel.ExposureProgram.Clear();

        Assert.Equal(0, viewModel.ExposureProgram.SelectedIndex.Value);
        Assert.Equal(string.Empty, viewModel.ExposureProgram.Text.Value);
    }

    [Fact]
    public void Choices_StartWithABlankChoice()
    {
        using var viewModel = new ExifViewModel();

        Assert.Null(viewModel.Contrast.Choices[0].Code);
        Assert.Equal(string.Empty, viewModel.Contrast.Choices[0].Label);
    }

    [Fact]
    public void UpdateExifValues_FileWithoutTheTag_SelectsTheBlankChoice()
    {
        using var viewModel = ShowFile(ExifTag.Contrast, 2);

        Assert.Equal(0, viewModel.Sharpness.SelectedIndex.Value);
    }

    [Fact]
    public void UpdateExifValues_CodeWithNoWord_SelectsNothing()
    {
        // 16 is no light source Exif names, so it is none of the choices, not the blank one
        using var viewModel = ShowFile(ExifTag.LightSource, 16);

        Assert.Equal(-1, viewModel.LightSource.SelectedIndex.Value);
    }

    [Fact]
    public async Task CommitChoiceAsync_BlankChoice_KeepsTheOtherTags()
    {
        using var viewModel = ShowFile(ExifTag.Contrast, 2, ExifTag.Saturation);
        viewModel.Contrast.SelectedIndex.Value = 0;

        await viewModel.CommitChoiceAsync(viewModel.Contrast);

        Assert.Null(ReadTag(ExifTag.Contrast));
        Assert.Equal((ushort)1, ReadTag(ExifTag.Saturation));
    }

    [Fact]
    public async Task CommitChoiceAsync_BlankChoice_RemovesTheTag()
    {
        using var viewModel = ShowFile(ExifTag.Contrast, 2);
        viewModel.Contrast.SelectedIndex.Value = 0;

        var written = await viewModel.CommitChoiceAsync(viewModel.Contrast);

        Assert.True(written);
        Assert.Null(ReadTag(ExifTag.Contrast));
        Assert.Equal(string.Empty, viewModel.Contrast.Text.Value);
    }

    [Fact]
    public async Task CommitChoiceAsync_ResolutionUnitPicked_WritesIt()
    {
        using var viewModel = ShowFile(ExifTag.ResolutionUnit, 2);
        viewModel.ResolutionUnit.SelectedIndex.Value = IndexOf(viewModel.ResolutionUnit, TranslationManager.Translation.Centimeters);

        var written = await viewModel.CommitChoiceAsync(viewModel.ResolutionUnit);

        Assert.True(written);
        Assert.Equal((ushort)3, ReadTag(ExifTag.ResolutionUnit));
    }

    [Fact]
    public async Task CommitChoiceAsync_ColorSpaceUnchanged_WritesNothing()
    {
        // A file without the tag must not get ColorSpace=0 from a dropdown opened and closed
        using var viewModel = ShowFile(ExifTag.Contrast, 2);
        var before = File.GetLastWriteTimeUtc(_path);

        var written = await viewModel.CommitChoiceAsync(viewModel.ColorRepresentation);

        Assert.False(written);
        Assert.Null(ReadTag(ExifTag.ColorSpace));
        Assert.Equal(before, File.GetLastWriteTimeUtc(_path));
    }

    [Fact]
    public void UpdateExifValues_UncalibratedColorSpace_SelectsUncalibrated()
    {
        using var viewModel = ShowFile(ExifTag.ColorSpace, 65535);

        Assert.Equal(TranslationManager.Translation.Uncalibrated,
            viewModel.ColorRepresentation.Choices[viewModel.ColorRepresentation.SelectedIndex.Value].Label);
    }

    [Fact]
    public async Task CommitChoiceAsync_UncalibratedPicked_Writes65535()
    {
        using var viewModel = ShowFile(ExifTag.ColorSpace, 1);
        viewModel.ColorRepresentation.SelectedIndex.Value = IndexOf(viewModel.ColorRepresentation, TranslationManager.Translation.Uncalibrated);

        await viewModel.CommitChoiceAsync(viewModel.ColorRepresentation);

        Assert.Equal((ushort)65535, ReadTag(ExifTag.ColorSpace));
    }

    [Fact]
    public void UpdateExifValues_PackBitsCompression_ShowsPackBits()
    {
        using var viewModel = ShowFile(ExifTag.Compression, 32773);

        Assert.Equal("PackBits", viewModel.Compression.Choices[viewModel.Compression.SelectedIndex.Value].Label);
        Assert.Equal("PackBits", viewModel.Compression.Text.Value);
    }

    [Fact]
    public async Task CommitChoiceAsync_MeteringModePicked_WritesItsCode()
    {
        using var viewModel = ShowFile(ExifTag.MeteringMode, 2);
        viewModel.MeteringMode.SelectedIndex.Value = IndexOf(viewModel.MeteringMode, "Spot");

        var written = await viewModel.CommitChoiceAsync(viewModel.MeteringMode);

        Assert.True(written);
        Assert.Equal((ushort)3, ReadTag(ExifTag.MeteringMode));
    }

    [Fact]
    public async Task CommitChoiceAsync_ReadOnlyCompression_WritesNothing()
    {
        using var viewModel = ShowFile(ExifTag.Compression, 6);
        viewModel.Compression.SelectedIndex.Value = IndexOf(viewModel.Compression, "LZW");

        var written = await viewModel.CommitChoiceAsync(viewModel.Compression);

        Assert.False(written);
        Assert.True(viewModel.Compression.IsReadOnly);
        Assert.Equal((ushort)6, ReadTag(ExifTag.Compression));
    }

    [Fact]
    public void UpdateExifValues_Rotated90_SelectsThatOrientation()
    {
        // Magick.NET writes the Orientation tag from the image's orientation, not from the profile
        using (var image = new MagickImage(MagickColors.White, 1, 1))
        {
            image.Format = MagickFormat.Jpeg;
            var profile = new ExifProfile();
            profile.SetValue(ExifTag.Orientation, (ushort)6);
            image.SetProfile(profile);
            image.Orientation = Orientation.RightTop;
            image.Write(_path);
        }

        using var viewModel = new ExifViewModel();
        viewModel.UpdateExifValues(new ImageModel { FileInfo = new FileInfo(_path), PixelWidth = 1, PixelHeight = 1 });

        Assert.True(viewModel.Orientation.IsReadOnly);
        Assert.Equal((ushort)6, viewModel.Orientation.Choices[viewModel.Orientation.SelectedIndex.Value].Code);
    }

    private static int IndexOf(ExifChoiceField field, string? label) =>
        Array.FindIndex(field.Choices, c => string.Equals(c.Label, label, StringComparison.Ordinal));

    private ExifViewModel ShowFile(ExifTag<ushort> tag, ushort code, ExifTag<ushort>? otherTag = null)
    {
        using (var image = new MagickImage(MagickColors.White, 1, 1))
        {
            image.Format = MagickFormat.Jpeg;
            var profile = new ExifProfile();
            profile.SetValue(tag, code);
            if (otherTag is not null)
            {
                profile.SetValue(otherTag, (ushort)1);
            }
            image.SetProfile(profile);
            image.Write(_path);
        }

        var viewModel = new ExifViewModel();
        using var magick = new MagickImage(_path);
        viewModel.UpdateExifValues(new ImageModel { FileInfo = new FileInfo(_path), PixelWidth = 1, PixelHeight = 1 },
            magick);
        return viewModel;
    }

    private ushort? ReadTag(ExifTag<ushort> tag)
    {
        using var image = new MagickImage(_path);
        return image.GetExifProfile()?.GetValue(tag)?.Value;
    }
}
