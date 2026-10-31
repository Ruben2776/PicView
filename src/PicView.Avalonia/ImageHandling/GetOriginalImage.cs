using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.Navigation;

namespace PicView.Avalonia.ImageHandling;

/// <summary>
/// Gets the full-size image of a file, e.g., for an action on a gallery item that must not use its thumbnail.
/// </summary>
public static class GetOriginalImage
{
    /// <summary>
    /// Gets the full-size bitmap of <paramref name="fileInfo"/>: the one in <paramref name="cache"/> when it is
    /// already loaded there, else decoded from disk, upright and in sRGB.
    /// </summary>
    /// <returns>The bitmap, or <see langword="null"/> if the file has no bitmap, like an SVG, or cannot be read.</returns>
    /// <remarks>A decoded bitmap belongs to the caller; a cached one stays owned by the cache.</remarks>
    public static async ValueTask<Bitmap?> GetBitmapAsync(FileInfo fileInfo, SharedImageCache cache)
    {
        if (cache.TryGetReady(fileInfo, out var model) && model.Image is Bitmap cached)
        {
            return cached;
        }

        return await Task.Run(() => DecodeAsync(fileInfo).AsTask()).ConfigureAwait(false);
    }

    private static async ValueTask<Bitmap?> DecodeAsync(FileInfo fileInfo)
    {
        try
        {
            if (fileInfo.Extension.Equals(".b64", StringComparison.OrdinalIgnoreCase))
            {
                return await GetImage.GetBase64ImageAsync(fileInfo).ConfigureAwait(false);
            }

            using var magickImage = GetImage.CreateAndPingMagickImage(fileInfo);
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            return magickImage.Format switch
            {
                MagickFormat.Svg or MagickFormat.Svgz => null,
                MagickFormat.Arw or MagickFormat.Nef or MagickFormat.Dng or MagickFormat.Cr2 or MagickFormat.Rw2 =>
                    await GetImage.GetRawBitmapAsync(fileInfo, magickImage).ConfigureAwait(false),
                _ => await GetImage.GetNonStandardBitmapAsync(fileInfo, magickImage).ConfigureAwait(false)
            };
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetOriginalImage), nameof(DecodeAsync), e);
            return null;
        }
    }
}
