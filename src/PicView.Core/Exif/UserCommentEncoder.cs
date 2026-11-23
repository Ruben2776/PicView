using System.Text;

namespace PicView.Core.Exif;

/// <summary>
/// Builds the bytes of an Exif UserComment: an 8-byte character code, then the text.
/// Mirrors what <see cref="ExifReader.GetUserComment"/> decodes.
/// </summary>
internal static class UserCommentEncoder
{
    /// <param name="text">The comment.</param>
    /// <param name="useUtf8">
    /// Whether non-ASCII text is written as UTF-8, which Exif 3.0 uses, rather than UTF-16. The caller passes
    /// <c>string.CompareOrdinal(ExifReader.GetExifVersion(profile), "0300") >= 0</c>, as the reader does.
    /// </param>
    internal static byte[] Encode(string text, bool useUtf8)
    {
        if (IsAscii(text))
        {
            return [.. "ASCII\0\0\0"u8, .. Encoding.ASCII.GetBytes(text)];
        }

        return [.. "UNICODE\0"u8, .. useUtf8 ? Encoding.UTF8.GetBytes(text) : Encoding.Unicode.GetBytes(text)];
    }

    private static bool IsAscii(string text)
    {
        foreach (var c in text)
        {
            if (c > 0x7F)
            {
                return false;
            }
        }

        return true;
    }
}
