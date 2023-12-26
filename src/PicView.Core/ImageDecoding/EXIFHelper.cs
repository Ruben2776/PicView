using ImageMagick;

namespace PicView.Core.ImageDecoding;

public static class EXIFHelper
{
    // 0 = none
    // 1 = 0 degrees
    // 2 = 0 degrees, flipped
    // 3 = 180 degrees
    // 4 = 180 degrees, flipped
    // 5 = 270 degrees, flipped
    // 6 = 90 degrees
    // 7 = 90 degrees, flipped
    // 8 = 270 degrees, flipped
    public static ushort GetImageOrientation(MagickImage image)
    {
        var profile = image.GetExifProfile();

        var orientationValue = profile?.GetValue(ExifTag.Orientation);
        return orientationValue?.Value ?? 0;
    }
}