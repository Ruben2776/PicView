using PicView.Core.Exif;

namespace PicView.Core.Metadata;

/// <summary>
/// Writes edited AI Metadata back to the place it was read from.
/// </summary>
public static class SdMetadataWriter
{
    /// <summary>Writes <paramref name="text"/> over the AI Metadata of <paramref name="file"/>.</summary>
    /// <param name="current">What was read from the file. Only editable data is written.</param>
    /// <returns>False when the data is not editable, or nothing was written.</returns>
    public static Task<bool> WriteAsync(FileInfo file, SdMetadata current, string text)
    {
        if (!current.IsEditable)
        {
            return Task.FromResult(false);
        }

        switch (current.Source)
        {
            case SdSource.PngText when current.PngKeyword is { } keyword:
                return ExifFunctions.TryRewriteFileAsync(file, bytes => PngTextChunkWriter.ReplaceText(bytes, keyword, text),
                    nameof(SdMetadataWriter), nameof(WriteAsync));
            case SdSource.ExifUserComment:
                // A JPEG is never encoded again, which would lose quality. A WebP has no segments to rewrite
                return ExifWriter.SetUserCommentAsync(file, text, allowEncode: current.Container == SdContainer.WebP);
            default:
                return Task.FromResult(false);
        }
    }
}
