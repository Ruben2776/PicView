using PicView.Avalonia.MotionPhoto;

namespace PicView.Tests.MotionPhoto;

/// <summary>
/// Integration tests for the statically-linked picview-ffmpeg decoder. These load the
/// bundled native library and decode a small committed H.264 sample, so they exercise
/// the real demux/decode/scale pipeline end to end. They are skipped automatically
/// when the native library has not been built (Build\Build-FFmpegNative.ps1).
/// </summary>
public class MotionPhotoDecoderTests
{
    private static string NativeLibraryPath => Path.Combine(
        AppContext.BaseDirectory, "ffmpeg", "win-x64", "picview-ffmpeg.dll");

    private static string SampleVideoPath => Path.Combine(
        AppContext.BaseDirectory, "MotionPhoto", "Samples", "sample_h264.mp4");

    private static void SkipUnlessNativeAvailable()
    {
        if (!File.Exists(NativeLibraryPath))
        {
            Assert.Skip("picview-ffmpeg native library not built; run Build\\Build-FFmpegNative.ps1");
        }
    }

    [Fact]
    public void FFmpegService_TryInitialize_WhenLibraryPresent_ReturnsTrue()
    {
        SkipUnlessNativeAvailable();
        Assert.True(FFmpegService.TryInitialize());
    }

    [Fact]
    public void FFmpegService_IsPlaybackSupported_OnDesktop_IsTrue()
    {
        Assert.True(FFmpegService.IsPlaybackSupported);
    }

    [Fact]
    public void Decoder_CreateAndDecodeSampleVideo_ProducesBgraFrames()
    {
        SkipUnlessNativeAvailable();
        if (!File.Exists(SampleVideoPath))
        {
            Assert.Skip("sample video missing");
        }

        using var stream = File.OpenRead(SampleVideoPath);
        var decoder = MotionPhotoDecoder.Create(stream);
        Assert.NotNull(decoder);
        Assert.True(decoder!.Width > 0);
        Assert.True(decoder.Height > 0);

        var frames = new List<(IntPtr buffer, int byteCount)>();
        var frameReady = new ManualResetEventSlim(false);
        var finished = new ManualResetEventSlim(false);
        var failed = false;

        decoder.FrameReady += (index, buffer, byteCount, width, height) =>
        {
            frames.Add((buffer, byteCount));
            Assert.Equal(width * height * 4, byteCount);
            decoder.ReleaseBuffer(index);
        };
        decoder.Ended += (_, _) => finished.Set();
        decoder.Failed += (_, _) =>
        {
            failed = true;
            finished.Set();
        };

        decoder.Play();
        Assert.True(finished.Wait(TimeSpan.FromSeconds(20)), "playback did not finish in time");
        Assert.False(failed, "decoding failed");
        Assert.True(frames.Count > 0, "no frames decoded");
        Assert.All(frames, f => Assert.True(f.byteCount > 0));

        decoder.Dispose();
    }

    [Fact]
    public void Decoder_CreateOnNonVideoStream_ReturnsNull()
    {
        SkipUnlessNativeAvailable();

        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
        var decoder = MotionPhotoDecoder.Create(stream);
        Assert.Null(decoder);
    }
}
