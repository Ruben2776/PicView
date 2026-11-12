using Avalonia;
using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.ImageHandling;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.Svg;

/// <summary>
/// Shows an SVG the parser does not support as a bitmap rendered by ImageMagick.
/// </summary>
internal static class SvgFallback
{
    public const int MinLongSide = 2048;
    public const int MaxLongSide = 8192;

    private const double BaseDensity = 96;

    /// <summary>
    /// The factor that brings the long side up to <see cref="MinLongSide"/> without passing <see cref="MaxLongSide"/>.
    /// </summary>
    public static double Scale(uint width, uint height)
    {
        var longSide = Math.Max(Math.Max(width, height), 1u);
        var scale = Math.Max(1.0, (double)MinLongSide / longSide);
        return Math.Min(scale, (double)MaxLongSide / longSide);
    }

    /// <summary>
    /// Renders the file at <see cref="Scale"/> times its own size. The bitmap's DPI is scaled to match, so
    /// its <see cref="Bitmap.Size"/> stays the SVG's own size. Returns null when ImageMagick fails.
    /// </summary>
    public static Bitmap? Render(FileInfo fileInfo, uint width, uint height)
    {
        try
        {
            var scale = Scale(width, height);
            var settings = new MagickReadSettings
            {
                BackgroundColor = MagickColors.Transparent,
                Density = new Density(BaseDensity * scale)
            };

            using var magickImage = new MagickImage();
            magickImage.Read(fileInfo, settings);
            return MagickBitmapConverter.ToBgraWriteableBitmap(magickImage, new Vector(96 * scale, 96 * scale));
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(SvgFallback), nameof(Render), e);
            return null;
        }
    }
}

