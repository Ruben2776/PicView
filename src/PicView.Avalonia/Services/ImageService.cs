using System.IO;
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

        switch (extension)
        {
            case ".gif":
            case ".png":
            case ".webp":
                using (var magick = new MagickImageCollection(imageModel.FileInfo))
                {
                    var isAnimated = magick.Count > 1;
                    imageModel.ImageType = isAnimated ? ImageType.Animated : ImageType.Raster;
                    var animBytes = await FileHelper.GetBytesFromFile(imageModel.FileInfo.FullName)
                        .ConfigureAwait(false);
                    using var animStream = new MemoryStream(animBytes);
                    Add(animStream);
                }

                return;

            case ".svg":
                imageModel.ImageType = ImageType.Vector;

                return;

            case ".svgz":
                imageModel.ImageType = ImageType.Vector;
                return;

            case ".b64":
                {
                    imageModel.ImageType = ImageType.Raster;
                    var magickImage = await ImageDecoder.Base64ToMagickImage(imageModel.FileInfo).ConfigureAwait(false);
                    using var b64Stream = new MemoryStream();
                    await magickImage.WriteAsync(b64Stream);
                    b64Stream.Position = 0;
                    Add(b64Stream);
                    return;
                }

            case ".jpg":
            case ".jpeg":
            case ".jpe":
            case ".bmp":
            case ".jfif":
            case ".ico":
            case ".wbmp":
                {
                    imageModel.ImageType = ImageType.Raster;
                    var bytes = await FileHelper.GetBytesFromFile(imageModel.FileInfo.FullName).ConfigureAwait(false);
                    using var memoryStream = new MemoryStream(bytes);
                    Add(memoryStream);
                    return;
                }

            default:
                {
                    imageModel.ImageType = ImageType.Raster;
                    var magickImage = await ImageDecoder.GetMagickImageAsync(imageModel.FileInfo, extension).ConfigureAwait(false);
                    var byteArray = magickImage.ToByteArray(MagickFormat.WebP);
                    var memoryStream = new MemoryStream(byteArray);
                    Add(memoryStream);
                }
                return;

                void Add(Stream stream)
                {
                    var bmp = new Bitmap(stream);
                    imageModel.Image = bmp;
                    imageModel.PixelWidth = bmp.PixelSize.Width;
                    imageModel.PixelHeight = bmp.PixelSize.Height;
                }
        }
    }
}