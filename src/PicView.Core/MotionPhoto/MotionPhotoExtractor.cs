using System.Buffers.Binary;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using SharpCompress.Archives;
using SharpCompress.Readers;

namespace PicView.Core.MotionPhoto;

/// <summary>
/// Extracts the video portion of a motion photo as a seekable stream, without writing
/// temporary files. Embedded videos are sliced from the source file (with ftyp box
/// validation to tolerate vendor-specific trailing blocks), sidecars are opened directly
/// and .livp containers are decompressed in memory.
/// </summary>
public static class MotionPhotoExtractor
{
    /// <summary>Safety cap to avoid allocating unreasonable amounts of memory on corrupt metadata.</summary>
    private const long MaxVideoBytes = 256 * 1024 * 1024;

    /// <summary>
    /// The search window (in bytes, both directions) used to correct the expected video start
    /// position. Some vendors (e.g. DJI) append trailing blocks after the video, which shifts
    /// the naive "last N bytes" slice into the middle of the stream.
    /// </summary>
    private const int FtypSearchWindowBytes = 8 * 1024;

    private const int BoxHeaderSize = 8;

    /// <summary>
    /// Extracts the motion photo video described by <paramref name="info"/>.
    /// </summary>
    /// <returns>A seekable stream positioned at zero, or null when extraction fails.</returns>
    public static async ValueTask<Stream?> ExtractAsync(FileInfo fileInfo, MotionPhotoInfo info, CancellationToken ct = default)
    {
        try
        {
            return info.Source switch
            {
                MotionPhotoSource.EmbeddedXmp or MotionPhotoSource.SamsungTrailer =>
                    await ExtractEmbeddedAsync(fileInfo, info.VideoOffset, ct).ConfigureAwait(false),
                MotionPhotoSource.Sidecar => OpenReadOnly(info.SidecarFile),
                MotionPhotoSource.LivpContainer =>
                    await ExtractLivpEntryToMemoryAsync(fileInfo, IsVideoFileName, ct).ConfigureAwait(false),
                _ => null,
            };
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MotionPhotoExtractor), nameof(ExtractAsync), e);
            return null;
        }
    }

    /// <summary>
    /// Extracts the cover image of a .livp container to a temporary file so the regular
    /// image decoding pipeline can process it.
    /// </summary>
    /// <returns>The path of the temporary image file, or null when the container holds no image.</returns>
    public static async ValueTask<string?> ExtractLivpCoverToTempFileAsync(FileInfo fileInfo, CancellationToken ct = default)
    {
        try
        {
            var stream = await ExtractLivpEntryToMemoryAsync(fileInfo, IsImageFileName, ct).ConfigureAwait(false);
            if (stream is null)
            {
                return null;
            }

            await using (stream.ConfigureAwait(false))
            {
                var tempPath = TempFileManager.GetNewTempFilePath("livp-cover.jpg");
                var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write,
                    FileShare.Read, 4096, FileOptions.Asynchronous);
                await using (fileStream.ConfigureAwait(false))
                {
                    await stream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
                }

                return tempPath;
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MotionPhotoExtractor), nameof(ExtractLivpCoverToTempFileAsync), e);
            return null;
        }
    }

    private static async ValueTask<Stream?> ExtractEmbeddedAsync(FileInfo fileInfo, long expectedStart, CancellationToken ct)
    {
        var fileLength = fileInfo.Length;
        if (expectedStart < 0 || expectedStart >= fileLength)
        {
            return null;
        }

        var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using (stream.ConfigureAwait(false))
        {
            var start = await LocateVideoStartAsync(stream, expectedStart, fileLength, ct).ConfigureAwait(false);
            if (start is null)
            {
                return null;
            }

            var length = fileLength - start.Value;
            if (length <= 0 || length > MaxVideoBytes)
            {
                return null;
            }

            // Hand out a live window over the source file instead of copying the whole
            // video into memory; playback can start immediately and uses no extra RAM.
            return new FileSliceStream(fileInfo.FullName, start.Value, length);
        }
    }

    private static Stream? OpenReadOnly(FileInfo? file)
    {
        if (file is null || !file.Exists || file.Length is 0)
        {
            return null;
        }

        return new FileStream(file.FullName, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    private static async ValueTask<Stream?> ExtractLivpEntryToMemoryAsync(
        FileInfo fileInfo, Func<string, bool> entryPredicate, CancellationToken ct)
    {
        if (!fileInfo.Exists || fileInfo.Length is 0 || fileInfo.Length > MaxVideoBytes)
        {
            return null;
        }

        var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using (stream.ConfigureAwait(false))
        {
            using var archive = ArchiveFactory.OpenArchive(stream, new ReaderOptions());
            foreach (var entry in archive.Entries)
            {
                if (entry.IsDirectory || entry.Key is null || !entryPredicate(entry.Key))
                {
                    continue;
                }

                var size = entry.Size;
                if (size <= 0 || size > MaxVideoBytes)
                {
                    continue;
                }

                var memory = new MemoryStream((int)size);
                var entryStream = entry.OpenEntryStream();
                await using (entryStream.ConfigureAwait(false))
                {
                    await entryStream.CopyToAsync(memory, ct).ConfigureAwait(false);
                }

                memory.Position = 0;
                return memory;
            }
        }

        return null;
    }

    /// <summary>
    /// Verifies that the expected video start position is a valid MP4 "ftyp" box; otherwise
    /// searches a ±8 KB window for the closest valid ftyp box. As a last resort the whole
    /// file is scanned, because some files carry metadata that disagrees with reality
    /// (e.g. Samsung mpv2 files whose XMP Item:Length points at the SEF trailer while the
    /// video actually sits in the middle of the file).
    /// </summary>
    internal static async ValueTask<long?> LocateVideoStartAsync(Stream stream, long expectedStart, long fileLength, CancellationToken ct)
    {
        if (await IsFtypBoxAtAsync(stream, expectedStart, fileLength, ct).ConfigureAwait(false))
        {
            return expectedStart;
        }

        var windowStart = Math.Max(0, expectedStart - FtypSearchWindowBytes);
        var windowEnd = Math.Min(fileLength, expectedStart + FtypSearchWindowBytes + BoxHeaderSize);
        var windowLength = (int)(windowEnd - windowStart);
        if (windowLength > BoxHeaderSize)
        {
            var buffer = new byte[windowLength];
            stream.Seek(windowStart, SeekOrigin.Begin);
            var totalRead = 0;
            while (totalRead < windowLength)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(totalRead, windowLength - totalRead), ct).ConfigureAwait(false);
                if (read is 0)
                {
                    break;
                }

                totalRead += read;
            }

            var windowed = FindFtypStart(buffer.AsSpan(0, totalRead), windowStart, expectedStart, fileLength);
            if (windowed is not null)
            {
                return windowed;
            }
        }

        return await FindVideoFtypInFileAsync(stream, fileLength, ct).ConfigureAwait(false);
    }

    /// <summary>Chunk size used by the whole-file ftyp fallback scan.</summary>
    private const int FtypFileScanChunkBytes = 1024 * 1024;

    /// <summary>
    /// Scans the entire file for ISO BMFF "ftyp" boxes and returns the last one whose major
    /// brand is not a HEIF/AVIF image brand (those belong to the still image at the start
    /// of the file, not to the embedded video). Used only when the metadata-derived
    /// position turned out to be wrong.
    /// </summary>
    private static async ValueTask<long?> FindVideoFtypInFileAsync(Stream stream, long fileLength, CancellationToken ct)
    {
        const int overlap = 16; // box size (4) + "ftyp" (4) + major brand (4), plus slack
        var buffer = new byte[FtypFileScanChunkBytes + overlap];
        long? lastCandidate = null;
        long position = 0;
        stream.Seek(0, SeekOrigin.Begin);
        while (position < fileLength)
        {
            var toRead = (int)Math.Min(buffer.Length, fileLength - position);
            var totalRead = 0;
            while (totalRead < toRead)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(totalRead, toRead - totalRead), ct).ConfigureAwait(false);
                if (read is 0)
                {
                    break;
                }

                totalRead += read;
            }

            if (totalRead < BoxHeaderSize)
            {
                break;
            }

            var span = buffer.AsSpan(0, totalRead);
            for (var i = 4; i + BoxHeaderSize + 4 <= span.Length; i++)
            {
                if (span[i] != (byte)'f' || span[i + 1] != (byte)'t' ||
                    span[i + 2] != (byte)'y' || span[i + 3] != (byte)'p')
                {
                    continue;
                }

                var boxStart = position + i - 4;
                var boxSize = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(i - 4));
                if (boxSize < BoxHeaderSize || boxStart + boxSize > fileLength)
                {
                    continue;
                }

                if (IsImageBrand(span.Slice(i + 4, 4)))
                {
                    continue;
                }

                lastCandidate = boxStart;
            }

            // Stop at EOF; otherwise step back so a box header straddling the
            // chunk boundary is re-examined in the next chunk.
            if (totalRead < toRead || toRead < buffer.Length)
            {
                break;
            }

            position += totalRead - overlap;
            stream.Seek(position, SeekOrigin.Begin);
        }

        return lastCandidate;
    }

    /// <summary>
    /// Whether the 4-byte ISO BMFF brand identifies a still-image file
    /// (HEIF/AVIF variants) rather than a video.
    /// </summary>
    private static bool IsImageBrand(ReadOnlySpan<byte> brand)
    {
        Span<byte> lower = stackalloc byte[4];
        for (var i = 0; i < 4; i++)
        {
            var b = brand[i];
            lower[i] = b is >= (byte)'A' and <= (byte)'Z' ? (byte)(b + 32) : b;
        }

        return lower.SequenceEqual("heic"u8) || lower.SequenceEqual("heix"u8) ||
               lower.SequenceEqual("hevc"u8) || lower.SequenceEqual("heif"u8) ||
               lower.SequenceEqual("mif1"u8) || lower.SequenceEqual("msf1"u8) ||
               lower.SequenceEqual("avif"u8) || lower.SequenceEqual("avis"u8);
    }

    /// <summary>
    /// Scans a byte window for the valid ftyp box closest to <paramref name="expectedStart"/>.
    /// </summary>
    internal static long? FindFtypStart(ReadOnlySpan<byte> window, long windowStart, long expectedStart, long fileLength)
    {
        long? best = null;
        var bestDistance = long.MaxValue;
        for (var offset = 0; offset + BoxHeaderSize <= window.Length; offset++)
        {
            var absolute = windowStart + offset;
            var distance = Math.Abs(absolute - expectedStart);
            if (distance >= bestDistance)
            {
                continue;
            }

            if (IsValidFtypBox(window.Slice(offset), absolute, fileLength))
            {
                best = absolute;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static async ValueTask<bool> IsFtypBoxAtAsync(Stream stream, long offset, long fileLength, CancellationToken ct)
    {
        if (offset < 0 || offset + BoxHeaderSize > fileLength)
        {
            return false;
        }

        var header = new byte[BoxHeaderSize];
        stream.Seek(offset, SeekOrigin.Begin);
        var read = await stream.ReadAsync(header.AsMemory(0, BoxHeaderSize), ct).ConfigureAwait(false);
        return read == BoxHeaderSize && IsValidFtypBox(header, offset, fileLength);
    }

    private static bool IsValidFtypBox(ReadOnlySpan<byte> bytes, long absoluteOffset, long fileLength)
    {
        if (bytes.Length < BoxHeaderSize)
        {
            return false;
        }

        if (bytes[4] != (byte)'f' || bytes[5] != (byte)'t' || bytes[6] != (byte)'y' || bytes[7] != (byte)'p')
        {
            return false;
        }

        var boxSize = BinaryPrimitives.ReadUInt32BigEndian(bytes);
        return boxSize >= BoxHeaderSize && absoluteOffset + boxSize <= fileLength;
    }

    private static bool IsVideoFileName(string name)
    {
        var extension = Path.GetExtension(name);
        return extension.Equals(".mov", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImageFileName(string name)
    {
        var extension = Path.GetExtension(name);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".heif", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".png", StringComparison.OrdinalIgnoreCase);
    }
}
