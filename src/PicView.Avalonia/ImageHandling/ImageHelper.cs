using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Gallery;
using PicView.Core.ImageDecoding;
using PicView.Core.Navigation;

namespace PicView.Avalonia.ImageHandling;

public static class ImageHelper
{
    public static async Task<ImageModel?> GetImageModelAsync(FileInfo fileInfo, bool isThumb = false, int height = 0)
    {
        return await Task.Run(async () =>
        {
            var imageModel = new ImageModel { FileInfo = fileInfo };

            try
            {
                switch (fileInfo.Extension.ToLower())
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
                        if (isThumb)
                        {
                            await AddThumbAsync(fileInfo, imageModel, height).ConfigureAwait(false);
                        }
                        else
                        {
                            await AddImageAsync(fileInfo, imageModel).ConfigureAwait(false);
                        }
                        break;

                    case ".svg":
                    case ".svgz":
                        AddSvgImage(fileInfo, imageModel);
                        break;

                    case ".b64":
                        await AddBase64ImageAsync(fileInfo, imageModel, isThumb, height).ConfigureAwait(false);
                        break;

                    default:
                        await AddDefaultImageAsync(fileInfo, imageModel, isThumb, height).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new ImageModel
                {
                    FileInfo = fileInfo,
                    ImageType = ImageType.Invalid,
                    Image = null, // TODO replace with error image
                    PixelHeight = 0,
                    PixelWidth = 0,
                    EXIFOrientation = EXIFHelper.EXIFOrientation.None
                };
            }

            return imageModel;
        });
    }

    private static async Task AddImageAsync(FileInfo fileInfo, ImageModel imageModel)
    {
        const int bufferSize = 4096;
        await using var fs = new FileStream(
                                        fileInfo.FullName,
                                        FileMode.Open,
                                        FileAccess.Read,
                                        FileShare.ReadWrite,
                                        bufferSize,
                                        useAsync: fileInfo.Length > 1e7);
        Add(fs, imageModel);
    }

    private static void AddSvgImage(FileInfo fileInfo, ImageModel imageModel)
    {
        var svg = new MagickImage();
        svg.Ping(fileInfo.FullName);
        imageModel.PixelWidth = svg.Width;
        imageModel.PixelHeight = svg.Height;
        imageModel.ImageType = ImageType.Svg;
        imageModel.Image = fileInfo.FullName;
    }

    private static async Task AddBase64ImageAsync(FileInfo fileInfo, ImageModel imageModel, bool isThumb, int height)
    {
        using var magickImage = await ImageDecoder.Base64ToMagickImage(fileInfo.FullName).ConfigureAwait(false);
        using var b64Stream = new MemoryStream();
        if (isThumb)
        {
            magickImage.Thumbnail(0, height);
        }
        else
        {
            await magickImage.WriteAsync(b64Stream);
            b64Stream.Position = 0;
        }
        Add(b64Stream, imageModel);
    }

    private static async Task AddDefaultImageAsync(FileInfo fileInfo, ImageModel imageModel, bool isThumb, int height)
    {
        if (isThumb)
        {
            await AddThumbAsync(fileInfo, imageModel, height).ConfigureAwait(false);
        }
        else
        {
            using var magickImage = new MagickImage();
            await using var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite, 4096, true);
            if (imageModel.FileInfo.Length >= 2147483648)
            {
                // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
                // ReSharper disable once MethodHasAsyncOverload
                magickImage.Read(fileStream);
            }
            else
            {
                await magickImage.ReadAsync(fileStream).ConfigureAwait(false);
            }
            magickImage.Format = MagickFormat.Png;
            await using var memoryStream = new MemoryStream();
            await magickImage.WriteAsync(memoryStream);
            memoryStream.Position = 0;
            Add(memoryStream, imageModel);
        }
    }

    private static async Task AddThumbAsync(FileInfo fileInfo, ImageModel imageModel, int height)
    {
        var thumb = await GetThumbAsync(fileInfo.FullName, height).ConfigureAwait(false);
        imageModel.Image = thumb;
        imageModel.PixelWidth = thumb?.PixelSize.Width ?? 0;
        imageModel.PixelHeight = thumb?.PixelSize.Height ?? 0;
        imageModel.ImageType = ImageType.Bitmap;
    }

    private static void Add(Stream stream, ImageModel imageModel)
    {
        var bitmap = new Bitmap(stream);
        imageModel.Image = bitmap;
        imageModel.PixelWidth = bitmap?.PixelSize.Width ?? 0;
        imageModel.PixelHeight = bitmap?.PixelSize.Height ?? 0;
        imageModel.ImageType = ImageType.Bitmap;
    }

    private static async Task<Bitmap?> GetThumbAsync(string path, int height)
    {
        try
        {
            using var magick = new MagickImage();
            magick.Ping(path);
            var profile = magick.GetExifProfile();
            if (profile == null)
            {
                return await CreateThumbAsync(magick, path, height).ConfigureAwait(false);
            }
            var thumbnail = profile.CreateThumbnail();
            if (thumbnail == null)
            {
                return await CreateThumbAsync(magick, path, height).ConfigureAwait(false);
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

    private static async Task<Bitmap> CreateThumbAsync(IMagickImage magick, string path, int height)
    {
        await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite, 4096, true);
        magick.Read(fileStream);

        var geometry = new MagickGeometry(0, height);
        magick.Thumbnail(geometry);
        magick.Format = MagickFormat.Png;
        await using var memoryStream = new MemoryStream();
        await magick.WriteAsync(memoryStream);
        memoryStream.Position = 0;
        return WriteableBitmap.Decode(memoryStream);
    }

    
    public static void SetImage(object image, Image imageControl, ImageType imageType)
    {
        imageControl.Source = imageType switch
        {
            ImageType.Svg => new SvgImage { Source = SvgSource.Load(image as string) },
            ImageType.Bitmap => image as Bitmap,
            ImageType.AnimatedBitmap => image as Bitmap,
            _ => imageControl.Source
        };
    }
}