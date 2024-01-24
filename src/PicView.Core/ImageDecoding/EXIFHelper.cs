using ImageMagick;

namespace PicView.Core.ImageDecoding;

public static class EXIFHelper
{
    public enum EXIFOrientation
    {
        None = 0,
        Normal = 1,
        Flipped = 2,
        Rotated180 = 3,
        Rotated180Flipped = 4,
        Rotated270Flipped = 5,
        Rotated90 = 6,
        Rotated90Flipped = 7,
        Rotated270 = 8
    }

    // 0 = none
    // 1 = 0 degrees
    // 2 = 0 degrees, flipped
    // 3 = 180 degrees
    // 4 = 180 degrees, flipped
    // 5 = 270 degrees, flipped
    // 6 = 90 degrees
    // 7 = 90 degrees, flipped
    // 8 = 270 degrees, flipped
    public static EXIFOrientation GetImageOrientation(MagickImage image)
    {
        var profile = image.GetExifProfile();

        var orientationValue = profile?.GetValue(ExifTag.Orientation);
        if (orientationValue is null)
        {
            return EXIFOrientation.None;
        }

        return orientationValue.Value switch
        {
            1 => EXIFOrientation.Normal,
            2 => EXIFOrientation.Flipped,
            3 => EXIFOrientation.Rotated180,
            4 => EXIFOrientation.Rotated180Flipped,
            5 => EXIFOrientation.Rotated270Flipped,
            6 => EXIFOrientation.Rotated90,
            7 => EXIFOrientation.Rotated90Flipped,
            8 => EXIFOrientation.Rotated270,
            _ => EXIFOrientation.None
        };
    }

    // ReSharper disable once InconsistentNaming
    public static bool SetEXIFRating(string filePath, ushort rating)
    {
        using var image = new MagickImage(filePath);
        var profile = image?.GetExifProfile();
        if (profile is null)
        {
            profile = new ExifProfile(filePath);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (profile is null || image is null)
                return false;
        }
        else if (image is null)
            return false;

        profile.SetValue(ExifTag.Rating, rating);
        image.SetProfile(profile);

        image.Write(filePath);
        return true;
    }
}