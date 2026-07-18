using PicView.Avalonia.AnimatedImage.Decoding;
using PicView.Avalonia.ImageHandling;

namespace PicView.Tests.AnimatedImage;

public class GifDecoderTests
{
    [Fact]
    public void Constructor_DanglingExtensionIntroducer_PreservesCompleteFrames()
    {
        using var stream = new MemoryStream(GetGifWithDanglingExtension());

        using var decoder = new GifDecoder(stream, CancellationToken.None);

        Assert.Equal(2, decoder.Frames.Count);
    }

    [Fact]
    public void IsAnimatedGif_DanglingExtensionIntroducer_ReturnsTrue()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.gif");

        try
        {
            File.WriteAllBytes(filePath, GetGifWithDanglingExtension());

            Assert.True(GetImageModel.IsAnimatedGif(new FileInfo(filePath)));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static byte[] GetGifWithDanglingExtension() => Convert.FromBase64String(
        "R0lGODlhAgABAIEAAAAAAAAAAAAAAAAAACH/C05FVFNDQVBFMi4wAwEAAAAh+QQABwAAACwAAAAAAgABAAAIBQABAAgIACH5BAEHAAEALAAAAAACAAEAgf///wAAAAAAAAAAAAgFAAEACAgAIQ==");
}
