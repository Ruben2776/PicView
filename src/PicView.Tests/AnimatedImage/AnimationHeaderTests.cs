using ImageMagick;
using PicView.Core.ImageDecoding;

namespace PicView.Tests.AnimatedImage;

/// <summary>
/// Pins that <see cref="ImageAnalyzer"/> tells an animation from a still by the file's header, without the ImageMagick
/// ping that decodes every frame, and that the two agree.
/// </summary>
public class AnimationHeaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "PicViewAnimationHeaderTests_" + Guid.NewGuid());

    public AnimationHeaderTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Theory]
    [InlineData(MagickFormat.WebP, 1, false)]
    [InlineData(MagickFormat.WebP, 3, true)]
    [InlineData(MagickFormat.Gif, 1, false)]
    [InlineData(MagickFormat.Gif, 3, true)]
    [InlineData(MagickFormat.Avif, 1, false)]
    public void HeaderCheck_KnownFormat_AnswersLikeTheMagickPing(MagickFormat format, int frames, bool animated)
    {
        var path = Write(format, frames);

        Assert.Equal(animated, ImageAnalyzer.TryDetectAnimationFromHeader(path));
        Assert.Equal(animated, ImageAnalyzer.GetImageFrames(path) > 1);
        Assert.Equal(animated, ImageAnalyzer.IsAnimated(new FileInfo(path)));
    }

    [Fact]
    public void HeaderCheck_AvifSequence_LeavesTheAnswerToTheMagickPing()
    {
        var path = Write(MagickFormat.Avif, 3);

        Assert.Null(ImageAnalyzer.TryDetectAnimationFromHeader(path));
        Assert.True(ImageAnalyzer.IsAnimated(new FileInfo(path)));
    }

    [Fact]
    public void HeaderCheck_LossyWebpWithoutExtendedHeader_IsNotAnimated()
    {
        var path = Path.Combine(_directory, "simple.webp");
        File.WriteAllBytes(path, [.. "RIFF"u8, 12, 0, 0, 0, .. "WEBP"u8, .. "VP8 "u8, 0, 0, 0, 0]);

        Assert.False(ImageAnalyzer.TryDetectAnimationFromHeader(path));
    }

    [Fact]
    public void HeaderCheck_AnimationFlagWithOneFrame_IsNotAnimated()
    {
        var path = Path.Combine(_directory, "oneframe.webp");
        File.WriteAllBytes(path,
        [
            .. "RIFF"u8, 38, 0, 0, 0, .. "WEBP"u8,
            .. "VP8X"u8, 10, 0, 0, 0, 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            .. "ANMF"u8, 0, 0, 0, 0
        ]);

        Assert.False(ImageAnalyzer.TryDetectAnimationFromHeader(path));
    }

    [Theory]
    [InlineData("truncated.gif", new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a', 1, 0 })]
    [InlineData("unknown.bin", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 })]
    [InlineData("empty.webp", new byte[0])]
    public void HeaderCheck_FileItCannotRead_GivesNoAnswer(string name, byte[] bytes)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllBytes(path, bytes);

        Assert.Null(ImageAnalyzer.TryDetectAnimationFromHeader(path));
    }

    [Fact]
    public void HeaderCheck_MissingFile_GivesNoAnswer() =>
        Assert.Null(ImageAnalyzer.TryDetectAnimationFromHeader(Path.Combine(_directory, "missing.webp")));

    private string Write(MagickFormat format, int frames)
    {
        var path = Path.Combine(_directory, $"{frames}.{format.ToString().ToLowerInvariant()}");
        using var collection = new MagickImageCollection();
        var colors = new[] { MagickColors.Red, MagickColors.Green, MagickColors.Blue };
        for (var i = 0; i < frames; i++)
        {
            collection.Add(new MagickImage(colors[i % colors.Length], 16, 16) { AnimationDelay = 10, Format = format });
        }

        collection.Write(path, format);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // A temporary directory another process still reads is left for the system to clean
        }

        GC.SuppressFinalize(this);
    }
}
