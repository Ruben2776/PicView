using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.Models;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.Services;

public class ImageService
{
    public async Task LoadImageAsync(ImageModel imageModel)
    {
        var extension = imageModel.FileInfo.Extension.ToLowerInvariant();
        var bytes = await FileHelper.GetBytesFromFile(imageModel.FileInfo.FullName).ConfigureAwait(false);
        using var memoryStream = new MemoryStream(bytes);

        switch (extension)
        {
            case ".gif":
            case ".png":
            case ".webp":
                using (var magick = new MagickImageCollection(imageModel.FileInfo))
                {
                    imageModel.IsAnimated = magick.Count > 1;
                }
                goto default;

            case ".svg":
                break;

            case ".svgz":
                break;

            case ".b64":
                break;

            default:
                var bmp = new Bitmap(memoryStream);
                imageModel.Image = bmp;
                imageModel.PixelWidth = bmp.PixelSize.Width;
                imageModel.PixelHeight = bmp.PixelSize.Height;
                break;
        }
    }
}