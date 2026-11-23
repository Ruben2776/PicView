namespace PicView.Core.Exif;

/// <summary>
/// Writes the metadata of a JPEG file without decoding or encoding its image, so an Exif edit loses no quality: the
/// metadata segments are replaced or left out, and every other byte, the image data and anything after it, such as a
/// motion photo's video, is copied as it is.
/// </summary>
internal static class JpegMetadataWriter
{
    /// <summary>The metadata kinds a rewrite can leave out. The Exif segment is replaced, not kept.</summary>
    [Flags]
    internal enum Drop
    {
        None = 0,
        Xmp = 1,
        Iptc = 2,
        ColorProfile = 4,
        Comment = 8
    }

    private const byte Marker = 0xFF;
    private const byte StartOfImage = 0xD8;
    private const byte StartOfScan = 0xDA;
    private const byte App0 = 0xE0;
    private const byte App1 = 0xE1;
    private const byte App2 = 0xE2;
    private const byte App13 = 0xED;
    private const byte CommentMarker = 0xFE;

    // The length field counts itself, and holds at most 0xFFFF
    private const int MaxPayload = 0xFFFF - 2;

    private static ReadOnlySpan<byte> ExifId => "Exif\0"u8;
    private static ReadOnlySpan<byte> ExifHeader => "Exif\0\0"u8;
    private static ReadOnlySpan<byte> XmpId => "http://ns.adobe.com/xap/1.0/\0"u8;
    private static ReadOnlySpan<byte> ExtendedXmpId => "http://ns.adobe.com/xmp/extension/\0"u8;
    private static ReadOnlySpan<byte> PhotoshopId => "Photoshop 3.0\0"u8;
    private static ReadOnlySpan<byte> IccId => "ICC_PROFILE\0"u8;

    internal static bool IsJpeg(ReadOnlySpan<byte> file) =>
        file.Length > 3 && file[0] == Marker && file[1] == StartOfImage && file[2] == Marker;

    /// <summary>
    /// Builds the file again with <paramref name="exif"/> as its Exif segment, in place of the old one, or with none
    /// when it is null, and without the kinds in <paramref name="drop"/>.
    /// </summary>
    /// <param name="jpeg">The whole file.</param>
    /// <param name="exif">The Exif data, with or without its "Exif\0\0" header, or null for none.</param>
    /// <param name="drop">The other metadata to leave out.</param>
    /// <returns>The new file, or null when <paramref name="jpeg"/> is not a JPEG this can read or the Exif data does
    /// not fit one segment: the caller then encodes the image instead.</returns>
    internal static byte[]? Rewrite(ReadOnlySpan<byte> jpeg, ReadOnlySpan<byte> exif, bool hasExif, Drop drop)
    {
        if (!IsJpeg(jpeg))
        {
            return null;
        }

        var exifPayloadLength = hasExif ? (exif.StartsWith(ExifId) ? exif.Length : ExifHeader.Length + exif.Length) : 0;
        if (exifPayloadLength > MaxPayload)
        {
            return null;
        }

        using var output = new MemoryStream(jpeg.Length + exifPayloadLength + 4);
        output.Write(jpeg[..2]);

        var exifWritten = !hasExif;
        var position = 2;
        while (true)
        {
            if (position + 4 > jpeg.Length || jpeg[position] != Marker)
            {
                return null;
            }

            // A marker may be preceded by fill bytes
            var markerPosition = position;
            while (position < jpeg.Length && jpeg[position] == Marker)
            {
                position++;
            }

            if (position + 3 > jpeg.Length)
            {
                return null;
            }

            var marker = jpeg[position];
            position++;
            if (marker == StartOfScan)
            {
                // The Exif segment goes before the image data when the file had none to replace
                if (!exifWritten)
                {
                    WriteExif(output, exif);
                }

                output.Write(jpeg[markerPosition..]);
                return output.ToArray();
            }

            var length = (jpeg[position] << 8) | jpeg[position + 1];
            if (length < 2 || position + length > jpeg.Length)
            {
                return null;
            }

            var segment = jpeg[markerPosition..(position + length)];
            var payload = jpeg[(position + 2)..(position + length)];
            position += length;

            // The Exif segment follows the JFIF one, so it is written where the first segment that is not APP0 is
            if (!exifWritten && marker != App0)
            {
                WriteExif(output, exif);
                exifWritten = true;
            }

            if (IsLeftOut(marker, payload, drop))
            {
                continue;
            }

            output.Write(segment);
        }
    }

    private static bool IsLeftOut(byte marker, ReadOnlySpan<byte> payload, Drop drop) => marker switch
    {
        // Every Exif segment the file had: the new one replaces them
        App1 when payload.StartsWith(ExifId) => true,
        App1 when payload.StartsWith(XmpId) || payload.StartsWith(ExtendedXmpId) => drop.HasFlag(Drop.Xmp),
        App13 when payload.StartsWith(PhotoshopId) => drop.HasFlag(Drop.Iptc),
        App2 when payload.StartsWith(IccId) => drop.HasFlag(Drop.ColorProfile),
        CommentMarker => drop.HasFlag(Drop.Comment),
        _ => false
    };

    private static void WriteExif(MemoryStream output, ReadOnlySpan<byte> exif)
    {
        var hasHeader = exif.StartsWith(ExifId);
        var length = 2 + (hasHeader ? 0 : ExifHeader.Length) + exif.Length;
        output.WriteByte(Marker);
        output.WriteByte(App1);
        output.WriteByte((byte)(length >> 8));
        output.WriteByte((byte)length);
        if (!hasHeader)
        {
            output.Write(ExifHeader);
        }

        output.Write(exif);
    }
}
