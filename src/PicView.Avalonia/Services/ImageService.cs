﻿using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.Models;
using PicView.Avalonia.Navigation;
using PicView.Core.FileHandling;
using PicView.Core.Gallery;
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
            case ".svgz":
                imageModel.Image = imageModel.FileInfo.FullName;
                var svg = new MagickImage();
                svg.Ping(imageModel.FileInfo.FullName);
                imageModel.PixelWidth = svg.Width;
                imageModel.PixelHeight = svg.Height;
                imageModel.ImageType = ImageType.Svg;
                return;

            case ".b64":
                {
                    using var magickImage = await ImageDecoder.Base64ToMagickImage(imageModel.FileInfo).ConfigureAwait(false);
                    using var b64Stream = new MemoryStream();
                    await magickImage.WriteAsync(b64Stream);
                    b64Stream.Position = 0;
                    Add(b64Stream);
                    return;
                }

            default:
                {
                    using var magickImage = new MagickImage();
                    if (imageModel.FileInfo.Length >= 2147483648)
                    {
                        await Task.Run(() =>
                        {
                            // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
                            // ReSharper disable once MethodHasAsyncOverload
                            magickImage.Read(imageModel.FileInfo);
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        await magickImage.ReadAsync(imageModel.FileInfo).ConfigureAwait(false);
                    }
                    magickImage.Format = MagickFormat.Png;
                    await using var memoryStream = new MemoryStream();
                    await magickImage.WriteAsync(memoryStream);
                    memoryStream.Position = 0;
                    Add(memoryStream);
                }
                return;

                void Add(Stream stream)
                {
                    var bmp = new Bitmap(stream);
                    imageModel.Image = bmp;
                    imageModel.PixelWidth = bmp.PixelSize.Width;
                    imageModel.PixelHeight = bmp.PixelSize.Height;
                    imageModel.ImageType = ImageType.Bitmap;
                }
        }
    }

    public class AvaloniaImageSource(Bitmap bitmap) : GalleryThumbInfo.IImageSource
    {
        private readonly Bitmap _bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));

        public object GetPlatformImageSource()
        {
            return _bitmap;
        }
    }
}