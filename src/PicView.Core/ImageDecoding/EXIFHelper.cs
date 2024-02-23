using ImageMagick;
using PicView.Core.Localization;
using System.Globalization;

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

    /// <summary>
    /// Gets the GPS values from the provided EXIF profile.
    /// </summary>
    /// <param name="profile">The EXIF profile.</param>
    /// <returns>An array containing the latitude, longitude, Google Maps link, and Bing Maps link.</returns>
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

        var googleLink = $"https://www.google.com/maps/search/?api=1&query={latitudeValue},{longitudeValue}";
        var bingLink = $"https://bing.com/maps/default.aspx?cp={latitudeValue}~{longitudeValue}&lvl=16.0&sty=c";

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

    public static string GetExposureProgram(IExifProfile? profile)
    {
        var exposureProgram = profile?.GetValue(ExifTag.ExposureProgram)?.Value;
        if (exposureProgram is null)
        {
            return string.Empty;
        }
        return exposureProgram switch
        {
            0 => TranslationHelper.GetTranslation("NotDefined"),
            1 => TranslationHelper.GetTranslation("Manual"),
            2 => TranslationHelper.GetTranslation("Normal"),
            3 => TranslationHelper.GetTranslation("AperturePriority"),
            4 => TranslationHelper.GetTranslation("ShutterPriority"),
            5 => TranslationHelper.GetTranslation("CreativeProgram"),
            6 => TranslationHelper.GetTranslation("ActionProgram"),
            7 => TranslationHelper.GetTranslation("Portrait"),
            8 => TranslationHelper.GetTranslation("Landscape"),
            _ => string.Empty
        };
    }

    public static string GetISOSpeed(IExifProfile? profile)
    {
        if (profile is null)
        {
            return string.Empty;
        }

        var isoSpeedRating = profile.GetValue(ExifTag.ISOSpeedRatings)?.Value;
        if (isoSpeedRating is not null)
        {
            return isoSpeedRating.GetValue(0)?.ToString() ?? string.Empty;
        }

        var isoSpeed = profile.GetValue(ExifTag.ISOSpeed)?.Value;
        if (isoSpeed is null)
        {
            return string.Empty;
        }
        return isoSpeed.ToString() ?? string.Empty;
    }

    public static string GetSaturation(IExifProfile? profile)
    {
        var saturation = profile?.GetValue(ExifTag.Saturation)?.Value;
        if (saturation is null)
        {
            return string.Empty;
        }
        return saturation switch
        {
            0 => TranslationHelper.GetTranslation("Normal"),
            1 => TranslationHelper.GetTranslation("Low"),
            2 => TranslationHelper.GetTranslation("High"),
            _ => string.Empty
        };
    }

    public static string GetContrast(IExifProfile profile)
    {
        var contrast = profile?.GetValue(ExifTag.Contrast)?.Value;
        if (contrast is null)
        {
            return string.Empty;
        }
        return contrast switch
        {
            0 => TranslationHelper.GetTranslation("Normal"),
            1 => TranslationHelper.GetTranslation("Soft"),
            2 => TranslationHelper.GetTranslation("Hard"),
            _ => string.Empty
        };
    }

    public static string GetSharpness(IExifProfile profile)
    {
        var sharpness = profile?.GetValue(ExifTag.Sharpness)?.Value;
        if (sharpness is null)
        {
            return string.Empty;
        }
        return sharpness switch
        {
            0 => TranslationHelper.GetTranslation("Normal"),
            1 => TranslationHelper.GetTranslation("Soft"),
            2 => TranslationHelper.GetTranslation("Hard"),
            _ => string.Empty
        };
    }

    public static string GetWhiteBalance(IExifProfile profile)
    {
        var whiteBalance = profile?.GetValue(ExifTag.WhiteBalance)?.Value;
        if (whiteBalance is null)
        {
            return string.Empty;
        }
        return whiteBalance switch
        {
            0 => TranslationHelper.GetTranslation("Auto"),
            1 => TranslationHelper.GetTranslation("Manual"),
            _ => string.Empty
        };
    }

    public static string GetResolutionUnit(IExifProfile? profile)
    {
        var resolutionUnit = profile?.GetValue(ExifTag.ResolutionUnit)?.Value;
        if (resolutionUnit is null)
        {
            return string.Empty;
        }
        return resolutionUnit switch
        {
            1 => TranslationHelper.GetTranslation("None"),
            2 => TranslationHelper.GetTranslation("Inches"),
            3 => TranslationHelper.GetTranslation("Centimeters"),
            _ => string.Empty
        };
    }

    public static string GetFlash(IExifProfile profile)
    {
        var flash = profile?.GetValue(ExifTag.Flash)?.Value;
        if (flash is null)
        {
            return string.Empty;
        }

        return flash.ToString() ?? string.Empty;

        // Maybe another time?
        return flash switch
        {
            0 => TranslationHelper.GetTranslation("NoFlash"),
            1 => TranslationHelper.GetTranslation("Fired"),
            5 => TranslationHelper.GetTranslation("FiredStrobeReturnLightDetected"),
            7 => TranslationHelper.GetTranslation("FiredStrobeReturnLightNotDetected"),
            9 => TranslationHelper.GetTranslation("OnDidNotFire"),
            13 => TranslationHelper.GetTranslation("OnFired"),
            15 => TranslationHelper.GetTranslation("OnFiredStrobeReturnLightDetected"),
            16 => TranslationHelper.GetTranslation("OnFiredStrobeReturnLightNotDetected"),
            24 => TranslationHelper.GetTranslation("AutoDidNotFire"),
            25 => TranslationHelper.GetTranslation("AutoFired"),
            29 => TranslationHelper.GetTranslation("AutoFiredStrobeReturnLightDetected"),
            31 => TranslationHelper.GetTranslation("AutoFiredStrobeReturnLightNotDetected"),
            32 => TranslationHelper.GetTranslation("NoFlashFunction"),
            65 => TranslationHelper.GetTranslation("FiredRedEyeReduction"),
            69 => TranslationHelper.GetTranslation("FiredRedEyeReductionStrobeReturnLightDetected"),
            71 => TranslationHelper.GetTranslation("FiredRedEyeReductionStrobeReturnLightNotDetected"),
            73 => TranslationHelper.GetTranslation("OnRedEyeReduction"),
            77 => TranslationHelper.GetTranslation("OnRedEyeReductionStrobeReturnLightDetected"),
            79 => TranslationHelper.GetTranslation("OnRedEyeReductionStrobeReturnLightNotDetected"),
            89 => TranslationHelper.GetTranslation("AutoDidNotFireRedEyeReduction"),
            93 => TranslationHelper.GetTranslation("AutoFiredRedEyeReduction"),
            95 => TranslationHelper.GetTranslation("AutoFiredRedEyeReductionStrobeReturnLightDetected"),
            97 => TranslationHelper.GetTranslation("AutoFiredRedEyeReductionStrobeReturnLightNotDetected"),
            _ => string.Empty
        };
    }
}