using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageMagick;
#if MAGICK_LEGACY
using Orientation = ImageMagick.OrientationType;
#endif
using ImageMagick.Formats;
using PicView.Avalonia.Svg;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageReading;
using PicView.Core.IPlatform;
using SkiaSharp;

namespace PicView.Avalonia.ImageHandling;

public static class GetThumbnails
{
    /// <summary>
    /// Makes a thumbnail of <paramref name="fileInfo"/> no taller than <paramref name="height"/>: the shell's when
    /// <paramref name="platform"/> has one, else the EXIF thumbnail when it is tall enough, else a decode of the file.
    /// Blocks while it reads the file.
    /// </summary>
    public static Bitmap? Decode(FileInfo fileInfo, uint height, IPlatformSpecificService? platform = null)
    {
        try
        {
            if (fileInfo is null)
            {
                return null;
            }

            if (platform is not null && fileInfo.IsCommon() && (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()))
            {
                var shellThumb = GetShellThumb(fileInfo.FullName, 0, (int)height, platform);
                if (shellThumb is not null)
                {
                    return shellThumb;
                }
            }

            using var magick = new MagickImage();
#pragma warning disable MA0042
            // ReSharper disable once MethodHasAsyncOverload
            magick.Ping(fileInfo, CreatePingSettings());
#pragma warning restore MA0042

            var profile = magick.GetExifProfile();
            if (profile is null)
            {
                return CreateThumb(magick, fileInfo, height);
            }

            using var thumbnail = profile.CreateThumbnail();
            if (thumbnail is null || thumbnail.Height < height)
            {
                return CreateThumb(magick, fileInfo, height);
            }

            thumbnail.AutoOrient();
            return MagickBitmapConverter.ToBgraWriteableBitmap(thumbnail);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetThumbnails), nameof(Decode), e);
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
                DebugHelper.LogDebug(nameof(GetThumbnails), nameof(Decode), exception);
                return null;
            }
        }
    }

    /// <summary>
    /// The thumbnail the OS shell or the file's EXIF data already has, without decoding the image; null if neither has
    /// one.
    /// </summary>
    public static Bitmap? GetReadyMade(FileInfo fileInfo, uint height, IPlatformSpecificService? platform = null)
    {
        if (platform is not null && fileInfo.IsCommon() && (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()))
        {
            return GetShellThumb(fileInfo.FullName, 0, (int)height, platform) ?? GetExifThumb(fileInfo.FullName);
        }
        return GetExifThumb(fileInfo.FullName);
    }

    private static WriteableBitmap? GetExifThumb(string path)
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
    private static WriteableBitmap? GetShellThumb(string path, int width, int height, IPlatformSpecificService platform)
    {
        try
        {
            var pixels = platform.GetShellThumbnail(path, width, height,
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

    private static Bitmap? CreateThumb(MagickImage magick, FileInfo fileInfo, uint height)
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
            case MagickFormat.Ico:
            case MagickFormat.Icon:
            case MagickFormat.Wbmp:
            {
                return GetSkBitmapThumb(fileInfo, height);
            }
        
            case MagickFormat.Svg:
            case MagickFormat.Svgz:
                // A bitmap, not the viewer's DrawingImage: that would be built on the UI thread and replay every
                // shape while the gallery scrolls
                return SvgFallback.RenderThumb(fileInfo, magick.Width, magick.Height, height);
            default:
            {
                var rawPreviewThumb = GetRawPreviewThumb(magick, height);
                if (rawPreviewThumb is not null)
                {
                    return rawPreviewThumb;
                }

                // Read the file natively instead of through a buffer the size of the file, and pass the pinged format
                // so formats without a signature (TGA) are still recognized
                var settings = new MagickReadSettings();
                if (magick.Format is not MagickFormat.Unknown)
                {
                    settings.Format = magick.Format;
                }
#pragma warning disable MA0042
                // ReSharper disable once MethodHasAsyncOverload
                magick.Read(fileInfo, settings);
#pragma warning restore MA0042

                var geometry = new MagickGeometry(0, height);
                magick.AutoOrient();
                magick.Thumbnail(geometry);
                return MagickBitmapConverter.ToBgraWriteableBitmap(magick);
            }
        }
    }

    /// <summary>
    /// Ping settings that also make the RAW coder keep the camera's embedded JPEG preview,
    /// so a RAW thumbnail does not need the whole sensor demosaiced.
    /// </summary>
    private static MagickReadSettings CreatePingSettings()
    {
        var settings = new MagickReadSettings();
        settings.SetDefines(new DngReadDefines { ReadThumbnail = true });
        return settings;
    }

    /// <summary>
    /// Makes the thumbnail from the embedded preview a RAW file was pinged with, decoding the JPEG at a reduced size.
    /// Returns null when there is no preview or it is smaller than <paramref name="height"/>.
    /// </summary>
    private static Bitmap? GetRawPreviewThumb(MagickImage magick, uint height)
    {
        var data = magick.GetProfile("dng:thumbnail")?.ToByteArray();
        if (data is null || data.Length == 0)
        {
            return null;
        }

        try
        {
            var settings = new MagickReadSettings { Format = MagickFormat.Jpeg };
            // Both sides at least height, so the preview is still tall enough after a 90 degree rotation
            settings.SetDefines(new JpegReadDefines { Size = new MagickGeometry(height, height) });
            using var preview = new MagickImage(data, settings);

            // The preview is stored in sensor orientation, the RAW file knows how the camera was held
            if (magick.Orientation is not Orientation.Undefined)
            {
                preview.Orientation = magick.Orientation;
            }
            preview.AutoOrient();
            if (preview.Height < height)
            {
                return null;
            }

            preview.Thumbnail(new MagickGeometry(0, height));
            return MagickBitmapConverter.ToBgraWriteableBitmap(preview);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetThumbnails), nameof(GetRawPreviewThumb), e);
            return null;
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
        // An icon stores each size as its own picture, and scaling picks the closest one,
        // so decode the largest, the one the viewer shows, and resize it
        var scaled = codec.EncodedFormat == SKEncodedImageFormat.Ico
            ? codec.Info.Size
            : codec.GetScaledDimensions((float)height / codec.Info.Height);

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
