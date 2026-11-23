using PicView.Core.Metadata;

namespace PicView.Tests.Metadata;

public class SdMetadataTests
{
    private const string A1111Payload =
        "a cat wearing a hat, masterpiece, best quality\n" +
        "Negative prompt: blurry, worst quality\n" +
        "Steps: 25, Sampler: DPM++ 2M Karras, CFG scale: 7, Seed: 1234567890, Size: 512x768, Model: someModel";

    [Fact]
    public void ParseUnknownPayload_NovelAiStealthWrapper_ParsesAsNovelAi()
    {
        // NovelAI's stealth payload wraps its PNG chunks in one JSON object.
        const string payload =
            "{\"Description\": \"a cat\", \"Software\": \"NovelAI\", " +
            "\"Comment\": \"{\\\"prompt\\\": \\\"a cat\\\", \\\"uc\\\": \\\"blurry\\\", \\\"steps\\\": 28}\"}";

        var metadata = SdMetadataReader.ParseUnknownPayload(payload);

        Assert.NotNull(metadata);
        Assert.Equal("NovelAI", metadata.Generator);
        Assert.Equal("a cat", metadata.Prompt);
        Assert.Equal("blurry", metadata.NegativePrompt);
        Assert.Equal("Steps: 28", metadata.Settings);
    }

    [Fact]
    public void ParseA1111_FullPayload_SplitsPromptNegativeAndSettings()
    {
        var metadata = SdMetadataParser.ParseA1111(A1111Payload);

        Assert.Equal("Automatic1111", metadata.Generator);
        Assert.Equal("a cat wearing a hat, masterpiece, best quality", metadata.Prompt);
        Assert.Equal("blurry, worst quality", metadata.NegativePrompt);
        Assert.StartsWith("Steps: 25, Sampler: DPM++ 2M Karras", metadata.Settings);
        Assert.Equal(A1111Payload, metadata.Raw);
    }

    [Fact]
    public void ParseA1111_WithoutNegativePrompt_LeavesNegativeNull()
    {
        var metadata = SdMetadataParser.ParseA1111("a cat\nSteps: 20, Sampler: Euler a");

        Assert.Equal("a cat", metadata.Prompt);
        Assert.Null(metadata.NegativePrompt);
        Assert.Equal("Steps: 20, Sampler: Euler a", metadata.Settings);
    }

    [Fact]
    public void ParseA1111_MultiLinePrompt_KeepsEveryPromptLine()
    {
        var metadata = SdMetadataParser.ParseA1111(
            "line one, <lora:something:0.8>\nline two\nNegative prompt: bad\nSteps: 10");

        Assert.Equal("line one, <lora:something:0.8>\nline two", metadata.Prompt);
        Assert.Equal("bad", metadata.NegativePrompt);
    }

    [Fact]
    public void ParseA1111_PromptOnly_TreatsEverythingAsPrompt()
    {
        // Prompt weighting uses colons and commas, so it must not be mistaken for settings.
        var metadata = SdMetadataParser.ParseA1111("(a cat:1.2), (a hat:0.8), detailed");

        Assert.Equal("(a cat:1.2), (a hat:0.8), detailed", metadata.Prompt);
        Assert.Null(metadata.Settings);
    }

    [Fact]
    public void ParseComfyUi_FollowsSamplerLinksToTextEncoders()
    {
        const string graph =
            """
            {
              "3": {
                "class_type": "KSampler",
                "inputs": {
                  "seed": 42, "steps": 30, "cfg": 8.0,
                  "sampler_name": "euler", "scheduler": "normal",
                  "positive": ["6", 0], "negative": ["7", 0]
                }
              },
              "4": { "class_type": "CheckpointLoaderSimple", "inputs": { "ckpt_name": "sd_xl_base.safetensors" } },
              "6": { "class_type": "CLIPTextEncode", "inputs": { "text": "a scenic landscape" } },
              "7": { "class_type": "CLIPTextEncode", "inputs": { "text": "text, watermark" } }
            }
            """;

        var metadata = SdMetadataParser.ParseComfyUi(graph, null);

        Assert.NotNull(metadata);
        Assert.Equal("ComfyUI", metadata.Generator);
        Assert.Equal("a scenic landscape", metadata.Prompt);
        Assert.Equal("text, watermark", metadata.NegativePrompt);
        Assert.Contains("Steps: 30", metadata.Settings);
        Assert.Contains("Model: sd_xl_base.safetensors", metadata.Settings);
    }

    [Fact]
    public void ParseComfyUi_UnrelatedJson_ReturnsNull()
    {
        Assert.Null(SdMetadataParser.ParseComfyUi("""{ "hello": "world" }""", null));
    }

    [Fact]
    public void ParseNovelAi_ReadsPromptFromDescriptionAndNegativeFromComment()
    {
        var metadata = SdMetadataParser.ParseNovelAi(
            "1girl, forest",
            """{ "uc": "lowres, bad anatomy", "steps": 28, "scale": 11, "sampler": "k_euler_ancestral", "seed": 7 }""");

        Assert.NotNull(metadata);
        Assert.Equal("NovelAI", metadata.Generator);
        Assert.Equal("1girl, forest", metadata.Prompt);
        Assert.Equal("lowres, bad anatomy", metadata.NegativePrompt);
        Assert.Contains("Steps: 28", metadata.Settings);
        Assert.Contains("Seed: 7", metadata.Settings);
    }

    [Fact]
    public void ParseNovelAi_V4_JoinsBaseAndCharacterCaptionsOnSeparateLines()
    {
        var metadata = SdMetadataParser.ParseNovelAi(
            "2girls, beach",
            """
            {
              "prompt": "2girls, beach",
              "uc": "lowres",
              "v4_prompt": { "caption": {
                "base_caption": "2girls, beach",
                "char_captions": [
                  { "char_caption": "girl, red hair", "centers": [{ "x": 0.3, "y": 0.5 }] },
                  { "char_caption": "girl, blue hair", "centers": [{ "x": 0.7, "y": 0.5 }] }
                ] } },
              "v4_negative_prompt": { "caption": {
                "base_caption": "lowres",
                "char_captions": [ { "char_caption": "extra arms" }, { "char_caption": "" } ] } }
            }
            """);

        Assert.NotNull(metadata);
        Assert.Equal("2girls, beach\ngirl, red hair\ngirl, blue hair", metadata.Prompt);
        Assert.Equal("lowres\nextra arms", metadata.NegativePrompt);
    }

    [Fact]
    public void ParseNovelAi_V4WithNoCharacters_ReturnsBaseCaption()
    {
        var metadata = SdMetadataParser.ParseNovelAi(
            null,
            """{ "v4_prompt": { "caption": { "base_caption": "1girl", "char_captions": [] } } }""");

        Assert.NotNull(metadata);
        Assert.Equal("1girl", metadata.Prompt);
    }

    [Fact]
    public void Read_PngWithParametersChunk_ReturnsA1111Metadata()
    {
        var path = SdTestFiles.WritePng(("parameters", A1111Payload, false));
        try
        {
            var metadata = SdMetadataReader.Read(new FileInfo(path));

            Assert.NotNull(metadata);
            Assert.Equal("Automatic1111", metadata.Generator);
            Assert.Equal("a cat wearing a hat, masterpiece, best quality", metadata.Prompt);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_PngWithCompressedChunk_InflatesPayload()
    {
        var path = SdTestFiles.WritePng(("parameters", A1111Payload, true));
        try
        {
            var metadata = SdMetadataReader.Read(new FileInfo(path));

            Assert.NotNull(metadata);
            Assert.Equal("blurry, worst quality", metadata.NegativePrompt);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_PngWithoutTextChunks_ReturnsNull()
    {
        var path = SdTestFiles.WritePng();
        try
        {
            Assert.Null(SdMetadataReader.Read(new FileInfo(path)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_ExifUserCommentWithParameters_ReturnsA1111Metadata()
    {
        var metadata = SdMetadataReader.Read(fileInfo: null, exifUserComment: A1111Payload);

        Assert.NotNull(metadata);
        Assert.Equal("blurry, worst quality", metadata.NegativePrompt);
    }

    [Fact]
    public void Read_OrdinaryComment_IsNotMistakenForGenerationParameters()
    {
        Assert.Null(SdMetadataReader.Read(fileInfo: null, exifUserComment: "Holiday photo, taken at the beach"));
    }

    private const string ComfyPrompt =
        """{ "3": { "class_type": "KSampler", "inputs": { "seed": 1, "steps": 20, "cfg": 7.0, "sampler_name": "euler", "scheduler": "normal", "positive": ["6", 0], "negative": ["7", 0] } }, "6": { "class_type": "CLIPTextEncode", "inputs": { "text": "a cat" } }, "7": { "class_type": "CLIPTextEncode", "inputs": { "text": "blurry" } } }""";

    [Fact]
    public void Read_PngParametersChunk_IsEditableA1111FromPngText()
    {
        var path = SdTestFiles.WritePng(("parameters", A1111Payload, false));
        try
        {
            var metadata = SdMetadataReader.Read(new FileInfo(path));

            Assert.NotNull(metadata);
            Assert.Equal(SdFormat.A1111, metadata.Format);
            Assert.Equal(SdSource.PngText, metadata.Source);
            Assert.Equal(SdContainer.Png, metadata.Container);
            Assert.Equal("parameters", metadata.PngKeyword);
            Assert.True(metadata.IsEditable);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_PngComfyUiPrompt_IsNotEditable()
    {
        var path = SdTestFiles.WritePng(("prompt", ComfyPrompt, false));
        try
        {
            var metadata = SdMetadataReader.Read(new FileInfo(path));

            Assert.NotNull(metadata);
            Assert.Equal(SdFormat.ComfyUi, metadata.Format);
            Assert.False(metadata.IsEditable);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_A1111InPngCommentChunk_IsNotEditable()
    {
        var path = SdTestFiles.WritePng(("Comment", A1111Payload, false));
        try
        {
            var metadata = SdMetadataReader.Read(new FileInfo(path));

            Assert.NotNull(metadata);
            Assert.Equal(SdFormat.A1111, metadata.Format);
            Assert.Equal("Comment", metadata.PngKeyword);
            Assert.False(metadata.IsEditable);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_JpegUserCommentWithParameters_IsEditable()
    {
        var path = SdTestFiles.WriteJpegWithUserComment(SdTestFiles.AsciiUserComment(A1111Payload));
        try
        {
            var metadata = SdMetadataReader.Read(new FileInfo(path), exifUserComment: A1111Payload);

            Assert.NotNull(metadata);
            Assert.Equal(SdSource.ExifUserComment, metadata.Source);
            Assert.Equal(SdContainer.Jpeg, metadata.Container);
            Assert.True(metadata.IsEditable);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_WebPUserCommentWithParameters_IsEditable()
    {
        var path = SdTestFiles.WriteWebPWithUserComment(SdTestFiles.AsciiUserComment(A1111Payload));
        try
        {
            var metadata = SdMetadataReader.Read(new FileInfo(path), exifUserComment: A1111Payload);

            Assert.NotNull(metadata);
            Assert.Equal(SdContainer.WebP, metadata.Container);
            Assert.True(metadata.IsEditable);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_JpegCommentSegment_IsNotEditable()
    {
        var path = SdTestFiles.WriteJpegWithComment(A1111Payload);
        try
        {
            var metadata = SdMetadataReader.Read(new FileInfo(path), imageComment: A1111Payload);

            Assert.NotNull(metadata);
            Assert.Equal(SdSource.ImageComment, metadata.Source);
            Assert.Equal(SdContainer.Jpeg, metadata.Container);
            Assert.False(metadata.IsEditable);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
