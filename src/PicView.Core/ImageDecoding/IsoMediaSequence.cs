using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace PicView.Core.ImageDecoding;

/// <summary>
/// Reads what the container of an ISO media image sequence, such as an animated AVIF, says about its frames: how many
/// there are and how long each one shows. It walks the boxes of the picture track
/// (moov, trak, mdia, minf, stbl) and decodes nothing.
/// </summary>
public static class IsoMediaSequence
{
    // More frames than an image sequence holds; a larger count is read as damage
    private const uint MaxFrames = 1 << 20;

    /// <summary>
    /// Reads the duration of every frame from the time-to-sample table of the picture track.
    /// </summary>
    /// <param name="stream">A seekable stream over the whole file. Its position is moved.</param>
    /// <returns>
    /// One duration per frame, or null when the file has no readable picture track: no movie box, a damaged box, or a
    /// fragmented file whose samples are listed elsewhere.
    /// </returns>
    public static TimeSpan[]? ReadFrameDurations(Stream stream)
    {
        try
        {
            if (!TryFindPictureTrack(stream, 0, out var track) || track.Samples is 0 or > MaxFrames)
            {
                return null;
            }

            var timescale = ReadTimescale(stream, track.Media);
            if (timescale is null or 0 ||
                !TryFindBox(stream, track.SampleTable.Start, track.SampleTable.End, "stts"u8, out var times))
            {
                return null;
            }

            return ReadDurations(stream, times, (int)track.Samples, timescale.Value);
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or ObjectDisposedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Tells whether the file carries an auxiliary track, which is where a sequence keeps its alpha plane.
    /// </summary>
    /// <param name="stream">A seekable stream over the whole file. Its position is moved.</param>
    public static bool HasAuxiliaryTrack(Stream stream)
    {
        try
        {
            if (!TryFindBox(stream, 0, stream.Length, "moov"u8, out var movie))
            {
                return false;
            }

            var next = movie.Start;
            while (TryFindBox(stream, next, movie.End, "trak"u8, out var track))
            {
                next = track.End;
                if (TryFindBox(stream, track.Start, track.End, "mdia"u8, out var media) &&
                    HasHandler(stream, media, "auxv"u8))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or ObjectDisposedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Reads the frame count from the sample size table of the picture track, walking the boxes from
    /// <paramref name="start"/>.
    /// </summary>
    /// <returns>
    /// The largest sample count among the picture tracks, or null when the file has no readable one: no movie box, a
    /// damaged box, or a fragmented file whose samples are listed elsewhere.
    /// </returns>
    internal static uint? CountFrames(Stream stream, long start) =>
        TryFindPictureTrack(stream, start, out var track) && track.Samples > 0 ? track.Samples : null;

    /// <summary>
    /// Finds the picture track with the most samples. A sequence can carry other tracks, such as its alpha plane.
    /// </summary>
    private static bool TryFindPictureTrack(Stream stream, long start, out PictureTrack found)
    {
        found = default;
        if (!TryFindBox(stream, start, stream.Length, "moov"u8, out var movie))
        {
            return false;
        }

        var any = false;
        var next = movie.Start;
        while (TryFindBox(stream, next, movie.End, "trak"u8, out var track))
        {
            next = track.End;
            if (!TryFindBox(stream, track.Start, track.End, "mdia"u8, out var media) ||
                !HasHandler(stream, media, "pict"u8) ||
                !TryFindBox(stream, media.Start, media.End, "minf"u8, out var information) ||
                !TryFindBox(stream, information.Start, information.End, "stbl"u8, out var table) ||
                CountSamples(stream, table) is not { } samples)
            {
                continue;
            }

            if (!any || samples > found.Samples)
            {
                found = new PictureTrack(media, table, samples);
                any = true;
            }
        }

        return any;
    }

    /// <summary>
    /// Tells what a track holds: pictures, an auxiliary plane of them, sound and so on.
    /// </summary>
    private static bool HasHandler(Stream stream, BoxRange media, ReadOnlySpan<byte> type)
    {
        if (!TryFindBox(stream, media.Start, media.End, "hdlr"u8, out var handler))
        {
            return false;
        }

        // The version and flags, then a reserved field, then the handler type
        Span<byte> content = stackalloc byte[12];
        stream.Position = handler.Start;
        return handler.End - handler.Start >= content.Length && TryRead(stream, content) &&
               content[8..].SequenceEqual(type);
    }

    private static uint? CountSamples(Stream stream, BoxRange table)
    {
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
    /// Reads how many units of the track's time make a second, from its media header.
    /// </summary>
    private static uint? ReadTimescale(Stream stream, BoxRange media)
    {
        if (!TryFindBox(stream, media.Start, media.End, "mdhd"u8, out var header))
        {
            return null;
        }

        // The version and flags, then two dates of four bytes each, or eight in version 1, then the timescale
        Span<byte> content = stackalloc byte[24];
        stream.Position = header.Start;
        if (header.End - header.Start < content.Length || !TryRead(stream, content))
        {
            return null;
        }

        return BinaryPrimitives.ReadUInt32BigEndian(content[(content[0] is 1 ? 20 : 12)..]);
    }

    /// <summary>
    /// Expands the time-to-sample table, which gives a duration for each run of samples sharing it.
    /// </summary>
    private static TimeSpan[]? ReadDurations(Stream stream, BoxRange times, int frames, uint timescale)
    {
        Span<byte> entry = stackalloc byte[8];
        stream.Position = times.Start;
        if (times.End - times.Start < entry.Length || !TryRead(stream, entry))
        {
            return null;
        }

        var runs = BinaryPrimitives.ReadUInt32BigEndian(entry[4..]);
        if (runs > (times.End - times.Start - entry.Length) / entry.Length)
        {
            return null;
        }

        var durations = new TimeSpan[frames];
        var filled = 0;
        var duration = TimeSpan.Zero;
        for (var run = 0u; run < runs && filled < frames; run++)
        {
            if (!TryRead(stream, entry))
            {
                return null;
            }

            var count = BinaryPrimitives.ReadUInt32BigEndian(entry);
            duration = TimeSpan.FromSeconds(BinaryPrimitives.ReadUInt32BigEndian(entry[4..]) / (double)timescale);
            var end = (int)Math.Min(frames, filled + (long)count);
            durations.AsSpan(filled, end - filled).Fill(duration);
            filled = end;
        }

        if (filled is 0)
        {
            return null;
        }

        // A table that lists fewer samples than the track holds leaves the rest at the last duration
        durations.AsSpan(filled).Fill(duration);
        return durations;
    }

    /// <summary>
    /// Walks the boxes between <paramref name="start"/> and <paramref name="end"/> for the first of
    /// <paramref name="type"/>, skipping the content of the others.
    /// </summary>
    private static bool TryFindBox(Stream stream, long start, long end, ReadOnlySpan<byte> type, out BoxRange content)
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

    private static bool TryRead(Stream stream, Span<byte> buffer) =>
        stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false) == buffer.Length;

    /// <summary>
    /// Where the content of an ISO media box starts and ends in the file.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    private readonly record struct BoxRange(long Start, long End);

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct PictureTrack(BoxRange Media, BoxRange SampleTable, uint Samples);
}
