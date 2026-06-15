using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageReading;

namespace PicView.Avalonia.ImageHandling;

public static class GetImage
{
    public static async ValueTask<Bitmap?> GetSkBitmapAsync(string file) =>
        await GetSkBitmapAsync(new FileInfo(file)).ConfigureAwait(false);

    public static async ValueTask<Bitmap?> GetSkBitmapAsync(FileInfo fileInfo)
    {
        if (fileInfo is null)
        {
            DebugHelper.LogDebug(nameof(GetImage), nameof(GetSkBitmapAsync), $"{nameof(fileInfo)} is null");
            return null;
        }
        await using var stream = FileStreamUtils.GetOptimizedFileStream(fileInfo);
        var bitmap = new Bitmap(stream);
        return bitmap;
    }
    
    public static async ValueTask<Bitmap?> GetNonStandardBitmapAsync(FileInfo fileInfo, MagickImage? magickImage)
    {
        var shouldDisposeMagickImage = magickImage is null;
        if (shouldDisposeMagickImage)
        {
            magickImage = new MagickImage();
        }
        magickImage = await MagickPerformanceReader.ReadMagickImageWithSpanAsync(fileInfo, magickImage);

        // Rotate image according to EXIF orientation
        magickImage.AutoOrient();
        TransformToSrgbIfNeeded(magickImage);

        var bitmap = magickImage.ToWriteableBitmap();
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

        var bitmap = magickImage.ToWriteableBitmap();
        if (shouldDisposeMagickImage)
        {
            magickImage.Dispose();
        }
        return bitmap;
    }
    
    public static async ValueTask<Bitmap?> GetBase64ImageAsync(FileInfo fileInfo)
    {
        var base64String = await File.ReadAllTextAsync(fileInfo.FullName).ConfigureAwait(false);
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
        
        await magickImage.ReadAsync(new MemoryStream(base64Data), readSettings).ConfigureAwait(false);

        // Rotate image according to EXIF orientation
        magickImage.AutoOrient();
        TransformToSrgbIfNeeded(magickImage);

        var bitmap = magickImage.ToWriteableBitmap();
        magickImage.Dispose();
        return bitmap;
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
}
