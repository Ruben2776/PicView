using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageMagick;
using PicView.Avalonia.AnimatedImage;
using PicView.Core.Animation;
using PicView.Core.ImageDecoding;

namespace PicView.Tests.AnimatedImage;

public class AnimationDecoderTests
{
    [AvaloniaTheory]
    [InlineData(ImageType.AnimatedGif, "gif")]
    [InlineData(ImageType.AnimatedWebp, "webp")]
    [InlineData(ImageType.AnimatedAvif, "avif")]
    public void Create_ThreeFrameFile_ReportsSizeAndDurations(ImageType type, string extension)
    {
        var path = CreateAnimatedFile(extension, 10);

        try
        {
            using var stream = File.OpenRead(path);
            using var decoder = AnimationDecoders.Create(type, stream);

            Assert.Equal(new PixelSize(16, 16), decoder.Size);
            Assert.Equal(3, decoder.FrameDurations.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [AvaloniaTheory]
    [InlineData(ImageType.AnimatedGif, "gif")]
    [InlineData(ImageType.AnimatedWebp, "webp")]
    [InlineData(ImageType.AnimatedAvif, "avif")]
    public void RenderFrame_OutOfOrder_DrawsTheAskedFrame(ImageType type, string extension)
    {
        var path = CreateAnimatedFile(extension, 10);

        try
        {
            using var stream = File.OpenRead(path);
            using var decoder = AnimationDecoders.Create(type, stream);
            using var target = new WriteableBitmap(decoder.Size, new Vector(96, 96), PixelFormat.Bgra8888,
                decoder.AlphaFormat);

            decoder.RenderFrame(2, target);
            Assert.Equal("blue", DominantChannelAtCenter(target));

            decoder.RenderFrame(0, target);
            Assert.Equal("red", DominantChannelAtCenter(target));

            decoder.RenderFrame(1, target);
            Assert.Equal("green", DominantChannelAtCenter(target));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void Create_WebpWithZeroDurations_ReportsThemAsZeroAndRenders()
    {
        var path = CreateAnimatedFile("webp", 0);
        ZeroWebpFrameDurations(path);

        try
        {
            using var stream = File.OpenRead(path);
            using var decoder = AnimationDecoders.Create(ImageType.AnimatedWebp, stream);
            using var target = new WriteableBitmap(decoder.Size, new Vector(96, 96), PixelFormat.Bgra8888,
                decoder.AlphaFormat);

            Assert.All(decoder.FrameDurations, duration => Assert.True(duration <= TimeSpan.Zero));
            decoder.RenderFrame(1, target);
            Assert.Equal(TimeSpan.FromMilliseconds(300), new FrameTimeline(decoder.FrameDurations).Total);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [AvaloniaTheory]
    [InlineData(ImageType.AnimatedGif, "gif")]
    [InlineData(ImageType.AnimatedWebp, "webp")]
    [InlineData(ImageType.AnimatedAvif, "avif")]
    public void RenderFrame_SameIndexIntoFreshTarget_DrawsTheFrameThere(ImageType type, string extension)
    {
        var path = CreateAnimatedFile(extension, 10);

        try
        {
            using var stream = File.OpenRead(path);
            using var decoder = AnimationDecoders.Create(type, stream);
            using var first = new WriteableBitmap(decoder.Size, new Vector(96, 96), PixelFormat.Bgra8888,
                decoder.AlphaFormat);
            using var second = new WriteableBitmap(decoder.Size, new Vector(96, 96), PixelFormat.Bgra8888,
                decoder.AlphaFormat);

            decoder.RenderFrame(1, first);
            decoder.RenderFrame(1, second);

            Assert.Equal("green", DominantChannelAtCenter(second));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [AvaloniaTheory]
    [InlineData(ImageType.AnimatedGif, "gif")]
    [InlineData(ImageType.AnimatedWebp, "webp")]
    [InlineData(ImageType.AnimatedAvif, "avif")]
    public void RenderFrame_IndexOutOfRange_Throws(ImageType type, string extension)
    {
        var path = CreateAnimatedFile(extension, 10);

        try
        {
            using var stream = File.OpenRead(path);
            using var decoder = AnimationDecoders.Create(type, stream);
            using var target = new WriteableBitmap(decoder.Size, new Vector(96, 96), PixelFormat.Bgra8888,
                decoder.AlphaFormat);

            Assert.ThrowsAny<ArgumentOutOfRangeException>(() => decoder.RenderFrame(3, target));
            Assert.ThrowsAny<ArgumentOutOfRangeException>(() => decoder.RenderFrame(-1, target));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void Create_StillImageType_Throws()
    {
        using var stream = new MemoryStream();

        Assert.Throws<ArgumentOutOfRangeException>(() => AnimationDecoders.Create(ImageType.Bitmap, stream));
    }

    // Magick.NET writes its 100 ms default for a zero delay, so the durations are zeroed in the file itself
    private static void ZeroWebpFrameDurations(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var marker = "ANMF"u8;
        var offset = 0;
        while (true)
        {
            var found = bytes.AsSpan(offset).IndexOf(marker);
            if (found < 0)
            {
                break;
            }

            // The chunk header is 8 bytes; the 3-byte duration follows 12 bytes of frame position and size
            var duration = offset + found + 8 + 12;
            bytes[duration] = bytes[duration + 1] = bytes[duration + 2] = 0;
            offset += found + marker.Length;
        }

        File.WriteAllBytes(path, bytes);
    }

    private static string CreateAnimatedFile(string extension, uint delay)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{extension}");
        var format = extension switch
        {
            "gif" => MagickFormat.Gif,
            "webp" => MagickFormat.WebP,
            _ => MagickFormat.Avif
        };

        using var collection = new MagickImageCollection();
        foreach (var color in new[] { MagickColors.Red, MagickColors.Green, MagickColors.Blue })
        {
            collection.Add(new MagickImage(color, 16, 16) { AnimationDelay = delay, Format = format });
        }

        collection.Write(path, format);
        return path;
    }

    private static string DominantChannelAtCenter(WriteableBitmap bitmap)
    {
        using var buffer = bitmap.Lock();
        var pixel = new byte[4];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address + 8 * buffer.RowBytes + 8 * 4, pixel, 0, 4);
        int blue = pixel[0], green = pixel[1], red = pixel[2];

        if (red > green && red > blue)
        {
            return "red";
        }

        return green > blue ? "green" : "blue";
    }
}
