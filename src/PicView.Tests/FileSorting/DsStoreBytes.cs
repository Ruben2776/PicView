#if MACOS
using System.Buffers.Binary;
using System.Text;

namespace PicView.Tests.FileSorting;

/// <summary>
/// Lays out <c>.DS_Store</c> files as Finder writes them: a bookkeeping block that maps block numbers to where the
/// blocks lie, a header block named <c>DSDB</c>, and a tree of nodes that hold the records.
/// </summary>
internal static class DsStoreBytes
{
    private const int MinBlockSize = 32;
    private const int AddressRun = 256;
    private const int FreeLists = 32;

    // Bytes 0-3 are not counted: every offset in the file is from byte 4.
    private const int OffsetBase = 4;
    private const int FileHeaderLength = 32;

    private const int BookkeepingBlock = 0;
    private const int HeaderBlock = 1;
    private const int FirstLeafBlock = 2;

    /// <summary>
    /// A file whose tree holds the records of <paramref name="leaves"/>: one leaf when given one group, and an inner
    /// root over the leaves when given several, with the last record of each leaf but the last moved up into the
    /// root. A record's value is written as given, so it carries its own length where its type has one.
    /// </summary>
    internal static byte[] Build(params (string Name, string Code, string Type, byte[] Value)[][] leaves)
    {
        var nodes = new List<byte[]>();
        for (var i = 0; i < leaves.Length; i++)
        {
            var records = i < leaves.Length - 1 ? leaves[i][..^1] : leaves[i];
            nodes.Add(Node(0, records.Select(Record)));
        }

        var root = FirstLeafBlock;
        if (leaves.Length > 1)
        {
            root = FirstLeafBlock + leaves.Length;
            var entries = new List<byte[]>();
            for (var i = 0; i < leaves.Length - 1; i++)
            {
                entries.Add([.. BigEndian(FirstLeafBlock + i), .. Record(leaves[i][^1])]);
            }

            nodes.Add(Node(root - 1, entries));
        }

        var header = new byte[20];
        BinaryPrimitives.WriteUInt32BigEndian(header, (uint)root);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), leaves.Length > 1 ? 1u : 0u);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(8), (uint)leaves.Sum(l => l.Length));
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(12), (uint)nodes.Count);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(16), 0x1000);

        // Blocks 1.. are the header and the nodes; block 0, the bookkeeping, goes last, as Finder places it.
        byte[][] contents = [header, .. nodes];
        var blockCount = contents.Length + 1;
        var addresses = new uint[blockCount];
        var file = new List<byte>(new byte[OffsetBase + FileHeaderLength]);
        for (var i = 0; i < contents.Length; i++)
        {
            addresses[i + 1] = Place(file, contents[i]);
        }

        var bookkeeping = new List<byte>();
        bookkeeping.AddRange(BigEndian(blockCount));
        bookkeeping.AddRange(BigEndian(0));
        var paddedCount = (blockCount + AddressRun - 1) / AddressRun * AddressRun;
        for (var i = 0; i < paddedCount; i++)
        {
            bookkeeping.AddRange(BigEndian(i < blockCount ? (int)addresses[i] : 0));
        }

        bookkeeping.AddRange(BigEndian(1));
        bookkeeping.Add(4);
        bookkeeping.AddRange("DSDB"u8.ToArray());
        bookkeeping.AddRange(BigEndian(HeaderBlock));
        for (var i = 0; i < FreeLists; i++)
        {
            bookkeeping.AddRange(BigEndian(0));
        }

        // The bookkeeping block's own address is in its table, so it is placed before that entry is known.
        var bookkeepingSize = BlockSize(bookkeeping.Count);
        var bookkeepingOffset = AlignedEnd(file, bookkeepingSize) - OffsetBase;
        var bookkeepingAddress = (uint)bookkeepingOffset | (uint)int.Log2(bookkeepingSize);
        var entry = BigEndian((int)bookkeepingAddress);
        for (var i = 0; i < 4; i++)
        {
            bookkeeping[8 + BookkeepingBlock * 4 + i] = entry[i];
        }

        Place(file, [.. bookkeeping]);

        var bytes = file.ToArray();
        BinaryPrimitives.WriteUInt32BigEndian(bytes, 1);
        "Bud1"u8.CopyTo(bytes.AsSpan(4));
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8), (uint)bookkeepingOffset);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(12), (uint)bookkeepingSize);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16), (uint)bookkeepingOffset);
        return bytes;
    }

    /// <summary>A <c>ustr</c> value: its length in UTF-16 units, then the text in UTF-16BE.</summary>
    internal static byte[] Ustr(string text) => [.. BigEndian(text.Length), .. Encoding.BigEndianUnicode.GetBytes(text)];

    /// <summary>A <c>blob</c> value: its length, then the bytes.</summary>
    internal static byte[] Blob(byte[] data) => [.. BigEndian(data.Length), .. data];

    /// <summary>A <c>long</c> value, or any other four-byte one.</summary>
    internal static byte[] Long(int value) => BigEndian(value);

    /// <summary>
    /// Points the first child of <paramref name="file"/>'s inner root at the root itself, so the nodes form a loop.
    /// </summary>
    internal static void PointTheRootsFirstChildAtItself(byte[] file)
    {
        var bookkeeping = OffsetBase + (int)BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(8));
        var header = BlockStart(file, bookkeeping, HeaderBlock);
        var root = BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(header));
        var rootStart = BlockStart(file, bookkeeping, (int)root);
        BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(rootStart + 8), root);
    }

    private static int BlockStart(byte[] file, int bookkeeping, int block)
    {
        var address = BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(bookkeeping + 8 + block * 4));
        return OffsetBase + (int)(address & ~0x1Fu);
    }

    private static byte[] Node(int lastChild, IEnumerable<byte[]> entries)
    {
        var list = entries.ToArray();
        return [.. BigEndian(lastChild), .. BigEndian(list.Length), .. list.SelectMany(e => e)];
    }

    private static byte[] Record((string Name, string Code, string Type, byte[] Value) record) =>
    [
        .. Ustr(record.Name), .. Encoding.ASCII.GetBytes(record.Code), .. Encoding.ASCII.GetBytes(record.Type),
        .. record.Value
    ];

    /// <summary>Appends <paramref name="content"/> as a block aligned to its size, and gives its address.</summary>
    private static uint Place(List<byte> file, byte[] content)
    {
        var size = BlockSize(content.Length);
        var start = AlignedEnd(file, size);
        file.AddRange(new byte[start - file.Count]);
        file.AddRange(content);
        file.AddRange(new byte[size - content.Length]);
        return (uint)(start - OffsetBase) | (uint)int.Log2(size);
    }

    private static int AlignedEnd(List<byte> file, int size)
    {
        var offset = file.Count - OffsetBase;
        return OffsetBase + (offset + size - 1) / size * size;
    }

    private static int BlockSize(int length)
    {
        var size = MinBlockSize;
        while (size < length)
        {
            size *= 2;
        }

        return size;
    }

    private static byte[] BigEndian(int value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        return bytes;
    }
}
#endif
