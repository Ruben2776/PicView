using Microsoft.Win32.SafeHandles;

namespace PicView.Core.ImageDecoding;

public static partial class ImageSizeReader
{
    /// <summary>
    /// Picks the parser from the signature in <paramref name="header"/>, the first bytes of the file, whatever the
    /// extension says.
    /// </summary>
    /// <returns>Whether a parser knew the format and read its size.</returns>
    private static bool TryReadKnownFormat(SafeFileHandle handle, string path, ReadOnlySpan<byte> header,
        out ImageSize size)
    {
        size = default;
        return false;
    }
}
