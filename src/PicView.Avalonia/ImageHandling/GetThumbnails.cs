using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageReading;

namespace PicView.Avalonia.ImageHandling;

public static class GetThumbnails
{
    public static async ValueTask<Bitmap?> GetThumbAsync(FileInfo? fileInfo, uint height)
    {
        try
        {
            using var magick = new MagickImage();
            magick.Ping(fileInfo);
            var profile = magick.GetExifProfile();
            if (profile == null)
            {
                return await CreateThumbAsync(magick, fileInfo, height).ConfigureAwait(false);
            }

            var thumbnail = profile.CreateThumbnail();
            if (thumbnail == null || thumbnail.Height < height)
            {
                return await CreateThumbAsync(magick, fileInfo, height).ConfigureAwait(false);
            }

            thumbnail.AutoOrient();
            return thumbnail.ToWriteableBitmap();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetThumbnails), nameof(GetThumbAsync), e);
            return null;
        }
    }

    public static WriteableBitmap? GetExifThumb(string path)
    {
        using var magick = new MagickImage();
        try
        {
            magick.Ping(path);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetThumbnails), nameof(GetExifThumb), e);
            return null;
        }

        var profile = magick.GetExifProfile();
        // ReSharper disable once UseNullPropagation
        if (profile is null)
        {
            return null;
        }
        var thumbnail = profile.CreateThumbnail();
        if (thumbnail is null)
        {
            return null;
        }
        thumbnail.AutoOrient();
        return thumbnail?.ToWriteableBitmap();
    }

    private static async ValueTask<Bitmap?> CreateThumbAsync(MagickImage magick, FileInfo fileInfo, uint height)
    {
        // TODO: extract thumbnails from PlatformService and convert to Avalonia image,
        // I.E. https://boldena.com/article/64006
        // https://github.com/AvaloniaUI/Avalonia/discussions/16703
        // https://stackoverflow.com/a/42178963/2923736 convert to DLLImport to LibraryImport, source generation & AOT support

        switch (magick.Format)
        {
            case MagickFormat.WebP:
            case MagickFormat.WebM:
            case MagickFormat.Gif:
            case MagickFormat.Gif87:
            case MagickFormat.Png:
            case MagickFormat.Png00:
            case MagickFormat.Png8:
            case MagickFormat.Png24:
            case MagickFormat.Png32:
            case MagickFormat.Png48:
            case MagickFormat.Png64:
            case MagickFormat.APng:
            case MagickFormat.Jpe:
            case MagickFormat.Jpeg:
            case MagickFormat.Pjpeg:
            case MagickFormat.Bmp:
            case MagickFormat.Tif:
            case MagickFormat.Tiff:
            case MagickFormat.Ico:
            case MagickFormat.Icon:
            case MagickFormat.Wbmp:
            {
                return await GetSkBitmapThumbAsync(fileInfo, height);
            }
        
            case MagickFormat.Svg:
            case MagickFormat.Svgz:
                return null;
            default:
            {
                magick = await MagickPerformanceReader.ReadMagickImageWithSpanAsync(fileInfo, magick);
        
                var geometry = new MagickGeometry(0, height);
                magick.AutoOrient();
                magick.Thumbnail(geometry);
                return magick.ToWriteableBitmap();
            }
        }
    }

    private static async ValueTask<Bitmap?> GetSkBitmapThumbAsync(FileInfo fileInfo, uint height)
    {
        if (fileInfo is null || !fileInfo.Exists)
        {
            return null;
        }
        await using var stream = FileStreamUtils.GetOptimizedFileStream(fileInfo);
        var thumb = Bitmap.DecodeToHeight(stream, (int)height);
        return thumb;
    }
}