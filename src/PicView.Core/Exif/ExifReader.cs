using System.Globalization;
using System.Text;
using ImageMagick;
using PicView.Core.Localization;

namespace PicView.Core.Exif;

public static class ExifReader
{
    public static DateTime? GetDateTaken(IExifProfile profile)
    {
        var getDateTaken =
            profile?.GetValue(ExifTag.DateTime)?.Value ??
            profile?.GetValue(ExifTag.DateTimeOriginal)?.Value ??
            profile?.GetValue(ExifTag.DateTimeDigitized)?.Value;

        if (string.IsNullOrWhiteSpace(getDateTaken))
        {
            return null;
        }

        // EXIF format: "yyyy:MM:dd HH:mm:ss"
        if (DateTime.TryParseExact(
                getDateTaken,
                "yyyy:MM:dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result))
        {
            return result;
        }

        // fallback: try normal parsing
        if (DateTime.TryParse(getDateTaken, out result))
        {
            return result;
        }

        return null;
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
            0 => TranslationManager.GetTranslation("NotDefined"),
            1 => TranslationManager.GetTranslation("Manual"),
            2 => TranslationManager.GetTranslation("Normal"),
            3 => TranslationManager.GetTranslation("AperturePriority"),
            4 => TranslationManager.GetTranslation("ShutterPriority"),
            5 => TranslationManager.GetTranslation("CreativeProgram"),
            6 => TranslationManager.GetTranslation("ActionProgram"),
            7 => TranslationManager.GetTranslation("Portrait"),
            8 => TranslationManager.GetTranslation("Landscape"),
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
            0 => TranslationManager.GetTranslation("Normal"),
            1 => TranslationManager.GetTranslation("Low"),
            2 => TranslationManager.GetTranslation("High"),
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
            0 => TranslationManager.GetTranslation("Normal"),
            1 => TranslationManager.GetTranslation("Soft"),
            2 => TranslationManager.GetTranslation("Hard"),
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
            0 => TranslationManager.GetTranslation("Normal"),
            1 => TranslationManager.GetTranslation("Soft"),
            2 => TranslationManager.GetTranslation("Hard"),
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
            0 => TranslationManager.GetTranslation("Auto"),
            1 => TranslationManager.GetTranslation("Manual"),
            _ => string.Empty
        };
    }

    public static ushort GetResolutionUnit(IExifProfile? profile)
    {
        var resolutionUnit = profile?.GetValue(ExifTag.ResolutionUnit)?.Value;
        if (resolutionUnit is null)
        {
            return 0;
        }

        return resolutionUnit switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            _ => 0
        };
    }


    /// <summary>
    /// Retrieves the flash mode information from the specified Exif profile.
    /// </summary>
    /// <param name="profile">The Exif profile from which to extract the flash mode. Can be null.</param>
    /// <returns>A string indicating the flash mode. Returns an empty string if the flash mode is not found or in case of an error.</returns>
    public static string GetFlashMode(IExifProfile? profile)
    {
        var flash = profile?.GetValue(ExifTag.Flash)?.Value;
        if (flash is null)
        {
            return string.Empty;
        }

        // https://exiftool.org/TagNames/EXIF.html
        // ReSharper disable once ConvertSwitchStatementToSwitchExpression
        switch (flash)
        {
            case 0:
            case 9:
            case 24:
            case 32:
                return TranslationManager.GetTranslation("FlashDidNotFire");

            case 1:
            case 13:
            case 25:
                return TranslationManager.GetTranslation("FlashFired");

            case 5:
                return TranslationManager.GetTranslation("FlashFired") + ", " +
                       TranslationManager.GetTranslation("StrobeReturnLightDetected");

            case 7:
                return TranslationManager.GetTranslation("FlashFired") + ", " +
                       TranslationManager.GetTranslation("StrobeReturnLightNotDetected");

            case 15:
                return TranslationManager.GetTranslation("FlashFired") + ", " +
                       TranslationManager.GetTranslation("StrobeReturnLightDetected");

            case 16:
                return TranslationManager.GetTranslation("FlashFired") + ", " +
                       TranslationManager.GetTranslation("StrobeReturnLightNotDetected");

            case 29:
                return TranslationManager.GetTranslation("FlashFired") + ", " +
                       TranslationManager.GetTranslation("StrobeReturnLightDetected");

            case 31:
                return TranslationManager.GetTranslation("FlashFired") + ", " +
                       TranslationManager.GetTranslation("StrobeReturnLightNotDetected");

            case 65:
                return TranslationManager.GetTranslation("FlashFired") + ", " +
                       TranslationManager.GetTranslation("RedEyeReduction");

            case 69:
                return TranslationManager.GetTranslation("FlashFired") + ", " +
                       TranslationManager.GetTranslation("RedEyeReduction") + ", " +
                       TranslationManager.GetTranslation("StrobeReturnLightDetected");

            case 71:
                return TranslationManager.GetTranslation("FlashFired") + ", " +
                       TranslationManager.GetTranslation("RedEyeReduction") + ", " +
                       TranslationManager.GetTranslation("StrobeReturnLightNotDetected");

            case 73:
                return TranslationManager.GetTranslation("FlashFired") + ", " +
                       TranslationManager.GetTranslation("RedEyeReduction");

            case 77:
                return TranslationManager.GetTranslation("FlashFired") + ", " +
                       TranslationManager.GetTranslation("RedEyeReduction") + ", " +
                       TranslationManager.GetTranslation("StrobeReturnLightDetected");

            case 79:
                return TranslationManager.GetTranslation("Unknown") + ", " +
                       TranslationManager.GetTranslation("RedEyeReduction") + ", " +
                       TranslationManager.GetTranslation("StrobeReturnLightNotDetected");

            case 89:
                return TranslationManager.GetTranslation("FlashDidNotFire") + ", " +
                       TranslationManager.GetTranslation("RedEyeReduction");

            case 93:
                return TranslationManager.GetTranslation("FlashFired") + ", " +
                       TranslationManager.GetTranslation("RedEyeReduction");

            case 95:
                return TranslationManager.GetTranslation("FlashFired") + ", " +
                       TranslationManager.GetTranslation("RedEyeReduction") + ", " +
                       TranslationManager.GetTranslation("StrobeReturnLightDetected");

            case 97:
                return TranslationManager.GetTranslation("FlashFired") + ", " +
                       TranslationManager.GetTranslation("RedEyeReduction") + ", " +
                       TranslationManager.GetTranslation("StrobeReturnLightNotDetected");

            default: return string.Empty;
        }
    }

    public static string GetLightSource(IExifProfile? profile)
    {
        var lightSource = profile?.GetValue(ExifTag.LightSource)?.Value;
        if (lightSource is null)
        {
            return string.Empty;
        }

        return lightSource switch
        {
            0 => TranslationManager.GetTranslation("Unknown"),
            1 => TranslationManager.GetTranslation("Daylight"),
            2 => TranslationManager.GetTranslation("Fluorescent"),
            3 => "Tungsten",
            4 => TranslationManager.GetTranslation("Flash"),
            9 => TranslationManager.GetTranslation("FineWeather"),
            10 => TranslationManager.GetTranslation("CloudyWeather"),
            11 => TranslationManager.GetTranslation("Shade"),
            12 => TranslationManager.GetTranslation("DaylightFluorescent"),
            13 => TranslationManager.GetTranslation("DayWhiteFluorescent"),
            14 => TranslationManager.GetTranslation("CoolWhiteFluorescent"),
            15 => TranslationManager.GetTranslation("WhiteFluorescent"),
            17 => "Illuminants A",
            18 => "Illuminants B",
            19 => "Illuminants C",
            20 => "D55",
            21 => "D65",
            22 => "D75",
            23 => "D50",
            24 => "ISO Studio Tungsten",
            255 => TranslationManager.GetTranslation("NotDefined"),
            _ => string.Empty
        };
    }

    public static string GetPhotometricInterpretation(IExifProfile? profile)
    {
        var photometricInterpretation = profile?.GetValue(ExifTag.PhotometricInterpretation)?.Value;
        if (photometricInterpretation is null)
        {
            return string.Empty;
        }

        return photometricInterpretation switch
        {
            0 => "WhiteIsZero",
            1 => "BlackIsZero",
            2 => "RGB",
            3 => "RGBPalette",
            4 => "TransparencyMask",
            5 => "CMYK",
            6 => "YCbCr",
            8 => "CIELab",
            9 => "ICCLab",
            10 => "ITULab",
            32803 => "ColorFilterArray",
            32844 => "PixarLogL",
            32845 => "PixarLogLuv",
            32892 => "LinearRaw",
            34892 => "LinearRaw",
            65535 => "Undefined",
            _ => string.Empty
        };
    }

    public static string GetExifVersion(IExifProfile? profile)
    {
        var exifVersion = profile?.GetValue(ExifTag.ExifVersion)?.Value;
        return exifVersion is null ? string.Empty : Encoding.ASCII.GetString(exifVersion);
    }

    public static string GetTitle(IExifProfile? profile)
    {
        var xPTitle = profile?.GetValue(ExifTag.XPTitle)?.Value;
        if (xPTitle is null)
        {
            return string.Empty;
        }

        var title = Encoding.Unicode.GetString(xPTitle).TrimEnd('\0');
        if (string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        var titleTag = profile?.GetValue(ExifTag.ImageDescription)?.Value;
        return titleTag ?? string.Empty;
    }

    public static string GetSubject(IExifProfile? profile)
    {
        var xPSubject = profile?.GetValue(ExifTag.XPSubject)?.Value;
        var subject = xPSubject is null ? string.Empty : Encoding.ASCII.GetString(xPSubject);
        if (!string.IsNullOrWhiteSpace(subject))
        {
            return subject;
        }

        var subjectTag = profile?.GetValue(ExifTag.XPSubject)?.Value;
        return subjectTag?.GetValue(0)?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Retrieves the user comment from the specified Exif profile, if available.
    /// </summary>
    /// <param name="profile">The Exif profile from which to extract the user comment. Can be null.</param>
    /// <returns>A string containing the user comment. Returns an empty string if no comment is found or in case of an error.</returns>
    public static string GetUserComment(IExifProfile? profile)
    {
        var commentBytes = profile?.GetValue(ExifTag.UserComment)?.Value;
        if (commentBytes is null || commentBytes.Length <= 8)
        {
            return string.Empty;
        }

        try
        {
            var decodedComment = Encoding.ASCII.GetString(commentBytes);
            if (string.IsNullOrWhiteSpace(decodedComment))
            {
                return string.Empty;
            }

            var result = decodedComment.StartsWith("UNICODE") ? decodedComment.Replace("UNICODE", "") : decodedComment;
            return result.StartsWith("ASCII") ? string.Empty : result;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}