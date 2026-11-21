using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.Exif;
using PicView.Core.ImageDecoding;

namespace PicView.Core.Gallery;

/// <summary>
/// What a gallery item reads from its file: the image's upright pixel size, the size it is shown at, with width and
/// height swapped for a file whose EXIF orientation is a quarter turn.
/// A class, so an item holds it as one reference that another thread replaces whole: a reader never pairs a width
/// from one read with a height from another.
/// </summary>
/// <remarks>
/// The XMP packet the motion photo badge needs is not read here: every file of the gallery is read for its details,
/// while the badge is detected only for the items shown, and only when the badges are shown, by
/// <see cref="MotionPhoto.MotionPhotoDetector"/>, which reads the packet itself.
/// </remarks>
public sealed record GalleryFileDetails(uint PixelWidth, uint PixelHeight)
{
    /// <summary>
    /// The details of a file that could not be read as an image: no pixel size.
    /// </summary>
    public static GalleryFileDetails Unreadable { get; } = new(0, 0);

    /// <summary>
    /// Reads <paramref name="file"/>'s details: from its header where <see cref="ImageSizeReader"/> knows the format
    /// and can vouch for its orientation, which is most photos, and else by an ImageMagick ping.
    /// </summary>
    /// <returns>The details, with the upright pixel size (width and height swapped for a file whose EXIF orientation
    /// is a quarter turn), or null when the file could not be read as an image.</returns>
    public static GalleryFileDetails? Read(FileInfo file)
    {
        var path = file.FullName;
        if (ImageSizeReader.IsContainer(Path.GetExtension(path.AsSpan())))
        {
            // A .b64 or .livp file holds an image without being one, and ImageMagick only throws on it
            return null;
        }

        if (ImageSizeReader.TryReadUprightHeader(path, out var size))
        {
            return new GalleryFileDetails(size.Width, size.Height);
        }

        try
        {
            using var magick = new MagickImage();
            ImageSizeReader.Ping(magick, path);
            return IsQuarterTurned(magick)
                ? new GalleryFileDetails(magick.Height, magick.Width)
                : new GalleryFileDetails(magick.Width, magick.Height);
        }
        catch (Exception ex)
        {
#if DEBUG
            DebugHelper.LogDebug(nameof(GalleryFileDetails), nameof(Read), ex);
#endif
            return null;
        }
    }

    /// <summary>
    /// Whether the EXIF orientation of the already pinged <paramref name="magick"/> is a quarter turn, which stores
    /// the file lying on its side.
    /// </summary>
    private static bool IsQuarterTurned(MagickImage magick) =>
        ExifOrientationHelper.GetImageOrientation(magick) is ExifOrientation.MirrorHorizontalRotate270Cw
            or ExifOrientation.Rotate90Cw
            or ExifOrientation.MirrorHorizontalRotate90Cw
            or ExifOrientation.Rotated270Cw;
}
