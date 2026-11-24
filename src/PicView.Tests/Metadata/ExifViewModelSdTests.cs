using ImageMagick;
using PicView.Core.Exif;
using PicView.Core.Localization;
using PicView.Core.Metadata;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Tests.Metadata;

[Collection("Sequential")]
public class ExifViewModelSdTests
{
    private const string Settings = "Steps: 20, Sampler: Euler, CFG scale: 7, Seed: 1, Size: 8x8";

    private const string Payload = "a catt\nNegative prompt: blurry\n" + Settings;

    private const string ComfyPrompt =
        """{ "3": { "class_type": "KSampler", "inputs": { "seed": 1, "steps": 20, "cfg": 7.0, "sampler_name": "euler", "scheduler": "normal", "positive": ["6", 0], "negative": ["7", 0] } }, "6": { "class_type": "CLIPTextEncode", "inputs": { "text": "a cat" } }, "7": { "class_type": "CLIPTextEncode", "inputs": { "text": "blurry" } } }""";

    public ExifViewModelSdTests() => TranslationManager.LoadLanguage("en");

    [Fact]
    public void UpdateExifValues_A1111Png_FillsFieldsAndIsEditable()
    {
        var path = WritePng("parameters", Payload);
        try
        {
            using var viewModel = Show(path);

            Assert.True(viewModel.IsSdMetadataAvailable.Value);
            Assert.Equal("Automatic1111", viewModel.SdGenerator.Value);
            Assert.Equal("a catt", viewModel.SdPrompt.Value);
            Assert.Equal("blurry", viewModel.SdNegativePrompt.Value);
            Assert.Equal(Settings, viewModel.SdSettings.Value);
            Assert.Equal(Payload, viewModel.SdRaw.Value);
            Assert.Equal(TranslationManager.Translation.StoredInPngText, viewModel.SdStorage.Value);
            Assert.True(viewModel.IsSdEditable.Value);
            Assert.True(viewModel.IsCommentShown.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void UpdateExifValues_ComfyUiPng_IsNotEditable()
    {
        var path = WritePng("prompt", ComfyPrompt);
        try
        {
            using var viewModel = Show(path);

            Assert.True(viewModel.IsSdMetadataAvailable.Value);
            Assert.Equal("ComfyUI", viewModel.SdGenerator.Value);
            Assert.False(viewModel.IsSdEditable.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void UpdateExifValues_UserCommentSource_HidesCommentBox()
    {
        var path = SdTestFiles.WriteJpegWithUserComment(SdTestFiles.AsciiUserComment(Payload));
        try
        {
            using var viewModel = Show(path);

            Assert.True(viewModel.IsSdMetadataAvailable.Value);
            Assert.Equal(TranslationManager.Translation.StoredInUserComment, viewModel.SdStorage.Value);
            Assert.True(viewModel.IsSdEditable.Value);
            Assert.False(viewModel.IsCommentShown.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void UpdateExifValues_OrdinaryJpegComment_ShowsCommentBox()
    {
        var path = SdTestFiles.WriteJpegWithUserComment(SdTestFiles.AsciiUserComment("Holiday photo, taken at the beach"));
        try
        {
            using var viewModel = Show(path);

            Assert.False(viewModel.IsSdMetadataAvailable.Value);
            Assert.False(viewModel.IsSdEditable.Value);
            Assert.True(viewModel.IsCommentShown.Value);
            Assert.Equal("Holiday photo, taken at the beach", viewModel.Comment.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CommitSdAsync_Png_WritesAndShowsTheFile()
    {
        var path = WritePng("parameters", Payload);
        try
        {
            using var viewModel = Show(path);

            Assert.True(await viewModel.CommitSdAsync(ExifViewModel.SdField.Prompt, "a cat"));

            AssertShowsFixedPrompt(viewModel, path, comment: null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CommitSdAsync_Jpeg_WritesAndShowsTheFile()
    {
        var path = SdTestFiles.WriteJpegWithUserComment(SdTestFiles.AsciiUserComment(Payload));
        try
        {
            using var viewModel = Show(path);

            Assert.True(await viewModel.CommitSdAsync(ExifViewModel.SdField.Prompt, "a cat"));

            AssertShowsFixedPrompt(viewModel, path, ReadUserComment(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CommitSdAsync_WebP_WritesAndShowsTheFile()
    {
        var path = SdTestFiles.WriteWebPWithUserComment(SdTestFiles.AsciiUserComment(Payload));
        try
        {
            using var viewModel = Show(path);
            Assert.True(viewModel.IsSdEditable.Value);

            Assert.True(await viewModel.CommitSdAsync(ExifViewModel.SdField.Prompt, "a cat"));

            Assert.True(viewModel.IsSdMetadataAvailable.Value);
            AssertShowsFixedPrompt(viewModel, path, ReadUserComment(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CommitSdAsync_CrLfTyped_StoresLf()
    {
        var path = WritePng("parameters", Payload);
        try
        {
            using var viewModel = Show(path);

            Assert.True(await viewModel.CommitSdAsync(ExifViewModel.SdField.Prompt, "a\r\nb"));

            var stored = SdMetadataReader.Read(new FileInfo(path));
            Assert.NotNull(stored);
            Assert.Equal("a\nb", stored.Prompt);
            Assert.StartsWith("a\nb\nNegative prompt: blurry\n", stored.Raw);

            var stamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(path, stamp);

            Assert.False(await viewModel.CommitSdAsync(ExifViewModel.SdField.Prompt, "a\r\nb"));

            Assert.Equal(stamp, File.GetLastWriteTimeUtc(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CommitSdAsync_Unchanged_DoesNotTouchTheFile()
    {
        var path = WritePng("parameters", Payload);
        try
        {
            using var viewModel = Show(path);
            var before = File.ReadAllBytes(path);
            var stamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(path, stamp);

            Assert.False(await viewModel.CommitSdAsync(ExifViewModel.SdField.Settings, Settings));
            Assert.False(await viewModel.CommitSdAsync(ExifViewModel.SdField.Prompt, "  a catt  "));

            Assert.Equal(before, File.ReadAllBytes(path));
            Assert.Equal(stamp, File.GetLastWriteTimeUtc(path));
            Assert.Equal("a catt", viewModel.SdPrompt.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CommitSdAsync_Malformed_RefusesAndShowsTheFileValue()
    {
        var path = WritePng("parameters", Payload);
        try
        {
            using var viewModel = Show(path);
            var before = File.ReadAllBytes(path);

            viewModel.SdPrompt.Value = "Negative prompt: x";
            Assert.False(await viewModel.CommitSdAsync(ExifViewModel.SdField.Prompt, "Negative prompt: x"));

            Assert.Equal("a catt", viewModel.SdPrompt.Value);
            Assert.Equal(before, File.ReadAllBytes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    // Clearing the Settings box would compose a text that still reads back, without its settings line
    [Fact]
    public async Task CommitSdAsync_SettingsCleared_Png_RefusesAndKeepsTheSettings()
    {
        var path = WritePng("parameters", Payload);
        try
        {
            using var viewModel = Show(path);
            var before = File.ReadAllBytes(path);

            viewModel.SdSettings.Value = string.Empty;
            Assert.False(await viewModel.CommitSdAsync(ExifViewModel.SdField.Settings, string.Empty));

            Assert.Equal(before, File.ReadAllBytes(path));
            Assert.Equal(Settings, viewModel.SdSettings.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CommitSdAsync_SettingsCleared_JpegWithNegativePrompt_RefusesAndKeepsTheSettings()
    {
        var path = SdTestFiles.WriteJpegWithUserComment(SdTestFiles.AsciiUserComment(Payload));
        try
        {
            using var viewModel = Show(path);
            var before = File.ReadAllBytes(path);

            viewModel.SdSettings.Value = string.Empty;
            Assert.False(await viewModel.CommitSdAsync(ExifViewModel.SdField.Settings, "  "));

            Assert.Equal(before, File.ReadAllBytes(path));
            Assert.Equal(Settings, viewModel.SdSettings.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CommitSdAsync_NotEditable_DoesNothing()
    {
        var comfy = WritePng("prompt", ComfyPrompt);
        var plain = WritePng(null, null);
        try
        {
            using (var viewModel = Show(comfy))
            {
                var before = File.ReadAllBytes(comfy);

                Assert.False(await viewModel.CommitSdAsync(ExifViewModel.SdField.Prompt, "a dog"));

                Assert.Equal("a cat", viewModel.SdPrompt.Value);
                Assert.Equal(before, File.ReadAllBytes(comfy));
            }

            using (var viewModel = Show(plain))
            {
                var before = File.ReadAllBytes(plain);
                var hidden = SdMetadataParser.ParseA1111(Payload);
                hidden.Source = SdSource.Pixels;
                hidden.Container = SdContainer.Png;
                hidden.StealthDetail = "alpha";
                viewModel.ApplySdMetadata(hidden);

                Assert.Equal(TranslationManager.Translation.StoredInPixels + " (alpha)", viewModel.SdStorage.Value);
                Assert.False(viewModel.IsSdEditable.Value);

                viewModel.SdPrompt.Value = "a dog";
                Assert.False(await viewModel.CommitSdAsync(ExifViewModel.SdField.Prompt, "a dog"));

                Assert.Equal("a catt", viewModel.SdPrompt.Value);
                Assert.Equal(before, File.ReadAllBytes(plain));
            }
        }
        finally
        {
            File.Delete(comfy);
            File.Delete(plain);
        }
    }

    [Fact]
    public async Task CommitSdAsync_AnotherImageShownMeanwhile_WritesTheFirstFile()
    {
        var first = WritePng("parameters", Payload);
        var second = WritePng("parameters", "a bird\n" + Settings);
        try
        {
            using var viewModel = Show(first);

            Task<bool> commit;
            // Holding the first file open keeps the write waiting, while the tab moves on to another image
            using (new FileStream(first, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                commit = viewModel.CommitSdAsync(ExifViewModel.SdField.Prompt, "a cat");
                Assert.False(commit.IsCompleted);
                viewModel.UpdateExifValues(new ImageModel { FileInfo = new FileInfo(second), PixelWidth = 8, PixelHeight = 8 });
            }

            Assert.True(await commit);
            Assert.Equal("a bird", viewModel.SdPrompt.Value);
            Assert.Equal("a cat", SdMetadataReader.Read(new FileInfo(first))?.Prompt);
            Assert.Equal("a bird", SdMetadataReader.Read(new FileInfo(second))?.Prompt);
        }
        finally
        {
            File.Delete(first);
            File.Delete(second);
        }
    }

    private static void AssertShowsFixedPrompt(ExifViewModel viewModel, string path, string? comment)
    {
        const string expected = "a cat\nNegative prompt: blurry\n" + Settings;
        Assert.Equal("a cat", viewModel.SdPrompt.Value);
        Assert.Equal("blurry", viewModel.SdNegativePrompt.Value);
        Assert.Equal(Settings, viewModel.SdSettings.Value);
        Assert.Equal(expected, viewModel.SdRaw.Value);

        var stored = SdMetadataReader.Read(new FileInfo(path), comment, null);
        Assert.NotNull(stored);
        Assert.Equal(expected, stored.Raw);
        Assert.Equal(Settings, stored.Settings);
    }

    private static ExifViewModel Show(string path)
    {
        var viewModel = new ExifViewModel();
        viewModel.UpdateExifValues(new ImageModel { FileInfo = new FileInfo(path), PixelWidth = 8, PixelHeight = 8 });
        return viewModel;
    }

    /// <summary>A decodable 8x8 PNG, with a tEXt chunk when <paramref name="keyword"/> is given.</summary>
    private static string WritePng(string? keyword, string? text)
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview_sd_vm_{Guid.NewGuid():N}.png");
        using (var image = new MagickImage(MagickColors.Gray, 8, 8))
        {
            image.Format = MagickFormat.Png;
            image.Write(path);
        }

        if (keyword is not null)
        {
            File.WriteAllBytes(path, SdTestFiles.InsertTextChunk(File.ReadAllBytes(path), keyword, text ?? string.Empty));
        }

        return path;
    }

    private static string ReadUserComment(string path)
    {
        // A full read: Ping does not give the profile of a WebP
        using var image = new MagickImage(path);
        return ExifReader.GetUserComment(image.GetExifProfile());
    }
}
