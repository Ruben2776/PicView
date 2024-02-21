using System.Globalization;
using ImageMagick;
using PicView.Core.Localization;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    public static async Task<bool> SetEXIFRating(string filePath, ushort rating)
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

        await image.WriteAsync(filePath);
        return true;
    }

    public static IExifProfile? GetExifProfile(string path)
    {
        using var magick = new MagickImage();
        try
        {
            magick.Ping(path);
            return magick.GetExifProfile();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static string GetDateTaken(IExifProfile profile)
    {
        var getDateTaken =
            profile?.GetValue(ExifTag.DateTime)?.Value ??
            profile?.GetValue(ExifTag.DateTimeOriginal)?.Value ??
            profile?.GetValue(ExifTag.DateTimeDigitized)?.Value ?? string.Empty;
        if (!string.IsNullOrEmpty(getDateTaken) &&
            DateTime.TryParseExact(getDateTaken, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var formattedDateTime))
        {
            return formattedDateTime.ToString(CultureInfo.CurrentCulture);
        }

        return string.Empty;
    }

    public static string?[]? GetGPSValues(IExifProfile profile)
    {
        if (profile is null)
        {
            return null;
        }
        var gpsLong = profile.GetValue(ExifTag.GPSLongitude);
        var gpsLongRef = profile.GetValue(ExifTag.GPSLongitudeRef);
        var gpsLatitude = profile.GetValue(ExifTag.GPSLatitude);
        var gpsLatitudeRef = profile.GetValue(ExifTag.GPSLatitudeRef);

        if (gpsLong is null || gpsLongRef is null || gpsLatitude is null ||
            gpsLatitudeRef is null)
        {
            return null;
        }

        var latitudeValue = GetCoordinates(gpsLatitudeRef.ToString(), gpsLatitude.Value).ToString(CultureInfo.InvariantCulture);
        var longitudeValue = GetCoordinates(gpsLongRef.ToString(), gpsLong.Value).ToString(CultureInfo.InvariantCulture);

        var googleLink = "https://www.google.com/maps/search/?api=1&query=" + latitudeValue + "," +
                         longitudeValue;
        var bingLink = "https://bing.com/maps/default.aspx?cp=" + latitudeValue + "~" + longitudeValue;

        var latitudeString = $"{gpsLatitude.Value[0]}\u00b0{gpsLatitude.Value[1]}'{gpsLatitude.Value[2].ToDouble():0.##}\"{gpsLatitudeRef}";
        var longitudeString = $"{gpsLong.Value[0]}\u00b0{gpsLong.Value[1]}'{gpsLong.Value[2].ToDouble():0.##}\"{gpsLongRef}";

        return [latitudeString, longitudeString, googleLink, bingLink];

        double GetCoordinates(string gpsRef, IReadOnlyList<Rational> rationals)
        {
            if (rationals[0].Denominator == 0 || rationals[1].Denominator == 0 || rationals[2].Denominator == 0)
            {
                return 0;
            }

            double degrees = rationals[0].Numerator / rationals[0].Denominator;
            double minutes = rationals[1].Numerator / rationals[1].Denominator;
            double seconds = rationals[2].Numerator / rationals[2].Denominator;

            var coordinate = degrees + minutes / 60d + seconds / 3600d;
            if (gpsRef is "S" or "W")
                coordinate *= -1;
            return coordinate;
        }
    }

    public static string GetColorSpace(IExifProfile profile)
    {
        var colorSpace = profile?.GetValue(ExifTag.ColorSpace)?.Value;
        if (colorSpace == null)
        {
            return string.Empty;
        }
        return colorSpace switch
        {
            1 => "sRGB",
            2 => "Adobe RGB",
            65535 => TranslationHelper.GetTranslation("Uncalibrated"),
            _ => string.Empty
        };
    }
}