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

public static class GetImage
{
    /// <summary>
    /// Decodes a common format (JPEG, PNG, WebP, ...) with Skia.
    /// Skia reads the file itself through native, memory-mapped I/O and decodes straight into the
    /// <see cref="WriteableBitmap"/> framebuffer, so no managed buffers or extra pixel copies are made.
    /// </summary>
    public static Bitmap? GetSkBitmap(FileInfo fileInfo)
    {
        if (fileInfo is null)
        {
            DebugHelper.LogDebug(nameof(GetImage), nameof(GetSkBitmap), $"{nameof(fileInfo)} is null");
            return null;
        }

        using var codec = SKCodec.Create(fileInfo.FullName);
        if (codec is null)
        {
            // Skia could not open or recognize the file natively, let Avalonia try the stream
            return GetSkBitmapFromStream(fileInfo);
        }

        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        if (info.Width <= 0 || info.Height <= 0)
        {
            return null;
        }

        var bitmap = new WriteableBitmap(new PixelSize(info.Width, info.Height), new Vector(96, 96),
            PixelFormat.Bgra8888, AlphaFormat.Premul);

        SKCodecResult result;
        using (var framebuffer = bitmap.Lock())
        {
            result = framebuffer.RowBytes == info.RowBytes
                ? codec.GetPixels(info, framebuffer.Address)
                : SKCodecResult.InvalidParameters;
        }

        // Truncated files still show what could be decoded, matching SKBitmap.Decode
        if (result is SKCodecResult.Success or SKCodecResult.IncompleteInput)
        {
            return bitmap;
        }

        bitmap.Dispose();
        DebugHelper.LogDebug(nameof(GetImage), nameof(GetSkBitmap), $"{fileInfo.FullName}: {result}");
        return GetSkBitmapFromStream(fileInfo);
    }

    private static Bitmap? GetSkBitmapFromStream(FileInfo fileInfo)
    {
        using var stream = FileStreamUtils.GetOptimizedFileStream(fileInfo);
        return new Bitmap(stream);
    }

    public static async ValueTask<Bitmap?> GetNonStandardBitmapAsync(FileInfo fileInfo, MagickImage? magickImage = null)
    {
        var shouldDisposeMagickImage = magickImage is null;
        if (shouldDisposeMagickImage)
        {
            magickImage = new MagickImage();
        }

        try
        {
            magickImage = await MagickPerformanceReader.ReadMagickImageWithSpanAsync(fileInfo, magickImage).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            magickImage?.Dispose();
            DebugHelper.LogDebug(nameof(GetImage), nameof(GetNonStandardBitmapAsync), e);
            return fileInfo.IsCommon() ? GetSkBitmap(fileInfo) : null;
        }

        // Rotate image according to EXIF orientation
        magickImage.AutoOrient();
        TransformToSrgbIfNeeded(magickImage);

        var bitmap = MagickBitmapConverter.ToBgraWriteableBitmap(magickImage);
        if (shouldDisposeMagickImage)
        {
            magickImage.Dispose();
        }
        return bitmap;
    }
    
    public static async ValueTask<Bitmap?> GetRawBitmapAsync(FileInfo fileInfo, MagickImage? magickImage)
    {
        var shouldDisposeMagickImage = magickImage is null;
        if (shouldDisposeMagickImage)
        {
            magickImage = new MagickImage();
        }
        // Raw images needs to be loaded by file path, else it just loads thumbnail 
        // https://github.com/Ruben2776/PicView/issues/221
        await magickImage.ReadAsync(fileInfo).ConfigureAwait(false);

        // Rotate image according to EXIF orientation
        magickImage.AutoOrient();
        TransformToSrgbIfNeeded(magickImage);

        var bitmap = MagickBitmapConverter.ToBgraWriteableBitmap(magickImage);
        if (shouldDisposeMagickImage)
        {
            magickImage.Dispose();
        }
        return bitmap;
    }
    
    public static async ValueTask<Bitmap?> GetBase64ImageAsync(FileInfo fileInfo, CancellationToken ct = default)
    {
        try
        {
            var base64String = await File.ReadAllTextAsync(fileInfo.FullName, ct).ConfigureAwait(false);
            return await GetBase64ImageAsync(base64String, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetImage), nameof(GetBase64ImageAsync), e);
            return null;
        }
    }
    
    public static async ValueTask<Bitmap?> GetBase64ImageAsync(string base64String, CancellationToken ct = default)
    {
        try 
        {
            var base64Data = Convert.FromBase64String(base64String);
            var magickImage = new MagickImage
            {
                Quality = 100,
                ColorSpace = ColorSpace.Transparent
            };

            var readSettings = new MagickReadSettings
            {
                Density = new Density(300, 300),
                BackgroundColor = MagickColors.Transparent
            };
        
            await magickImage.ReadAsync(new MemoryStream(base64Data), readSettings, ct).ConfigureAwait(false);

            // Rotate image according to EXIF orientation
            magickImage.AutoOrient();
            TransformToSrgbIfNeeded(magickImage);

            var bitmap = MagickBitmapConverter.ToBgraWriteableBitmap(magickImage);
            magickImage.Dispose();
            return bitmap;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetImage), nameof(GetBase64ImageAsync), e);
            return null;
        }
    }
    
    public static MagickImage CreateAndPingMagickImage(FileInfo fileInfo)
    {
        var magickImage = new MagickImage();
        magickImage.Ping(fileInfo);
        return magickImage;
    }

    internal static void TransformToSrgbIfNeeded(MagickImage magickImage)
    {
        if (magickImage.GetColorProfile() is null)
        {
            return;
        }

        try
        {
            magickImage.TransformColorSpace(ColorProfiles.SRGB);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetImage), nameof(TransformToSrgbIfNeeded), e);
        }
    }
    
    public static WriteableBitmap? GetShellBitmap(string path, CoreViewModel core)
    {
        try
        {
            var platformService = core.PlatformService;

            var pixels = platformService.GetPixelsFromNativeImagingComponent(path, out var pixelWidth, out var pixelHeight);

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
            DebugHelper.LogDebug(nameof(GetImage), nameof(GetShellBitmap), e);
            return null;
        }
    }
}
