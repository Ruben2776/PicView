using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageMagick;
using PicView.Avalonia.AnimatedImage;
using PicView.Avalonia.MotionPhoto;
using PicView.Core.ImageDecoding;

namespace PicView.Tests.AnimatedImage;

/// <summary>
/// Pins how <see cref="FFmpegAvifAnimationDecoder" /> gets any frame out of a video that only goes forward, which a
/// worker thread decodes ahead, and that an AVIF the bundled library cannot decode is left to ImageMagick.
/// </summary>
public class FFmpegAvifAnimationDecoderTests
{
    private static string SampleVideoPath => Path.Combine(
        AppContext.BaseDirectory, "MotionPhoto", "Samples", "sample_h264.mp4");

    [AvaloniaFact]
    public void RenderFrame_FramesInOrderRoundAndRound_DrawsEachOne()
    {
        var source = new FakeFrameSource(frames: 5);
        using var decoder = new FFmpegAvifAnimationDecoder(source, Durations(5));
        using var target = NewTarget(decoder);

        for (var i = 0; i < 15; i++)
        {
            decoder.RenderFrame(i % 5, target);
            Assert.Equal(i % 5, FrameIn(target));
        }

        Assert.True(source.Restarts >= 2);
    }

    [AvaloniaFact]
    public void RenderFrame_SameFrameIntoFreshTarget_DrawsItThere()
    {
        var source = new FakeFrameSource(frames: 5);
        using var decoder = new FFmpegAvifAnimationDecoder(source, Durations(5));
        using var first = NewTarget(decoder);
        using var second = NewTarget(decoder);

        decoder.RenderFrame(2, first);
        decoder.RenderFrame(2, second);

        Assert.Equal(2, FrameIn(second));
    }

    [AvaloniaFact]
    public void RenderFrame_FrameFurtherAheadThanIsDecoded_WaitsForIt()
    {
        var source = new FakeFrameSource(frames: 40);
        using var decoder = new FFmpegAvifAnimationDecoder(source, Durations(40));
        using var target = NewTarget(decoder);

        decoder.RenderFrame(30, target);
        Assert.Equal(30, FrameIn(target));

        decoder.RenderFrame(31, target);
        Assert.Equal(31, FrameIn(target));
    }

    [AvaloniaFact]
    public void RenderFrame_EarlierFrame_StartsTheVideoOver()
    {
        var source = new FakeFrameSource(frames: 5);
        using var decoder = new FFmpegAvifAnimationDecoder(source, Durations(5));
        using var target = NewTarget(decoder);

        decoder.RenderFrame(4, target);
        decoder.RenderFrame(0, target);
        Assert.Equal(0, FrameIn(target));

        decoder.RenderFrame(1, target);
        Assert.Equal(1, FrameIn(target));
        Assert.True(source.Restarts >= 1);
    }

    [AvaloniaFact]
    public void RenderFrame_VideoShorterThanTheContainerSays_ThrowsAndLoopsAgain()
    {
        var source = new FakeFrameSource(frames: 2);
        using var decoder = new FFmpegAvifAnimationDecoder(source, Durations(4));
        using var target = NewTarget(decoder);

        decoder.RenderFrame(1, target);
        Assert.Throws<InvalidDataException>(() => decoder.RenderFrame(2, target));
        Assert.Throws<InvalidDataException>(() => decoder.RenderFrame(3, target));

        decoder.RenderFrame(0, target);
        Assert.Equal(0, FrameIn(target));
    }

    [AvaloniaFact]
    public void RenderFrame_IndexOutOfRange_Throws()
    {
        using var decoder = new FFmpegAvifAnimationDecoder(new FakeFrameSource(frames: 3), Durations(3));
        using var target = NewTarget(decoder);

        Assert.Throws<ArgumentOutOfRangeException>(() => decoder.RenderFrame(3, target));
        Assert.Throws<ArgumentOutOfRangeException>(() => decoder.RenderFrame(-1, target));
    }

    [Fact]
    public void Create_SourceWithoutAFirstFrame_ThrowsAndDisposesTheSource()
    {
        var source = new FakeFrameSource(frames: 0);

        Assert.Throws<InvalidDataException>(() => new FFmpegAvifAnimationDecoder(source, Durations(3)));
        Assert.True(source.IsDisposed);
    }

    [Fact]
    public async Task Dispose_Decoder_EndsTheWorkerWhichDisposesTheSource()
    {
        var source = new FakeFrameSource(frames: 3);
        var decoder = new FFmpegAvifAnimationDecoder(source, Durations(3));

        decoder.Dispose();
        await decoder.Completion.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        Assert.True(source.IsDisposed);
    }

    [AvaloniaFact]
    public void RenderFrame_AfterDispose_Throws()
    {
        var decoder = new FFmpegAvifAnimationDecoder(new FakeFrameSource(frames: 3), Durations(3));
        using var target = NewTarget(decoder);

        decoder.Dispose();

        Assert.Throws<ObjectDisposedException>(() => decoder.RenderFrame(1, target));
    }

    [AvaloniaFact]
    public void Create_Avif_PlaysWhetherOrNotTheLibraryDecodesAv1()
    {
        // The bundled library decodes AV1 only when it was built with dav1d. Without it ImageMagick takes the file
        var path = WriteAvif();

        try
        {
            using var stream = File.OpenRead(path);
            using var decoder = AnimationDecoders.Create(ImageType.AnimatedAvif, stream);
            using var target = NewTarget(decoder);

            Assert.Equal(new PixelSize(16, 16), decoder.Size);
            Assert.Equal(3, decoder.FrameDurations.Count);
            decoder.RenderFrame(2, target);
            Assert.Equal("blue", DominantChannelAtCenter(target));
            decoder.RenderFrame(0, target);
            Assert.Equal("red", DominantChannelAtCenter(target));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void TryCreate_LibraryWithAv1_DecodesTheFramesInOrderAndLoops()
    {
        var path = WriteAvif();

        try
        {
            using var stream = File.OpenRead(path);
            using var decoder = FFmpegAvifAnimationDecoder.TryCreate(stream);
            if (decoder is null)
            {
                Assert.Skip("picview-ffmpeg is missing or was built without dav1d; run Build\\Build-FFmpegNative.ps1");
            }

            using var target = NewTarget(decoder);

            Assert.Equal(new PixelSize(16, 16), decoder.Size);
            Assert.Equal(3, decoder.FrameDurations.Count);
            foreach (var expected in new[] { "red", "green", "blue", "red", "green" })
            {
                decoder.RenderFrame(Array.IndexOf(["red", "green", "blue"], expected), target);
                Assert.Equal(expected, DominantChannelAtCenter(target));
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TryCreate_StreamThatIsNoSequence_GivesNoDecoder()
    {
        using var stream = new MemoryStream([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]);

        Assert.Null(FFmpegAvifAnimationDecoder.TryCreate(stream));
    }

    [Fact]
    public void FrameSource_SampleVideo_DecodesForwardAndStartsOver()
    {
        if (!FFmpegService.TryInitialize() || !File.Exists(SampleVideoPath))
        {
            Assert.Skip("picview-ffmpeg native library not built; run Build\\Build-FFmpegNative.ps1");
        }

        using var stream = File.OpenRead(SampleVideoPath);
        using var source = FFmpegFrameSource.TryOpen(stream);
        Assert.NotNull(source);

        var capacity = source.Size.Width * source.Size.Height * 4;
        var buffer = Marshal.AllocHGlobal(capacity);
        try
        {
            var first = Count(source, buffer, capacity);
            Assert.True(first > 1);

            Assert.True(source.Restart());
            Assert.Equal(first, Count(source, buffer, capacity));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [Fact]
    public void FrameSource_StreamThatIsNoVideo_GivesNoSource()
    {
        using var stream = new MemoryStream([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]);

        Assert.Null(FFmpegFrameSource.TryOpen(stream));
    }

    private static string WriteAvif()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.avif");
        using var collection = new MagickImageCollection();
        foreach (var color in new[] { MagickColors.Red, MagickColors.Green, MagickColors.Blue })
        {
            collection.Add(new MagickImage(color, 16, 16) { AnimationDelay = 10, Format = MagickFormat.Avif });
        }

        collection.Write(path, MagickFormat.Avif);
        return path;
    }

    private static string DominantChannelAtCenter(WriteableBitmap bitmap)
    {
        using var buffer = bitmap.Lock();
        var pixel = new byte[4];
        Marshal.Copy(buffer.Address + 8 * buffer.RowBytes + 8 * 4, pixel, 0, 4);
        int blue = pixel[0], green = pixel[1], red = pixel[2];

        if (red > green && red > blue)
        {
            return "red";
        }

        return green > blue ? "green" : "blue";
    }

    private static int Count(FFmpegFrameSource source, IntPtr buffer, int capacity)
    {
        var frames = 0;
        while (source.TryDecodeNext(buffer, capacity, out var size))
        {
            Assert.True(size.Width > 0 && size.Height > 0);
            frames++;
        }

        return frames;
    }

    private static TimeSpan[] Durations(int frames) =>
        Enumerable.Repeat(TimeSpan.FromMilliseconds(40), frames).ToArray();

    private static WriteableBitmap NewTarget(IAnimationDecoder decoder) =>
        new(decoder.Size, new Vector(96, 96), PixelFormat.Bgra8888, decoder.AlphaFormat);

    /// <summary>Reads back the frame number a <see cref="FakeFrameSource" /> filled the frame with.</summary>
    private static int FrameIn(WriteableBitmap bitmap)
    {
        using var buffer = bitmap.Lock();
        return Marshal.ReadByte(buffer.Address + buffer.RowBytes * (buffer.Size.Height - 1) + 4);
    }

    /// <summary>
    /// A video of <paramref name="frames" /> frames, each filled with its own number.
    /// </summary>
    private sealed class FakeFrameSource(int frames) : IVideoFrameSource
    {
        private int _next;

        public int Decoded { get; private set; }

        public int Restarts { get; private set; }

        public bool IsDisposed { get; private set; }

        public PixelSize Size => new(4, 4);

        public bool TryDecodeNext(IntPtr destination, int capacity, out PixelSize frameSize)
        {
            frameSize = Size;
            if (_next >= frames)
            {
                return false;
            }

            var pixels = new byte[capacity];
            Array.Fill(pixels, (byte)_next);
            Marshal.Copy(pixels, 0, destination, capacity);
            _next++;
            Decoded++;
            return true;
        }

        public bool Restart()
        {
            _next = 0;
            Restarts++;
            return true;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
