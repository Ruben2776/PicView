using ImageMagick;
using PicView.Core.ImageDecoding;

namespace PicView.Tests.AnimatedImage;

public class AvifAnimationTests
{
    [Fact]
    public void AnimatedAvif_IsDetectedAndDecodedByMagick()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.avif");

        try
        {
            CreateAnimatedAvif(filePath);

            Assert.True(ImageAnalyzer.IsAnimated(new FileInfo(filePath)));

            using var collection = new MagickImageCollection(filePath);
            collection.Coalesce();

            Assert.True(collection.Count > 1);

            foreach (var frame in collection)
            {
                Assert.Equal(16u, frame.Width);
                Assert.Equal(16u, frame.Height);

                if (!frame.HasAlpha)
                {
                    frame.Alpha(AlphaOption.Opaque);
                }

                using var pixels = frame.GetPixels();
                var bytes = pixels.ToByteArray(PixelMapping.BGRA);

                Assert.NotNull(bytes);
                Assert.Equal(16 * 16 * 4, bytes.Length);
            }
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    private static void CreateAnimatedAvif(string filePath)
    {
        using var collection = new MagickImageCollection();

        foreach (var color in new[] { MagickColors.Red, MagickColors.Green, MagickColors.Blue })
        {
            var frame = new MagickImage(color, 16, 16)
            {
                AnimationDelay = 10,
                Format = MagickFormat.Avif
            };
            collection.Add(frame);
        }

        collection.Write(filePath, MagickFormat.Avif);
    }
}
