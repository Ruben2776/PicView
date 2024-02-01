using System.Text;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.Models;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;

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
                    var isAnimated = magick.Count > 1;
                    imageModel.ImageType = isAnimated ? ImageType.Animated : ImageType.Raster;
                }
                goto default;

            case ".svg":
                imageModel.ImageType = ImageType.Vector;

                break;

            case ".svgz":
                imageModel.ImageType = ImageType.Vector;
                break;

            case ".b64":
                imageModel.ImageType = ImageType.Raster;
                var base64 = Encoding.UTF8.GetString(bytes);
                var base64Data = Convert.FromBase64String(base64);
                using (var base64Stream = new MemoryStream(base64Data))
                {
                    var b64 = new Bitmap(base64Stream);
                    imageModel.Image = b64;
                    imageModel.PixelWidth = b64.PixelSize.Width;
                    imageModel.PixelHeight = b64.PixelSize.Height;
                    break;
                }

            default:
                var bmp = new Bitmap(memoryStream);
                imageModel.Image = bmp;
                imageModel.PixelWidth = bmp.PixelSize.Width;
                imageModel.PixelHeight = bmp.PixelSize.Height;
                break;
        }
    }
}