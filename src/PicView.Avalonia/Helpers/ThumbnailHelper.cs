using Avalonia.Media.Imaging;
using ImageMagick;

namespace PicView.Avalonia.Helpers;

public static class ThumbnailHelper
{
    public static async Task<Bitmap?> GetThumb(FileInfo fileInfo, int height)
    {
        try
        {
            using var magick = new MagickImage();
            magick.Ping(fileInfo.FullName);
            var profile = magick.GetExifProfile();
            if (profile == null)
            {
                return await CreateThumb(magick).ConfigureAwait(false);
            }
            var thumbnail = profile.CreateThumbnail();
            if (thumbnail == null)
            {
                return await CreateThumb(magick).ConfigureAwait(false);
            }

            var byteArray = thumbnail.ToByteArray();
            var stream = new MemoryStream(byteArray);
            return new Bitmap(stream);
        }
        catch (Exception)
        {
            return null;
        }
        
        async Task<Bitmap> CreateThumb(IMagickImage magick)
        {
            if (fileInfo.Length >= 2147483648)
            {
                await using var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite, 4096, true);
                // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
                // ReSharper disable once MethodHasAsyncOverload
                magick.Read(fileStream);
            }
            else
            {
                await magick.ReadAsync(fileInfo.FullName);
            }

            var geometry = new MagickGeometry(0, height);
            magick.Thumbnail(geometry);
            magick.Format = MagickFormat.Png;
            await using var memoryStream = new MemoryStream();
            await magick.WriteAsync(memoryStream);
            memoryStream.Position = 0;
            return new Bitmap(memoryStream);
        }
    }
}
