using ImageMagick;

namespace PicView.Core.ImageTransformations;

/// <summary>
/// Cuts a crop's area out of an image file and turns it to match what the user saw.
/// </summary>
public static class CropRender
{
    /// <summary>
    /// In place: applies the file's EXIF orientation (the viewer shows it that way, so
    /// <paramref name="source"/> is in that frame), cuts <paramref name="source"/>, then turns and flips the cut.
    /// </summary>
    public static void CutAndOrient(MagickImage image, PixelArea source, Orientation orientation)
    {
        image.AutoOrient();
        image.Crop(new MagickGeometry(source.X, source.Y, source.Width, source.Height));
        image.ResetPage();

        if (orientation.Degrees != 0)
        {
            image.Rotate(orientation.Degrees);
        }

        if (orientation.IsFlipped)
        {
            image.Flop();
        }
    }
}
