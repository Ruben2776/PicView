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
            case ".jpg":
            case ".jpeg":
            case ".jpe":
            case ".bmp":
            case ".jfif":
            case ".ico":
            case ".wbmp":
            {
                var bytes = await FileHelper.GetBytesFromFile(imageModel.FileInfo.FullName).ConfigureAwait(false);
                using var memoryStream = new MemoryStream(bytes);
                Add(memoryStream);
                return;
            }

            case ".svg":
                return;

            case ".svgz":
                return;

            case ".b64":
                {
                    var magickImage = await ImageDecoder.Base64ToMagickImage(imageModel.FileInfo).ConfigureAwait(false);
                    using var b64Stream = new MemoryStream();
                    await magickImage.WriteAsync(b64Stream);
                    b64Stream.Position = 0;
                    Add(b64Stream);
                    return;
                }
            
            default:
                {
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