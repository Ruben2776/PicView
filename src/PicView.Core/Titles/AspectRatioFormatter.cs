using PicView.Core.Localization;

namespace PicView.Core.Titles;

public static class AspectRatioFormatter
{
        
    /// <summary>
    /// Generates a string representing the aspect ratio of an image based on its width and height.
    /// If the aspect ratio exceeds a certain limit, no aspect ratio is returned.
    /// </summary>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <returns>A string representing the aspect ratio in the format "x:y", or an empty string if the ratio is too large.</returns>
    public static string FormatAspectRatio(uint width, uint height)
    {
        if (width <= 0 || height <= 0)
        {
            return string.Empty;
        }

        var gcd = GCD(width, height);
        var aspectX = width / gcd;
        var aspectY = height / gcd;

        if (!IsAspectRatioWithinLimits(aspectX, aspectY))
        {
            return string.Empty;
        }

        Span<char> buffer = stackalloc char[32];
        var charsWritten = 0;

        buffer[charsWritten++] = ',';
        buffer[charsWritten++] = ' ';

        aspectX.TryFormat(buffer[charsWritten..], out var written);
        charsWritten += written;

        buffer[charsWritten++] = ':';

        aspectY.TryFormat(buffer[charsWritten..], out written);
        charsWritten += written;

        return new string(buffer[..charsWritten]);
    }
    
    private const uint MaxAspectRatioX = 48;
    private const uint MaxAspectRatioY = 18;
    private static bool IsAspectRatioWithinLimits(uint x, uint y) => x <= MaxAspectRatioX && y <= MaxAspectRatioY;

    /// <summary>
    /// Calculates the Greatest Common Divisor (GCD) of two integers.
    /// </summary>
    /// <param name="x">The first integer.</param>
    /// <param name="y">The second integer.</param>
    /// <returns>The GCD of the two integers.</returns>
    // ReSharper disable once InconsistentNaming
    public static uint GCD(uint x, uint y)
    {
        while (true)
        {
            if (y == 0)
            {
                return x;
            }

            var x1 = x;
            x = y;
            y = x1 % y;
        }
    }
    
    

    /// <summary>
    /// Generates a formatted aspect ratio string based on the given width, height, and their greatest common divisor (GCD).
    /// The result includes the aspect ratio and a description of the orientation (landscape, portrait, or square).
    /// </summary>
    /// <param name="gcd">The greatest common divisor of the width and height.</param>
    /// <param name="width">The width dimension of the image or element.</param>
    /// <param name="height">The height dimension of the image or element.</param>
    /// <returns>A formatted string representing the aspect ratio and orientation.</returns>
    public static string GetFormattedAspectRatio(uint gcd, uint width, uint height)
    {
        var firstRatio = width / gcd;
        var secondRatio = height / gcd;

        var orientation = firstRatio == secondRatio 
            ? TranslationManager.Translation.Square 
            : firstRatio > secondRatio 
                ? TranslationManager.Translation.Landscape 
                : TranslationManager.Translation.Portrait;

        Span<char> buffer = stackalloc char[64];
        var charsWritten = 0;

        // Format the first ratio
        firstRatio.TryFormat(buffer, out var written);
        charsWritten += written;

        buffer[charsWritten++] = ':';

        secondRatio.TryFormat(buffer[charsWritten..], out written);
        charsWritten += written;

        buffer[charsWritten++] = ' ';
        buffer[charsWritten++] = '(';

        var orientationSpan = orientation.AsSpan();
        orientationSpan.CopyTo(buffer[charsWritten..]);
        charsWritten += orientationSpan.Length;

        buffer[charsWritten++] = ')';

        return new string(buffer[..charsWritten]);
    }
}