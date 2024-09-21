using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.Navigation;

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

    public static async Task AddThumbAsync(FileInfo fileInfo, ImageModel imageModel, uint height)
    {
        var thumb = await GetThumbAsync(fileInfo.FullName, height, fileInfo).ConfigureAwait(false);
        imageModel.Image = thumb;
        imageModel.PixelWidth = thumb?.PixelSize.Width ?? 0;
        imageModel.PixelHeight = thumb?.PixelSize.Height ?? 0;
        imageModel.ImageType = ImageType.Bitmap;
    }

    public static async Task<Bitmap> CreateThumbAsync(IMagickImage magick, string path, uint height,
        FileInfo? fileInfo = null)
    {
        fileInfo ??= new FileInfo(path);
        await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite, 4096, true);
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
        magick.Format = MagickFormat.Png;
        await using var memoryStream = new MemoryStream();
        await magick.WriteAsync(memoryStream);
        memoryStream.Position = 0;
        return WriteableBitmap.Decode(memoryStream);
    }
}