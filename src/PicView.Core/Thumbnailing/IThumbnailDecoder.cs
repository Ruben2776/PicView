namespace PicView.Core.Thumbnailing;

/// <summary>
/// Turns files and images into thumbnail bitmaps for <see cref="Thumbnails"/>, which decides when each is used. The
/// platform's image library sits behind it; every method may block, and each returns a new bitmap or null.
/// </summary>
public interface IThumbnailDecoder
{
    /// <summary>
    /// A thumbnail the file or the OS already has: the shell's, then the file's EXIF thumbnail. Never decodes the image.
    /// </summary>
    object? GetReadyMade(FileInfo file, uint height);

    /// <summary>
    /// Reads the file and makes a thumbnail no taller than <paramref name="height"/>.
    /// </summary>
    object? Decode(FileInfo file, uint height);

    /// <summary>
    /// A new bitmap of <paramref name="image"/>, an already decoded still image, no taller than
    /// <paramref name="height"/>; null when it cannot scale it. Must not keep or dispose <paramref name="image"/>.
    /// </summary>
    object? ScaleDown(object image, uint height);
}
