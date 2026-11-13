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
            using var magickImage = new MagickImage();
            magickImage.Read(fileInfo, ReadSettings(BaseDensity * scale));
            return MagickBitmapConverter.ToBgraWriteableBitmap(magickImage, new Vector(96 * scale, 96 * scale));
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(SvgFallback), nameof(Render), e);
            return null;
        }
    }

    /// <summary>
    /// Renders the file as a thumbnail <paramref name="height"/> pixels tall, at 96 DPI like other thumbnails, with
    /// its long side capped at <see cref="MinLongSide"/>: an SVG of a few bytes can ask for any size, so a very wide
    /// one is fitted to the cap instead. The density is chosen so the vector is drawn at the thumbnail's size rather
    /// than scaled from a raster of another size. Returns null when ImageMagick fails.
    /// </summary>
    /// <param name="fileInfo">The SVG or SVGZ file.</param>
    /// <param name="svgWidth">The SVG's own width, from the ping; 0 when unknown.</param>
    /// <param name="svgHeight">The SVG's own height, from the ping; 0 when unknown.</param>
    /// <param name="height">The thumbnail height in pixels.</param>
    public static Bitmap? RenderThumb(FileInfo fileInfo, uint svgWidth, uint svgHeight, uint height)
    {
        try
        {
            // Multiply before dividing: ImageMagick rounds the canvas up, so 230.39999 instead of 230.4 adds a pixel
            var density = svgHeight > 0 ? BaseDensity * height / svgHeight : BaseDensity;
            var isCapped = svgWidth * density / BaseDensity > MinLongSide;
            if (isCapped)
            {
                density = BaseDensity * MinLongSide / svgWidth;
            }

            using var magickImage = new MagickImage();
            magickImage.Read(fileInfo, ReadSettings(density));
            // The density rounds, so snap to the exact height, or to the cap for a very wide SVG
            magickImage.Thumbnail(isCapped ? new MagickGeometry(MinLongSide, 0) : new MagickGeometry(0, height));
            return MagickBitmapConverter.ToBgraWriteableBitmap(magickImage);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(SvgFallback), nameof(RenderThumb), e);
            return null;
        }
    }

    private static MagickReadSettings ReadSettings(double density) => new()
    {
        BackgroundColor = MagickColors.Transparent,
        Density = new Density(density)
    };
}

