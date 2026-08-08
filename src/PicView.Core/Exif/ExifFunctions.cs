using System.Globalization;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;

namespace PicView.Core.Exif;

public static class ExifFunctions
{
    /// <summary>
    /// Determines whether the specified format is an EXIF-supported image type.
    /// </summary>
    /// <param name="format">The MagickFormat of the image to check.</param>
    /// <returns>True if the format supports EXIF metadata; otherwise, false.</returns>
    public static bool IsExifImage(this MagickFormat? format)
    {
        if (format is null)
        {
            return false;
        }

        return format.Value switch
        {
            MagickFormat.Jpeg or
                MagickFormat.Tiff or
                MagickFormat.Dng or // Adobe Digital Negative
                MagickFormat.Cr2 or // Canon RAW
                MagickFormat.Cr3 or
                MagickFormat.Nef or // Nikon RAW
                MagickFormat.Arw or // Sony RAW
                MagickFormat.Orf or // Olympus RAW
                MagickFormat.Rw2 or // Panasonic RAW
                MagickFormat.Pef or // Pentax RAW
                MagickFormat.Raf or // Fujifilm RAW
                MagickFormat.Srw or // Samsung RAW
                MagickFormat.Heif or
                MagickFormat.Heic // High Efficiency Image Format
                => true,

            _ => false
        };
    }

    public static async Task<bool> TryUpdateImageProfileAsync(FileInfo fileInfo, Func<MagickImage, bool> updateAction,
        string callingClassName, string callingMethodName)
    {
        try
        {
            using var magickImage = new MagickImage(fileInfo);

            // If the update action returns false, it means no changes were made that require saving.
            if (!updateAction(magickImage))
            {
                return false;
            }
            
            await magickImage.WriteAsync(fileInfo).ConfigureAwait(false);
            return true;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(callingClassName, callingMethodName, e);
            return false;
        }
    }

    public static bool TryParseRational(string input, out Rational result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Handle fraction format "num/den"
        if (input.Contains('/'))
        {
            var parts = input.Split('/');
            if (parts.Length == 2 && double.TryParse(parts[0], CultureInfo.InvariantCulture, out var num) &&
                double.TryParse(parts[1], CultureInfo.InvariantCulture, out var den))
            {
                if (den == 0)
                {
                    return false;
                }

                result = new Rational((uint)num, (uint)den);
                return true;
            }
        }

        // Handle decimal format
        if (!double.TryParse(input, CultureInfo.InvariantCulture, out var val))
        {
            return false;
        }

        result = new Rational(val);
        return true;
    }

    /// <summary>
    /// Tries to parse a string into a SignedRational.
    /// </summary>
    /// <param name="input">The string to parse (e.g., "-10/20" or "-0.5").</param>
    /// <param name="result">The parsed SignedRational.</param>
    /// <returns>True if parsing was successful, otherwise false.</returns>
    public static bool TryParseSignedRational(string input, out SignedRational result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Handle fraction format "num/den"
        if (input.Contains('/'))
        {
            var parts = input.Split('/');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var numerator) &&
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var denominator))
            {
                if (denominator == 0)
                {
                    return false; // Avoid division by zero
                }

                result = new SignedRational(numerator, denominator, false);
                return true;
            }
        }

        // Handle decimal format
        if (!double.TryParse(input, CultureInfo.InvariantCulture, out var val))
        {
            return false;
        }

        result = new SignedRational(val);
        return true;
    }
}