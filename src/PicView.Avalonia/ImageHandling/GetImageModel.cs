using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.ImageHandling;

public static class GetImageModel
{
    public static async Task<ImageModel?> GetImageModelAsync(FileInfo fileInfo)
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
                    await AddImageAsync(fileInfo, imageModel).ConfigureAwait(false);
                    if (ImageFunctions.IsAnimated(fileInfo))
                    {
                        imageModel.ImageType = ImageType.AnimatedWebp;
                    }

                    break;
                case ".gif":
                    await AddImageAsync(fileInfo, imageModel).ConfigureAwait(false);
                    if (ImageFunctions.IsAnimated(fileInfo))
                    {
                        imageModel.ImageType = ImageType.AnimatedGif;
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
                    await AddImageAsync(fileInfo, imageModel).ConfigureAwait(false);

                    break;

                case ".svg":
                case ".svgz":
                    AddSvgImage(fileInfo, imageModel);
                    break;

                case ".b64":
                    await AddBase64ImageAsync(fileInfo, imageModel).ConfigureAwait(false);
                    break;

                default:
                    await AddDefaultImageAsync(fileInfo, imageModel).ConfigureAwait(false);
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

    private static async Task AddDefaultImageAsync(FileInfo fileInfo, ImageModel imageModel)
    {
        var bitmap = await GetImage.GetDefaultBitmapAsync(fileInfo).ConfigureAwait(false);
        SetModel(bitmap, fileInfo, imageModel);
    }


    #region Bitmap

    private static async Task AddImageAsync(FileInfo fileInfo, ImageModel imageModel)
    {
        var bitmap = await GetImage.GetStandardBitmapAsync(fileInfo).ConfigureAwait(false);
        SetModel(bitmap, fileInfo, imageModel);
    }

    private static void SetModel(Bitmap bitmap, FileInfo fileInfo, ImageModel imageModel)
    {
        imageModel.Image = bitmap;
        imageModel.PixelWidth = bitmap?.PixelSize.Width ?? 0;
        imageModel.PixelHeight = bitmap?.PixelSize.Height ?? 0;
        imageModel.ImageType = ImageType.Bitmap;
        imageModel.EXIFOrientation = EXIFHelper.GetImageOrientation(fileInfo);
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

    private static async Task AddBase64ImageAsync(FileInfo fileInfo, ImageModel imageModel)
    {
        var base64 = await GetImage.GetBase64ImageAsync(fileInfo).ConfigureAwait(false);
        SetModel(base64, fileInfo, imageModel);
    }

    #endregion
}