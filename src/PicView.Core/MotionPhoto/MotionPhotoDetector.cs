using System.Buffers;
using System.Text;
using PicView.Core.DebugTools;

namespace PicView.Core.MotionPhoto;

/// <summary>
/// Detects whether an image file is a motion photo (Google/Samsung/DJI/OPPO style embedded
/// video, Apple/vivo style sidecar file, or .livp container).
/// <para>
/// XMP metadata is located with plain string searches instead of XML parsing, because vendor
/// namespaces vary widely (GCamera, OpCamera, dji, Samsung...). This mirrors the approach
/// proven in other viewers.
/// </para>
/// </summary>
public static class MotionPhotoDetector
{
    private static readonly byte[] SamsungMarkerBytes = [.. "MotionPhoto_Data"u8];
    private static readonly byte[] XmpMetaStartBytes = [.. "<x:xmpmeta"u8];
    private static readonly byte[] XmpEndTagBytes = [.. "</x:xmpmeta>"u8];

    /// <summary>Scan up to 32 MB from the file tail when searching for the Samsung trailer marker.</summary>
    private const int SamsungScanWindowBytes = 32 * 1024 * 1024;

    /// <summary>Scan up to 1 MB from the file start when searching for an XMP packet.</summary>
    private const int XmpScanWindowBytes = 1024 * 1024;

    // Motion photo videos only exist in these containers. The gate lives here (rather
    // than in each caller) so every detection path behaves identically - e.g. a PNG
    // with a same-named video file must never be flagged as a motion photo.
    public static bool IsMotionPhotoExtension(string extension) =>
        extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".heif", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Attempts to detect motion photo data for the given file.
    /// </summary>
    /// <param name="fileInfo">The image file to inspect.</param>
    /// <param name="xmpPacket">
    /// Optional XMP packet text (e.g. from Magick.NET). When null, a lightweight byte scan
    /// of the file head is used as fallback.
    /// </param>
    /// <param name="fastMode">
    /// When true, significantly limits the backward scan size for Samsung motion photo trailers
    /// (to 512 KB) to prevent disk I/O thrashing during heavy operations like gallery loading.
    /// </param>
    /// <returns>A <see cref="MotionPhotoInfo"/> describing the video location, or null if not a motion photo.</returns>
    public static MotionPhotoInfo? TryDetect(FileInfo fileInfo, string? xmpPacket, bool fastMode = false)
    {
        try
        {
            if (!fileInfo.Exists || fileInfo.Length is 0)
            {
                return null;
            }

            var extension = fileInfo.Extension;
            if (extension.Equals(".livp", StringComparison.OrdinalIgnoreCase))
            {
                return new MotionPhotoInfo { Source = MotionPhotoSource.LivpContainer };
            }
            
            if (!IsMotionPhotoExtension(extension))
            {
                return null;
            }

            xmpPacket ??= ReadXmpPacket(fileInfo);
            if (xmpPacket is { Length: > 0 })
            {
                var fromXmp = TryDetectFromXmp(fileInfo.Length, xmpPacket);
                if (fromXmp is not null)
                {
                    return fromXmp;
                }
            }

            // Samsung motion photos carry a "MotionPhoto_Data" trailer marker in both
            // JPEG and HEIC/HEIF files (HEIC samples without any XMP exist in the wild).
            var samsung = TryDetectSamsungTrailer(fileInfo, fastMode);
            if (samsung is not null)
            {
                return samsung;
            }

            return TryDetectSidecar(fileInfo);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MotionPhotoDetector), nameof(TryDetect), e);
            return null;
        }
    }

    /// <summary>
    /// Searches the XMP packet text for motion photo metadata.
    /// Supports the new Container:Directory standard (Item:Semantic=MotionPhoto + Item:Length)
    /// and the legacy MicroVideo standard (MicroVideoOffset).
    /// </summary>
    internal static MotionPhotoInfo? TryDetectFromXmp(long fileLength, string xmp)
    {
        // New standard: Item:Semantic = MotionPhoto, with Item:Length holding the video byte count.
        // Handle both element form (<Item:Semantic>MotionPhoto</Item:Semantic>)
        // and attribute form (Item:Semantic="MotionPhoto").
        var semanticIndex = xmp.IndexOf(">MotionPhoto<", StringComparison.Ordinal);
        if (semanticIndex < 0)
        {
            semanticIndex = xmp.IndexOf("\"MotionPhoto\"", StringComparison.Ordinal);
        }

        if (semanticIndex >= 0)
        {
            // Only accept an Item:Length that belongs to the same Directory item as the
            // MotionPhoto semantic. The still-image item carries its own Item:Length,
            // which must never be mistaken for the video length.
            var itemEnd = FindItemEnd(xmp, semanticIndex);
            var markerIndex = xmp.IndexOf("Item:Length", semanticIndex, itemEnd - semanticIndex, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                // Attribute-reordered form: Item:Length before Item:Semantic within the same tag.
                var tagStart = xmp.LastIndexOf('<', semanticIndex);
                if (tagStart >= 0)
                {
                    markerIndex = xmp.IndexOf("Item:Length", tagStart, semanticIndex - tagStart, StringComparison.OrdinalIgnoreCase);
                }
            }

            if (markerIndex >= 0)
            {
                var videoLength = ExtractNumberAfter(xmp, markerIndex + "Item:Length".Length);
                if (videoLength is > 0 && videoLength <= fileLength)
                {
                    return new MotionPhotoInfo
                    {
                        Source = MotionPhotoSource.EmbeddedXmp,
                        VideoOffset = fileLength - videoLength.Value,
                        VideoLength = videoLength.Value,
                    };
                }
            }
        }

        // Legacy standard: GCamera:MicroVideoOffset (bytes from end of file)
        var microVideoIndex = xmp.IndexOf("MicroVideoOffset", StringComparison.OrdinalIgnoreCase);
        if (microVideoIndex >= 0)
        {
            var offset = ExtractNumberAfter(xmp, microVideoIndex + "MicroVideoOffset".Length);
            if (offset is > 0 && offset <= fileLength)
            {
                return new MotionPhotoInfo
                {
                    Source = MotionPhotoSource.EmbeddedXmp,
                    VideoOffset = fileLength - offset.Value,
                    VideoLength = offset.Value,
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the end of the Directory item containing the given position: the earliest of the
    /// self-closing tag end (attribute form), the closing item tag (element form) or the start
    /// of the next sibling item. Returns the packet length when no boundary is found.
    /// </summary>
    private static int FindItemEnd(string xmp, int startIndex)
    {
        var end = xmp.Length;
        var selfClose = xmp.IndexOf("/>", startIndex, StringComparison.Ordinal);
        if (selfClose >= 0)
        {
            end = Math.Min(end, selfClose);
        }

        var elementClose = xmp.IndexOf("</Container:Item>", startIndex, StringComparison.OrdinalIgnoreCase);
        if (elementClose >= 0)
        {
            end = Math.Min(end, elementClose);
        }

        var nextItem = xmp.IndexOf("<Container:Item", startIndex, StringComparison.OrdinalIgnoreCase);
        if (nextItem >= 0)
        {
            end = Math.Min(end, nextItem);
        }

        return end;
    }

    /// <summary>
    /// Scans the tail of the file for the legacy Samsung "MotionPhoto_Data" trailer marker.
    /// The video starts immediately after the marker.
    /// </summary>
    /// <param name="fileInfo">The image file to inspect.</param>
    /// <param name="fastMode">
    /// When true, scans only the last 512 KB instead of 32 MB, optimizing for gallery loading performance
    /// at the risk of missing very large trailers.
    /// </param>
    internal static MotionPhotoInfo? TryDetectSamsungTrailer(FileInfo fileInfo, bool fastMode = false)
    {
        var fileLength = fileInfo.Length;
        var minimumSize = SamsungMarkerBytes.Length + 16;
        if (fileLength < minimumSize)
        {
            return null;
        }

        const int chunkSize = 64 * 1024;
        var maxScan = fastMode ? 512 * 1024 : SamsungScanWindowBytes; var windowLength = (int)Math.Min(fileLength, maxScan);
        var scanStart = fileLength - windowLength;
        var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
        try
        {
            using var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite, 4096, FileOptions.SequentialScan);

            var currentEnd = fileLength;
            while (currentEnd > scanStart)
            {
                var chunkStart = Math.Max(scanStart, currentEnd - chunkSize);
                var bytesToRead = (int)(currentEnd - chunkStart);
                if (bytesToRead < SamsungMarkerBytes.Length)
                {
                    break;
                }

                stream.Seek(chunkStart, SeekOrigin.Begin);
                var totalRead = 0;
                while (totalRead < bytesToRead)
                {
                    var read = stream.Read(buffer, totalRead, bytesToRead - totalRead);
                    if (read is 0)
                    {
                        break;
                    }

                    totalRead += read;
                }

                if (totalRead < SamsungMarkerBytes.Length)
                {
                    break;
                }

                var chunkSpan = buffer.AsSpan(0, totalRead);
                var markerIndex = chunkSpan.LastIndexOf(SamsungMarkerBytes);
                if (markerIndex >= 0)
                {
                    var videoStart = chunkStart + markerIndex + SamsungMarkerBytes.Length;
                    if (videoStart >= fileLength)
                    {
                        return null;
                    }

                    // The versionless trailer format keeps the video right after the marker.
                    // Newer mpv2/mpv3 files also carry the marker inside their SEF trailer at
                    // the end of the file, where no video follows - reject those, the video
                    // there must be located via XMP or the extractor's ftyp fallback instead.
                    if (!HasFtypBoxAt(stream, videoStart))
                    {
                        return null;
                    }

                    return new MotionPhotoInfo
                    {
                        Source = MotionPhotoSource.SamsungTrailer,
                        VideoOffset = videoStart,
                        VideoLength = fileLength - videoStart,
                    };
                }

                if (chunkStart <= scanStart || totalRead < bytesToRead)
                {
                    break;
                }

                currentEnd = chunkStart + (SamsungMarkerBytes.Length - 1);
            }

            return null;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MotionPhotoDetector), nameof(TryDetectSamsungTrailer), e);
            return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Looks for a same-named sidecar video file (.mov preferred, then .mp4) next to the image.
    /// The candidate must start with a valid ISO BMFF "ftyp" box, so unrelated same-named
    /// files are not mistaken for a motion photo video.
    /// </summary>
    internal static MotionPhotoInfo? TryDetectSidecar(FileInfo fileInfo)
    {
        var directory = fileInfo.DirectoryName;
        if (string.IsNullOrEmpty(directory))
        {
            return null;
        }

        var baseName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
        foreach (var extension in new[] { ".mov", ".mp4" })
        {
            var sidecar = new FileInfo(Path.Combine(directory, baseName + extension));
            if (sidecar.Exists && sidecar.Length > 0 && HasVideoFileHeader(sidecar))
            {
                return new MotionPhotoInfo
                {
                    Source = MotionPhotoSource.Sidecar,
                    SidecarFile = sidecar,
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Checks whether the file starts with an ISO BMFF box whose type is "ftyp"
    /// (4-byte box size followed by the "ftyp" signature).
    /// </summary>
    private static bool HasVideoFileHeader(FileInfo file) => HasFtypBoxAt(file, 0);

    /// <summary>
    /// Checks whether an ISO BMFF "ftyp" box starts at the given offset inside the file.
    /// </summary>
    private static bool HasFtypBoxAt(Stream stream, long offset)
    {
        Span<byte> header = stackalloc byte[8];
        try
        {
            stream.Seek(offset, SeekOrigin.Begin);
            var totalRead = 0;
            while (totalRead < header.Length)
            {
                var read = stream.Read(header.Slice(totalRead));
                if (read is 0)
                {
                    break;
                }

                totalRead += read;
            }

            return totalRead == header.Length &&
                   header[4] == (byte)'f' && header[5] == (byte)'t' &&
                   header[6] == (byte)'y' && header[7] == (byte)'p';
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MotionPhotoDetector), nameof(HasFtypBoxAt), e);
            return false;
        }
    }

    private static bool HasFtypBoxAt(FileInfo file, long offset)
    {
        try
        {
            using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
            return HasFtypBoxAt(stream, offset);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MotionPhotoDetector), nameof(HasFtypBoxAt), e);
            return false;
        }
    }

    /// <summary>
    /// Reads the XMP packet by locating the "&lt;x:xmpmeta" root element in the head of the
    /// file. Works for JPEG (APP1 XMP segment) and HEIC/HEIF (XMP metadata item) alike, as
    /// both embed the raw packet text. Only the head of the file is scanned.
    /// </summary>
    internal static string? ReadXmpPacket(FileInfo fileInfo)
    {
        var fileLength = fileInfo.Length;
        var minimumSize = XmpMetaStartBytes.Length + 4;
        if (fileLength < minimumSize)
        {
            return null;
        }

        const int chunkSize = 64 * 1024;
        var windowLength = (int)Math.Min(fileLength, XmpScanWindowBytes);
        var initialSize = Math.Min(windowLength, chunkSize);
        var buffer = ArrayPool<byte>.Shared.Rent(initialSize);
        try
        {
            using var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite, 4096, FileOptions.SequentialScan);

            var totalRead = 0;
            while (totalRead < initialSize)
            {
                var read = stream.Read(buffer, totalRead, initialSize - totalRead);
                if (read is 0)
                {
                    break;
                }

                totalRead += read;
            }

            var span = buffer.AsSpan(0, totalRead);
            var packetIndex = span.IndexOf(XmpMetaStartBytes);
            if (packetIndex < 0)
            {
                if (windowLength <= initialSize)
                {
                    return null;
                }

                var ext = fileInfo.Extension;
                if (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return ReadXmpPacketFull(stream, windowLength);
            }

            // Stop at the end of the XMP packet instead of converting the rest of the
            // scan window (mostly image data) into a string.
            var packetSpan = span.Slice(packetIndex);
            var packetEnd = packetSpan.IndexOf(XmpEndTagBytes);
            if (packetEnd >= 0)
            {
                packetSpan = packetSpan.Slice(0, packetEnd + XmpEndTagBytes.Length);
                return Encoding.UTF8.GetString(packetSpan);
            }

            return ReadXmpPacketFull(stream, windowLength, packetIndex);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MotionPhotoDetector), nameof(ReadXmpPacket), e);
            return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static string? ReadXmpPacketFull(Stream stream, int windowLength, int knownStartIndex = -1)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(windowLength);
        try
        {
            stream.Seek(0, SeekOrigin.Begin);
            var totalRead = 0;
            while (totalRead < windowLength)
            {
                var read = stream.Read(buffer, totalRead, windowLength - totalRead);
                if (read is 0)
                {
                    break;
                }

                totalRead += read;
            }

            var span = buffer.AsSpan(0, totalRead);
            var packetIndex = knownStartIndex >= 0 ? knownStartIndex : span.IndexOf(XmpMetaStartBytes);
            if (packetIndex < 0)
            {
                return null;
            }

            var packetSpan = span.Slice(packetIndex);
            var packetEnd = packetSpan.IndexOf(XmpEndTagBytes);
            if (packetEnd >= 0)
            {
                packetSpan = packetSpan.Slice(0, packetEnd + XmpEndTagBytes.Length);
            }

            return Encoding.UTF8.GetString(packetSpan);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MotionPhotoDetector), nameof(ReadXmpPacketFull), e);
            return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Extracts the first run of ASCII digits found at or after <paramref name="startIndex"/>,
    /// which allows handling both attribute ("Item:Length="123"") and element
    /// ("&lt;Item:Length&gt;123&lt;/Item:Length&gt;") XMP forms without parsing XML.
    /// </summary>
    private static long? ExtractNumberAfter(string text, int startIndex)
    {
        var index = startIndex;
        while (index < text.Length && !char.IsAsciiDigit(text[index]))
        {
            index++;
        }

        if (index >= text.Length)
        {
            return null;
        }

        long value = 0;
        var digitCount = 0;
        while (index < text.Length && char.IsAsciiDigit(text[index]))
        {
            if (value > long.MaxValue / 10)
            {
                return null;
            }

            value = value * 10 + (text[index] - '0');
            digitCount++;
            index++;
        }

        return digitCount is 0 ? null : value;
    }
}
