using System.Buffers.Binary;
using System.Text;

namespace PicView.Core.MacOS.FileSorting;

/// <summary>
/// A reader of the <c>.DS_Store</c> files Finder keeps its view settings in, as far as one folder's saved view
/// needs. Finder files a folder's settings in its parent's <c>.DS_Store</c>, under the folder's name.
/// </summary>
/// <remarks>
/// The file is a set of blocks: a bookkeeping block that gives where each numbered block lies, a header block named
/// <c>DSDB</c>, and a B-tree of nodes holding the records. Every offset is counted from byte 4. The bytes come from a
/// file any program may have written, so every count, length and block number is checked against the span, the tree
/// is walked with limits on its depth, its nodes and the bytes it reads, and anything malformed reads as no answer
/// rather than throwing. Blocks no longer in the tree still hold old records, so records are reached only through it.
/// </remarks>
public static class DsStore
{
    // Bytes 0-3 are not counted: every offset in the file is from byte 4.
    private const int OffsetBase = 4;
    private const int FileHeaderLength = 20;
    private const int AddressRun = 256;
    private const int HeaderBlockLength = 20;
    private const int NodeHeaderLength = 8;

    // A tree of any size Finder writes is a few levels deep.
    private const int MaxDepth = 16;

    // A file or folder name on a Mac is at most 255 UTF-16 units, however its accents are written.
    private const int MaxNameLength = 1024;

    private static ReadOnlySpan<byte> Alignment => [0, 0, 0, 1];
    private static ReadOnlySpan<byte> Magic => "Bud1"u8;
    private static ReadOnlySpan<byte> TreeName => "DSDB"u8;

    private static ReadOnlySpan<byte> GroupCode => "GRP0"u8;
    private static ReadOnlySpan<byte> IconViewCode => "icvp"u8;
    private static ReadOnlySpan<byte> ListViewCode => "lsvC"u8;
    private static ReadOnlySpan<byte> OlderListViewCode => "lsvp"u8;

    private static ReadOnlySpan<byte> UstrType => "ustr"u8;
    private static ReadOnlySpan<byte> BlobType => "blob"u8;

    /// <summary>The view Finder saved for <paramref name="folderName"/> in the <c>.DS_Store</c> <paramref name="file"/>.</summary>
    /// <param name="file">The bytes of the <c>.DS_Store</c> of the folder's parent.</param>
    /// <param name="folderName">
    /// The folder's name, matched without case and whichever way either writes an accented letter.
    /// </param>
    /// <returns>Null when the file is not a <c>.DS_Store</c>, is malformed, or holds no setting for the folder.</returns>
    public static FinderSavedView? ViewOf(ReadOnlySpan<byte> file, string folderName)
    {
        if (!IsValidUtf16(folderName) || !TryReadTree(file, out var addresses, out var root))
        {
            return null;
        }

        Span<char> name = stackalloc char[MaxNameLength];
        Span<char> composed = stackalloc char[MaxNameLength];
        var folder = folderName.IsNormalized() ? folderName : folderName.Normalize();
        var walker = new Walker(file, addresses, folder, name, composed);
        return walker.TryVisit(root, 0) ? walker.View : null;
    }

    /// <summary>The table of block addresses, and the number of the tree's root node.</summary>
    private static bool TryReadTree(ReadOnlySpan<byte> file, out ReadOnlySpan<byte> addresses, out uint root)
    {
        addresses = default;
        root = 0;
        if (file.Length < FileHeaderLength || !file[..4].SequenceEqual(Alignment) || !file[4..8].SequenceEqual(Magic))
        {
            return false;
        }

        long offset = BinaryPrimitives.ReadUInt32BigEndian(file[8..]);
        long size = BinaryPrimitives.ReadUInt32BigEndian(file[12..]);
        if (offset != BinaryPrimitives.ReadUInt32BigEndian(file[16..]) || OffsetBase + offset + size > file.Length)
        {
            return false;
        }

        // A count of addresses, four unknown bytes, the addresses padded to a run of 256, then the directory of
        // named blocks.
        var bookkeeping = file.Slice(OffsetBase + (int)offset, (int)size);
        if (bookkeeping.Length < 8)
        {
            return false;
        }

        long count = BinaryPrimitives.ReadUInt32BigEndian(bookkeeping);
        var directory = 8 + (count + AddressRun - 1) / AddressRun * AddressRun * 4;
        if (directory + 4 > bookkeeping.Length)
        {
            return false;
        }

        addresses = bookkeeping.Slice(8, (int)(count * 4));
        if (!TryFindNamedBlock(bookkeeping[(int)directory..], TreeName, out var header) ||
            !TryGetBlock(file, addresses, header, out var headerBlock) || headerBlock.Length < HeaderBlockLength)
        {
            return false;
        }

        root = BinaryPrimitives.ReadUInt32BigEndian(headerBlock);
        return true;
    }

    /// <summary>The block number of the entry named <paramref name="name"/> in the bookkeeping block's directory.</summary>
    private static bool TryFindNamedBlock(ReadOnlySpan<byte> directory, ReadOnlySpan<byte> name, out uint block)
    {
        block = 0;
        var count = BinaryPrimitives.ReadUInt32BigEndian(directory);
        var at = 4;
        for (uint i = 0; i < count; i++)
        {
            // A length byte, that many bytes of name, and a block number.
            if (at + 1 > directory.Length || at + 1 + directory[at] + 4 > directory.Length)
            {
                return false;
            }

            var entryName = directory.Slice(at + 1, directory[at]);
            at += 1 + directory[at];
            if (entryName.SequenceEqual(name))
            {
                block = BinaryPrimitives.ReadUInt32BigEndian(directory[at..]);
                return true;
            }

            at += 4;
        }

        return false;
    }

    /// <summary>
    /// The bytes of block <paramref name="number"/>. Its address packs an offset, a multiple of 32, with the base-2
    /// logarithm of its size in the low five bits.
    /// </summary>
    private static bool TryGetBlock(ReadOnlySpan<byte> file, ReadOnlySpan<byte> addresses, uint number,
        out ReadOnlySpan<byte> block)
    {
        block = default;
        if (number >= addresses.Length / 4)
        {
            return false;
        }

        var address = BinaryPrimitives.ReadUInt32BigEndian(addresses[(int)(number * 4)..]);
        long start = OffsetBase + (address & ~0x1Fu);
        var size = 1L << (int)(address & 0x1F);
        if (start + size > file.Length)
        {
            return false;
        }

        block = file.Slice((int)start, (int)size);
        return true;
    }

    // Normalizing throws on a lone surrogate, which a crafted name may hold.
    private static bool IsValidUtf16(ReadOnlySpan<char> text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                i++;
            }
            else if (char.IsSurrogate(text[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Walks the tree in order, every node of it, and keeps the first valid value of each setting it finds for the
    /// folder. The tree is ordered by Finder's own comparison of names, which is not reproduced here.
    /// </summary>
    private ref struct Walker
    {
        private readonly ReadOnlySpan<byte> _file;
        private readonly ReadOnlySpan<byte> _addresses;
        private readonly string _folder;
        private readonly Span<char> _name;
        private readonly Span<char> _composed;

        // A node may be visited once per block, and the nodes may read the file's bytes once: blocks of a sound
        // file do not overlap. Either limit ends a loop, or blocks that all point at one large block.
        private int _nodesLeft;
        private long _bytesLeft;

        private string? _groupBy;
        private string? _iconArrangeBy;
        private string? _listSortColumn;
        private bool? _listSortAscending;
        private string? _olderListSortColumn;
        private bool? _olderListSortAscending;

        public Walker(ReadOnlySpan<byte> file, ReadOnlySpan<byte> addresses, string folder, Span<char> name,
            Span<char> composed)
        {
            _file = file;
            _addresses = addresses;
            _folder = folder;
            _name = name;
            _composed = composed;
            _nodesLeft = addresses.Length / 4;
            _bytesLeft = file.Length;
        }

        public readonly FinderSavedView? View
        {
            get
            {
                var column = _listSortColumn ?? _olderListSortColumn;
                var ascending = _listSortColumn is not null ? _listSortAscending : _olderListSortAscending;
                if (_groupBy is null && _iconArrangeBy is null && column is null)
                {
                    return null;
                }

                return new FinderSavedView(_groupBy, _iconArrangeBy, column, ascending);
            }
        }

        /// <summary>
        /// Reads node <paramref name="number"/> and the nodes under it. A node is a 32-bit <c>P</c> and a count:
        /// a leaf when <c>P</c> is 0, holding that many records; else each record follows the child before it, and
        /// <c>P</c> is the child after the last.
        /// </summary>
        /// <returns>False when the tree is malformed: a node missing, too deep, or reached too often.</returns>
        public bool TryVisit(uint number, int depth)
        {
            if (depth >= MaxDepth || --_nodesLeft < 0 || !TryGetBlock(_file, _addresses, number, out var node))
            {
                return false;
            }

            _bytesLeft -= node.Length;
            if (_bytesLeft < 0 || node.Length < NodeHeaderLength)
            {
                return false;
            }

            var lastChild = BinaryPrimitives.ReadUInt32BigEndian(node);
            var count = BinaryPrimitives.ReadUInt32BigEndian(node[4..]);
            var at = NodeHeaderLength;
            for (uint i = 0; i < count; i++)
            {
                if (lastChild != 0)
                {
                    if (at + 4 > node.Length)
                    {
                        return true;
                    }

                    var child = BinaryPrimitives.ReadUInt32BigEndian(node[at..]);
                    at += 4;
                    if (!TryVisit(child, depth + 1))
                    {
                        return false;
                    }
                }

                // A record whose length is not known ends the read of its node.
                if (!TryReadRecord(node, ref at))
                {
                    return true;
                }
            }

            return lastChild == 0 || TryVisit(lastChild, depth + 1);
        }

        /// <summary>
        /// Reads the record at <paramref name="at"/> and moves past it: the name's length in UTF-16 units, the name
        /// in UTF-16BE, a four-byte code, a four-byte type, then the value.
        /// </summary>
        /// <returns>False when the record runs past its node, or its type is unknown.</returns>
        private bool TryReadRecord(ReadOnlySpan<byte> node, ref int at)
        {
            if (at + 4 > node.Length)
            {
                return false;
            }

            var nameLength = BinaryPrimitives.ReadUInt32BigEndian(node[at..]) * 2L;
            if (at + 4 + nameLength + 8 > node.Length)
            {
                return false;
            }

            var name = node.Slice(at + 4, (int)nameLength);
            at += 4 + (int)nameLength;
            var code = node.Slice(at, 4);
            var type = node.Slice(at + 4, 4);
            at += 8;

            if (!TryGetValueLength(node[at..], type, out var valueLength))
            {
                return false;
            }

            var value = node.Slice(at, valueLength);
            at += valueLength;
            if (IsWanted(code) && IsFolder(name))
            {
                Take(code, type, value);
            }

            return true;
        }

        private static bool TryGetValueLength(ReadOnlySpan<byte> rest, ReadOnlySpan<byte> type, out int length)
        {
            length = 0;
            if (type.SequenceEqual("long"u8) || type.SequenceEqual("shor"u8) || type.SequenceEqual("type"u8))
            {
                length = 4;
            }
            else if (type.SequenceEqual("bool"u8))
            {
                length = 1;
            }
            else if (type.SequenceEqual("comp"u8) || type.SequenceEqual("dutc"u8))
            {
                length = 8;
            }
            else if (type.SequenceEqual(BlobType) || type.SequenceEqual(UstrType))
            {
                // A length, in bytes for a blob and in UTF-16 units for a string, then the data.
                if (rest.Length < 4)
                {
                    return false;
                }

                long units = BinaryPrimitives.ReadUInt32BigEndian(rest);
                var total = 4 + (type.SequenceEqual(UstrType) ? units * 2 : units);
                if (total > rest.Length)
                {
                    return false;
                }

                length = (int)total;
                return true;
            }
            else
            {
                return false;
            }

            return length <= rest.Length;
        }

        private static bool IsWanted(ReadOnlySpan<byte> code) =>
            code.SequenceEqual(GroupCode) || code.SequenceEqual(IconViewCode) ||
            code.SequenceEqual(ListViewCode) || code.SequenceEqual(OlderListViewCode);

        /// <summary>Whether the UTF-16BE <paramref name="name"/> is the folder's, without case and composed.</summary>
        private readonly bool IsFolder(ReadOnlySpan<byte> name)
        {
            var units = name.Length / 2;
            if (units > _name.Length)
            {
                return false;
            }

            var text = _name[..units];
            for (var i = 0; i < units; i++)
            {
                text[i] = (char)((name[i * 2] << 8) | name[i * 2 + 1]);
            }

            ReadOnlySpan<char> shown = text;
            if (!IsValidUtf16(shown))
            {
                return false;
            }

            if (!shown.IsNormalized())
            {
                if (!shown.TryNormalize(_composed, out var written))
                {
                    return false;
                }

                shown = _composed[..written];
            }

            return shown.Equals(_folder, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Keeps <paramref name="value"/> where it is the first valid one of its setting.</summary>
        private void Take(ReadOnlySpan<byte> code, ReadOnlySpan<byte> type, ReadOnlySpan<byte> value)
        {
            if (code.SequenceEqual(GroupCode))
            {
                if (_groupBy is null && type.SequenceEqual(UstrType))
                {
                    _groupBy = Encoding.BigEndianUnicode.GetString(value[4..]);
                }

                return;
            }

            if (!type.SequenceEqual(BlobType))
            {
                return;
            }

            var plist = value[4..];
            if (code.SequenceEqual(IconViewCode))
            {
                if (_iconArrangeBy is null && BinaryPlist.TryReadString(plist, "arrangeBy", out var arrangeBy))
                {
                    _iconArrangeBy = arrangeBy;
                }

                return;
            }

            var newer = code.SequenceEqual(ListViewCode);
            if ((newer ? _listSortColumn : _olderListSortColumn) is not null ||
                !BinaryPlist.TryReadString(plist, "sortColumn", out var column) || column is null)
            {
                return;
            }

            bool? ascending = BinaryPlist.TryReadColumnAscending(plist, column, out var flag) ? flag : null;
            if (newer)
            {
                _listSortColumn = column;
                _listSortAscending = ascending;
            }
            else
            {
                _olderListSortColumn = column;
                _olderListSortAscending = ascending;
            }
        }
    }
}
