using System.Security;
using ImageMagick;
using Microsoft.Win32.SafeHandles;

namespace PicView.Core.ImageDecoding;

/// <summary>
/// Reads the pixel size of an image file. The common formats answer from their header, without ImageMagick; the rest
/// are pinged by ImageMagick. It never throws: a file that cannot be read as an image gives an empty size.
/// </summary>
public static partial class ImageSizeReader
{
    /// <summary>
    /// The bytes read from the start of the file to tell its format. The formats with a fixed header answer from
    /// these alone.
    /// </summary>
    private const int SignatureLength = 32;

    /// <inheritdoc cref="Read(string?)"/>
    public static ImageSize Read(FileInfo? file) => Read(file?.FullName);

    /// <summary>
    /// Reads the stored pixel size of the image at <paramref name="path"/>, with no EXIF orientation applied.
    /// </summary>
    /// <returns>The size, or an empty one when the file is missing, cannot be opened or is not an image.</returns>
    public static ImageSize Read(string? path)
    {
        if (string.IsNullOrEmpty(path) || IsContainer(Path.GetExtension(path.AsSpan())))
        {
            return default;
        }

        if (TryReadHeader(path, out var size, out var opened))
        {
            return size;
        }

        // A file that could not even be opened would only make ImageMagick throw
        return opened ? PingWithMagick(path) : default;
    }

    /// <summary>
    /// Reads the size from the header alone, without ImageMagick.
    /// </summary>
    /// <returns>Whether the size was read. False for a format without a parser here, and for a header that is cut
    /// short, damaged or a variant the parser does not handle.</returns>
    internal static bool TryReadHeader(string path, out ImageSize size) => TryReadHeader(path, out size, out _);

    /// <param name="opened">Whether the file could be opened and read from, so another reader may still make
    /// something of it.</param>
    /// <inheritdoc cref="TryReadHeader(string, out ImageSize)"/>
    private static bool TryReadHeader(string path, out ImageSize size, out bool opened)
    {
        size = default;
        opened = false;
        try
        {
            using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            opened = true;

            Span<byte> buffer = stackalloc byte[SignatureLength];
            ReadOnlySpan<byte> header = buffer[..ReadUpTo(handle, 0, buffer)];
            return TryReadKnownFormat(handle, path, header, out size) && !size.IsEmpty;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException
                                       or ArgumentException or SecurityException)
        {
            size = default;
            return false;
        }
    }

    /// <summary>
    /// Whether <paramref name="extension"/> names a container that holds an image without being one: the image model
    /// reads these in its own way, and ImageMagick only throws on them.
    /// </summary>
    private static bool IsContainer(ReadOnlySpan<char> extension) =>
        extension.Equals(".b64", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".livp", StringComparison.OrdinalIgnoreCase);

    private static ImageSize PingWithMagick(string path)
    {
        try
        {
            using var magick = new MagickImage();
            magick.Ping(path);
            return new ImageSize(magick.Width, magick.Height);
        }
        catch (Exception)
        {
            return default;
        }
    }

    /// <summary>
    /// Fills <paramref name="buffer"/> from <paramref name="offset"/> in the file.
    /// </summary>
    /// <returns>Whether the buffer was filled; false when the file ends first.</returns>
    private static bool TryReadAt(SafeFileHandle handle, long offset, Span<byte> buffer) =>
        ReadUpTo(handle, offset, buffer) == buffer.Length;

    /// <summary>
    /// Reads from <paramref name="offset"/> until <paramref name="buffer"/> is full or the file ends.
    /// </summary>
    /// <returns>The number of bytes read.</returns>
    private static int ReadUpTo(SafeFileHandle handle, long offset, Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = RandomAccess.Read(handle, buffer[total..], offset + total);
            if (read is 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }
}
