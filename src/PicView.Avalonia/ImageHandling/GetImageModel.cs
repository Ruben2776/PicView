using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.ImageHandling;

public static class GetImageModel
{
    public static async Task<ImageModel?> GetImageModelAsync(FileInfo fileInfo, bool isThumb = false, uint height = 0)
    {
        if (fileInfo is null)
        {
#if DEBUG
            Console.WriteLine($"Error: {nameof(GetImageModel)}:{nameof(GetImageModelAsync)}: fileInfo is null");
#endif
            return new ImageModel
            {
                FileInfo = null,
                ImageType = ImageType.Invalid,
                Image = null, // TODO replace with error image
                PixelHeight = 0,
                PixelWidth = 0,
                EXIFOrientation = EXIFHelper.EXIFOrientation.None
            };
        }

        var imageModel = new ImageModel { FileInfo = fileInfo };

        try
        {
            var ext = fileInfo.Extension.ToLower();
            if (string.IsNullOrEmpty(ext))
            {
                using var magickImage = new MagickImage();
                magickImage.Ping(fileInfo);
                ext = magickImage.Format.ToString().ToLower();
            }

            switch (ext)
            {
                case ".webp":
                    if (isThumb)
                    {
                        await GetThumbnails.AddThumbAsync(fileInfo, imageModel, height).ConfigureAwait(false);
                    }
                    else
                    {
                        await AddImageAsync(fileInfo, imageModel).ConfigureAwait(false);
                        if (ImageFunctions.IsAnimated(fileInfo))
                        {
                            imageModel.ImageType = ImageType.AnimatedWebp;
                        }
                    }

                    break;
                case ".gif":
                    if (isThumb)
                    {
                        await GetThumbnails.AddThumbAsync(fileInfo, imageModel, height).ConfigureAwait(false);
                    }
                    else
                    {
                        await AddImageAsync(fileInfo, imageModel).ConfigureAwait(false);
                        if (ImageFunctions.IsAnimated(fileInfo))
                        {
                            imageModel.ImageType = ImageType.AnimatedGif;
                        }
                    }

                    break;
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".jpe":
                case ".bmp":
                case ".jfif":
                case ".ico":
                case ".wbmp":
                    if (isThumb)
                    {
                        await GetThumbnails.AddThumbAsync(fileInfo, imageModel, height).ConfigureAwait(false);
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
#if DEBUG
            Console.WriteLine($"Error: {nameof(GetImageModel)}:{nameof(GetImageModelAsync)}: \n{e}");
#endif
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
    }


    private static async Task AddImageAsync(FileInfo fileInfo, ImageModel imageModel)
    {
        if (fileInfo is null)
        {
#if DEBUG
            Console.WriteLine($"Error: {nameof(GetImageModel)}:{nameof(AddImageAsync)}: {nameof(fileInfo)} is null");
#endif
            return;
        }

        await using var fileStream = FileHelper.GetOptimizedFileStream(fileInfo);
        Add(fileStream, imageModel);
        imageModel.EXIFOrientation = EXIFHelper.GetImageOrientation(fileInfo);
    }

    private static async Task AddDefaultImageAsync(FileInfo fileInfo, ImageModel imageModel, bool isThumb, uint height)
    {
        if (isThumb)
        {
            await GetThumbnails.AddThumbAsync(fileInfo, imageModel, height).ConfigureAwait(false);
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
            imageModel.EXIFOrientation = EXIFHelper.GetImageOrientation(magickImage);
        }
    }


    #region Bitmap

    private static void Add(Stream stream, ImageModel imageModel)
    {
        if (stream is null)
        {
#if DEBUG
            Console.WriteLine($"Error: {nameof(GetImageModel)}:{nameof(Add)}: {nameof(stream)} is null");
#endif
            return;
        }

        var bitmap = new Bitmap(stream);
        imageModel.Image = bitmap;
        imageModel.PixelWidth = bitmap?.PixelSize.Width ?? 0;
        imageModel.PixelHeight = bitmap?.PixelSize.Height ?? 0;
        imageModel.ImageType = ImageType.Bitmap;
    }

    #endregion

    #region SVG

    private static void AddSvgImage(FileInfo fileInfo, ImageModel imageModel)
    {
        var svg = new MagickImage();
        svg.Ping(fileInfo.FullName);
        imageModel.PixelWidth = (int)svg.Width;
        imageModel.PixelHeight = (int)svg.Height;
        imageModel.ImageType = ImageType.Svg;
        imageModel.Image = fileInfo.FullName;
    }

    #endregion

    #region Base64

    private static async Task AddBase64ImageAsync(FileInfo fileInfo, ImageModel imageModel, bool isThumb, uint height)
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

    #endregion
}