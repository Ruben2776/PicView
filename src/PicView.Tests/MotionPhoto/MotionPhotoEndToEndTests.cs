using System.Text;
using PicView.Avalonia.MotionPhoto;
using PicView.Core.MotionPhoto;

namespace PicView.Tests.MotionPhoto;

/// <summary>
/// End-to-end pipeline test: synthesizes a Google/Samsung style motion photo
/// (JPEG + XMP + appended MP4), then runs detection, extraction and decoding.
/// </summary>
public class MotionPhotoEndToEndTests
{
    private static string SampleVideoPath => Path.Combine(
        AppContext.BaseDirectory, "MotionPhoto", "Samples", "sample_h264.mp4");

    /// <summary>Builds a minimal JPEG with the XMP packet in an APP1 segment.</summary>
    private static byte[] BuildJpegWithXmp(string xmp)
    {
        var xmpBytes = Encoding.UTF8.GetBytes(xmp);
        var nsHeader = "http://ns.adobe.com/xap/1.0/"u8;
        // APP1 payload: namespace header + NUL + packet
        var payloadLength = nsHeader.Length + 1 + xmpBytes.Length;

        using var ms = new MemoryStream();
        ms.WriteByte(0xFF); // SOI
        ms.WriteByte(0xD8);
        ms.WriteByte(0xFF); // APP1 marker
        ms.WriteByte(0xE1);
        ms.WriteByte((byte)((payloadLength + 2) >> 8));
        ms.WriteByte((byte)(payloadLength + 2));
        ms.Write(nsHeader);
        ms.WriteByte(0);
        ms.Write(xmpBytes);
        ms.WriteByte(0xFF); // EOI
        ms.WriteByte(0xD9);
        return ms.ToArray();
    }

    [Fact]
    public async Task SynthesizedMotionPhoto_Detects_Extracts_AndDecodes()
    {
        if (!FFmpegService.TryInitialize())
        {
            Assert.Skip("picview-ffmpeg native library not built; run Build\\Build-FFmpegNative.ps1");
        }

        if (!File.Exists(SampleVideoPath))
        {
            Assert.Skip("sample video missing");
        }

        var videoBytes = await File.ReadAllBytesAsync(SampleVideoPath);
        var jpeg = BuildJpegWithXmp(MotionPhotoFixtures.NewStandardXmp(videoBytes.Length));

        var directory = MotionPhotoFixtures.CreateTempDirectory();
        var filePath = Path.Combine(directory, "synthetic-motion-photo.jpg");
        try
        {
            await using (var output = File.Create(filePath))
            {
                await output.WriteAsync(jpeg);
                await output.WriteAsync(videoBytes);
            }

            var fileInfo = new FileInfo(filePath);

            // 1. Detection
            var info = MotionPhotoDetector.TryDetect(fileInfo, null);
            Assert.NotNull(info);
            Assert.Equal(MotionPhotoSource.EmbeddedXmp, info!.Source);
            Assert.Equal(fileInfo.Length - videoBytes.Length, info.VideoOffset);

            // 2. Extraction
            var stream = await MotionPhotoExtractor.ExtractAsync(fileInfo, info);
            Assert.NotNull(stream);
            await using (stream)
            {
                Assert.Equal(videoBytes.Length, stream!.Length);

                // 3. Decoding
                var decoder = MotionPhotoDecoder.Create(stream);
                Assert.NotNull(decoder);

                var frameCount = 0;
                var finished = new ManualResetEventSlim(false);
                var failed = false;
                decoder!.FrameReady += (index, _, byteCount, width, height) =>
                {
                    Assert.Equal(width * height * 4, byteCount);
                    Interlocked.Increment(ref frameCount);
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
                Assert.True(frameCount > 0, "no frames decoded");
                decoder.Dispose();
            }
        }
        finally
        {
            MotionPhotoFixtures.DeleteDirectory(directory);
        }
    }
}
