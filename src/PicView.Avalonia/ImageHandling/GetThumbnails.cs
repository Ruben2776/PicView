using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageReading;
using PicView.Core.ViewModels;
using SkiaSharp;

namespace PicView.Avalonia.ImageHandling;

public static class GetThumbnails
{
    public static async ValueTask<Bitmap?> GetThumbAsync(FileInfo fileInfo, uint height, CoreViewModel? core = null, MagickImage? magick = null)
    {
        try
        {
            if (fileInfo is null)
            {
                return null;
            }

            if (core is not null && fileInfo.IsCommon() && (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()))
            {
                var shellThumb = GetShellThumb(fileInfo.FullName, 0, (int)height, core);
                if (shellThumb is not null)
                {
                    return shellThumb;
                }
            }

            var shouldDisposeMagick = false;
            try
            {
                if (magick is null)
                {
                    shouldDisposeMagick = true;
                    magick = new MagickImage();
#pragma warning disable MA0042
                    // ReSharper disable once MethodHasAsyncOverload
                    magick.Ping(fileInfo);
#pragma warning restore MA0042
                }

                var profile = magick.GetExifProfile();
                if (profile is null)
                {
                    return await CreateThumbAsync(magick, fileInfo, height).ConfigureAwait(false);
                }

                using var thumbnail = profile.CreateThumbnail();
                if (thumbnail is null || thumbnail.Height < height)
                {
                    return await CreateThumbAsync(magick, fileInfo, height).ConfigureAwait(false);
                }

                thumbnail.AutoOrient();
                return MagickBitmapConverter.ToBgraWriteableBitmap(thumbnail);
            }
            finally
            {
                if (shouldDisposeMagick)
                {
                    magick.Dispose();
                }
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetThumbnails), nameof(GetThumbAsync), e);
            if (!fileInfo.IsCommon())
            {
                return null;
            }
            try
            {
                return GetSkBitmapThumb(fileInfo, height);
            }
            catch (Exception exception)
            {
                DebugHelper.LogDebug(nameof(GetThumbnails), nameof(GetThumbAsync), exception);
                return null;
            }
        }
    }

    /// <summary>
    /// Tries to get shell thumbnail or exif thumbnail if available, otherwise returns null.
    /// </summary>
    public static Bitmap? GetThumbQuick(FileInfo fileInfo, CoreViewModel? core = null)
    {
        var height = Settings.Gallery.DockedGalleryItemSize > Settings.Gallery.ExpandedGalleryItemSize ?
            Settings.Gallery.DockedGalleryItemSize : Settings.Gallery.ExpandedGalleryItemSize;
        if (core is not null && fileInfo.IsCommon() && (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()))
        {
            return GetShellThumb(fileInfo.FullName, 0, (int)height, core) ?? GetExifThumb(fileInfo.FullName);
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
        using var thumbnail = profile.CreateThumbnail();
        if (thumbnail is null)
        {
            return null;
        }
        thumbnail.AutoOrient();
        return MagickBitmapConverter.ToBgraWriteableBitmap(thumbnail);
    }

    /// <summary>
    /// Attempts to get a shell/OS-level thumbnail for the given file path.
    /// On Windows, this uses the IShellItemImageFactory COM interface.
    /// On macOS, this uses ImageIO (CGImageSource).
    /// Returns null on unsupported platforms or on failure.
    /// </summary>
    public static WriteableBitmap? GetShellThumb(string path, int width, int height, CoreViewModel core)
    {
        try
        {
            var platformService = core.PlatformService;

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
                return GetSkBitmapThumb(fileInfo, height);
            }
        
            case MagickFormat.Svg:
            case MagickFormat.Svgz:
                return null;
            default:
            {
                magick = await MagickPerformanceReader.ReadMagickImageWithSpanAsync(fileInfo, magick).ConfigureAwait(false);
        
                var geometry = new MagickGeometry(0, height);
                magick.AutoOrient();
                magick.Thumbnail(geometry);
                return MagickBitmapConverter.ToBgraWriteableBitmap(magick);
            }
        }
    }

    /// <summary>
    /// Decodes a common format to <paramref name="height"/> with Skia, reading the file natively.
    /// The codec decodes at a reduced size where the format supports it (JPEG DCT scaling, WebP),
    /// and the result lands straight in the <see cref="WriteableBitmap"/> framebuffer without managed buffers.
    /// </summary>
    private static Bitmap? GetSkBitmapThumb(FileInfo fileInfo, uint height)
    {
        if (fileInfo is null || !fileInfo.Exists)
        {
            return null;
        }

        using var codec = SKCodec.Create(fileInfo.FullName);
        if (codec is null || codec.Info.Width <= 0 || codec.Info.Height <= 0)
        {
            return GetSkBitmapThumbFromStream(fileInfo, height);
        }

        // Same target size as Bitmap.DecodeToHeight
        var aspectRatio = codec.Info.Width / (double)codec.Info.Height;
        var target = new SKImageInfo(Math.Max(1, (int)(aspectRatio * height)), (int)height,
            SKColorType.Bgra8888, SKAlphaType.Premul);
        var scaled = codec.GetScaledDimensions((float)height / codec.Info.Height);

        var bitmap = new WriteableBitmap(new PixelSize(target.Width, target.Height), new Vector(96, 96),
            PixelFormat.Bgra8888, AlphaFormat.Premul);
        bool decoded;
        using (var framebuffer = bitmap.Lock())
        {
            decoded = scaled.Width == target.Width && scaled.Height == target.Height && framebuffer.RowBytes == target.RowBytes
                ? IsDecoded(codec.GetPixels(target, framebuffer.Address))
                : DecodeAndResize(codec, scaled, target, framebuffer);
        }

        if (decoded)
        {
            return bitmap;
        }

        bitmap.Dispose();
        return GetSkBitmapThumbFromStream(fileInfo, height);
    }

    private static bool DecodeAndResize(SKCodec codec, SKSizeI scaled, SKImageInfo target, ILockedFramebuffer framebuffer)
    {
        using var source = new SKBitmap(new SKImageInfo(scaled.Width, scaled.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        if (!IsDecoded(codec.GetPixels(source.Info, source.GetPixels())))
        {
            return false;
        }

        // Matches BitmapInterpolationMode.HighQuality, the DecodeToHeight default
        var sampling = target.Height > scaled.Height
            ? new SKSamplingOptions(SKCubicResampler.Mitchell)
            : new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
        using var destination = new SKPixmap(target, framebuffer.Address, framebuffer.RowBytes);
        return source.ScalePixels(destination, sampling);
    }

    // Truncated files still show what could be decoded, matching SKBitmap.Decode
    private static bool IsDecoded(SKCodecResult result) =>
        result is SKCodecResult.Success or SKCodecResult.IncompleteInput;

    private static Bitmap GetSkBitmapThumbFromStream(FileInfo fileInfo, uint height)
    {
        using var stream = FileStreamUtils.GetOptimizedFileStream(fileInfo);
        return Bitmap.DecodeToHeight(stream, (int)height);
    }
}
