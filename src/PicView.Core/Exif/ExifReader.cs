using System.Globalization;
using System.Text;
using ImageMagick;
using PicView.Core.Localization;

namespace PicView.Core.Exif;

public static class ExifReader
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

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
        if (DateTime.TryParse(getDateTaken, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        return null;
    }


    // Each enumerated tag reads through a Label function, which Image Information's dropdown of that tag shares, so
    // the word shown for a file is always one of its choices

    public static string? GetExposureProgram(IExifProfile? profile) =>
        Label(profile?.GetValue(ExifTag.ExposureProgram)?.Value, ExposureProgramLabel);

    internal static string ExposureProgramLabel(ushort code) => (code switch
    {
        0 => TranslationManager.Translation.NotDefined,
        1 => TranslationManager.Translation.Manual,
        2 => TranslationManager.Translation.Normal,
        3 => TranslationManager.Translation.AperturePriority,
        4 => TranslationManager.Translation.ShutterPriority,
        5 => TranslationManager.Translation.CreativeProgram,
        6 => TranslationManager.Translation.ActionProgram,
        7 => TranslationManager.Translation.Portrait,
        8 => TranslationManager.Translation.Landscape,
        _ => null
    }) ?? string.Empty;

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

    public static string GetSaturation(IExifProfile? profile) =>
        Label(profile?.GetValue(ExifTag.Saturation)?.Value, SaturationLabel);

    internal static string SaturationLabel(ushort code) => (code switch
    {
        0 => TranslationManager.Translation.Normal,
        1 => TranslationManager.Translation.Low,
        2 => TranslationManager.Translation.High,
        _ => null
    }) ?? string.Empty;

    public static string GetContrast(IExifProfile? profile) =>
        Label(profile?.GetValue(ExifTag.Contrast)?.Value, SoftHardLabel);

    public static string GetSharpness(IExifProfile? profile) =>
        Label(profile?.GetValue(ExifTag.Sharpness)?.Value, SoftHardLabel);

    /// <summary>The words of Contrast and Sharpness, which share their codes.</summary>
    internal static string SoftHardLabel(ushort code) => (code switch
    {
        0 => TranslationManager.Translation.Normal,
        1 => TranslationManager.Translation.Soft,
        2 => TranslationManager.Translation.Hard,
        _ => null
    }) ?? string.Empty;

    public static string GetWhiteBalance(IExifProfile? profile) =>
        Label(profile?.GetValue(ExifTag.WhiteBalance)?.Value, WhiteBalanceLabel);

    internal static string WhiteBalanceLabel(ushort code) => (code switch
    {
        0 => TranslationManager.Translation.Auto,
        1 => TranslationManager.Translation.Manual,
        _ => null
    }) ?? string.Empty;

    /// <summary>
    /// Retrieves the flash mode information from the specified Exif profile.
    /// </summary>
    /// <param name="profile">The Exif profile from which to extract the flash mode. Can be null.</param>
    /// <returns>A string indicating the flash mode. Returns an empty string if the flash mode is not found.</returns>
    public static string GetFlashMode(IExifProfile? profile) =>
        Label(profile?.GetValue(ExifTag.Flash)?.Value, FlashModeLabel);

    // https://exiftool.org/TagNames/EXIF.html
    internal static string FlashModeLabel(ushort code) => code switch
    {
        0 or 9 or 24 or 32 => TranslationManager.Translation.FlashDidNotFire ?? string.Empty,
        1 or 13 or 25 => TranslationManager.Translation.FlashFired ?? string.Empty,
        5 or 15 or 29 => TranslationManager.Translation.FlashFired + ", " + TranslationManager.Translation.StrobeReturnLightDetected,
        7 or 16 or 31 => TranslationManager.Translation.FlashFired + ", " + TranslationManager.Translation.StrobeReturnLightNotDetected,
        65 or 73 or 93 => TranslationManager.Translation.FlashFired + ", " + TranslationManager.Translation.RedEyeReduction,
        69 or 77 or 95 => TranslationManager.Translation.FlashFired + ", " + TranslationManager.Translation.RedEyeReduction + ", " + TranslationManager.Translation.StrobeReturnLightDetected,
        71 or 97 => TranslationManager.Translation.FlashFired + ", " + TranslationManager.Translation.RedEyeReduction + ", " + TranslationManager.Translation.StrobeReturnLightNotDetected,
        79 => TranslationManager.Translation.Unknown + ", " + TranslationManager.Translation.RedEyeReduction + ", " + TranslationManager.Translation.StrobeReturnLightNotDetected,
        89 => TranslationManager.Translation.FlashDidNotFire + ", " + TranslationManager.Translation.RedEyeReduction,
        _ => string.Empty
    };

    public static string GetLightSource(IExifProfile? profile) =>
        Label(profile?.GetValue(ExifTag.LightSource)?.Value, LightSourceLabel);

    internal static string LightSourceLabel(ushort code) => (code switch
    {
        0 => TranslationManager.Translation.Unknown,
        1 => TranslationManager.Translation.Daylight,
        2 => TranslationManager.Translation.Fluorescent,
        3 => "Tungsten",
        4 => TranslationManager.Translation.Flash,
        9 => TranslationManager.Translation.FineWeather,
        10 => TranslationManager.Translation.CloudyWeather,
        11 => TranslationManager.Translation.Shade,
        12 => TranslationManager.Translation.DaylightFluorescent,
        13 => TranslationManager.Translation.DayWhiteFluorescent,
        14 => TranslationManager.Translation.CoolWhiteFluorescent,
        15 => TranslationManager.Translation.WhiteFluorescent,
        17 => "Illuminants A",
        18 => "Illuminants B",
        19 => "Illuminants C",
        20 => "D55",
        21 => "D65",
        22 => "D75",
        23 => "D50",
        24 => "ISO Studio Tungsten",
        255 => TranslationManager.Translation.NotDefined,
        _ => null
    }) ?? string.Empty;

    public static string GetPhotometricInterpretation(IExifProfile? profile) =>
        Label(profile?.GetValue(ExifTag.PhotometricInterpretation)?.Value, PhotometricInterpretationLabel);

    internal static string PhotometricInterpretationLabel(ushort code) => code switch
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
        32892 or 34892 => "LinearRaw",
        65535 => "Undefined",
        _ => string.Empty
    };

    internal static string ResolutionUnitLabel(ushort code) => (code switch
    {
        1 => TranslationManager.Translation.None,
        2 => TranslationManager.Translation.Inches,
        3 => TranslationManager.Translation.Centimeters,
        _ => null
    }) ?? string.Empty;

    internal static string ColorSpaceLabel(ushort code) => (code switch
    {
        1 => "sRGB",
        2 => "Adobe RGB",
        65535 => TranslationManager.Translation.Uncalibrated ?? "Uncalibrated",
        _ => null
    }) ?? string.Empty;

    internal static string MeteringModeLabel(ushort code) => (code switch
    {
        0 => TranslationManager.Translation.Unknown,
        1 => "Average",
        2 => "Center-weighted average",
        3 => "Spot",
        4 => "Multi-spot",
        5 => "Pattern",
        6 => "Partial",
        255 => "Other",
        _ => null
    }) ?? string.Empty;

    internal static string OrientationLabel(ushort code)
    {
        var t = TranslationManager.Translation;
        return code switch
        {
            1 => t.Normal ?? string.Empty,
            2 => t.Flipped ?? string.Empty,
            3 => $"{t.Rotated} 180°",
            4 => $"{t.Rotated} 180°, {t.Flipped}",
            5 => $"{t.Rotated} 270°, {t.Flipped}",
            6 => $"{t.Rotated} 90°",
            7 => $"{t.Rotated} 90°, {t.Flipped}",
            8 => $"{t.Rotated} 270°",
            _ => string.Empty
        };
    }

    /// <summary>The codes of <see cref="CompressionLabel"/>, in the order Image Information lists them.</summary>
    internal static ReadOnlySpan<ushort> CompressionCodes =>
    [
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 99, 262, 32766, 32767, 32769, 32770, 32771, 32772, 32773, 32809, 32867, 32895,
        32896, 32897, 32898, 32908, 32909, 32946, 32947, 33003, 33005, 34661, 34676, 34677, 34712, 34713, 34715, 34718,
        34719, 34720, 34887, 34892, 34925, 34926, 34927, 34933, 34934, 50000, 50001, 50002, 52546, 65000, 65535
    ];

    internal static string CompressionLabel(ushort code) => (code switch
    {
        1 => TranslationManager.Translation.None,
        2 => "CCITT 1D",
        3 => "T4/Group 3 Fax",
        4 => "T6/Group 4 Fax",
        5 => "LZW",
        6 => "JPEG (old-style)",
        7 or 99 => "JPEG",
        8 => "Adobe Deflate",
        9 => "JBIG B&W",
        10 => "JBIG Color",
        262 => "Kodak 262",
        32766 => "Next",
        32767 => "Sony ARW Compressed",
        32769 => "Packed RAW",
        32770 => "Samsung SRW Compressed",
        32771 => "CCIRLEW",
        32772 => "Samsung SRW Compressed 2",
        32773 => "PackBits",
        32809 => "Thunderscan",
        32867 => "Kodak KDC Compressed",
        32895 => "IT8CTPAD",
        32896 => "IT8LW",
        32897 => "IT8MP",
        32898 => "IT8BL",
        32908 => "PixarFilm",
        32909 => "PixarLog",
        32946 => "Deflate",
        32947 => "DCS",
        33003 => "Aperio JPEG 2000 YCbCr",
        33005 => "Aperio JPEG 2000 RGB",
        34661 => "JBIG",
        34676 => "SGILog",
        34677 => "SGILog24",
        34712 => "JPEG 2000",
        34713 => "Nikon NEF Compressed",
        34715 => "JBIG2 TIFF FX",
        34718 => "Microsoft Document Imaging (MDI) Binary Level Codec",
        34719 => "Microsoft Document Imaging (MDI) Progressive Transform Codec",
        34720 => "Microsoft Document Imaging (MDI) Vector",
        34887 => "ESRI Lerc",
        34892 => "Lossy JPEG",
        34925 => "LZMA2",
        34926 => "Zstd (old)",
        34927 => "WebP (old)",
        34933 => "PNG",
        34934 => "JPEG XR",
        50000 => "Zstd",
        50001 => "WebP",
        50002 => "JPEG XL (old)",
        52546 => "JPEG XL",
        65000 => "Kodak DCR Compressed",
        65535 => "Pentax PEF Compressed",
        _ => null
    }) ?? string.Empty;

    private static string Label(ushort? code, Func<ushort, string> label) =>
        code is { } value ? label(value) : string.Empty;

    public static string GetExifVersion(IExifProfile? profile)
    {
        var exifVersion = profile?.GetValue(ExifTag.ExifVersion)?.Value;
        return exifVersion is null ? string.Empty : Encoding.ASCII.GetString(exifVersion);
    }

    public static string GetTitle(IExifProfile? profile)
    {
        var title = string.Empty;
        var xPTitle = profile?.GetValue(ExifTag.XPTitle)?.Value;
        if (xPTitle is not null)
        {
            title = Encoding.Unicode.GetString(xPTitle).TrimEnd('\0');
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        var titleTag = profile?.GetValue(ExifTag.ImageDescription)?.Value;
        return titleTag ?? string.Empty;
    }

    public static string GetSubject(IExifProfile? profile) => GetXpString(profile?.GetValue(ExifTag.XPSubject)?.Value);

    /// <summary>
    /// Retrieves the Windows keywords (tags) from the specified Exif profile.
    /// Windows stores them as one semicolon-separated string, which is returned as is.
    /// </summary>
    public static string GetKeywords(IExifProfile? profile) => GetXpString(profile?.GetValue(ExifTag.XPKeywords)?.Value);

    // Windows XP* tags hold null-terminated UTF-16LE text.
    private static string GetXpString(byte[]? value) =>
        value is null ? string.Empty : Encoding.Unicode.GetString(value).TrimEnd('\0');

    /// <summary>
    /// Retrieves the user comment from the specified Exif profile, if available.
    /// </summary>
    /// <param name="profile">The Exif profile from which to extract the user comment. Can be null.</param>
    /// <returns>A string containing the user comment. Returns an empty string if no comment is found or in case of an error.</returns>
    public static string GetUserComment(IExifProfile? profile)
    {
        if (profile is null)
        {
            return string.Empty;
        }

        try
        {
            // ImageMagick normalizes the profile byte order when a tag is first read.
            // Preserve the original TIFF byte order before accessing UserComment.
            var isBigEndian = IsBigEndian(profile);
            var commentBytes = profile.GetValue(ExifTag.UserComment)?.Value;
            if (commentBytes is null || commentBytes.Length == 0)
            {
                return string.Empty;
            }

            var value = commentBytes.AsSpan();
            string result;
            if (value.StartsWith("UNICODE\0"u8))
            {
                var exifVersion = GetExifVersion(profile);
                var usesUtf8 = string.CompareOrdinal(exifVersion, "0300") >= 0;
                result = DecodeUnicodeComment(value[8..], usesUtf8, isBigEndian);
            }
            else if (value.StartsWith("ASCII\0\0\0"u8))
            {
                result = DecodeAsciiComment(value[8..], isBigEndian);
            }
            else if (value.StartsWith("JIS\0\0\0\0\0"u8))
            {
                // ISO-2022-JP requires a code-page decoder that PicView does not currently include.
                result = string.Empty;
            }
            else if (value.Length >= 8 && value[..8].IndexOfAnyExcept((byte)0) < 0)
            {
                result = DecodeUtf8OrFallback(value[8..], Encoding.ASCII);
            }
            else
            {
                // PicView previously wrote comments without the EXIF character-code prefix.
                result = Encoding.ASCII.GetString(value).TrimEnd('\0');
            }

            return string.IsNullOrWhiteSpace(result) ? string.Empty : result;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static string DecodeAsciiComment(ReadOnlySpan<byte> comment, bool isBigEndian)
    {
        try
        {
            var utf8 = StrictUtf8.GetString(comment).TrimEnd('\0');
            if (!ContainsUnexpectedControlCharacters(utf8))
            {
                return utf8;
            }
        }
        catch (DecoderFallbackException)
        {
            // Some applications label Unicode comment bytes as ASCII.
        }

        return DecodeUnicodeComment(comment, false, isBigEndian);
    }

    private static bool ContainsUnexpectedControlCharacters(ReadOnlySpan<char> text)
    {
        foreach (var character in text)
        {
            if (char.IsControl(character) && character is not ('\t' or '\r' or '\n'))
            {
                return true;
            }
        }

        return false;
    }

    private static string DecodeUnicodeComment(ReadOnlySpan<byte> comment, bool usesUtf8, bool isBigEndian)
    {
        if (comment.Length >= 2 && comment[0] == 0xfe && comment[1] == 0xff)
        {
            return Encoding.BigEndianUnicode.GetString(comment[2..]).TrimEnd('\0');
        }

        if (comment.Length >= 2 && comment[0] == 0xff && comment[1] == 0xfe)
        {
            return Encoding.Unicode.GetString(comment[2..]).TrimEnd('\0');
        }

        if (usesUtf8)
        {
            try
            {
                return StrictUtf8.GetString(comment).TrimEnd('\0');
            }
            catch (DecoderFallbackException)
            {
                var fallback = isBigEndian ? Encoding.BigEndianUnicode : Encoding.Unicode;
                return fallback.GetString(comment).TrimEnd('\0');
            }
        }

        var bigEndianPairs = 0;
        var littleEndianPairs = 0;
        for (var i = 0; i + 1 < comment.Length; i += 2)
        {
            if (comment[i] == 0 && comment[i + 1] != 0)
            {
                bigEndianPairs++;
            }
            else if (comment[i] != 0 && comment[i + 1] == 0)
            {
                littleEndianPairs++;
            }
        }

        if (bigEndianPairs > littleEndianPairs)
        {
            return Encoding.BigEndianUnicode.GetString(comment).TrimEnd('\0');
        }

        if (littleEndianPairs > bigEndianPairs)
        {
            return Encoding.Unicode.GetString(comment).TrimEnd('\0');
        }

        var encoding = isBigEndian ? Encoding.BigEndianUnicode : Encoding.Unicode;
        return encoding.GetString(comment).TrimEnd('\0');
    }

    private static string DecodeUtf8OrFallback(ReadOnlySpan<byte> comment, Encoding fallback)
    {
        try
        {
            return StrictUtf8.GetString(comment).TrimEnd('\0');
        }
        catch (DecoderFallbackException)
        {
            return fallback.GetString(comment).TrimEnd('\0');
        }
    }

    private static bool IsBigEndian(IExifProfile profile)
    {
        var data = profile.ToByteArray();
        return (data.Length >= 2 && data[0] == (byte)'M' && data[1] == (byte)'M') ||
               (data.Length >= 8 && data[6] == (byte)'M' && data[7] == (byte)'M');
    }
}
