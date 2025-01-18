using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.ImageHandling;

public static class GetImage
{
    public static async Task<Bitmap?> GetStandardBitmapAsync(string file)
    {
        return await GetStandardBitmapAsync(new FileInfo(file)).ConfigureAwait(false);
    }
    
    public static async Task<Bitmap?> GetStandardBitmapAsync(FileInfo fileInfo)
    {
        if (fileInfo is null)
        {
#if DEBUG
            Console.WriteLine($"Error: {nameof(GetImage)}:{nameof(GetStandardBitmapAsync)}: {nameof(fileInfo)} is null");
#endif
            return null;
        }
        await using var fileStream = FileHelper.GetOptimizedFileStream(fileInfo);
        var bitmap = new Bitmap(fileStream);
        return bitmap;
    }
    
    public static async Task<Bitmap?> GetDefaultBitmapAsync(FileInfo fileInfo)
    {
        using var magickImage = new MagickImage();
        await using var fileStream = FileHelper.GetOptimizedFileStream(fileInfo);
        if (fileInfo.Length >= 2147483648)
        {
            // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
            // ReSharper disable once MethodHasAsyncOverload
            magickImage.Read(fileStream);
        }
        else
        {
            await magickImage.ReadAsync(fileStream).ConfigureAwait(false);
        }

        var bitmap = magickImage.ToWriteableBitmap();
        return bitmap;
    }
    
    public static async Task<Bitmap?> GetBase64ImageAsync(FileInfo fileInfo)
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
        var bitmap = magickImage.ToWriteableBitmap();
        magickImage.Dispose();
        return bitmap;
    }
}
