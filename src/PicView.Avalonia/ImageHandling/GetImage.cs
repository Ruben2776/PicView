using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.Exif;
using PicView.Core.FileHandling;
using PicView.Core.ImageReading;

namespace PicView.Avalonia.ImageHandling;

public static class GetImage
{
    public static async ValueTask<object?> GetImageCore(FileInfo fileInfo, MagickImage? magickImage = null)
    {
        if (fileInfo is null)
        {
            return null;
        }

        var shouldDisposeMagickImage = magickImage is null;

        try
        {
            // Initialize MagickImage if not provided
            magickImage ??= CreateAndPingMagickImage(fileInfo);

            // Check if the image needs rotation based on EXIF
            var orientation = ExifOrientationHelper.GetImageOrientation(magickImage);
            var shouldAutoOrient = orientation is not (ExifOrientation.None or ExifOrientation.Horizontal);
            
            if (fileInfo.Extension.Equals(".b64", StringComparison.InvariantCultureIgnoreCase))
            {
                return await GetBase64ImageAsync(fileInfo).ConfigureAwait(false);
            }

            // Process the image based on type
            // If the image requires rotation, we bypass GetSkBitmapAsync to use Magick's AutoOrient
            return magickImage.Format switch
            {
                MagickFormat.WebP or
                    MagickFormat.WebM or
                    MagickFormat.Png or
                    MagickFormat.Png00 or
                    MagickFormat.Png8 or
                    MagickFormat.Png24 or
                    MagickFormat.Png32 or
                    MagickFormat.Png48 or
                    MagickFormat.Png64 or
                    MagickFormat.APng or
                    MagickFormat.Jpe or
                    MagickFormat.Jpeg or
                    MagickFormat.Pjpeg or
                    MagickFormat.Bmp or
                    MagickFormat.Tif or
                    MagickFormat.Tiff or
                    MagickFormat.Ico or
                    MagickFormat.Icon or
                    MagickFormat.Wbmp when !shouldAutoOrient => await GetSkBitmapAsync(fileInfo).ConfigureAwait(false),

                MagickFormat.Arw or
                    MagickFormat.Nef or
                    MagickFormat.Dng or
                    MagickFormat.Cr2 or
                    MagickFormat.Rw2 => await GetRawBitmapAsync(fileInfo, magickImage).ConfigureAwait(false),

                _ => await GetNonStandardBitmapAsync(fileInfo, magickImage).ConfigureAwait(false)
            };
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetImage), nameof(GetImageCore), e);
            return null;
        }
        finally
        {
            if (shouldDisposeMagickImage)
            {
                magickImage?.Dispose();
            }
        }
    }
    
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
}