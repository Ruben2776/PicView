using System.IO.Compression;
using System.Text;
using ImageMagick;
using PicView.Core.Metadata;

namespace PicView.Tests.Metadata;

public class StealthPngInfoTests
{
    private const string Payload =
        "a hidden prompt, masterpiece\n" +
        "Negative prompt: visible metadata\n" +
        "Steps: 28, Sampler: DPM++ 2M Karras, CFG scale: 7, Seed: 987654321";

    [Theory]
    [InlineData("stealth_pnginfo", false, true)]
    [InlineData("stealth_pngcomp", true, true)]
    [InlineData("stealth_rgbinfo", false, false)]
    [InlineData("stealth_rgbcomp", true, false)]
    public void ReadStealth_EverySignatureVariant_RecoversParameters(
        string signature,
        bool compressed,
        bool useAlpha)
    {
        var path = WriteStealthPng(signature, compressed, useAlpha);
        try
        {
            var metadata = SdMetadataReader.ReadStealth(new FileInfo(path));

            Assert.NotNull(metadata);
            Assert.Equal(SdSource.Pixels, metadata.Source);
            Assert.Equal("a hidden prompt, masterpiece", metadata.Prompt);
            Assert.Equal("visible metadata", metadata.NegativePrompt);
            Assert.Contains("Steps: 28", metadata.Settings);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadStealth_SetsPixelsSourceAndDetail()
    {
        var path = WriteStealthPng("stealth_pngcomp", compressed: true, useAlpha: true);
        try
        {
            var metadata = SdMetadataReader.ReadStealth(new FileInfo(path));

            Assert.NotNull(metadata);
            Assert.Equal(SdSource.Pixels, metadata.Source);
            Assert.Equal(SdContainer.Png, metadata.Container);
            Assert.False(metadata.IsEditable);
            Assert.Equal("alpha, compressed", metadata.StealthDetail);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // About 17 KB of gzip in the pixels that inflates to 17 MB is refused rather than allocated
    [Fact]
    public void ReadStealth_PayloadInflatingPastTheCap_ReturnsNull()
    {
        var text = new byte[17 * 1024 * 1024];
        Array.Fill(text, (byte)'a');
        var path = WriteStealthPng("stealth_rgbcomp", compressed: true, useAlpha: false, text);
        try
        {
            Assert.Null(SdMetadataReader.ReadStealth(new FileInfo(path)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadStealth_OrdinaryImage_ReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview_plain_{Guid.NewGuid():N}.png");
        using (var image = new MagickImage(MagickColors.CornflowerBlue, 64, 64))
        {
            image.Write(path);
        }

        try
        {
            Assert.Null(SdMetadataReader.ReadStealth(new FileInfo(path)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadStealth_MissingFile_ReturnsNull()
    {
        var missing = new FileInfo(Path.Combine(Path.GetTempPath(), $"picview_absent_{Guid.NewGuid():N}.png"));

        Assert.Null(SdMetadataReader.ReadStealth(missing));
    }

    [Fact]
    public void ReadStealth_CancelledBeforeStart_Throws()
    {
        var path = WriteStealthPng("stealth_pnginfo", compressed: false, useAlpha: true);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            Assert.Throws<OperationCanceledException>(
                () => SdMetadataReader.ReadStealth(new FileInfo(path), cts.Token));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Writes a PNG carrying a stealth payload, using the same layout the A1111 extension does:
    /// signature, then a 32-bit big-endian bit count, then the payload, written into the low bit
    /// of the chosen channels and walked in column-major order.
    /// </summary>
    private static string WriteStealthPng(string signature, bool compressed, bool useAlpha, byte[]? text = null)
    {
        var payload = text ?? Encoding.UTF8.GetBytes(Payload);
        if (compressed)
        {
            using var buffer = new MemoryStream();
            using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzip.Write(payload);
            }

            payload = buffer.ToArray();
        }

        var bits = new StringBuilder();
        AppendBits(bits, Encoding.ASCII.GetBytes(signature));
        bits.Append(Convert.ToString(payload.Length * 8, 2).PadLeft(32, '0'));
        AppendBits(bits, payload);
        var stream = bits.ToString();

        const int width = 256;
        const int height = 256;
        var channels = useAlpha ? 4 : 3;
        var pixels = new byte[width * height * channels];

        // A flat opaque image, so flipping low bits stays invisible.
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = (byte)(i % channels == 3 ? 255 : 128);
        }

        // Column-major, matching the reader: alpha mode uses the last channel, RGB mode uses
        // red, green and blue in turn.
        var bitChannels = useAlpha ? [3] : new[] { 0, 1, 2 };
        for (var index = 0; index < stream.Length; index++)
        {
            var pixelIndex = index / bitChannels.Length;
            var channel = bitChannels[index % bitChannels.Length];
            var x = pixelIndex / height;
            var y = pixelIndex % height;
            var offset = ((y * width) + x) * channels + channel;
            pixels[offset] = (byte)((pixels[offset] & 0xFE) | (stream[index] - '0'));
        }

        var settings = new PixelReadSettings(width, height, StorageType.Char,
            useAlpha ? PixelMapping.RGBA : PixelMapping.RGB);
        var path = Path.Combine(Path.GetTempPath(), $"picview_stealth_{Guid.NewGuid():N}.png");
        using var image = new MagickImage(pixels, settings);
        image.Write(path, MagickFormat.Png32);
        return path;
    }

    private static void AppendBits(StringBuilder builder, byte[] data)
    {
        foreach (var b in data)
        {
            builder.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
        }
    }
}
