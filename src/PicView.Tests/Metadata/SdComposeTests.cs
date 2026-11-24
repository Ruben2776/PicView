using PicView.Core.Metadata;

namespace PicView.Tests.Metadata;

public class SdComposeTests
{
    private const string Sample =
        "a cat wearing a hat, masterpiece\n" +
        "Negative prompt: blurry, worst quality\n" +
        "Steps: 25, Sampler: DPM++ 2M Karras, CFG scale: 7, Seed: 1234567890, Size: 512x768";

    [Fact]
    public void Compose_AllParts_WritesA1111Layout()
    {
        Assert.Equal("a cat\nNegative prompt: blurry\nSteps: 20, Sampler: Euler a",
            SdMetadataParser.ComposeA1111("a cat", "blurry", "Steps: 20, Sampler: Euler a"));
    }

    [Fact]
    public void Compose_NoNegative_LeavesTheLineOut()
    {
        Assert.Equal("a cat\nSteps: 20, Seed: 1", SdMetadataParser.ComposeA1111("a cat", "", "Steps: 20, Seed: 1"));
    }

    [Fact]
    public void Compose_CrLf_IsNormalized()
    {
        Assert.Equal("a\nb\nSteps: 20, Seed: 1", SdMetadataParser.ComposeA1111("a\r\nb", null, "Steps: 20, Seed: 1"));
    }

    [Theory]
    [InlineData("a cat", "blurry", "Steps: 20, Seed: 1")]
    [InlineData("line one\nline two", "bad", "Steps: 20, Seed: 1")]
    [InlineData("(masterpiece:1.2), <lora:x:0.8>", "blurry", "Steps: 20, Sampler: Euler a")]
    public void ComposeThenParse_RoundTrips(string prompt, string negative, string settings)
    {
        var parsed = SdMetadataParser.ParseA1111(SdMetadataParser.ComposeA1111(prompt, negative, settings));

        Assert.Equal(prompt, parsed.Prompt);
        Assert.Equal(negative, parsed.NegativePrompt);
        Assert.Equal(settings, parsed.Settings);
    }

    [Fact]
    public void ComposeParsedSample_GivesBackTheStoredText()
    {
        var parsed = SdMetadataParser.ParseA1111(Sample);

        Assert.Equal(Sample, SdMetadataParser.ComposeA1111(parsed.Prompt, parsed.NegativePrompt, parsed.Settings));
    }

    [Theory]
    [InlineData(SdSource.PngText, "Negative prompt: x", "", "Steps: 20, Seed: 1")]
    [InlineData(SdSource.ExifUserComment, "Negative prompt: x", "", "Steps: 20, Seed: 1")]
    [InlineData(SdSource.PngText, "a cat", "", "just words")]
    [InlineData(SdSource.ExifUserComment, "a cat", "", "just words")]
    [InlineData(SdSource.PngText, "", "", "")]
    [InlineData(SdSource.ExifUserComment, "", "", "")]
    public void ReadsBackAs_Malformed_IsFalse(SdSource source, string prompt, string negative, string settings)
    {
        var text = SdMetadataParser.ComposeA1111(prompt, negative, settings);

        Assert.False(SdMetadataReader.ReadsBackAs(source, text, prompt, negative, settings));
    }

    [Theory]
    [InlineData(SdSource.PngText)]
    [InlineData(SdSource.ExifUserComment)]
    public void ReadsBackAs_WellFormed_IsTrue(SdSource source)
    {
        const string prompt = "a cat";
        const string negative = "blurry";
        const string settings = "Steps: 20, Sampler: Euler a";
        var text = SdMetadataParser.ComposeA1111(prompt, negative, settings);

        Assert.True(SdMetadataReader.ReadsBackAs(source, text, prompt, negative, settings));
    }
}
