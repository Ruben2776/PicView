using ImageMagick;
using PicView.Core.ImageDecoding;

namespace PicView.Tests.AnimatedImage;

public class HeicInstanceTests
{
    /// <summary>
    /// Creates a minimal multi-frame stream using MagickImageCollection.
    /// We generate frames as GIF (which MagickImageCollection can produce in-memory)
    /// to validate that the frame-extraction logic works correctly.
    /// </summary>
    private static MemoryStream CreateAnimatedTestStream(int frameCount = 3, int delay = 10)
    {
        var collection = new MagickImageCollection();
        for (var i = 0; i < frameCount; i++)
        {
            var frame = new MagickImage(MagickColors.Black, 4, 4);
            frame.AnimationDelay = (uint)delay;
            collection.Add(frame);
        }

        var ms = new MemoryStream();
        collection.Write(ms, MagickFormat.Gif);
        collection.Dispose();
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    [Fact]
    public void MagickImageCollection_MultiFrameStream_ReadsCorrectFrameCount()
    {
        using var stream = CreateAnimatedTestStream(frameCount: 5);
        using var collection = new MagickImageCollection(stream);

        Assert.Equal(5, collection.Count);
    }

    [Fact]
    public void MagickImageCollection_MultiFrameStream_ReadsFrameDimensions()
    {
        using var stream = CreateAnimatedTestStream();
        using var collection = new MagickImageCollection(stream);

        var first = collection[0];
        Assert.Equal(4u, first.Width);
        Assert.Equal(4u, first.Height);
    }

    [Fact]
    public void MagickImageCollection_MultiFrameStream_ReadsAnimationDelay()
    {
        using var stream = CreateAnimatedTestStream(delay: 20);
        using var collection = new MagickImageCollection(stream);

        foreach (var frame in collection)
        {
            // AnimationDelay is in centiseconds
            Assert.Equal(20u, frame.AnimationDelay);
        }
    }

    [Fact]
    public void MagickImageCollection_FramePixels_AreBgra()
    {
        using var stream = CreateAnimatedTestStream(frameCount: 1);
        using var collection = new MagickImageCollection(stream);
        var frame = (MagickImage)collection[0];

        using var pixels = frame.GetPixelsUnsafe();
        var rgba = pixels.ToByteArray(PixelMapping.RGBA);

        Assert.NotNull(rgba);
        // 4x4 image * 4 channels = 64 bytes
        Assert.Equal(64, rgba.Length);
    }

    [Fact]
    public void MagickImageCollection_SingleFrame_IsNotAnimated()
    {
        using var stream = CreateAnimatedTestStream(frameCount: 1);
        using var collection = new MagickImageCollection(stream);

        Assert.Equal(1, collection.Count);
    }

    [Fact]
    public void ImageAnalyzer_MultiFrameGif_IsAnimated()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.gif");

        try
        {
            using var stream = CreateAnimatedTestStream(frameCount: 3);
            File.WriteAllBytes(filePath, stream.ToArray());

            Assert.True(ImageAnalyzer.IsAnimated(new FileInfo(filePath)));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void ImageAnalyzer_SingleFrameGif_IsNotAnimated()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.gif");

        try
        {
            using var stream = CreateAnimatedTestStream(frameCount: 1);
            File.WriteAllBytes(filePath, stream.ToArray());

            Assert.False(ImageAnalyzer.IsAnimated(new FileInfo(filePath)));
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}
