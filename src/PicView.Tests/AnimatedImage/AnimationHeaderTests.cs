using System.Buffers.Binary;
using System.Text;
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
    [InlineData(MagickFormat.Avif, 3, true)]
    public void HeaderCheck_KnownFormat_AnswersLikeTheMagickPing(MagickFormat format, int frames, bool animated)
    {
        var path = Write(format, frames);

        Assert.Equal(animated, ImageAnalyzer.TryDetectAnimationFromHeader(path));
        Assert.Equal(animated, ImageAnalyzer.GetImageFrames(path) > 1);
        Assert.Equal(animated, ImageAnalyzer.IsAnimated(new FileInfo(path)));
    }

    [Theory]
    [InlineData(1u, false)]
    [InlineData(2u, true)]
    [InlineData(500u, true)]
    public void HeaderCheck_AvifSequence_CountsTheSamplesOfItsPictureTrack(uint samples, bool animated)
    {
        var path = Path.Combine(_directory, $"sequence{samples}.avif");
        File.WriteAllBytes(path, [.. SequenceFileType(), .. Box("mdat", new byte[64]), .. Movie(Track("pict", samples))]);

        Assert.Equal(animated, ImageAnalyzer.TryDetectAnimationFromHeader(path));
    }

    [Fact]
    public void HeaderCheck_AvifSequenceWithAlphaTrack_CountsOnlyThePictureTrack()
    {
        var path = Path.Combine(_directory, "alpha.avif");
        File.WriteAllBytes(path, [.. SequenceFileType(), .. Movie([.. Track("auxv", 9), .. Track("pict", 1)])]);

        Assert.False(ImageAnalyzer.TryDetectAnimationFromHeader(path));
    }

    [Fact]
    public void HeaderCheck_AvifSequenceBehindLargeSizeBox_IsFound()
    {
        var path = Path.Combine(_directory, "large.avif");
        byte[] largeData = [0, 0, 0, 1, .. "mdat"u8, 0, 0, 0, 0, 0, 0, 0, 24, 1, 2, 3, 4, 5, 6, 7, 8];
        File.WriteAllBytes(path, [.. SequenceFileType(), .. largeData, .. Movie(Track("pict", 4))]);

        Assert.True(ImageAnalyzer.TryDetectAnimationFromHeader(path));
    }

    [Fact]
    public void HeaderCheck_AvifSequenceWithoutMovie_GivesNoAnswer()
    {
        var path = Path.Combine(_directory, "nomovie.avif");
        File.WriteAllBytes(path, [.. SequenceFileType(), .. Box("mdat", new byte[64])]);

        Assert.Null(ImageAnalyzer.TryDetectAnimationFromHeader(path));
    }

    [Fact]
    public void HeaderCheck_AvifSequenceWithoutSamples_GivesNoAnswer()
    {
        var path = Path.Combine(_directory, "fragmented.avif");
        File.WriteAllBytes(path, [.. SequenceFileType(), .. Movie(Track("pict", 0))]);

        Assert.Null(ImageAnalyzer.TryDetectAnimationFromHeader(path));
    }

    [Fact]
    public void HeaderCheck_AvifSequenceWithBoxLongerThanTheFile_GivesNoAnswer()
    {
        var path = Path.Combine(_directory, "damaged.avif");
        File.WriteAllBytes(path, [.. SequenceFileType(), 0, 0, 1, 0, .. "moov"u8, 0, 0, 0, 0]);

        Assert.Null(ImageAnalyzer.TryDetectAnimationFromHeader(path));
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

    private static byte[] SequenceFileType() => Box("ftyp", [.. "avis"u8, 0, 0, 0, 0, .. "avis"u8, .. "msf1"u8]);

    private static byte[] Movie(byte[] tracks) => Box("moov", [.. Box("mvhd", new byte[100]), .. tracks]);

    /// <summary>
    /// A track as an ISO media file lays it out, down to the sample size box that holds the count.
    /// </summary>
    private static byte[] Track(string handler, uint samples)
    {
        var sampleCount = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(sampleCount, samples);
        var sampleSizes = Box("stsz", [0, 0, 0, 0, 0, 0, 0, 0, .. sampleCount]);
        var sampleTable = Box("stbl", [.. Box("stsd", new byte[8]), .. sampleSizes]);
        var handlerBox = Box("hdlr", [0, 0, 0, 0, 0, 0, 0, 0, .. Encoding.ASCII.GetBytes(handler), .. new byte[13]]);
        var media = Box("mdia", [.. Box("mdhd", new byte[24]), .. handlerBox, .. Box("minf", sampleTable)]);
        return Box("trak", [.. Box("tkhd", new byte[84]), .. media]);
    }

    private static byte[] Box(string type, byte[] content)
    {
        var size = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(size, (uint)(content.Length + 8));
        return [.. size, .. Encoding.ASCII.GetBytes(type), .. content];
    }

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
