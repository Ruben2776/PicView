using System.Globalization;

namespace PicView.Core.Comparison;

/// <summary>
/// What Pixel Info reads under the pointer: a pixel's place in the image's own pixels, counted from 0 at the top
/// left, and its color as 0xAARRGGBB with straight alpha, or null for an image without pixels to read.
/// </summary>
public readonly record struct PixelReading(int X, int Y, uint? Argb)
{
    /// <summary>
    /// The pixel at a point of the image as shown: <paramref name="x"/> and <paramref name="y"/> are measured from
    /// the shown image's top left, in the image's own unrotated and unflipped space. False when the point is off the
    /// image.
    /// </summary>
    public static bool TryLocate(double x, double y, double shownWidth, double shownHeight, int pixelWidth,
        int pixelHeight, out int pixelX, out int pixelY)
    {
        pixelX = pixelY = 0;
        if (shownWidth <= 0 || shownHeight <= 0 || pixelWidth <= 0 || pixelHeight <= 0 ||
            x < 0 || y < 0 || x >= shownWidth || y >= shownHeight)
        {
            return false;
        }

        pixelX = Math.Min(pixelWidth - 1, (int)(x / shownWidth * pixelWidth));
        pixelY = Math.Min(pixelHeight - 1, (int)(y / shownHeight * pixelHeight));
        return true;
    }

    /// <summary>
    /// The reading as the compare bar writes it, <c>X: 12 Y: 34  #RRGGBB</c>: the alpha follows as a fourth pair only
    /// for a pixel that is not opaque, and there is no color without one.
    /// </summary>
    public string Text
    {
        get
        {
            var place = string.Create(CultureInfo.InvariantCulture, $"X: {X} Y: {Y}");
            if (Argb is not { } argb)
            {
                return place;
            }

            var alpha = argb >> 24;
            var rgb = argb & 0xFFFFFF;
            return alpha is 0xFF
                ? string.Create(CultureInfo.InvariantCulture, $"{place}  #{rgb:X6}")
                : string.Create(CultureInfo.InvariantCulture, $"{place}  #{rgb:X6}{alpha:X2}");
        }
    }
}
