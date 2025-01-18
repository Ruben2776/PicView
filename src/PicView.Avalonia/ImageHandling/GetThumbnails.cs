using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.ImageHandling;

public static class GetThumbnails
{
    public static async Task<Bitmap?> GetThumbAsync(string path, uint height, FileInfo? fileInfo = null)
    {
        try
        {
            using var magick = new MagickImage();
            magick.Ping(path);
            var profile = magick.GetExifProfile();
            if (profile == null)
            {
                return await CreateThumbAsync(magick, path, height, fileInfo).ConfigureAwait(false);
            }

            var thumbnail = profile.CreateThumbnail();
            if (thumbnail == null)
            {
                return await CreateThumbAsync(magick, path, height, fileInfo).ConfigureAwait(false);
            }

            var byteArray = thumbnail.ToByteArray();
            var stream = new MemoryStream(byteArray);
            return new Bitmap(stream);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static async Task<Bitmap?> CreateThumbAsync(MagickImage magick, string path, uint height,
        FileInfo? fileInfo = null)
    {
        // TODO: extract thumbnails from PlatformService and convert to Avalonia image,
        // I.E. https://boldena.com/article/64006
        // https://github.com/AvaloniaUI/Avalonia/discussions/16703
        // https://stackoverflow.com/a/42178963/2923736 convert to DLLImport to LibraryImport, source generation & AOT support
        
        fileInfo ??= new FileInfo(path);
        await using var fileStream = FileHelper.GetOptimizedFileStream(fileInfo);

        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".webp":
            case ".png":
            case ".jpg":
            case ".jpeg":
            case ".jpe":
            case ".bmp":
            case ".jfif":
            case ".ico":
            case ".wbmp":
                return Bitmap.DecodeToHeight(fileStream, (int)height);

            case ".svg":
            case ".svgz":
                return null;
        }
        

        if (fileInfo.Length >= 2147483648)
        {
            // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
            // ReSharper disable once MethodHasAsyncOverload
            magick.Read(fileStream);
        }
        else
        {
            await magick.ReadAsync(fileStream).ConfigureAwait(false);
        }

        var geometry = new MagickGeometry(0, height);
        magick.Thumbnail(geometry);
        return magick.ToWriteableBitmap();
    }
}

