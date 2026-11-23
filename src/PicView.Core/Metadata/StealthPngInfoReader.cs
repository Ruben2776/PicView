using System.IO.Compression;
using System.Text;
using ImageMagick;
using PicView.Core.DebugTools;

namespace PicView.Core.Metadata;

/// <summary>
/// Reads "stealth pnginfo": generation parameters hidden in the least significant bits of the
/// pixels rather than in a metadata block, as written by the A1111 stealth_pnginfo extension.
/// It exists so the parameters survive sites that strip PNG text chunks, which is why an image
/// can look metadata-free and still carry a full prompt.
/// <para>
/// The bits are walked in column-major order and carry a 15-character signature, a 32-bit
/// big-endian bit count, then the payload. Alpha mode uses the alpha channel's low bit;
/// RGB mode uses the low bit of each of red, green and blue.
/// </para>
/// </summary>
internal static class StealthPngInfoReader
{
    private const int SignatureLength = 15;
    private const int SignatureBits = SignatureLength * 8;
    private const int LengthBits = 32;

    /// <summary>Refuse absurd declared lengths rather than allocating on a corrupt read.</summary>
    private const int MaxPayloadBytes = 32 * 1024 * 1024;

    internal sealed record StealthPayload(string Text, string Mode, bool Compressed);

    public static StealthPayload? Read(MagickImage image, CancellationToken cancellationToken = default)
    {
        try
        {
            using var pixels = image.GetPixels();
            var width = (int)image.Width;
            var height = (int)image.Height;
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            var channels = (int)image.ChannelCount;

            // Alpha mode is the common case; the extension falls back to RGB for images
            // without an alpha channel.
            if (image.HasAlpha && channels >= 2)
            {
                var payload = TryRead(
                    pixels, width, height, channels, [channels - 1],
                    "stealth_pnginfo", "stealth_pngcomp", "alpha", cancellationToken);
                if (payload is not null)
                {
                    return payload;
                }
            }

            if (channels >= 3)
            {
                return TryRead(
                    pixels, width, height, channels, [0, 1, 2],
                    "stealth_rgbinfo", "stealth_rgbcomp", "rgb", cancellationToken);
            }

            return null;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(StealthPngInfoReader), nameof(Read), e);
            return null;
        }
    }

    private static StealthPayload? TryRead(
        IPixelCollection<byte> pixels,
        int width,
        int height,
        int channels,
        int[] bitChannels,
        string plainSignature,
        string compressedSignature,
        string mode,
        CancellationToken cancellationToken)
    {
        var reader = new LsbBitReader(pixels, width, height, channels, bitChannels);

        var signature = reader.ReadAscii(SignatureBits);
        bool compressed;
        if (signature == plainSignature)
        {
            compressed = false;
        }
        else if (signature == compressedSignature)
        {
            compressed = true;
        }
        else
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var bitCount = reader.ReadInt32(LengthBits);
        if (bitCount <= 0 || bitCount % 8 != 0 || bitCount / 8 > MaxPayloadBytes || bitCount > reader.Remaining)
        {
            return null;
        }

        var data = reader.ReadBytes(bitCount / 8, cancellationToken);
        if (data is null)
        {
            return null;
        }

        if (compressed)
        {
            data = Inflate(data);
            if (data is null)
            {
                return null;
            }
        }

        var text = Encoding.UTF8.GetString(data);
        return string.IsNullOrWhiteSpace(text) ? null : new StealthPayload(text, mode, compressed);
    }

    private static byte[]? Inflate(byte[] compressed)
    {
        try
        {
            using var input = new MemoryStream(compressed);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output, 81920);
            return output.ToArray();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(StealthPngInfoReader), nameof(Inflate), e);
            return null;
        }
    }

    /// <summary>
    /// Walks the low bits of the chosen channels in column-major order, fetching one column of
    /// pixels at a time. A signature check therefore touches only the first ~120 pixels, so
    /// images without hidden data cost almost nothing.
    /// </summary>
    private sealed class LsbBitReader(
        IPixelCollection<byte> pixels,
        int width,
        int height,
        int channels,
        int[] bitChannels)
    {
        private byte[]? _column;
        private int _columnX = -1;
        private int _position;

        public long Remaining => (long)width * height * bitChannels.Length - _position;

        private int NextBit()
        {
            if (Remaining <= 0)
            {
                return -1;
            }

            var pixelIndex = _position / bitChannels.Length;
            var channel = bitChannels[_position % bitChannels.Length];
            var x = pixelIndex / height;
            var y = pixelIndex % height;
            _position++;

            if (x != _columnX)
            {
                _column = pixels.GetArea(x, 0, 1, (uint)height);
                _columnX = x;
            }

            if (_column is null || _column.Length < height * channels)
            {
                return -1;
            }

            return _column[y * channels + channel] & 1;
        }

        public string? ReadAscii(int bitCount)
        {
            var builder = new StringBuilder(bitCount / 8);
            for (var i = 0; i < bitCount / 8; i++)
            {
                var value = 0;
                for (var bit = 0; bit < 8; bit++)
                {
                    var next = NextBit();
                    if (next < 0)
                    {
                        return null;
                    }

                    value = (value << 1) | next;
                }

                builder.Append((char)value);
            }

            return builder.ToString();
        }

        public int ReadInt32(int bitCount)
        {
            var value = 0;
            for (var i = 0; i < bitCount; i++)
            {
                var next = NextBit();
                if (next < 0)
                {
                    return -1;
                }

                value = (value << 1) | next;
            }

            return value;
        }

        public byte[]? ReadBytes(int count, CancellationToken cancellationToken)
        {
            var data = new byte[count];
            for (var i = 0; i < count; i++)
            {
                if ((i & 0xFFF) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var value = 0;
                for (var bit = 0; bit < 8; bit++)
                {
                    var next = NextBit();
                    if (next < 0)
                    {
                        return null;
                    }

                    value = (value << 1) | next;
                }

                data[i] = (byte)value;
            }

            return data;
        }
    }
}
