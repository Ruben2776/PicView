using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageReading;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.ImageHandling;

public static class GetThumbnails
{
    public static async ValueTask<Bitmap?> GetThumbAsync(FileInfo fileInfo, uint height)
    {
        try
        {
            if (fileInfo is null)
            {
                return null;
            }

            if (fileInfo.IsCommon() && (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()))
            {
                var shellThumb = GetShellThumb(fileInfo.FullName, 0, (int)height);
                if (shellThumb is not null)
                {
                    return shellThumb;
                }
            }
            
            using var magick = new MagickImage();
            await magick.PingAsync(fileInfo);
            var profile = magick.GetExifProfile();
            if (profile is null)
            {
                return await CreateThumbAsync(magick, fileInfo, height).ConfigureAwait(false);
            }

            var thumbnail = profile.CreateThumbnail();
            if (thumbnail is null || thumbnail.Height < height)
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

    public static Bitmap? GetThumbQuick(FileInfo fileInfo)
    {
        if (fileInfo is null)
        {
            return null;
        }
        var height = Settings.Gallery.DockedGalleryItemSize > Settings.Gallery.ExpandedGalleryItemSize ?
            Settings.Gallery.DockedGalleryItemSize : Settings.Gallery.ExpandedGalleryItemSize;
        if (fileInfo.IsCommon() && (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()))
        {
            var shellThumb = GetShellThumb(fileInfo.FullName, 0, (int)height);
            if (shellThumb is not null)
            {
                return shellThumb;
            }
        }
        return GetExifThumb(fileInfo.FullName);
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

    /// <summary>
    /// Attempts to get a shell/OS-level thumbnail for the given file path.
    /// On Windows, this uses the IShellItemImageFactory COM interface.
    /// On macOS, this uses ImageIO (CGImageSource).
    /// Returns null on unsupported platforms or on failure.
    /// </summary>
    public static WriteableBitmap? GetShellThumb(string path, int width, int height)
    {
        try
        {
            var core = Dispatcher.UIThread.Invoke(() => Application.Current?.DataContext as CoreViewModel);

            var platformService = core?.PlatformService;
            if (platformService is null)
            {
                return null;
            }

            var pixels = platformService.GetShellThumbnail(path, width, height,
                out var pixelWidth, out var pixelHeight);

            if (pixels is null || pixelWidth <= 0 || pixelHeight <= 0)
            {
                return null;
            }

            var pixelSize = new PixelSize(pixelWidth, pixelHeight);
            var bitmap = new WriteableBitmap(pixelSize, new Vector(96, 96),
                PixelFormat.Bgra8888, AlphaFormat.Premul);

            using var framebuffer = bitmap.Lock();
            Marshal.Copy(pixels, 0, framebuffer.Address,
                Math.Min(pixels.Length, framebuffer.RowBytes * pixelHeight));

            return bitmap;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetThumbnails), nameof(GetShellThumb), e);
            return null;
        }
    }

    private static async ValueTask<Bitmap?> CreateThumbAsync(MagickImage magick, FileInfo fileInfo, uint height)
    {
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
