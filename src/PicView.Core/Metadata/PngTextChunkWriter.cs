using System.Buffers.Binary;
using System.Text;

namespace PicView.Core.Metadata;

/// <summary>
/// Replaces one text chunk of a PNG and leaves every other byte of the file as it was, so the image data is
/// never decoded or encoded again.
/// </summary>
internal static class PngTextChunkWriter
{
    private static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Length field, type and CRC around every chunk's data.</summary>
    private const int ChunkOverhead = 12;

    private static readonly uint[] CrcTable = BuildCrcTable();

    /// <summary>
    /// Replaces the first tEXt, zTXt or iTXt chunk whose keyword equals <paramref name="keyword"/> (ignoring case,
    /// as <see cref="PngTextChunkReader"/> does) with one holding <paramref name="text"/>, in the same position.
    /// The chunk keeps the keyword's spelling from the file. Text that fits Latin-1 is written as tEXt, anything
    /// else as uncompressed UTF-8 iTXt.
    /// </summary>
    /// <returns>
    /// The new file, or null when <paramref name="png"/> is not a PNG, is cut short, has no IEND or no such text
    /// chunk, or when the CRC of the chunk to replace is wrong.
    /// </returns>
    public static byte[]? ReplaceText(ReadOnlySpan<byte> png, string keyword, string text)
    {
        if (png.Length < Signature.Length + ChunkOverhead || !png[..Signature.Length].SequenceEqual(Signature))
        {
            return null;
        }

        var targetStart = -1;
        var targetEnd = -1;
        var keywordBytes = ReadOnlySpan<byte>.Empty;
        var sawEnd = false;

        var position = Signature.Length;
        while (position + ChunkOverhead <= png.Length)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(png[position..]);
            if (length > (uint)(png.Length - position - ChunkOverhead))
            {
                return null;
            }

            var type = png.Slice(position + 4, 4);
            var data = png.Slice(position + 8, (int)length);
            var chunkEnd = position + ChunkOverhead + (int)length;

            if (type.SequenceEqual("IEND"u8))
            {
                sawEnd = true;
                break;
            }

            if (targetStart < 0 && (type.SequenceEqual("tEXt"u8) || type.SequenceEqual("zTXt"u8) ||
                                    type.SequenceEqual("iTXt"u8)))
            {
                var separator = data.IndexOf((byte)0);
                if (separator > 0 && Encoding.Latin1.GetString(data[..separator])
                        .Equals(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    var crc = BinaryPrimitives.ReadUInt32BigEndian(png[(chunkEnd - 4)..]);
                    if (Crc32(png.Slice(position + 4, 4 + (int)length)) != crc)
                    {
                        return null;
                    }

                    targetStart = position;
                    targetEnd = chunkEnd;
                    keywordBytes = data[..separator];
                }
            }

            position = chunkEnd;
        }

        if (!sawEnd || targetStart < 0)
        {
            return null;
        }

        var (newType, newData) = BuildChunk(keywordBytes, text);
        var result = new byte[targetStart + ChunkOverhead + newData.Length + (png.Length - targetEnd)];
        png[..targetStart].CopyTo(result);

        var offset = targetStart;
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(offset), (uint)newData.Length);
        newType.CopyTo(result.AsSpan(offset + 4));
        newData.CopyTo(result.AsSpan(offset + 8));
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(offset + 8 + newData.Length),
            Crc32(result.AsSpan(offset + 4, 4 + newData.Length)));

        png[targetEnd..].CopyTo(result.AsSpan(offset + ChunkOverhead + newData.Length));
        return result;
    }

    private static (byte[] Type, byte[] Data) BuildChunk(ReadOnlySpan<byte> keyword, string text)
    {
        var isLatin1 = true;
        foreach (var c in text)
        {
            if (c > 0xFF)
            {
                isLatin1 = false;
                break;
            }
        }

        using var data = new MemoryStream();
        data.Write(keyword);
        data.WriteByte(0);
        if (isLatin1)
        {
            data.Write(Encoding.Latin1.GetBytes(text));
            return ("tEXt"u8.ToArray(), data.ToArray());
        }

        // Compression flag 0, compression method 0, empty language tag, empty translated keyword
        data.Write([0, 0, 0, 0]);
        data.Write(Encoding.UTF8.GetBytes(text));
        return ("iTXt"u8.ToArray(), data.ToArray());
    }

    /// <summary>CRC-32 as PNG defines it: ISO-HDLC, reflected polynomial 0xEDB88320.</summary>
    private static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return ~crc;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }
}
