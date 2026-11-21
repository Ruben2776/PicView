using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ImageMagick;
using PicView.Core.DebugTools;

namespace PicView.Core.ImageDecoding;

/// <summary>
/// Provides methods to analyze image properties
/// </summary>
public static class ImageAnalyzer
{
    /// <summary>
    ///     Gets the number of frames in an image.
    /// </summary>
    /// <param name="file">The path to the image file.</param>
    /// <returns>The number of frames in the image. Returns 0 if an error occurs.</returns>
    /// <remarks>
    ///     This method uses the Magick.NET library to load the image and retrieve the frame count.
    /// </remarks>
    public static int GetImageFrames(string file)
    {
        try
        {
            using var magickImageCollection = new MagickImageCollection();
            magickImageCollection.Ping(file);
            return magickImageCollection.Count;
        }
#if DEBUG
        catch (MagickCorruptImageErrorException ex)
        {
            // This can happen with malformed or unsupported image files.
            // We'll treat it as a non-animated image with 0 frames.

            Trace.WriteLine($"Caught a corrupt image exception for file '{file}'. Treating as 0 frames. Details: {ex.Message}");
            DebugHelper.LogDebug(nameof(ImageAnalyzer),nameof(GetImageFrames), ex);
            return 0;
        }
#endif
        catch (MagickException ex)
        {
            DebugHelper.LogDebug(nameof(ImageAnalyzer),nameof(GetImageFrames), ex);
            return 0;
        }
    }
    
    /// <summary>
    /// Determines if the specified image file is animated. A WebP, GIF or AVIF answers from its header; the ImageMagick
    /// ping, which decodes every frame of an animation to count them, is only for what the header cannot settle.
    /// </summary>
    /// <param name="fileInfo">File information for the image</param>
    /// <returns>True if the image is animated; otherwise, false</returns>
    public static bool IsAnimated(FileInfo fileInfo) =>
        TryDetectAnimationFromHeader(fileInfo.FullName) ?? GetImageFrames(fileInfo.FullName) > 1;

    /// <summary>
    /// Tells whether <paramref name="file"/> holds more than one frame by walking its container, without decoding.
    /// </summary>
    /// <returns>
    /// Whether the file is animated, or null when the header does not settle it: an unknown or damaged file, or an AVIF
    /// image sequence whose sample table cannot be read.
    /// </returns>
    internal static bool? TryDetectAnimationFromHeader(string file)
    {
        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, bufferSize: 4096);
            Span<byte> header = stackalloc byte[12];
            if (stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length)
            {
                return null;
            }

            if (header[..4].SequenceEqual("RIFF"u8) && header[8..].SequenceEqual("WEBP"u8))
            {
                return IsAnimatedWebp(stream);
            }

            if (header[..6].SequenceEqual("GIF87a"u8) || header[..6].SequenceEqual("GIF89a"u8))
            {
                stream.Position = 6;
                return IsAnimatedGif(stream);
            }

            if (header[4..8].SequenceEqual("ftyp"u8))
            {
                var boxSize = BinaryPrimitives.ReadUInt32BigEndian(header);
                return IsAnimatedIsoMedia(stream, header[8..], boxSize);
            }

            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads the chunks after the RIFF header. Only the extended format animates: its first chunk, VP8X, carries an
    /// animation flag, and each frame is an ANMF chunk.
    /// </summary>
    private static bool? IsAnimatedWebp(FileStream stream)
    {
        Span<byte> chunk = stackalloc byte[8];
        if (!TryRead(stream, chunk))
        {
            return null;
        }

        if (!chunk[..4].SequenceEqual("VP8X"u8))
        {
            return false;
        }

        var flags = stream.ReadByte();
        if (flags < 0)
        {
            return null;
        }

        const int animationFlag = 0x02;
        if ((flags & animationFlag) is 0)
        {
            return false;
        }

        stream.Position--;
        var frames = 0;
        while (true)
        {
            // A chunk's data is padded to an even length
            var size = BinaryPrimitives.ReadUInt32LittleEndian(chunk[4..]);
            stream.Position += size + (size & 1);
            if (stream.Position > stream.Length || !TryRead(stream, chunk))
            {
                return false;
            }

            if (chunk[..4].SequenceEqual("ANMF"u8) && ++frames > 1)
            {
                return true;
            }
        }
    }

    /// <summary>
    /// Walks the blocks after the signature, skipping the pixel data, until a second image starts.
    /// </summary>
    private static bool? IsAnimatedGif(FileStream stream)
    {
        Span<byte> screen = stackalloc byte[7];
        if (!TryRead(stream, screen))
        {
            return null;
        }

        stream.Position += ColorTableLength(screen[4]);

        const int imageSeparator = 0x2C;
        const int extensionIntroducer = 0x21;
        const int trailer = 0x3B;
        Span<byte> image = stackalloc byte[9];
        var frames = 0;
        while (true)
        {
            switch (stream.ReadByte())
            {
                case imageSeparator:
                    if (++frames > 1)
                    {
                        return true;
                    }

                    if (!TryRead(stream, image))
                    {
                        return null;
                    }

                    // The local color table, then the LZW code size, then the pixel data in sub-blocks
                    stream.Position += ColorTableLength(image[8]) + 1;
                    break;
                case extensionIntroducer:
                    // The extension's label, then its data in sub-blocks
                    stream.Position++;
                    break;
                case trailer:
                    return false;
                default:
                    // Cut short or damaged before a second image: a decoder may still recover more
                    return null;
            }

            if (!TrySkipSubBlocks(stream))
            {
                return null;
            }
        }
    }

    /// <summary>
    /// The length of a GIF color table, from the packed byte that says whether there is one and how large.
    /// </summary>
    private static int ColorTableLength(byte packed) => (packed & 0x80) is 0 ? 0 : 3 << ((packed & 0x07) + 1);

    private static bool TrySkipSubBlocks(FileStream stream)
    {
        while (true)
        {
            var length = stream.ReadByte();
            switch (length)
            {
                case < 0:
                    return false;
                case 0:
                    return true;
                default:
                    stream.Position += length;
                    break;
            }
        }
    }

    /// <summary>
    /// Reads the brands of an ISO media file, such as an AVIF. A still image names no sequence brand; a sequence
    /// says how many frames it holds in its track's sample table.
    /// </summary>
    private static bool? IsAnimatedIsoMedia(FileStream stream, ReadOnlySpan<byte> majorBrand, uint boxSize)
    {
        const int brandsStart = 16;
        const int maxBoxSize = 1024;
        if (boxSize is < brandsStart or > maxBoxSize)
        {
            return null;
        }

        // The minor version sits between the major brand and the compatible ones
        Span<byte> compatible = stackalloc byte[(int)boxSize - brandsStart];
        stream.Position = brandsStart;
        if (!TryRead(stream, compatible))
        {
            return null;
        }

        var isSequence = IsSequenceBrand(majorBrand);
        var isAvif = majorBrand.SequenceEqual("avif"u8);
        for (var i = 0; i + 4 <= compatible.Length; i += 4)
        {
            var brand = compatible.Slice(i, 4);
            isSequence |= IsSequenceBrand(brand);
            isAvif |= brand.SequenceEqual("avif"u8);
        }

        if (!isSequence)
        {
            return isAvif ? false : null;
        }

        // A sequence can still hold a single frame
        return CountSequenceFrames(stream, boxSize) switch
        {
            null => null,
            var frames => frames > 1
        };
    }

    /// <summary>
    /// Reads the frame count of an image sequence from the sample table of its picture track
    /// (moov, trak, mdia, minf, stbl, stsz), which is there without decoding a frame.
    /// </summary>
    /// <returns>
    /// The largest sample count among the picture tracks, or null when the file has no readable one: no movie box, a
    /// damaged box, or a fragmented file whose samples are listed elsewhere.
    /// </returns>
    private static uint? CountSequenceFrames(FileStream stream, long start)
    {
        if (!TryFindBox(stream, start, stream.Length, "moov"u8, out var movie))
        {
            return null;
        }

        uint? frames = null;
        var next = movie.Start;
        while (TryFindBox(stream, next, movie.End, "trak"u8, out var track))
        {
            next = track.End;
            if (!TryFindBox(stream, track.Start, track.End, "mdia"u8, out var media) || !IsPictureTrack(stream, media))
            {
                continue;
            }

            if (CountSamples(stream, media) is { } samples && samples > (frames ?? 0))
            {
                frames = samples;
            }
        }

        // No samples in the movie box means they are listed in fragments, which this does not walk
        return frames is 0 ? null : frames;
    }

    /// <summary>
    /// Tells a track of pictures from the others a sequence can carry, such as its alpha plane or audio.
    /// </summary>
    private static bool IsPictureTrack(FileStream stream, BoxRange media)
    {
        if (!TryFindBox(stream, media.Start, media.End, "hdlr"u8, out var handler))
        {
            return false;
        }

        // The version and flags, then a reserved field, then the handler type
        Span<byte> content = stackalloc byte[12];
        stream.Position = handler.Start;
        return handler.End - handler.Start >= content.Length && TryRead(stream, content) &&
               content[8..].SequenceEqual("pict"u8);
    }

    private static uint? CountSamples(FileStream stream, BoxRange media)
    {
        if (!TryFindBox(stream, media.Start, media.End, "minf"u8, out var information) ||
            !TryFindBox(stream, information.Start, information.End, "stbl"u8, out var table))
        {
            return null;
        }

        // The sample size box and its compact form both hold the count after eight bytes
        if (!TryFindBox(stream, table.Start, table.End, "stsz"u8, out var sizes) &&
            !TryFindBox(stream, table.Start, table.End, "stz2"u8, out sizes))
        {
            return null;
        }

        Span<byte> content = stackalloc byte[12];
        stream.Position = sizes.Start;
        if (sizes.End - sizes.Start < content.Length || !TryRead(stream, content))
        {
            return null;
        }

        return BinaryPrimitives.ReadUInt32BigEndian(content[8..]);
    }

    /// <summary>
    /// Walks the boxes between <paramref name="start"/> and <paramref name="end"/> for the first of
    /// <paramref name="type"/>, skipping the content of the others.
    /// </summary>
    private static bool TryFindBox(FileStream stream, long start, long end, ReadOnlySpan<byte> type, out BoxRange content)
    {
        // Far more boxes than a real file holds at one level, so a damaged one cannot keep this walking
        const int maxBoxes = 4096;
        const int headerLength = 8;
        const int largeSizeLength = 8;
        content = default;
        Span<byte> header = stackalloc byte[headerLength];
        Span<byte> largeSize = stackalloc byte[largeSizeLength];
        var position = start;
        for (var i = 0; i < maxBoxes && position + headerLength <= end; i++)
        {
            stream.Position = position;
            if (!TryRead(stream, header))
            {
                return false;
            }

            long size = BinaryPrimitives.ReadUInt32BigEndian(header);
            var contentStart = position + headerLength;
            switch (size)
            {
                case 0:
                    // The box runs to the end of what holds it
                    size = end - position;
                    break;
                case 1:
                    if (!TryRead(stream, largeSize))
                    {
                        return false;
                    }

                    size = (long)Math.Min(BinaryPrimitives.ReadUInt64BigEndian(largeSize), long.MaxValue);
                    contentStart += largeSizeLength;
                    break;
            }

            if (size < contentStart - position || size > end - position)
            {
                return false;
            }

            if (header[4..].SequenceEqual(type))
            {
                content = new BoxRange(contentStart, position + size);
                return true;
            }

            position += size;
        }

        return false;
    }

    /// <summary>
    /// Where the content of an ISO media box starts and ends in the file.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    private readonly record struct BoxRange(long Start, long End);

    private static bool IsSequenceBrand(ReadOnlySpan<byte> brand) =>
        brand.SequenceEqual("avis"u8) || brand.SequenceEqual("msf1"u8);

    private static bool TryRead(FileStream stream, Span<byte> buffer) =>
        stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false) == buffer.Length;

    /// <summary>
    ///     Retrieves the compression quality of the specified image file.
    /// </summary>
    /// <param name="file">The path to the image file.</param>
    /// <returns>The compression quality of the image, as a percentage (0-100).</returns>
    /// <remarks>
    ///     This method uses the Magick.NET library to load the image and retrieve the compression quality.
    /// </remarks>
    public static uint GetCompressionQuality(string file)
    {
        using var magickImage = new MagickImage();
        magickImage.Ping(file);
        return magickImage.Quality;
    }
}