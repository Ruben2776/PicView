using System.Text;

namespace PicView.Core.MacOS.FileSorting;

/// <summary>
/// A minimal reader of Apple binary property lists (<c>bplist00</c>), as far as Finder's view settings in a
/// <c>.DS_Store</c> need: a string in the top dictionary, and the sort direction of a named column. The bytes come
/// from a file any program may have written, so every size, count and reference is checked against the span, and
/// anything malformed reads as no answer rather than throwing.
/// </summary>
internal static class BinaryPlist
{
    private const int HeaderLength = 8;
    private const int TrailerLength = 32;

    // Finder's dictionaries and column arrays hold a few dozen entries. A cap keeps a crafted list, whose entries
    // may all point at one large dictionary, from making the lookups quadratic.
    private const int MaxContainerCount = 1024;

    private const byte FalseMarker = 0x08;
    private const byte TrueMarker = 0x09;
    private const byte AsciiStringType = 0x50;
    private const byte Utf16StringType = 0x60;
    private const byte ArrayType = 0xA0;
    private const byte DictionaryType = 0xD0;
    private const byte IntegerType = 0x10;

    private static ReadOnlySpan<byte> Header => "bplist00"u8;

    /// <summary>The string under <paramref name="key"/> in the top dictionary.</summary>
    /// <returns>False when the list is malformed, has no such key, or holds another type there.</returns>
    public static bool TryReadString(ReadOnlySpan<byte> plist, string key, out string? value)
    {
        value = null;
        if (!Reader.TryCreate(plist, out var reader) ||
            !reader.TryFindValue(reader.Top, key, out var valueIndex))
        {
            return false;
        }

        return reader.TryReadString(valueIndex, out value);
    }

    /// <summary>
    /// The <c>ascending</c> of the column named <paramref name="identifier"/> in the top dictionary's
    /// <c>columns</c>: an array of dictionaries that each hold <c>identifier</c> and <c>ascending</c>, or a
    /// dictionary from identifier to a dictionary holding <c>ascending</c>.
    /// </summary>
    /// <returns>False when the list is malformed, or has no such column or flag.</returns>
    public static bool TryReadColumnAscending(ReadOnlySpan<byte> plist, string identifier, out bool ascending)
    {
        ascending = false;
        if (!Reader.TryCreate(plist, out var reader) ||
            !reader.TryFindValue(reader.Top, "columns", out var columns) ||
            !reader.TryReadContainer(columns, out var type, out var count, out var refs))
        {
            return false;
        }

        if (type == ArrayType)
        {
            for (long i = 0; i < count; i++)
            {
                if (reader.TryReadReference(refs, i, out var column) &&
                    reader.TryFindValue(column, "identifier", out var name) &&
                    reader.StringEquals(name, identifier))
                {
                    return reader.TryFindValue(column, "ascending", out var flag) &&
                           reader.TryReadBool(flag, out ascending);
                }
            }

            return false;
        }

        return reader.TryFindValue(columns, identifier, out var entry) &&
               reader.TryFindValue(entry, "ascending", out var direction) &&
               reader.TryReadBool(direction, out ascending);
    }

    private readonly ref struct Reader
    {
        private readonly ReadOnlySpan<byte> _bytes;
        private readonly int _trailerStart;
        private readonly int _offsetSize;
        private readonly int _referenceSize;
        private readonly long _objectCount;
        private readonly long _tableStart;

        public long Top { get; }

        private Reader(ReadOnlySpan<byte> bytes, int offsetSize, int referenceSize, long objectCount, long top,
            long tableStart)
        {
            _bytes = bytes;
            _trailerStart = bytes.Length - TrailerLength;
            _offsetSize = offsetSize;
            _referenceSize = referenceSize;
            _objectCount = objectCount;
            Top = top;
            _tableStart = tableStart;
        }

        public static bool TryCreate(ReadOnlySpan<byte> bytes, out Reader reader)
        {
            reader = default;
            if (bytes.Length < HeaderLength + TrailerLength || !bytes[..HeaderLength].SequenceEqual(Header))
            {
                return false;
            }

            var trailerStart = bytes.Length - TrailerLength;
            var trailer = bytes[trailerStart..];
            int offsetSize = trailer[6], referenceSize = trailer[7];
            if (offsetSize is < 1 or > 8 || referenceSize is < 1 or > 8)
            {
                return false;
            }

            var objectCount = ReadBigEndian(trailer.Slice(8, 8), 8);
            var top = ReadBigEndian(trailer.Slice(16, 8), 8);
            var tableStart = ReadBigEndian(trailer.Slice(24, 8), 8);

            // Every object takes at least a byte, so a count above the span is a lie; this also keeps the products
            // below far from overflowing.
            if (objectCount is < 1 or > int.MaxValue || objectCount > bytes.Length || top < 0 || top >= objectCount ||
                tableStart < HeaderLength || tableStart > trailerStart ||
                trailerStart - tableStart < objectCount * offsetSize)
            {
                return false;
            }

            reader = new Reader(bytes, offsetSize, referenceSize, objectCount, top, tableStart);
            return true;
        }

        /// <summary>
        /// The value of <paramref name="key"/> in the dictionary object at <paramref name="dictionary"/>.
        /// </summary>
        public bool TryFindValue(long dictionary, string key, out long valueIndex)
        {
            valueIndex = 0;
            if (!TryReadContainer(dictionary, out var type, out var count, out var refs) || type != DictionaryType)
            {
                return false;
            }

            for (long i = 0; i < count; i++)
            {
                if (TryReadReference(refs, i, out var keyIndex) && StringEquals(keyIndex, key))
                {
                    return TryReadReference(refs, count + i, out valueIndex);
                }
            }

            return false;
        }

        /// <summary>
        /// The type nibble, the number of entries, and where the references to them start, of the array or
        /// dictionary at <paramref name="index"/>. A dictionary holds its key references, then its value references.
        /// </summary>
        public bool TryReadContainer(long index, out byte type, out long count, out long refs)
        {
            type = 0;
            refs = 0;
            if (!TryReadHeader(index, out type, out count, out var dataStart) ||
                type is not (ArrayType or DictionaryType))
            {
                return false;
            }

            var references = type == DictionaryType ? count * 2 : count;
            if (count > MaxContainerCount || dataStart + references * _referenceSize > _trailerStart)
            {
                return false;
            }

            refs = dataStart;
            return true;
        }

        /// <summary>
        /// The object index in reference number <paramref name="n"/> of the list starting at <paramref name="refs"/>.
        /// </summary>
        public bool TryReadReference(long refs, long n, out long index)
        {
            index = ReadBigEndian(_bytes.Slice((int)(refs + n * _referenceSize), _referenceSize), _referenceSize);
            return index >= 0 && index < _objectCount;
        }

        public bool TryReadBool(long index, out bool value)
        {
            value = false;
            if (!TryGetOffset(index, out var offset))
            {
                return false;
            }

            var marker = _bytes[(int)offset];
            value = marker == TrueMarker;
            return marker is TrueMarker or FalseMarker;
        }

        public bool StringEquals(long index, string text)
        {
            if (!TryReadHeader(index, out var type, out var count, out var dataStart))
            {
                return false;
            }

            if (type == AsciiStringType)
            {
                if (count != text.Length || dataStart + count > _trailerStart)
                {
                    return false;
                }

                for (var i = 0; i < text.Length; i++)
                {
                    if (_bytes[(int)dataStart + i] != text[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            if (type != Utf16StringType || count != text.Length || dataStart + count * 2 > _trailerStart)
            {
                return false;
            }

            for (var i = 0; i < text.Length; i++)
            {
                var at = (int)dataStart + i * 2;
                if (((_bytes[at] << 8) | _bytes[at + 1]) != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryReadString(long index, out string? value)
        {
            value = null;
            if (!TryReadHeader(index, out var type, out var count, out var dataStart))
            {
                return false;
            }

            if (type == AsciiStringType)
            {
                if (dataStart + count > _trailerStart)
                {
                    return false;
                }

                value = Encoding.ASCII.GetString(_bytes.Slice((int)dataStart, (int)count));
                return true;
            }

            if (type != Utf16StringType || dataStart + count * 2 > _trailerStart)
            {
                return false;
            }

            value = Encoding.BigEndianUnicode.GetString(_bytes.Slice((int)dataStart, (int)count * 2));
            return true;
        }

        private bool TryGetOffset(long index, out long offset)
        {
            offset = 0;
            if (index < 0 || index >= _objectCount)
            {
                return false;
            }

            var at = (int)(_tableStart + index * _offsetSize);
            offset = ReadBigEndian(_bytes.Slice(at, _offsetSize), _offsetSize);
            return offset is >= HeaderLength && offset < _trailerStart;
        }

        /// <summary>
        /// The type nibble and the count of the object at <paramref name="index"/>, and where its data starts.
        /// </summary>
        private bool TryReadHeader(long index, out byte type, out long count, out long dataStart)
        {
            type = 0;
            count = 0;
            dataStart = 0;
            if (!TryGetOffset(index, out var offset))
            {
                return false;
            }

            var marker = _bytes[(int)offset];
            type = (byte)(marker & 0xF0);
            count = marker & 0x0F;
            dataStart = offset + 1;
            if (count != 0x0F)
            {
                return true;
            }

            // The count follows as an integer object: 0x1n, then 2^n bytes.
            if (dataStart >= _trailerStart)
            {
                return false;
            }

            var integer = _bytes[(int)dataStart];
            var size = 1 << (integer & 0x0F);
            if ((integer & 0xF0) != IntegerType || (integer & 0x0F) > 3 || dataStart + 1 + size > _trailerStart)
            {
                return false;
            }

            count = ReadBigEndian(_bytes.Slice((int)dataStart + 1, size), size);
            dataStart += 1 + size;

            // No object can hold more entries than the span has bytes.
            return count >= 0 && count <= _trailerStart;
        }

        private static long ReadBigEndian(ReadOnlySpan<byte> bytes, int size)
        {
            ulong value = 0;
            for (var i = 0; i < size; i++)
            {
                value = (value << 8) | bytes[i];
            }

            // A value beyond long.MaxValue is far beyond any span, so it reads as out of range.
            return value > long.MaxValue ? -1 : (long)value;
        }
    }
}
